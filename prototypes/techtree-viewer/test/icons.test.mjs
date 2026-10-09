import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
test('159 individuelle SVGs, vollständiger Katalog, nur Blau und gelbe Akzente',()=>{
 const root=new URL('../',import.meta.url),manifest=JSON.parse(fs.readFileSync(new URL('assets/icons.json',root)));
 const {trees}=JSON.parse(fs.readFileSync(new URL('../../Assets/SpaceMiner/Resources/Research/catalog.json',root)));
 const files=new Set,hashes=new Set;
 for(const n of trees.flatMap(t=>t.nodes)){
  const entry=manifest.mapping[n.id];assert.ok(entry);assert.equal(entry.name,n.name);files.add(entry.file);
  const svg=fs.readFileSync(new URL(entry.file,root),'utf8');assert.match(svg,/viewBox="0 0 96 96"/);
  hashes.add(crypto.createHash('sha256').update(svg).digest('hex'));
  for(const color of svg.match(/#[0-9a-f]{6}/g))assert.ok(manifest.palette.includes(color));
 }
 assert.equal(files.size,159);assert.equal(hashes.size,159);
});
