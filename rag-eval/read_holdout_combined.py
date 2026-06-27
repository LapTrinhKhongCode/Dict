# -*- coding: utf-8 -*-
import json

all_h = []
for fname in ["results/score_holdout_set.json", "results/score_holdout_v2.json"]:
    with open(fname) as fp:
        d = json.load(fp)
    all_h.extend(d["details"])

total = len(all_h)
passed = sum(1 for d in all_h if d.get("pass"))
recalls = [d.get("point_recall", 0) for d in all_h if not d.get("must_refuse")]
avg_r = sum(recalls) / len(recalls) if recalls else 0
oos = [d for d in all_h if d.get("must_refuse")]
oos_pass = sum(1 for d in oos if d.get("pass"))

cats = {}
for d in all_h:
    c = d.get("category", "?")
    cats.setdefault(c, {"pass": 0, "total": 0})
    cats[c]["total"] += 1
    if d.get("pass"):
        cats[c]["pass"] += 1

print(f"Combined Holdout ({total} cau):")
print(f"  Pass: {passed}/{total} = {passed/total*100:.1f}%")
print(f"  Avg recall (in-domain): {avg_r*100:.1f}%")
print(f"  Refuse acc: {oos_pass}/{len(oos)} = {oos_pass/len(oos)*100:.1f}%")
for c, v in sorted(cats.items()):
    pct = v["pass"] / v["total"] * 100
    print(f"  {c}: {v['pass']}/{v['total']} = {pct:.0f}%")
