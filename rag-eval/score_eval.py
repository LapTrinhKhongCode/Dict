#!/usr/bin/env python3
"""
score_eval.py — Scores raw results produced by run_eval.py using keyword
matching against expected_points and must_refuse logic.

Usage:
  python score_eval.py --dataset test_set          (default)
  python score_eval.py --dataset dev_set
  python score_eval.py --dataset holdout_set --verbose

Outputs:
  results/score_{dataset}.json    — full breakdown per question + aggregated metrics
  Console summary table
"""

import argparse
import json
import re
import unicodedata
from pathlib import Path

RESULTS_DIR = Path(__file__).parent / "results"

# ── helpers ────────────────────────────────────────────────────────────────

def normalize(text: str) -> str:
    """Lower, strip diacritics via NFKC, collapse spaces."""
    text = unicodedata.normalize("NFKC", text).lower()
    text = re.sub(r"\s+", " ", text).strip()
    return text


def contains_any(text: str, patterns: list[str]) -> bool:
    t = normalize(text)
    return any(normalize(p) in t for p in patterns)


def point_hit(answer: str, point: str, token_threshold: float = 0.6) -> bool:
    """Exact phrase match OR token-recall fallback for long points."""
    a = normalize(answer)
    p = normalize(point)
    if p in a:
        return True
    # Soft match: check token recall for points > 20 chars
    if len(p) <= 20:
        return False
    tokens = [t for t in re.split(r"[\W_]+", p) if len(t) >= 3]
    if not tokens:
        return False
    hits = sum(1 for t in tokens if t in a)
    return (hits / len(tokens)) >= token_threshold


def number_hit(answer: str, num: str) -> bool:
    """Numeric value found in answer (loose — checks string presence)."""
    return normalize(str(num)) in normalize(answer)


REFUSE_INDICATORS = [
    "không có thông tin", "không đề cập", "không tìm thấy thông tin",
    "nằm ngoài phạm vi", "từ chối", "không thể trả lời",
    "không liên quan", "không được đề cập", "không chứa thông tin",
    "chưa có tài liệu", "ngoài nội dung",
]

def is_refusal(answer: str) -> bool:
    return contains_any(answer, REFUSE_INDICATORS) or (len(answer.strip()) < 15 and not answer.strip())


# ── per-question scoring ────────────────────────────────────────────────────

def score_case(rec: dict) -> dict:
    answer        = rec.get("answer", "") or ""
    must_refuse   = rec.get("must_refuse", False)
    exp_points    = rec.get("expected_points", []) or []
    exp_numbers   = rec.get("expected_numbers", []) or []
    has_error     = bool(rec.get("error"))

    result = {
        "id": rec["id"],
        "category": rec["category"],
        "scope": rec["scope"],
        "must_refuse": must_refuse,
        "has_error": has_error,
    }

    if has_error:
        result.update({"pass": False, "reason": f"api_error: {rec['error']}", "point_recall": 0.0})
        return result

    # ── out-of-scope: should refuse ──────────────────────────────────────
    if must_refuse:
        refused = is_refusal(answer)
        result["pass"] = refused
        result["reason"] = "correct_refusal" if refused else "should_have_refused"
        result["point_recall"] = 1.0 if refused else 0.0
        return result

    # ── in-domain: check expected_points ─────────────────────────────────
    hits = [point_hit(answer, p) for p in exp_points]
    num_hits = [number_hit(answer, n) for n in exp_numbers]

    point_recall = sum(hits) / len(hits) if hits else 1.0
    num_recall   = sum(num_hits) / len(num_hits) if num_hits else 1.0

    refused = is_refusal(answer)
    # Pass = majority of expected points covered AND not refusing
    passed = (point_recall >= 0.5) and not refused

    missed = [p for p, h in zip(exp_points, hits) if not h]
    if refused and not passed:
        reason = f"refusal_detected(coverage={sum(hits)}/{len(exp_points)})"
    else:
        reason = "ok" if passed else f"low_coverage({sum(hits)}/{len(exp_points)})"
    result.update({
        "pass": passed,
        "point_recall": round(point_recall, 3),
        "num_recall": round(num_recall, 3),
        "points_hit": sum(hits),
        "points_total": len(exp_points),
        "missed_points": missed[:3],   # first 3 only to keep output small
        "reason": reason,
    })
    return result


# ── aggregation ─────────────────────────────────────────────────────────────

def aggregate(scored: list[dict]) -> dict:
    total      = len(scored)
    passed     = sum(1 for r in scored if r["pass"])
    errored    = sum(1 for r in scored if r.get("has_error"))
    pass_rate  = passed / total if total else 0

    by_cat: dict[str, dict] = {}
    for r in scored:
        cat = r["category"]
        if cat not in by_cat:
            by_cat[cat] = {"total": 0, "pass": 0}
        by_cat[cat]["total"] += 1
        if r["pass"]:
            by_cat[cat]["pass"] += 1

    cat_rates = {
        cat: {"pass": v["pass"], "total": v["total"],
              "rate": round(v["pass"] / v["total"], 3)}
        for cat, v in sorted(by_cat.items())
    }

    refuse_cases = [r for r in scored if r["must_refuse"]]
    in_domain    = [r for r in scored if not r["must_refuse"] and not r.get("has_error")]
    avg_recall   = (sum(r.get("point_recall", 0) for r in in_domain) / len(in_domain)
                    if in_domain else 0)

    return {
        "total": total,
        "pass": passed,
        "fail": total - passed,
        "errors": errored,
        "pass_rate": round(pass_rate, 3),
        "avg_point_recall": round(avg_recall, 3),
        "refuse_accuracy": round(
            sum(1 for r in refuse_cases if r["pass"]) / len(refuse_cases), 3
        ) if refuse_cases else None,
        "by_category": cat_rates,
    }


# ── main ────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dataset", default="test_set",
                        choices=["test_set", "dev_set", "holdout_set"])
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args()

    raw_path   = RESULTS_DIR / f"raw_{args.dataset}.jsonl"
    score_path = RESULTS_DIR / f"score_{args.dataset}.json"

    if not raw_path.exists():
        print(f"[score_eval] raw file not found: {raw_path}")
        print("  → Run:  python run_eval.py --dataset", args.dataset)
        return

    records = [json.loads(l) for l in raw_path.read_text(encoding="utf-8").splitlines() if l.strip()]
    scored  = [score_case(r) for r in records]
    metrics = aggregate(scored)

    output = {"metrics": metrics, "details": scored}
    score_path.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")

    # ── console summary ──────────────────────────────────────────────────
    m = metrics
    print(f"\n{'─'*52}")
    print(f"  Dataset  : {args.dataset}")
    print(f"  Pass     : {m['pass']}/{m['total']}  ({m['pass_rate']*100:.1f}%)")
    print(f"  Avg recall (in-domain) : {m['avg_point_recall']*100:.1f}%")
    if m["refuse_accuracy"] is not None:
        print(f"  Refuse accuracy        : {m['refuse_accuracy']*100:.1f}%")
    print(f"  API errors : {m['errors']}")
    print(f"{'─'*52}")
    print("  By category:")
    for cat, v in m["by_category"].items():
        bar = "█" * int(v["rate"] * 10) + "░" * (10 - int(v["rate"] * 10))
        print(f"    {cat:<14} {bar}  {v['pass']}/{v['total']} ({v['rate']*100:.0f}%)")
    print(f"{'─'*52}")
    print(f"  Full report → {score_path}\n")

    if args.verbose:
        fails = [r for r in scored if not r["pass"]]
        print(f"\n{'═'*52}")
        print(f"  FAILED ({len(fails)}):")
        for r in fails:
            print(f"  [{r['id']}] {r['category']} — {r['reason']}")
            if r.get("missed_points"):
                for mp in r["missed_points"]:
                    print(f"       ✗ {mp[:80]}")
        print(f"{'═'*52}\n")


if __name__ == "__main__":
    main()
