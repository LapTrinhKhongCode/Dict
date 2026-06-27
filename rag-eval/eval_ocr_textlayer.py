# -*- coding: utf-8 -*-
"""
Evaluate OCR output against the native text layer of copyable PDF files.

This script is intended for the thesis OCR evaluation section. It does not use
manual ground truth; instead, it uses PDF text extracted by PyMuPDF as
pseudo-ground-truth and compares it with OCR text stored by the backend.

Outputs:
  - results/ocr_textlayer_pages.csv
  - results/ocr_textlayer_summary.json
  - chart_ocr_cer_by_document.png
  - chart_ocr_cer_distribution.png
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import statistics
import tempfile
import unicodedata
from collections import defaultdict
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import fitz  # PyMuPDF
import matplotlib
import pyodbc
import requests
import urllib3
from dotenv import load_dotenv
from rapidfuzz.distance import Levenshtein

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402


urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

ROOT = Path(__file__).resolve().parent
RESULTS_DIR = ROOT / "results"
RESULTS_DIR.mkdir(exist_ok=True)


@dataclass
class PageMetric:
    job_id: int
    file_name: str
    page: int
    ref_chars: int
    ocr_chars: int
    distance: int
    cer: float
    accuracy: float
    char_recall: float
    char_precision: float
    char_f1: float
    status: str


def normalize_text(text: str, compact: bool = True) -> str:
    """Normalize Japanese/Latin PDF/OCR text for page-level CER."""
    text = unicodedata.normalize("NFKC", text or "")
    text = re.sub(r"\[\[RAG_STRUCTURED\b.*?\]\]", " ", text, flags=re.I | re.S)
    text = re.sub(r"\[\[/RAG_STRUCTURED\]\]", " ", text, flags=re.I)
    # Remove common headers injected by OCR pipelines, e.g. "[Trang 2]".
    text = re.sub(r"\[Trang\s+\d+\]", " ", text, flags=re.I)
    # Normalize punctuation variants that are not meaningful for OCR quality here.
    text = text.replace("–", "-").replace("—", "-").replace("−", "-")
    text = text.replace("“", '"').replace("”", '"').replace("’", "'")
    if compact:
        # For Japanese PDFs, text extraction and OCR often disagree on whitespace
        # segmentation. Removing whitespace makes CER reflect character recognition.
        text = re.sub(r"\s+", "", text)
    else:
        text = re.sub(r"\s+", " ", text).strip()
    return text


def char_bag_scores(ocr: str, ref: str) -> tuple[float, float, float]:
    """Order-insensitive character coverage for PDFs with different reading order."""
    if not ref:
        return 1.0, 1.0, 1.0
    ref_counts = Counter(ref)
    ocr_counts = Counter(ocr)
    overlap = sum(min(ref_counts[ch], ocr_counts.get(ch, 0)) for ch in ref_counts)
    recall = overlap / max(sum(ref_counts.values()), 1)
    precision = overlap / max(sum(ocr_counts.values()), 1)
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) else 0.0
    return recall, precision, f1


def extract_pdf_text_pages(pdf_path: Path) -> dict[int, str]:
    pages: dict[int, str] = {}
    with fitz.open(pdf_path) as doc:
        for idx, page in enumerate(doc, start=1):
            pages[idx] = page.get_text("text") or ""
    return pages


def download_file(url: str, out_path: Path, timeout: int = 90) -> None:
    with requests.get(url, stream=True, timeout=timeout, verify=False) as response:
        response.raise_for_status()
        with out_path.open("wb") as f:
            for chunk in response.iter_content(chunk_size=1024 * 1024):
                if chunk:
                    f.write(chunk)


def get_project_files(api_base: str, token: str, project_id: int) -> list[dict[str, Any]]:
    headers = {"Authorization": f"Bearer {token}"}
    response = requests.get(
        f"{api_base}/api/Projects/{project_id}/files",
        headers=headers,
        verify=False,
        timeout=60,
    )
    response.raise_for_status()
    return response.json()


def build_odbc_connection_string() -> str:
    appsettings = (ROOT.parent / "Dict" / "appsettings.json").read_text(encoding="utf-8-sig")
    match = re.search(r'"DefaultConnection"\s*:\s*"([^"]+)"', appsettings)
    if not match:
        raise RuntimeError("Cannot find ConnectionStrings:DefaultConnection in Dict/appsettings.json")

    parts: dict[str, str] = {}
    for part in match.group(1).strip(";").split(";"):
        if "=" not in part:
            continue
        key, value = part.split("=", 1)
        parts[key.strip().lower()] = value.strip()

    server = parts.get("server")
    database = parts.get("initial catalog")
    user = parts.get("user id")
    password = parts.get("password")
    if not all([server, database, user, password]):
        raise RuntimeError("DefaultConnection is missing server/database/user/password")

    return (
        "DRIVER={ODBC Driver 17 for SQL Server};"
        f"SERVER={server};DATABASE={database};UID={user};PWD={password};"
        "Encrypt=yes;TrustServerCertificate=no;Connection Timeout=120;"
    )


def get_project_files_sql(project_id: int) -> list[dict[str, Any]]:
    conn = pyodbc.connect(build_odbc_connection_string(), timeout=120)
    try:
        rows = conn.cursor().execute(
            """
            SELECT j.Id, j.Status, m.FileName, m.StorageUrl, m.MimeType, m.SizeBytes
            FROM ocr_jobs j
            INNER JOIN media_store m ON m.Id = j.MediaId
            WHERE j.ProjectId = ?
              AND (LOWER(m.FileName) LIKE '%.pdf' OR LOWER(m.MimeType) LIKE '%pdf%')
            ORDER BY j.Id DESC
            """,
            project_id,
        ).fetchall()
        return [
            {
                "id": int(r.Id),
                "status": r.Status,
                "name": r.FileName,
                "imageUrl": r.StorageUrl,
                "type": "pdf",
                "mimeType": r.MimeType,
                "sizeBytes": r.SizeBytes,
            }
            for r in rows
        ]
    finally:
        conn.close()


def get_job_detail_sql(job_id: int) -> dict[str, Any]:
    conn = pyodbc.connect(build_odbc_connection_string(), timeout=120)
    try:
        cur = conn.cursor()
        job = cur.execute("SELECT Id, Status, DetectedText FROM ocr_jobs WHERE Id = ?", job_id).fetchone()
        if not job:
            raise KeyError(f"OCR job {job_id} not found")
        rows = cur.execute(
            """
            SELECT PageNumber, WordText, BoundingBox
            FROM ocr_results
            WHERE OcrJobId = ?
            ORDER BY PageNumber, Id
            """,
            job_id,
        ).fetchall()
        return {
            "jobId": int(job.Id),
            "status": job.Status,
            "detectedText": job.DetectedText or "",
            "results": [
                {
                    "pageNumber": int(r.PageNumber or 1),
                    "wordText": r.WordText or "",
                    "boundingBox": r.BoundingBox or "[]",
                }
                for r in rows
            ],
        }
    finally:
        conn.close()


def get_job_detail(api_base: str, token: str, job_id: int, timeout: int) -> dict[str, Any]:
    headers = {"Authorization": f"Bearer {token}"}
    response = requests.get(
        f"{api_base}/api/Infer/job/{job_id}",
        headers=headers,
        verify=False,
        timeout=timeout,
    )
    response.raise_for_status()
    return response.json()


def ocr_pages_from_job(job: dict[str, Any]) -> dict[int, str]:
    by_page: dict[int, list[str]] = defaultdict(list)
    for item in job.get("results") or []:
        page = int(item.get("pageNumber") or 1)
        text = (item.get("wordText") or "").strip()
        if text:
            by_page[page].append(text)
    return {page: "\n".join(parts) for page, parts in by_page.items()}


def evaluate_one_document(
    file_info: dict[str, Any],
    api_base: str,
    token: str,
    tmp_dir: Path,
    job_timeout: int,
    min_ref_chars: int,
    max_pages_per_doc: int,
    include_missing_ocr_pages: bool,
    source: str,
) -> tuple[list[PageMetric], dict[str, Any]]:
    job_id = int(file_info["id"])
    file_name = file_info.get("name") or f"job_{job_id}.pdf"
    url = file_info.get("imageUrl")
    if not url:
        return [], {"job_id": job_id, "file_name": file_name, "status": "skip_no_url"}

    pdf_path = tmp_dir / f"{job_id}_{re.sub(r'[^A-Za-z0-9_.-]+', '_', file_name)}"
    download_file(url, pdf_path)
    ref_pages_raw = extract_pdf_text_pages(pdf_path)

    job = get_job_detail_sql(job_id) if source == "sql" else get_job_detail(api_base, token, job_id, timeout=job_timeout)
    ocr_pages_raw = ocr_pages_from_job(job)

    # By default, evaluate only pages that actually have OCR rows. In this app,
    # PDF OCR can be lazy-loaded page by page when the user opens pages in the
    # reader, so treating unopened pages as OCR errors would unfairly measure
    # user coverage instead of OCR recognition quality.
    if include_missing_ocr_pages:
        pages = sorted(ref_pages_raw.keys())
    else:
        pages = sorted(set(ref_pages_raw.keys()) & set(ocr_pages_raw.keys()))
    if max_pages_per_doc > 0:
        pages = pages[:max_pages_per_doc]

    metrics: list[PageMetric] = []
    skipped_blank_ref = 0
    for page in pages:
        ref = normalize_text(ref_pages_raw.get(page, ""))
        ocr = normalize_text(ocr_pages_raw.get(page, ""))
        if len(ref) < min_ref_chars:
            skipped_blank_ref += 1
            continue

        distance = Levenshtein.distance(ocr, ref)
        cer = min(distance / max(len(ref), 1), 1.0)
        char_recall, char_precision, char_f1 = char_bag_scores(ocr, ref)
        metrics.append(
            PageMetric(
                job_id=job_id,
                file_name=file_name,
                page=page,
                ref_chars=len(ref),
                ocr_chars=len(ocr),
                distance=distance,
                cer=cer,
                accuracy=max(0.0, 1.0 - cer),
                char_recall=char_recall,
                char_precision=char_precision,
                char_f1=char_f1,
                status=job.get("status", file_info.get("status", "")),
            )
        )

    meta = {
        "job_id": job_id,
        "file_name": file_name,
        "api_status": job.get("status", file_info.get("status")),
        "pdf_pages": len(ref_pages_raw),
        "ocr_pages": len(ocr_pages_raw),
        "ocr_page_numbers": sorted(ocr_pages_raw.keys()),
        "evaluated_pages": len(metrics),
        "skipped_blank_ref_pages": skipped_blank_ref,
        "pdf_path": str(pdf_path),
    }
    return metrics, meta


def summarize(metrics: list[PageMetric], metas: list[dict[str, Any]]) -> dict[str, Any]:
    by_doc: dict[int, list[PageMetric]] = defaultdict(list)
    for m in metrics:
        by_doc[m.job_id].append(m)

    doc_rows = []
    for job_id, rows in sorted(by_doc.items()):
        cer_values = [r.cer for r in rows]
        weighted_distance = sum(r.distance for r in rows)
        weighted_ref = sum(r.ref_chars for r in rows)
        weighted_cer = weighted_distance / weighted_ref if weighted_ref else 0.0
        # Weighted order-insensitive coverage by page reference length.
        weighted_char_recall = sum(r.char_recall * r.ref_chars for r in rows) / weighted_ref if weighted_ref else 0.0
        weighted_char_precision = (
            sum(r.char_precision * r.ocr_chars for r in rows) / max(sum(r.ocr_chars for r in rows), 1)
            if rows else 0.0
        )
        weighted_char_f1 = (
            2 * weighted_char_precision * weighted_char_recall / (weighted_char_precision + weighted_char_recall)
            if (weighted_char_precision + weighted_char_recall) else 0.0
        )
        doc_rows.append(
            {
                "job_id": job_id,
                "file_name": rows[0].file_name,
                "pages": len(rows),
                "ref_chars": weighted_ref,
                "ocr_chars": sum(r.ocr_chars for r in rows),
                "mean_cer": statistics.mean(cer_values),
                "median_cer": statistics.median(cer_values),
                "weighted_cer": weighted_cer,
                "weighted_accuracy": max(0.0, 1.0 - weighted_cer),
                "char_recall": weighted_char_recall,
                "char_precision": weighted_char_precision,
                "char_f1": weighted_char_f1,
            }
        )

    all_ref = sum(m.ref_chars for m in metrics)
    all_distance = sum(m.distance for m in metrics)
    weighted_cer = all_distance / all_ref if all_ref else 0.0
    all_ocr = sum(m.ocr_chars for m in metrics)
    weighted_char_recall = sum(m.char_recall * m.ref_chars for m in metrics) / all_ref if all_ref else 0.0
    weighted_char_precision = sum(m.char_precision * m.ocr_chars for m in metrics) / all_ocr if all_ocr else 0.0
    weighted_char_f1 = (
        2 * weighted_char_precision * weighted_char_recall / (weighted_char_precision + weighted_char_recall)
        if (weighted_char_precision + weighted_char_recall) else 0.0
    )
    return {
        "method": "PDF text layer used as pseudo-ground-truth; whitespace removed after NFKC normalization",
        "documents_seen": len(metas),
        "documents_evaluated": len(doc_rows),
        "pages_evaluated": len(metrics),
        "total_ref_chars": all_ref,
        "weighted_cer": weighted_cer,
        "weighted_accuracy": max(0.0, 1.0 - weighted_cer),
        "weighted_char_recall": weighted_char_recall,
        "weighted_char_precision": weighted_char_precision,
        "weighted_char_f1": weighted_char_f1,
        "mean_page_cer": statistics.mean([m.cer for m in metrics]) if metrics else 0.0,
        "median_page_cer": statistics.median([m.cer for m in metrics]) if metrics else 0.0,
        "documents": doc_rows,
        "job_meta": metas,
    }


def write_outputs(metrics: list[PageMetric], summary: dict[str, Any]) -> None:
    csv_path = RESULTS_DIR / "ocr_textlayer_pages.csv"
    with csv_path.open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=[
                "job_id",
                "file_name",
                "page",
                "status",
                "ref_chars",
                "ocr_chars",
                "distance",
                "cer",
                "accuracy",
                "char_recall",
                "char_precision",
                "char_f1",
            ],
        )
        writer.writeheader()
        for m in metrics:
            writer.writerow(
                {
                    "job_id": m.job_id,
                    "file_name": m.file_name,
                    "page": m.page,
                    "status": m.status,
                    "ref_chars": m.ref_chars,
                    "ocr_chars": m.ocr_chars,
                    "distance": m.distance,
                    "cer": round(m.cer, 6),
                    "accuracy": round(m.accuracy, 6),
                    "char_recall": round(m.char_recall, 6),
                    "char_precision": round(m.char_precision, 6),
                    "char_f1": round(m.char_f1, 6),
                }
            )

    json_path = RESULTS_DIR / "ocr_textlayer_summary.json"
    json_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")


def plot_outputs(summary: dict[str, Any], metrics: list[PageMetric]) -> None:
    docs = summary.get("documents", [])
    if docs:
        labels = [f"Job {d['job_id']}" for d in docs]
        values = [d["char_recall"] * 100 for d in docs]
        fig, ax = plt.subplots(figsize=(12, 5))
        bars = ax.bar(range(len(values)), values, color="#42A5F5")
        for bar, val in zip(bars, values):
            ax.text(bar.get_x() + bar.get_width() / 2, val + 0.2, f"{val:.1f}%", ha="center", fontsize=8)
        ax.set_xticks(range(len(labels)))
        ax.set_xticklabels(labels, rotation=0, fontsize=8)
        ax.set_ylim(0, 105)
        ax.set_ylabel("Character coverage / recall (%)")
        ax.set_title("Hình 4.X. OCR character coverage theo tài liệu\n(PDF text layer làm pseudo-ground-truth, bỏ qua khác biệt thứ tự đọc)")
        ax.grid(axis="y", alpha=0.3)
        ax.spines["top"].set_visible(False)
        ax.spines["right"].set_visible(False)
        plt.tight_layout()
        plt.savefig(ROOT / "chart_ocr_cer_by_document.png", dpi=150, bbox_inches="tight")
        plt.close()

    if metrics:
        cers = [m.cer * 100 for m in metrics]
        recalls = [m.char_recall * 100 for m in metrics]
        fig, ax = plt.subplots(figsize=(9, 5))
        ax.hist(recalls, bins=20, color="#66BB6A", edgecolor="white")
        ax.axvline(statistics.mean(recalls), color="#E53935", linestyle="--", linewidth=2, label=f"Mean={statistics.mean(recalls):.1f}%")
        ax.axvline(statistics.median(recalls), color="#1565C0", linestyle=":", linewidth=2, label=f"Median={statistics.median(recalls):.1f}%")
        ax.set_xlabel("Character coverage per page (%)")
        ax.set_ylabel("Số trang")
        ax.set_title("Hình 4.X+1. Phân bố character coverage theo trang OCR")
        ax.legend()
        ax.grid(axis="y", alpha=0.3)
        ax.spines["top"].set_visible(False)
        ax.spines["right"].set_visible(False)
        plt.tight_layout()
        plt.savefig(ROOT / "chart_ocr_cer_distribution.png", dpi=150, bbox_inches="tight")
        plt.close()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-id", type=int, default=None)
    parser.add_argument("--job-id", type=int, action="append", default=None, help="Evaluate only selected job id(s)")
    parser.add_argument("--job-timeout", type=int, default=180)
    parser.add_argument("--min-ref-chars", type=int, default=30)
    parser.add_argument("--max-pages-per-doc", type=int, default=0, help="0 = all pages")
    parser.add_argument(
        "--include-missing-ocr-pages",
        action="store_true",
        help="Count PDF pages without OCR rows as full errors. Default evaluates only pages with OCR rows.",
    )
    parser.add_argument("--keep-pdfs", action="store_true")
    parser.add_argument("--source", choices=["sql", "api"], default="sql", help="Read OCR rows from SQL directly or backend API")
    args = parser.parse_args()

    load_dotenv(ROOT / ".env")
    api_base = os.environ["API_BASE_URL"].rstrip("/")
    token = os.environ["API_TOKEN"]
    project_id = args.project_id or int(os.environ.get("PROJECT_ID", "13"))

    files = get_project_files_sql(project_id) if args.source == "sql" else get_project_files(api_base, token, project_id)
    pdf_files = [
        f for f in files
        if (f.get("type") == "pdf" or str(f.get("name", "")).lower().endswith(".pdf"))
    ]
    if args.job_id:
        wanted = set(args.job_id)
        pdf_files = [f for f in pdf_files if int(f["id"]) in wanted]

    tmp_parent = RESULTS_DIR / "ocr_eval_pdfs" if args.keep_pdfs else None
    if tmp_parent:
        tmp_parent.mkdir(exist_ok=True)
        tmp_ctx = None
        tmp_dir = tmp_parent
    else:
        tmp_ctx = tempfile.TemporaryDirectory()
        tmp_dir = Path(tmp_ctx.name)

    all_metrics: list[PageMetric] = []
    metas: list[dict[str, Any]] = []
    try:
        for idx, file_info in enumerate(pdf_files, start=1):
            job_id = int(file_info["id"])
            name = file_info.get("name")
            print(f"[{idx}/{len(pdf_files)}] job={job_id} {name} status={file_info.get('status')}")
            try:
                metrics, meta = evaluate_one_document(
                    file_info=file_info,
                    api_base=api_base,
                    token=token,
                    tmp_dir=tmp_dir,
                    job_timeout=args.job_timeout,
                    min_ref_chars=args.min_ref_chars,
                    max_pages_per_doc=args.max_pages_per_doc,
                    include_missing_ocr_pages=args.include_missing_ocr_pages,
                    source=args.source,
                )
                all_metrics.extend(metrics)
                metas.append(meta)
                if metrics:
                    doc_cer = sum(m.distance for m in metrics) / max(sum(m.ref_chars for m in metrics), 1)
                    print(f"  pages={len(metrics)} CER={doc_cer*100:.2f}%")
                else:
                    print(f"  skipped: no evaluable pages ({meta})")
            except Exception as exc:
                print(f"  ERROR: {type(exc).__name__}: {exc}")
                metas.append({
                    "job_id": job_id,
                    "file_name": name,
                    "status": "error",
                    "error": f"{type(exc).__name__}: {exc}",
                })

        summary = summarize(all_metrics, metas)
        write_outputs(all_metrics, summary)
        plot_outputs(summary, all_metrics)
        print("\n=== OCR text-layer evaluation summary ===")
        print(f"Documents evaluated : {summary['documents_evaluated']}/{summary['documents_seen']}")
        print(f"Pages evaluated     : {summary['pages_evaluated']}")
        print(f"Weighted CER        : {summary['weighted_cer']*100:.2f}%")
        print(f"Weighted accuracy   : {summary['weighted_accuracy']*100:.2f}%")
        print(f"Char coverage       : {summary['weighted_char_recall']*100:.2f}%")
        print(f"Char precision      : {summary['weighted_char_precision']*100:.2f}%")
        print(f"Char F1             : {summary['weighted_char_f1']*100:.2f}%")
        print(f"Mean page CER       : {summary['mean_page_cer']*100:.2f}%")
        print(f"Median page CER     : {summary['median_page_cer']*100:.2f}%")
        print(f"CSV                 : {RESULTS_DIR / 'ocr_textlayer_pages.csv'}")
        print(f"JSON                : {RESULTS_DIR / 'ocr_textlayer_summary.json'}")
    finally:
        if tmp_ctx is not None:
            tmp_ctx.cleanup()


if __name__ == "__main__":
    main()
