"""Use existing local Three.js viewer technology without changing the drone viewer."""
from pathlib import Path
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from urllib.parse import urlsplit,unquote
import argparse
parser=argparse.ArgumentParser();parser.add_argument('--revision',default='R04',choices=['R01','R02','R03','R04']);parser.add_argument('--port',default=8770,type=int);args=parser.parse_args()
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Design/Models/IndoorDroneI75/I75Viewer'
VIEW=ROOT/'Design/Models/TouchConsole/ConsoleViewer'
VIEW.mkdir(exist_ok=True)
h=(SOURCE/'index.html').read_text(encoding='utf-8').replace('./vendor/','/vendor/').replace('I75 Modellansicht','Touchpult R02').replace('I–75','TOUCHPULT').replace('der I75','des Touchpults')
a=h.index('<select id=');b=h.index('</select>',a)+len('</select>')
h=h[:a]+'<select id="variant">'+''.join(f'<option value="{r}"'+(' selected' if r==args.revision else '')+f'>{r} · {label}</option>' for r,label in [('R01','ursprüngliches Modell'),('R02','kantiger Rahmen und Details'),('R03','längeres Display / Front 90 cm'),('R04','Front 60 cm')])+'</select>'+h[b:]
(VIEW/'index.html').write_text(h,encoding='utf-8')
j=(SOURCE/'viewer.js').read_text(encoding='utf-8').replace('I75 3D-Ansicht','Touchpult R02')
j=j.replace('loader.load(`../Surface-${variant}-2026-10-26/I75_V02_Surface_${variant}.glb`','loader.load(`/models/${variant}.glb`')
import re
j=re.sub(r"load\('R0[1-4]'\);",f"load('{args.revision}');",j).replace('cm Länge','cm Tiefe · Beispielanzeige')
j=j.replace('reset:[1.1,.65,1.3]','reset:[1.1,.95,-1.3]').replace('front:[0,.12,1],back:[0,.12,-1]','front:[0,.12,-1],back:[0,.12,1]')
j=j.replace('box.getCenter(center);','box.getCenter(center);center.y-=.15;')
(VIEW/'viewer.js').write_text(j,encoding='utf-8')
(VIEW/'index.html').write_text(h.replace('Touchpult R02','Touchpult · Variantenvergleich'),encoding='utf-8')
class Handler(SimpleHTTPRequestHandler):
    def translate_path(self,path):
        p=unquote(urlsplit(path).path)
        if '..' in Path(p).parts:return str(VIEW/'invalid')
        if p=='/model.glb':return str(ROOT/f'Design/Models/TouchConsole/Revision-{args.revision}/TouchConsole-{args.revision}.glb')
        if p in [f'/models/R0{i}.glb' for i in range(1,5)]:
            revision=Path(p).stem
            return str(ROOT/f'Design/Models/TouchConsole/Revision-{revision}/TouchConsole-{revision}.glb')
        if p.startswith('/vendor/'):return str(SOURCE/p.lstrip('/'))
        if p in ('/','/index.html','/viewer.js'):return str(VIEW/('index.html' if p=='/' else p.lstrip('/')))
        return str(VIEW/'invalid')
    def list_directory(self,path):self.send_error(403);return None
    def end_headers(self):self.send_header('Cache-Control','no-cache');super().end_headers()
print(f'Console viewer http://127.0.0.1:{args.port}/',flush=True)
ThreadingHTTPServer(('127.0.0.1',args.port),Handler).serve_forever()
