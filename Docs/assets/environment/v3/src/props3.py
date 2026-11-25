"""P01-P20 redrawn in the v3 style (thin sketchy ink, grime). Each prop is a function drawing at (x, y) with scale s,
in a 400 x 300 local box, so the outside scene can reuse them."""
import math
from style import *
from style import P as C

def T(x, y, s, body):
    return f'<g transform="translate({x} {y}) scale({s})">{body}</g>'

def radio(x=0, y=0, s=1):
    rnd(101)
    b = shape([(60, 110), (340, 104), (346, 250), (56, 256)], C["brick"], 2.6, 2.0)
    b += shape([(70, 90), (330, 86), (340, 110), (60, 112)], "#7d2a22", 2.2, 1.2)
    b += blob(140, 182, 52, 52, "#2a1c14", 2.4)
    for i in range(5):
        for j in range(5):
            if (i - 2) ** 2 + (j - 2) ** 2 <= 5: b += f'<circle cx="{116+i*12}" cy="{158+j*12}" r="3.4" fill="#5e4333"/>'
    b += box(220, 140, 100, 34, C["cream"], 2.0, 0.8)
    for i in range(9): b += line([(228 + i * 10, 150), (228 + i * 10, 166 - (i % 2) * 6)], 1.0, 0.2)
    b += line([(262, 142), (262, 172)], 2.2, 0.2, C["brick"])
    b += blob(240, 214, 16, 16, C["gold"], 2.0) + blob(298, 214, 16, 16, C["gold"], 2.0)
    b += line([(300, 90), (380, 6)], 2.6, 0.6) + f'<circle cx="381" cy="5" r="5" fill="{INK}"/>'
    b += box(150, 72, 110, 16, "#2a1c14", 1.6, 0.6)  # handle
    b += line([(80, 236), (120, 236)], 1.2, 0.6, "#e9dcc2", 0.4)
    return T(x, y, s, b)

def tabo_balde(x=0, y=0, s=1):
    rnd(102)
    b = shape([(70, 110), (250, 110), (232, 280), (88, 280)], "#3b7fb8", 2.6, 1.8)
    b += f'<ellipse cx="160" cy="110" rx="90" ry="16" fill="#2c6aa0"/>' + ink(rough(ellipse_pts(160, 110, 90, 16, 24)), 2.4)
    b += line([(80, 160), (240, 160)], 1.2, 1.0, "#1f4e78", 0.7) + line([(84, 210), (236, 210)], 1.2, 1.0, "#1f4e78", 0.7)
    b += line([(74, 110), (100, 40), (160, 24), (220, 40), (246, 110)], 2.4, 1.0)
    b += f'<ellipse cx="160" cy="112" rx="78" ry="11" fill="#7fb4d8" opacity=".6"/>'
    b += shape([(250, 190), (330, 186), (322, 270), (258, 272)], C["orange"], 2.4, 1.4)
    b += shape([(320, 200), (380, 196), (382, 214), (322, 218)], C["orange"], 2.2, 1.0)
    b += f'<ellipse cx="290" cy="188" rx="40" ry="8" fill="#a85a1c"/>'
    return T(x, y, s, b)

def banig(x=0, y=0, s=1):
    rnd(103)
    b = shape([(60, 150), (340, 140), (346, 244), (64, 254)], "#c9a46a", 2.6, 1.6)
    for i in range(12):
        xx = 80 + i * 22
        b += line([(xx, 146), (xx + 2, 250)], 1.6, 0.8, ["#a8392f", "#8a6a3a", "#36585f"][i % 3], 0.85)
    for j in range(4): b += line([(64, 168 + j * 22), (342, 160 + j * 22)], 1.0, 1.0, "#8a6a3a", 0.6)
    b += blob(62, 200, 22, 52, "#e0c08a", 2.4) + blob(62, 200, 12, 30, "#b8925a", 1.4) + blob(62, 200, 4, 10, "#6a4a2a", 1.0)
    b += line([(200, 140), (204, 250)], 3.0, 0.6, "#5a3a1a")  # tie
    return T(x, y, s, b)

def monobloc(x=0, y=0, s=1):
    rnd(104)
    b = shape([(130, 30), (270, 30), (262, 160), (138, 160)], "#efe9dc", 2.6, 1.6)
    for i in range(4): b += box(150, 50 + i * 24, 100, 10, "#d8d2c4", 1.2, 0.6)
    b += shape([(110, 160), (290, 160), (300, 190), (100, 190)], "#e6e0d2", 2.6, 1.4)
    for p in [[(112, 190), (96, 290)], [(288, 190), (306, 290)], [(150, 190), (144, 280)], [(250, 190), (256, 280)]]:
        b += line(p, 5, 0.8, "#d8d2c4") + line(p, 1.6, 0.8)
    b += shape([(100, 120), (130, 118), (132, 168), (102, 170)], "#e0dace", 2.0, 1.0) + shape([(270, 118), (300, 120), (298, 170), (268, 168)], "#e0dace", 2.0, 1.0)
    b += f'<path d="M160 100 q20 40 60 30" stroke="#6a5a4a" stroke-width="2" fill="none" opacity=".5"/>'  # crack in the plastic
    return T(x, y, s, b)

def gasmask_p(x=0, y=0, s=1):
    rnd(105)
    b = line([(200, 10), (200, 50)], 1.8, 0.4) + f'<circle cx="200" cy="8" r="6" fill="#2a1c14"/>'
    b += line([(120, 90), (150, 50), (250, 50), (280, 90)], 3.0, 1.0, "#3a3f3a")
    b += shape([(120, 100), (200, 70), (280, 100), (272, 190), (200, 240), (128, 190)], "#5d6a50", 2.6, 1.8)
    b += blob(160, 130, 26, 24, "#9fc8cc", 2.4) + blob(240, 130, 26, 24, "#9fc8cc", 2.4)
    b += f'<path d="M146 122 l20 -6" stroke="#fff" stroke-width="3" opacity=".6"/><path d="M226 122 l20 -6" stroke="#fff" stroke-width="3" opacity=".6"/>'
    b += blob(200, 212, 36, 34, "#3a3f3a", 2.6) + blob(200, 212, 22, 20, "#2a2e2a", 1.6)
    for k in range(5): b += line([(186 + k * 7, 198), (186 + k * 7, 226)], 1.0, 0.3, op=0.6)
    return T(x, y, s, b)

def galon_p(x=0, y=0, s=1):
    rnd(106)
    b = shape([(110, 60), (290, 60), (298, 280), (102, 280)], C["pink"], 2.6, 2.0)
    b += box(170, 24, 60, 40, C["pink_dk"], 2.4, 1.0) + line([(230, 70), (270, 70), (270, 120)], 6, 0.6, C["pink_dk"])
    b += shape([(118, 120), (282, 120), (282, 230), (118, 230)], "#e7a24a", 2.0, 1.0)
    b += shape([(118, 230), (160, 160), (190, 190), (220, 140), (282, 230)], "#2a1c24", 1.4, 1.5)
    b += blob(240, 150, 16, 16, "#f6d65a", 1.4)
    b += text(200, 220, "peak", 36, "#f0c0dc", font="Comic Relief")
    b += line([(118, 250), (282, 250)], 1.0, 1.0, "#6a2a50", 0.6)
    return T(x, y, s, b)

def bigas_p(x=0, y=0, s=1):
    rnd(107)
    b = shape([(110, 280), (98, 120), (120, 60), (170, 40), (240, 40), (290, 64), (304, 120), (296, 280)], "#e3d6b8", 2.6, 2.6)
    b += line([(120, 64), (160, 86), (200, 70), (240, 88), (286, 66)], 2.0, 1.0)
    b += text(200, 190, "BIGAS", 52, "#2f6b3a") + text(200, 226, "25 KILO", 20, "#2f6b3a", font="Bitter", weight="bold")
    for k in range(10): b += line([(110 + k * 19, 260), (114 + k * 19, 276)], 0.9, 0.4, op=0.4)
    b += blob(160, 110, 30, 8, "#cfc2a0", 0)  # fold shadow
    return T(x, y, s, b)

def cans_p(x=0, y=0, s=1):
    rnd(108)
    def can(cx, cy):
        b = shape([(cx, cy + 14), (cx + 110, cy + 14), (cx + 110, cy + 74), (cx, cy + 74)], C["tuna"], 2.4, 1.0)
        b += f'<ellipse cx="{cx+55}" cy="{cy+14}" rx="55" ry="12" fill="#b6bac0"/>' + ink(rough(ellipse_pts(cx + 55, cy + 14, 55, 12, 18)), 2.0, 0.8)
        b += blob(cx + 55, cy + 12, 12, 5, "#8a9095", 1.4)
        b += text(cx + 55, cy + 46, "MILLENIUM", 17, "#eceaf8", font="Bitter", weight="bold") + text(cx + 55, cy + 66, "tuna", 15, "#c9cdf2", font="Comic Relief")
        b += px(["..aaaa..b", ".aaaaaa.bb", "aaacaaaaab", ".aaaaaa.b."], {'a': '#c9cdf2', 'b': '#c9cdf2', 'c': INK}, cx + 82, cy + 52, 2)
        return b
    b = can(80, 200 - 74 + 10) + can(200, 136) + can(140, 64)
    return T(x, y, s, b)

def clock_p(x=0, y=0, s=1):
    rnd(109)
    b = blob(200, 150, 110, 110, C["cream"], 3.0) + blob(200, 150, 94, 94, "#efe3c8", 1.4)
    for i in range(12):
        a = math.radians(i * 30); r0 = 78 if i % 3 else 70
        b += line([(200 + r0 * math.sin(a), 150 - r0 * math.cos(a)), (200 + 88 * math.sin(a), 150 - 88 * math.cos(a))], 2.4 if i % 3 == 0 else 1.4, 0.3)
    b += line([(200, 150), (200, 76)], 4.4, 0.4) + line([(200, 150), (250, 178)], 4.4, 0.4) + line([(200, 150), (150, 96)], 1.4, 0.4, C["brick"])
    b += blob(200, 150, 8, 8, C["brick"], 1.6)
    b += line([(160, 210), (240, 226)], 1.4, 2.0, op=0.5)  # crack in the glass
    b += text(200, 120, "SILONG", 14, "#6a5a4a", font="Bitter", weight="bold")
    return T(x, y, s, b)

def capiz_p(x=0, y=0, s=1):
    rnd(110)
    b = box(70, 20, 260, 260, C["wood_dk"], 3.0, 1.4)
    for i in range(6):
        for j in range(6):
            b += box(84 + i * 39, 34 + j * 39, 34, 34, "#efe4cc", 1.0, 0.5)
            b += f'<path d="M{86+i*39} {58+j*39} q14 -10 30 0" stroke="#d8cbb0" stroke-width="1.2" fill="none"/>'
    b += line([(200, 20), (200, 280)], 4, 0.6, C["wood_dk"])
    b += box(320, 130, 10, 40, "#8a8a80", 1.6, 0.4)  # latch
    return T(x, y, s, b)

def rooster(x=0, y=0, s=1):
    """A proper tandang: tall serrated comb, wattles, golden hackle cape, arching sickle tail, spurs."""
    rnd(111)
    b = ""
    # tail: long arching sickle feathers (dark green-black with a sheen), drawn back to front
    for k, (tip, col) in enumerate([((356, 40), "#16241c"), ((376, 92), "#1f3a2e"), ((380, 150), "#16241c"), ((340, 20), "#2c4a36"), ((364, 200), "#1f3a2e")]):
        tx, ty = tip
        pts = [(246, 176), (262, 120 - k * 4), (300, 70 + k * 10), (tx, ty), (tx + 8, ty + 18), (320, 100 + k * 18), (292, 150 + k * 10), (268, 200)]
        b += oshape(pts, col, 2.0, 0.6)
        b += ocurve([(262, 150), (300, 100 + k * 12), (tx - 4, ty + 10)], 1.0, 0.4, "#5a8a6a", 0.5)
    # body: round breast and back
    b += oshape([(132, 170), (160, 136), (214, 128), (262, 150), (282, 186), (264, 232), (214, 254), (160, 248), (128, 214)], "#8a2e1e", 2.6, 0.8)
    b += oshape([(176, 168), (236, 160), (262, 196), (234, 228), (190, 226), (170, 200)], "#5a1a12", 2.0, 0.6)  # wing
    for k in range(4): b += ocurve([(190 + k * 14, 178), (198 + k * 12, 200), (196 + k * 12, 222)], 1.0, 0.4, "#2a0c08", 0.7)
    b += oshape([(138, 196), (160, 186), (176, 214), (160, 244), (136, 226)], "#1e1411", 0, 0.5, line=False, op=0.5)  # dark breast
    # golden hackle cape over the neck
    b += oshape([(108, 96), (130, 70), (162, 76), (186, 112), (196, 156), (176, 182), (146, 178), (124, 150), (110, 124)], "#d0862a", 2.4, 0.8)
    for k in range(6): b += ocurve([(122 + k * 10, 96 + k * 6), (130 + k * 9, 130 + k * 4), (138 + k * 8, 170)], 1.0, 0.5, "#8a4a14", 0.7)
    # head, comb, beak, wattle, eye
    b += oshape([(98, 82), (106, 64), (126, 56), (146, 66), (148, 88), (130, 100), (108, 98)], "#c8701e", 2.4, 0.6)
    b += oshape([(104, 62), (100, 40), (112, 48), (116, 24), (126, 44), (134, 22), (142, 44), (154, 34), (150, 60), (136, 64), (114, 66)], "#d02a1e", 2.2, 0.4)
    b += shape([(100, 76), (78, 82), (98, 88)], "#e0a23a", 2.0, 0.3)
    b += oshape([(102, 90), (114, 92), (116, 114), (108, 124), (100, 110)], "#d02a1e", 2.0, 0.4)
    b += blob(118, 74, 4.5, 4.5, "#f6d65a", 1.2) + f'<circle cx="118" cy="74" r="2" fill="{INK}"/>'
    # legs with spurs
    for lx in (192, 226):
        b += line([(lx, 248), (lx - 4, 290)], 4, 0.3, "#e0a23a") + line([(lx - 4, 290), (lx - 26, 296)], 3, 0.3, "#e0a23a") + line([(lx - 4, 290), (lx + 16, 296)], 3, 0.3, "#e0a23a") + line([(lx - 4, 290), (lx - 6, 298)], 3, 0.3, "#e0a23a")
        b += line([(lx - 2, 272), (lx + 10, 268)], 2.4, 0.2, "#e0a23a")
    return T(x, y, s, b)

def jeepney(x=0, y=0, s=1):
    """Side view of a jeepney: long low hood with chrome grille and horses, roof that reaches over the driver,
    open passenger windows, two-tone paint with chrome trim and stripes, fender arches, open rear step."""
    rnd(112)
    b = ""
    # passenger body (two-tone: cream top, red bottom) with chrome trim
    b += shape([(60, 96), (420, 92), (424, 206), (56, 210)], "#e9dcc2", 2.8, 1.0)
    b += shape([(58, 160), (424, 156), (424, 206), (56, 210)], C["brick"], 2.4, 0.8)
    b += shape([(58, 154), (424, 150), (424, 162), (58, 166)], "#cfd4d4", 1.8, 0.6)  # chrome strip
    b += line([(70, 186), (200, 176), (320, 190), (410, 180)], 3, 0.8, "#f2c94c")      # painted swoosh
    for i in range(7):  # open windows with a grab bar, passengers' silhouettes inside dark
        wx = 116 + i * 42
        b += box(wx, 104, 34, 40, "#2a2026", 1.8, 0.5) + line([(wx, 116), (wx + 34, 116)], 1.6, 0.3, "#cfd4d4")
    # roof with rails, reaching forward over the driver; route sign on the front edge
    b += shape([(0, 74), (430, 70), (434, 94), (-4, 98)], "#d8dcd8", 2.6, 0.8)
    for i in range(9): b += line([(30 + i * 46, 70), (34 + i * 46, 60)], 1.4, 0.2)
    b += line([(20, 60), (420, 56)], 1.6, 0.4)
    b += shape([(-20, 76), (10, 72), (12, 100), (-18, 104)], "#d8dcd8", 2.2, 0.6)
    b += box(130, 32, 200, 26, "#f2e8c8", 2.2, 0.6) + text(230, 51, "SILONG • BUNKER", 17, C["brick"])
    # driver cab: open side, windshield
    b += shape([(8, 100), (60, 98), (60, 206), (8, 208)], "#e9dcc2", 2.4, 0.8)
    b += shape([(14, 104), (52, 102), (52, 150), (16, 152)], "#2a2026", 1.6, 0.5)
    b += shape([(-14, 104), (8, 102), (8, 150), (-14, 152)], "#8fb4c0", 1.6, 0.5)
    # long low hood with front fender arch, chrome grille, bumper, horses
    b += shape([(-150, 140), (-14, 128), (8, 128), (8, 208), (-158, 210)], "#e9dcc2", 2.6, 0.8)
    b += shape([(-150, 170), (8, 166), (8, 208), (-158, 210)], C["brick"], 2.0, 0.6)
    b += f'<path d="M-130 210 Q-96 150 -60 210Z" fill="#2a2026"/>' + ocurve([(-134, 210), (-118, 176), (-96, 164), (-72, 176), (-56, 210)], 2.4, 0.5)
    b += shape([(-174, 142), (-150, 138), (-150, 206), (-176, 208)], "#cfd4d4", 2.2, 0.5)
    for k in range(6): b += line([(-172, 148 + k * 10), (-152, 146 + k * 10)], 1.0, 0.2)
    b += shape([(-190, 200), (10, 198), (10, 214), (-190, 216)], "#b8bcbc", 2.2, 0.6)
    b += blob(-160, 154, 9, 9, "#f2e8a0", 1.6) + blob(-140, 156, 7, 7, "#f2e8a0", 1.4)
    for hx in (-120, -80, -40):
        b += oshape([(hx, 130), (hx + 4, 112), (hx + 10, 104), (hx + 20, 100), (hx + 24, 108), (hx + 16, 112), (hx + 14, 128)], "#dfe4e4", 1.6, 0.3)
    b += line([(-10, 120), (-36, 96), (-44, 96)], 1.8, 0.3) + blob(-48, 94, 8, 6, "#cfd4d4", 1.4)  # side mirror
    # rear open step + spare tyre
    b += box(420, 190, 30, 12, "#9ca0a0", 1.8, 0.4) + blob(440, 140, 22, 30, "#2a2024", 2.2)
    # wheels
    for wx in (-96, 340):
        b += blob(wx, 216, 34, 34, "#1e1a1c", 2.6) + blob(wx, 216, 16, 16, "#cfd4d4", 1.8) + blob(wx, 216, 5, 5, "#555", 1.0)
    b += f'<path d="M300 210 Q340 160 380 210Z" fill="#2a2026" opacity=".0"/>'
    b += text(300, 140, "Lucky 7", 18, "#f2c94c", font="Comic Relief", rot=-4)
    b += line([(90, 196), (120, 194)], 2.0, 0.6, C["rust"], 0.7)
    return T(x, y, s, b)

def pole(x=0, y=0, s=1):
    rnd(113)
    b = box(190, 10, 20, 290, "#3a2a20", 2.4, 1.0) + box(120, 40, 160, 14, "#3a2a20", 2.2, 0.8) + box(140, 70, 120, 10, "#3a2a20", 2.0, 0.6)
    for ix in (130, 170, 230, 270): b += blob(ix, 36, 5, 7, "#9aa1a5", 1.2)
    b += blob(240, 120, 22, 28, "#6a706e", 2.2)  # transformer
    for k in range(4):
        b += line([(0, 50 + k * 8), (130 + k * 40, 44), (400, 60 + k * 10)], 1.4, 1.0)
    b += line([(240, 92), (230, 60)], 1.2, 0.4) + line([(60, 70), (100, 140), (160, 120), (200, 170)], 1.2, 1.0, op=0.7)  # tangled
    for cx, cy in [(70, 52), (100, 54), (310, 60)]:
        b += shape([(cx - 10, cy), (cx - 4, cy - 14), (cx + 6, cy - 16), (cx + 10, cy - 6), (cx + 4, cy)], "#120c0c", 1.0, 0.5)
    return T(x, y, s, b)

def sarisari(x=0, y=0, s=1):
    rnd(114)
    b = box(40, 70, 320, 220, "#6a4a30", 2.8, 1.6)
    b += shape([(20, 80), (380, 80), (360, 30), (40, 30)], C["brick"], 2.6, 1.4)
    for k in range(6): b += line([(60 + k * 56, 32), (50 + k * 58, 80)], 1.2, 0.6, "#7a1e18", 0.7)
    b += box(64, 46, 272, 26, C["cream"], 2.0, 0.8) + text(200, 66, "SARI-SARI STORE", 20, C["brick"])
    b += box(70, 100, 260, 130, "#2a1c14", 2.4, 1.0)
    # grill
    for k in range(12): b += line([(80 + k * 22, 100), (80 + k * 22, 230)], 1.6, 0.4, "#9ca0a0")
    # snack sachets hanging (pixel strip)
    cols = ["#e0a23a", "#a8392f", "#5b63b8", "#7f9a4a", "#c96aa0"]
    for k in range(10):
        b += f'<rect x="{88+k*24}" y="{108}" width="16" height="22" fill="{cols[k%5]}" shape-rendering="crispEdges"/>'
        b += f'<rect x="{90+k*24}" y="{114}" width="12" height="3" fill="#f2e8c8" shape-rendering="crispEdges"/>'
    b += cans_p(110, 150, 0.32)[0:0]
    b += box(90, 170, 60, 50, C["tuna"], 1.8, 0.6) + box(160, 180, 40, 40, C["pink"], 1.8, 0.6) + box(210, 176, 50, 44, C["aid"], 1.8, 0.6)
    b += box(40, 230, 320, 20, "#8a6a4a", 2.2, 0.8)  # counter
    b += shape([(260, 250), (340, 246), (342, 290), (258, 290)], "#c9c2b0", 2.0, 1.0) + text(300, 276, "BAWAL UMIHI", 10, C["brick"], font="Bitter", weight="bold")
    return T(x, y, s, b)

def hoop(x=0, y=0, s=1):
    rnd(115)
    b = box(300, 60, 16, 240, "#6f7478", 2.4, 1.0)
    b += shape([(150, 40), (330, 36), (334, 150), (148, 154)], "#e9e2d0", 2.6, 1.4)
    b += ink(rough(rect(210, 80, 64, 50), True, 0.8), 3.0, 1.0, "#a8392f")
    b += line([(186, 160), (270, 160)], 4, 0.4, C["orange"])
    for k in range(6): b += line([(190 + k * 15, 160), (200 + k * 12, 210)], 1.0, 0.6, "#e9e2d0")
    b += line([(196, 210), (266, 210)], 1.0, 0.6, "#e9e2d0")
    b += line([(170, 90), (230, 140)], 1.4, 1.0, C["rust"], 0.6)
    return T(x, y, s, b)

def tank(x=0, y=0, s=1):
    rnd(116)
    b = shape([(110, 60), (290, 60), (296, 210), (104, 210)], "#3b7fb8", 2.6, 2.0)
    b += f'<ellipse cx="200" cy="60" rx="90" ry="18" fill="#2c6aa0"/>' + ink(rough(ellipse_pts(200, 60, 90, 18, 22)), 2.2)
    for yy in (110, 160): b += line([(106, yy), (294, yy)], 1.6, 1.0, "#1f4e78")
    b += box(116, 210, 20, 80, "#4a3022", 2.0, 0.6) + box(264, 210, 20, 80, "#4a3022", 2.0, 0.6) + line([(126, 250), (274, 230)], 1.8, 0.6)
    b += line([(290, 180), (340, 180), (340, 290)], 5, 0.6, "#7d8584")
    b += text(200, 146, "SILONG", 22, "#d8e4ea")
    return T(x, y, s, b)

def deadtree(x=0, y=0, s=1):
    rnd(117)
    b = shape([(186, 300), (190, 160), (176, 110), (196, 108), (206, 150), (214, 300)], "#2e2a22", 2.4, 2.0)
    for p in [[(196, 160), (130, 110), (100, 60)], [(200, 140), (270, 80), (300, 40)], [(190, 120), (170, 50)], [(130, 110), (90, 116)], [(270, 80), (310, 90)]]:
        b += line(p, 7, 1.2, "#2e2a22") + line(p, 1.4, 1.2)
    b += line([(300, 40), (318, 20)], 2.0, 0.4) + line([(100, 60), (84, 36)], 2.0, 0.4)
    b += blob(250, 300, 70, 8, "#2a2420", 0)
    return T(x, y, s, b)

def billboard(x=0, y=0, s=1):
    rnd(118)
    b = box(70, 270, 14, 30, "#4a3022", 2.0, 0.6) + box(316, 270, 14, 30, "#4a3022", 2.0, 0.6)
    b += box(20, 40, 360, 230, C["teal_dk"], 2.8, 1.6)
    b += blob(110, 150, 62, 62, "#f2c94c", 2.6)
    for i in range(12):
        a = math.radians(i * 30)
        b += line([(110 + 72 * math.cos(a), 150 + 72 * math.sin(a)), (110 + 88 * math.cos(a), 150 + 88 * math.sin(a))], 2.6, 0.6, "#c48a1c")
    b += f'<ellipse cx="92" cy="140" rx="7" ry="13" fill="{INK}"/><ellipse cx="128" cy="140" rx="7" ry="13" fill="{INK}"/><circle cx="94" cy="135" r="2.4" fill="#fff"/><circle cx="130" cy="135" r="2.4" fill="#fff"/>'
    b += line([(86, 170), (110, 186), (134, 170)], 3, 0.6)
    b += line([(170, 150), (196, 120), (212, 96)], 6, 0.6, "#f2c94c") + blob(214, 92, 14, 14, "#fff", 2.0)  # rubber-hose arm waving
    b += text(290, 130, "SILONG", 46, "#f4d65a", stroke="#3a0a08", sw=3)
    b += text(290, 166, "SHELTERS", 22, C["cream"])
    b += text(290, 222, "LIGTAS NA, SULIT PA!", 15, C["cream"], font="Bitter", weight="bold")
    b += shape([(330, 40), (380, 40), (380, 120)], "#1a1210", 0, 1.0, line=False)  # torn corner
    b += line([(40, 250), (90, 230)], 1.2, 1.0, C["rust"], 0.7)
    return T(x, y, s, b)

def cot(x=0, y=0, s=1):
    rnd(119)
    b = shape([(40, 160), (360, 156), (366, 196), (36, 200)], "#5d6a50", 2.6, 1.4)
    b += blob(100, 146, 50, 18, C["cream"], 2.2)
    b += shape([(170, 150), (330, 146), (340, 170), (180, 176)], "#8a6a5a", 2.0, 1.4)  # blanket
    for p in [[(50, 200), (30, 290)], [(350, 196), (372, 290)], [(120, 200), (170, 290)], [(280, 198), (232, 290)]]:
        b += line(p, 3.4, 0.6)
    return T(x, y, s, b)

def candles(x=0, y=0, s=1):
    rnd(120)
    b = box(120, 130, 40, 150, C["cream"], 2.4, 1.0) + box(190, 180, 40, 100, C["cream"], 2.4, 1.0)
    b += line([(140, 130), (140, 116)], 1.6, 0.2) + line([(210, 180), (210, 166)], 1.6, 0.2)
    b += '<g class="flame">' + px(["..a..", ".aba.", ".aba.", "abcba", ".bcb."], {'a': '#f6d65a', 'b': '#e0a23a', 'c': '#fff4d0'}, 132, 92, 3) + "</g>"
    b += '<g class="flame">' + px(["..a..", ".aba.", "abcba", ".bcb."], {'a': '#f6d65a', 'b': '#e0a23a', 'c': '#fff4d0'}, 202, 144, 3) + "</g>"
    b += line([(116, 160), (110, 200), (118, 230)], 2.6, 0.6, "#f2ecd8", 0.9)  # drips
    b += box(260, 240, 90, 40, C["brick"], 2.2, 0.8) + text(305, 266, "POSPORO", 13, C["cream"], font="Bitter", weight="bold")
    b += line([(270, 236), (320, 210)], 1.8, 0.3, "#d8c090") + blob(322, 208, 4, 4, "#2a1c14", 0.8)
    b += f'<ellipse cx="180" cy="284" rx="110" ry="10" fill="#e8dcc0" opacity=".7"/>'
    return T(x, y, s, b)

PROPS = [
    ("P01", "Transistor radio", radio), ("P02", "Tabo and balde", tabo_balde), ("P03", "Banig, rolled", banig),
    ("P04", "Monobloc chair", monobloc), ("P05", "Gas mask on a hook", gasmask_p), ("P06", "Water galon (Peak)", galon_p),
    ("P07", "Rice sack (BIGAS)", bigas_p), ("P08", "Millenium Tuna cans", cans_p), ("P09", "Wall clock", clock_p),
    ("P10", "Capiz window", capiz_p), ("P11", "Rooster (Tandang)", rooster), ("P12", "Jeepney", jeepney),
    ("P13", "Power pole + crows", pole), ("P14", "Sari-sari store", sarisari), ("P15", "Basketball hoop", hoop),
    ("P16", "Water tank", tank), ("P17", "Dead tree", deadtree), ("P18", "Silong billboard", billboard),
    ("P19", "Cot bed", cot), ("P20", "Candles and matches", candles),
]

if __name__ == "__main__":
    import sys, numpy as np
    from PIL import Image
    out = "/home/claude/envd/project/v3/props/"; import os; os.makedirs(out, exist_ok=True)
    lay = "/home/claude/envgen/v3/layers/props/"; os.makedirs(lay, exist_ok=True)
    for code, name, fn in PROPS:
        if code == "P12": body = fn(122, 60, 0.57)
        else: body = fn(0, 0, 1)
        a = render(body, 400, 300)
        a = texture(a, age=0.35, grime=0.25, stain=0.1, seed=hash(code) % 1000, grain_amt=0.03, mottle_scale=60,
                    rust=0.35 if code in ("P13", "P15", "P16", "P05") else None)
        save(a, f"{lay}{code}.png")
        bg = np.zeros_like(a); bg[:, :, :3] = np.array([0x3a, 0x2c, 0x24]) / 255; bg[:, :, 3] = 1
        save(over(bg, a), f"{out}{code}.png", (400, 300))
        print(code)
