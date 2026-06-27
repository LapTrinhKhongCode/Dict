# -*- coding: utf-8 -*-
"""
3 charts for thesis sections 3.3.1 and 3.3.2:
  M: Parent-Child Chunking structure diagram (3.3.1)
  N: Multi-stage Retrieval Pipeline flow (3.3.2)
  O: Dynamic ef_search — how it scales with scope/query count (3.3.2 HNSW)
"""
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import matplotlib.patheffects as pe
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import numpy as np
import warnings
warnings.filterwarnings("ignore")

# ══════════════════════════════════════════════════════════════
# CHART M: Parent-Child Chunking — OCR text → indexed structure
# ══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(13, 6))
ax.set_xlim(0, 13)
ax.set_ylim(0, 6)
ax.axis("off")
ax.set_facecolor("#FAFAFA")
fig.patch.set_facecolor("#FAFAFA")

def box(ax, x, y, w, h, color, text, fontsize=9, text_color="white", radius=0.15, bold=False):
    rect = FancyBboxPatch((x, y), w, h, boxstyle=f"round,pad={radius}",
                          facecolor=color, edgecolor="white", linewidth=1.5)
    ax.add_patch(rect)
    weight = "bold" if bold else "normal"
    ax.text(x + w/2, y + h/2, text, ha="center", va="center",
            fontsize=fontsize, color=text_color, fontweight=weight,
            wrap=True)

def arrow(ax, x1, y1, x2, y2, color="#555555"):
    ax.annotate("", xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=1.5))

# ── Column 0: OCR Raw ──
box(ax, 0.2, 2.2, 2.0, 1.6, "#1565C0",
    "OCR Text\n(trang 1…N)\ntext/table/figure\nsegments", fontsize=8.5, bold=True)

arrow(ax, 2.2, 3.0, 2.7, 3.0)

# ── Column 1: Parent chunks ──
ax.text(3.8, 5.6, "Parent Chunks\n(~1600 chars, overlap 240)", ha="center",
        fontsize=9, color="#1B5E20", fontweight="bold")
box(ax, 2.7, 4.5, 2.2, 0.85, "#2E7D32", "Parent 0\n[trang 1-2]", fontsize=8)
box(ax, 2.7, 3.3, 2.2, 0.85, "#388E3C", "Parent 1\n[trang 2-3]", fontsize=8)
box(ax, 2.7, 2.1, 2.2, 0.85, "#43A047", "Parent 2\n[trang 3-4]", fontsize=8)
box(ax, 2.7, 0.9, 2.2, 0.85, "#66BB6A", "Parent N\n[trang …]", fontsize=8, text_color="#1B5E20")

# overlap indicator
ax.annotate("", xy=(3.8, 3.3), xytext=(3.8, 3.35),
            arrowprops=dict(arrowstyle="<->", color="#FF7043", lw=1.5))
ax.text(4.0, 3.32, "overlap\n240 chars", fontsize=7, color="#FF7043")

arrow(ax, 4.9, 3.0, 5.4, 3.0)

# ── Column 2: Child chunks ──
ax.text(6.6, 5.6, "Child Chunks\n(~700 chars, overlap 120)", ha="center",
        fontsize=9, color="#4A148C", fontweight="bold")
for i, (label, y_pos) in enumerate([
    ("Child 0-0", 4.8), ("Child 0-1", 4.05),
    ("Child 1-0", 3.1), ("Child 1-1", 2.35),
    ("Child 2-0", 1.5), ("Child N-0", 0.75)
]):
    color = ["#6A1B9A", "#7B1FA2", "#8E24AA", "#9C27B0", "#AB47BC", "#CE93D8"][i]
    tcolor = "white" if i < 4 else "#4A148C"
    box(ax, 5.4, y_pos, 2.4, 0.6, color, label, fontsize=8, text_color=tcolor)

# parent→child links
for py, cy_list in [(4.925, [5.1, 4.35]), (3.725, [3.4, 2.65]), (2.525, [1.8])]:
    for cy in cy_list:
        ax.plot([4.9, 5.4], [py, cy], color="#9C27B0", lw=0.8, linestyle="--", alpha=0.7)

arrow(ax, 7.8, 3.0, 8.3, 3.0)

# ── Column 3: Qdrant index ──
ax.text(9.4, 5.6, "Qdrant HNSW Index\n(document_vectors)", ha="center",
        fontsize=9, color="#BF360C", fontweight="bold")

box(ax, 8.3, 4.3, 2.2, 0.75, "#E64A19", "Parent vector\n(parent_id, page)", fontsize=7.5)
box(ax, 8.3, 3.4, 2.2, 0.75, "#F4511E", "Child vector\n+ parent_id ref", fontsize=7.5)
box(ax, 8.3, 2.5, 2.2, 0.75, "#FF7043", "Child vector\n+ parent_id ref", fontsize=7.5)
box(ax, 8.3, 1.6, 2.2, 0.75, "#FF8A65", "…metadata:\njob_id, page, type", fontsize=7.5, text_color="#BF360C")

arrow(ax, 10.5, 3.2, 11.0, 3.2)

# ── Column 4: Retrieval result ──
box(ax, 11.0, 2.3, 1.8, 1.8, "#0277BD",
    "Retrieve\nchild →\nexpand to\nparent\ncontext", fontsize=7.5)

# Labels bottom
ax.text(0.2, 0.3, "① Segment\nParser", ha="left", fontsize=8, color="#1565C0")
ax.text(2.9, 0.3, "② Page-aware\nParent Chunking", ha="left", fontsize=8, color="#1B5E20")
ax.text(5.5, 0.3, "③ Fine-grained\nChild Chunking", ha="left", fontsize=8, color="#4A148C")
ax.text(8.4, 0.3, "④ HNSW\nVector Index", ha="left", fontsize=8, color="#BF360C")
ax.text(11.0, 0.3, "⑤ Context\nExpansion", ha="left", fontsize=8, color="#0277BD")

ax.set_title("Hình 3.X. Kiến trúc Parent-Child Chunking trong giai đoạn Indexing\n"
             "(OCR text → phân cấp chunk → Qdrant HNSW với payload metadata)",
             fontsize=11, fontweight="bold", pad=10)

plt.tight_layout()
plt.savefig("chart_indexing_structure.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_indexing_structure.png")


# ══════════════════════════════════════════════════════════════
# CHART N: Multi-stage Retrieval Pipeline flowchart
# ══════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(14, 7))
ax.set_xlim(0, 14)
ax.set_ylim(0, 7)
ax.axis("off")
fig.patch.set_facecolor("#F5F5F5")
ax.set_facecolor("#F5F5F5")

STAGE_COLORS = {
    "query":    "#1565C0",
    "expand":   "#4A148C",
    "search":   "#1B5E20",
    "fuse":     "#E65100",
    "rerank":   "#BF360C",
    "safety":   "#880E4F",
    "generate": "#0277BD",
}

def stage_box(ax, x, y, w, h, color, title, detail="", fontsize=9):
    rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.1",
                          facecolor=color, edgecolor="white", linewidth=1.8, alpha=0.92)
    ax.add_patch(rect)
    ax.text(x + w/2, y + h*0.65, title, ha="center", va="center",
            fontsize=fontsize, color="white", fontweight="bold")
    if detail:
        ax.text(x + w/2, y + h*0.28, detail, ha="center", va="center",
                fontsize=7.5, color="white", alpha=0.9)

def harrow(ax, x1, y, x2, color="#555", label=""):
    ax.annotate("", xy=(x2, y), xytext=(x1, y),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=1.8))
    if label:
        ax.text((x1+x2)/2, y+0.1, label, ha="center", fontsize=7.5, color=color)

def varrow(ax, x, y1, y2, color="#555"):
    ax.annotate("", xy=(x, y2), xytext=(x, y1),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=1.5))

# ── Row 1: Query input ──
stage_box(ax, 0.2, 5.4, 2.0, 1.0, STAGE_COLORS["query"], "User Query", "câu hỏi tự nhiên")
harrow(ax, 2.2, 5.9, 2.8, STAGE_COLORS["query"])

# ── Row 1: Query expansion (3 paths) ──
stage_box(ax, 2.8, 5.4, 2.8, 1.0, STAGE_COLORS["expand"],
          "Query Transform", "Variants + HyDE + Decomposition")
ax.text(4.2, 5.2, f"↑ {3} variants  |  HyDE doc  |  {2} sub-queries", ha="center", fontsize=7.5, color="#4A148C")
harrow(ax, 5.6, 5.9, 6.2, STAGE_COLORS["expand"])

# ── Row 1: Dense search ──
stage_box(ax, 6.2, 5.4, 2.6, 1.0, STAGE_COLORS["search"],
          "Dense Search", "Qdrant HNSW\nef_search=40–320 (dynamic)")
harrow(ax, 8.8, 5.9, 9.4, STAGE_COLORS["search"])

# ── Row 2: BM25 (parallel) ──
# Show parallel track
ax.text(4.2, 4.85, "║ Parallel", ha="center", fontsize=8, color="#666", style="italic")
stage_box(ax, 2.8, 3.8, 2.8, 0.85, STAGE_COLORS["search"],
          "BM25 Sparse", "TF-sat + IDF + Length-norm\nk1=1.34, b=0.57")
# connect expand → BM25
ax.plot([4.2, 4.2], [5.4, 4.65], color=STAGE_COLORS["expand"], lw=1.5, linestyle="--", alpha=0.7)
# BM25 → RRF
ax.plot([5.6, 9.4], [4.22, 4.22], color=STAGE_COLORS["search"], lw=1.5, linestyle="--", alpha=0.7)

# ── RRF box (right side) ──
stage_box(ax, 9.4, 4.8, 2.2, 1.5, STAGE_COLORS["fuse"],
          "RRF Fusion", "DenseRRF + KeywordRRF\nk=44\n+ Cross/Exact/ContentType")
ax.text(9.2, 4.65, "Dense →", ha="right", fontsize=7.5, color=STAGE_COLORS["search"])
ax.text(9.2, 4.25, "BM25 →", ha="right", fontsize=7.5, color=STAGE_COLORS["search"])

varrow(ax, 10.5, 4.8, 4.2, STAGE_COLORS["fuse"])

# ── LLM Rerank ──
stage_box(ax, 9.4, 3.0, 2.2, 1.5, STAGE_COLORS["rerank"],
          "LLM Reranking", f"Top-16 → LLM judge\n→ Top-9 final context")
varrow(ax, 10.5, 3.0, 2.3, STAGE_COLORS["rerank"])

# ── Safety gates ──
stage_box(ax, 6.2, 3.0, 2.8, 1.5, STAGE_COLORS["safety"],
          "Safety Gates", "OOS: score<0.42 → refuse\nClarify: score<0.61 → ask\nAmbiguity: multi-doc → pick")
ax.plot([9.4, 9.0], [3.75, 3.75], color=STAGE_COLORS["rerank"], lw=1.5)
ax.annotate("", xy=(9.0, 3.75), xytext=(9.4, 3.75),
            arrowprops=dict(arrowstyle="-|>", color=STAGE_COLORS["rerank"], lw=1.5))

# ── Cache lookup (top shortcut) ──
stage_box(ax, 0.2, 3.0, 2.0, 1.0, "#00695C", "Cache Lookup", "cosine≥0.92\n→ instant return")
ax.plot([1.2, 1.2], [5.4, 4.0], color="#00695C", lw=1.5, linestyle=":")
ax.annotate("", xy=(1.2, 4.0), xytext=(1.2, 5.4),
            arrowprops=dict(arrowstyle="-|>", color="#00695C", lw=1.3))

# ── History RAG ──
stage_box(ax, 0.2, 1.6, 2.0, 1.0, "#37474F", "History RAG", "conv. history\ntop-3 turns")

# ── Generate ──
stage_box(ax, 3.5, 1.0, 3.5, 1.4, STAGE_COLORS["generate"],
          "LLM Generation", "Gemini 2.5 Flash / Qwen2.5\nStream + Citation injection", fontsize=9)
harrow(ax, 6.2, 3.75, 7.0, STAGE_COLORS["safety"], label="pass")
ax.plot([7.0, 7.0], [3.0, 2.4], color=STAGE_COLORS["safety"], lw=1.5)
ax.annotate("", xy=(5.25, 2.4), xytext=(7.0, 2.4),
            arrowprops=dict(arrowstyle="-|>", color=STAGE_COLORS["safety"], lw=1.5))
ax.plot([2.2, 3.5], [1.7, 1.7], color="#37474F", lw=1.3, linestyle="--")

# ── Answer ──
stage_box(ax, 7.5, 1.0, 2.4, 1.4, STAGE_COLORS["query"], "Answer +\nCitations", "[Nguồn X, Tr.Y]")
harrow(ax, 7.0, 1.7, 7.5, STAGE_COLORS["generate"])

ax.set_title("Hình 3.X+1. Kiến trúc Multi-stage RAG Pipeline — luồng xử lý đầy đủ",
             fontsize=12, fontweight="bold", pad=12)

plt.tight_layout()
plt.savefig("chart_retrieval_pipeline.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_retrieval_pipeline.png")


# ══════════════════════════════════════════════════════════════
# CHART O: Dynamic ef_search — HNSW adaptive search depth
# ══════════════════════════════════════════════════════════════
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))
fig.suptitle("Hình 3.X+2. Chiến lược điều chỉnh ef_search động trong Qdrant HNSW\n"
             "(ef_search càng cao → accuracy↑ nhưng latency↑)", fontsize=11, fontweight="bold")

# ─ Left: ef_search vs scope_units (for different query counts) ─
# Formula from code: baseEf(mode) + log2(scope+1)*24 + (q-1)*8 + topK*4, clamp(40,320)
def calc_ef(scope_units, query_count, topk=9, mode="high"):
    base = {"low": 48, "balance": 96, "high": 144}[mode]
    scope_boost = int(round(np.log2(max(1, scope_units) + 1) * 24))
    query_boost = (max(1, query_count) - 1) * 8
    topk_boost = topk * 4
    return np.clip(base + scope_boost + query_boost + topk_boost, 40, 320)

scope_range = np.arange(1, 51)
for q, color, label in [(1, "#42A5F5", "1 query variant"), (3, "#66BB6A", "3 variants (Optuna best)"), (6, "#FF7043", "6 variants (max)")]:
    ef_vals = [calc_ef(s, q, mode="high") for s in scope_range]
    ax1.plot(scope_range, ef_vals, color=color, linewidth=2.2, label=label)

ax1.axhline(40,  color="#E53935", linestyle=":", lw=1.5, alpha=0.7)
ax1.axhline(320, color="#E53935", linestyle=":", lw=1.5, alpha=0.7)
ax1.text(1, 42, "min ef=40", fontsize=8, color="#E53935")
ax1.text(1, 315, "max ef=320", fontsize=8, color="#E53935")

ax1.axvline(1,  color="#BDBDBD", linestyle="--", lw=1.0)
ax1.text(1.5, 50, "1 doc\n(single)", fontsize=7.5, color="gray")
ax1.axvline(10, color="#BDBDBD", linestyle="--", lw=1.0)
ax1.text(10.5, 50, "10 docs\n(project)", fontsize=7.5, color="gray")

ax1.set_xlabel("Số tài liệu trong scope (scope_units)", fontsize=10)
ax1.set_ylabel("ef_search value", fontsize=10)
ax1.set_title("ef_search theo scope size\n(mode=high, topK=9)", fontsize=10, pad=8)
ax1.legend(fontsize=9, loc="lower right")
ax1.grid(alpha=0.3)
ax1.spines["top"].set_visible(False)
ax1.spines["right"].set_visible(False)

# ─ Right: ef_search decomposition — how each component contributes ─
modes = ["fast\n(single-doc)", "balance\n(multi-doc)", "high\n(project)"]
base_efs   = [48, 96, 144]
scope_10   = [int(round(np.log2(11) * 24))] * 3    # scope=10
query3     = [16] * 3                                # (3-1)*8
topk9      = [36] * 3                                # 9*4

x = np.arange(len(modes))
w = 0.5
b1 = ax2.bar(x, base_efs, w, label="Base ef (mode)", color="#1565C0")
b2 = ax2.bar(x, scope_10, w, bottom=base_efs, label="Scope boost (10 docs)", color="#43A047")
b3 = ax2.bar(x, query3, w, bottom=[a+b for a,b in zip(base_efs, scope_10)], label="Query boost (3 variants)", color="#FB8C00")
b4 = ax2.bar(x, topk9, w, bottom=[a+b+c for a,b,c in zip(base_efs, scope_10, query3)], label="TopK boost (K=9)", color="#E53935")

totals = [b+s+q+t for b,s,q,t in zip(base_efs, scope_10, query3, topk9)]
for i, total in enumerate(totals):
    ax2.text(i, total + 3, f"ef={min(total, 320)}", ha="center", fontsize=9.5, fontweight="bold", color="#212121")

ax2.axhline(320, color="#9C27B0", linestyle="--", lw=2.0)
ax2.text(2.3, 322, "Clamp max=320", fontsize=8, color="#9C27B0")

ax2.set_xticks(x)
ax2.set_xticklabels(modes, fontsize=9)
ax2.set_ylabel("ef_search value", fontsize=10)
ax2.set_title("Phân tích thành phần ef_search\n(scope=10, 3 variants, topK=9)", fontsize=10, pad=8)
ax2.legend(fontsize=8.5, loc="upper left")
ax2.grid(axis="y", alpha=0.3)
ax2.spines["top"].set_visible(False)
ax2.spines["right"].set_visible(False)

plt.tight_layout()
plt.savefig("chart_hnsw_ef_search.png", dpi=150, bbox_inches="tight")
plt.close()
print("Saved: chart_hnsw_ef_search.png")

print("\nAll 3 structural diagrams done!")
