# -*- coding: utf-8 -*-
"""
Additional evaluation charts for thesis - each with unique scientific purpose.
"""
import json, optuna
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.colors import Normalize
from matplotlib.cm import ScalarMappable
import numpy as np
import warnings
warnings.filterwarnings("ignore")
optuna.logging.set_verbosity(optuna.logging.WARNING)

# ══════════════════════════════════════════════════════════════
# CHART A: RagTuning Best Params vs Search Range
# ══════════════════════════════════════════════════════════════
# Best params from Optuna sweep (appsettings.Development.json)
# Ranges are EXACT from RETRIEVAL_PARAMS in optuna_sweep.py + topK range in suggest_int call
best_params = {
    "RetrievePerQuery":          (23,  5,   30),   # (best, min, max) — from optuna_sweep.py
    "CandidatePoolLimit":        (30,  20,  80),
    "RerankCandidateLimit":      (16,  10,  30),
    "QueryVariantLimit":         (3,   2,   6),
    "DecompSubQueryLimit":       (2,   1,   5),
    "RrfK":                      (44,  20,  100),
    "BM25 k1":                   (1.34, 0.8, 2.5),
    "BM25 b":                    (0.57, 0.2, 1.0),
    "OutOfScopeThreshold":       (0.539, 0.38, 0.56),   # Optuna best=0.539 (appsettings set to 0.42 manually)
    "ClarifyThreshold":          (0.613, 0.48, 0.62),
    "TopK":                      (9,   5,   20),         # suggest_int("topK", 5, 20)
}

fig, ax = plt.subplots(figsize=(10, 6.5))

params = list(best_params.keys())
n = len(params)
y = np.arange(n)

for i, (name, (best, lo, hi)) in enumerate(best_params.items()):
    norm_lo   = 0.0
    norm_hi   = 1.0
    norm_best = (best - lo) / (hi - lo)

    # Search range bar (gray)
    ax.barh(i, norm_hi - norm_lo, left=norm_lo, height=0.35,
            color="#E0E0E0", edgecolor="#BDBDBD", linewidth=0.8)

    # Best value marker (colored dot)
    color = "#E53935" if norm_best > 0.7 else "#FB8C00" if norm_best > 0.4 else "#43A047"
    ax.plot(norm_best, i, "D", color=color, markersize=10, zorder=5)

    # Value label
    ax.text(norm_best + 0.02, i, f"{best}", va="center", fontsize=9, color=color, fontweight="bold")

ax.set_yticks(y)
ax.set_yticklabels(params, fontsize=10)
ax.set_xlim(-0.05, 1.25)
ax.set_xlabel("Normalized Search Range (0 = min, 1 = max)", fontsize=10)
ax.set_title("Hình 3.X. Giá trị tham số tối ưu trong không gian tìm kiếm Optuna\n"
             "(◆ = giá trị tối ưu; thanh xám = khoảng tìm kiếm)", fontsize=11, pad=12)

ax.axvline(0.0, color="#BDBDBD", linestyle="--", linewidth=0.8, alpha=0.5)
ax.axvline(1.0, color="#BDBDBD", linestyle="--", linewidth=0.8, alpha=0.5)
ax.text(0.0, n+0.2, "Min", ha="center", fontsize=8, color="gray")
ax.text(1.0, n+0.2, "Max", ha="center", fontsize=8, color="gray")

legend_items = [
    mpatches.Patch(color="#43A047", label="Thiên về min (<40%)"),
    mpatches.Patch(color="#FB8C00", label="Giữa (40–70%)"),
    mpatches.Patch(color="#E53935", label="Thiên về max (>70%)"),
]
ax.legend(handles=legend_items, loc="lower right", fontsize=9)
ax.grid(axis="x", alpha=0.3)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)
ax.invert_yaxis()

plt.tight_layout()
plt.savefig("chart_params_range.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_params_range.png")

# ══════════════════════════════════════════════════════════════
# CHART B: Optuna Parallel Coordinates (top 20 trials)
# ══════════════════════════════════════════════════════════════
study = optuna.load_study(study_name="rag_retrieval_sweep", storage="sqlite:///sweep2.db")
trials = [t for t in study.trials if t.value is not None]
trials_sorted = sorted(trials, key=lambda t: t.value, reverse=True)[:20]

plot_params = ["RetrievePerQuery", "CandidatePoolLimit", "TopK",
               "OutOfScopeScoreThreshold", "DecompositionSubQueryLimit"]
param_labels = ["RetrievePerQ", "CandidatePool", "TopK", "OOSThreshold", "DecompSubQ"]

n_params = len(plot_params)
fig, ax = plt.subplots(figsize=(12, 5.5))

cmap = plt.cm.RdYlGn
values_list = [t.value for t in trials_sorted]
norm = Normalize(vmin=min(values_list), vmax=max(values_list))

for trial in trials_sorted:
    vals = []
    for p in plot_params:
        v = trial.params.get(p, None)
        if v is None:
            vals.append(0.5)
            continue
        all_vals = [t.params[p] for t in trials if p in t.params]
        mn, mx = min(all_vals), max(all_vals)
        vals.append((v - mn) / (mx - mn) if mx > mn else 0.5)
    color = cmap(norm(trial.value))
    lw = 2.5 if trial.number == study.best_trial.number else 0.8
    alpha = 1.0 if trial.number == study.best_trial.number else 0.5
    ax.plot(range(n_params), vals, color=color, linewidth=lw, alpha=alpha,
            zorder=3 if trial.number == study.best_trial.number else 1)

# Highlight best trial
best = study.best_trial
best_vals = []
for p in plot_params:
    v = best.params.get(p, None)
    if v is None:
        best_vals.append(0.5)
        continue
    all_vals = [t.params[p] for t in trials if p in t.params]
    mn, mx = min(all_vals), max(all_vals)
    best_vals.append((v - mn) / (mx - mn) if mx > mn else 0.5)

ax.plot(range(n_params), best_vals, "o-", color="#B71C1C", linewidth=3,
        markersize=9, zorder=5, label=f"Best trial #{best.number} (obj={best.value:.4f})")

ax.set_xticks(range(n_params))
ax.set_xticklabels(param_labels, fontsize=10)
ax.set_ylabel("Normalized Value", fontsize=10)
ax.set_ylim(-0.05, 1.15)
ax.set_title("Hình 3.X+1. Parallel Coordinates — Top 20 trials Optuna\n"
             "(màu xanh = objective cao; màu đỏ = objective thấp)", fontsize=11, pad=12)
ax.grid(axis="y", alpha=0.3)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)

sm = ScalarMappable(cmap=cmap, norm=norm)
sm.set_array([])
cbar = plt.colorbar(sm, ax=ax, pad=0.02, shrink=0.8)
cbar.set_label("Objective Score", fontsize=9)

ax.legend(loc="upper right", fontsize=9)

# Add param value annotations for best trial
for i, (p, pname, nv) in enumerate(zip(plot_params, param_labels, best_vals)):
    v = best.params.get(p, None)
    if v is None:
        continue
    ax.annotate(f"{v:.2f}" if isinstance(v, float) else str(v),
                xy=(i, nv), xytext=(i, nv + 0.08),
                ha="center", fontsize=8, color="#B71C1C", fontweight="bold")

plt.tight_layout()
plt.savefig("chart_parallel_coords.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_parallel_coords.png")

# ══════════════════════════════════════════════════════════════
# CHART C: Error Analysis — Why Questions Fail (Dev Set)
# ══════════════════════════════════════════════════════════════
with open("results/score_dev_set.json") as f:
    dev = json.load(f)

fail_reasons = {}
partial_by_cat = {}
for d in dev["details"]:
    reason = d.get("reason", "unknown")
    cat = d.get("category", "?")
    recall = d.get("point_recall", 0)

    if not d.get("pass"):
        # Classify failure type
        if "refusal" in reason.lower() or "refuse" in reason.lower():
            ftype = "False Refusal\n(từ chối nhầm)"
        elif recall > 0:
            ftype = "Partial Recall\n(thiếu ý)"
        elif "wrong" in reason.lower() or recall == 0:
            ftype = "Wrong Answer\n(sai nội dung)"
        else:
            ftype = "Other"
        fail_reasons[ftype] = fail_reasons.get(ftype, 0) + 1

    # Track partial by category (pass but recall < 1)
    if d.get("pass") and recall < 1.0:
        partial_by_cat[cat] = partial_by_cat.get(cat, 0) + 1

fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(11, 4.5))

# Left: pie of failure types
if fail_reasons:
    labels = list(fail_reasons.keys())
    sizes  = list(fail_reasons.values())
    colors_pie = ["#EF5350", "#FFA726", "#66BB6A", "#42A5F5"][:len(labels)]
    wedges, texts, autotexts = ax1.pie(sizes, labels=labels, autopct="%1.0f%%",
                                        colors=colors_pie, startangle=90,
                                        textprops={"fontsize": 10},
                                        wedgeprops={"edgecolor": "white", "linewidth": 1.5})
    for at in autotexts:
        at.set_fontweight("bold")
    ax1.set_title(f"Phân loại nguyên nhân sai\n(Dev Set, {sum(sizes)} câu sai / 72 câu)", fontsize=10, pad=10)

# Right: "pass but partial" by category
CATS = ["fact", "comparison", "logic", "multi-file", "out-of-scope"]
CAT_SHORT = ["Fact", "Comparison", "Logic", "Multi-file", "OOS"]
pass_total  = [sum(1 for d in dev["details"] if d.get("pass") and d.get("category") == c) for c in CATS]
pass_full   = [sum(1 for d in dev["details"] if d.get("pass") and d.get("category") == c and d.get("point_recall", 0) >= 1.0) for c in CATS]
pass_partial= [p - f for p, f in zip(pass_total, pass_full)]
fail_total  = [sum(1 for d in dev["details"] if not d.get("pass") and d.get("category") == c) for c in CATS]

x = np.arange(len(CATS))
w = 0.5
b1 = ax2.bar(x, pass_full,   w, label="Đúng hoàn toàn (recall=100%)", color="#66BB6A")
b2 = ax2.bar(x, pass_partial,w, bottom=pass_full, label="Đúng một phần (50-99%)", color="#FFA726")
b3 = ax2.bar(x, fail_total,  w, bottom=[p+q for p,q in zip(pass_full, pass_partial)],
             label="Sai", color="#EF5350")

ax2.set_xticks(x)
ax2.set_xticklabels(CAT_SHORT, fontsize=10)
ax2.set_ylabel("Số câu hỏi", fontsize=10)
ax2.set_title("Phân bố kết quả theo loại câu hỏi\n(Dev Set — 72 câu)", fontsize=10, pad=10)
ax2.legend(fontsize=8, loc="upper right")
ax2.grid(axis="y", alpha=0.3)
ax2.spines["top"].set_visible(False)
ax2.spines["right"].set_visible(False)

fig.suptitle("Hình 4.X+3. Phân tích câu trả lời sai — Dev Set (Document RAG)", fontsize=11, y=1.01)
plt.tight_layout()
plt.savefig("chart_error_analysis.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_error_analysis.png")

# ══════════════════════════════════════════════════════════════
# CHART D: Latency Breakdown (from thesis 4.2.2 data)
# ══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(9, 4))

stages = ["Embedding\ncâu hỏi", "Vector Search\n(Qdrant)", "BM25 +\nReranking", "LLM Generate\n(Qwen2.5:7b)", "Stream +\nNetwork"]
times_local  = [0.05, 0.15, 0.45, 6.5, 0.35]  # local machine (seconds)
times_vastai = [0.04, 0.12, 0.38, 1.2, 0.36]  # Vast.AI RTX 3090 Ti

x = np.arange(len(stages))
w = 0.35
b1 = ax.bar(x - w/2, times_local,  w, label="Local CPU (qwen2.5:7b)", color="#90CAF9", edgecolor="white")
b2 = ax.bar(x + w/2, times_vastai, w, label="GPU RTX 3090 Ti (Vast.AI)", color="#66BB6A", edgecolor="white")

for bar in list(b1) + list(b2):
    h = bar.get_height()
    ax.text(bar.get_x() + bar.get_width()/2, h + 0.05,
            f"{h:.2f}s", ha="center", va="bottom", fontsize=8.5, fontweight="bold")

ax.set_xticks(x)
ax.set_xticklabels(stages, fontsize=9)
ax.set_ylabel("Thời gian (giây)", fontsize=11)
ax.set_title("Hình 4.X+4. Phân bổ độ trễ theo từng giai đoạn xử lý RAG Pipeline\n"
             "(Local vs GPU server — câu hỏi in-domain điển hình)", fontsize=11, pad=12)
ax.legend(fontsize=10, loc="upper left")
ax.grid(axis="y", alpha=0.3)
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)

# Total annotations
total_local  = sum(times_local)
total_vastai = sum(times_vastai)
ax.text(0.98, 0.95, f"Total local: ~{total_local:.1f}s\nTotal GPU: ~{total_vastai:.1f}s",
        transform=ax.transAxes, ha="right", va="top", fontsize=9,
        bbox=dict(boxstyle="round,pad=0.4", facecolor="lightyellow", alpha=0.9))

plt.tight_layout()
plt.savefig("chart_latency.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_latency.png")

print("\nAll 4 new charts generated!")
