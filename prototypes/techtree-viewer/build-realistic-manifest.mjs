import fs from 'node:fs';
const root=new URL('./',import.meta.url);
const prompts=JSON.parse(fs.readFileSync(new URL('assets/realistic-v3-prompts.json',root)));
const {trees}=JSON.parse(fs.readFileSync(new URL('../../Assets/SpaceMiner/Resources/Research/catalog.json',root)));
const names=new Map(trees.flatMap(t=>t.nodes.map(n=>[n.id,n.name])));
// The generated atlases were inspected: their actual grid overrides the requested grid.
const grids=[[10,6],[10,6],[9,6]];
const manifest={version:3,style:'Realistische technische Miniaturen, entsättigtes Blau mit kleinen gelben Akzenten',mapping:{}};
for(const [batchIndex,b]of prompts.batches.entries())for(const [slot,id]of b.nodes.entries()){
 const [columns,rows]=grids[batchIndex];manifest.mapping[id]={file:b.image,columns,rows,column:slot%columns,row:Math.floor(slot/columns),name:names.get(id)};
}
fs.writeFileSync(new URL('assets/realistic-v3.json',root),JSON.stringify(manifest,null,2)+'\n');
console.log(Object.keys(manifest.mapping).length+' unique atlas regions');
