"""v3.3 UI kit: paper-and-wood buttons (after the zip's button references), a vanity mirror for the avatar,
a locked customisation shop, a name plaque and a screen frame. All drawn in the v3 style, saved as PNGs."""
import math, os, sys
import numpy as np
from PIL import Image
from style import *
from style import P as C

OUT = "/home/claude/envd/project/v3/ui3/"
os.makedirs(OUT, exist_ok=True)

def finish(body, w, h, seed=1, grime=0.18, shadow=True, rimpx=3, halft=True):
    a = render(body, w, h)
    a = texture(a, age=0.25, grime=grime, stain=0.04, seed=seed, grain_amt=0.03, mottle_scale=50)
    if halft: a = comic(a, cell=5, strength=0.2, seed=seed)
    if rimpx: a = rim(a, rimpx)
    if shadow:
        from room import paper
        a = paper(a, 6, 8, 4, 0.5)
    return a

def rivet(x, y, r=6):
    return blob(x, y, r, r, "#c9a04a", 1.6, 8, 0.2) + f'<circle cx="{x-r*0.3:.1f}" cy="{y-r*0.35:.1f}" r="{r*0.35:.1f}" fill="#f6e2a0"/>' + line([(x - r * 0.5, y), (x + r * 0.5, y)], 1.0, 0.1, "#6a4a1a", 0.8)

def plate(w, h, face, lip, light, kind="paper", seed=1):
    """A chunky bevelled button: ink outline, a lighter top band, a darker lip under it, paper grain,
    brass rivets in the corners; 'wood' adds plank grain."""
    rnd(seed)
    pad = 14
    x0, y0, x1, y1 = pad, pad, w - pad, h - pad - 8
    r = 14
    def rr(x0, y0, x1, y1, r):
        pts = []
        for cx, cy, a0 in [(x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)]:
            for k in range(6):
                a = math.radians(a0 + k * 18)
                pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        return pts
    b = shape(rr(x0, y0 + 8, x1, y1 + 8, r), lip, 2.8, 0.8)                  # the lip / thickness
    b += shape(rr(x0, y0, x1, y1, r), face, 2.8, 0.8)
    b += shape(rr(x0 + 8, y0 + 6, x1 - 8, y0 + (y1 - y0) * 0.42, r - 6), light, 0, 0.6, line=False, op=0.55)  # top light
    b += ink(rough(rr(x0 + 6, y0 + 5, x1 - 6, y1 - 5, r - 5), True, 0.6), 1.2, 0.35, "#fff")              # inner edge
    if kind == "wood":
        for k in range(4):
            yy = y0 + 10 + k * (y1 - y0 - 16) / 3
            b += line([(x0 + 20, yy + jit(2)), (x1 - 20, yy + jit(2))], 1.0, 1.8, "#3a2416", 0.45)
        b += f'<ellipse cx="{x0+60}" cy="{(y0+y1)/2+6}" rx="10" ry="5" fill="#3a2416" opacity=".35"/>'
    if kind == "paper":
        for k in range(3):  # paper fold / crease
            xx = x0 + (x1 - x0) * (0.25 + 0.27 * k)
            b += line([(xx, y0 + 4), (xx + jit(6), y1 - 4)], 0.9, 1.0, "#000", 0.12)
    for (rx, ry) in [(x0 + 14, y0 + 13), (x1 - 14, y0 + 13), (x0 + 14, y1 - 12), (x1 - 14, y1 - 12)]:
        b += rivet(rx, ry, 5.5)
    return finish(b, w, h, seed)

BUTTONS = {  # name: (w, h, face, lip, light, kind)
    "btn_play":     (520, 120, "#a8392f", "#6a1a14", "#e0705a", "paper"),
    "btn_bots":     (520, 108, "#e9dcc2", "#a8946c", "#fff6e0", "paper"),
    "btn_tutorial": (520, 108, "#e0a23a", "#94601a", "#ffd68a", "paper"),
    "btn_small":    (188, 98, "#5b3d2c", "#2e1e14", "#8a6248", "wood"),
    "btn_quit":     (212, 92, "#2a2a2c", "#141416", "#56565a", "wood"),
    "btn_shop":     (300, 96, "#36585f", "#1a2e32", "#6a9aa2", "paper"),
}

def mirror(w=520, h=720):
    """A Filipino vanity mirror on the wall: carved wood frame with a crest, tape on a crack, a brad at the top."""
    rnd(55)
    cx, cy = w / 2, h / 2 + 50
    rx, ry = 200, 255
    outer = ellipse_pts(cx, cy, rx + 42, ry + 42, 40)
    b = oshape(outer, "#5b3d2c", 3.0, 1.2)
    b += oshape(ellipse_pts(cx, cy, rx + 28, ry + 28, 40), "#7a5640", 2.0, 0.8)
    for k in range(16):  # carved beads round the frame
        a = math.radians(k * 22.5)
        b += blob(cx + (rx + 35) * math.cos(a), cy + (ry + 35) * math.sin(a), 7, 7, "#9a7350", 1.4, 10, 0.3)
    # crest on top
    b += oshape([(cx - 90, cy - ry - 30), (cx - 50, cy - ry - 80), (cx, cy - ry - 100), (cx + 50, cy - ry - 80), (cx + 90, cy - ry - 30)], "#7a5640", 2.6, 0.8)
    b += blob(cx, cy - ry - 62, 22, 22, "#f2c94c", 2.0) + f'<ellipse cx="{cx-7}" cy="{cy-ry-66}" rx="2.6" ry="4.4" fill="{INK}"/><ellipse cx="{cx+7}" cy="{cy-ry-66}" rx="2.6" ry="4.4" fill="{INK}"/>'
    b += line([(cx - 8, cy - ry - 55), (cx, cy - ry - 51), (cx + 8, cy - ry - 55)], 1.8, 0.2)
    for k in range(8):
        a = math.radians(k * 45)
        b += line([(cx + 27 * math.cos(a), cy - ry - 62 + 27 * math.sin(a)), (cx + 34 * math.cos(a), cy - ry - 62 + 34 * math.sin(a))], 2.0, 0.3, "#c48a1c")
    # glass: cut out later (drawn here only as an outline)
    b += ink(rough(ellipse_pts(cx, cy, rx, ry, 40), True, 0.6), 2.6)
    return b, (cx, cy, rx, ry)

def mirror_glass(w, h, geo):
    cx, cy, rx, ry = geo
    b = f'<defs><linearGradient id="gl" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#9cb2b4" stop-opacity=".0"/><stop offset=".5" stop-color="#d8e8e6" stop-opacity=".0"/><stop offset="1" stop-color="#2a3a3c" stop-opacity=".35"/></linearGradient></defs>'
    b += f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="url(#gl)"/>'
    for k, (ox, wd) in enumerate([(-120, 34), (-70, 12), (90, 20)]):  # reflection streaks
        b += f'<path d="M{cx+ox} {cy-ry+40} l{wd} 0 l-140 {ry*1.4} l{-wd} 0z" fill="#ffffff" opacity="{0.16 if k != 1 else 0.1}" clip-path="url(#gc)"/>'
    b = b.replace("<defs>", f'<defs><clipPath id="gc"><ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}"/></clipPath>')
    b += line([(cx + 60, cy + 120), (cx + 110, cy + 60), (cx + 150, cy + 80)], 1.2, 0.6, "#ffffff", 0.6)  # hairline crack
    b += f'<rect x="{cx+95}" y="{cy+70}" width="44" height="14" fill="#d8ceb0" opacity=".85" transform="rotate(-30 {cx+117} {cy+77})"/>'
    return b

def plaque(w=360, h=96):
    rnd(61)
    b = shape([(14, 20), (w - 14, 14), (w - 10, h - 18), (10, h - 14)], "#e9dcc2", 2.6, 1.4)
    b += shape([(22, 26), (w - 22, 22), (w - 20, h - 24), (20, h - 20)], "#e9dcc2", 1.2, 0.8, line=True)
    b += rivet(30, h / 2, 5) + rivet(w - 30, h / 2, 5)
    b += line([(w / 2 - 70, 16), (w / 2, -20), (w / 2 + 70, 16)], 1.6, 0.4)  # hanging string
    return finish(b, w, h + 30, 7, rimpx=3)

def shop(w=620, h=380):
    """The customisation shop as a little sari-sari stall: red awning, TINDAHAN sign, a shelf of locked slots."""
    rnd(71)
    b = box(20, 60, w - 40, h - 80, "#5b3d2c", 3.0, 1.4)                     # back board
    for k in range(5): b += line([(30, 90 + k * 56), (w - 30, 88 + k * 56)], 1.0, 1.6, "#3a2416", 0.4)
    # awning
    aw = [(6, 74), (w - 6, 74), (w - 26, 20), (26, 20)]
    b += shape(aw, C["brick"], 2.8, 1.2)
    for k in range(8):
        x = 40 + k * (w - 80) / 7
        b += line([(x, 22), (x - 6, 74)], 8, 0.4, "#e9dcc2", 0.85)
    for k in range(13):  # scalloped edge
        x = 6 + k * (w - 12) / 13
        b += blob(x + (w - 12) / 26, 76, (w - 12) / 26, 10, C["brick"], 1.8, 12, 0.3)
    # 2 x 4 slots
    for r in range(2):
        for c in range(4):
            x = 52 + c * 134; y = 112 + r * 124
            b += box(x, y, 110, 100, "#e9dcc2", 2.2, 0.8)
            b += box(x + 8, y + 8, 94, 70, "#cdbf9e", 1.2, 0.5)
            # padlock
            lx, ly = x + 55, y + 46
            b += line([(lx - 13, ly - 4), (lx - 13, ly - 16), (lx, ly - 26), (lx + 13, ly - 16), (lx + 13, ly - 4)], 4, 0.4, "#5a5a5e")
            b += box(lx - 20, ly - 6, 40, 30, "#c9a04a", 2.0, 0.5) + blob(lx, ly + 6, 4, 4, INK, 0.8)
        b += box(36, 214 + r * 124, w - 72, 12, "#7a5640", 2.2, 0.6)        # shelf plank
    return finish(b, w, h, 9, rimpx=3)

def screen_frame(W_=1920, H_=1080, t=26):
    """A plain wooden frame round the whole screen with brass corner plates; the middle is see-through."""
    rnd(81)
    b = f'<path d="M0 0 H{W_} V{H_} H0Z M{t} {t} V{H_-t} H{W_-t} V{t}Z" fill="#3e2a1f" fill-rule="evenodd"/>'
    b += ink(rough(rect(t, t, W_ - 2 * t, H_ - 2 * t), True, 1.0), 2.6) + ink(rough(rect(t - 8, t - 8, W_ - 2 * t + 16, H_ - 2 * t + 16), True, 1.0), 1.4, 0.6, "#a07a58")
    for k in range(9):
        y = 10 + k * 120
        b += line([(4, y), (20, y + 60)], 1.0, 1.0, "#2a1c14", 0.6) + line([(W_ - 20, y), (W_ - 4, y + 60)], 1.0, 1.0, "#2a1c14", 0.6)
    for (x, y, sx, sy) in [(0, 0, 1, 1), (W_, 0, -1, 1), (0, H_, 1, -1), (W_, H_, -1, -1)]:
        b += shape([(x, y), (x + sx * 110, y), (x + sx * 110, y + sy * 34), (x + sx * 34, y + sy * 34), (x + sx * 34, y + sy * 110), (x, y + sy * 110)], "#b08a48", 2.4, 0.6)
        b += rivet(x + sx * 18, y + sy * 18, 6) + rivet(x + sx * 80, y + sy * 18, 4.5) + rivet(x + sx * 18, y + sy * 80, 4.5)
    a = render(b, W_, H_)
    a = texture(a, age=0.3, grime=0.25, stain=0.05, seed=12, grain_amt=0.03, mottle_scale=60)
    # inner shadow into the picture + slight vignette (kept see-through)
    yy, xx = np.mgrid[:H_, :W_].astype(np.float32)
    d = np.minimum.reduce([xx - t, yy - t, W_ - t - xx, H_ - t - yy])
    inner = np.clip(1 - d / 26, 0, 1) ** 2 * 0.55 * (d > 0)
    vx = ((xx - W_ / 2) / (W_ / 2)) ** 2 + ((yy - H_ * 0.48) / (H_ / 2)) ** 2
    vig = np.clip((np.sqrt(vx) - 0.62) / 0.6, 0, 1) ** 1.5 * 0.42 * (d > 0)
    al = np.maximum(a[:, :, 3], np.maximum(inner, vig))
    rgb = np.where(a[:, :, 3:4] > 0.01, a[:, :, :3], np.zeros(3) + 0.03)
    out = np.dstack([rgb, al]).astype(np.float32)
    return out

if __name__ == "__main__":
    for name, (w, h, f, l, li, kind) in BUTTONS.items():
        save(plate(w, h, f, l, li, kind, seed=hash(name) % 100), f"{OUT}{name}.png")
    mb, geo = mirror()
    m = finish(mb, 520, 720, 5, rimpx=4)
    cx, cy, rx, ry = geo
    yy, xx = np.mgrid[:720, :520].astype(np.float32)
    hole = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 < 1
    m[hole, 3] = 0
    save(m, f"{OUT}mirror_frame.png")
    save(render(mirror_glass(520, 720, geo), 520, 720), f"{OUT}mirror_glass.png")
    # mirror backing: dim bluish glass behind the avatar
    back = render(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#3a4a4c"/>', 520, 720)
    back = texture(back, age=0.4, grime=0.3, stain=0.1, seed=3)
    save(back, f"{OUT}mirror_back.png")
    save(plaque(), f"{OUT}plaque.png")
    save(shop(), f"{OUT}shop.png")
    save(screen_frame(), f"{OUT}screen_frame.png")
    print("ui3 ok", geo)
