#!/usr/bin/env python3
"""リリースごとのダウンロード数を、公開日順の棒グラフ(SVG)にする。Mac版=青、Windows版=オレンジ。"""
import json, os, subprocess, sys

repo = os.environ.get("GITHUB_REPOSITORY", "mksmkss/PhotoForumPPTX")
out = sys.argv[1] if len(sys.argv) > 1 else "docs/downloads.svg"
raw = subprocess.run(["gh", "api", f"repos/{repo}/releases", "--paginate"], check=True, capture_output=True, text=True).stdout
# --paginate は配列を連結して出力するので、"][" を区切りとして分けて読む
releases = []
dec = json.JSONDecoder(); i = 0
while i < len(raw):
    while i < len(raw) and raw[i].isspace(): i += 1
    if i >= len(raw): break
    obj, i = dec.raw_decode(raw, i)
    releases += obj if isinstance(obj, list) else [obj]

rows = []
for r in releases:
    if r.get("draft"): continue
    n = sum(a["download_count"] for a in r["assets"] if a["name"].endswith(".zip"))
    rows.append((r["published_at"], r["tag_name"], n, r["tag_name"].startswith("win-")))
rows.sort()
total = sum(r[2] for r in rows)

W, H, L, B, T = max(640, 40 + 34 * len(rows) + 40), 310, 40, 80, 50
top = max([r[2] for r in rows] + [1])
step = 1 if top <= 10 else (2 if top <= 20 else 5)
ymax = ((top + step - 1) // step) * step
ph = H - B - T
bw = (W - L - 20) / max(1, len(rows))
s = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}" font-family="-apple-system,Helvetica,Arial,sans-serif" font-size="11">',
     '<style>.t{fill:#555}.g{stroke:#8884;stroke-width:1}@media (prefers-color-scheme:dark){.t{fill:#bbb}}</style>',
     f'<text x="{L}" y="22" font-size="14" font-weight="600" class="t">Downloads by release (total {total})</text>',
     f'<rect x="{W-190}" y="12" width="10" height="10" fill="#3b82f6"/><text x="{W-175}" y="21" class="t">Mac</text>',
     f'<rect x="{W-130}" y="12" width="10" height="10" fill="#f97316"/><text x="{W-115}" y="21" class="t">Windows</text>']
for v in range(0, ymax + 1, step):
    y = T + ph - ph * v / ymax
    s.append(f'<line class="g" x1="{L}" x2="{W-10}" y1="{y:.1f}" y2="{y:.1f}"/><text class="t" x="{L-6}" y="{y+4:.1f}" text-anchor="end">{v}</text>')
for k, (_, tag, n, win) in enumerate(rows):
    x = L + 6 + k * bw; h = ph * n / ymax; y = T + ph - h
    s.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{bw-10:.1f}" height="{h:.1f}" rx="2" fill="{"#f97316" if win else "#3b82f6"}"><title>{tag}: {n}</title></rect>')
    s.append(f'<text class="t" x="{x+(bw-10)/2:.1f}" y="{y-4:.1f}" text-anchor="middle">{n}</text>')
    s.append(f'<text class="t" transform="translate({x+(bw-10)/2+4:.1f},{T+ph+12}) rotate(45)">{tag}</text>')
s.append('</svg>')
os.makedirs(os.path.dirname(out) or ".", exist_ok=True)
open(out, "w").write("\n".join(s) + "\n")
print(f"{len(rows)} releases, total {total}")
