"""v3.2 main menu mockup on the final street (M-A), same layout as MainMenuController."""
import sys
sys.path.insert(0, "/home/claude/envgen")
from lib import page, caption, write, GOLD, CHAR, CREAM
INK = "#1e1411"
css = f"""
.wrap{{position:relative;width:1600px;height:900px;overflow:hidden;flex:none}}
.screen{{position:relative;width:1920px;height:1080px;transform:scale(.8333);transform-origin:0 0;overflow:hidden;background:#000}}
.full{{position:absolute;inset:0;width:100%;height:100%;display:block}}
.clouds{{animation:drift 40s linear infinite alternate}}@keyframes drift{{from{{transform:translateX(-30px)}}to{{transform:translateX(60px)}}}}
.laundry{{transform-origin:83% 34%;animation:flap 1.9s ease-in-out infinite alternate}}@keyframes flap{{from{{transform:skewX(-1.2deg)}}to{{transform:skewX(1.2deg)}}}}
.crows{{animation:crows 14s linear infinite}}@keyframes crows{{from{{transform:translate(0,0)}}to{{transform:translate(-320px,-40px)}}}}
.shade{{position:absolute;inset:0;background:linear-gradient(90deg,rgba(14,8,6,.78) 0,rgba(14,8,6,.55) 34%,rgba(14,8,6,0) 52%)}}
.warm{{position:absolute;inset:0;background:rgba(255,168,92,.12);mix-blend-mode:soft-light}}
.logo{{position:absolute;left:110px;top:70px;font:400 128px/0.92 Anton,sans-serif;color:#f4d65a;letter-spacing:2px;
 -webkit-text-stroke:5px #b91c14;paint-order:stroke fill;text-shadow:5px 6px 0 #3a0a08,9px 11px 0 #1a0605;transform:rotate(-2deg);transform-origin:0 0}}
.logo span{{display:block;font-size:104px;color:{GOLD}}}
.logo{{animation:bob 4s ease-in-out infinite alternate}}@keyframes bob{{from{{translate:0 0}}to{{translate:0 -4px}}}}
.tagline{{position:absolute;left:114px;top:356px;font:italic 400 30px Roboto,sans-serif;color:{CREAM};text-shadow:0 2px 0 #000}}
.btn{{position:absolute;left:120px;width:492px;box-sizing:border-box;border:3px solid {INK};border-radius:14px;outline:3px solid rgba(239,230,210,.9);
 display:flex;align-items:center;padding-left:28px;font:400 38px Anton,sans-serif;letter-spacing:1px;box-shadow:0 7px 0 rgba(0,0,0,.55),8px 14px 0 rgba(0,0,0,.25)}}
.btn:hover{{translate:0 -3px}}
.small{{position:absolute;width:156px;height:70px;box-sizing:border-box;border:3px solid {INK};border-radius:12px;outline:3px solid rgba(239,230,210,.9);
 display:flex;align-items:center;justify-content:center;font:400 26px Anton,sans-serif;background:#13191d;color:{CREAM};box-shadow:0 6px 0 rgba(0,0,0,.55)}}
.new{{margin-left:auto;margin-right:24px;padding:4px 12px;border-radius:8px;background:{CHAR};color:#f4d65a;font-size:22px;border:2px solid {INK};transform:rotate(4deg)}}
.chip{{position:absolute;right:60px;top:60px;width:410px;height:90px;box-sizing:border-box;border-radius:14px;background:rgba(19,25,29,.88);border:3px solid {INK};outline:3px solid rgba(239,230,210,.85);
 display:flex;align-items:center;gap:14px;padding:0 14px;color:{CREAM}}}
.chip img{{width:64px;height:64px;object-fit:cover;object-position:50% 8%;border-radius:50%;background:#3e2a1f;border:3px solid {GOLD}}}
.chip b{{font:400 28px Anton,sans-serif;display:block}}.chip small{{font:400 18px Roboto,sans-serif;color:#cfc2ad}}
.pup{{position:absolute;right:30px;bottom:-6px;height:470px;filter:drop-shadow(10px 12px 6px rgba(0,0,0,.45));animation:breathe 3.4s ease-in-out infinite alternate;transform-origin:50% 100%}}
@keyframes breathe{{from{{transform:scaleY(1)}}to{{transform:scaleY(1.012)}}}}
.pupshadow{{display:none}}
.footer{{position:absolute;left:690px;bottom:40px;font:400 20px Roboto,sans-serif;color:#cfc2ad}}

.pbtn{{position:absolute;background-size:100% 100%;display:flex;align-items:center;padding:0 0 10px 40px;box-sizing:border-box;font-family:Anton,sans-serif;letter-spacing:1px;text-shadow:0 2px 0 rgba(0,0,0,.25);transition:translate .12s}}
.pbtn.sm{{justify-content:center;padding:0 0 10px 0}}
.pbtn:hover{{translate:0 -4px}}
.pbtn .new{{font-style:normal;margin-left:auto;margin-right:46px;padding:4px 12px;border-radius:8px;background:{CHAR};color:#f4d65a;font-size:22px;border:2px solid {INK};transform:rotate(5deg)}}
.shade2{{position:absolute;inset:0;background:linear-gradient(270deg,rgba(14,8,6,.55) 0,rgba(14,8,6,.25) 26%,rgba(14,8,6,0) 40%)}}
.mirror{{position:absolute;width:520px;height:720px}}
.mirror img{{position:absolute;inset:0;width:100%;height:100%}}
.mirror .glass{{position:absolute;inset:0;clip-path:ellipse(200px 255px at 260px 410px)}}
.mirror .me{{position:absolute;left:50%;top:auto;bottom:52px;width:auto;height:470px;inset:auto;translate:-50% 0;left:260px;bottom:50px;filter:brightness(.92) saturate(.9);animation:breathe 3.4s ease-in-out infinite alternate;transform-origin:50% 100%}}
.plaque{{position:absolute;width:360px;height:126px;background:url(./v3/ui3/plaque.png) 0 0/100% 100%;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:6px 0 26px;gap:2px;box-sizing:border-box;color:{CHAR}}}
.plaque b{{font:400 32px Anton,sans-serif;line-height:1}}.plaque small{{font:500 16px Roboto,sans-serif;color:#5a4a3a}}
.shopbox{{position:absolute;width:420px;height:258px}}
.shopbox img{{width:100%;height:100%;display:block;filter:saturate(.75) brightness(.9)}}
.sign{{position:absolute;left:50%;top:-30px;translate:-50% 0;padding:4px 22px;background:#e9dcc2;border:3px solid {INK};border-radius:6px;outline:3px solid rgba(239,230,210,.85);font:400 28px Anton,sans-serif;color:{INK};letter-spacing:2px;box-shadow:4px 6px 0 rgba(0,0,0,.35);transform:rotate(-2deg)}}
.soon{{position:absolute;right:-22px;top:56px;padding:6px 16px;background:#a8392f;color:#f4d65a;border:3px solid {INK};font:400 26px Anton,sans-serif;transform:rotate(12deg);box-shadow:4px 6px 0 rgba(0,0,0,.35)}}
.pts{{position:absolute;left:-30px;top:40px;width:70px;height:70px;border-radius:50%;background:#e0a23a;border:3px solid {INK};outline:3px solid rgba(239,230,210,.85);display:flex;flex-direction:column;align-items:center;justify-content:center;font:400 28px/0.9 Anton,sans-serif;color:{INK};box-shadow:4px 6px 0 rgba(0,0,0,.35)}}
.pts small{{font-size:13px}}
.shopnote{{position:absolute;left:0;right:0;bottom:-4px;text-align:center;font:500 15px Roboto,sans-serif;color:#efe6cf;text-shadow:0 2px 2px #000}}
@media (prefers-reduced-motion: reduce){{*{{animation:none!important}}}}
"""
def B(x, y, img, w, h, text, fg, size=40, extra="", cls=""):
    return (f'<div class="pbtn {cls}" style="left:{x}px;top:{y}px;width:{w}px;height:{h}px;background-image:url(./v3/ui3/{img}.png);color:{fg};font-size:{size}px">'
            f'<span>{text}</span>{extra}</div>')
btns = B(106, 420, "btn_play", 520, 120, "PLAY ONLINE", CREAM, 48)
btns += B(106, 546, "btn_bots", 520, 108, "PLAY VS BOTS", CHAR, 40)
btns += B(106, 662, "btn_tutorial", 520, 108, "TUTORIAL", CHAR, 40, extra='<i class="new">NEW</i>')
for i, t in enumerate(["CARDS", "PROFILE", "SETTINGS"]):
    btns += B(106 + i * 168, 778, "btn_small", 188, 98, t, CREAM, 26, cls="sm")
btns += B(106, 880, "btn_quit", 212, 92, "QUIT", CREAM, 28, cls="sm")
MX, MY = 1310, 22
mirror = f'''<div class="mirror" style="left:{MX}px;top:{MY}px">
<img src="./v3/ui3/mirror_back.png" alt=""><div class="glass"><img class="me" src="./v3/cast/c1.png" alt="Your avatar in the mirror"></div>
<img src="./v3/ui3/mirror_glass.png" alt=""><img src="./v3/ui3/mirror_frame.png" alt="Mirror"></div>
<div class="plaque" style="left:{MX+80}px;top:{MY+626}px"><b>ANDRE</b><small>12 wins · streak 3</small></div>
<div class="shopbox" style="left:{MX+50}px;top:{MY+768}px"><img src="./v3/ui3/shop.png" alt="Avatar shop, locked">
<div class="sign">TINDAHAN</div><div class="soon">SOON</div><div class="pts">0<small>PTS</small></div>
<div class="shopnote">Win games to earn points · clothes coming soon</div></div>'''
screen = f'''<div class="wrap"><div class="screen">
<img class="full" src="./v3/shero_d1_base.jpg" alt="Main menu street at sundown"><img class="full clouds" src="./v3/shero_d1_clouds.png" alt="">
<img class="full crows" src="./v3/shero_d1_crows.png" alt=""><img class="full laundry" src="./v3/shero_d1_laundry.png" alt="">
<div class="shade"></div><div class="shade2"></div><div class="warm"></div>
<div class="logo">TEN DAYS<span>TO SUNDOWN</span></div>
<div class="tagline">The bunker has room for two. Make sure one of them is you.</div>
{btns}{mirror}
<div class="footer">v0.9 · Vartcade Studios</div>
<img class="full" src="./v3/ui3/screen_frame.png" alt="">
</div></div>'''
inner = screen + caption("MENU", "MAIN MENU v3.3", "Paper-and-wood buttons after your zip references (bevel, lip, brass rivets, paper grain and a pale cut edge). Your avatar looks back from a vanity mirror with your name plaque; under it the TINDAHAN shop, locked until clothes exist. A wooden frame with brass corners and a slight vignette goes round the screen.", "Every piece is a PNG in v3/ui3\nready for 9-slice in Unity")
write("V3_Menu.dc.html", page("v3.3: Main menu", 1600, 980, inner, css=css))
print("menu ok")
