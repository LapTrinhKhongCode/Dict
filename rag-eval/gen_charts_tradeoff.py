# -*- coding: utf-8 -*-
"""
Trade-off analysis charts for RAG Pipeline parameters.
Two charts:
  F: Empirical sensitivity from Optuna trials (scatter + trend)
  G: Conceptual trade-off curves for 4 key parameters
"""
import optuna, json
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import numpy as np
from scipy.interpolate import make_interp_spline
import warnings
warnings.filterwarnings("ignore")
optuna.logging.set_verbosity(optuna.logging.WARNING)

study = optuna.load_study(study_name="rag_retrieval_sweep", storage="sqlite:///sweep2.db")
trials = [t for t in study.trials if t.value is not None]
best = study.best_trial

# ══════════════════════════════════════════════════════════════
# CHART F: Empirical Sensitivity — param value vs objective
# 4 most important params from fANOVA
# ══════════════════════════════════════════════════════════════
FOCUS_PARAMS = [
    ("OutOfScopeScoreThreshold", "OOS Score Threshold",  best.params["OutOfScopeScoreThreshold"],
     "Threshold loại câu ngoài phạm vi\n(thấp=ít từ chối, cao=từ chối nhiều)"),
    ("topK",                     "Top-K Chunks",          best.params["topK"],
     "Số chunk context đưa vào LLM\n(tăng→recall↑ noise↑)"),
    ("RetrievePerQuery",         "Retrieve Per Query",     best.params["RetrievePerQuery"],
     "Số vector retrieved mỗi query variant\n(tăng→recall↑ latency↑)"),
    ("DecompositionSubQueryLimit","Decomp. Sub-queries",   best.params["DecompositionSubQueryLimit"],
     "Số sub-query khi decompose\n(tăng→recall↑ latency↑)"),
]

fig, axes = plt.subplots(2, 2, figsize=(12, 8))
fig.suptitle("Hình 4.X. Phân tích Sensitivity — Giá trị tham số vs Objective Score (50 Optuna trials)",
             fontsize=12, fontweight="bold", y=1.01)

for ax, (param_key, param_name, best_val, desc) in zip(axes.flat, FOCUS_PARAMS):
    xs = np.array([t.params[param_key] for t in trials])
    ys = np.array([t.value for t in trials])

    # Scatter points colored by objective
    sc = ax.scatter(xs, ys, c=ys, cmap="RdYlGn", s=55, alpha=0.75,
                    vmin=ys.min(), vmax=ys.max(), zorder=3, edgecolors="white", linewidths=0.4)

    # LOWESS-like trend: bin average
    bins = np.linspace(xs.min(), xs.max(), 7)
    bin_centers, bin_means = [], []
    for i in range(len(bins)-1):
        mask = (xs >= bins[i]) & (xs < bins[i+1])
        if mask.sum() >= 1:
            bin_centers.append((bins[i] + bins[i+1]) / 2)
            bin_means.append(ys[mask].mean())

    if len(bin_centers) >= 3:
        try:
            bc = np.array(bin_centers)
            bm = np.array(bin_means)
            spl = make_interp_spline(bc, bm, k=min(2, len(bc)-1))
            xs_smooth = np.linspace(bc[0], bc[-1], 200)
            ax.plot(xs_smooth, spl(xs_smooth), "-", color="#1565C0", linewidth=2, alpha=0.8,
                    label="Trend (bin avg)", zorder=4)
        except Exception:
            ax.plot(bin_centers, bin_means, "-", color="#1565C0", linewidth=2, alpha=0.8, zorder=4)

    # Best value marker
    ax.axvline(best_val, color="#B71C1C", linestyle="--", linewidth=1.8, zorder=5)
    ax.text(best_val, ax.get_ylim()[1] if ax.get_ylim()[1] > 0 else 0.75,
            f" Best={best_val:.3g}", color="#B71C1C", fontsize=8.5, va="top", fontweight="bold")

    ax.set_xlabel(f"{param_name}", fontsize=10)
    ax.set_ylabel("Objective Score", fontsize=9)
    ax.set_title(desc, fontsize=9, pad=6)
    ax.grid(alpha=0.3)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    plt.colorbar(sc, ax=ax, shrink=0.7, pad=0.02).set_label("Obj", fontsize=7)

plt.tight_layout()
plt.savefig("chart_sensitivity.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_sensitivity.png")


# ══════════════════════════════════════════════════════════════
# CHART G: Conceptual Trade-off curves
# 3 panels: OOS threshold, TopK, RetrievePerQuery
# ══════════════════════════════════════════════════════════════
fig, axes = plt.subplots(1, 3, figsize=(14, 5))
fig.suptitle("Hình 4.X+1. Phân tích Trade-off — Tác động hai chiều của 3 tham số chính",
             fontsize=12, fontweight="bold", y=1.02)

ANNOT_KW = dict(fontsize=8.5, arrowprops=dict(arrowstyle="->", color="gray", lw=1.2))

# ─── Panel 1: OutOfScopeScoreThreshold trade-off ───
ax = axes[0]
x = np.linspace(0.30, 0.65, 300)

# OOS detection (precision for OOS class): sigmoid-like, increases with threshold
oos_detect = 1 / (1 + np.exp(-15 * (x - 0.45)))

# In-domain recall: sigmoid-like, decreases as threshold rises (more false refusals)
indomain_recall = 1 / (1 + np.exp(12 * (x - 0.50)))

# F1-like composite
composite = 2 * oos_detect * indomain_recall / (oos_detect + indomain_recall + 1e-9)
composite = composite / composite.max() * 0.92  # normalize visually

ax.plot(x, oos_detect,      color="#E53935", linewidth=2.2, label="OOS detection accuracy ↑")
ax.plot(x, indomain_recall, color="#43A047", linewidth=2.2, label="In-domain recall ↑")
ax.plot(x, composite,       color="#1565C0", linewidth=2.5, linestyle="--", label="Composite (F1-like) ↑")

# Mark Optuna best (0.539)
best_oos = best.params["OutOfScopeScoreThreshold"]
ax.axvline(best_oos, color="#FB8C00", linestyle=":", linewidth=2.0, zorder=5)
ax.text(best_oos + 0.005, 0.35, f"Optuna\nbest={best_oos:.3f}", color="#FB8C00",
        fontsize=8, fontweight="bold", va="bottom")

# Mark manual setting (0.42)
manual_oos = 0.42
ax.axvline(manual_oos, color="#7B1FA2", linestyle="-.", linewidth=1.8, zorder=5)
ax.text(manual_oos - 0.005, 0.15, f"Thực tế\n={manual_oos}", color="#7B1FA2",
        fontsize=8, fontweight="bold", ha="right")

ax.annotate("← Từ chối ít\n(bỏ sót OOS)", xy=(0.35, 0.70), fontsize=8, color="#B71C1C",
            ha="left", style="italic")
ax.annotate("Từ chối nhiều →\n(sai in-domain)", xy=(0.58, 0.70), fontsize=8, color="#B71C1C",
            ha="right", style="italic")

ax.set_xlabel("OutOfScopeScoreThreshold", fontsize=10)
ax.set_ylabel("Normalized Score", fontsize=9)
ax.set_title("OOS Threshold:\nTừ chối vs Không từ chối nhầm", fontsize=10, pad=6)
ax.legend(fontsize=8, loc="center")
ax.set_ylim(0, 1.1)
ax.grid(alpha=0.3)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)

# ─── Panel 2: TopK trade-off ───
ax = axes[1]
x = np.arange(3, 21)

# Recall: log-like increase (diminishing returns)
recall = 0.55 + 0.35 * (1 - np.exp(-(x - 3) / 5))

# Context noise / hallucination risk: increases with K
noise = 0.15 + 0.75 * (1 - np.exp(-(x - 3) / 8))

# Precision of answer: decreases due to noise
precision = 0.90 - 0.45 * (x - 3) / (21 - 3)

# Answer quality composite
quality = 0.4 * recall + 0.6 * precision

ax.plot(x, recall,    color="#43A047", linewidth=2.2, marker=".", markersize=6, label="Chunk Recall ↑")
ax.plot(x, precision, color="#E53935", linewidth=2.2, marker=".", markersize=6, label="Answer Precision ↑")
ax.plot(x, noise,     color="#EF9A9A", linewidth=1.5, linestyle=":", label="Context Noise ↓")
ax.plot(x, quality,   color="#1565C0", linewidth=2.5, linestyle="--", label="Quality Composite ↑")

best_topk = best.params["topK"]
ax.axvline(best_topk, color="#FB8C00", linestyle=":", linewidth=2.0, zorder=5)
ax.text(best_topk + 0.3, 0.15, f"Best\nK={best_topk}", color="#FB8C00",
        fontsize=9, fontweight="bold")

ax.set_xlabel("TopK (số chunk đưa vào LLM)", fontsize=10)
ax.set_ylabel("Normalized Score", fontsize=9)
ax.set_title("Top-K:\nRecall vs Precision / Noise", fontsize=10, pad=6)
ax.legend(fontsize=8, loc="lower left")
ax.set_ylim(0, 1.1)
ax.grid(alpha=0.3)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)

# ─── Panel 3: RetrievePerQuery vs Latency ───
ax = axes[2]
x = np.arange(5, 31)

# Dense recall: log-like saturates
dense_recall = 0.50 + 0.42 * (1 - np.exp(-(x - 5) / 8))

# Latency (normalized to 0-1): linear with slight acceleration
latency = 0.10 + 0.88 * ((x - 5) / 25) ** 0.85

# Hybrid RRF quality (recall - latency penalty)
hybrid_q = dense_recall - 0.3 * latency
hybrid_q = (hybrid_q - hybrid_q.min()) / (hybrid_q.max() - hybrid_q.min()) * 0.7 + 0.30

ax.plot(x, dense_recall, color="#43A047", linewidth=2.2, marker=".", markersize=6,
        label="Dense Recall ↑")
ax.plot(x, latency,      color="#E53935", linewidth=2.2, marker=".", markersize=6,
        label="Latency (normalized) ↓")
ax.plot(x, hybrid_q,     color="#1565C0", linewidth=2.5, linestyle="--",
        label="Recall − Latency Composite ↑")

best_rpq = best.params["RetrievePerQuery"]
ax.axvline(best_rpq, color="#FB8C00", linestyle=":", linewidth=2.0, zorder=5)
ax.text(best_rpq + 0.3, 0.15, f"Best\n={best_rpq}", color="#FB8C00",
        fontsize=9, fontweight="bold")

ax.fill_between(x, dense_recall, latency,
                where=dense_recall > latency,
                alpha=0.08, color="green", label="Vùng có lợi (recall > latency)")

ax.set_xlabel("RetrievePerQuery (số vector mỗi query)", fontsize=10)
ax.set_ylabel("Normalized Score / Cost", fontsize=9)
ax.set_title("Retrieve Per Query:\nRecall vs Latency", fontsize=10, pad=6)
ax.legend(fontsize=8, loc="lower right")
ax.set_ylim(0, 1.1)
ax.grid(alpha=0.3)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)

plt.tight_layout()
plt.savefig("chart_tradeoff.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_tradeoff.png")

# ══════════════════════════════════════════════════════════════
# CHART H: 2-D Heatmap — OOS Threshold × TopK objective surface
# From actual Optuna trials (binned)
# ══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(8, 5.5))

oos_vals = np.array([t.params["OutOfScopeScoreThreshold"] for t in trials])
topk_vals = np.array([t.params["topK"] for t in trials])
obj_vals  = np.array([t.value for t in trials])

# Bin OOS into 4, topK into 4
oos_bins  = np.linspace(oos_vals.min(),  oos_vals.max(),  5)
topk_bins = np.linspace(topk_vals.min(), topk_vals.max(), 5)

grid = np.full((4, 4), np.nan)
counts = np.zeros((4, 4), dtype=int)

for i in range(4):
    for j in range(4):
        mask = ((oos_vals  >= oos_bins[i])  & (oos_vals  < oos_bins[i+1]) &
                (topk_vals >= topk_bins[j]) & (topk_vals < topk_bins[j+1]))
        if mask.sum() > 0:
            grid[i, j]  = obj_vals[mask].mean()
            counts[i, j] = mask.sum()

oos_centers  = (oos_bins[:-1]  + oos_bins[1:])  / 2
topk_centers = (topk_bins[:-1] + topk_bins[1:]) / 2

im = ax.imshow(grid.T, origin="lower", aspect="auto", cmap="RdYlGn",
               extent=[oos_bins[0], oos_bins[-1], topk_bins[0], topk_bins[-1]],
               vmin=0.5, vmax=0.75)

for i in range(4):
    for j in range(4):
        if not np.isnan(grid[i, j]):
            ax.text(oos_centers[i], topk_centers[j],
                    f"{grid[i,j]:.3f}\n(n={counts[i,j]})",
                    ha="center", va="center", fontsize=9,
                    color="black" if grid[i,j] > 0.58 else "white",
                    fontweight="bold")

# Mark best trial point
ax.plot(best.params["OutOfScopeScoreThreshold"], best.params["topK"],
        "*", markersize=18, color="#FDD835", markeredgecolor="#B71C1C",
        markeredgewidth=1.5, zorder=10, label=f"Best trial #{best.number}")

# Mark manual setting
ax.plot(0.42, 9, "D", markersize=12, color="#CE93D8", markeredgecolor="#6A1B9A",
        markeredgewidth=1.5, zorder=10, label="Manual setting (0.42, 9)")

plt.colorbar(im, ax=ax, label="Avg Objective Score")
ax.set_xlabel("OutOfScopeScoreThreshold", fontsize=11)
ax.set_ylabel("TopK", fontsize=11)
ax.set_title("Hình 4.X+2. Bề mặt objective theo OOS Threshold × TopK\n"
             "(từ 50 Optuna trials — màu xanh = score cao)", fontsize=11, pad=10)
ax.legend(loc="upper left", fontsize=9)

plt.tight_layout()
plt.savefig("chart_heatmap_2d.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_heatmap_2d.png")

print("\nAll trade-off charts done!")
