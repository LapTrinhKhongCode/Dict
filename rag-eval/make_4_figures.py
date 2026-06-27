# -*- coding: utf-8 -*-
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch, Rectangle
from matplotlib.lines import Line2D

plt.rcParams['font.family'] = 'DejaVu Sans'
plt.rcParams['font.size'] = 12

C = {
    'blue':   '#1a6cb5',
    'purple': '#8e2fb0',
    'dgreen': '#1d5e2a',
    'orange': '#e0700f',
    'brick':  '#b5391a',
    'teal':   '#0f6f63',
    'magenta':'#9c1458',
    'green':  '#3a8f3a',
    'gray':   '#5a5a5a',
    'lgray':  '#d9d9d9',
}

def rbox(ax, x, y, w, h, color, title, sub='', tc='white', fs=13, subfs=10.5):
    p = FancyBboxPatch((x, y), w, h, boxstyle='round,pad=0.005,rounding_size=0.05',
                       linewidth=0, facecolor=color, zorder=2)
    ax.add_patch(p)
    if sub:
        ax.text(x+w/2, y+h*0.62, title, ha='center', va='center', color=tc,
                fontweight='bold', fontsize=fs, zorder=3)
        ax.text(x+w/2, y+h*0.30, sub, ha='center', va='center', color=tc,
                fontsize=subfs, zorder=3)
    else:
        ax.text(x+w/2, y+h/2, title, ha='center', va='center', color=tc,
                fontweight='bold', fontsize=fs, zorder=3)

def arrow(ax, x1, y1, x2, y2, color='#5a5a5a', lw=2.2, style='-|>', mut=18):
    a = FancyArrowPatch((x1, y1), (x2, y2), arrowstyle=style, mutation_scale=mut,
                        linewidth=lw, color=color, zorder=1,
                        shrinkA=2, shrinkB=2)
    ax.add_patch(a)

def caption(fig, text):
    fig.text(0.5, 0.035, text, ha='center', va='center', fontsize=11.5,
             color='#333333', style='italic')


# ============================================================
# FIGURE 1.8 — SignalR realtime
# ============================================================
def fig_signalr():
    fig, ax = plt.subplots(figsize=(11.5, 5.8))
    ax.set_xlim(0, 12.5); ax.set_ylim(0, 8.4); ax.axis('off')

    # Client (left) and Hub (right)
    rbox(ax, 0.5, 2.3, 2.9, 3.2, C['blue'], 'Client', 'Trình duyệt · Vue 3', fs=14, subfs=10)
    rbox(ax, 8.8, 2.3, 2.9, 3.2, C['dgreen'], 'SignalR Hub', 'ASP.NET Core', fs=14, subfs=10)

    # Three transport lanes between Client and Hub
    lanes = [
        (4.85, C['teal'],    'WebSockets',          'kết nối bền vững · hai chiều · ưu tiên', '<|-|>'),
        (3.85, C['orange'],  'Server-Sent Events',  'dự phòng · đẩy một chiều', '-|>'),
        (2.85, C['brick'],   'Long Polling',        'dự phòng cuối · request lặp', '-|>'),
    ]
    for y, col, name, desc, st in lanes:
        arrow(ax, 3.5, y, 8.7, y, color=col, lw=2.6, style=st, mut=16)
        ax.text(6.1, y+0.20, name, ha='center', va='bottom', color=col,
                fontweight='bold', fontsize=11.5)
        ax.text(6.1, y-0.24, desc, ha='center', va='top', color='#555555', fontsize=9)

    # Broadcast to other collaborators (top)
    rbox(ax, 8.0, 6.3, 4.0, 1.4, C['magenta'],
         'Thành viên khác', 'trong Workspace', fs=12, subfs=10.5)
    arrow(ax, 10.2, 5.5, 10.2, 6.3, color=C['magenta'], lw=2.2, style='<|-|>', mut=15)
    ax.text(9.85, 5.95, 'broadcast\nthời gian thực', ha='right', va='center',
            fontsize=8.6, color=C['magenta'])

    caption(fig, 'SignalR tự động chọn giao thức tốt nhất (WebSockets → SSE → Long Polling) và broadcast '
                 'sự kiện tới mọi thành viên trong Workspace với độ trễ thấp.')
    fig.savefig('fig_signalr.png', dpi=150, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    print('saved fig_signalr.png')


# ============================================================
# FIGURE 2.2 — RBAC 3 tiers
# ============================================================
def fig_rbac():
    fig, ax = plt.subplots(figsize=(13.2, 6.8))
    ax.set_xlim(0, 15); ax.set_ylim(0, 10); ax.axis('off')

    tiers = [
        (7.0, C['blue'],   'TẦNG HỆ THỐNG', 'System Level',
         ['System Admin', 'Moderator', 'Premium', 'System User']),
        (3.9, C['teal'],   'TẦNG TỔ CHỨC', 'Organization Level',
         ['Owner', 'Admin', 'Billing Manager', 'Member']),
        (0.8, C['dgreen'], 'TẦNG WORKSPACE', 'Workspace Level',
         ['Workspace Owner', 'Workspace Admin', 'Workspace Member', 'Viewer']),
    ]
    panel_x, panel_w, panel_h = 0.6, 14.0, 2.5
    for y, col, tname, ten, roles in tiers:
        # tier panel background
        p = FancyBboxPatch((panel_x, y), panel_w, panel_h,
                           boxstyle='round,pad=0.01,rounding_size=0.06',
                           linewidth=0, facecolor=col, alpha=0.13, zorder=1)
        ax.add_patch(p)
        ax.text(panel_x+0.35, y+panel_h*0.68, tname, ha='left', va='center',
                fontweight='bold', fontsize=12.5, color=col)
        ax.text(panel_x+0.35, y+panel_h*0.30, ten, ha='left', va='center',
                fontsize=9.5, color=col, style='italic')
        # role chips
        rw, rh, gap = 2.4, 1.1, 0.2
        x0 = panel_x + 3.5
        for i, r in enumerate(roles):
            rx = x0 + i*(rw+gap)
            rbox(ax, rx, y+panel_h/2-rh/2, rw, rh, col, r, '', fs=10)

    # hierarchy arrows (scope narrows downward)
    for y0, y1 in [(7.0, 6.4), (3.9, 3.3)]:
        arrow(ax, 2.6, y0, 2.6, y1, color=C['gray'], lw=2.0, style='-|>', mut=16)
    ax.text(0.18, 5.0, 'Phạm vi thu hẹp · phân quyền chi tiết dần',
            ha='center', va='center', fontsize=9.5, color=C['gray'], rotation=90)

    caption(fig, 'Phân quyền RBAC ba tầng (System · Organization · Workspace): một người dùng có thể giữ '
                 'nhiều vai trò theo từng ngữ cảnh, bảo đảm cô lập dữ liệu giữa các khách hàng.')
    fig.savefig('fig_rbac.png', dpi=150, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    print('saved fig_rbac.png')


# ============================================================
# FIGURE 3.1 — Text Chunking + Overlap (sliding window)
# ============================================================
def fig_chunking():
    fig, ax = plt.subplots(figsize=(11.5, 5.2))
    ax.set_xlim(0, 13); ax.set_ylim(0, 7.2); ax.axis('off')

    # long document bar on top
    ax.text(0.5, 6.7, 'Văn bản OCR sau làm sạch', ha='left', va='center',
            fontsize=11, color='#333', fontweight='bold')
    doc = Rectangle((0.5, 5.6), 12.0, 0.8, facecolor=C['lgray'], edgecolor='#aaaaaa', zorder=1)
    ax.add_patch(doc)

    # chunks as sliding windows
    chunk_len = 4.2
    overlap = 0.9
    step = chunk_len - overlap
    colors = [C['blue'], C['teal'], C['orange']]
    y_levels = [4.0, 2.7, 1.4]
    starts = [0.5 + i*step for i in range(3)]
    for i, (x0, y, col) in enumerate(zip(starts, y_levels, colors)):
        rbox(ax, x0, y, chunk_len, 0.95, col, f'Chunk {i+1}', '≤ 500 tokens', fs=12, subfs=9)
        # overlap shading with next chunk
        if i < 2:
            ov = Rectangle((x0+chunk_len-overlap, y), overlap, 0.95,
                           facecolor='white', alpha=0.32, edgecolor='none', zorder=3)
            ax.add_patch(ov)

    # overlap brackets between consecutive chunks
    for i in range(2):
        ox = starts[i] + chunk_len - overlap
        ax.annotate('', xy=(ox+overlap, 0.75), xytext=(ox, 0.75),
                    arrowprops=dict(arrowstyle='<|-|>', color=C['magenta'], lw=1.8))
        ax.text(ox+overlap/2, 0.45, 'Overlap\n50 tokens', ha='center', va='top',
                fontsize=9, color=C['magenta'], fontweight='bold')

    # dashed guides from doc to chunk1
    ax.add_line(Line2D([0.5, 0.5], [5.6, 4.95], color='#999', ls='--', lw=1))
    ax.add_line(Line2D([0.5+chunk_len, 0.5+chunk_len], [5.6, 4.95], color='#999', ls='--', lw=1))

    caption(fig, 'Văn bản được chia thành các khối ≤ 500 tokens, gối đầu 50 tokens giữa hai khối liền kề '
                 'để không đứt gãy ngữ cảnh ở ranh giới đoạn.')
    fig.savefig('fig_chunking.png', dpi=150, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    print('saved fig_chunking.png')


# ============================================================
# FIGURE 4.3 — Latency breakdown (Bảng 4.13)
# ============================================================
def fig_latency():
    stages = ['Embedding\nRequest', 'Qdrant\nVector Search', 'Prompt\nConstruction', 'Gemini API\nGeneration']
    ms = [150, 45, 15, 1900]
    pct = [7.1, 2.1, 0.8, 90.0]
    cols = [C['blue'], C['teal'], C['purple'], C['brick']]

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 4.8),
                                   gridspec_kw={'width_ratios': [2.1, 1.0]})

    # left: horizontal bars (ms)
    yp = range(len(stages))
    ax1.barh(yp, ms, color=cols, zorder=3, height=0.62)
    ax1.set_yticks(list(yp)); ax1.set_yticklabels(stages, fontsize=10.5)
    ax1.invert_yaxis()
    ax1.set_xlabel('Thời gian trung bình (ms)', fontsize=11)
    ax1.set_xlim(0, 2150)
    for i, (v, p) in enumerate(zip(ms, pct)):
        ax1.text(v+30, i, f'{v:,} ms  ({p}%)', va='center', ha='left',
                 fontsize=10.5, fontweight='bold', color='#222')
    ax1.grid(axis='x', alpha=0.25, zorder=0)
    for s in ['top', 'right']:
        ax1.spines[s].set_visible(False)

    # right: single 100% stacked bar (share)
    bottom = 0
    for s, p, c in zip(stages, pct, cols):
        ax2.bar(0, p, bottom=bottom, color=c, width=0.6, zorder=3)
        if p >= 3:
            ax2.text(0, bottom+p/2, f'{p}%', ha='center', va='center',
                     color='white', fontweight='bold', fontsize=11)
        bottom += p
    ax2.set_ylim(0, 100); ax2.set_xlim(-0.6, 0.6)
    ax2.set_xticks([]); ax2.set_ylabel('Tỷ trọng (%)', fontsize=11)
    ax2.set_title('Tổng ~2.110 ms', fontsize=11, color='#333')
    for s in ['top', 'right']:
        ax2.spines[s].set_visible(False)
    # legend for small slices
    handles = [Rectangle((0,0),1,1,color=c) for c in cols]
    ax2.legend(handles, [s.replace('\n',' ') for s in stages],
               loc='lower center', bbox_to_anchor=(0.5, -0.42),
               fontsize=8.6, frameon=False)

    fig.subplots_adjust(bottom=0.22, wspace=0.25)
    fig.savefig('fig_latency.png', dpi=150, bbox_inches='tight', facecolor='white')
    plt.close(fig)
    print('saved fig_latency.png')


if __name__ == '__main__':
    fig_signalr()
    fig_rbac()
    fig_chunking()
    fig_latency()
    print('ALL DONE')
