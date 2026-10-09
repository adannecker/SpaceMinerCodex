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
