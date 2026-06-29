#!/usr/bin/env python3
"""
plot_eval.py — Generate evaluation charts from raw_*.jsonl and score_*.json.

Usage:
  python plot_eval.py --dataset run_1 run_2
"""

import argparse
import inspect
import json
from collections import defaultdict
from pathlib import Path

import matplotlib.pyplot as plt
plt.rcParams['font.family'] = 'sans-serif'
plt.rcParams['font.sans-serif'] = ['Meiryo', 'Yu Gothic', 'MS Gothic', 'Segoe UI', 'Arial']
RESULTS_DIR = Path(__file__).parent / "results"

def load_data(dataset: str):
    raw_path = RESULTS_DIR / f"raw_{dataset}.jsonl"
    score_path = RESULTS_DIR / f"score_{dataset}.json"
    if not raw_path.exists():
        raise FileNotFoundError(f"Missing raw file: {raw_path}")
    if not score_path.exists():
        raise FileNotFoundError(f"Missing score file: {score_path}")

    raw_records = {}
    for line in raw_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line:
            continue
        rec = json.loads(line)
        raw_records[rec["id"]] = rec

    score = json.loads(score_path.read_text(encoding="utf-8"))
    details = score["details"]
    merged = []
    for row in details:
        rec = dict(raw_records.get(row["id"], {}))
        rec.update(row)
        merged.append(rec)
    return score["metrics"], merged


def load_and_merge_multiple_datasets(datasets: list):
    """Hàm mới: Đọc và gộp dữ liệu từ nhiều lần chạy (nhiều dataset)"""
    all_merged_records = []
    
    # Dùng defaultdict để cộng dồn pass/total cho từng category
    combined_metrics = {
        "by_category": defaultdict(lambda: {"pass": 0, "total": 0, "rate": 0.0})
    }

    for ds in datasets:
        metrics, records = load_data(ds)
        all_merged_records.extend(records)
        
        # Gộp số liệu category
        if "by_category" in metrics:
            for cat, stats in metrics["by_category"].items():
                combined_metrics["by_category"][cat]["pass"] += stats.get("pass", 0)
                combined_metrics["by_category"][cat]["total"] += stats.get("total", 0)

    # Tính toán lại rate (tỷ lệ % pass) cho các category đã gộp
    for cat, stats in combined_metrics["by_category"].items():
        if stats["total"] > 0:
            stats["rate"] = stats["pass"] / stats["total"]

    return combined_metrics, all_merged_records


def save_fig(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    plt.tight_layout()
    plt.savefig(path, dpi=160, bbox_inches="tight")
    plt.close()


def plot_boxplot_compat(data: list[list[float]], labels: list[str], showfliers: bool = False):
    boxplot_params = inspect.signature(plt.boxplot).parameters
    if "tick_labels" in boxplot_params:
        plt.boxplot(data, tick_labels=labels, showfliers=showfliers)
        return
    plt.boxplot(data, labels=labels, showfliers=showfliers)


def plot_pass_by_category(dataset_name: str, metrics: dict, out_dir: Path):
    cats = list(metrics["by_category"].keys())
    rates = [metrics["by_category"][c]["rate"] * 100 for c in cats]
    labels = [f"{metrics['by_category'][c]['pass']}/{metrics['by_category'][c]['total']}" for c in cats]

    plt.figure(figsize=(9, 5))
    # Mảng này có 5 màu, đủ cho 5 category của bạn
    bars = plt.bar(cats, rates, color=["#4C78A8", "#F58518", "#54A24B", "#B279A2", "#E45756"][: len(cats)])
    plt.ylim(0, 100)
    plt.ylabel("Accuracy (%)")
    plt.title(f"Accuracy by category — {dataset_name}")
    for bar, rate, label in zip(bars, rates, labels):
        plt.text(bar.get_x() + bar.get_width() / 2, rate + 1, f"{rate:.1f}%\n{label}", ha="center", va="bottom", fontsize=9)
    save_fig(out_dir / f"{dataset_name}_accuracy_by_category.png")


def plot_latency_histogram(dataset_name: str, records: list[dict], out_dir: Path):
    latencies = [r["elapsed_ms"] for r in records if r.get("elapsed_ms") is not None]
    if not latencies:
        return

    plt.figure(figsize=(9, 5))
    plt.hist(latencies, bins=20, color="#4C78A8", edgecolor="white")
    plt.xlabel("Latency (ms)")
    plt.ylabel("Question count")
    plt.title(f"Latency distribution — {dataset_name}")
    save_fig(out_dir / f"{dataset_name}_latency_hist.png")


def plot_latency_by_category(dataset_name: str, records: list[dict], out_dir: Path):
    by_cat = defaultdict(list)
    for r in records:
        if r.get("elapsed_ms") is not None:
            by_cat[r["category"]].append(r["elapsed_ms"])
    if not by_cat:
        return

    cats = list(by_cat.keys())
    data = [by_cat[c] for c in cats]
    plt.figure(figsize=(10, 5))
    plot_boxplot_compat(data, cats, showfliers=False)
    plt.ylabel("Latency (ms)")
    plt.title(f"Latency by category — {dataset_name}")
    save_fig(out_dir / f"{dataset_name}_latency_by_category.png")


def plot_recall_histogram(dataset_name: str, records: list[dict], out_dir: Path):
    recalls = [r["point_recall"] * 100 for r in records if not r.get("must_refuse") and not r.get("has_error")]
    if not recalls:
        return

    plt.figure(figsize=(9, 5))
    plt.hist(recalls, bins=[0, 20, 40, 60, 80, 100], color="#54A24B", edgecolor="white")
    plt.xlabel("Point Recall (%)")
    plt.ylabel("Question count")
    plt.title(f"Point Recall distribution — {dataset_name}")
    save_fig(out_dir / f"{dataset_name}_point_recall_hist.png")


def plot_accuracy_by_source(dataset_name: str, records: list[dict], out_dir: Path):
    by_src = defaultdict(lambda: {"pass": 0, "total": 0})
    for r in records:
        src = r.get("source_file") or "out-of-scope"
        by_src[src]["total"] += 1
        if r.get("pass"):
            by_src[src]["pass"] += 1

    items = sorted(by_src.items(), key=lambda kv: kv[0].lower())
    labels = [k if len(k) <= 24 else k[:21] + "..." for k, _ in items]
    rates = [v["pass"] / v["total"] * 100 for _, v in items]

    plt.figure(figsize=(12, 6))
    bars = plt.barh(labels, rates, color="#F58518")
    plt.xlim(0, 100)
    plt.xlabel("Accuracy (%)")
    plt.title(f"Accuracy by source file — {dataset_name}")
    for bar, rate in zip(bars, rates):
        plt.text(rate + 1, bar.get_y() + bar.get_height() / 2, f"{rate:.1f}%", va="center", fontsize=8)
    save_fig(out_dir / f"{dataset_name}_accuracy_by_source.png")


def main():
    parser = argparse.ArgumentParser()
    # Sửa nargs='+' để cho phép nhập nhiều dataset cùng lúc cách nhau bởi dấu cách
    parser.add_argument("--dataset", nargs='+', required=True, help="Danh sách các dataset cần gộp để vẽ")
    args = parser.parse_args()

    # Gọi hàm gộp
    metrics, records = load_and_merge_multiple_datasets(args.dataset)
    
    # Tạo tên chung cho thư mục output nếu có nhiều file
    combined_name = "_and_".join(args.dataset)
    if len(combined_name) > 30:
        combined_name = "merged_datasets" # Tránh tên file quá dài
        
    out_dir = RESULTS_DIR / f"plots_{combined_name}"

    plot_pass_by_category(combined_name, metrics, out_dir)
    plot_latency_histogram(combined_name, records, out_dir)
    plot_latency_by_category(combined_name, records, out_dir)
    plot_recall_histogram(combined_name, records, out_dir)
    plot_accuracy_by_source(combined_name, records, out_dir)

    print(f"[plot_eval] Charts saved to {out_dir}")


if __name__ == "__main__":
    main()