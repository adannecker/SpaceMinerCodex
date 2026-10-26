"""Local-only GLB preview. Serves the viewer and the two explicit model files."""
from pathlib import Path
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from urllib.parse import urlsplit,unquote
import argparse,json

ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/'Design/Models/IndoorDroneI75'
parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8769);args=parser.parse_args()
class Handler(SimpleHTTPRequestHandler):
    extensions_map={**SimpleHTTPRequestHandler.extensions_map,'.js':'text/javascript','.glb':'model/gltf-binary'}
    def __init__(self,*a,**kw):super().__init__(*a,directory=str(MODELS),**kw)
    def allowed(self):
        path=unquote(urlsplit(self.path).path)
        if '..' in Path(path).parts:return False
        return path.startswith('/I75Viewer/') or path in [f'/Surface-{v}-2026-10-26/I75_V02_Surface_{v}.glb' for v in ('R01','R02')]
    def do_GET(self):
        if urlsplit(self.path).path=='/health':
            body=json.dumps({'service':'spaceminer-i75-viewer','version':1}).encode()
            self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(body)));self.end_headers();self.wfile.write(body);return
        if not self.allowed():self.send_error(404);return
        super().do_GET()
    def do_HEAD(self):
        if not self.allowed():self.send_error(404);return
        super().do_HEAD()
    def list_directory(self,path):self.send_error(403);return None
    def end_headers(self):self.send_header('Cache-Control','no-cache');super().end_headers()

server=ThreadingHTTPServer(('127.0.0.1',args.port),Handler)
print(f'I75 viewer: http://127.0.0.1:{args.port}/I75Viewer/',flush=True)
server.serve_forever()
