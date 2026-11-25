"""v3 game mockup: the room in play with HUD, cards, the day/night dial and transition, and the Rondo wheel spin.
Also a close-up board for the dial and the wheel."""
import sys, math, random
sys.path.insert(0, "/home/claude/envgen")
from lib import page, caption, write, GOLD, CHAR, CREAM

INK = "#1e1411"
RONDO = ["purge", "trading_only", "hunger", "tornado", "doomsday", "blackout", "outbreak", "peaceful"]
RNAME = ["PURGE", "TRADING ONLY", "HUNGER", "TORNADO", "DOOMSDAY", "BLACKOUT", "OUTBREAK", "PEACEFUL"]
WEDGE = ["#e9dcc2", "#36585f", "#e9dcc2", "#a8392f", "#e9dcc2", "#36585f", "#e9dcc2", "#a8392f"]
LAND = 3   # lands on Tornado in the demo

def dial_svg(cls="dial", label="DAY 3"):
    """Day/night porthole: a disc with day on top and night below turns 180 degrees; a ring of ticks counts the phase."""
    random.seed(2)
    stars = "".join(f'<circle cx="{random.uniform(40,160):.0f}" cy="{random.uniform(110,170):.0f}" r="{random.choice([1.2,1.6,2]):.1f}" fill="#efe6cf"/>' for _ in range(14))
    ticks = ""
    for i in range(24):
        a = math.radians(i * 15 - 90)
        x0, y0 = 100 + 82 * math.cos(a), 100 + 82 * math.sin(a)
        x1, y1 = 100 + 94 * math.cos(a), 100 + 94 * math.sin(a)
        ticks += f'<line class="tick" style="animation-delay:{i*0.18:.2f}s" x1="{x0:.1f}" y1="{y0:.1f}" x2="{x1:.1f}" y2="{y1:.1f}" stroke="#f2c94c" stroke-width="5" stroke-linecap="round"/>'
    return f'''<svg class="{cls}" viewBox="0 0 200 236" role="img" aria-label="Day and night dial">
<defs><clipPath id="port"><circle cx="100" cy="100" r="70"/></clipPath>
<linearGradient id="dayg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6a8ab0"/><stop offset=".7" stop-color="#e9a868"/><stop offset="1" stop-color="#f6c47a"/></linearGradient>
<linearGradient id="nightg" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="#05070f"/><stop offset="1" stop-color="#1a2440"/></linearGradient></defs>
<g>{ticks}</g>
<circle cx="100" cy="100" r="76" fill="#3e2a1f" stroke="{INK}" stroke-width="3"/>
<g clip-path="url(#port)"><g class="disc" style="transform-origin:100px 100px">
<rect x="0" y="0" width="200" height="100" fill="url(#dayg)"/><rect x="0" y="100" width="200" height="100" fill="url(#nightg)"/>
<circle cx="100" cy="52" r="15" fill="#f6a24a" stroke="{INK}" stroke-width="2"/>
<g transform="rotate(180 100 100)"><circle cx="100" cy="52" r="13" fill="#e8e2cc" stroke="{INK}" stroke-width="2"/><circle cx="106" cy="47" r="11" fill="#1a2440"/></g>{stars}</g>
<path d="M20 112 L40 100 L52 106 L68 94 L86 104 L104 90 L122 102 L140 94 L160 104 L180 98 L180 180 L20 180Z" fill="#160e10"/>
<rect x="56" y="118" width="8" height="10" fill="#f2c46a" opacity=".7"/><rect x="120" y="114" width="8" height="10" fill="#f2c46a" opacity=".7"/></g>
<circle cx="100" cy="100" r="70" fill="none" stroke="{INK}" stroke-width="3"/>
<circle cx="100" cy="100" r="74" fill="none" stroke="#7a5640" stroke-width="5"/>
{"".join(f'<circle cx="{100+74*math.cos(math.radians(a)):.1f}" cy="{100+74*math.sin(math.radians(a)):.1f}" r="2.6" fill="#c9b08a"/>' for a in range(0,360,45))}
<rect x="44" y="190" width="112" height="38" rx="8" fill="#a8392f" stroke="{INK}" stroke-width="3"/>
<text class="lbl l3" x="100" y="218" text-anchor="middle" font-family="Anton" font-size="24" fill="#f4d65a">{label}</text>
<text class="lbl l4" x="100" y="218" text-anchor="middle" font-family="Anton" font-size="24" fill="#f4d65a">DAY 4</text></svg>'''

def wheel_svg(cls="wheel", size=240):
    """Rondo wheel: 8 wedges with the result badges, bulbs on the rim, a brass pointer and the Silong sun hub."""
    w = ""
    for i in range(8):
        a0, a1 = math.radians(i * 45 - 22.5 - 90), math.radians(i * 45 + 22.5 - 90)
        p = f"M100 100 L{100+88*math.cos(a0):.2f} {100+88*math.sin(a0):.2f} A88 88 0 0 1 {100+88*math.cos(a1):.2f} {100+88*math.sin(a1):.2f}Z"
        w += f'<path d="{p}" fill="{WEDGE[i]}" stroke="{INK}" stroke-width="2"/>'
        am = math.radians(i * 45 - 90)
        cx, cy = 100 + 58 * math.cos(am), 100 + 58 * math.sin(am)
        w += f'<image href="./v3/rondo/rondo_{RONDO[i]}.png" x="{cx-21:.1f}" y="{cy-21:.1f}" width="42" height="42" transform="rotate({i*45} {cx:.1f} {cy:.1f})"/>'
    bulbs = "".join(f'<circle class="bulb b{i%2}" cx="{100+95*math.cos(math.radians(i*22.5)):.1f}" cy="{100+95*math.sin(math.radians(i*22.5)):.1f}" r="3.6" fill="#ffe08a" stroke="{INK}" stroke-width="1"/>' for i in range(16))
    sun = "".join(f'<line x1="{100+16*math.cos(math.radians(a)):.1f}" y1="{100+16*math.sin(math.radians(a)):.1f}" x2="{100+22*math.cos(math.radians(a)):.1f}" y2="{100+22*math.sin(math.radians(a)):.1f}" stroke="#c48a1c" stroke-width="2.4" stroke-linecap="round"/>' for a in range(0, 360, 30))
    return f'''<svg class="{cls}" viewBox="0 0 200 220" width="{size}" role="img" aria-label="Rondo wheel">
<ellipse cx="100" cy="210" rx="70" ry="9" fill="#000" opacity=".35"/>
<circle cx="100" cy="100" r="100" fill="#3e2a1f" stroke="{INK}" stroke-width="3"/>{bulbs}
<g class="spin" style="transform-origin:100px 100px">{w}<circle cx="100" cy="100" r="88" fill="none" stroke="{INK}" stroke-width="3"/></g>
<circle cx="100" cy="100" r="15" fill="#f2c94c" stroke="{INK}" stroke-width="2.4"/>{sun}
<ellipse cx="95" cy="98" rx="1.8" ry="3" fill="{INK}"/><ellipse cx="105" cy="98" rx="1.8" ry="3" fill="{INK}"/><path d="M94 104 q6 5 12 0" stroke="{INK}" stroke-width="1.6" fill="none"/>
<g class="pointer" style="transform-origin:100px 6px"><path d="M88 -4 L112 -4 L100 22Z" fill="#d8b04a" stroke="{INK}" stroke-width="2.4"/><circle cx="100" cy="2" r="4" fill="#8a6a2a"/></g></svg>'''

T = 20  # loop seconds
def pct(s): return f"{s / T * 100:.2f}%"

css = f"""
.screen{{position:relative;width:1920px;height:1080px;transform:scale(.8333);transform-origin:0 0;overflow:hidden;background:#000}}
.wrap{{position:relative;width:1600px;height:900px;overflow:hidden;flex:none}}
.full{{position:absolute;inset:0;width:100%;height:100%;display:block}}
.layer{{position:absolute;inset:0}}
.night{{animation:night {T}s linear infinite}}
@keyframes night{{0%,{pct(5)}{{opacity:0}}{pct(6.4)},{pct(11)}{{opacity:1}}{pct(12.4)},100%{{opacity:0}}}}
.lamp{{transform-origin:50% 8.9%;animation:sway 3.2s ease-in-out infinite alternate}}
@keyframes sway{{from{{transform:rotate(-1.6deg)}}to{{transform:rotate(1.6deg)}}}}
.fan{{transform-origin:93.75% 42%;animation:spin .7s linear infinite}}
.spinA{{animation:spin .7s linear infinite}}
@keyframes spin{{to{{transform:rotate(360deg)}}}}
.bob1{{animation:bob 3.4s ease-in-out infinite alternate}}.bob2{{animation:bob 3.9s ease-in-out -1.2s infinite alternate}}.bob3{{animation:bob 3.1s ease-in-out -2s infinite alternate}}
@keyframes bob{{from{{transform:translateY(0)}}to{{transform:translateY(-3px)}}}}
.spider{{animation:drop 5s ease-in-out infinite alternate}}@keyframes drop{{0%,30%{{transform:translateY(0)}}100%{{transform:translateY(26px)}}}}
.dust{{position:absolute;width:3px;height:3px;background:#f4e6c4;animation:rise 6s linear infinite;opacity:0}}
@keyframes rise{{0%{{transform:translateY(0);opacity:0}}15%{{opacity:.9}}100%{{transform:translateY(-200px);opacity:0}}}}
/* HUD */
.hud{{position:absolute;inset:0;font-family:Anton,sans-serif}}
.dial{{position:absolute;left:30px;top:18px;width:170px}}
.dial .disc{{animation:disc {T}s ease-in-out infinite}}
@keyframes disc{{0%,{pct(5)}{{transform:rotate(0deg)}}{pct(6.6)},{pct(11)}{{transform:rotate(180deg)}}{pct(12.6)},100%{{transform:rotate(360deg)}}}}
.dial .tick{{opacity:.18;animation:tick {T}s linear infinite}}
@keyframes tick{{0%{{opacity:.18}}1%{{opacity:1}}{pct(5)}{{opacity:1}}{pct(5.2)},100%{{opacity:.18}}}}
.plate{{position:absolute;box-sizing:border-box;background:linear-gradient(#41686f,#30515a);border:3px solid {INK};border-radius:12px;box-shadow:inset 0 3px 0 rgba(255,255,255,.18),0 6px 0 #1a2a2e,4px 10px 0 rgba(0,0,0,.35);color:{CREAM};display:flex;align-items:center;justify-content:center;font-size:26px;letter-spacing:1px;text-shadow:0 2px 0 #1a0f0a}}
.plate.hot{{background:linear-gradient(#b84a3c,#922e24);box-shadow:inset 0 3px 0 rgba(255,255,255,.2),0 6px 0 #5a1a14,4px 10px 0 rgba(0,0,0,.35)}}
.info{{position:absolute;left:214px;top:44px;color:{CREAM};font-size:30px;text-shadow:0 2px 0 #000}}
.info small{{display:block;font:500 17px Roboto,sans-serif;color:#e4d8c4;letter-spacing:.5px}}
.l3{{animation:l3 {T}s steps(1) infinite}}.l4{{opacity:0;animation:l4 {T}s steps(1) infinite}}
@keyframes l3{{0%{{opacity:1}}{pct(12)}{{opacity:0}}}}@keyframes l4{{0%{{opacity:0}}{pct(12)}{{opacity:1}}}}
.torn{{opacity:0;animation:torn {T}s linear infinite}}
@keyframes torn{{0%,{pct(16.6)}{{opacity:0}}{pct(17.4)},{pct(19.3)}{{opacity:1}}100%{{opacity:0}}}}
.tag{{position:absolute;transform:translateX(-50%);padding:6px 14px;border-radius:8px;background:rgba(19,25,29,.9);color:{CREAM};font:400 22px Anton,sans-serif;letter-spacing:.5px;white-space:nowrap}}
.tag b{{color:{GOLD};font-weight:400}}
.ocard{{position:absolute;width:62px;height:87px;border-radius:5px;box-shadow:2px 4px 0 rgba(0,0,0,.45)}}
.hand{{position:absolute;left:50%;bottom:-40px;width:0;height:0}}
.hcard{{position:absolute;width:156px;height:220px;border-radius:9px;box-shadow:0 6px 0 rgba(0,0,0,.45);transform-origin:50% 140%}}
.hcard.up{{animation:lift 4s ease-in-out infinite}}
@keyframes lift{{0%,30%{{translate:0 0}}45%,75%{{translate:0 -46px}}90%,100%{{translate:0 0}}}}
.pile{{position:absolute;width:118px;height:166px;border-radius:8px;box-shadow:4px 7px 0 rgba(0,0,0,.45)}}
.wheel{{position:absolute;left:1240px;top:712px;width:220px;filter:drop-shadow(6px 10px 0 rgba(0,0,0,.35))}}
.wheel .spin{{animation:wspin {T}s cubic-bezier(.12,.6,.15,1) infinite}}
@keyframes wspin{{0%,{pct(13.2)}{{transform:rotate(0deg)}}{pct(16.4)},{pct(19.4)}{{transform:rotate({1440 + (360 - LAND*45)}deg)}}100%{{transform:rotate({1440 + (360 - LAND*45)}deg)}}}}
.wheel .pointer{{animation:tickp {T}s linear infinite}}
@keyframes tickp{{0%,{pct(13.2)}{{transform:rotate(0)}}{pct(13.5)}{{transform:rotate(-14deg)}}{pct(13.8)}{{transform:rotate(0)}}{pct(14.1)}{{transform:rotate(-14deg)}}{pct(14.5)}{{transform:rotate(0)}}{pct(14.9)}{{transform:rotate(-12deg)}}{pct(15.4)}{{transform:rotate(0)}}{pct(16)}{{transform:rotate(-8deg)}}{pct(16.4)},100%{{transform:rotate(0)}}}}
.wheel .bulb{{animation:bulb .5s steps(1) infinite}}.wheel .b1{{animation-delay:-.25s}}
@keyframes bulb{{50%{{opacity:.25}}}}
.yourturn{{position:absolute;left:50%;top:120px;translate:-50% 0;padding:14px 40px;background:#a8392f;border:4px solid {INK};border-radius:12px;font:400 64px Anton,sans-serif;color:#f4d65a;-webkit-text-stroke:2px #3a0a08;box-shadow:6px 8px 0 rgba(0,0,0,.4);animation:yt {T}s ease-out infinite;opacity:0}}
@keyframes yt{{0%{{opacity:0;transform:scale(.6)}}1.5%{{opacity:1;transform:scale(1.06)}}2.5%{{transform:scale(1)}}{pct(2.6)}{{opacity:1}}{pct(3.2)},100%{{opacity:0}}}}
.phase{{position:absolute;left:840px;top:250px;width:240px;height:338px;border-radius:12px;box-shadow:8px 12px 0 rgba(0,0,0,.45);opacity:0;transform-origin:0 0}}
.pn{{animation:pn {T}s ease-in infinite}}
@keyframes pn{{0%,{pct(5.4)}{{opacity:0;transform:translateY(-500px)}}{pct(6)}{{opacity:1;transform:translateY(0)}}{pct(7.6)}{{opacity:1;transform:none}}{pct(8.4)}{{opacity:0;transform:rotate(-35deg) translate(60px,300px)}}100%{{opacity:0}}}}
.pd{{animation:pd {T}s ease-in infinite}}
@keyframes pd{{0%,{pct(11.4)}{{opacity:0;transform:translateY(-500px)}}{pct(12)}{{opacity:1;transform:translateY(0)}}{pct(12.8)}{{opacity:1;transform:none}}{pct(13.3)}{{opacity:0;transform:rotate(-35deg) translate(60px,300px)}}100%{{opacity:0}}}}
.result{{position:absolute;left:50%;top:150px;translate:-50% 0;display:flex;align-items:center;gap:18px;padding:12px 30px 12px 14px;background:#13191d;border:4px solid {GOLD};border-radius:14px;opacity:0;animation:res {T}s ease-out infinite;box-shadow:6px 8px 0 rgba(0,0,0,.4)}}
.result img{{width:110px;height:110px}}
.result div{{font:400 54px Anton,sans-serif;color:#f4d65a}}
.result small{{display:block;font:500 18px Roboto,sans-serif;color:{CREAM}}}
@keyframes res{{0%,{pct(16.5)}{{opacity:0;transform:scale(.7)}}{pct(16.9)}{{opacity:1;transform:scale(1.05)}}{pct(17.2)}{{transform:scale(1)}}{pct(19.2)}{{opacity:1}}{pct(19.7)},100%{{opacity:0}}}}
.nightdim{{position:absolute;inset:0;background:radial-gradient(circle at 50% 28%,rgba(255,210,122,0) 0 12%,rgba(20,30,80,.45) 40%,rgba(5,8,25,.7) 100%);mix-blend-mode:multiply;animation:night {T}s linear infinite;opacity:0}}
.warm{{position:absolute;inset:0;background:rgba(255,168,92,.12);mix-blend-mode:soft-light;pointer-events:none}}
.paper{{position:absolute;inset:0;background-image:url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='300' height='300'><filter id='p'><feTurbulence type='fractalNoise' baseFrequency='.04 .5' numOctaves='3' stitchTiles='stitch'/><feColorMatrix type='matrix' values='0 0 0 0 .55  0 0 0 0 .45  0 0 0 0 .32  0 0 0 .9 0'/></filter><rect width='100%' height='100%' filter='url(%23p)'/></svg>");mix-blend-mode:multiply;opacity:.3;pointer-events:none}}
.btn{{position:absolute;height:54px;padding:0 6px;font-size:24px}}
@media (prefers-reduced-motion: reduce){{*{{animation:none!important}}}}
"""

def room_group(tag, cls=""):
    L = lambda k, c="": f'<img class="full {c}" src="./v3/hero_{tag}_{k}.png" alt="">'
    return (f'<div class="layer {cls}"><img class="full" src="./v3/hero_{tag}_base.jpg" alt="Room {tag}">{L("spider","spider")}{L("fan","fan")}'
            f'{L("seat1","bob1")}{L("seat2","bob2")}{L("seat3","bob3")}{L("table")}{L("lamp","lamp")}</div>')

random.seed(4)
dust = "".join(f'<div class="dust" style="left:{random.uniform(44,56):.1f}%;top:{random.uniform(30,52):.1f}%;animation-delay:-{random.uniform(0,6):.1f}s"></div>' for _ in range(16))

def opp_cards(cx, cy, n, tilt):
    s = ""
    fronts = ["front_food", "front_water", "front_medicine", "front_entertainment", "front_swap_deck"]
    for i in range(n):
        x = cx + (i - (n - 1) / 2) * 40
        s += f'<img class="ocard" src="./v3/cards/{fronts[(i + cx) % 5]}.jpg" alt="" style="left:{x-31:.0f}px;top:{cy:.0f}px;transform:rotate({180+tilt+(i-(n-1)/2)*4:.0f}deg)">'
    return s

hand_cards = ["front_food", "front_water", "front_skip_next_player", "front_medicine", "front_reverse_turn", "front_entertainment"]
hand = ""
for i, f in enumerate(hand_cards):
    k = i - (len(hand_cards) - 1) / 2
    up = " up" if i == 3 else ""
    hand += f'<img class="hcard{up}" src="./v3/cards/{f}.jpg" alt="" style="left:{k*112-78:.0f}px;top:-200px;transform:rotate({k*5:.1f}deg) translateY({abs(k)*abs(k)*5:.0f}px)">'

screen = f'''<div class="wrap"><div class="screen">
{room_group("d1")}{room_group("n1","night")}<img class="full torn" src="./v3/room_tornado.jpg" alt="Tornado room">{dust}
<div class="nightdim"></div><div class="warm"></div><div class="paper"></div>
<div class="hud">
{opp_cards(520, 690, 4, -14)}{opp_cards(960, 684, 5, 0)}{opp_cards(1400, 690, 3, 14)}
<div class="tag" style="left:520px;top:600px">ANA <b>· 4</b></div><div class="tag" style="left:960px;top:600px">BOY <b>· 5</b></div><div class="tag" style="left:1400px;top:600px">JUN <b>· 3</b></div>
<img class="pile" src="./v3/cards/back_facedown.jpg" alt="Draw pile" style="left:690px;top:760px;transform:rotate(-4deg)">
<img class="pile" src="./v3/cards/back_facedown.jpg" alt="" style="left:696px;top:754px;transform:rotate(2deg)">
<img class="pile" src="./v3/cards/front_swap_deck.jpg" alt="Last played" style="left:930px;top:770px;transform:rotate(8deg)">
{wheel_svg()}
<div class="hand">{hand}</div>
{dial_svg()}
<div class="info l3">DAY 3 OF 10<small>Sundown in 8 days · your turn</small></div><div class="info l4">DAY 4 OF 10<small>Sundown in 7 days · Rondo spin</small></div>
<div class="plate btn" style="right:120px;top:30px;width:56px">II</div><div class="plate btn" style="right:36px;top:30px;width:56px">☰</div>
<div class="plate btn hot" style="right:40px;bottom:210px;width:200px;color:#f4d65a">PLAY CARD</div>
<div class="plate btn" style="right:40px;bottom:140px;width:200px">TRADE</div>
<div class="plate btn" style="right:40px;bottom:70px;width:200px">END TURN</div>
<div class="yourturn">YOUR TURN</div>
<img class="phase pn" src="./v3/cards/night_03.jpg" alt="Night 3"><img class="phase pd" src="./v3/cards/day_04.jpg" alt="Day 4">
<div class="result"><img src="./v3/rondo/rondo_tornado.png" alt=""><div>TORNADO<small>The Rondo landed: the room changes for this Day</small></div></div>
</div></div></div>'''

inner = screen + caption("GAME", "WHAT A MATCH LOOKS LIKE (20 s LOOP)", "Your turn on Day 3, then Night falls: the dial turns, the room darkens and the NIGHT 3 card drops and rips. Day 4 comes back and the Rondo spins and lands on Tornado. Opponents' cards face them; your hand fans along the bottom; the dial's ticks count the turn.", "Day 0-5 s · Night 5-12 s\nRondo 13-17 s")
write("V3_Game.dc.html", page("v3: The game, in motion", 1600, 980, inner, css=css))

# ---------------- close-up: dial + wheel
css2 = css + """.big{display:flex;gap:80px;align-items:center;justify-content:center;flex:1;padding:30px}
.big .dial{position:static;width:300px}.big .wheel{position:static;width:380px;filter:drop-shadow(6px 10px 0 rgba(0,0,0,.35))}
.note{font:400 15px/1.5 Roboto,sans-serif;color:#e4d8c4;max-width:330px}.note b{color:#f4d65a;font:400 22px Anton,sans-serif;display:block;margin-bottom:6px}"""
inner = f'''<div class="big">{dial_svg()}<div class="note"><b>DAY / NIGHT DIAL</b>A porthole with a sky disc: day on top, night below. It turns half a circle when Night falls and again when Day comes (after the GIFs in your zip). The 24 ticks light up through your turn, like the timer ring in the second GIF.</div>
{wheel_svg(size=380)}<div class="note"><b>RONDO WHEEL</b>Eight wedges with the result badges, bulbs that chase on the rim, a brass pointer that clicks over the pegs, and the Silong sun on the hub. It spins fast, slows down, wobbles the pointer, then shows the result banner.</div></div>''' + caption("UI", "DIAL AND WHEEL, CLOSE UP", "Same 20 s loop as the game board. In Unity both are UI pieces: the disc and the wheel rotate in code, the ticks and bulbs are Images.", "Replaces the current\nRondo dial and Day label")
write("V3_DialWheel.dc.html", page("v3: Dial and wheel", 1600, 620, inner, css=css2))
print("game ok")
