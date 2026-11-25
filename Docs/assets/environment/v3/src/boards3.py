"""v3 canvas boards: the picked direction, combined and redrawn."""
import sys
sys.path.insert(0, "/home/claude/envgen")
from lib import page, caption, write, GOLD, CHAR, CREAM

def tag(c):
    return f'<div style="flex: none; padding: 4px 8px; border-radius: 6px; background: {GOLD}; color: {CHAR}; font: 400 18px/1 Anton, sans-serif">{c}</div>'

RM = "@media (prefers-reduced-motion: reduce){*{animation:none!important}}"

# ------------------------------------------------------------------ main
rows = [("ROOM", "R-B + R-A + R-C details", "Filipino home bunker with R-A's steel shelf, poster and pipes, and R-C's portrait, kid's drawings and peeling wallpaper. Spider web, crows on the wire, tally marks, card-art labels on the supplies."),
        ("OUTSIDE", "M-A", "Filipino street at sundown: sari-sari store, jeepney, rooster, Silong billboard, houses with laundry, the bunker hatch, a cat, posters and a 10 ARAW graffiti."),
        ("RONDO", "8 results", "Each Rondo result changes both scenes: Purge, Trading only, Hunger, Tornado, Doomsday, Blackout, Outbreak, Peaceful."),
        ("LIGHT", "L1-L5", "Built in: afternoon on Day 1, red dusk by Day 9, cold blue at Night."),
        ("MOTION", "A01-A10", "Playing on the two hero boards: lamp sway and flicker, dust, crows, laundry, clouds, calendar rip, puppet idle, fan, spider."),
        ("PIXEL", "X01-X12", "Finer pixels (3 px at 1080p), shown at their real size in the scenes."),
        ("SCREEN", "OV3 OV7 OV8 + OV10", "Dusk, storybook paper, night tint, and a new low-opacity warm balance."),
        ("CAST", "C1-C6", "Grey tank tops, arms in their own skin, pupils, swaying long hair, left seat turned in. Pushed to the game."),
        ("GAME", "Dial · Wheel · Match", "The day/night porthole dial, the Rondo wheel, and a 20-second loop of a real turn, Night and a Rondo spin.")]
r = "".join(f'''<div style="display: flex; gap: 18px; align-items: baseline; padding: 8px 0; border-bottom: 2px dashed #5a4a3e">
<div style="width: 130px; flex: none; font: 400 26px/1 Anton, sans-serif; color: {GOLD}">{a}</div>
<div style="width: 230px; flex: none; font: 400 19px/1.1 Anton, sans-serif">{b}</div>
<div style="font: 400 15px/1.4 Roboto, sans-serif; color: #e4d8c4">{c}</div></div>''' for a, b, c in rows)
inner = f'''<div style="flex: 1; box-sizing: border-box; padding: 40px 56px; display: flex; flex-direction: column; gap: 8px">
<div style="font: 500 13px/1 Roboto, sans-serif; letter-spacing: 2px; color: {GOLD}">TEN DAYS TO SUNDOWN · v3.1 · YOUR PICKS, COMBINED</div>
<div style="font: 400 58px/1 Anton, sans-serif; color: #f4d65a; text-shadow: 3px 4px 0 #3a0a08">THE NEW LOOK</div>
<div style="font: 400 16px/1.5 Roboto, sans-serif; max-width: 1080px"><b style="color: #f4d65a">New in v3.1:</b> paper cut-out 2.5D (soft shadows behind everything, a pale paper edge and a slight turn on the seated cast), bigger cast in grey tank tops, a smaller table with a banig mat, scratches and a spill, poster moved left and calendar up, the day/night dial and the Rondo wheel, and a full game board. Thinner, sketchier ink; grime, drips, rust and paper grain on everything; muted colours that sit with the team's cards. Every scene is built in layers at 1920×1080, so the same files go into Unity.</div>
{r}
<div style="font: 400 14px/1.5 Roboto, sans-serif; color: #b9ad99; margin-top: 6px">Reply with what to change, by board and item (e.g. "Room: bigger window, move calendar left; P11 tail longer; OV10 a bit stronger"), or say <b style="color: #f4d65a">"approved"</b> and I'll put the scenes into the game.</div>
</div>'''
write("V3_Main.dc.html", page("v3: The new look", 1280, 860, inner))

# ------------------------------------------------------------------ hero room (animated)
css_room = """.stage{position:relative;width:1600px;height:900px;overflow:hidden;background:#000;flex:none}
.stage img{position:absolute;inset:0;width:100%;height:100%;display:block}
.lamp{transform-origin:50% 8.9%;animation:sway 3.2s ease-in-out infinite alternate}
@keyframes sway{from{transform:rotate(-1.6deg)}to{transform:rotate(1.6deg)}}
.flick{position:absolute;left:38%;top:12%;width:24%;height:40%;background:radial-gradient(closest-side,rgba(255,214,140,.32),rgba(255,214,140,0));mix-blend-mode:screen;animation:flick 2.6s steps(1) infinite}
@keyframes flick{0%{opacity:1}11%{opacity:.4}14%{opacity:1}58%{opacity:1}61%{opacity:.55}66%{opacity:1}}
.bob1{animation:bob 3.4s ease-in-out infinite alternate}.bob2{animation:bob 3.9s ease-in-out -1.2s infinite alternate}.bob3{animation:bob 3.1s ease-in-out -2s infinite alternate}
@keyframes bob{from{transform:translateY(0)}to{transform:translateY(-3px)}}
.fan{transform-origin:93.75% 42.04%;animation:spin .7s linear infinite}
@keyframes spin{to{transform:rotate(360deg)}}
.spider{animation:drop 5s ease-in-out infinite alternate}
@keyframes drop{0%,30%{transform:translateY(0)}100%{transform:translateY(26px)}}
.cal{transform-origin:34.6% 18.1%;animation:rip 7s ease-in infinite}
@keyframes rip{0%,62%{transform:none;opacity:1}78%{transform:rotate(-24deg) translate(10px,20px);opacity:1}100%{transform:rotate(-40deg) translate(40px,240px);opacity:0}}
.dust{position:absolute;width:3px;height:3px;background:#f4e6c4;animation:rise 6s linear infinite;opacity:0}
@keyframes rise{0%{transform:translateY(0);opacity:0}15%{opacity:.9}100%{transform:translateY(-200px);opacity:0}}
.fly{position:absolute;width:4px;height:4px;background:#1a1210;border-radius:50%;left:50%;top:27%;animation:orbit 2.2s linear infinite}
@keyframes orbit{from{transform:rotate(0deg) translateX(46px) rotate(0deg)}to{transform:rotate(360deg) translateX(46px) rotate(-360deg)}}
.crow{position:absolute;left:61.2%;top:16.4%;width:22px;height:14px;animation:crow 9s ease-in infinite}
@keyframes crow{0%,55%{transform:none;opacity:1}100%{transform:translate(160px,-120px) scale(.6);opacity:0}}
""" + RM
import random
random.seed(4)
dust = "".join(f'<div class="dust" style="left:{random.uniform(42,58):.1f}%;top:{random.uniform(30,52):.1f}%;animation-delay:-{random.uniform(0,6):.1f}s"></div>' for _ in range(18))
flies = "".join(f'<div class="fly" style="animation-delay:-{i*0.7:.1f}s;animation-duration:{2+i*0.4:.1f}s"></div>' for i in range(3))
crow = '<svg class="crow" viewBox="0 0 22 14" aria-hidden="true"><path d="M1 12 L6 4 L13 2 L17 6 L21 6 L16 9 L12 12Z" fill="#120c0c"/></svg>'
L = lambda k, cls="": f'<img src="./v3/hero_d1_{k}.png" alt="" class="{cls}">'
stage = f'''<div class="stage"><img src="./v3/hero_d1_base.jpg" alt="Final room, Day 1">{crow}{L("calpage","cal")}{L("spider","spider")}{L("fan","fan")}
{L("seat1","bob1")}{L("seat2","bob2")}{L("seat3","bob3")}{L("table")}{L("lamp","lamp")}<div class="flick"></div>{flies}{dust}</div>'''
inner = stage + caption("ROOM", "FINAL ROOM · DAY 1 · MOVING", "R-B with R-A and R-C pieces. Moving: lamp sway (A02) and flicker (A01), flies, dust in the light (A07, X09), a crow leaving the wire (A04), calendar page rip (A08), seats breathing (A09), the fan and the spider. The table is its own layer so cards sit on it.", "1920×1080 layers\nready for Unity")
write("V3_Room.dc.html", page("v3: Final room", 1600, 980, inner, css=css_room))

# ------------------------------------------------------------------ room + street states
def states(prefix, title, code, note):
    S = [("d1", "Day 1 · L1"), ("d5", "Day 5 · L2"), ("d9", "Day 9 · L4"), ("night", "Night · L5"),
         ("purge", "Purge"), ("trading_only", "Trading only"), ("hunger", "Hunger"), ("tornado", "Tornado"),
         ("doomsday", "Doomsday"), ("blackout", "Blackout"), ("outbreak", "Outbreak"), ("peaceful", "Peaceful")]
    tiles = "".join(f'''<div style="display: flex; flex-direction: column; gap: 6px"><img src="./v3/{prefix}_{k}.jpg" alt="{n}" style="display: block; width: 100%; aspect-ratio: 16 / 9; border-radius: 8px; border: 2px solid #4a3a30">
<div style="font: 400 17px/1 Anton, sans-serif; color: {GOLD if i < 4 else CREAM}">{n}</div></div>''' for i, (k, n) in enumerate(S))
    inner = f'<div style="flex: 1; box-sizing: border-box; padding: 22px; display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; align-content: start">{tiles}</div>' + caption(code, title, note, "Top row: light over the days\nThen: the 8 Rondo results")
    return inner
write("V3_RoomStates.dc.html", page("v3: Room states", 1600, 1040, states("room", "ROOM: DAYS, NIGHT AND THE RONDO", "ROOM",
      "The shelf empties as the days run out (full, half, almost empty). Each Rondo result changes the window and the room: boarded window and red siren, a PALIT-PALIT sign, empty shelves and flies, taped window and flying papers, falling dust, candles, green fog and a gas mask, warm sunbeams.")))

css_street = """.stage{position:relative;width:1600px;height:900px;overflow:hidden;background:#000;flex:none}
.stage img{position:absolute;inset:0;width:100%;height:100%;display:block}
.clouds{animation:drift 40s linear infinite alternate}@keyframes drift{from{transform:translateX(-30px)}to{transform:translateX(60px)}}
.laundry{transform-origin:83% 34%;animation:flap 1.9s ease-in-out infinite alternate}@keyframes flap{from{transform:skewX(-1.2deg)}to{transform:skewX(1.2deg)}}
.crows{animation:crows 14s linear infinite}@keyframes crows{from{transform:translate(0,0)}to{transform:translate(-320px,-40px)}}
.hatch{position:absolute;left:76.5%;top:70%;width:4%;height:7%;background:radial-gradient(closest-side,rgba(255,214,140,.7),rgba(255,214,140,0));animation:blink 2.4s ease-in-out infinite}
@keyframes blink{50%{opacity:.35}}
""" + RM
stage = '''<div class="stage"><img src="./v3/shero_d1_base.jpg" alt="Final street, Day 1"><img src="./v3/shero_d1_clouds.png" alt="" class="clouds">
<img src="./v3/shero_d1_crows.png" alt="" class="crows"><img src="./v3/shero_d1_laundry.png" alt="" class="laundry"><div class="hatch"></div></div>'''
inner = stage + caption("STREET", "FINAL OUTSIDE (M-A) · MENU · MOVING", "Sari-sari store with a pixel BUKAS sign, the jeepney, a rooster, the Silong billboard, houses with laundry, posters, a cat on the roof and the bunker hatch. Moving: clouds (A03), crows (A04), laundry (A05), the hatch light.", "Space top-left kept\nfor the logo and menu")
write("V3_Street.dc.html", page("v3: Final street", 1600, 980, inner, css=css_street))
write("V3_StreetStates.dc.html", page("v3: Street states", 1600, 1040, states("street", "OUTSIDE: DAYS, NIGHT AND THE RONDO", "STREET",
      "The same street through the 10 days and for each Rondo result: boarded store, a PALIT-PALIT stall, SARADO / UBOS NA, rain and a funnel, meteors, a blackout with only the stars, green fog, and a clear peaceful evening.")))

# ------------------------------------------------------------------ props
from props3 import PROPS
tiles = "".join(f'''<div style="background: #2e2420; border: 2px solid #4a3a30; border-radius: 10px; overflow: hidden; display: flex; flex-direction: column">
<img src="./v3/props/{c}.png" alt="{n}" style="display: block; width: 100%; aspect-ratio: 4 / 3">
<div style="display: flex; gap: 8px; align-items: center; padding: 8px 10px">{tag(c)}<div style="font: 500 14px/1.2 Roboto, sans-serif">{n}</div></div></div>''' for c, n, _ in PROPS)
inner = f'<div style="flex: 1; box-sizing: border-box; padding: 22px; display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 14px; align-content: start">{tiles}</div>' + caption("P01-20", "PROPS, REDRAWN", "Thinner sketchy lines, grime and rust. P11 is now a real tandang (tall comb, wattles, golden hackles, arching sickle tail, spurs); P12 is a proper jeepney (long hood, chrome horses, open windows, roof sign, two-tone paint). Labels follow the cards: Millenium Tuna, Peak water, First Aid.", "Each prop is its own\ntransparent PNG")
write("V3_Props.dc.html", page("v3: Props", 1600, 1110, inner))

# ------------------------------------------------------------------ pixel details (at true scale, zoomed 2x)
X = [("X01", "Dither shadows", "Under the ceiling and along the floor line"), ("X02", "Card-style labels", "Millenium Tuna, Peak, First Aid, BIGAS"),
     ("X03", "Pixel TV", "Static and a scrolling 'SUNDOWN SA 3 ARAW'"), ("X04", "Candle flame", "On the shelf; lit for Blackout"),
     ("X05", "Night sky", "Pixel stars and moon in the window"), ("X06", "Rust", "Fine speckle on metal: tank, pole, hoop, shelf"),
     ("X07", "Radio meter", "Bars bounce while the host talks"), ("X08", "Days-left clock", "Red pixel digits under the window"),
     ("X09", "Dust", "Square specks in the lamp light"), ("X10", "Rain", "Tornado: streaks on the window"),
     ("X11", "Mould", "Grows out of the wall crack"), ("X12", "LED sign", "BUKAS on the sari-sari store")]
from PIL import Image as _I
def zw(c):
    w, h = _I.open(f"/home/claude/envd/project/v3/px/{c}.png").size
    k = min(2.0, 222 / h, 360 / w)
    return round(w * k)
dust_px = "".join(f'<div class="pdust" style="left:{random.uniform(38,62):.0f}%;top:{random.uniform(40,80):.0f}%;animation-delay:-{random.uniform(0,5):.1f}s"></div>' for _ in range(14))
tiles = "".join(f'''<div style="background: #1c1614; border: 2px solid #4a3a30; border-radius: 10px; overflow: hidden; display: flex; flex-direction: column">
<div style="position: relative; height: 230px; display: flex; align-items: center; justify-content: center; background: #120e0c; overflow: hidden"><img src="./v3/px/{c}.png" alt="{n}" style="display: block; width: {zw(c)}px; max-width: 96%; max-height: 222px; object-fit: contain; image-rendering: pixelated">{dust_px if c == "X09" else ""}</div>
<div style="display: flex; flex-direction: column; gap: 4px; padding: 8px 10px"><div style="display: flex; gap: 8px; align-items: center">{tag(c)}<div style="font: 500 14px/1.2 Roboto, sans-serif">{n}</div></div>
<div style="font: 400 12px/1.3 Roboto, sans-serif; color: #cfc2ad">{d}</div></div></div>''' for c, n, d in X)
inner = f'<div style="flex: 1; box-sizing: border-box; padding: 22px; display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; align-content: start">{tiles}</div>' + caption("X01-12", "PIXEL DETAILS, FINER", "Cut straight from the scenes, so this is their real size: 3 px cells on a 1080p screen (they were 2-3 times bigger in v2). Small touches inside the line art, not a pixel-art game.", "Point-filtered sprites\nin Unity")
write("V3_Pixel.dc.html", page("v3: Pixel details", 1600, 1110, inner, css=".pdust{position:absolute;width:5px;height:5px;background:#f4e6c4;animation:rise 5s linear infinite;opacity:0}@keyframes rise{0%{transform:translateY(0);opacity:0}15%{opacity:.9}100%{transform:translateY(-120px);opacity:0}}" + RM))

# ------------------------------------------------------------------ overlays
PAPER = "url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='300' height='300'><filter id='p'><feTurbulence type='fractalNoise' baseFrequency='.04 .5' numOctaves='3' stitchTiles='stitch'/><feColorMatrix type='matrix' values='0 0 0 0 .55  0 0 0 0 .45  0 0 0 0 .32  0 0 0 .9 0'/></filter><rect width='100%' height='100%' filter='url(%23p)'/></svg>\")"
css_ov = f""".ov{{position:absolute;inset:0;pointer-events:none}}
.dusk{{background:linear-gradient(180deg,rgba(90,50,140,.5),rgba(242,140,58,.42));mix-blend-mode:soft-light}}
.vig{{background:radial-gradient(ellipse at 50% 40%,transparent 45%,rgba(10,4,2,.7) 100%)}}
.paper{{background-image:{PAPER};mix-blend-mode:multiply;opacity:.45}}
.night{{background:radial-gradient(circle at 50% 30%,rgba(255,210,122,0) 0 13%,rgba(20,30,80,.55) 42%,rgba(5,8,25,.82) 100%);mix-blend-mode:multiply}}
.warm{{background:rgba(255,168,92,.13);mix-blend-mode:soft-light}}
.warm2{{background:linear-gradient(180deg,rgba(255,190,120,.06),rgba(255,150,80,.1))}}
"""
O = [("BASE", "No overlay", "The v3 room as drawn", ""),
     ("OV3", "Dusk grade", "Purple-to-orange wash; grows as Sundown nears", '<div class="ov dusk"></div><div class="ov vig"></div>'),
     ("OV7", "Storybook paper", "Paper fibre, multiplied in", '<div class="ov paper"></div>'),
     ("OV8", "Night tint", "Blue dark except the lamp; for the Night phase", '<div class="ov night"></div>'),
     ("OV10", "Warm balance (new)", "Low-opacity warm wash that ties the colours together", '<div class="ov warm"></div><div class="ov warm2"></div>'),
     ("STACK", "Suggested stack", "Always OV10 + OV7; OV3 rising by Day; OV8 at Night", '<div class="ov warm"></div><div class="ov warm2"></div><div class="ov paper"></div><div class="ov dusk" style="opacity:.6"></div>')]
tiles = "".join(f'''<div style="background: #2e2420; border: 2px solid #4a3a30; border-radius: 10px; overflow: hidden; display: flex; flex-direction: column">
<div style="position: relative; aspect-ratio: 16 / 9; overflow: hidden"><img src="./v3/room_d1.jpg" alt="Room with {n}" style="display: block; width: 100%; height: 100%">{ov}</div>
<div style="display: flex; flex-direction: column; gap: 4px; padding: 8px 10px"><div style="display: flex; gap: 8px; align-items: center">{tag(c)}<div style="font: 500 15px/1.2 Roboto, sans-serif">{n}</div></div>
<div style="font: 400 13px/1.3 Roboto, sans-serif; color: #cfc2ad">{d}</div></div></div>''' for c, n, d, ov in O)
inner = f'<div style="flex: 1; box-sizing: border-box; padding: 22px; display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; align-content: start">{tiles}</div>' + caption("OV", "SCREEN OVERLAYS: YOUR PICKS + OV10", "OV10 is the new warm balance: about 10-13% strength, so it evens out the greens and blues without turning everything orange. Each overlay gets its own switch in Settings.", "One UI layer each\nin Unity")
write("V3_Overlays.dc.html", page("v3: Screen overlays", 1600, 840, inner, css=css_ov))

# ------------------------------------------------------------------ cast
C6 = [("c1", "C1", "Long hair, side part", "Big"), ("c2", "C2", "Curly", "Big"), ("c3", "C3", "Short spiky", "Big"),
      ("c4", "C4", "Wavy red", "Thin"), ("c5", "C5", "Close crop", "Thin"), ("c6", "C6", "Updo, earrings", "Thin")]
cards = "".join(f'<img src="./v3/cards/{f}.jpg" alt="" style="position: absolute; left: {40+i*58}px; top: 0; width: 100px; border-radius: 6px; box-shadow: 0 2px 0 #000">' for i, f in enumerate(["front_food", "front_water", "front_medicine"]))
chars = "".join(f'''<div style="flex: 1; display: flex; flex-direction: column; align-items: center; gap: 6px; background: #2e2420; border: 2px solid #4a3a30; border-radius: 12px; padding: 12px 6px">
<img src="./v3/cast/{k}.png" alt="{c}" style="display: block; height: 300px"><div style="display: flex; gap: 6px; align-items: center">{tag(c)}<div style="font: 500 13px/1.2 Roboto, sans-serif">{n} · {b}</div></div></div>''' for k, c, n, b in C6)
inner = f'''<div style="flex: 1; box-sizing: border-box; padding: 22px 26px; display: flex; flex-direction: column; gap: 16px">
<div style="display: flex; gap: 12px">{chars}</div>
<div style="display: flex; gap: 22px; align-items: stretch">
<div style="flex: 1; background: #1c1614; border-radius: 12px; padding: 14px 18px; display: flex; gap: 18px; align-items: center">
<img src="./v3/cast/c1_m.png" alt="C1 turned to face right" style="height: 190px"><div style="font: 400 14px/1.5 Roboto, sans-serif; color: #e4d8c4"><b style="color: {GOLD}">Left seat turns in.</b> The art faces left, so whoever sits on the left of the table is mirrored to look toward the middle. The seat number on the chest stays readable.</div></div>
<div style="flex: 1; background: #1c1614; border-radius: 12px; padding: 14px 18px; display: flex; gap: 22px; align-items: center">
<div style="display: flex; flex-direction: column; align-items: center; gap: 6px"><div style="position: relative; width: 260px; height: 150px; transform: rotate(180deg)">{cards}</div><div style="font: 400 15px/1 Anton, sans-serif; color: #b9ad99">THEIR TURN: FACING THEM</div></div>
<div style="font: 400 30px/1 Anton, sans-serif; color: {GOLD}">→</div>
<div style="display: flex; flex-direction: column; align-items: center; gap: 6px"><div style="position: relative; width: 260px; height: 150px; transform: scale(1.06)">{cards}</div><div style="font: 400 15px/1 Anton, sans-serif; color: {GOLD}">YOUR TURN: TURNED TO YOU</div></div></div>
</div></div>''' + caption("C1-C6", "THE CAST, FIXED (IN THE GAME NOW)", "Default outfit is a dark grey tank top with arms in each character's own skin; pupils; C1 and C4 long hair sways; left seat mirrored. Opponents' cards lie facing their owner (angled toward their seat) and turn round, a little bigger, on your Day turn or when you pick that player.", "Pushed to update-layout\n(commits 9c3aef6, baf2ea3)")
write("V3_Cast.dc.html", page("v3: The cast", 1600, 740, inner))
print("boards ok")
