import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
test('Realistische Miniaturen: 159 getrennte Bildbereiche, vorhandene Atlanten, vollständiger Katalog',()=>{
 const root=new URL('../',import.meta.url),manifest=JSON.parse(fs.readFileSync(new URL('assets/realistic-v3.json',root)));
 const {trees}=JSON.parse(fs.readFileSync(new URL('../../Assets/SpaceMiner/Resources/Research/catalog.json',root)));
 const regions=new Set;
 for(const n of trees.flatMap(t=>t.nodes)){
  const a=manifest.mapping[n.id];assert.equal(a.name,n.name);assert.ok(fs.existsSync(new URL(a.file,root)));
  assert.ok(a.column>=0&&a.column<a.columns&&a.row>=0&&a.row<a.rows);
  regions.add([a.file,a.row,a.column].join(':'));
 }
 assert.equal(regions.size,159);
});
test('Gemessene Ausschnitte bleiben im Atlas und stimmen mit Unity überein',()=>{
 const root=new URL('../',import.meta.url);
 const {mapping}=JSON.parse(fs.readFileSync(new URL('assets/realistic-v3.json',root)));
 const {entries}=JSON.parse(fs.readFileSync(new URL('../../Assets/SpaceMiner/Resources/Research/miniatures.json',root)));
 const fields=['imageWidth','imageHeight','cropX','cropY','cropWidth','cropHeight'];
 // Observed gutters differ in each PNG; these boundaries exclude the next row's artwork.
 const atlasRowEdges={
  'assets/realistic-v3-1.png':[0,157,315,475,644,810,1024],
  'assets/realistic-v3-2.png':[0,156,311,471,627,799,1024],
  'assets/realistic-v3-3.png':[0,165,327,483,647,809,1024]
 };
 const regions=[];
 for(const [id,a]of Object.entries(mapping)){
  const png=fs.readFileSync(new URL(a.file,root));
  assert.equal(a.imageWidth,png.readUInt32BE(16));assert.equal(a.imageHeight,png.readUInt32BE(20));
  assert.ok(a.cropWidth>0&&a.cropHeight>0,id);
  assert.ok(a.cropX>=0&&a.cropX+a.cropWidth<=a.imageWidth,id);
  const rowEdges=atlasRowEdges[a.file];
  assert.ok(a.cropY>=rowEdges[a.row]&&a.cropY+a.cropHeight<=rowEdges[a.row+1],id);
  const exported=entries.find(e=>e.id===id);assert.ok(exported,id);
  for(const field of fields)assert.equal(exported[field],a[field],`${id}: ${field}`);
  for(const b of regions.filter(b=>b.file===a.file))assert.ok(a.cropX+a.cropWidth<=b.cropX||b.cropX+b.cropWidth<=a.cropX||a.cropY+a.cropHeight<=b.cropY||b.cropY+b.cropHeight<=a.cropY,`${id}: overlapping artwork regions`);
  regions.push(a);
 }
 assert.equal(entries.length,159);
 assert.ok(mapping['drohnen-02'].cropWidth>250,'Hologrammtisch vollständig statt in zwei Hälften');
 assert.equal(mapping['drohnen-03'].file,'assets/realistic-v3-3.png');
});
