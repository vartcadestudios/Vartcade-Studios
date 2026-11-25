"""Final room v3: R-B Filipino home bunker + R-A shelves/poster/pipes + R-C portrait/drawings/wallpaper.
Layers at 1920x1080 (y down). Table edge at y=556, seats at x 470 / 960 / 1440."""
import math, json, sys
import numpy as np
from PIL import Image
from style import *
from style import P as C

OUT = "/home/claude/envd/project/v3/"
LAY = "/home/claude/envgen/v3/layers/"
import os; os.makedirs(LAY, exist_ok=True)
EDGE = 650
SEATS = (("c1", 520, True), ("c5", 960, False), ("c4", 1400, False))
CHAR_H = 400
WIN = (1080, 118, 260, 220)  # window opening x, y, w, h

# ---------------------------------------------------------------- wall
def wall_layer(age):
    rnd(21)
    b = f'<rect width="{W}" height="{H}" fill="{C["wood"]}"/>'
    # planks of uneven width with knots and grain
    x = 0
    while x < W:
        pw = R.choice([84, 96, 110, 76, 120])
        tone = R.choice([C["wood"], "#563a2a", "#61412f", "#523626"])
        b += f'<rect x="{x}" y="70" width="{pw}" height="{H}" fill="{tone}"/>'
        b += line([(x, 70), (x + jit(2), 420)], 2.2, 1.0)
        for g in range(3):
            gx = x + R.uniform(12, pw - 12)
            b += line([(gx, 90 + R.uniform(0, 80)), (gx + jit(4), 200 + R.uniform(0, 200))], 0.9, 2.5, op=0.35)
        if R.random() < 0.5:
            kx, ky = x + R.uniform(20, pw - 20), R.uniform(120, 380)
            b += f'<ellipse cx="{kx:.0f}" cy="{ky:.0f}" rx="7" ry="11" fill="#3a2519" opacity=".6"/>' + ink(rough(ellipse_pts(kx, ky, 9, 14, 12)), 0.9, 0.5)
        if R.random() < 0.35:  # nail heads
            b += f'<circle cx="{x+8}" cy="{R.uniform(100,400):.0f}" r="2.4" fill="#2a1c14"/>'
        x += pw
    # lower wainscot: chipped teal-green paint over concrete
    b += shape([(0, 412), (W, 406), (W, H), (0, H)], "#40605a", 2.4, 2.0)
    b += line([(0, 424), (W, 418)], 1.4, 1.5, op=0.5)
    rnd(5)
    for _ in range(26):  # chipped paint showing concrete
        cx, cy = R.uniform(0, W), R.uniform(430, 560)
        pts = ellipse_pts(cx, cy, R.uniform(14, 46), R.uniform(6, 16), 10)
        pts = [(px_ + jit(5), py_ + jit(4)) for px_, py_ in pts]
        b += shape(pts, C["concrete"], 1.2, 1.5)
    # ceiling beam + pipes (from R-A), with a valve and a drip
    b += shape([(0, 0), (W, 0), (W, 74), (0, 80)], "#2b1d16", 2.6, 1.5)
    b += line([(0, 52), (W, 48)], 1.2, 1.5, op=0.4)
    b += shape([(0, 82), (W, 76), (W, 98), (0, 104)], "#6a706e", 2.4, 1.0)   # main pipe
    for xx in [230, 640, 1300, 1700]:
        b += box(xx, 74, 22, 34, "#555b5a", 2.0, 1.0)
    b += blob(1520, 88, 20, 20, C["rust"], 2.2) + line([(1500, 88), (1540, 88)], 2.0) + line([(1520, 68), (1520, 108)], 2.0)
    b += shape([(1860, 96), (1880, 96), (1880, 300), (1860, 300)], "#6a706e", 2.2, 1.0)  # down-pipe at right
    # peeling wallpaper patch (R-C) upper right
    b += shape([(1650, 104), (1910, 102), (1906, 260), (1880, 300), (1840, 262), (1800, 290), (1760, 250), (1700, 268), (1652, 230)], "#6d7b5c", 2.2, 2.0)
    for i in range(7):
        for j in range(4):
            cx, cy = 1675 + i * 34, 125 + j * 34
            if cy < 250: b += f'<circle cx="{cx}" cy="{cy}" r="5" fill="#879571" opacity=".8"/>'
    b += shape([(1760, 250), (1800, 290), (1786, 340), (1770, 300)], "#93a07c", 1.8, 1.0)  # curled strip
    # window opening is cut out later; frame drawn in decor
    return b

# ---------------------------------------------------------------- sky inside the window (per day / disaster)
def sky_layer(day=1, sundown=10, night=False, disaster=None):
    x, y, w, h = WIN
    t = (day - 1) / max(1, sundown - 1)
    def mix(a, b_, k):
        a = [int(a[i:i + 2], 16) for i in (1, 3, 5)]; b2 = [int(b_[i:i + 2], 16) for i in (1, 3, 5)]
        return "#" + "".join(f"{round(a[i]+(b2[i]-a[i])*k):02x}" for i in range(3))
    top, mid, bot = mix("#6b334e", "#06070e", t), mix("#c85a3a", "#16101e", t), mix("#f29a4a", "#3a1414", t)
    if night: top, mid, bot = "#03040a", "#0a0e1c", "#14182c"
    if disaster == "peaceful": top, mid, bot = "#5e86a8", "#e9b46a", "#f6d58a"
    if disaster == "purge": top, mid, bot = "#1a0508", "#5a0e10", "#b0201a"
    if disaster == "tornado": top, mid, bot = "#2a3330", "#46524a", "#6c7a64"
    if disaster == "doomsday": top, mid, bot = "#1a0604", "#6a1808", "#e0541a"
    if disaster == "outbreak": top, mid, bot = "#1c2416", "#3e5a2a", "#8aa64a"
    if disaster == "blackout": top, mid, bot = "#04050a", "#0b0e18", "#161a28"
    d = f'<linearGradient id="sk" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{top}"/><stop offset=".6" stop-color="{mid}"/><stop offset="1" stop-color="{bot}"/></linearGradient>'
    b = f'<rect x="{x-20}" y="{y-20}" width="{w+40}" height="{h+40}" fill="url(#sk)"/>'
    sun_y = y + 40 + t * (h - 30)
    if not night and disaster not in ("blackout", "tornado", "outbreak", "purge"):
        b += f'<circle cx="{x+w*0.62}" cy="{sun_y:.0f}" r="34" fill="#f6a24a" opacity=".95"/>'
    if night or disaster == "blackout":
        rnd(8)
        b += f'<circle cx="{x+60}" cy="{y+50}" r="16" fill="#e8e2cc"/><circle cx="{x+68}" cy="{y+45}" r="14" fill="{top}"/>'
        for _ in range(16):
            sx, sy = x + R.uniform(0, w), y + R.uniform(0, h * 0.6)
            b += f'<rect class="tw" x="{sx:.0f}" y="{sy:.0f}" width="3" height="3" fill="#efe6cf" shape-rendering="crispEdges"/>'
    # rooftops and a far tree line
    b += shape([(x - 20, y + h - 40), (x + 30, y + h - 62), (x + 70, y + h - 46), (x + 120, y + h - 70), (x + 170, y + h - 52), (x + 210, y + h - 76), (x + w + 20, y + h - 50), (x + w + 20, y + h + 20), (x - 20, y + h + 20)], "#160e10", 1.6, 1.5)
    # power line with crows (A04 lifts one off)
    b += line([(x - 20, y + 70), (x + w * 0.5, y + 92), (x + w + 20, y + 74)], 1.4, 0.5)
    if disaster not in ("tornado",):
        for i, cx in enumerate([x + 52, x + 92, x + 186]):
            cy = y + 78 + (8 if 60 < cx - x < 200 else 4)
            cls = ' class="crow"' if i == 1 else ""
            b += f'<g{cls}>' + shape([(cx - 9, cy), (cx - 3, cy - 12), (cx + 6, cy - 14), (cx + 9, cy - 6), (cx + 4, cy)], "#120c0c", 1.0, 0.6) + f'<path d="M{cx+6} {cy-12} l7 -1 l-6 4z" fill="#3a2a20"/></g>'
    if disaster == "peaceful":
        for bx, by in [(x + 40, y + 40), (x + 70, y + 30), (x + 200, y + 60)]:
            b += line([(bx, by), (bx + 6, by - 5), (bx + 12, by)], 1.6) + line([(bx + 12, by), (bx + 18, by - 5), (bx + 24, by)], 1.6)
    if disaster == "tornado":
        b += shape([(x + 140, y), (x + 210, y), (x + 186, y + 60), (x + 176, y + 110), (x + 168, y + h - 50), (x + 160, y + h - 50), (x + 150, y + 110), (x + 144, y + 60)], "#26302c", 1.4, 3.0, op=0.85)
        rnd(4)
        for _ in range(26):
            rx_, ry_ = x + R.uniform(0, w), y + R.uniform(0, h)
            b += f'<rect class="rain" x="{rx_:.0f}" y="{ry_:.0f}" width="2" height="12" fill="#9ab4c0" opacity=".8" shape-rendering="crispEdges"/>'
    if disaster == "doomsday":
        for mx, my in [(x + 40, y + 30), (x + 150, y + 10), (x + 220, y + 60)]:
            b += line([(mx, my), (mx - 30, my - 30)], 4, 0.5, "#f6c04a", 0.7) + f'<circle cx="{mx}" cy="{my}" r="6" fill="#ffe08a"/>'
        b += shape([(x + 90, y + h - 60), (x + 96, y + 100), (x + 70, y + 98), (x + 70, y + 70), (x + 110, y + 56), (x + 150, y + 70), (x + 150, y + 98), (x + 124, y + 100), (x + 130, y + h - 60)], "#c9a880", 1.2, 1.5, op=0.65)
    if disaster == "outbreak":
        rnd(6)
        for i in range(6):
            b += blob(x + R.uniform(0, w), y + h - R.uniform(20, 90), R.uniform(40, 90), R.uniform(16, 30), "#a8c46a", 0, 16, 3).replace('opacity="1.0"', 'opacity=".45"')
    if disaster == "purge":
        b += f'<circle class="siren" cx="{x+w*0.3}" cy="{y+h-60}" r="60" fill="#ff3a2a" opacity=".55"/>'
    return b, d

# ---------------------------------------------------------------- decor (frames, posters, shelves...)
def poster(x, y, age):
    b = shape([(x, y), (x + 200, y + 6), (x + 196, y + 260), (x - 4, y + 254)], "#d8c79c", 2.4, 1.5)
    b += shape([(x + 10, y + 14), (x + 188, y + 18), (x + 186, y + 70), (x + 8, y + 66)], C["brick"], 1.8, 1.0)
    b += text(x + 98, y + 54, "MANATILI SA LOOB", 26, "#f0d88a", rot=1)
    # rubber-hose sun mascot pointing down to the bunker (Silong brand)
    b += blob(x + 98, y + 140, 44, 44, "#f2c94c", 2.4)
    for i in range(10):
        a = i * 36
        b += line([(x + 98 + 52 * math.cos(math.radians(a)), y + 140 + 52 * math.sin(math.radians(a))),
                   (x + 98 + 66 * math.cos(math.radians(a)), y + 140 + 66 * math.sin(math.radians(a)))], 2.2, 0.6, "#c48a1c")
    b += f'<ellipse cx="{x+84}" cy="{y+132}" rx="5" ry="9" fill="{INK}"/><ellipse cx="{x+110}" cy="{y+132}" rx="5" ry="9" fill="{INK}"/>'
    b += line([(x + 80, y + 156), (x + 98, y + 166), (x + 116, y + 156)], 2.4, 0.5)
    b += text(x + 92, y + 226, "SILONG SHELTERS", 19, C["teal_dk"])
    b += text(x + 98, y + 246, "PANG-SAMPUNG ARAW NA HANDA", 10, "#5a4a3a", font="Roboto", weight="bold")
    b += f'<rect x="{x+86}" y="{y-6}" width="30" height="14" fill="#cfc6a0" opacity=".8" transform="rotate(-6 {x+100} {y})"/>'  # tape
    b += shape([(x + 160, y + 200), (x + 196, y + 196), (x + 196, y + 262), (x + 150, y + 256)], "#bfae86", 1.4, 1.2)  # torn corner
    return b

def portrait(x, y):
    b = f'<g transform="rotate(-6 {x+70} {y+90})">'
    b += box(x, y, 140, 180, "#4a3022", 2.6, 1.2) + box(x + 14, y + 14, 112, 152, "#2a2630", 1.8, 0.8)
    b += blob(x + 70, y + 84, 32, 40, "#6d6470", 1.6)
    b += f'<path d="M{x+30} {y+166} Q{x+70} {y+110} {x+110} {y+166}Z" fill="#3d3a46"/>'
    b += f'<g class="eyes"><ellipse cx="{x+58}" cy="{y+80}" rx="7" ry="5" fill="#efe6cf"/><ellipse cx="{x+82}" cy="{y+80}" rx="7" ry="5" fill="#efe6cf"/>'
    b += f'<circle class="pupil" cx="{x+60}" cy="{y+81}" r="2.6" fill="{INK}"/><circle class="pupil" cx="{x+84}" cy="{y+81}" r="2.6" fill="{INK}"/></g>'
    b += line([(x + 70, y - 30), (x + 20, y + 2)], 1.2) + line([(x + 70, y - 30), (x + 120, y + 2)], 1.2) + f'<circle cx="{x+70}" cy="{y-30}" r="3" fill="#2a1c14"/>'
    return b + "</g>"

def drawings(x, y):
    b = f'<g transform="rotate(5 {x} {y})">' + box(x, y, 110, 84, "#efe6cf", 1.6, 1.0)
    b += line([(x + 14, y + 70), (x + 30, y + 40), (x + 46, y + 70)], 2.4, 1.0, "#a8392f") + line([(x + 58, y + 70), (x + 74, y + 30), (x + 92, y + 70)], 2.4, 1.0, "#a8392f")
    b += f'<circle cx="{x+86}" cy="{y+20}" r="10" fill="none" stroke="#e0a23a" stroke-width="2.6"/>'
    b += f'<rect x="{x+40}" y="{y-6}" width="26" height="12" fill="#cfc6a0" opacity=".8"/></g>'
    b += f'<g transform="rotate(-8 {x+60} {y+120})">' + box(x + 10, y + 100, 96, 74, "#e3d6b8", 1.6, 1.0)
    b += blob(x + 40, y + 140, 14, 16, "#6aa0c8", 1.4) + blob(x + 76, y + 140, 10, 18, "#c96aa0", 1.4)
    b += line([(x + 30, y + 162), (x + 90, y + 162)], 2, 1.0, "#5d7146") + f'<rect x="{x+48}" y="{y+94}" width="22" height="12" fill="#cfc6a0" opacity=".8"/></g>'
    return b

NO_NUMBERS = False   # True when exporting for Unity: the game writes the day numbers itself

def calendar(x, y, days_left):
    b = box(x, y, 128, 168, C["cream"], 2.4, 1.0) + box(x, y, 128, 44, C["brick"], 2.4, 1.0)
    b += text(x + 64, y + 30, "BOTIKA SILONG", 17, "#f4d65a")
    if not NO_NUMBERS: b += text(x + 64, y + 128, str(days_left), 70, "#5a2420")
    b += text(x + 64, y + 156, "ARAW NA LANG", 13, "#5a2420", font="Roboto", weight="bold")
    for i in range(5): b += f'<circle cx="{x+16+i*24}" cy="{y+4}" r="3.5" fill="#2a1c14"/>'
    b += line([(x + 64, y - 26), (x + 64, y)], 1.4) + f'<circle cx="{x+64}" cy="{y-28}" r="3" fill="#2a1c14"/>'
    return b

def clock(x, y):
    b = blob(x, y, 46, 46, C["cream"], 2.6) + blob(x, y, 38, 38, "#efe3c8", 1.2)
    for i in range(12):
        a = math.radians(i * 30); b += line([(x + 32 * math.sin(a), y - 32 * math.cos(a)), (x + 36 * math.sin(a), y - 36 * math.cos(a))], 1.6, 0.2)
    b += f'<g class="clockhand" style="transform-origin:{x}px {y}px">' + line([(x, y), (x + 20, y + 10)], 3, 0.3) + "</g>" + line([(x, y), (x - 2, y - 28)], 3, 0.3)
    b += f'<circle cx="{x}" cy="{y}" r="4" fill="{C["brick"]}"/>'
    return b

def bawal(x, y):
    b = shape([(x, y), (x + 270, y - 6), (x + 266, y + 64), (x + 4, y + 70)], "#c9a36a", 2.4, 1.5)
    b += text(x + 135, y + 46, "BAWAL ANG PANG-TATLO!", 28, "#a82018", rot=-1)
    b += line([(x + 30, y - 4), (x + 60, y - 50), (x + 90, y - 4)], 1.2, 0.5) + f'<circle cx="{x+60}" cy="{y-50}" r="3" fill="#2a1c14"/>'
    return b

def window_frame(age, disaster=None):
    x, y, w, h = WIN
    b = ""
    # capiz panes
    for i in range(6):
        for j in range(5):
            px_, py_ = x + 4 + i * (w - 8) / 6, y + 4 + j * (h - 8) / 5
            b += f'<rect x="{px_+2:.0f}" y="{py_+2:.0f}" width="{(w-8)/6-4:.0f}" height="{(h-8)/5-4:.0f}" fill="#f2e7cf" opacity=".22"/>'
    for i in range(1, 6): b += line([(x + 4 + i * (w - 8) / 6, y), (x + 4 + i * (w - 8) / 6, y + h)], 2.0, 0.6, "#3e2a1f")
    for j in range(1, 5): b += line([(x, y + 4 + j * (h - 8) / 5), (x + w, y + 4 + j * (h - 8) / 5)], 2.0, 0.6, "#3e2a1f")
    # broken panes (age): dark holes
    if age > 0.5:
        b += f'<rect x="{x+4+3*(w-8)/6+2:.0f}" y="{y+4+2*(h-8)/5+2:.0f}" width="{(w-8)/6-4:.0f}" height="{(h-8)/5-4:.0f}" fill="#000" opacity=".25"/>'
    b += shape([(x - 18, y - 18), (x + w + 18, y - 18), (x + w + 18, y + h + 18), (x - 18, y + h + 18)], "none", 3.0, 1.2, extra='fill-rule="evenodd"')
    b += f'<path d="M{x-18} {y-18} h{w+36} v{h+36} h{-w-36}Z M{x} {y} v{h} h{w} v{-h}Z" fill="{C["wood_dk"]}" fill-rule="evenodd"/>' + ink(rough(rect(x - 18, y - 18, w + 36, h + 36)), 2.6) + ink(rough(rect(x, y, w, h)), 2.0)
    b += box(x - 30, y + h + 12, w + 60, 14, C["wood_lt"], 2.2, 1.0)  # sill
    if disaster == "tornado":
        b += line([(x - 6, y - 6), (x + w + 6, y + h + 6)], 10, 0.8, "#cdbf9a", 0.9) + line([(x + w + 6, y - 6), (x - 6, y + h + 6)], 10, 0.8, "#cdbf9a", 0.9)
    if disaster == "outbreak":
        b += f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#dfe8d6" opacity=".28"/>' + line([(x - 10, y + 10), (x + w + 10, y + 6)], 9, 0.6, "#e6c43a", 0.95) + line([(x - 10, y + h - 10), (x + w + 10, y + h - 4)], 9, 0.6, "#e6c43a", 0.95)
    if disaster == "trading_only":
        b += shape([(x + 40, y + h + 30), (x + 220, y + h + 26), (x + 222, y + h + 80), (x + 38, y + h + 84)], "#e9dcc2", 2.2, 1.2)
        b += text(x + 130, y + h + 64, "PALIT-PALIT LANG", 22, C["brick"])
    if disaster == "peaceful":
        b += box(x + w - 60, y + h - 20, 34, 32, "#7d8584", 2.0, 1.0) + line([(x + w - 43, y + h - 20), (x + w - 48, y + h - 60)], 2.4, 1.0, "#3d6a2e")
        b += blob(x + w - 50, y + h - 66, 12, 12, "#f2c94c", 1.8) + blob(x + w - 50, y + h - 66, 5, 5, "#7a4a1a", 1.0)
    return b

def tuna(x, y, s=1.0):
    w, h = 54 * s, 34 * s
    b = shape([(x, y + 6 * s), (x + w, y + 6 * s), (x + w, y + h), (x, y + h)], C["tuna"], 2.0, 0.8)
    b += f'<ellipse cx="{x+w/2}" cy="{y+6*s}" rx="{w/2}" ry="{6*s}" fill="#a6aab0"/>' + ink(rough(ellipse_pts(x + w / 2, y + 6 * s, w / 2, 6 * s, 14)), 1.6, 1.0)
    b += text(x + w / 2, y + 22 * s, "MILLENIUM", 9 * s, "#e8e2f2", font="Bitter", weight="bold")
    b += text(x + w / 2, y + 31 * s, "tuna", 8 * s, "#c9cdf2", font="Comic Relief")
    return b

def galon(x, y):
    b = shape([(x, y + 14), (x + 70, y + 14), (x + 72, y + 110), (x - 2, y + 110)], C["pink"], 2.4, 1.2)
    b += box(x + 24, y, 22, 16, C["pink_dk"], 2.0, 0.8) + shape([(x + 6, y + 40), (x + 64, y + 40), (x + 64, y + 84), (x + 6, y + 84)], "#e7a24a", 1.6, 1.0)
    b += shape([(x + 6, y + 84), (x + 22, y + 58), (x + 34, y + 70), (x + 46, y + 52), (x + 64, y + 84)], "#2a1c24", 1.0, 1.0)
    b += text(x + 36, y + 80, "peak", 14, "#f0c0dc", font="Comic Relief")
    return b

def aid(x, y):
    b = shape([(x, y + 60), (x + 4, y + 16), (x + 30, y + 4), (x + 80, y + 6), (x + 104, y + 20), (x + 106, y + 60)], C["aid"], 2.4, 1.6)
    b += f'<rect x="{x+46}" y="{y+16}" width="14" height="30" fill="{INK}"/><rect x="{x+38}" y="{y+24}" width="30" height="14" fill="{INK}"/>'
    b += text(x + 53, y + 58, "FIRST AID", 12, C["brick"], font="Bitter", weight="bold")
    return b

def books(x, y):
    b = shape([(x, y + 20), (x + 120, y + 16), (x + 116, y + 74), (x + 4, y + 76)], "#3e3048", 2.4, 1.2)
    b += shape([(x + 12, y - 30), (x + 52, y - 34), (x + 56, y + 30), (x + 14, y + 32)], "#a8392f", 2.0, 1.0)
    b += text(x + 34, y - 12, "HOW NOT", 8, "#f4e0a0", font="Bitter", weight="bold") + blob(x + 34, y + 6, 10, 10, "#7f9a4a", 1.2)
    for i in range(4): b += box(x + 62 + i * 12, y - 16 + (i % 2) * 4, 10, 40, ["#556", "#6a5", "#855", "#567"][i], 1.4, 0.6)
    b += f'<rect x="{x+40}" y="{y+40}" width="40" height="10" rx="5" fill="#1e1411"/>'
    return b

def bigas(x, y):
    b = shape([(x, y + 92), (x - 4, y + 30), (x + 8, y), (x + 40, y - 6), (x + 76, y + 2), (x + 86, y + 30), (x + 84, y + 92)], "#e3d6b8", 2.4, 1.8)
    b += text(x + 42, y + 56, "BIGAS", 18, "#2f6b3a") + line([(x + 8, y + 4), (x + 30, y + 14), (x + 52, y + 6), (x + 76, y + 10)], 1.4, 1.0)
    return b

def shelf(stage, age):
    """Steel shelf (R-A) with the team's supplies (labels after the cards). stage 0 full, 1 half, 2 almost empty."""
    x0, x1 = 40, 400
    b = ""
    for xx in (x0, x1 - 16):
        b += box(xx, 150, 16, EDGE - 150, C["metal_dk"], 2.4, 1.0)
    levels = [200, 304, 408, 512]
    for yy in levels:
        b += box(x0 - 6, yy, x1 - x0 + 12, 14, C["metal"], 2.4, 1.0)
        b += hatch(x0 + 16, yy + 14, x1 - x0 - 32, 26, 6, 60, 0.25, 1.0)
    # rust drips on the uprights
    for xx in (x0 + 8, x1 - 8):
        b += line([(xx, 160), (xx + 1, 240)], 3, 0.5, C["rust"], 0.7)
    # top: radio with fine pixel meter (X07) + candle (X04)
    b += box(70, 150, 120, 50, C["teal_dk"], 2.4, 1.0) + blob(98, 175, 16, 16, "#2a1c14", 1.8)
    b += f'<rect x="124" y="160" width="56" height="28" fill="#140e0c"/>'
    for i in range(6):
        col = ["#9fd15a", "#9fd15a", "#9fd15a", "#e0a23a", "#e0a23a", "#c03a2a"][i]
        b += f'<rect class="vu" style="animation-delay:-{i*0.17:.2f}s" x="{127+i*9}" y="{164}" width="6" height="21" fill="{col}" shape-rendering="crispEdges"/>'
    b += line([(176, 150), (206, 104)], 2.0, 0.5)
    b += box(300, 152, 20, 48, C["cream"], 2.0, 0.6) + line([(310, 152), (310, 144)], 1.6, 0.2)
    b += '<g class="flame">' + px(["..a..", ".aba.", ".aba.", "abcba", ".bcb."], {'a': '#f6d65a', 'b': '#e0a23a', 'c': '#fff4d0'}, 302, 126, 3) + '</g>'
    # level 1: tuna cans (X02 pixel fish on some)
    if stage < 2:
        for i, xx in enumerate([60, 118, 176, 234][: 4 - stage * 2]):
            b += tuna(xx, 264)
        b += tuna(89, 232) if stage == 0 else ""
    else:
        b += tuna(70, 264)
    # level 2: galon + first aid
    if stage < 2: b += galon(70, 302 - 8 + 0)[:0] + galon(64, 196 + 100)
    if stage == 0: b += aid(170, 340)
    if stage == 1: b += aid(200, 344)
    # level 3: bigas + books
    if stage == 0: b += bigas(70, 412) + books(200, 432)
    if stage == 1: b += books(150, 432)
    if stage == 2:
        b += f'<g transform="rotate(80 230 480)">' + tuna(210, 470, 0.9) + "</g>"  # a fallen empty can
        b += shape([(240, 496), (330, 494), (330, 508), (240, 510)], "#bfae86", 1.4, 1.0)  # torn label
    return b

def spider(x, y, flip=1, web_only=False):
    """Corner web (top-left) with a spider that lowers on its thread (A-loop)."""
    b = ""
    for a in range(0, 91, 15):
        r = math.radians(a)
        b += line([(x, y), (x + flip * 190 * math.cos(r), y + 190 * math.sin(r))], 1.3, 0.5, "#e2dbcb", 0.8)
    for k in range(1, 6):
        pts = [(x + flip * 34 * k * math.cos(math.radians(a)) + jit(2), y + 34 * k * math.sin(math.radians(a)) + jit(2)) for a in range(0, 91, 15)]
        b += ink(rough(pts, False, 1.2), 1.2, 0.8, "#e2dbcb")
    if web_only: return b
    return b + spider_only(x, y, flip)

def spider_only(x, y, flip=1):
    b = ""
    sx = x + flip * 80
    b += f'<g class="spider"><line x1="{sx}" y1="{y+60}" x2="{sx}" y2="{y+150}" stroke="#e2dbcb" stroke-width="1.1" opacity=".8"/>'
    b += blob(sx, y + 158, 8, 9, "#120c0a", 1.0) + blob(sx, y + 147, 5, 5, "#120c0a", 0.8)
    for s_ in (-1, 1):
        for k in range(4):
            b += line([(sx, y + 154 + k * 3), (sx + s_ * 13, y + 146 + k * 5), (sx + s_ * 18, y + 158 + k * 5)], 1.1, 0.2)
    return b + "</g>"

def fan_blades(x, y):
    b = ""
    for a in (0, 120, 240):
        b += f'<g transform="rotate({a} {x} {y})">' + blob(x, y - 30, 14, 26, C["orange"], 1.8) + "</g>"
    return b + blob(x, y, 9, 9, "#9aa1a5", 1.6)

def fan(x, y, blades=True):
    b = line([(x, y + 40), (x, y + 196)], 4, 0.5) + blob(x, y + 200, 34, 8, "#7d8584", 2.0)
    b += blob(x, y, 62, 62, "#3a585e", 2.4).replace('opacity="1.0"', 'opacity=".35"')
    if blades: b += fan_blades(x, y)
    for k in range(8):
        a = math.radians(k * 22.5); b += line([(x + 62 * math.cos(a), y + 62 * math.sin(a)), (x - 62 * math.cos(a), y - 62 * math.sin(a))], 0.8, 0.3, op=0.45)
    b += ink(rough(ellipse_pts(x, y, 62, 62, 28)), 2.4)
    return b

def tv(x, y):
    b = box(x - 10, y + 90, 150, 66, "#6b4a30", 2.4, 1.2) + text(x + 65, y + 130, "SILONG", 16, "#3e2a1f")
    b += shape([(x, y), (x + 130, y + 2), (x + 128, y + 92), (x + 2, y + 90)], "#5b4636", 2.4, 1.0)
    b += f'<rect x="{x+12}" y="{y+12}" width="88" height="66" fill="#1c2b28"/>'
    rnd(31)
    for _ in range(60):
        b += f'<rect class="st" style="animation-delay:-{R.random():.2f}s" x="{x+12+R.randrange(0,29)*3}" y="{y+12+R.randrange(0,22)*3}" width="3" height="3" fill="#c9d4cc" shape-rendering="crispEdges"/>'
    b += f'<clipPath id="tvc"><rect x="{x+12}" y="{y+64}" width="88" height="14"/></clipPath><g clip-path="url(#tvc)"><g class="scroll">' + ptext("SUNDOWN SA 3 ARAW", "#f4d65a", x + 12, y + 66, 2) + "</g></g>"
    b += blob(x + 114, y + 30, 6, 6, "#2a1c14", 1.0) + blob(x + 114, y + 56, 6, 6, "#2a1c14", 1.0)
    b += line([(x + 40, y), (x + 20, y - 40)], 1.6, 0.3) + line([(x + 80, y), (x + 104, y - 44)], 1.6, 0.3)
    return b

def chair(x, y):
    b = shape([(x, y), (x + 90, y), (x + 84, y + 80), (x + 6, y + 80)], "#ece6d8", 2.4, 1.2)
    for i in range(3): b += line([(x + 14, y + 16 + i * 18), (x + 76, y + 16 + i * 18)], 1.2, 0.8, op=0.5)
    b += shape([(x - 8, y + 80), (x + 98, y + 80), (x + 92, y + 94), (x - 2, y + 94)], "#e2dccd", 2.2, 1.0)
    return b

def banig(x, y):
    b = shape([(x, y), (x + 40, y - 4), (x + 46, y + 300), (x + 4, y + 304)], "#c9a46a", 2.4, 1.4)
    for k in range(10): b += line([(x + 2, y + 20 + k * 28), (x + 44, y + 16 + k * 28)], 1.4, 0.8, "#8a6a3a", 0.8)
    b += blob(x + 22, y + 2, 20, 8, "#e0c08a", 1.8)
    return b

def crack(x, y):
    b = line([(x, y), (x - 12, y + 40), (x + 6, y + 70), (x - 8, y + 120), (x + 4, y + 170)], 2.4, 0.8)
    b += line([(x - 12, y + 40), (x - 40, y + 56)], 1.4, 0.6) + line([(x + 6, y + 70), (x + 34, y + 84)], 1.4, 0.6)
    b += px(["a.a..", ".aba.", "abbba", ".a.b."], {'a': '#56703a', 'b': '#8fae4e'}, x - 20, y + 30, 3)
    b += px(["a..a", ".ab.", "abba"], {'a': '#56703a', 'b': '#8fae4e'}, x + 2, y + 100, 3)
    return b

def daysclock(x, y, n):
    b = box(x, y, 120, 54, "#2b1e18", 2.4, 1.0) + f'<rect x="{x+8}" y="{y+8}" width="104" height="38" fill="#120808"/>'
    b += ptext("ARAW", "#7a2b24", x + 14, y + 13, 2)
    if not NO_NUMBERS: b += ptext(f"{n:02d}", "#ff4a3a", x + 60, y + 14, 5)
    return b

def gasmask(x, y):
    b = line([(x, y - 30), (x, y)], 1.4, 0.3) + f'<circle cx="{x}" cy="{y-32}" r="3" fill="#2a1c14"/>'
    b += shape([(x - 40, y + 20), (x, y), (x + 40, y + 20), (x + 34, y + 70), (x, y + 96), (x - 34, y + 70)], "#5d6a50", 2.4, 1.4)
    b += blob(x - 17, y + 36, 12, 12, "#9fc8cc", 2.0) + blob(x + 17, y + 36, 12, 12, "#9fc8cc", 2.0) + blob(x, y + 76, 16, 16, "#3a3f3a", 2.2)
    return b

def tally(x, y, n):
    """Days scratched into the wall, in groups of five."""
    b = ""
    for i in range(n):
        g, k = divmod(i, 5)
        gx = x + g * 44
        if k < 4: b += line([(gx + k * 8, y), (gx + k * 8 + 2, y + 34)], 1.8, 0.8, "#e9dcc2", 0.75)
        else: b += line([(gx - 4, y + 26), (gx + 30, y + 6)], 1.8, 0.8, "#e9dcc2", 0.75)
    return b

def photos(x, y):
    b = f'<g transform="rotate(4 {x} {y})">' + box(x, y, 52, 64, "#3e2a1f", 2.0, 0.8) + box(x + 6, y + 6, 40, 52, "#b9a98a", 1.2, 0.6)
    b += blob(x + 18, y + 30, 6, 7, "#6b5a48", 0.8) + blob(x + 32, y + 32, 6, 7, "#6b5a48", 0.8) + "</g>"
    return b

def decor(age, stage, days_left, disaster=None, night=False, skip=False):
    rnd(40)
    b = ""
    if not skip: b += spider_only(1362, 104)
    b += f'<g transform="translate(436 112) scale(0.86) translate(-436 -112)">' + poster(436, 112, age) + '</g>'
    b += clock(860, 150)
    b += bawal(1380, 116)[:0]  # (kept above the window instead, below)
    b += bawal(1076, 46)[:0]
    b += calendar(664, 196, days_left)
    b += daysclock(1150, 380, days_left)
    b += portrait(1560, 170)
    b += drawings(1720, 330)
    b += tv(1580, 494)
    b += fan(1800, 454, blades=not skip)
    b += banig(1872, 344)
    b += shelf(stage, age)
    b += bawal(440, 40)
    b += line([(205, 104), (200, 130), (210, 150)], 1.8, 1.0)  # radio cord up to the pipe
    b += line([(1700, 104), (1690, 200), (1702, 300), (1690, 400)], 1.8, 1.4)  # TV cord taped down the wall
    for ty in (170, 260, 350): b += f'<rect x="{1682}" y="{ty}" width="20" height="9" fill="#cfc6a0" opacity=".8" transform="rotate(-20 1692 {ty})"/>'
    b += photos(806, 262)
    if disaster == "outbreak": b += gasmask(1520, 330)
    return b

def fx_layer(disaster):
    """Room-wide signs of the Rondo result, on top of the decor."""
    rnd(90)
    b = ""
    x, y, w, h = WIN
    if disaster == "tornado":
        for i in range(14):  # wind streaks blowing in
            sx, sy = R.uniform(600, 1900), R.uniform(120, 520)
            b += line([(sx, sy), (sx - 140, sy + 30 + jit(10))], 1.4, 2.0, "#e9dcc2", 0.45)
        for i in range(6):  # papers and leaves in the air
            px_, py_ = R.uniform(300, 1700), R.uniform(140, 460); a = R.uniform(-40, 40)
            b += f'<g class="fly{i%3}" transform="rotate({a:.0f} {px_:.0f} {py_:.0f})">' + box(px_, py_, 34, 26, "#e9dcc2", 1.6, 1.0) + line([(px_ + 6, py_ + 8), (px_ + 28, py_ + 8)], 1.0, 0.4, op=0.6) + "</g>"
            b += blob(px_ + 120, py_ + 40, 9, 5, "#6d7a3a", 1.2)
        b += f'<g transform="rotate(-6 {x+w/2} {y+h})">' + box(x - 40, y + h + 40, 70, 20, C["wood_lt"], 2.0, 1.0) + "</g>"  # plank blown off
    if disaster == "doomsday":
        for i in range(70):  # dust shaken from the ceiling
            b += f'<rect class="fall" style="animation-delay:-{R.random()*3:.2f}s" x="{R.uniform(0,W):.0f}" y="{R.uniform(96,540):.0f}" width="3" height="3" fill="#cdbf9a" opacity=".8" shape-rendering="crispEdges"/>'
        b += line([(300, 100), (330, 180), (300, 260), (340, 330)], 2.6, 0.8) + line([(1500, 100), (1470, 200), (1510, 300)], 2.6, 0.8)
        b += line([(700, 96), (760, 150), (820, 140), (900, 200)], 2.2, 0.8)
    if disaster == "outbreak":
        for i in range(9):
            b += blob(R.uniform(0, W), R.uniform(470, 560), R.uniform(120, 260), R.uniform(30, 60), "#9fc060", 0, 18, 6).replace('opacity="1.0"', 'opacity=".28"')
        b += blob(x + w / 2, y + h / 2, w * 0.7, h * 0.7, "#9fc060", 0, 18, 6).replace('opacity="1.0"', 'opacity=".22"')
    if disaster == "purge":
        b += f'<path class="siren" d="M{x+w/2} {y+h/2} L{x-700} {y-60} L{x-700} {y+520}Z" fill="#ff3020" opacity=".18"/>'
        for i, (bx, by) in enumerate([(x - 30, y + 50), (x - 24, y + 150)]):  # boards nailed over the window
            b += f'<g transform="rotate({8 - i*14} {bx+w/2+30} {by})">' + box(bx, by, w + 60, 26, C["wood_lt"], 2.2, 1.0) + f'<circle cx="{bx+12}" cy="{by+13}" r="3" fill="#2a1c14"/><circle cx="{bx+w+48}" cy="{by+13}" r="3" fill="#2a1c14"/></g>'
    if disaster == "hunger":
        for (fx, fy) in [(150, 260), (260, 360), (200, 470), (330, 300)]:
            b += f'<g class="buzz">' + px(["a.a", ".b."], {'a': '#9aa1a5', 'b': '#120c0a'}, fx, fy, 3) + "</g>"
        b += text(220, 392, "UBOS NA", 30, "#e9dcc2", font="Comic Relief", rot=-4, op=0.85)
        for cx_ in (90, 1700, 1820):
            b += f'<g transform="rotate(70 {cx_} 690)">' + tuna(cx_, 670, 0.9) + "</g>"
    if disaster == "trading_only":
        b += box(1150, 470, 130, 86, "#7a5a3a", 2.4, 1.2) + text(1215, 520, "PALIT", 22, "#e9dcc2")
        b += tuna(1160, 440, 0.8) + galon(1222, 400)[:0] + blob(1250, 456, 16, 14, "#c96aa0", 1.6)
        b += shape([(1090, 360), (1330, 356), (1332, 410), (1088, 414)], "#e9dcc2", 2.2, 1.2) + text(1210, 396, "PALIT-PALIT LANG", 26, C["brick"])
        b += line([(1100, 360), (1120, 336)], 1.2) + line([(1320, 356), (1300, 336)], 1.2)
    if disaster == "peaceful":
        for i in range(5):
            rx_ = x + 30 + i * 50
            b += f'<path d="M{rx_} {y+h} L{rx_-120} {556} L{rx_-60} {556}Z" fill="#ffe0a0" opacity=".08"/>'
    return b

def lamp_layer(on=True, swing=0):
    x = 960
    b = f'<g transform="rotate({swing} {x} 96)">' + line([(x, 96), (x, 220)], 2.0, 0.3)
    b += shape([(x - 62, 262), (x - 40, 226), (x + 40, 226), (x + 62, 262)], "#2c373c", 2.6, 1.0)
    b += blob(x, 266, 14, 12, "#ffd27a" if on else "#4a4a46", 1.8) + "</g>"
    return b

def floor_layer():
    """Concrete floor either side of the table, with a woven banig mat peeking out."""
    b = f'<path d="M0 {EDGE} L{W} {EDGE} L{W} {H} L0 {H}Z" fill="#3e3a34"/>'
    for k in range(9):
        yy = EDGE + 8 + k * k * 7
        b += line([(0, yy), (W, yy)], 1.0, 1.5, op=0.35)
    for xx in range(-600, 2600, 160):
        b += line([(960 + (xx - 960) * 0.35, EDGE), (xx, H)], 1.0, 1.5, op=0.3)
    b += f'<path d="M0 {EDGE} L{W} {EDGE}" stroke="#1e1411" stroke-width="5"/>'
    return b

TL, TR, BL, BR = (420, EDGE), (1500, EDGE), (-60, H), (1980, H)

def table_layer():
    """A smaller worn table: planks in perspective, a woven banig mat in the middle, scratches, stains, a spill."""
    rnd(77)
    b = f'<path d="M{TL[0]} {TL[1]} L{TR[0]} {TR[1]} L{BR[0]} {BR[1]} L{BL[0]} {BL[1]}Z" fill="#4c3326"/>'
    for i in range(1, 9):
        t = i / 9
        b += line([(TL[0] + (TR[0] - TL[0]) * t, EDGE), (BL[0] + (BR[0] - BL[0]) * t, H)], 1.6, 1.0, op=0.55)
    b += f'<path d="M{TL[0]} {EDGE} L{TR[0]} {EDGE} L{TR[0]+6} {EDGE+18} L{TL[0]-6} {EDGE+18}Z" fill="#6a4837"/>'
    b += ink(rough([TL, TR], False, 1.0), 2.8) + ink(rough([TL, BL], False, 1.0), 2.6) + ink(rough([TR, BR], False, 1.0), 2.6)
    for _ in range(26):  # grain
        yy = R.uniform(EDGE + 30, H); xx = R.uniform(300, 1600)
        b += line([(xx, yy), (xx + R.uniform(80, 260), yy + jit(3))], 1.0, 1.2, op=0.28)
    # woven banig mat (the play area)
    m = [(760, 704), (1160, 704), (1290, 960), (630, 960)]
    b += f'<path d="M{m[0][0]} {m[0][1]+8} L{m[1][0]+6} {m[1][1]+8} L{m[2][0]+10} {m[2][1]+10} L{m[3][0]+6} {m[3][1]+10}Z" fill="#1a0f0a" opacity=".35"/>'
    b += shape(m, "#a98a5e", 2.4, 1.2)
    for i in range(1, 13):
        t = i / 13
        col = ["#a8392f", "#8a6a3a", "#36585f"][i % 3]
        b += line([(m[0][0] + (m[1][0] - m[0][0]) * t, 706), (m[3][0] + (m[2][0] - m[3][0]) * t, 958)], 1.8, 0.8, col, 0.45)
    for j in range(1, 6):
        t = j / 6; y = 704 + 256 * t
        b += line([(760 - 130 * t, y), (1160 + 130 * t, y)], 1.0, 1.0, "#6a4a28", 0.45)
    for k in range(14):  # fringe
        x0 = 772 + k * 28
        b += line([(x0, 704), (x0 - 2, 692)], 1.2, 0.4, "#b8925a")
    # scratches, a carved tally, rings and a spill
    for _ in range(12):
        sx, sy = R.uniform(460, 1500), R.uniform(EDGE + 20, 1060)
        if 600 < sx < 1320 and sy > 694: continue
        b += line([(sx, sy), (sx + R.uniform(30, 90), sy + jit(8))], 1.2, 0.8, "#d8b89a", 0.45)
    b += line([(1300, 676), (1302, 694)], 1.6, 0.3, "#d8b89a", 0.6) + line([(1310, 676), (1312, 694)], 1.6, 0.3, "#d8b89a", 0.6) + line([(1320, 676), (1322, 694)], 1.6, 0.3, "#d8b89a", 0.6) + line([(1296, 690), (1330, 678)], 1.6, 0.3, "#d8b89a", 0.6)
    b += f'<ellipse cx="560" cy="760" rx="40" ry="14" fill="none" stroke="#2a1a12" stroke-width="4" opacity=".45"/><ellipse cx="584" cy="770" rx="36" ry="12" fill="none" stroke="#2a1a12" stroke-width="3" opacity=".3"/>'
    b += oshape([(1460, 860), (1530, 846), (1600, 870), (1640, 920), (1590, 950), (1520, 940), (1470, 910)], "#7a98a0", 1.2, 2.0, op=0.3)
    b += oshape([(1500, 870), (1540, 864), (1560, 878), (1530, 884)], "#e0ecef", 0, 1.0, line=False, op=0.5)
    # candle stub with a drip and its shadow, a matchbox
    cx_, cy_ = 1478, 664   # candle stub stands on the table, right of the mat
    b += f'<ellipse cx="{cx_+18}" cy="{cy_+46}" rx="34" ry="9" fill="#1a0f0a" opacity=".4"/>'
    b += box(cx_, cy_, 26, 44, C["cream"], 2.0, 0.6) + line([(cx_ + 3, cy_ + 10), (cx_ - 2, cy_ + 32)], 2.2, 0.4, "#f2ecd8")
    b += f'<ellipse cx="{cx_+13}" cy="{cy_+46}" rx="24" ry="6" fill="#efe6cf"/>'
    b += '<g class="flame">' + px(["..a..", ".aba.", "abcba", ".bcb."], {'a': '#f6d65a', 'b': '#e0a23a', 'c': '#fff4d0'}, cx_ + 6, cy_ - 13, 3) + '</g>'
    b += f'<path d="M386 954 l84 -8 l8 26 l-84 8z" fill="#1a0f0a" opacity=".35"/>' + shape([(380, 940), (462, 932), (468, 956), (386, 964)], C["brick"], 2.0, 0.6) + text(424, 954, "POSPORO", 11, C["cream"], font="Bitter", weight="bold")
    return b

def wall_marks(days_left):
    """Things painted or scratched on the wall itself (no paper edge): web, crack + mould, tally, water stains."""
    rnd(41)
    b = spider(1362, 104, web_only=True) + crack(1010, 130) + tally(452, 410, 11 - days_left)
    for sx in (360, 980, 1420):
        b += f'<ellipse cx="{sx}" cy="{118}" rx="44" ry="16" fill="#3a2a1a" opacity=".22"/>'
    return b

def floor_props(age, stage, disaster):
    """Clutter on the floor either side of the table, standing on the floor line (y 650+)."""
    import props3 as PR
    b = ""
    # left: water galon, tabo and balde, a rice sack slumped by the shelf, slippers
    b += PR.galon_p(-30, 520, 0.55)                      # base of the galon at about y 674
    b += PR.tabo_balde(120, 600, 0.6)
    if stage < 2: b += PR.bigas_p(240, 560, 0.42)
    b += tsinelas(60, 780) + tsinelas(150, 840, -20)
    b += crate(20, 860, "SILONG")
    # right: cardboard boxes, a gasera (oil lamp), a rolled mat, slippers, an extension cord
    b += crate(1740, 690, "DE LATA") + crate(1780, 610, "TUBIG", 0.8)
    b += gasera(1680, 716)
    b += line([(1560, 650), (1600, 700), (1700, 760), (1860, 800), (1920, 790)], 3.2, 2.0, "#1e1411")
    b += tsinelas(1780, 900, 15)
    b += PR.banig(1640, 830, 0.7)
    return b

def tsinelas(x, y, rot=0):
    b = f'<g transform="rotate({rot} {x+40} {y+20})">'
    for dx in (0, 48):
        b += oshape(ellipse_pts(x + dx + 20, y + 20, 18, 34, 14), "#3b7fb8", 2.0, 0.6) + oshape(ellipse_pts(x + dx + 20, y + 22, 13, 28, 12), "#e9dcc2", 1.0, 0.4)
        b += line([(x + dx + 8, y + 18), (x + dx + 20, y + 6), (x + dx + 32, y + 18)], 3.0, 0.3, "#a8392f")
    return b + "</g>"

def crate(x, y, label, s=1.0):
    w, h = 150 * s, 100 * s
    b = shape([(x, y + 18 * s), (x + w, y + 18 * s), (x + w, y + h), (x, y + h)], "#b08a5a", 2.4, 1.0)
    b += shape([(x, y + 18 * s), (x + 22 * s, y), (x + w + 22 * s, y), (x + w, y + 18 * s)], "#c9a46a", 2.2, 0.8)
    b += shape([(x + w, y + 18 * s), (x + w + 22 * s, y), (x + w + 22 * s, y + h - 18 * s), (x + w, y + h)], "#8a6a40", 2.2, 0.8)
    b += line([(x + 10, y + 50 * s), (x + w - 10, y + 50 * s)], 6 * s, 0.4, "#c8b890", 0.8)  # tape
    b += text(x + w / 2, y + 80 * s, label, 22 * s, "#5a3a20")
    return b

def gasera(x, y):
    b = oshape([(x, y + 60), (x + 6, y + 20), (x + 34, y + 20), (x + 40, y + 60)], "#7d8584", 2.2, 0.6)
    b += box(x + 14, y + 6, 12, 16, "#555b5a", 1.6, 0.4) + line([(x + 20, y + 6), (x + 20, y - 4)], 1.4, 0.2)
    b += '<g class="flame">' + px([".a.", "aba", "aba", ".b."], {'a': '#f6d65a', 'b': '#e0a23a'}, x + 16, y - 16, 3) + "</g>"
    b += oshape(ellipse_pts(x + 20, y + 62, 30, 6, 12), "#1a0f0a", 0, 0.5, line=False, op=0.4)
    return b

def paper(D, dx=9, dy=12, blur=6, op=0.45):
    """2.5D: everything on the wall is a paper cut-out standing a little off it, with a soft shadow and a lit top edge."""
    a = D[:, :, 3]
    sh = ndimage.gaussian_filter(ndimage.shift(a, (dy, dx), order=0), blur) * op
    out = np.zeros_like(D); out[:, :, :3] = np.array([0.05, 0.03, 0.02]); out[:, :, 3] = sh
    out = over(out, D)
    edge = np.clip(a - ndimage.shift(a, (2, 2), order=0), 0, 1) * 0.22
    out[:, :, :3] = np.clip(out[:, :, :3] + edge[..., None] * np.array([1.0, 0.92, 0.78]), 0, 1)
    return out

def light_fn(day, sundown, night, disaster, age):
    """Returns a function that lights and grades any full-frame layer the same way (base and animated parts)."""
    x, y, w, h = WIN
    lamp_on = not night and disaster != "blackout"
    lift = np.zeros((H, W, 3), np.float32)
    if lamp_on: lift += glow(H, W, 960, 300, 620, (1.0, 0.82, 0.5), 0.33 * (1 - 0.3 * age))
    if not night and disaster not in ("blackout",):
        lift += glow(H, W, x + w / 2, y + h + 140, 360, (1.0, 0.6, 0.35), 0.16 * (1 - age))
    if disaster == "blackout": lift += glow(H, W, 310, 140, 420, (1.0, 0.7, 0.35), 0.7)
    if disaster == "purge": lift += glow(H, W, x + w / 2, y + h / 2, 1100, (1.0, 0.12, 0.08), 0.42)
    if disaster == "outbreak": lift += glow(H, W, x + w / 2, y + h / 2, 1000, (0.55, 0.95, 0.3), 0.25)
    if disaster == "doomsday": lift += glow(H, W, x + w / 2, y + h / 2, 1200, (1.0, 0.42, 0.12), 0.4)
    if disaster == "peaceful": lift += glow(H, W, x + w / 2, y + h, 900, (1.0, 0.85, 0.55), 0.3)
    if disaster == "tornado": lift += glow(H, W, x + w / 2, y + h / 2, 900, (0.6, 0.75, 0.8), 0.15)
    if night: g = dict(tint=(0.55, 0.68, 0.85), sat=0.7, vignette=0.75)
    elif disaster == "purge": g = dict(tint=(1.0, 0.72, 0.7), sat=0.85, vignette=0.7)
    elif disaster == "outbreak": g = dict(tint=(0.86, 1.0, 0.78), sat=0.8, vignette=0.6)
    elif disaster == "tornado": g = dict(tint=(0.78, 0.86, 0.9), sat=0.65, vignette=0.7)
    elif disaster == "doomsday": g = dict(tint=(1.05, 0.75, 0.6), sat=0.9, vignette=0.75)
    elif disaster == "peaceful": g = dict(tint=(1.08, 1.02, 0.92), sat=1.0, vignette=0.35)
    elif disaster == "blackout": g = dict(tint=(0.5, 0.48, 0.5), sat=0.7, vignette=0.85)
    else:
        tint = tuple((1.04, 0.98, 0.9)[i] * (1 - age) + (0.82, 0.62, 0.62)[i] * age for i in range(3))
        g = dict(tint=tint, sat=0.92 - 0.2 * age, vignette=0.45 + 0.4 * age)
    def apply(layer):
        o = layer.copy()
        o[:, :, :3] = np.clip(o[:, :, :3] * (1 + lift * 0.6) + lift * 0.25, 0, 1)
        return grade(o, **g)
    return apply

def compose(day=1, sundown=10, night=False, disaster=None, chars=True, name="room", parts=False):
    """parts=True leaves the moving bits out of the base and returns them as separate lit layers."""
    age = (day - 1) / max(1, sundown - 1)
    days_left = sundown - day + 1
    stage = 0 if days_left > 7 else 1 if days_left > 3 else 2
    if disaster == "hunger": stage = 2
    sky, defs = sky_layer(day, sundown, night, disaster)
    S = render(sky, defs=defs)
    Wl = render(wall_layer(age) + wall_marks(days_left))
    x, y, w, h = WIN
    Wl[y:y + h, x:x + w, 3] = 0  # window hole
    Wl = texture(Wl, age=age, grime=0.42, stain=0.4, seed=12, grain_amt=0.04)
    D = render(decor(age, stage, days_left, disaster, night, skip=parts))
    D = texture(D, age=age, grime=0.2, stain=0.12, seed=19, grain_amt=0.03, mottle_scale=120)
    D = paper(rim(comic(D, seed=3)), 10, 13, 6, 0.5)
    FP = paper(rim(comic(texture(render(floor_props(age, stage, disaster)), age=age, grime=0.25, stain=0.05, seed=29, grain_amt=0.03, mottle_scale=70), seed=5)), 10, 8, 5, 0.55)
    T = texture(render(table_layer()), age=age, grime=0.3, stain=0.05, seed=33)
    F = texture(render(floor_layer()), age=age, grime=0.4, stain=0.0, seed=44)
    WF = paper(comic(texture(render(window_frame(age, disaster)), age=age, grime=0.25, stain=0.1, seed=23), seed=4), 8, 10, 5, 0.45)
    out = over(S, Wl); out = over(out, F); out = over(out, WF); out = over(out, D); out = over(out, FP)
    if disaster: out = over(out, render(fx_layer(disaster)))
    out = over(out, render(f'<rect x="0" y="{EDGE-60}" width="{W}" height="60" fill="#000" opacity=".0"/>'))
    out = over(out, render(dither(0, 100, W, 60, "#100a08", 3, "up", 0.55) + dither(0, EDGE - 90, W, 90, "#0e0a08", 3, "down", 0.35)))
    lit = light_fn(day, sundown, night, disaster, age)
    lamp_on = not night and disaster != "blackout"
    if not parts:
        if chars: out = seat_chars(out)
        out = over(out, T)
        out = over(out, render(lamp_layer(lamp_on)))
        out = lit(out); out[:, :, 3] = 1
        return out
    base = lit(out); base[:, :, 3] = 1
    layers = {"table": lit(T), "lamp": lit(render(lamp_layer(lamp_on)))}
    for k, (code, sx, flip) in enumerate(SEATS, 1):
        layers[f"seat{k}"] = lit(seat_chars(np.zeros((H, W, 4), np.float32), [(code, sx, flip)]))
    layers["fan"] = lit(texture(render(fan_blades(1800, 454)), age=age, grime=0.15, stain=0, seed=7))
    layers["spider"] = lit(rim(render(spider_only(1362, 104)), 2))
    layers["calpage"] = lit(render(calendar(664, 196, days_left + 1)))
    return base, layers

CAST = "/home/claude/rig/cast_out/"
from scipy import ndimage
def cutout(im, rim=4, rim_col=(239, 230, 210)):
    """Paper Mario feel: a thin pale paper edge round a cut-out sprite."""
    a = np.asarray(im).astype(np.float32) / 255
    al = a[:, :, 3] > 0.5
    pad = rim + 2
    big = np.zeros((a.shape[0] + 2 * pad, a.shape[1] + 2 * pad, 4), np.float32)
    big[pad:-pad, pad:-pad] = a
    m = np.zeros(big.shape[:2], bool); m[pad:-pad, pad:-pad] = al
    ring = ndimage.binary_dilation(m, iterations=rim) & ~m
    out = big.copy()
    out[ring, :3] = np.array(rim_col) / 255; out[ring, 3] = 1
    out[:, :, 3] = np.maximum(out[:, :, 3], ndimage.gaussian_filter(ring.astype(np.float32), 0.6))
    return out

def angle(a, turn):
    """Fake a slight turn about the vertical axis: the side away from the viewer shrinks (turn -1..1)."""
    h, w = a.shape[:2]
    im = Image.fromarray((a * 255).astype(np.uint8), "RGBA")
    k = abs(turn) * 0.09
    nw = int(w * (1 - abs(turn) * 0.08))
    if turn > 0:   # right edge further away
        quad = (0, 0, 0, h, w, h - h * k * 0.5, w, h * k * 0.5)
    else:
        quad = (0, h * k * 0.5, 0, h - h * k * 0.5, w, h, w, 0)
    im = im.transform((nw, h), Image.QUAD, quad, Image.BICUBIC)
    return np.asarray(im).astype(np.float32) / 255

def place(out, a, x0, y0, shadow=None):
    h, w = a.shape[:2]
    layer = np.zeros_like(out)
    ys, xs = max(0, y0), max(0, x0)
    ye, xe = min(out.shape[0], y0 + h), min(out.shape[1], x0 + w)
    layer[ys:ye, xs:xe] = a[ys - y0:ye - y0, xs - x0:xe - x0]
    if shadow:
        dx, dy, blur, op = shadow
        sh = ndimage.shift(layer[:, :, 3], (dy, dx), order=0)
        sh = ndimage.gaussian_filter(sh, blur) * op
        sl = np.zeros_like(out); sl[:, :, :3] = np.array([0.06, 0.03, 0.02]); sl[:, :, 3] = sh
        out = over(out, sl)
    return over(out, layer)

def seat_chars(out, who=SEATS, shadow=True):
    for code, sx, flip in who:
        im = Image.open(f"{CAST}{code}.png").convert("RGBA")
        k = CHAR_H / im.height; im = im.resize((round(im.width * k), CHAR_H), Image.LANCZOS)
        if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
        a = cutout(im)
        turn = (sx - 960) / 440          # left seat turns in, right seat turns in, middle faces us
        if abs(turn) > 0.1: a = angle(a, -turn)
        x0 = sx - a.shape[1] // 2; y0 = EDGE + 70 - a.shape[0]
        dx = int(np.sign(sx - 960) * 26)
        out = place(out, a, x0, y0, (int(dx * 1.3), 20, 11, 0.7) if shadow else None)
    return out

def export_parts(tag="d1", day=1, **kw):
    """Hero render for the canvas: base + animated layers, each a full-frame PNG at 1280x720."""
    base, layers = compose(day=day, parts=True, **kw)
    save(base, f"{OUT}hero_{tag}_base.png", (1280, 720))
    for k, a in layers.items():
        save(a, f"{OUT}hero_{tag}_{k}.png", (1280, 720))
    return list(layers)

if __name__ == "__main__":
    if sys.argv[1:2] == ["hero"]:
        print(export_parts("d1", 1)); sys.exit()
    if sys.argv[1:2] == ["heronight"]:
        print(export_parts("n1", 1, night=True)); sys.exit()
    jobs = sys.argv[1:] or ["d1"]
    for j in jobs:
        if j[0] == "d" and j[1:].isdigit():
            d = int(j[1:]); img = compose(day=d, name=j)
        elif j == "night": img = compose(day=5, night=True)
        else: img = compose(day=4, disaster=j)
        save(img, f"{OUT}room_{j}.png", (1280, 720))
        print("ok", j)
