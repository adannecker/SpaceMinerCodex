import http from 'node:http';
import {readFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
const root=path.dirname(fileURLToPath(import.meta.url));
const port=Number(process.env.TECHTREE_PORT||4317);
const mime={'.html':'text/html; charset=utf-8','.mjs':'text/javascript; charset=utf-8','.css':'text/css; charset=utf-8','.svg':'image/svg+xml','.png':'image/png','.json':'application/json; charset=utf-8'};
http.createServer(async(req,res)=>{
 try{
  const url=new URL(req.url,'http://127.0.0.1');
  let file;
  if(url.pathname==='/catalog.json')file=path.resolve(root,'../../Assets/SpaceMiner/Resources/Research/catalog.json');
  else {
   file=path.resolve(root,'.'+decodeURIComponent(url.pathname==='/'?'/index.html':url.pathname));
   if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
  }
  const buffer=await readFile(file);
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(buffer);
 }catch{res.writeHead(404);res.end('Nicht gefunden');}
}).listen(port,'127.0.0.1',()=>console.log('Techtree-Vorschau: http://127.0.0.1:'+port));
