"""Operator arm preview using the project's existing Three.js viewer components."""
from pathlib import Path
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from urllib.parse import urlsplit,unquote
import re
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Design/Models/IndoorDroneI75/I75Viewer'
BASE=ROOT/'Design/Models/OperatorArm';VIEW=BASE/'Viewer';VIEW.mkdir(exist_ok=True)
h=(SOURCE/'index.html').read_text(encoding='utf-8').replace('./vendor/','/vendor/').replace('I75 Modellansicht','Operator-Arm').replace('I–75','OPERATOR ARM').replace('der I75','des Operator-Arms')
a=h.index('<select id=');b=h.index('</select>',a)+len('</select>')
h=h[:a]+'<select id="variant" aria-label="Modellvariante"><option value="R04">R04 · Blender / Space Operator</option><option value="R03">R03 · übernommenes Tripo-Ausgangsmodell</option><option value="R02">R02 · verworfener Textentwurf</option><option value="Original">Tripo · erster kostenloser Neulauf</option></select>'+h[b:]
h=h.replace('#stage{position:absolute;inset:0;', '#stage{position:absolute;inset:60px 0 220px;')
(VIEW/'index.html').write_text(h,encoding='utf-8')
j=(SOURCE/'viewer.js').read_text(encoding='utf-8').replace('I75 3D-Ansicht','Operator-Arm 3D-Ansicht')
j=j.replace('loader.load(`../Surface-${variant}-2026-10-26/I75_V02_Surface_${variant}.glb`','loader.load(`/models/${variant}.glb`')
j=re.sub(r"load\('R0[1-4]'\);","load('R04');",j)
j=j.replace('reset:[1.1,.65,1.3]','reset:[0,.2,1.6]')
j=j.replace('floor.receiveShadow=true','floor.receiveShadow=true;floor.visible=false')
j=re.sub(r'    status.textContent=`\$\{variant\} · .*?;', '    status.textContent=`${variant} · ${triangles.toLocaleString("de-DE")} Dreiecke · Maßstab noch offen`;', j)
(VIEW/'viewer.js').write_text(j,encoding='utf-8')
class Handler(SimpleHTTPRequestHandler):
 def translate_path(self,path):
  p=unquote(urlsplit(path).path)
  if '..' in Path(p).parts:return str(VIEW/'invalid')
  if p=='/models/Original.glb':return str(BASE/'Original/Tripo-H31-Retry.glb')
  if p=='/models/R01.glb':return str(BASE/'Revision-R01/OperatorArm-R01.glb')
  if p=='/models/R02.glb':return str(BASE/'Revision-R02/OperatorArm-R02.glb')
  if p=='/models/R03.glb':return str(BASE/'Revision-R03/OperatorArm-R03.glb')
  if p=='/models/R04.glb':return str(BASE/'Revision-R04/OperatorArm-R04-Preview.glb')
  if p.startswith('/vendor/'):return str(SOURCE/p.lstrip('/'))
  if p in ('/','/index.html','/viewer.js'):return str(VIEW/('index.html' if p=='/' else p.lstrip('/')))
  return str(VIEW/'invalid')
 def list_directory(self,path):self.send_error(403);return None
 def end_headers(self):self.send_header('Cache-Control','no-cache');super().end_headers()
print('Operator arm viewer: http://127.0.0.1:8786/',flush=True)
ThreadingHTTPServer(('127.0.0.1',8786),Handler).serve_forever()
