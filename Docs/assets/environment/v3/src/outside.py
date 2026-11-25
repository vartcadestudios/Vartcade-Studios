"""Final outside scene v3: M-A Filipino street (main menu, Sundown results backdrop). 1920x1080 layers."""
import sys, math
import numpy as np
from style import *
from style import P as C
import props3 as PR
from room import paper

OUT = "/home/claude/envd/project/v3/"
GROUND = 760

def sky(day, sundown=10, night=False, disaster=None, movers=True):
    t = (day - 1) / max(1, sundown - 1)
    def mix(a, b_, k):
        a = [int(a[i:i + 2], 16) for i in (1, 3, 5)]; b2 = [int(b_[i:i + 2], 16) for i in (1, 3, 5)]
        return "#" + "".join(f"{round(a[i]+(b2[i]-a[i])*k):02x}" for i in range(3))
    top, mid, bot = mix("#4a3a5e", "#0a0a14", t), mix("#c86a4a", "#3a1420", t), mix("#f6b46a", "#b0401a", t)
    if night: top, mid, bot = "#03040a", "#0a1020", "#1a2238"
    pal = {"peaceful": ("#6a9ac0", "#f0c890", "#f8e0a0"), "purge": ("#14040a", "#4a0a12", "#a01a14"),
           "tornado": ("#26302c", "#4a564e", "#788a70"), "doomsday": ("#1a0604", "#7a2008", "#f06a1a"),
           "outbreak": ("#1a2414", "#3e5a2a", "#a0b85a"), "blackout": ("#03040a", "#0a0e1a", "#141a2a"),
           "hunger": ("#3a2a2a", "#a0603a", "#d89a5a"), "trading_only": ("#4a4a6a", "#d08a5a", "#f6c47a")}
    if disaster in pal: top, mid, bot = pal[disaster]
    d = f'<linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{top}"/><stop offset=".55" stop-color="{mid}"/><stop offset=".8" stop-color="{bot}"/></linearGradient>'
    b = f'<rect width="{W}" height="{H}" fill="url(#sky)"/>'
    dark = night or disaster in ("blackout",)
    if not dark and disaster not in ("tornado", "outbreak", "purge"):
        sy = 420 + t * 260
        b += f'<circle cx="980" cy="{sy:.0f}" r="{120}" fill="#f6a24a" opacity=".25"/><circle cx="980" cy="{sy:.0f}" r="70" fill="#f6a24a"/>'
    if dark:
        rnd(9)
        b += '<g>' + "".join(f'<rect class="tw" style="animation-delay:-{R.random()*2.4:.2f}s" x="{R.uniform(0,W):.0f}" y="{R.uniform(0,420):.0f}" width="3" height="3" fill="#efe6cf" shape-rendering="crispEdges"/>' for _ in range(90)) + "</g>"
        b += f'<circle cx="1500" cy="140" r="38" fill="#e8e2cc"/><circle cx="1516" cy="130" r="34" fill="{top}"/>'
    # drifting clouds (A03)
    rnd(12)
    for i in range(5 if movers else 0):
        cx, cy = R.uniform(100, 1800), R.uniform(80, 300)
        col = "#2a2236" if t > 0.5 or dark else "#8a5a6a"
        b += f'<g class="cloud c{i%3}">' + oshape([(cx - 140, cy + 20), (cx - 100, cy - 20), (cx - 30, cy - 34), (cx + 40, cy - 26), (cx + 120, cy - 4), (cx + 150, cy + 20)], col, 0, 6, line=False, op=0.7) + "</g>"
    if disaster == "tornado":
        b += oshape([(1300, 0), (1460, 0), (1420, 160), (1380, 340), (1360, 560), (1340, 560), (1330, 340), (1310, 160)], "#2a3430", 0, 6, line=False, op=0.85)
    if disaster == "doomsday":
        for mx, my in [(300, 120), (900, 60), (1500, 180), (1200, 300)]:
            b += line([(mx, my), (mx - 120, my - 120)], 8, 0.5, "#f6c04a", 0.6) + f'<circle cx="{mx}" cy="{my}" r="12" fill="#ffe08a"/>'
        b += oshape([(1560, 640), (1572, 420), (1500, 410), (1490, 340), (1560, 300), (1650, 310), (1680, 380), (1640, 420), (1594, 424), (1600, 640)], "#c9a880", 0, 4, line=False, op=0.6)
    if disaster == "peaceful":
        for bx, by in [(400, 200), (440, 180), (480, 210), (1200, 160), (1240, 150)]:
            b += line([(bx, by), (bx + 10, by - 8), (bx + 20, by)], 2.2) + line([(bx + 20, by), (bx + 30, by - 8), (bx + 40, by)], 2.2)
    elif movers:
        for bx, by in [(1100, 120), (1150, 100), (300, 260)]:  # crows in the sky
            b += f'<g class="crowfly">' + line([(bx, by), (bx + 12, by - 10), (bx + 24, by)], 2.6) + line([(bx + 24, by), (bx + 36, by - 10), (bx + 48, by)], 2.6) + "</g>"
    return b, d

def far(night, age):
    rnd(30)
    b = ""
    x = -20
    while x < W:  # far rooftops: GI sheets, tanks, antennas
        w = R.uniform(90, 200); h = R.uniform(80, 200)
        top = 620 - h
        b += shape([(x, 640), (x, top + 20), (x + w * 0.5, top), (x + w, top + 20), (x + w, 640)], "#1c1418", 1.4, 1.5)
        if R.random() < 0.4: b += box(x + w * 0.3, top - 34, 30, 30, "#1c1418", 1.2, 0.8)
        if R.random() < 0.4: b += line([(x + w * 0.7, top), (x + w * 0.7, top - 50), (x + w * 0.6, top - 40)], 1.2, 0.5, "#1c1418")
        if night or R.random() < 0.15:
            for _ in range(2):
                wx, wy = x + R.uniform(10, w - 24), top + R.uniform(30, h - 30)
                b += f'<rect x="{wx:.0f}" y="{wy:.0f}" width="14" height="18" fill="#f2c46a" opacity="{.85 if night else .4}"/>'
        x += w - 10
    return b

def laundry():
    b = line([(1460, 360), (1600, 382), (1740, 356)], 1.4, 0.8)
    for k, (cx, col) in enumerate([(1500, C["brick"]), (1550, C["gold"]), (1610, C["teal"]), (1670, "#e9dcc2")]):
        b += shape([(cx, 368), (cx + 34, 370), (cx + 32, 420), (cx + 2, 418)], col, 2.0, 1.0)
    return b

def clouds(night, t):
    rnd(12); b = ""
    for i in range(5):
        cx, cy = R.uniform(100, 1800), R.uniform(80, 300)
        col = "#2a2236" if t > 0.5 or night else "#8a5a6a"
        b += oshape([(cx - 140, cy + 20), (cx - 100, cy - 20), (cx - 30, cy - 34), (cx + 40, cy - 26), (cx + 120, cy - 4), (cx + 150, cy + 20)], col, 0, 6, line=False, op=0.7)
    return b

def crows():
    b = ""
    for bx, by in [(1100, 120), (1150, 100), (300, 260)]:
        b += line([(bx, by), (bx + 12, by - 10), (bx + 24, by)], 2.6) + line([(bx + 24, by), (bx + 36, by - 10), (bx + 48, by)], 2.6)
    return b

def houses(age, movers=True):
    """Two-storey houses right of centre: capiz windows, GI roof, laundry line (A05)."""
    rnd(31)
    b = ""
    for hx, col in [(1430, "#7a6a54"), (1690, "#6a5a68")]:
        b += shape([(hx, GROUND), (hx, 380), (hx + 250, 380), (hx + 250, GROUND)], col, 2.6, 1.6)
        b += shape([(hx - 20, 384), (hx + 125, 300), (hx + 270, 384)], "#8a8a86", 2.6, 1.4)
        for k in range(10): b += line([(hx - 10 + k * 28, 380), (hx + 115 + k * 2, 306)], 1.0, 0.6, "#5a5a56", 0.6)
        b += box(hx + 30, 420, 80, 70, C["wood_dk"], 2.2, 1.0) + box(hx + 140, 420, 80, 70, C["wood_dk"], 2.2, 1.0)
        for wx in (hx + 30, hx + 140):
            for i in range(4):
                for j in range(3): b += f'<rect x="{wx+4+i*19}" y="{424+j*22}" width="16" height="19" fill="#efe4cc" opacity=".55"/>'
        b += box(hx + 80, 560, 90, GROUND - 560, "#3e2a1f", 2.2, 1.0)
        b += line([(hx, 520), (hx + 250, 516)], 1.4, 1.0, op=0.5)
    if movers: b += laundry()
    return b

def street_story(age):
    """Small stories on the walls: Silong posters, a countdown in graffiti, a boarded window, a cat on the roof."""
    rnd(55)
    b = ""
    for px_, py_ in [(1450, 580), (1474, 600)]:
        b += f'<g transform="rotate({jit(6):.1f} {px_+30} {py_+40})">' + box(px_, py_, 60, 80, "#e9dcc2", 1.8, 1.0) + blob(px_ + 30, py_ + 30, 14, 14, "#f2c94c", 1.4) + text(px_ + 30, py_ + 68, "SILONG", 11, C["teal_dk"]) + "</g>"
    b += text(1800, 730, "10 ARAW", 44, "#d8d0c0", font="Comic Relief", rot=-6, op=0.75)
    b += line([(1700, 610), (1900, 600)], 1.0, 2, "#d8d0c0", 0.0)
    b += f'<g transform="rotate(-8 1500 455)">' + box(1440, 440, 130, 18, C["wood_lt"], 2.0, 0.8) + "</g>" + f'<g transform="rotate(10 1500 470)">' + box(1440, 466, 130, 18, C["wood_lt"], 2.0, 0.8) + "</g>"
    b += '<g transform="translate(0 -12)">' + oshape([(1600, 300), (1606, 280), (1616, 290), (1630, 288), (1640, 280), (1644, 300), (1640, 322), (1604, 322)], "#120c0c", 1.2, 0.4)  # cat
    b += ocurve([(1640, 316), (1670, 310), (1676, 290)], 3, 0.4, "#120c0c")
    b += '<g class="tw">' + f'<circle cx="1614" cy="298" r="2" fill="#f6d65a"/><circle cx="1628" cy="298" r="2" fill="#f6d65a"/></g></g>'
    # pixel LED "BUKAS" (open) sign on the store (X12)
    b += box(380, 480, 96, 30, "#120808", 1.8, 0.6) + ptext("BUKAS", "#ff5a3a", 388, 487, 3)
    return b

def street(age, disaster=None):
    rnd(40)
    b = f'<rect x="0" y="{GROUND}" width="{W}" height="{H-GROUND}" fill="#3a3436"/>'
    b += shape([(0, GROUND), (W, GROUND), (W, GROUND + 30), (0, GROUND + 34)], "#6a645e", 2.2, 1.2)  # curb
    b += line([(0, 930), (W, 924)], 6, 1.0, "#d8b04a", 0.7).replace('stroke-linecap="round"', 'stroke-dasharray="70 50"')
    for _ in range(8):  # cracks
        cx = R.uniform(0, W); cy = R.uniform(GROUND + 60, H - 20)
        b += line([(cx, cy), (cx + R.uniform(40, 120), cy + jit(20)), (cx + R.uniform(120, 200), cy + jit(30))], 1.6, 2.0, op=0.7)
    for px_, py_, rw in [(620, 980, 140), (1280, 900, 100)]:  # puddles reflecting the sky
        b += oshape(ellipse_pts(px_, py_, rw, rw * 0.18, 18), "#8a6a7a", 1.4, 3.0, op=0.55)
    b += blob(1100, 1010, 70, 18, "#2a2628", 2.2) + blob(1100, 1010, 52, 12, "#3a3436", 1.2)  # manhole
    for tx, ty in [(300, 1000), (1560, 970), (1800, 1030)]:  # litter
        b += shape([(tx, ty), (tx + 40, ty - 6), (tx + 46, ty + 14), (tx + 4, ty + 18)], "#bfae86", 1.4, 1.5)
    # Silong bunker hatch in the pavement
    hx, hy = 1520, 900
    b += oshape(ellipse_pts(hx, hy, 150, 40, 22), "#4a5458", 2.6, 1.0) + oshape(ellipse_pts(hx, hy - 6, 118, 28, 22), "#5e6a6e", 2.0, 0.8)
    b += box(hx - 40, hy - 40, 80, 14, "#6e7a7e", 2.0, 0.6) + text(hx, hy + 6, "SILONG", 24, "#d8e4ea", op=0.8)
    b += line([(hx, hy - 40), (hx, hy - 120)], 2.0, 0.3) + '<g class="hatchlight">' + blob(hx, hy - 126, 10, 10, "#ffd27a", 1.6) + "</g>"
    return b

def compose(day=1, sundown=10, night=False, disaster=None, parts=False):
    age = (day - 1) / max(1, sundown - 1)
    sk, d = sky(day, sundown, night, disaster, movers=not parts)
    out = render(sk, defs=d)
    farl = render(far(night or disaster == "blackout", age))
    out = over(out, farl)
    mid = ""
    mid += PR.pole(380, 320, 1.4)
    mid += line([(940, 345), (1100, 400), (1440, 392)], 1.4, 1.0) + line([(940, 355), (1120, 430), (1440, 410)], 1.4, 1.0)
    mid += houses(age, movers=not parts)
    mid += PR.billboard(1080, 340, 0.95)
    mid += PR.deadtree(1800, 480, 1.0)
    mid += PR.sarisari(-20, 330, 1.45)
    mid += PR.hoop(1660, 470, 0.85)
    mid += street_story(age)
    # water tank on a wooden platform that stands on the roof (it used to float)
    mid += box(1584, 309, 120, 12, C["wood_dk"], 2.2, 0.6)
    for px_, ry in [(1598, 323), (1648, 352), (1694, 378)]:
        mid += box(px_ - 5, 321, 10, ry - 321, C["wood_dk"], 1.8, 0.4)
    mid += line([(1598, 323), (1648, 352)], 2.0, 0.4) + line([(1648, 321), (1694, 378)], 2.0, 0.4)
    mid += PR.tank(1530, 150, 0.55)
    M = texture(render(mid), age=age, grime=0.3, stain=0.15, seed=51, grain_amt=0.035, mottle_scale=90, rust=0.25)
    M = paper(rim(comic(M, seed=7)), 10, 8, 7, 0.45)
    out = over(out, M)
    st = texture(render(street(age, disaster)), age=age, grime=0.4, stain=0.0, seed=61)
    out = over(out, st)
    fg = PR.jeepney(860, 560, 1.15) + PR.monobloc(300, 610, 0.5) + PR.rooster(470, 650, 0.4) + PR.banig(10, 700, 0.5)
    fg += oshape([(1300, 770), (1290, 730), (1320, 700), (1360, 706), (1376, 740), (1370, 772)], "#1e1a1c", 2.2, 1.0) + oshape([(1350, 772), (1352, 742), (1384, 726), (1414, 744), (1410, 774)], "#2a2628", 2.2, 1.0)  # trash bags
    if disaster == "hunger":
        fg += shape([(120, 560), (400, 556), (402, 610), (118, 614)], "#e9dcc2", 2.4, 1.2) + text(260, 598, "SARADO / UBOS NA", 32, C["brick"])
    if disaster == "trading_only":
        fg += box(560, 780, 260, 90, "#7a5a3a", 2.4, 1.0) + text(690, 838, "PALIT-PALIT LANG", 28, "#e9dcc2")
    if disaster == "purge":
        for i in range(3): fg += f'<g transform="rotate({(-1)**i*7} 280 {520+i*60})">' + box(60, 500 + i * 60, 440, 30, C["wood_lt"], 2.4, 1.0) + "</g>"
    F = texture(render(fg), age=age, grime=0.25, stain=0.05, seed=71, grain_amt=0.03, mottle_scale=70)
    F = paper(rim(comic(F, seed=9)), 12, 10, 8, 0.55)
    out = over(out, F)
    # disaster fx over everything
    fx = ""
    rnd(91)
    if disaster == "tornado":
        for _ in range(40):
            sx, sy = R.uniform(0, W), R.uniform(0, H)
            fx += f'<rect class="rain" x="{sx:.0f}" y="{sy:.0f}" width="3" height="18" fill="#9ab4c0" opacity=".6" shape-rendering="crispEdges"/>'
        for _ in range(8):
            px_, py_ = R.uniform(200, 1800), R.uniform(200, 700)
            fx += f'<g class="fly{R.randint(0,2)}">' + box(px_, py_, 40, 30, "#e9dcc2", 1.6, 1.0) + "</g>"
    if disaster == "outbreak":
        for _ in range(12):
            fx += oshape(ellipse_pts(R.uniform(0, W), R.uniform(600, 1060), R.uniform(160, 340), R.uniform(40, 80), 16), "#9fc060", 0, 6, line=False, op=0.3)
    if disaster == "doomsday":
        for _ in range(120):
            fx += f'<rect class="fall" style="animation-delay:-{R.random()*3:.2f}s" x="{R.uniform(0,W):.0f}" y="{R.uniform(0,H):.0f}" width="3" height="3" fill="#f6b46a" opacity=".7" shape-rendering="crispEdges"/>'
    if fx: out = over(out, render(fx))
    hh, ww = out.shape[:2]
    lit = lighter(day, sundown, night, disaster, age)
    out = lit(out); out[:, :, 3] = 1
    if not parts: return out
    t = age
    layers = {"clouds": lit(render(clouds(night, t))), "laundry": lit(texture(render(laundry()), age=age, grime=0.2, stain=0, seed=5)),
              "crows": lit(render(crows()))}
    return out, layers

def lighter(day, sundown, night, disaster, age):
    hh, ww = H, W
    lift = np.zeros((hh, ww, 3), np.float32)
    dark = night or disaster == "blackout"
    if not dark and disaster not in ("tornado", "outbreak", "purge"):
        lift += glow(hh, ww, 980, 420 + age * 260, 900, (1.0, 0.62, 0.3), 0.35 * (1 - 0.4 * age))
    lift += glow(hh, ww, 1520, 774, 160, (1.0, 0.82, 0.45), 0.5)  # hatch lamp
    if disaster == "purge": lift += glow(hh, ww, 900, 600, 1200, (1.0, 0.1, 0.08), 0.4)
    if disaster == "outbreak": lift += glow(hh, ww, 900, 800, 1100, (0.55, 0.95, 0.3), 0.3)
    if disaster == "doomsday": lift += glow(hh, ww, 1580, 500, 1200, (1.0, 0.45, 0.15), 0.45)
    if dark: lift += glow(hh, ww, 290, 520, 320, (1.0, 0.75, 0.4), 0.3)  # store's lamp
    if night: g = dict(tint=(0.55, 0.66, 0.85), sat=0.7, vignette=0.7)
    elif disaster == "blackout": g = dict(tint=(0.45, 0.45, 0.55), sat=0.6, vignette=0.85)
    elif disaster == "purge": g = dict(tint=(1.0, 0.7, 0.68), sat=0.85, vignette=0.7)
    elif disaster == "outbreak": g = dict(tint=(0.86, 1.0, 0.78), sat=0.8, vignette=0.6)
    elif disaster == "tornado": g = dict(tint=(0.78, 0.86, 0.9), sat=0.6, vignette=0.7)
    elif disaster == "doomsday": g = dict(tint=(1.05, 0.75, 0.6), sat=0.9, vignette=0.75)
    elif disaster == "peaceful": g = dict(tint=(1.06, 1.02, 0.94), sat=1.0, vignette=0.3)
    else:
        tint = tuple((1.04, 0.98, 0.9)[i] * (1 - age) + (0.84, 0.64, 0.64)[i] * age for i in range(3))
        g = dict(tint=tint, sat=0.95 - 0.2 * age, vignette=0.4 + 0.35 * age)
    def apply(layer):
        o = layer.copy()
        o[:, :, :3] = np.clip(o[:, :, :3] * (1 + lift * 0.6) + lift * 0.25, 0, 1)
        return grade(o, **g)
    return apply

if __name__ == "__main__":
    if sys.argv[1:2] == ["hero"]:
        base, layers = compose(day=1, parts=True)
        save(base, f"{OUT}shero_d1_base.png", (1280, 720))
        for k, a in layers.items(): save(a, f"{OUT}shero_d1_{k}.png", (1280, 720))
        print(list(layers)); sys.exit()
    for j in sys.argv[1:] or ["d1"]:
        if j[0] == "d" and j[1:].isdigit(): img = compose(day=int(j[1:]))
        elif j == "night": img = compose(day=5, night=True)
        else: img = compose(day=4, disaster=j)
        save(img, f"{OUT}street_{j}.png", (1280, 720)); print("ok", j)
