#!/usr/bin/env python3
"""Diagnose retrieval vs generation failures using page_hit/file_hit."""
import json
from pathlib import Path

raw = [json.loads(l) for l in Path("results/raw_test_set.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]
ds  = {c["id"]: c for c in [json.loads(l) for l in Path("datasets/test_set.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]}

failing = ["q049","q111","q021","q001","q106","q054","q020","q105"]
print("=== PAGE HIT / FILE HIT for failing cases ===\n")
for r in raw:
    if r["id"] not in failing:
        continue
    expected_pages = ds[r["id"]].get("source_pages", [])
    expected_file  = ds[r["id"]].get("source_file", "")
    sources = r.get("sources") or []
    retrieved_pages = [s.get("pageNumber") for s in sources]
    retrieved_files = list({s.get("documentName", "") for s in sources})
    page_hit = any(p in retrieved_pages for p in expected_pages) if expected_pages else None
    file_hit = any((expected_file or "") in (f or "") for f in retrieved_files) if expected_file else None

    fail_type = "RETRIEVAL" if not page_hit else "GENERATION"
    print(f"[{r['id']}] -> root cause = {fail_type}")
    print(f"  expected_pages={expected_pages}  retrieved={retrieved_pages}")
    print(f"  expected_file={expected_file!r}")
    print(f"  file_hit={file_hit}  page_hit={page_hit}")
    print()
