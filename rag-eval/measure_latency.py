#!/usr/bin/env python3
"""
measure_latency.py — Đo độ trễ thực tế của luồng Hỏi-đáp tài liệu (Document RAG).

Gọi cùng endpoint SSE như run_eval.py nhưng đọc luồng TĂNG DẦN theo thời gian thực
để đo được ba mốc thời gian cho mỗi câu hỏi:
  - TTFT  (time-to-first-token): thời gian tới token đầu tiên hiển thị (cảm giác chờ thật của user)
  - E2E   (end-to-end): thời gian tới khi sinh xong toàn bộ câu trả lời
  - Mốc 'sources': thời gian tới khi nhận được danh sách nguồn truy xuất (xấp xỉ thời gian retrieval)

Dùng chung .env với run_eval.py:
  API_BASE_URL=https://localhost:7084
  API_TOKEN=<JWT>
  PROJECT_ID=<id>
  TOP_K=5
  MODE=high            # high | (mode thấp nếu backend hỗ trợ)

Cách chạy (trên VM sau khi Ollama + Qdrant + backend đã sẵn sàng):
  python measure_latency.py --dataset final_set --warmup 3
  python measure_latency.py --dataset final_set --mode low   --warmup 3   # so sánh mode
  python measure_latency.py --dataset dev_set   --repeats 1

Kết quả:
  results/latency_{dataset}_{mode}.csv      (mỗi câu một dòng)
  results/latency_{dataset}_{mode}.json     (thống kê tổng hợp)
"""

import argparse
import json
import os
import statistics as stats
import sys
import time
from pathlib import Path

import requests
import urllib3
from dotenv import load_dotenv

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

DATASETS_DIR = Path(__file__).parent / "datasets"
RESULTS_DIR = Path(__file__).parent / "results"
RESULTS_DIR.mkdir(exist_ok=True)

load_dotenv(Path(__file__).parent / ".env")

API_BASE = os.environ["API_BASE_URL"].rstrip("/")
TOKEN = os.environ["API_TOKEN"]
PROJECT_ID = int(os.environ["PROJECT_ID"])
TOP_K = int(os.environ.get("TOP_K", "5"))
DELAY_BETWEEN = float(os.environ.get("DELAY_BETWEEN", "2"))

HEADERS = {
    "Authorization": f"Bearer {TOKEN}",
    "Content-Type": "application/json",
    "Accept": "text/event-stream",
}
STREAM_URL = f"{API_BASE}/api/rag/project/{PROJECT_ID}/ask/stream"


def measure_one(question: str, mode: str, timeout: int = 180) -> dict:
    """Gửi 1 câu hỏi, đọc SSE tăng dần, trả về các mốc thời gian (giây)."""
    payload = {"question": question, "topK": TOP_K, "mode": mode, "skipClarify": True}
    rec = {
        "ttft": None, "t_sources": None, "e2e": None,
        "answer_len": 0, "n_sources": 0, "n_chunks": 0,
        "first_event": None, "stage_times": {}, "error": None,
    }
    t0 = time.perf_counter()
    try:
        resp = requests.post(
            STREAM_URL, headers=HEADERS, json=payload,
            timeout=timeout, verify=False, stream=True,
        )
        if resp.status_code == 401:
            print("\n[FATAL] 401 Unauthorized — kiểm tra API_TOKEN trong .env", file=sys.stderr)
            sys.exit(1)
        if resp.status_code != 200:
            rec["error"] = f"http_{resp.status_code}"
            return rec

        event_type = None
        answer = []
        buf = ""
        # Đọc tăng dần theo từng dòng để bắt mốc thời gian chính xác
        for raw_line in resp.iter_lines(decode_unicode=True):
            now = time.perf_counter() - t0
            if raw_line is None:
                continue
            line = raw_line.rstrip("\r")
            if line == "":
                event_type = None  # kết thúc một block SSE
                continue
            if line.startswith("event:"):
                event_type = line[len("event:"):].strip()
                if rec["first_event"] is None:
                    rec["first_event"] = event_type
                # mốc thời gian xuất hiện lần đầu của mỗi loại event
                rec["stage_times"].setdefault(event_type, round(now, 4))
            elif line.startswith("data:"):
                data = line[len("data:"):].strip()
                if event_type == "chunk":
                    if rec["ttft"] is None:
                        rec["ttft"] = round(now, 4)
                    rec["n_chunks"] += 1
                    answer.append(data)
                elif event_type == "sources":
                    if rec["t_sources"] is None:
                        rec["t_sources"] = round(now, 4)
                    try:
                        rec["n_sources"] = len(json.loads(data).get("sources", []))
                    except Exception:
                        pass
                elif event_type == "done":
                    try:
                        p = json.loads(data)
                        if p.get("answer"):
                            answer = [p["answer"]]
                    except Exception:
                        pass
                elif event_type == "error":
                    rec["error"] = data[:120]
        rec["e2e"] = round(time.perf_counter() - t0, 4)
        rec["answer_len"] = sum(len(a) for a in answer)
    except requests.exceptions.Timeout:
        rec["error"] = "timeout"
        rec["e2e"] = round(time.perf_counter() - t0, 4)
    except Exception as e:
        rec["error"] = str(e)[:120]
        rec["e2e"] = round(time.perf_counter() - t0, 4)
    return rec


def pctl(values, q):
    if not values:
        return None
    s = sorted(values)
    k = (len(s) - 1) * q
    f = int(k)
    c = min(f + 1, len(s) - 1)
    return round(s[f] + (s[c] - s[f]) * (k - f), 3)


def summarize(label, values):
    vals = [v for v in values if v is not None]
    if not vals:
        return {"n": 0}
    return {
        "n": len(vals),
        "mean": round(stats.mean(vals), 3),
        "median": round(stats.median(vals), 3),
        "p90": pctl(vals, 0.90),
        "p95": pctl(vals, 0.95),
        "min": round(min(vals), 3),
        "max": round(max(vals), 3),
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dataset", default="final_set",
                    choices=["test_set", "dev_set", "holdout_set", "holdout_v2", "final_set"])
    ap.add_argument("--mode", default=os.environ.get("MODE", "high"),
                    help="Chế độ pipeline gửi cho backend (vd: high, low)")
    ap.add_argument("--warmup", type=int, default=3,
                    help="Số câu đầu bỏ qua để làm nóng model/cache")
    ap.add_argument("--repeats", type=int, default=1,
                    help="Số lần lặp toàn bộ dataset (tăng để median ổn định hơn)")
    ap.add_argument("--limit", type=int, default=0,
                    help="Chỉ chạy N câu đầu (0 = toàn bộ)")
    args = ap.parse_args()

    path = DATASETS_DIR / f"{args.dataset}.jsonl"
    cases = [json.loads(l) for l in path.read_text(encoding="utf-8").splitlines() if l.strip()]
    # Bỏ câu out-of-scope khỏi đo latency sinh câu trả lời (chúng bị từ chối sớm, không phản ánh full pipeline)
    answerable = [c for c in cases if not c.get("must_refuse", False)]
    if args.limit:
        answerable = answerable[: args.limit]

    print(f"[latency] dataset={args.dataset} mode={args.mode} questions={len(answerable)} "
          f"warmup={args.warmup} repeats={args.repeats} project_id={PROJECT_ID}")
    print(f"[latency] endpoint={STREAM_URL}")

    rows = []
    seq = 0
    total_runs = len(answerable) * args.repeats
    for rep in range(args.repeats):
        for c in answerable:
            seq += 1
            m = measure_one(c["question"], args.mode)
            is_warm = (rep == 0 and seq <= args.warmup)
            rows.append({
                "seq": seq, "rep": rep + 1, "id": c["id"], "category": c.get("category", ""),
                "warmup": is_warm,
                "ttft_s": m["ttft"], "t_sources_s": m["t_sources"], "e2e_s": m["e2e"],
                "answer_len": m["answer_len"], "n_sources": m["n_sources"],
                "n_chunks": m["n_chunks"], "first_event": m["first_event"],
                "stage_times": m["stage_times"], "error": m["error"],
            })
            tag = " [warmup]" if is_warm else ""
            err = f" ERROR={m['error']}" if m["error"] else ""
            print(f"  [{seq}/{total_runs}] {c['id']:<14} ttft={m['ttft']}s e2e={m['e2e']}s{tag}{err}")
            time.sleep(DELAY_BETWEEN)

    # CSV
    csv_path = RESULTS_DIR / f"latency_{args.dataset}_{args.mode}.csv"
    import csv as _csv
    with open(csv_path, "w", newline="", encoding="utf-8") as f:
        w = _csv.writer(f)
        w.writerow(["seq", "rep", "id", "category", "warmup",
                    "ttft_s", "t_sources_s", "e2e_s", "answer_len",
                    "n_sources", "n_chunks", "first_event", "error"])
        for r in rows:
            w.writerow([r["seq"], r["rep"], r["id"], r["category"], r["warmup"],
                        r["ttft_s"], r["t_sources_s"], r["e2e_s"], r["answer_len"],
                        r["n_sources"], r["n_chunks"], r["first_event"], r["error"]])

    # Thống kê (loại warmup + câu lỗi)
    valid = [r for r in rows if not r["warmup"] and not r["error"]]
    summary = {
        "dataset": args.dataset, "mode": args.mode,
        "n_total": len(rows), "n_warmup": sum(1 for r in rows if r["warmup"]),
        "n_error": sum(1 for r in rows if r["error"]), "n_valid": len(valid),
        "gpu": os.environ.get("GPU_LABEL", "RTX 3090 Ti"),
        "ttft_s": summarize("ttft", [r["ttft_s"] for r in valid]),
        "t_sources_s": summarize("sources", [r["t_sources_s"] for r in valid]),
        "e2e_s": summarize("e2e", [r["e2e_s"] for r in valid]),
        "by_category": {},
    }
    cats = sorted(set(r["category"] for r in valid))
    for cat in cats:
        cv = [r for r in valid if r["category"] == cat]
        summary["by_category"][cat] = {
            "n": len(cv),
            "ttft_median": summarize("", [r["ttft_s"] for r in cv]).get("median"),
            "e2e_median": summarize("", [r["e2e_s"] for r in cv]).get("median"),
        }

    json_path = RESULTS_DIR / f"latency_{args.dataset}_{args.mode}.json"
    json_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")

    print("\n=== KẾT QUẢ ĐỘ TRỄ ({} câu hợp lệ, đã loại {} warmup + {} lỗi) ===".format(
        summary["n_valid"], summary["n_warmup"], summary["n_error"]))
    for key, name in [("ttft_s", "TTFT (tới token đầu)"),
                      ("t_sources_s", "Tới nguồn truy xuất"),
                      ("e2e_s", "End-to-end (sinh xong)")]:
        s = summary[key]
        if s.get("n"):
            print(f"  {name:<26} median={s['median']}s  mean={s['mean']}s  "
                  f"p90={s['p90']}s  p95={s['p95']}s  (min {s['min']} / max {s['max']})")
    print(f"\n  CSV : {csv_path}")
    print(f"  JSON: {json_path}")


if __name__ == "__main__":
    main()
