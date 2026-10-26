"""Author the paint stencils used by TextureIndoorDroneI75.py (Pillow)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Design/Models/IndoorDroneI75/Surface-R01-2026-10-26/Source'
OUT.mkdir(parents=True,exist_ok=True)
image=Image.new('RGBA',(1536,512)); draw=ImageDraw.Draw(image)
def font(size,bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if bold else 'ARIALN.TTF'),size)
white=(217,225,221,255); yellow=(240,173,55,255)
draw.text((28,8),'I–75',font=font(194,True),fill=white)
draw.text((610,24),'STATION SYSTEMS',font=font(56,True),fill=white)
draw.text((612,100),'INTERNAL SERVICE DRONE',font=font(43),fill=white)
draw.line((32,219,1498,219),fill=yellow,width=7)
draw.text((35,254),'02 / MODULAR UTILITY',font=font(65,True),fill=white)
draw.text((38,345),'MAGNETIC GUIDEWAY  •  QUICK-CHANGE TOOL INTERFACE',font=font(35),fill=white)
draw.text((38,420),'SM / I75–02     •     SERVICE ACCESS →',font=font(34),fill=white)
for i in range(66):
    x=1120+i*5
    if i%7 not in (1,4):draw.rectangle((x,405,x+2+(i%3==0),476),fill=white)
image.save(OUT/'I75_service-stencil.png')
print(OUT/'I75_service-stencil.png')
