# -*- coding: utf-8 -*-
"""
Conceptual diagrams for thesis Section 3 (algorithm explanations).
These are illustrative — based on formulas, not measurement data.
  P: BM25 vs TF-IDF TF saturation curve
  Q: Optuna TPE flow (random → model → propose)
  R: HyDE concept flow
  S: RRF worked example (numeric table + score bar)
"""
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import numpy as np
import warnings
warnings.filterwarnings("ignore")

# ══════════════════════════════════════════════════════════════
# CHART P: BM25 vs TF-IDF — TF Saturation
# ══════════════════════════════════════════════════════════════
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 4.5))
fig.suptitle("Hình 3.X. Cơ chế tính điểm BM25 so với TF-IDF",
             fontsize=12, fontweight="bold")

# Left: TF saturation curve
tf = np.linspace(0, 30, 300)
k1, b = 1.34, 0.57       # Optuna best values
doc_len, avg_len = 150, 200  # typical chunk

# TF-IDF: linear in TF (simplified, IDF constant=1)
tfidf_score = tf * 1.0

# BM25 TF component: tf*(k1+1) / (tf + k1*(1-b+b*dl/avgdl))
norm = 1 - b + b * (doc_len / avg_len)
bm25_tf = (tf * (k1 + 1)) / (tf + k1 * norm)

ax1.plot(tf, tfidf_score, color="#E53935", linewidth=2.5, label="TF-IDF (tuyến tính)")
ax1.plot(tf, bm25_tf,     color="#1565C0", linewidth=2.5, label=f"BM25 (bão hòa, k1={k1})")
ax1.axhline(k1 + 1, color="#1565C0", linestyle=":", linewidth=1.5, alpha=0.6)
ax1.text(1, k1 + 1.1, f"Giới hạn trên ≈ k1+1 = {k1+1:.2f}", fontsize=8.5, color="#1565C0")

# Annotate spam point
ax1.annotate("Spam từ khóa 20 lần\n→ TF-IDF vẫn tăng mãi",
             xy=(20, 20), xytext=(15, 14),
             arrowprops=dict(arrowstyle="->", color="#E53935", lw=1.3),
             fontsize=8, color="#E53935")
ax1.annotate("BM25 bão hòa\n→ không spam được",
             xy=(20, bm25_tf[int(20/30*300)]), xytext=(14, 1.8),
             arrowprops=dict(arrowstyle="->", color="#1565C0", lw=1.3),
             fontsize=8, color="#1565C0")

ax1.set_xlabel("Tần suất từ khóa (TF)", fontsize=10)
ax1.set_ylabel("Điểm số TF component", fontsize=10)
ax1.set_title("Bão hòa tần suất (TF Saturation)", fontsize=10, pad=8)
ax1.legend(fontsize=9)
ax1.set_ylim(0, 25)
ax1.set_xlim(0, 30)
ax1.grid(alpha=0.3)
ax1.spines["top"].set_visible(False)
ax1.spines["right"].set_visible(False)

# Right: Length normalization effect
doc_lengths = np.linspace(50, 500, 200)
tf_fixed = 3
avg_len2 = 200

bm25_scores = []
for dl in doc_lengths:
    norm_dl = 1 - b + b * (dl / avg_len2)
    score = (tf_fixed * (k1 + 1)) / (tf_fixed + k1 * norm_dl)
    bm25_scores.append(score)

ax2.plot(doc_lengths, bm25_scores, color="#388E3C", linewidth=2.5)
ax2.axvline(avg_len2, color="#FB8C00", linestyle="--", linewidth=1.8)
ax2.text(avg_len2 + 5, min(bm25_scores) + 0.1, f"avg={avg_len2}", fontsize=8.5, color="#FB8C00")

ax2.annotate("Chunk ngắn: từ xuất hiện\n3 lần → điểm cao hơn",
             xy=(70, bm25_scores[int((70-50)/450*200)]),
             xytext=(130, bm25_scores[0] - 0.25),
             arrowprops=dict(arrowstyle="->", color="#388E3C", lw=1.3),
             fontsize=8, color="#1B5E20")
ax2.annotate("Chunk dài: loãng thông tin\n→ điểm thấp hơn",
             xy=(420, bm25_scores[int((420-50)/450*200)]),
             xytext=(300, bm25_scores[-1] + 0.2),
             arrowprops=dict(arrowstyle="->", color="#388E3C", lw=1.3),
             fontsize=8, color="#1B5E20")

ax2.set_xlabel("Độ dài chunk (số từ)", fontsize=10)
ax2.set_ylabel(f"BM25 score (TF={tf_fixed} lần, b={b})", fontsize=10)
ax2.set_title("Chuẩn hóa độ dài (Length Normalization)", fontsize=10, pad=8)
ax2.grid(alpha=0.3)
ax2.spines["top"].set_visible(False)
ax2.spines["right"].set_visible(False)

plt.tight_layout()
plt.savefig("concept_bm25.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: concept_bm25.png")


# ══════════════════════════════════════════════════════════════
# CHART Q: Optuna TPE — from random exploration to guided search
# ══════════════════════════════════════════════════════════════
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(13, 5))
fig.suptitle("Hình 3.X+1. Cơ chế tối ưu Bayesian của Optuna TPE",
             fontsize=12, fontweight="bold")

# Left: 1D illustration — TPE builds l(x) and g(x)
np.random.seed(42)
x_range = np.linspace(0, 1, 400)

# Simulate an objective (hidden, unknown to Optuna)
def true_obj(x): return np.sin(3*x) * 0.4 + 0.6 - (x - 0.6)**2 * 0.8

# Good trials (top 25%) and bad trials
all_x = np.random.uniform(0, 1, 50)
all_y = true_obj(all_x) + np.random.normal(0, 0.04, 50)
threshold = np.percentile(all_y, 75)
good_mask = all_y >= threshold
bad_mask  = ~good_mask

ax1.fill_between(x_range, 0, 1, alpha=0.04, color="gray")
ax1.scatter(all_x[bad_mask],  [0.08]*bad_mask.sum(),  c="#EF5350", s=50, zorder=4, label="Bad trials (l(x))")
ax1.scatter(all_x[good_mask], [0.08]*good_mask.sum(), c="#66BB6A", s=70, marker="*", zorder=5, label="Good trials (g(x))")

# Approximate KDE for l(x) and g(x)
from scipy.stats import gaussian_kde
if bad_mask.sum() > 2:
    kde_bad  = gaussian_kde(all_x[bad_mask],  bw_method=0.3)
    ax1.plot(x_range, kde_bad(x_range) / kde_bad(x_range).max() * 0.6 + 0.15,
             color="#EF5350", linewidth=2.0, linestyle="--", label="l(x) — phân phối bad")
if good_mask.sum() > 2:
    kde_good = gaussian_kde(all_x[good_mask], bw_method=0.4)
    ax1.plot(x_range, kde_good(x_range) / kde_good(x_range).max() * 0.6 + 0.15,
             color="#66BB6A", linewidth=2.0, label="g(x) — phân phối good")

# EI = g(x)/l(x) — propose from peak of this ratio
ei = kde_good(x_range) / (kde_bad(x_range) + 1e-8)
ei_norm = (ei - ei.min()) / (ei.max() - ei.min()) * 0.5 + 0.15
ax1.plot(x_range, ei_norm, color="#FB8C00", linewidth=2.5, label="g(x)/l(x) → propose đây")

best_x = x_range[np.argmax(ei)]
ax1.axvline(best_x, color="#FB8C00", linestyle=":", linewidth=2)
ax1.annotate(f"  Đề xuất\n  x={best_x:.2f}", xy=(best_x, 0.72), fontsize=9,
             color="#FB8C00", fontweight="bold")

ax1.set_xlim(0, 1)
ax1.set_ylim(0, 1.1)
ax1.set_xlabel("Không gian tham số (ví dụ: OutOfScopeThreshold)", fontsize=10)
ax1.set_title("TPE: xây dựng mô hình l(x), g(x)\nvà đề xuất từ tỉ lệ g/l", fontsize=10, pad=8)
ax1.legend(fontsize=8, loc="upper left")
ax1.grid(alpha=0.2)
ax1.spines["top"].set_visible(False)
ax1.spines["right"].set_visible(False)

# Right: Trial progression diagram (boxes)
ax2.set_xlim(0, 10)
ax2.set_ylim(0, 6)
ax2.axis("off")

def tbox(ax, x, y, w, h, color, text, fs=9):
    rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.15",
                          facecolor=color, edgecolor="white", linewidth=1.5)
    ax.add_patch(rect)
    ax.text(x+w/2, y+h/2, text, ha="center", va="center",
            fontsize=fs, color="white", fontweight="bold")

def tarrow(ax, x1, y, x2):
    ax.annotate("", xy=(x2, y), xytext=(x1, y),
                arrowprops=dict(arrowstyle="-|>", color="#555", lw=1.5))

# Phase 1: Random
tbox(ax2, 0.3, 4.2, 2.5, 1.2, "#1565C0", "Giai đoạn 1\nRandom Sampling\n(10 trials đầu)", fs=8.5)
tarrow(ax2, 2.8, 4.8, 3.3)
tbox(ax2, 3.3, 4.2, 2.5, 1.2, "#4A148C", "Xây dựng\nl(x) và g(x)\ntừ kết quả", fs=8.5)
tarrow(ax2, 5.8, 4.8, 6.3)
tbox(ax2, 6.3, 4.2, 3.2, 1.2, "#1B5E20", "Giai đoạn 2\nTPE Propose\n(trial 11–50)", fs=8.5)

# Phase 2 loop
ax2.annotate("", xy=(7.9, 4.2), xytext=(7.9, 3.0),
             arrowprops=dict(arrowstyle="-|>", color="#555", lw=1.3))
tbox(ax2, 6.3, 1.8, 3.2, 1.1, "#E65100", "Chạy eval trên\ndev_set (24 câu)\n→ objective score", fs=8)
ax2.annotate("", xy=(3.3+2.5, 3.65), xytext=(6.3, 2.35),
             arrowprops=dict(arrowstyle="-|>", color="#555", lw=1.3))

# Objective formula
ax2.text(5.0, 1.3, "Objective = 0.4×recall + 0.3×page_hit + 0.2×refuse_acc", 
         ha="center", fontsize=8.5, color="#333",
         bbox=dict(boxstyle="round,pad=0.3", facecolor="#FFF9C4", alpha=0.9))

# Best result
tbox(ax2, 3.5, 0.2, 3.0, 0.9, "#BF360C", f"Best: Trial #22 — obj=0.7321", fs=9)

ax2.set_title("Luồng hoạt động Optuna TPE (50 trials, sweep2.db)", fontsize=10, pad=8)

plt.tight_layout()
plt.savefig("concept_optuna_tpe.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: concept_optuna_tpe.png")


# ══════════════════════════════════════════════════════════════
# CHART R: HyDE — Hypothetical Document Embeddings
# ══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(13, 5))
ax.set_xlim(0, 13)
ax.set_ylim(0, 5.5)
ax.axis("off")
fig.patch.set_facecolor("#F8F9FA")
ax.set_facecolor("#F8F9FA")

COLORS = {
    "query":  "#1565C0",
    "llm":    "#4A148C",
    "embed":  "#1B5E20",
    "qdrant": "#E65100",
    "doc":    "#0277BD",
    "bad":    "#B71C1C",
}

def rbox(ax, x, y, w, h, color, title, sub="", fs=9, sub_fs=8):
    rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                          facecolor=color, edgecolor="white", linewidth=1.8, alpha=0.92)
    ax.add_patch(rect)
    ax.text(x+w/2, y+h*(0.62 if sub else 0.5), title, ha="center", va="center",
            fontsize=fs, color="white", fontweight="bold")
    if sub:
        ax.text(x+w/2, y+h*0.28, sub, ha="center", va="center",
                fontsize=sub_fs, color="white", alpha=0.9)

def rarrow(ax, x1, y, x2, label="", color="#555"):
    ax.annotate("", xy=(x2, y), xytext=(x1, y),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=2.0))
    if label:
        ax.text((x1+x2)/2, y+0.18, label, ha="center", fontsize=8, color=color)

# ── Top row: Standard (naive) retrieval ──
ax.text(6.0, 5.15, "❌  Vấn đề: Dense Search trực tiếp từ câu hỏi ngắn", 
        ha="center", fontsize=10, color=COLORS["bad"], fontweight="bold")

rbox(ax, 0.2, 3.8, 2.3, 1.0, COLORS["query"], "User Query", '"MTBF là bao nhiêu?"', fs=9)
rarrow(ax, 2.5, 4.3, 3.0, "embed", COLORS["bad"])
rbox(ax, 3.0, 3.8, 2.0, 1.0, COLORS["embed"], "Query Vector", "384 chiều\n(nghi vấn)", fs=8.5)
rarrow(ax, 5.0, 4.3, 5.5, "search", COLORS["bad"])
rbox(ax, 5.5, 3.8, 2.5, 1.0, COLORS["qdrant"], "Qdrant Search", "tìm vector\ngần nhất", fs=8.5)
rarrow(ax, 8.0, 4.3, 8.5, "", COLORS["bad"])
rbox(ax, 8.5, 3.8, 2.3, 1.0, COLORS["bad"], "❌ Kết quả kém", "vector câu hỏi\n≠ vector tài liệu", fs=8)

ax.text(10.9, 4.3, "Semantic gap:\n'câu hỏi ngắn'\nvs 'đoạn dài'", 
        ha="left", fontsize=7.5, color=COLORS["bad"])

# Divider
ax.axhline(3.6, color="#BDBDBD", linewidth=1.0, linestyle="--", xmin=0.01, xmax=0.99)

# ── Bottom row: HyDE ──
ax.text(6.0, 3.35, "✅  Giải pháp: HyDE — Dùng câu trả lời giả để tìm kiếm", 
        ha="center", fontsize=10, color="#1B5E20", fontweight="bold")

rbox(ax, 0.2, 0.8, 2.3, 1.0, COLORS["query"], "User Query", '"MTBF là bao nhiêu?"', fs=9)
rarrow(ax, 2.5, 1.3, 3.0, "LLM", "#4A148C")
rbox(ax, 3.0, 0.8, 2.5, 2.2, COLORS["llm"],
     "LLM sinh\ncâu trả lời\ngiả định",
     '"Giá trị MTBF\ncủa thiết bị X\nlà 15.000 giờ..."', fs=8.5, sub_fs=7.5)
rarrow(ax, 5.5, 1.9, 6.0, "embed", COLORS["embed"])
rbox(ax, 6.0, 1.35, 2.0, 1.1, COLORS["embed"], "HyDE Vector", "384 chiều\n(khẳng định)", fs=8.5)

# Show vector space comparison
ax.annotate("", xy=(8.0, 2.6), xytext=(7.0, 2.6),
            arrowprops=dict(arrowstyle="-|>", color=COLORS["embed"], lw=1.8))
ax.text(8.1, 2.7, "gần hơn\nvới tài liệu", fontsize=7.5, color="#1B5E20")

rarrow(ax, 8.0, 1.9, 8.5, "search", COLORS["embed"])
rbox(ax, 8.5, 1.35, 2.3, 1.1, COLORS["qdrant"], "Qdrant Search", "vector gần\nvới tài liệu thật", fs=8.5)
rarrow(ax, 10.8, 1.9, 11.2, "", COLORS["doc"])
rbox(ax, 11.2, 1.35, 1.6, 1.1, COLORS["doc"], "✅ Đúng\ntài liệu", "", fs=9)

# Note about HyDE text potentially wrong
ax.text(4.25, 0.55, "⚠ Nội dung câu trả lời giả có thể sai — chỉ cần văn phong và từ vựng chuyên ngành đúng",
        ha="center", fontsize=8, color="#555", style="italic")

ax.set_title("Hình 3.X+2. HyDE (Hypothetical Document Embeddings) — thu hẹp khoảng cách ngữ nghĩa",
             fontsize=11, fontweight="bold", pad=10)

plt.tight_layout()
plt.savefig("concept_hyde.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: concept_hyde.png")


# ══════════════════════════════════════════════════════════════
# CHART S: RRF Worked Example
# ══════════════════════════════════════════════════════════════
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(13, 5))
fig.suptitle("Hình 3.X+3. Reciprocal Rank Fusion (RRF) — Ví dụ minh họa (k=44)",
             fontsize=12, fontweight="bold")

# Left: table showing the ranking and score calculation
ax1.axis("off")

docs = ["Chunk A", "Chunk B", "Chunk C", "Chunk D", "Chunk E"]
dense_ranks  = [1, 3, 2, 5, 4]
bm25_ranks   = [4, 2, 3, 1, 5]
k = 44

def rrf(r): return round(1.0 / (k + r), 5)

dense_scores = [rrf(r) for r in dense_ranks]
bm25_scores  = [rrf(r) for r in bm25_ranks]
final_scores = [d + b for d, b in zip(dense_scores, bm25_scores)]
order = sorted(range(len(docs)), key=lambda i: -final_scores[i])

col_labels = ["Chunk", "Dense\nRank", "Dense\nRRF", "BM25\nRank", "BM25\nRRF", "Final\nRRF Score", "Kết quả"]
table_data = []
for i in order:
    pos = order.index(i) + 1
    table_data.append([
        docs[i],
        f"#{dense_ranks[i]}",
        f"{dense_scores[i]:.4f}",
        f"#{bm25_ranks[i]}",
        f"{bm25_scores[i]:.4f}",
        f"{final_scores[i]:.4f}",
        f"→ Hạng {order.index(i)+1}"
    ])

tbl = ax1.table(cellText=table_data, colLabels=col_labels,
                cellLoc="center", loc="center",
                bbox=[0.0, 0.05, 1.0, 0.9])
tbl.auto_set_font_size(False)
tbl.set_fontsize(9.5)

# Color header
for j in range(len(col_labels)):
    tbl[0, j].set_facecolor("#1565C0")
    tbl[0, j].set_text_props(color="white", fontweight="bold")

# Highlight top result
winner = order[0]
winner_row_in_table = 1  # first data row = best
for j in range(len(col_labels)):
    tbl[1, j].set_facecolor("#C8E6C9")
    tbl[1, j].set_text_props(fontweight="bold")

# Highlight Chunk A (dense rank 1 but not final winner if BM25 bad)
# Find Chunk A row
chunk_a_row = [i for i, row in enumerate(table_data) if row[0] == "Chunk A"]
if chunk_a_row and chunk_a_row[0]+1 != 1:
    for j in range(len(col_labels)):
        tbl[chunk_a_row[0]+1, j].set_facecolor("#FFCDD2")

ax1.set_title("Tính điểm RRF cho 5 chunk\n(Dense rank ≠ BM25 rank → fusion)", 
              fontsize=10, pad=8)

# Add insight text
insight = ("Chunk A: Dense #1 nhưng BM25 #4\n"
           "Chunk B: Dense #3 nhưng BM25 #2\n"
           "→ RRF ưu tiên chunk đồng thuận 2 phía")
ax1.text(0.5, -0.02, insight, ha="center", va="top", transform=ax1.transAxes,
         fontsize=8.5, color="#555", style="italic",
         bbox=dict(boxstyle="round,pad=0.3", facecolor="#FFF9C4", alpha=0.8))

# Right: Bar chart of final RRF scores with breakdown
x = np.arange(len(docs))
final_sorted = [final_scores[i] for i in order]
dense_sorted = [dense_scores[i] for i in order]
bm25_sorted  = [bm25_scores[i]  for i in order]
labels_sorted = [docs[i] for i in order]

b1 = ax2.bar(x, dense_sorted, 0.5, label=f"Dense RRF (1/(k+rank))",  color="#1565C0", alpha=0.85)
b2 = ax2.bar(x, bm25_sorted,  0.5, bottom=dense_sorted, label=f"BM25 RRF (1/(k+rank))", color="#388E3C", alpha=0.85)

for i, (ds, bs, fs) in enumerate(zip(dense_sorted, bm25_sorted, final_sorted)):
    ax2.text(i, fs + 0.0002, f"{fs:.4f}", ha="center", fontsize=8.5, fontweight="bold")

ax2.set_xticks(x)
ax2.set_xticklabels(labels_sorted, fontsize=10)
ax2.set_ylabel(f"RRF Score  (k={k})", fontsize=10)
ax2.set_title("Phân bổ điểm RRF theo nguồn\n(xanh = dense, lá = BM25)", fontsize=10, pad=8)
ax2.legend(fontsize=9, loc="upper right")
ax2.grid(axis="y", alpha=0.3)
ax2.spines["top"].set_visible(False)
ax2.spines["right"].set_visible(False)

# Annotate formula
ax2.text(0.01, 0.97,
         f"Công thức: Score(d) = Σ  1/(k + rank_i(d))\nk={k} (từ Optuna tối ưu)",
         transform=ax2.transAxes, va="top", fontsize=8.5,
         bbox=dict(boxstyle="round,pad=0.4", facecolor="#E3F2FD", alpha=0.9))

plt.tight_layout()
plt.savefig("concept_rrf.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: concept_rrf.png")

print("\nAll 4 conceptual diagrams done!")
