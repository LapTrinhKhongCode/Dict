# RAG Parameter Tuning Guide

## Nguyên tắc

**Scorer (`score_eval.py`) là thước đo cố định — không sửa để boost score.**  
Chỉ tune các tham số hệ thống, rồi re-run `run_eval.py` + `score_eval.py` để đo tác động thực.

---

## Baseline (2026-06-24)

| Metric | Value |
|--------|-------|
| Pass rate | 16/24 (66.7%) |
| Avg point recall | 73.2% |
| Refuse accuracy | 83.3% |
| TOP_K | 5, MODE=high |

---

## Tham số cần tune (ưu tiên theo tác động)

### 1. TOP_K (`.env`)
- **Tác động:** Số sources gửi vào LLM context
- **Failure pattern liên quan:** `low_coverage` (q001, q106)
- **Range test:** 5, 7, 8, 10
- **Tradeoff:** Tăng > 10 → LLM bị "lost in the middle", recall giảm
- **Gợi ý nguyên tắc:**
  - Với ~500 trang / 8 file: TOP_K = 7–8
  - Formula: `TOP_K ≈ max_expected_points_per_question × 1.5`, capped 10

### 2. OutOfScopeScoreThreshold (`DocumentRagService.cs:45`)
- **Hiện tại:** 0.50f
- **Tác động:** Vector score thấp hơn ngưỡng → system từ chối trả lời (error event)
- **Failure pattern:** `refusal_detected` với coverage=0 (q049, q111) — retrieval không tìm được chunk đúng
- **Range test:** 0.42, 0.45, 0.48, 0.50
- **Lưu ý:** Giảm quá thấp → system trả lời lung tung với câu hỏi out-of-scope

### 3. ClarifyScoreThreshold (`DocumentRagService.cs:46`)
- **Hiện tại:** 0.57f
- **Tác động:** Score trong [OutOfScope, Clarify] → emit `clarify` event thay vì answer
- **Failure pattern:** Câu hỏi có context nhưng score thấp → bị chuyển sang clarify flow
- **Range test:** 0.50, 0.53, 0.55, 0.57

### 4. Anti-refusal instruction trong prompt (`DocumentRagService.cs:3345–3349`)
- **Tác động:** Giảm `refusal_detected` với coverage > 0 (model có data nhưng vẫn hedge)
- **Failure pattern:** q021, q054 — point_recall cao nhưng model thêm disclaimers
- **Thử:** Thêm dòng "If context contains partial information, answer based on what you have"

### 5. RetrievePerQuery / CandidatePoolLimit
- **Hiện tại:** RetrievePerQuery=15, CandidatePool=50
- **Tác động:** Ảnh hưởng recall ở layer retrieval, trước rerank
- **Test sau khi đã ổn định các param trên**

---

## Quy trình tuning đúng

```
1. Thay đổi 1 tham số
2. Rebuild backend
3. python run_eval.py --dataset dev_set   ← dùng dev_set khi tuning
4. python score_eval.py --dataset dev_set
5. Ghi kết quả vào bảng dưới
6. Sau khi chọn được config tốt nhất → chạy test_set 1 lần duy nhất
```

**Dùng `dev_set` khi tune, `test_set` chỉ là final evaluation — tránh overfit vào test_set.**

---

## Experiment Log

| Date | TOP_K | OutOfScope | Clarify | Prompt fix | dev_set pass | Notes |
|------|-------|-----------|---------|------------|--------------|-------|
| baseline | 5 | 0.50 | 0.57 | no | — | test_set: 16/24 (66.7%) |
| 2026-06-24 | 8 | 0.50 | 0.57 | +anti-refuse | — | prompt fix in place, not yet re-run |
