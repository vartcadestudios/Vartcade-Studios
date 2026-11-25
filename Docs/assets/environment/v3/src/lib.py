import math, json, os
INK="#1a0f0c"; CREAM="#f4e9d8"; GOLD="#e5a836"; BRICK="#b53b32"; TEAL="#4e7c86"; TEALDK="#3b5f67"; ORANGE="#d9822b"; DRED="#632b29"; CHAR="#13191d"
ROOT="/home/claude/envd/project"
FONTS='<link rel="preconnect" href="https://fonts.googleapis.com"><link href="https://fonts.googleapis.com/css2?family=Anton&amp;family=Roboto:wght@400;500;700&amp;display=swap" rel="stylesheet">'
def page(title, w, h, inner, css="", interactive=False):
    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>{title}</title>
<script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
{FONTS}
<style>body{{margin:0}}
{css}</style>
</helmet>
<div style="width: {w}px; height: {h}px; box-sizing: border-box; background: #241c1a; font-family: Roboto, sans-serif; color: {CREAM}; overflow: hidden; display: flex; flex-direction: column">
{inner}
</div>
</x-dc>
<script type="text/x-dc" data-dc-script data-props='{{"$preview":{{"width":{w},"height":{h}}}}}'>
class Component extends DCLogic {{
renderVals() {{
return {{}};
}}
}}
</script>
</body>
</html>
"""
def caption(code, title, notes, tags=""):
    return f"""<div style="height: 80px; flex: none; box-sizing: border-box; padding: 12px 24px; background: {CHAR}; display: flex; align-items: center; gap: 18px; border-top: 4px solid {GOLD}">
<div style="flex: none; padding: 8px 14px; border-radius: 8px; background: {GOLD}; color: {CHAR}; font: 400 26px/1 Anton, sans-serif; letter-spacing: 1px">{code}</div>
<div style="display: flex; flex-direction: column; gap: 4px; min-width: 0">
<div style="font: 400 22px/1 Anton, sans-serif; letter-spacing: .5px">{title}</div>
<div style="font: 400 13px/1.35 Roboto, sans-serif; color: #d8ccb8">{notes}</div>
</div>
<div style="margin-left: auto; flex: none; font: 500 12px/1.3 Roboto, sans-serif; color: #b9ad99; text-align: right; white-space: pre-line">{tags}</div>
</div>"""
def scene(svg_body, w=1280, h=720, label="scene"):
    return f'<svg role="img" aria-label="{label}" xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}" style="display: block; flex: none">{svg_body}</svg>'
def write(name, content):
    open(os.path.join(ROOT,name),"w").write(content)
S=f'stroke="{INK}" stroke-linejoin="round" stroke-linecap="round"'
def table(color="#4a3427", lip="#5e4333"):
    return f'<path d="M-40 720 L210 520 L1070 520 L1320 720Z" fill="{color}" {S} stroke-width="8"/><path d="M210 520 L1070 520 L1074 534 L206 534Z" fill="{lip}" {S} stroke-width="6"/>'
def bulb(x,y,cord=120,glow="#ffd27a",on=True):
    g=f'<circle cx="{x}" cy="{y+cord+28}" r="150" fill="url(#glow)"/>' if on else ""
    return f'{g}<line x1="{x}" y1="{y}" x2="{x}" y2="{y+cord}" stroke="{INK}" stroke-width="4"/><path d="M{x-40} {y+cord+22} Q{x} {y+cord-22} {x+40} {y+cord+22}Z" fill="#2d3a3e" {S} stroke-width="6"/><circle cx="{x}" cy="{y+cord+30}" r="12" fill="{glow if on else "#3a3a3a"}" {S} stroke-width="4"/>'
GLOW='<defs><radialGradient id="glow"><stop offset="0" stop-color="#ffd27a" stop-opacity=".45"/><stop offset="1" stop-color="#ffd27a" stop-opacity="0"/></radialGradient><radialGradient id="vig" cx=".5" cy=".45" r=".75"><stop offset=".55" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".7"/></radialGradient><linearGradient id="dusk" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#3b2a4a"/><stop offset=".6" stop-color="#c85a3a"/><stop offset="1" stop-color="#f29a4a"/></linearGradient></defs>'
def bust(x,y,s=1.0,shirt=TEAL,skin="#c68a5e",hair="#3a2618",num="02",eyes="open"):
    e = f'<ellipse cx="-13" cy="-62" rx="4" ry="5" fill="{INK}"/><ellipse cx="13" cy="-62" rx="4" ry="5" fill="{INK}"/>' if eyes=="open" else f'<path d="M-19 -62 h12 M7 -62 h12" stroke="{INK}" stroke-width="4"/>'
    return f'''<g transform="translate({x} {y}) scale({s})"><path d="M-62 60 Q-60 -2 -26 -8 L26 -8 Q60 -2 62 60Z" fill="{shirt}" {S} stroke-width="6"/><text x="-40" y="30" font-family="Anton" font-size="18" fill="{BRICK}">{num}</text>
<circle cx="0" cy="-58" r="40" fill="{skin}" {S} stroke-width="6"/><path d="M-42 -62 Q-40 -108 0 -104 Q42 -108 42 -62 Q30 -86 0 -84 Q-26 -86 -42 -62Z" fill="{hair}" {S} stroke-width="6"/>{e}<path d="M-10 -36 q10 6 20 0" fill="none" stroke="{INK}" stroke-width="4"/></g>'''
