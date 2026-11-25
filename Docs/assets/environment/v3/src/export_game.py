"""Exports the v3.3 art for Unity into Assets/_Project/Resources, plus a layout file for the C# side."""
import os, sys, json, math
import numpy as np
from PIL import Image
import room, outside
from style import save, render, texture, comic, rim, over, ink, rough, ellipse_pts, oshape, line, blob, shape, box, text, rnd
from style import P as C
room.NO_NUMBERS = True
RES = "/home/claude/vartcade-studios-andre/Assets/_Project/Resources/"
lay = {}

def crop_save(a, path, name):
    al = a[:, :, 3] > 0.01
    ys, xs = np.nonzero(al)
    x0, x1, y0, y1 = int(xs.min()), int(xs.max()) + 1, int(ys.min()), int(ys.max()) + 1
    Image.fromarray((np.clip(a[y0:y1, x0:x1], 0, 1) * 255).astype(np.uint8), "RGBA").save(path, optimize=True)
    lay[name] = [x0, y0, x1 - x0, y1 - y0]   # 1920x1080 space, y from the top

def jpg(a, path):
    Image.fromarray((np.clip(a[:, :, :3], 0, 1) * 255).astype(np.uint8)).save(path, quality=86, optimize=True)

def export_room():
    d = RES + "RoomV3/"; os.makedirs(d, exist_ok=True)
    jobs = [("day1", dict(day=1)), ("day5", dict(day=5)), ("day9", dict(day=9)), ("night", dict(day=5, night=True))]
    for r in ["purge", "trading_only", "hunger", "tornado", "doomsday", "blackout", "outbreak", "peaceful"]:
        jobs.append((r, dict(day=4, disaster=r)))
    only = sys.argv[2:] if len(sys.argv) > 2 else None
    for tag, kw in jobs:
        if only and tag not in only: continue
        base, layers = room.compose(parts=True, **kw)
        jpg(base, f"{d}room_{tag}.jpg")
        if tag == "day1":
            for k in ("table", "lamp", "fan", "spider"):
                crop_save(layers[k], f"{d}room_{k}.png", f"room_{k}")
        if tag == "night":
            for k in ("table", "lamp"):
                crop_save(layers[k], f"{d}room_{k}_night.png", f"room_{k}_night")
        print("room", tag, flush=True)

def export_street():
    d = RES + "StreetV3/"; os.makedirs(d, exist_ok=True)
    base, layers = outside.compose(day=1, parts=True)
    jpg(base, f"{d}street_base.jpg")
    for k, a in layers.items():
        crop_save(a, f"{d}street_{k}.png", f"street_{k}")
    print("street ok")

def export_ui():
    d = RES + "UI3/"; os.makedirs(d, exist_ok=True)
    src = "/home/claude/envd/project/v3/ui3/"
    for f in os.listdir(src):
        Image.open(src + f).save(d + f, optimize=True)
    print("ui ok")

def export_dial():
    d = RES + "Dial/"; os.makedirs(d, exist_ok=True)
    S = 512
    # disc: day sky on top with the sun, night sky below with the moon and stars (turns 180 degrees)
    b = f'<defs><linearGradient id="dg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6a8ab0"/><stop offset=".75" stop-color="#e9a868"/><stop offset="1" stop-color="#f6c47a"/></linearGradient>' \
        f'<linearGradient id="ng" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="#05070f"/><stop offset="1" stop-color="#1a2440"/></linearGradient></defs>'
    b += f'<rect x="0" y="0" width="{S}" height="{S/2}" fill="url(#dg)"/><rect x="0" y="{S/2}" width="{S}" height="{S/2}" fill="url(#ng)"/>'
    b += blob(S / 2, S * 0.22, 40, 40, "#f6a24a", 4)
    rnd(3)
    import random
    random.seed(5)
    for _ in range(26):
        x, y = random.uniform(60, S - 60), random.uniform(S * 0.56, S * 0.92)
        b += f'<rect x="{x:.0f}" y="{y:.0f}" width="6" height="6" fill="#efe6cf"/>'
    b += f'<g transform="rotate(180 {S/2} {S/2})">' + blob(S / 2, S * 0.22, 34, 34, "#e8e2cc", 4) + f'<circle cx="{S/2+14}" cy="{S*0.22-10}" r="28" fill="#1a2440"/></g>'
    save(render(b, S, S), d + "dial_disc.png")
    # horizon: rooftops with two lit windows
    h = f'<path d="M0 {S*0.56} L60 {S*0.5} L100 {S*0.53} L150 {S*0.47} L200 {S*0.52} L256 {S*0.45} L310 {S*0.51} L360 {S*0.47} L420 {S*0.52} L{S} {S*0.49} L{S} {S} L0 {S}Z" fill="#160e10"/>'
    h += f'<rect x="130" y="{S*0.6}" width="18" height="22" fill="#f2c46a" opacity=".8"/><rect x="330" y="{S*0.58}" width="18" height="22" fill="#f2c46a" opacity=".8"/>'
    save(render(h, S, S), d + "dial_horizon.png")
    # frame: wooden ring with brass studs; see-through middle
    fr = f'<path d="M{S/2} 8 A{S/2-8} {S/2-8} 0 1 1 {S/2-0.1} 8Z M{S/2} 70 A{S/2-70} {S/2-70} 0 1 0 {S/2+0.1} 70Z" fill="#5b3d2c" fill-rule="evenodd"/>'
    fr += ink(rough(ellipse_pts(S / 2, S / 2, S / 2 - 8, S / 2 - 8, 48), True, 0.8), 5) + ink(rough(ellipse_pts(S / 2, S / 2, S / 2 - 70, S / 2 - 70, 48), True, 0.8), 5)
    fr += ink(rough(ellipse_pts(S / 2, S / 2, S / 2 - 40, S / 2 - 40, 48), True, 0.6), 3, 0.5, "#a07a58")
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        x, y = S / 2 + (S / 2 - 39) * math.cos(a), S / 2 + (S / 2 - 39) * math.sin(a)
        fr += blob(x, y, 11, 11, "#c9a04a", 2.4) + f'<circle cx="{x-3:.1f}" cy="{y-4:.1f}" r="4" fill="#f6e2a0"/>'
    a = render(fr, S, S); a = texture(a, age=0.3, grime=0.2, stain=0.05, seed=4); a = rim(comic(a, seed=2), 5)
    save(a, d + "dial_frame.png")
    # one tick
    save(render(f'<rect x="4" y="4" width="16" height="40" rx="8" fill="#f2c94c" stroke="#1e1411" stroke-width="4"/>', 24, 48), d + "dial_tick.png")
    print("dial ok")

def export_wheel():
    d = RES + "Wheel/"; os.makedirs(d, exist_ok=True)
    S = 512; c = S / 2; R = 226
    names = ["purge", "trading_only", "hunger", "tornado", "doomsday", "blackout", "outbreak", "peaceful"]
    cols = ["#e9dcc2", "#36585f", "#e9dcc2", "#a8392f", "#e9dcc2", "#36585f", "#e9dcc2", "#a8392f"]
    b = ""
    for i in range(8):  # wedge i spans i*45 .. (i+1)*45 clockwise from the top (as RondoDial expects)
        a0, a1 = math.radians(i * 45 - 90), math.radians(i * 45 + 45 - 90)
        b += f'<path d="M{c} {c} L{c+R*math.cos(a0):.2f} {c+R*math.sin(a0):.2f} A{R} {R} 0 0 1 {c+R*math.cos(a1):.2f} {c+R*math.sin(a1):.2f}Z" fill="{cols[i]}" stroke="#1e1411" stroke-width="5"/>'
    face = render(b, S, S)
    face = texture(face, age=0.2, grime=0.15, stain=0.03, seed=8)
    badges = RES + "Rondo/"
    for i, n in enumerate(names):
        p = badges + f"rondo_{n}.png"
        if not os.path.exists(p): continue
        im = Image.open(p).convert("RGBA").resize((118, 118), Image.LANCZOS)
        am = math.radians(i * 45 + 22.5 - 90)
        im = im.rotate(-(i * 45 + 22.5), resample=Image.BICUBIC, expand=True)
        x = int(c + 150 * math.cos(am) - im.width / 2); y = int(c + 150 * math.sin(am) - im.height / 2)
        lay_ = np.zeros_like(face); lay_[y:y + im.height, x:x + im.width] = np.asarray(im).astype(np.float32) / 255
        face = over(face, lay_)
    face = comic(face, seed=3)
    save(face, d + "wheel_face.png")
    for k, name in enumerate(("wheel_rim_a", "wheel_rim_b")):
        r = f'<path d="M{c} 4 A{c-4} {c-4} 0 1 1 {c-0.1} 4Z M{c} {c-R+2} A{R-2} {R-2} 0 1 0 {c+0.1} {c-R+2}Z" fill="#3e2a1f" fill-rule="evenodd"/>'
        r += ink(rough(ellipse_pts(c, c, c - 4, c - 4, 60), True, 0.6), 5)
        for j in range(16):
            a = math.radians(j * 22.5)
            x, y = c + (c - 17) * math.cos(a), c + (c - 17) * math.sin(a)
            on = (j % 2 == k)
            r += blob(x, y, 9, 9, "#ffe08a" if on else "#7a6a40", 2.2)
            if on: r += f'<circle cx="{x:.1f}" cy="{y:.1f}" r="16" fill="#ffe08a" opacity=".25"/>'
        a_ = render(r, S, S); a_ = texture(a_, age=0.25, grime=0.2, stain=0.04, seed=9 + k); a_ = rim(a_, 5)
        save(a_, d + name + ".png")
    hub = blob(64, 64, 44, 44, "#f2c94c", 4) + f'<ellipse cx="52" cy="58" rx="5" ry="9" fill="#1e1411"/><ellipse cx="76" cy="58" rx="5" ry="9" fill="#1e1411"/>' + line([(48, 78), (64, 88), (80, 78)], 4, 0.3)
    for j in range(12):
        a = math.radians(j * 30); hub += line([(64 + 50 * math.cos(a), 64 + 50 * math.sin(a)), (64 + 60 * math.cos(a), 64 + 60 * math.sin(a))], 5, 0.3, "#c48a1c")
    save(rim(render(hub, 128, 128), 4), d + "wheel_hub.png")
    ptr = shape([(10, 8), (70, 8), (40, 92)], "#d8b04a", 5, 0.6) + blob(40, 24, 9, 9, "#8a6a2a", 2)
    save(rim(render(ptr, 80, 100), 4), d + "wheel_pointer.png")
    print("wheel ok")

def export_overlays():
    d = RES + "Overlays/"; os.makedirs(d, exist_ok=True)
    from style import fbm
    n = 512
    fib = fbm(n, n, 40, 4, 11)
    fib = np.asarray(Image.fromarray((fib * 255).astype(np.uint8)).resize((n, n))).astype(np.float32) / 255
    streak = np.asarray(Image.fromarray((fbm(n, n // 8, 6, 3, 12) * 255).astype(np.uint8)).resize((n, n))).astype(np.float32) / 255
    a = np.clip((fib * 0.6 + streak * 0.4 - 0.45) * 1.6, 0, 1) * 0.5
    paper = np.dstack([np.full((n, n), 0.3), np.full((n, n), 0.22), np.full((n, n), 0.14), a]).astype(np.float32)
    save(paper, d + "ov_paper.png")
    # dusk: purple at the top to orange at the bottom
    g = np.zeros((256, 4, 4), np.float32)
    for y in range(256):
        t = y / 255
        g[y, :, :3] = np.array([0.36, 0.2, 0.55]) * (1 - t) + np.array([0.95, 0.55, 0.22]) * t
        g[y, :, 3] = 1
    save(g, d + "ov_dusk.png")
    # night: dark blue everywhere except a pool round the lamp (centre at 50%, 28%)
    W, H = 480, 270
    yy, xx = np.mgrid[:H, :W].astype(np.float32)
    dd = np.sqrt(((xx - W * 0.5) / (W * 0.5)) ** 2 + ((yy - H * 0.28) / (H * 0.6)) ** 2)
    al = np.clip((dd - 0.18) / 0.6, 0, 1) ** 1.2
    night = np.dstack([np.full((H, W), 0.03), np.full((H, W), 0.05), np.full((H, W), 0.14), al * 0.85]).astype(np.float32)
    save(night, d + "ov_night.png")
    print("overlays ok")

if __name__ == "__main__":
    what = sys.argv[1]
    {"room": export_room, "street": export_street, "ui": export_ui, "dial": export_dial, "wheel": export_wheel, "overlays": export_overlays}[what]()
    if lay:
        jp = f"/home/claude/envgen/v3/layout_{what}_{'_'.join(sys.argv[2:]) or 'all'}.json"
        json.dump(lay, open(jp, "w"), indent=1)
