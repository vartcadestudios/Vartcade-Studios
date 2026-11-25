# Builds the 6 cast puppets (C1-C6) for Unity: pupils, split back hair, skin-matched necks.
import json, numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage
L="/home/claude/psdout/layers/"
OUT="/home/claude/vartcade-studios-andre/Assets/_Project/Resources/Puppets/"
PREV="/home/claude/rig/cast_out/"
import os; os.makedirs(PREV,exist_ok=True)
INK=(26,15,12)
heads=json.load(open("heads.json")); lay=json.load(open("layout.json"))
TORSO={"big":("p2_g0_0_Layer24.png","p2_g0_2_Layer28.png"),"thin":("p3_0_Layer33.png","p3_3_Layer37.png")}
# collar curve (x0,x1, y at ends, y dip) in raw torso-crop px
COLLAR={"big":(250,462,88,118),"thin":(198,356,150,178)}
TEAL=np.array([0x4E,0x7C,0x86])
# code, body template (for torso/arm and head box), head file, skin, hair, eyes (source px boxes L,R), pupils [(cx,cy,r)], hair split
CAST=[
 ("c1","big_f","head_big_f.png",(198,138,94),(84,52,30),[(95,306,167,348),(255,318,357,358)],[],"c1"),
 ("c2","big_m","head_big_m.png",(232,176,134),(36,26,22),[(102,350,192,390),(260,340,352,380)],[(140,367,11),(297,362,10)],None),
 ("c3","big_m","h2/head_spiky_m.png",(160,104,66),(24,20,18),[(35,322,92,352),(167,318,265,352)],[(60,335,8),(222,336,9)],None),
 ("c4","thin_f","head_thin_f.png",(185,130,86),(122,58,32),[(102,288,187,344),(252,298,350,344)],[(150,316,10),(291,325,9)],"c4"),
 ("c5","thin_m","head_thin_m.png",(141,90,59),(30,24,22),[(37,246,95,282),(157,231,232,262)],[(66,264,8),(190,246,7)],None),
 ("c6","thin_f","h2/head_ear_f.png",(214,160,118),(52,36,28),[(115,374,200,426),(275,384,365,432)],[],None),
]
def near(a,col,tol):
    return (np.abs(a[:,:,:3].astype(int)-np.array(col)).sum(axis=2)<tol)&(a[:,:,3]>200)

def fill_eyes(im,eyes):
    # the sketch leaves the eye openings see-through: put eye whites behind them
    for e in eyes:
        box=(e[0]-6,e[1]-6,e[2]+6,e[3]+6)
        crop=im.crop(box); white=Image.new("RGBA",crop.size,(246,242,234,255))
        m=np.array(crop)[:,:,3]>0
        m=ndimage.binary_fill_holes(ndimage.binary_closing(m,iterations=3))|(np.array(crop)[:,:,3]>0)
        white.alpha_composite(crop); w=np.array(white); w[~ndimage.binary_dilation(m,iterations=0) if False else ~m,3]=0
        # only fill pixels the eye outline encloses
        inner=ndimage.binary_fill_holes(np.array(crop)[:,:,3]>160)
        out=np.array(crop); sel=inner&(out[:,:,3]<255)
        out[sel]=w[sel]; im.paste(Image.fromarray(out),box[:2])
    return im
def pupils(im,skin,pl):
    a=np.array(im); lum=a[:,:,:3].mean(axis=2)
    skinm=near(a,skin,60)
    for cx,cy,r in pl:
        yy,xx=np.ogrid[:a.shape[0],:a.shape[1]]
        d=np.hypot(xx-cx,yy-cy)
        eye=(d<=r)&~skinm&(lum>70)&(a[:,:,3]>200)
        a[eye,:3]=(70,46,34)                       # iris
        a[(d<=r*0.55)&eye,:3]=(18,10,8)             # pupil
        a[(np.hypot(xx-(cx-r*0.35),yy-(cy-r*0.4))<=max(1.6,r*0.28))&eye,:3]=(250,246,238)  # catch light
    return Image.fromarray(a)

def outline(mask,w):
    return mask&~ndimage.binary_erosion(mask,iterations=w)

def split_hair(code,im,skin,hair):
    a=np.array(im); H,W=a.shape[:2]
    skinm=ndimage.binary_fill_holes(near(a,skin,70))
    skinm=ndimage.binary_opening(skinm,iterations=2)
    keep=ndimage.binary_dilation(skinm,iterations=13)
    yy,xx=np.mgrid[:H,:W]
    if code=="c1":
        cut=330
        remove=(yy>cut)&~keep&(a[:,:,3]>0)
        remove|=(xx<98)&(yy>140)&~keep&(a[:,:,3]>0)   # the loose outer strand: redrawn in the back layer
        head=a.copy(); head[remove,3]=0
        BH=H+70; back=Image.new("RGBA",(W,BH),(0,0,0,0)); d=ImageDraw.Draw(back)
        pts=[(300,150),(150,140),(74,170),(58,300),(50,440),(46,580),(52,690),(66,735),(88,700),(112,742),(140,706),(170,728),(200,690),
             (400,690),(430,724),(462,694),(494,738),(524,700),(552,730),(574,690),(584,560),(586,400),(578,250),(552,160)]
        d.polygon(pts,fill=hair+(255,))
        m=np.array(back)[:,:,3]>0
        ba=np.array(back); ba[outline(m,6),:3]=INK
        back=Image.fromarray(ba); d=ImageDraw.Draw(back)
        for s in [[(80,380),(74,560),(84,690)],[(110,460),(104,600),(118,720)],[(540,420),(548,560),(536,700)],[(512,480),(516,640),(500,712)]]:
            d.line(s,fill=INK+(255,),width=3,joint="curve")
        return Image.fromarray(head),back,(0,0)
    # c4: lift the flared curls below the cheek line into their own layer
    cut=345
    remove=(yy>cut)&~keep&(a[:,:,3]>0)
    head=a.copy(); head[remove,3]=0
    b=np.zeros_like(a); b[remove]=a[remove]
    m=b[:,:,3]>0
    # hair-coloured fill under the lower face so a sway never opens a gap, and fill holes in the curls
    under=ndimage.binary_dilation(skinm&(yy>cut-40),iterations=8)
    full=ndimage.binary_fill_holes(m|under)
    add=full&~m
    b[add,:3]=hair; b[add,3]=255
    return Image.fromarray(head),Image.fromarray(b),(0,0)

TANK={"big":[(225,95),(300,105),(330,150),(360,165),(395,150),(425,105),(495,95),(540,180),(575,300),(600,450),(620,720),(108,720),(108,450),(115,330),(160,230),(205,140)],
      "thin":[(190,140),(245,150),(270,190),(295,200),(325,185),(345,140),(400,150),(440,240),(470,360),(490,600),(500,848),(98,848),(95,600),(100,400),(120,290),(160,200)]}
GREY=np.array([0x3a,0x3d,0x41])
def shade_fill(lum,base,div):
    sh=np.clip(lum/div,0.6,1.25)[...,None]
    return np.clip(np.array(base)*sh,0,255)

def torso_img(kind,skin):
    """Default outfit (v2.2): a dark grey long-sleeved top with a crew neck; the neck shows the character's skin.
    A brass paper-fastener (brad) marks the shoulder joint, like a paper puppet."""
    tf,af=TORSO[kind]
    im=Image.open(L+tf).convert("RGBA"); bb=im.getbbox(); im=im.crop(bb)
    a=np.array(im).astype(float); lum=a[:,:,:3].mean(axis=2); al=a[:,:,3]
    fill=lum>24
    H,W=lum.shape; yy,xx=np.mgrid[:H,:W]
    out=np.where(fill[...,None],shade_fill(lum,GREY,40),np.array(INK))
    x0,x1,ye,yd=COLLAR[kind]
    t=np.clip((xx-x0)/(x1-x0),0,1); curve=ye+(yd-ye)*np.sin(t*np.pi)
    neck=fill&(yy<curve)&(xx>x0-30)&(xx<x1+30)
    out[neck]=shade_fill(lum,skin,40)[neck]
    o=Image.fromarray(np.dstack([out,al]).astype(np.uint8),"RGBA"); d=ImageDraw.Draw(o)
    pts=[(x0+(x1-x0)*i/20,ye+(yd-ye)*np.sin(i/20*np.pi)) for i in range(21)]
    d.line(pts,fill=INK+(255,),width=7,joint="curve")
    pts2=[(x0+(x1-x0)*i/20,ye+10+(yd-ye)*np.sin(i/20*np.pi)) for i in range(21)]
    d.line(pts2,fill=(80,84,90,255),width=4,joint="curve")   # ribbed collar
    arm=Image.open(L+af).convert("RGBA"); ab=arm.getbbox(); arm=arm.crop(ab)
    aa=np.array(arm).astype(float); al2=aa[:,:,3]; l2=aa[:,:,:3].mean(axis=2)
    f2=l2>24
    arm=Image.fromarray(np.dstack([np.where(f2[...,None],shade_fill(l2,GREY,90),np.array(INK)),al2]).astype(np.uint8),"RGBA")
    # cuff near the bottom of the sleeve
    aw,ah=arm.size; da=ImageDraw.Draw(arm)
    am=np.array(arm)[:,:,3]>100
    ys=np.nonzero(am.any(axis=1))[0]; yc=int(ys.max()-ah*0.07)
    xs=np.nonzero(am[yc])[0]
    if len(xs): da.line([(xs.min()+4,yc),(xs.max()-4,yc-6)],fill=INK+(255,),width=5)
    # brass brad at the shoulder pivot
    bx,by=aw*0.5,ah*0.07; r=max(9,aw*0.06)
    da.ellipse((bx-r,by-r,bx+r,by+r),fill=(201,160,74,255),outline=INK+(255,),width=4)
    da.ellipse((bx-r*0.4,by-r*0.55,bx+r*0.1,by-r*0.1),fill=(245,220,150,255))
    return o,bb,arm,ab

HPX=640
table=[]
for code,body,hf,skin,hair,eyes,pl,split in CAST:
    kind=body.split("_")[0]
    head=Image.open(hf).convert("RGBA")
    head=fill_eyes(head,eyes)
    head=pupils(head,skin,pl)
    back=None
    if split: head,back,_=split_hair(code,head,skin,hair)
    torso,tb,arm,ab=torso_img(kind,skin)
    # place everything in the body's frame (pixels at HPX tall), same as the shipped layout
    l=lay[body]; W=l["aspect"]*HPX
    def px(r): return (r[0]*W,(1-r[3])*HPX,r[2]*W,(1-r[1])*HPX)  # x0,top,x1,bottom
    T=px(l["parts"]["torso"]); A=px(l["parts"]["arm"]); Hb=px(l["parts"]["head"])
    if hf.startswith("h2/"):
        bh=Hb[3]-Hb[1]; s=bh/head.height; w=head.width*s; cx=(Hb[0]+Hb[2])/2
        Hr=(cx-w/2,Hb[3]-bh,cx+w/2,Hb[3])
    else:
        Hr=Hb; s=(Hb[3]-Hb[1])/head.height
    rects={"torso":T,"arm":A,"head":Hr}
    if back is not None:
        rects["hairback"]=(Hr[0],Hr[1],Hr[0]+back.width*s,Hr[1]+back.height*s)
    X0=min(r[0] for r in rects.values()); Y0=min(r[1] for r in rects.values()); X1=max(r[2] for r in rects.values()); Y1=max(r[3] for r in rects.values())
    k=HPX/(Y1-Y0)
    # every part gets a pale cut-paper edge (v3.3), so each sprite is padded by PAD px all round
    PAD=5; pu=PAD/k
    inner={n:r for n,r in rects.items()}
    rects={n:(r[0]-pu,r[1]-pu,r[2]+pu,r[3]+pu) for n,r in rects.items()}
    X0=min(r[0] for r in rects.values()); Y0=min(r[1] for r in rects.values()); X1=max(r[2] for r in rects.values()); Y1=max(r[3] for r in rects.values())
    k=HPX/(Y1-Y0); FW=(X1-X0)*k
    norm={n:[(r[0]-X0)/(X1-X0),1-(r[3]-Y0)/(Y1-Y0),(r[2]-X0)/(X1-X0),1-(r[1]-Y0)/(Y1-Y0)] for n,r in rects.items()}
    imgs={"torso":torso,"arm":arm,"head":head}
    if back is not None: imgs["hairback"]=back
    sizes={}
    for n,img in imgs.items():
        r=rects[n]; wo=max(1,round((r[2]-r[0])*k)); ho=max(1,round((r[3]-r[1])*k))
        ri=inner[n]; wi=max(1,round((ri[2]-ri[0])*k)); hi=max(1,round((ri[3]-ri[1])*k))
        cv=Image.new("RGBA",(wo,ho),(0,0,0,0)); px0=(wo-wi)//2; py0=(ho-hi)//2
        cv.alpha_composite(img.resize((wi,hi),Image.LANCZOS),(px0,py0))
        a=np.array(cv); al=a[:,:,3]>128
        ring=ndimage.binary_dilation(al,iterations=3)&~al
        a[ring]=(238,229,208,240)
        Image.fromarray(a).save(f"{OUT}{code}_{n}.png",optimize=True)
        sizes[n]=(wi,hi,wo,ho,px0,py0)
    # eyes in head-local 0-1, y up (adjusted for the padding)
    wi,hi,wo,ho,px0,py0=sizes["head"]
    ev=[[(e[0]/head.width*wi+px0)/wo,(((1-e[3]/head.height)*hi)+(ho-hi-py0))/ho,(e[2]/head.width*wi+px0)/wo,(((1-e[1]/head.height)*hi)+(ho-hi-py0))/ho] for e in eyes]
    table.append((code,FW/HPX,skin,norm,ev))
    # preview, drawn in order: hairback, torso, arm, head
    cv=Image.new("RGBA",(round(FW)+20,HPX+20),(0,0,0,0))
    for n in ["hairback","torso","arm","head"]:
        if n not in imgs: continue
        r=norm[n]; p=Image.open(f"{OUT}{code}_{n}.png")
        cv.alpha_composite(p,(10+round(r[0]*FW),10+round((1-r[3])*HPX)))
    cv.save(f"{PREV}{code}.png")
    print(code,round(FW),list(rects))
json.dump([{"code":c,"aspect":a,"skin":s,"parts":n,"eyes":e} for c,a,s,n,e in table],open("cast_layout.json","w"),indent=1)
