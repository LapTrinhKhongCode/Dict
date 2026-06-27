# -*- coding: utf-8 -*-
"""
4 additional charts for thesis:
  I:  Best-so-far convergence (Optuna)
  J:  BM25 k1 × b heatmap
  K:  Retrieval score distribution (in-domain vs OOS)
  L:  Category distribution across 3 evaluation sets
"""
import json, glob, os, optuna
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import numpy as np
import warnings
warnings.filterwarnings("ignore")
optuna.logging.set_verbosity(optuna.logging.WARNING)

study = optuna.load_study(study_name="rag_retrieval_sweep", storage="sqlite:///sweep2.db")
trials = sorted([t for t in study.trials if t.value is not None], key=lambda t: t.number)
best = study.best_trial

# ══════════════════════════════════════════════════════════════
# CHART I: Best-so-far convergence curve
# ══════════════════════════════════════════════════════════════
trial_nums = [t.number for t in trials]
obj_vals   = [t.value  for t in trials]

best_so_far = []
running_best = -1
for v in obj_vals:
    if v > running_best:
        running_best = v
    best_so_far.append(running_best)

fig, ax = plt.subplots(figsize=(9, 4.5))

# All trial scores (scatter)
colors = ["#EF5350" if v < 0.60 else "#FFA726" if v < 0.68 else "#66BB6A" for v in obj_vals]
ax.scatter(trial_nums, obj_vals, c=colors, s=45, alpha=0.70, zorder=3,
           edgecolors="white", linewidths=0.4, label="Objective từng trial")

# Best-so-far line
ax.step(trial_nums, best_so_far, where="post", color="#1565C0", linewidth=2.8,
        zorder=4, label="Best-so-far")
ax.fill_between(trial_nums, best_so_far, alpha=0.08, color="#1565C0", step="post")

# Mark best trial
ax.plot(best.number, best.value, "*", markersize=18, color="#FDD835",
        markeredgecolor="#B71C1C", markeredgewidth=1.5, zorder=5,
        label=f"Best: trial #{best.number}, obj={best.value:.4f}")

# Improvement annotations
improvements = [(i, v) for i, v in zip(trial_nums, best_so_far)
                if i == 0 or v > best_so_far[trial_nums.index(i)-1]]
for i, (tn, bv) in enumerate(improvements):
    if i > 0:
        ax.annotate(f"↑{bv:.3f}", xy=(tn, bv), xytext=(tn+0.5, bv+0.008),
                    fontsize=7.5, color="#1565C0", fontweight="bold")

ax.set_xlabel("Trial Number", fontsize=11)
ax.set_ylabel("Objective Score (Accuracy)", fontsize=11)
ax.set_title("Hình 3.X. Đường hội tụ Best-so-far của Optuna TPE\n"
             "(50 trials — màu xanh = tốt, đỏ = kém)", fontsize=11, pad=10)
ax.legend(fontsize=9, loc="lower right")
ax.grid(alpha=0.3)
ax.set_xlim(-1, 51)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)

extra_legend = [
    mpatches.Patch(facecolor="#EF5350", alpha=0.7, label="< 0.60"),
    mpatches.Patch(facecolor="#FFA726", alpha=0.7, label="0.60–0.68"),
    mpatches.Patch(facecolor="#66BB6A", alpha=0.7, label="> 0.68"),
]
handles, labels = ax.get_legend_handles_labels()
ax.legend(handles=handles + extra_legend, labels=labels + ["< 0.60", "0.60–0.68", "> 0.68"],
          fontsize=8.5, loc="lower right", ncol=2)

plt.tight_layout()
plt.savefig("chart_convergence_bsf.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_convergence_bsf.png")


# ══════════════════════════════════════════════════════════════
# CHART J: BM25 k1 × b heatmap (from Optuna trials)
# ══════════════════════════════════════════════════════════════
k1_vals  = np.array([t.params["Bm25K1"] for t in trials])
b_vals   = np.array([t.params["Bm25B"]  for t in trials])
obj_arr  = np.array([t.value             for t in trials])

fig, (ax, ax_text) = plt.subplots(1, 2, figsize=(12, 5),
                                   gridspec_kw={"width_ratios": [2, 1]})

# Bin into 4×4
k1_bins = np.linspace(k1_vals.min(), k1_vals.max(), 5)
b_bins  = np.linspace(b_vals.min(),  b_vals.max(),  5)

grid   = np.full((4, 4), np.nan)
counts = np.zeros((4, 4), dtype=int)

for i in range(4):
    for j in range(4):
        mask = ((k1_vals >= k1_bins[i]) & (k1_vals < k1_bins[i+1]) &
                (b_vals  >= b_bins[j])  & (b_vals  < b_bins[j+1]))
        if mask.sum() > 0:
            grid[i, j]   = obj_arr[mask].mean()
            counts[i, j] = mask.sum()

k1_centers = (k1_bins[:-1] + k1_bins[1:]) / 2
b_centers  = (b_bins[:-1]  + b_bins[1:])  / 2

im = ax.imshow(grid.T, origin="lower", aspect="auto", cmap="YlOrRd",
               extent=[k1_bins[0], k1_bins[-1], b_bins[0], b_bins[-1]],
               vmin=np.nanmin(grid), vmax=np.nanmax(grid))

for i in range(4):
    for j in range(4):
        if not np.isnan(grid[i, j]):
            ax.text(k1_centers[i], b_centers[j],
                    f"{grid[i,j]:.3f}\n(n={counts[i,j]})",
                    ha="center", va="center", fontsize=9,
                    color="black" if grid[i,j] < 0.64 else "white",
                    fontweight="bold")

# Mark best
ax.plot(best.params["Bm25K1"], best.params["Bm25B"],
        "*", markersize=18, color="#E3F2FD", markeredgecolor="#0D47A1",
        markeredgewidth=1.8, zorder=10,
        label=f"Best: k1={best.params['Bm25K1']:.3f}, b={best.params['Bm25B']:.3f}")

plt.colorbar(im, ax=ax, label="Avg Objective Score")
ax.set_xlabel("BM25 k1 (term frequency saturation)", fontsize=10)
ax.set_ylabel("BM25 b (document length normalization)", fontsize=10)
ax.set_title("Hình 3.X+1. Bề mặt objective BM25 k1 × b\n(từ 50 Optuna trials)", fontsize=11, pad=10)
ax.legend(loc="upper left", fontsize=9)

# Right panel: interpretation text
ax_text.axis("off")
text_content = (
    "Giải thích tham số BM25:\n\n"
    "▪ k1 (term frequency saturation)\n"
    "  • k1 thấp (0.5-1.0): TF bão hòa nhanh,\n"
    "    ít ưu tiên từ xuất hiện nhiều lần\n"
    "  • k1 cao (>1.5): ưu tiên mạnh hơn\n"
    "    tần suất từ khóa\n"
    f"  → Best: k1={best.params['Bm25K1']:.3f} (trung-cao)\n\n"
    "▪ b (document length normalization)\n"
    "  • b=0: không chuẩn hóa độ dài\n"
    "  • b=1: chuẩn hóa hoàn toàn\n"
    "  • b~0.5-0.7: cân bằng tốt nhất\n"
    "    cho OCR text (độ dài không đều)\n"
    f"  → Best: b={best.params['Bm25B']:.3f}\n\n"
    "▪ Kết hợp BM25 + Dense (RRF):\n"
    "  BM25 bắt exact keyword match\n"
    "  Dense bắt semantic similarity\n"
    "  → Hybrid tốt hơn từng loại đơn lẻ"
)
ax_text.text(0.02, 0.95, text_content, transform=ax_text.transAxes,
             va="top", ha="left", fontsize=9.5, fontfamily="monospace",
             bbox=dict(boxstyle="round,pad=0.5", facecolor="#FFF8E1", alpha=0.8))

plt.tight_layout()
plt.savefig("chart_bm25_heatmap.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_bm25_heatmap.png")


# ══════════════════════════════════════════════════════════════
# CHART K: Retrieval score distribution — in-domain vs OOS
# ══════════════════════════════════════════════════════════════
scores_in_all, scores_oos_all = [], []

for fname in ["results/raw_dev_set.jsonl", "results/raw_final_set.jsonl",
              "results/raw_holdout_set.jsonl"]:
    if not os.path.exists(fname):
        continue
    with open(fname, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            d = json.loads(line)
            is_oos = d.get("must_refuse", False)
            for s in d.get("sources", []):
                sc = s.get("score", 0)
                if is_oos:
                    scores_oos_all.append(sc)
                else:
                    scores_in_all.append(sc)

fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))
fig.suptitle("Hình 3.X+2. Phân bố Retrieval Score — In-domain vs Out-of-Scope",
             fontsize=12, fontweight="bold")

# Left: overlapping histogram
bins = np.linspace(0.78, 1.0, 35)
ax1.hist(scores_in_all,  bins=bins, alpha=0.55, color="#43A047", label=f"In-domain (n={len(scores_in_all)})",
         density=True, edgecolor="white", linewidth=0.3)
ax1.hist(scores_oos_all, bins=bins, alpha=0.55, color="#EF5350", label=f"Out-of-scope (n={len(scores_oos_all)})",
         density=True, edgecolor="white", linewidth=0.3)

# Mark OOS threshold
oos_thresh = 0.42  # manual setting
# In cosine similarity context for Qdrant the score here is the raw retrieval score
# Draw the mean lines
mean_in  = np.mean(scores_in_all)
mean_oos = np.mean(scores_oos_all)
ax1.axvline(mean_in,  color="#2E7D32", linestyle="--", linewidth=2.0,
            label=f"Mean in-domain={mean_in:.3f}")
ax1.axvline(mean_oos, color="#C62828", linestyle="--", linewidth=2.0,
            label=f"Mean OOS={mean_oos:.3f}")

ax1.set_xlabel("Retrieval Score (cosine similarity)", fontsize=10)
ax1.set_ylabel("Mật độ", fontsize=10)
ax1.set_title("Phân bố điểm tương đồng\nchunk được truy xuất", fontsize=10, pad=8)
ax1.legend(fontsize=8.5, loc="upper left")
ax1.grid(alpha=0.3)
ax1.spines["top"].set_visible(False)
ax1.spines["right"].set_visible(False)

# Right: box plot + strip plot by top-1 vs top-5
# Group by rank (1st retrieved, 2nd, ... 5th)
rank_scores_in  = {r: [] for r in range(1, 9)}
rank_scores_oos = {r: [] for r in range(1, 9)}

for fname in ["results/raw_dev_set.jsonl", "results/raw_final_set.jsonl",
              "results/raw_holdout_set.jsonl"]:
    if not os.path.exists(fname):
        continue
    with open(fname, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            d = json.loads(line)
            is_oos = d.get("must_refuse", False)
            srcs = sorted(d.get("sources", []), key=lambda s: -s.get("score", 0))
            for rank, s in enumerate(srcs[:8], 1):
                sc = s.get("score", 0)
                if is_oos:
                    rank_scores_oos[rank].append(sc)
                else:
                    rank_scores_in[rank].append(sc)

ranks = list(range(1, 9))
means_in  = [np.mean(rank_scores_in[r])  if rank_scores_in[r]  else 0 for r in ranks]
means_oos = [np.mean(rank_scores_oos[r]) if rank_scores_oos[r] else 0 for r in ranks]
stds_in   = [np.std(rank_scores_in[r])   if rank_scores_in[r]  else 0 for r in ranks]
stds_oos  = [np.std(rank_scores_oos[r])  if rank_scores_oos[r] else 0 for r in ranks]

x = np.array(ranks)
ax2.errorbar(x - 0.15, means_in,  yerr=stds_in,  fmt="o-", color="#43A047",
             linewidth=2, markersize=7, capsize=4, label="In-domain", zorder=4)
ax2.errorbar(x + 0.15, means_oos, yerr=stds_oos, fmt="s--", color="#EF5350",
             linewidth=2, markersize=7, capsize=4, label="Out-of-scope", zorder=4)
ax2.fill_between(x - 0.15,
                 [m - s for m, s in zip(means_in, stds_in)],
                 [m + s for m, s in zip(means_in, stds_in)],
                 alpha=0.1, color="#43A047")
ax2.fill_between(x + 0.15,
                 [m - s for m, s in zip(means_oos, stds_oos)],
                 [m + s for m, s in zip(means_oos, stds_oos)],
                 alpha=0.1, color="#EF5350")

ax2.set_xticks(ranks)
ax2.set_xlabel("Retrieval Rank (1 = top result)", fontsize=10)
ax2.set_ylabel("Avg Similarity Score ± σ", fontsize=10)
ax2.set_title("Score theo thứ hạng truy xuất\n(in-domain vs OOS)", fontsize=10, pad=8)
ax2.legend(fontsize=9)
ax2.grid(alpha=0.3)
ax2.spines["top"].set_visible(False)
ax2.spines["right"].set_visible(False)

# Gap annotation
gap = means_in[0] - means_oos[0]
ax2.annotate(f"Δ={gap:.4f}\n(rank 1)", xy=(1, (means_in[0]+means_oos[0])/2),
             xytext=(2.5, means_oos[0] + 0.005),
             fontsize=9, color="#6A1B9A", fontweight="bold",
             arrowprops=dict(arrowstyle="->", color="#6A1B9A", lw=1.3))

plt.tight_layout()
plt.savefig("chart_score_distribution.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_score_distribution.png")


# ══════════════════════════════════════════════════════════════
# CHART L: Category distribution across 3 evaluation sets
# ══════════════════════════════════════════════════════════════
SETS = [
    ("Dev Set\n(72 câu)",     "datasets/dev_set.jsonl"),
    ("Holdout Set\n(32 câu)", ["datasets/holdout_set.jsonl", "datasets/holdout_v2.jsonl"]),
    ("Final Set\n(25 câu)",   "datasets/final_set.jsonl"),
]
CATS = ["fact", "comparison", "logic", "multi-file", "out-of-scope"]
CAT_COLORS = ["#42A5F5", "#EF5350", "#66BB6A", "#FFA726", "#AB47BC"]

cat_counts = {}
for name, files in SETS:
    cnt = {c: 0 for c in CATS}
    file_list = files if isinstance(files, list) else [files]
    for f in file_list:
        if not os.path.exists(f):
            continue
        with open(f, encoding="utf-8") as fp:
            for line in fp:
                line = line.strip()
                if not line: continue
                d = json.loads(line)
                c = d.get("category", "?")
                if c in cnt:
                    cnt[c] += 1
    cat_counts[name] = cnt

fig, axes = plt.subplots(1, 4, figsize=(14, 5),
                          gridspec_kw={"width_ratios": [1, 1, 1, 1.2]})
fig.suptitle("Hình 4.X. Phân bố loại câu hỏi trong 3 bộ đánh giá\n(methodology — đảm bảo tính cân bằng)",
             fontsize=11, fontweight="bold")

for ax, (name, _) in zip(axes[:3], SETS):
    cnt = cat_counts[name]
    vals = [cnt[c] for c in CATS]
    total = sum(vals)
    wedges, texts, autotexts = ax.pie(
        vals, labels=None,
        autopct=lambda p: f"{p:.0f}%\n({round(p*total/100)})" if p > 0 else "",
        colors=CAT_COLORS, startangle=90,
        wedgeprops={"edgecolor": "white", "linewidth": 1.5},
        textprops={"fontsize": 8.5}
    )
    for at in autotexts:
        at.set_fontweight("bold")
    ax.set_title(name, fontsize=10, pad=8)

# Legend panel
axes[3].axis("off")
legend_items = [mpatches.Patch(color=c, label=cat.replace("-", " ").title())
                for c, cat in zip(CAT_COLORS, CATS)]
axes[3].legend(handles=legend_items, loc="center", fontsize=11,
               title="Loại câu hỏi", title_fontsize=11,
               frameon=True, edgecolor="gray")

# Add summary table below legend
set_names_short = ["Dev", "Holdout", "Final"]
summary_data = [[cat_counts[n][c] for n, _ in SETS] for c in CATS]
row_labels = [c.replace("-", " ").title() for c in CATS]
col_labels = set_names_short

tbl = axes[3].table(cellText=summary_data,
                     rowLabels=row_labels,
                     colLabels=col_labels,
                     cellLoc="center",
                     loc="lower center",
                     bbox=[0.0, 0.0, 1.0, 0.38])
tbl.auto_set_font_size(False)
tbl.set_fontsize(9.5)
for (row, col), cell in tbl.get_celld().items():
    if row == 0 or col == -1:
        cell.set_facecolor("#E3F2FD")
        cell.set_text_props(fontweight="bold")

plt.tight_layout()
plt.savefig("chart_dataset_balance.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_dataset_balance.png")

print("\nAll 4 supplementary charts done!")
