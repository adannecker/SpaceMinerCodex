import fs from 'node:fs';
const root=new URL('./',import.meta.url);
const prompts=JSON.parse(fs.readFileSync(new URL('assets/realistic-v3-prompts.json',root)));
const {trees}=JSON.parse(fs.readFileSync(new URL('../../Assets/SpaceMiner/Resources/Research/catalog.json',root)));
const names=new Map(trees.flatMap(t=>t.nodes.map(n=>[n.id,n.name])));
// The generated atlases were inspected: their actual grid overrides the requested grid.
const grids=[[10,6],[10,6],[9,6]];
const {atlases}=JSON.parse(fs.readFileSync(new URL('assets/realistic-v3-crops.json',root)));
const manifest={version:3,style:'Realistische technische Miniaturen, entsättigtes Blau mit kleinen gelben Akzenten',mapping:{}};
for(const [batchIndex,b]of prompts.batches.entries())for(const [slot,id]of b.nodes.entries()){
 const [columns,rows]=grids[batchIndex],measured=atlases[batchIndex];
 if(measured.file!==b.image)throw new Error('Crop atlas order mismatch');
 manifest.mapping[id]={file:b.image,columns,rows,column:slot%columns,row:Math.floor(slot/columns),name:names.get(id),imageWidth:measured.imageWidth,imageHeight:measured.imageHeight,...measured.crops[slot]};
}
// The table in atlas 2 spans columns 6–7. Its second half is not a separate icon:
// use an otherwise unused complete cargo platform for Frachtraumumbau instead.
{
 const a=atlases[2],slot=51;
 manifest.mapping['drohnen-03']={file:a.file,columns:9,rows:6,column:slot%9,row:Math.floor(slot/9),name:names.get('drohnen-03'),imageWidth:a.imageWidth,imageHeight:a.imageHeight,...a.crops[slot]};
}
fs.writeFileSync(new URL('assets/realistic-v3.json',root),JSON.stringify(manifest,null,2)+'\n');
console.log(Object.keys(manifest.mapping).length+' unique atlas regions');
