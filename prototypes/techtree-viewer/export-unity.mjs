import fs from 'node:fs';
import {layout} from './graph.mjs';
const root=new URL('./',import.meta.url),target=new URL('../../Assets/SpaceMiner/Resources/Research/',root);
const {trees}=JSON.parse(fs.readFileSync(new URL('catalog.json',target)));
const atlas=JSON.parse(fs.readFileSync(new URL('assets/realistic-v3.json',root)));
fs.writeFileSync(new URL('miniatures.json',target),JSON.stringify({entries:Object.entries(atlas.mapping).map(([id,a])=>({id,texture:a.file.split('/').at(-1).replace('.png',''),columns:a.columns,rows:a.rows,column:a.column,row:a.row}))},null,2));
for(let i=1;i<=3;i++)fs.copyFileSync(new URL(`assets/realistic-v3-${i}.png`,root),new URL(`realistic-v3-${i}.png`,target));
fs.writeFileSync(new URL('layouts.json',target),JSON.stringify({trees:trees.map(t=>{const g=layout(t);return {id:t.id,width:g.width*.75,height:g.height*.75,nodes:[...g.nodes.values()].map(n=>({id:n.id,x:n.x*.75,y:n.y*.75,width:n.w*.75,height:n.h*.75})),edges:g.edges.map(e=>({source:e.source,target:e.target,sharedBus:!!e.sharedBus,points:e.points.map(p=>({x:p[0]*.75,y:p[1]*.75}))}))};})},null,2));
console.log('Exported 159 miniature regions and 11 checked orthogonal layouts to Unity Resources.');
