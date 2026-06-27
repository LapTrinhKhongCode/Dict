# -*- coding: utf-8 -*-
import json

datasets = {
    "Dev Set":     ("results/score_dev_set.json",),
    "Holdout Set": ("results/score_holdout_set.json",),
    "Final Set":   ("results/score_final_set.json",),
}

for name, (score_f,) in datasets.items():
    with open(score_f) as f:
        s = json.load(f)
    details = s["details"]
    total = len(details)
    passed = sum(1 for d in details if d.get("pass"))
    recalls = [d.get("point_recall", 0) for d in details if not d.get("must_refuse")]
    avg_recall = sum(recalls) / len(recalls) if recalls else 0
    oos = [d for d in details if d.get("must_refuse")]
    oos_pass = sum(1 for d in oos if d.get("pass"))
    cats = {}
    for d in details:
        c = d.get("category", "?")
        cats.setdefault(c, {"pass": 0, "total": 0})
        cats[c]["total"] += 1
        if d.get("pass"):
            cats[c]["pass"] += 1
    fails = [d for d in details if not d.get("pass")]
    partial = sum(1 for d in details if d.get("pass") and d.get("point_recall", 0) < 1.0)

    print(f"\n=== {name} ({total} cau) ===")
    print(f"  Pass: {passed}/{total} = {passed/total*100:.1f}%")
    print(f"  Avg recall (in-domain): {avg_recall*100:.1f}%")
    if oos:
        print(f"  Refuse acc: {oos_pass}/{len(oos)} = {oos_pass/len(oos)*100:.1f}%")
    for c, v in sorted(cats.items()):
        pct = v["pass"] / v["total"] * 100
        print(f"    {c}: {v['pass']}/{v['total']} = {pct:.0f}%")
    print(f"  Fail: {len(fails)}, Pass-partial (recall<1): {partial}")
