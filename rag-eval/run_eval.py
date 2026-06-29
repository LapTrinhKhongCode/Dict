#!/usr/bin/env python3
"""
run_eval.py — Calls the backend project-scope RAG stream API for each question
in the dataset and saves raw results to results/raw_{dataset}.jsonl

Usage:
  python run_eval.py --dataset test_set    (default)
  python run_eval.py --dataset dev_set
  python run_eval.py --dataset holdout_set

Requires .env:
  API_BASE_URL=https://localhost:7084
  API_TOKEN=<your JWT token>
  PROJECT_ID=<numeric project id>     # all docs must be indexed under this project
  TOP_K=5
"""

import argparse
import json
import os
import re
import sys
import time
from pathlib import Path

import requests
import urllib3
from dotenv import load_dotenv
from tqdm import tqdm

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

DATASETS_DIR = Path(__file__).parent / "datasets"
RESULTS_DIR  = Path(__file__).parent / "results"
RESULTS_DIR.mkdir(exist_ok=True)

load_dotenv(Path(__file__).parent / ".env")

API_BASE = os.environ["API_BASE_URL"].rstrip("/")
TOKEN    = os.environ["API_TOKEN"]
PROJECT_ID = int(os.environ["PROJECT_ID"])
TOP_K    = int(os.environ.get("TOP_K", "5"))
MODE     = os.environ.get("MODE", "high")
DELAY_BETWEEN = float(os.environ.get("DELAY_BETWEEN", "3"))   # seconds between questions
RETRIES  = int(os.environ.get("RETRIES", "3"))

HEADERS = {
    "Authorization": f"Bearer {TOKEN}",
    "Content-Type": "application/json",
    "Accept": "text/event-stream",
}

STREAM_URL = f"{API_BASE}/api/rag/project/{PROJECT_ID}/ask/stream"


def get_available_datasets() -> list[str]:
    return sorted(path.stem for path in DATASETS_DIR.glob("*.jsonl") if path.stem != "job_map")


def parse_sse(raw: str) -> dict:
    """Parse SSE events from a streamed response string → collected fields."""
    result = {"answer": "", "sources": [], "citations": [], "error": None}
    for block in raw.split("\n\n"):
        block = block.strip()
        if not block:
            continue
        event_type = None
        data_lines = []
        for line in block.splitlines():
            if line.startswith("event:"):
                event_type = line[len("event:"):].strip()
            elif line.startswith("data:"):
                data_lines.append(line[len("data:"):].strip())
        data = "\n".join(data_lines)
        if event_type == "chunk":
            result["answer"] += data
        elif event_type == "sources":
            try:
                p = json.loads(data)
                result["sources"] = p.get("sources", [])
            except Exception:
                pass
        elif event_type == "done":
            try:
                p = json.loads(data)
                result["answer"] = p.get("answer", result["answer"])
                result["citations"] = p.get("citations", [])
            except Exception:
                pass
        elif event_type == "error":
            result["error"] = data
        elif event_type == "clarify":
            result["error"] = f"clarify_event: {data}"  # shouldn't happen with skipClarify=True
    return result


def call_rag(question: str) -> dict:
    payload = {"question": question, "topK": TOP_K, "mode": MODE, "skipClarify": True}
    for attempt in range(RETRIES + 1):
        try:
            resp = requests.post(
                STREAM_URL, headers=HEADERS, json=payload,
                timeout=120, verify=False, stream=True
            )
            if resp.status_code == 401:
                print("\n[FATAL] 401 Unauthorized — check API_TOKEN in .env", file=sys.stderr)
                sys.exit(1)
            result = parse_sse(resp.text)
            # Retry on transient API errors (network/gemini timeout)
            if result["error"] and attempt < RETRIES:
                wait = 5 * (attempt + 1)
                print(f"\n  [retry {attempt+1}/{RETRIES}] error={result['error'][:60]}... waiting {wait}s")
                time.sleep(wait)
                continue
            return result
        except requests.exceptions.Timeout:
            if attempt < RETRIES:
                time.sleep(5 * (attempt + 1))
            else:
                return {"answer": "", "sources": [], "citations": [], "error": "timeout"}
        except Exception as e:
            if attempt < RETRIES:
                time.sleep(5 * (attempt + 1))
            else:
                return {"answer": "", "sources": [], "citations": [], "error": str(e)}
    return {"answer": "", "sources": [], "citations": [], "error": "max_retries"}


def main():
    available_datasets = get_available_datasets()
    parser = argparse.ArgumentParser()
    parser.add_argument("--dataset", default="test_set",
                        choices=available_datasets)
    args = parser.parse_args()

    dataset_path = DATASETS_DIR / f"{args.dataset}.jsonl"
    output_path  = RESULTS_DIR / f"raw_{args.dataset}.jsonl"

    cases = [json.loads(line) for line in dataset_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    print(f"[run_eval] dataset={args.dataset}  questions={len(cases)}  project_id={PROJECT_ID}  mode={MODE}")
    print(f"[run_eval] output → {output_path}")

    with open(output_path, "w", encoding="utf-8") as fout:
        for i, case in enumerate(tqdm(cases, desc="Evaluating")):
            if i > 0:
                time.sleep(DELAY_BETWEEN)
            resp = call_rag(case["question"])
            record = {
                "id": case["id"],
                "category": case["category"],
                "scope": case["scope"],
                "question": case["question"],
                "source_file": case.get("source_file"),
                "source_pages": case.get("source_pages", []),
                "expected_points": case.get("expected_points", []),
                "expected_numbers": case.get("expected_numbers", []),
                "must_refuse": case.get("must_refuse", False),
                "answer": resp["answer"],
                "sources": resp["sources"],
                "citations": resp["citations"],
                "error": resp["error"],
            }
            fout.write(json.dumps(record, ensure_ascii=False) + "\n")
            fout.flush()

    print(f"[run_eval] Done. Results saved to {output_path}")


if __name__ == "__main__":
    main()
