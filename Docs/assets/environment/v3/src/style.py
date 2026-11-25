"""v3 drawing kit: thin sketchy ink, wobbly shapes, and raster texture passes (grime, stains, grain, rust).
Everything is drawn at 1920x1080 so the same layers can go straight into Unity."""
import math, random, io
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
import cairosvg

INK = "#1e1411"
W, H = 1920, 1080
R = random.Random(11)

# ---------- palette (muted, grimy; sampled by eye from the team's cards and the reference mood) ----------
P = dict(
    wood="#5b3d2c", wood_dk="#3e2a1f", wood_lt="#7a5640", plaster="#8a7a62", plaster_dk="#6b5d4a",
    concrete="#6d6c62", concrete_dk="#4f4e47", metal="#7d8584", metal_dk="#555c5c", rust="#8a4a25", rust_lt="#b8652e",
    cream="#e9dcc2", paper="#d9c9a6", teal="#4e7c86", teal_dk="#36585f", brick="#a8392f", gold="#e0a23a",
    orange="#d07a2c", tuna="#5b63b8", tuna_dk="#3c4290", pink="#c96aa0", pink_dk="#8f4673", aid="#d6bf8a",
    green="#5d7146", moss="#7f9a4a", night="#141a2a", glass="#c9d6cf",
)

def rnd(seed):
    global R
    R = random.Random(seed)

def jit(a):
    return R.uniform(-a, a)

def _sub(pts, closed, step):
    out = []
    n = len(pts)
    rng = range(n) if closed else range(n - 1)
    for i in rng:
        x0, y0 = pts[i]; x1, y1 = pts[(i + 1) % n]
        L = math.hypot(x1 - x0, y1 - y0); k = max(1, int(L / step))
        for j in range(k):
            t = j / k
            out.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t))
    if not closed: out.append(pts[-1])
    return out

def smooth(pts, closed=True, amp=1.2):
    """A wobbly Catmull-Rom curve through the points (for organic shapes: feathers, sacks, hair)."""
    p = [(x + jit(amp), y + jit(amp)) for x, y in pts]
    n = len(p)
    if n < 3: return rough(pts, closed, amp)
    d = f"M{p[0][0]:.1f} {p[0][1]:.1f}"
    rng = range(n) if closed else range(n - 1)
    for i in rng:
        p0 = p[(i - 1) % n] if (closed or i > 0) else p[i]
        p1 = p[i]; p2 = p[(i + 1) % n]
        p3 = p[(i + 2) % n] if (closed or i + 2 < n) else p2
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d += f" C{c1[0]:.1f} {c1[1]:.1f} {c2[0]:.1f} {c2[1]:.1f} {p2[0]:.1f} {p2[1]:.1f}"
    return d + (" Z" if closed else "")

def oshape(pts, fill, w=2.6, amp=1.0, line=True, op=1.0):
    """Organic filled shape with a curved, sketchy outline."""
    s = f'<path d="{smooth(pts, True, amp)}" fill="{fill}" opacity="{op}"/>'
    return s + (ink(smooth(pts, True, amp * 0.7), w) if line else "")

def ocurve(pts, w=2.4, amp=0.8, color=INK, op=1.0):
    return ink(smooth(pts, False, amp), w, op, color)

def rough(pts, closed=True, amp=1.6, step=28):
    """Wobbly path through the points: subdivide, jitter, straight segments."""
    p = _sub(pts, closed, step)
    p = [(x + jit(amp), y + jit(amp)) for x, y in p]
    if len(p) < 3:
        return "M" + " L".join(f"{x:.1f} {y:.1f}" for x, y in p)
    # straight wobbly segments keep corners sharp (curves rounded small boxes into "D" shapes)
    d = f"M{p[0][0]:.1f} {p[0][1]:.1f}" + "".join(f" L{x:.1f} {y:.1f}" for x, y in p[1:])
    return d + (" Z" if closed else "")

def ink(d, w=2.6, op=1.0, color=INK):
    """A sketchy line: a main stroke plus a fainter, slightly offset second pass."""
    dx, dy = jit(1.2), jit(1.2)
    return (f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{w}" stroke-linecap="round" stroke-linejoin="round" opacity="{op}"/>'
            f'<path d="{d}" transform="translate({dx:.1f} {dy:.1f})" fill="none" stroke="{color}" stroke-width="{w*0.45:.2f}" stroke-linecap="round" opacity="{0.45*op:.2f}"/>')

def shape(pts, fill, w=2.6, amp=1.6, line=True, op=1.0, extra=""):
    d = rough(pts, True, amp)
    s = f'<path d="{d}" fill="{fill}" opacity="{op}" {extra}/>'
    return s + (ink(rough(pts, True, amp * 0.8), w) if line else "")

def line(pts, w=2.4, amp=1.2, color=INK, op=1.0):
    return ink(rough(pts, False, amp), w, op, color)

def rect(x, y, w, h):
    return [(x, y), (x + w, y), (x + w, y + h), (x, y + h)]

def box(x, y, w, h, fill, lw=2.6, amp=1.4, **k):
    return shape(rect(x, y, w, h), fill, lw, amp, **k)

def ellipse_pts(cx, cy, rx, ry, n=28, rot=0):
    c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    out = []
    for i in range(n):
        a = 2 * math.pi * i / n
        x, y = rx * math.cos(a), ry * math.sin(a)
        out.append((cx + x * c - y * s, cy + x * s + y * c))
    return out

def blob(cx, cy, rx, ry, fill, lw=2.4, n=24, amp=1.2):
    return shape(ellipse_pts(cx, cy, rx, ry, n), fill, lw, amp)

def hatch(x, y, w, h, gap=7, ang=45, op=0.35, lw=1.2, color=INK, clip=None):
    """Loose pencil hatching inside a box (for shade under shelves, corners)."""
    out = []
    a = math.radians(ang); dx = math.cos(a); dy = math.sin(a)
    L = w + h
    for i in range(int(-L / gap), int(L / gap)):
        ox = x + w / 2 + i * gap * dy; oy = y + h / 2 - i * gap * dx
        p0 = (ox - dx * L, oy - dy * L); p1 = (ox + dx * L, oy + dy * L)
        out.append(f'M{p0[0]+jit(1):.1f} {p0[1]+jit(1):.1f} L{p1[0]+jit(1):.1f} {p1[1]+jit(1):.1f}')
    cid = f"hc{R.randint(0, 10**9)}"
    cl = clip or f'<rect x="{x}" y="{y}" width="{w}" height="{h}"/>'
    return (f'<clipPath id="{cid}">{cl}</clipPath><g clip-path="url(#{cid})"><path d="{" ".join(out)}" stroke="{color}" '
            f'stroke-width="{lw}" opacity="{op}" fill="none"/></g>')

def text(x, y, s, size, fill, font="Anton", anchor="middle", rot=0, op=1, weight="normal", stroke=None, sw=0):
    st = f' stroke="{stroke}" stroke-width="{sw}" paint-order="stroke"' if stroke else ""
    return (f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" font-weight="{weight}" fill="{fill}" opacity="{op}" '
            f'text-anchor="{anchor}" transform="rotate({rot} {x} {y})"{st}>{s}</text>')

# ---------- pixel bits (fine: 3 px cells at 1920 wide) ----------
FONT = {'A':"010101111101101",'B':"110101110101110",'C':"011100100100011",'D':"110101101101110",'E':"111100110100111",'F':"111100110100100",
'G':"011100101101011",'H':"101101111101101",'I':"111010010010111",'K':"101101110101101",'L':"100100100100111",'M':"101111111101101",
'N':"110101101101101",'O':"010101101101010",'P':"110101110100100",'R':"110101110101101",'S':"011100010001110",'T':"111010010010010",
'U':"101101101101111",'W':"101101111111101",'Y':"101101010010010",'0':"111101101101111",'1':"010110010010111",'2':"110001010100111",
'3':"110001010001110",'4':"101101111001001",'5':"111100110001110",'6':"011100111101111",'7':"111001010010010",'8':"111101111101111",
'9':"111101111001110",':':"000010000010000",'-':"000000111000000",' ':"000000000000000",'!':"010010010000010",'.':"000000000000010"}

def px(rows, pal, x, y, s=3, cls="", op=1):
    out = []
    for j, r in enumerate(rows):
        i = 0
        while i < len(r):
            c = r[i]
            if c in ". ": i += 1; continue
            k = i
            while k < len(r) and r[k] == c: k += 1
            out.append(f'<rect x="{x+i*s}" y="{y+j*s}" width="{(k-i)*s}" height="{s}" fill="{pal[c]}"/>')
            i = k
    c = f' class="{cls}"' if cls else ""
    return f'<g shape-rendering="crispEdges" opacity="{op}"{c}>' + "".join(out) + "</g>"

def ptext(t, color, x, y, s=3):
    rows = ["" for _ in range(5)]
    for ch in t.upper():
        g = FONT.get(ch, FONT[' '])
        for r in range(5): rows[r] += g[r * 3:r * 3 + 3].replace('1', 'a').replace('0', '.') + '.'
    return px(rows, {'a': color}, x, y, s)

def dither(x, y, w, h, color, s=3, direction="down", op=0.8, strength=0.85):
    out = []; n = int(h // s)
    B = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    for j in range(n):
        t = j / max(1, n - 1)
        if direction == "up": t = 1 - t
        for i in range(int(w // s)):
            if B[j % 4][i % 4] / 16 < t * strength:
                out.append(f'<rect x="{x+i*s}" y="{y+j*s}" width="{s}" height="{s}"/>')
    return f'<g shape-rendering="crispEdges" fill="{color}" opacity="{op}">' + "".join(out) + "</g>"

# ---------- rendering + texture passes ----------
def svg_doc(body, w=W, h=H, defs=""):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><defs>{defs}</defs>{body}</svg>'

def render(body, w=W, h=H, defs=""):
    png = cairosvg.svg2png(bytestring=svg_doc(body, w, h, defs).encode())
    return np.asarray(Image.open(io.BytesIO(png)).convert("RGBA")).astype(np.float32) / 255.0

def fbm(h, w, scale=180, octaves=5, seed=0):
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32); amp = 1.0; tot = 0
    for o in range(octaves):
        s = max(2, scale / (2 ** o))
        gh, gw = int(h / s) + 3, int(w / s) + 3
        g = rng.random((gh, gw)).astype(np.float32)
        z = ndimage.zoom(g, (h / (gh - 2), w / (gw - 2)), order=3)[:h, :w]
        out += z * amp; tot += amp; amp *= 0.5
    out /= tot
    return (out - out.min()) / (out.max() - out.min() + 1e-6)

def drips(h, w, n=40, seed=1, top=0, maxlen=520):
    rng = np.random.default_rng(seed)
    m = np.zeros((h, w), np.float32)
    for _ in range(n):
        x = int(rng.integers(0, w)); y0 = int(top + rng.integers(0, 60)); L = int(rng.integers(80, maxlen)); wd = int(rng.integers(2, 9))
        y1 = min(h, y0 + L)
        prof = np.linspace(1, 0, y1 - y0) ** 1.5
        x0 = max(0, x - wd); x1 = min(w, x + wd)
        m[y0:y1, x0:x1] = np.maximum(m[y0:y1, x0:x1], prof[:, None] * rng.uniform(0.4, 1.0))
    return ndimage.gaussian_filter(m, 2.2)

def grain(h, w, seed=3, amt=0.06):
    rng = np.random.default_rng(seed)
    return (rng.standard_normal((h, w)).astype(np.float32) * amt)

def halftone(h, w, cell=7, cover=None):
    """Card-style print dots; cover (0-1 map) decides dot size."""
    yy, xx = np.mgrid[:h, :w].astype(np.float32)
    cx = (xx % cell) - cell / 2; cy = (yy % cell) - cell / 2
    d = np.sqrt(cx * cx + cy * cy) / (cell / 2)
    if cover is None: cover = np.ones((h, w), np.float32) * 0.5
    return (d < cover * 1.1).astype(np.float32)

def texture(layer, age=0.3, grime=0.35, stain=0.25, seed=5, rust=None, grain_amt=0.045, mottle_scale=220):
    """Dirty a layer: mottled grime, drips from the top, fine grain. age 0..1 makes it worse."""
    a = layer.copy(); h, w = a.shape[:2]
    mot = fbm(h, w, mottle_scale, 5, seed)
    fine = fbm(h, w, 26, 3, seed + 1)
    dark = 1 - grime * (0.55 + 0.6 * age) * (mot * 0.7 + fine * 0.3)
    st = drips(h, w, int(30 + 50 * age), seed + 2) * stain * (0.6 + age)
    dark = dark * (1 - st * 0.55)
    rgb = a[:, :, :3] * dark[..., None]
    # a warm yellow-brown cast in the stains
    rgb = rgb * (1 - st[..., None] * 0.25) + np.array([0.33, 0.25, 0.12]) * st[..., None] * 0.25
    rgb += grain(h, w, seed + 3, grain_amt)[..., None]
    if rust is not None:
        n = fbm(h, w, 14, 3, seed + 9) * 0.6 + fbm(h, w, 60, 3, seed + 10) * 0.4
        rmask = (n > (0.74 - 0.12 * age)).astype(np.float32) * rust
        rmask = ndimage.gaussian_filter(rmask, 0.8) * (layer[:, :, 3] > 0.5)
        rgb = rgb * (1 - rmask[..., None]) + np.array([0.54, 0.29, 0.14]) * rmask[..., None]
    a[:, :, :3] = np.clip(rgb, 0, 1)
    return a

def over(dst, src):
    sa = src[:, :, 3:4]
    out = dst.copy()
    out[:, :, :3] = src[:, :, :3] * sa + dst[:, :, :3] * (1 - sa)
    out[:, :, 3:4] = sa + dst[:, :, 3:4] * (1 - sa)
    return out

def save(a, path, size=None):
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "RGBA")
    if size: im = im.resize(size, Image.LANCZOS)
    im.save(path, optimize=True)
    return im

def grade(a, tint=(1, 1, 1), lift=0.0, sat=1.0, vignette=0.0, warm=0.0):
    rgb = a[:, :, :3].copy()
    g = rgb.mean(axis=2, keepdims=True)
    rgb = g + (rgb - g) * sat
    rgb = rgb * np.array(tint) + lift
    if warm: rgb = rgb * (1 - warm) + (rgb * np.array([1.08, 0.98, 0.84])) * warm
    if vignette:
        h, w = rgb.shape[:2]; yy, xx = np.mgrid[:h, :w].astype(np.float32)
        d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h * 0.45) / (h / 2)) ** 2)
        v = np.clip((d - 0.55) / 0.75, 0, 1) ** 1.6 * vignette
        rgb = rgb * (1 - v[..., None])
    out = a.copy(); out[:, :, :3] = np.clip(rgb, 0, 1)
    return out

def glow(h, w, cx, cy, r, color, strength):
    yy, xx = np.mgrid[:h, :w].astype(np.float32)
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / r
    g = np.clip(1 - d, 0, 1) ** 2 * strength
    return g[..., None] * np.array(color, np.float32)

# ---------- v3.2: paper / comic / puppet pass ----------
def comic(layer, cell=6, strength=0.28, seed=0):
    """Print-dot shading in the darker parts (comic feel) plus a faint paper fibre."""
    a = layer.copy(); h, w = a.shape[:2]
    L = a[:, :, :3].mean(axis=2)
    cover = np.clip((0.42 - L) / 0.3, 0, 1) * (a[:, :, 3] > 0.5)
    dots = halftone(h, w, cell, cover * 0.85)
    a[:, :, :3] *= (1 - dots * strength)[..., None]
    fib = fbm(h, w, 30, 3, seed + 21)
    fib = ndimage.zoom(fib[:, ::6], (1, 6), order=1)[:h, :w] if w >= 6 else fib
    a[:, :, :3] *= (1 - 0.07 * fib)[..., None]
    a[:, :, :3] = np.clip(a[:, :, :3], 0, 1)
    return a

def rim(layer, px_=3, col=(0.93, 0.89, 0.8), op=0.95):
    """A pale cut-paper edge round every shape in the layer."""
    al = layer[:, :, 3] > 0.5
    solid = ndimage.binary_opening(al, iterations=3)   # thin lines (wires, cords) get no paper edge
    ring = ndimage.binary_dilation(solid, iterations=px_) & ~al
    out = layer.copy()
    out[ring, :3] = np.array(col); out[ring, 3] = op
    return out
