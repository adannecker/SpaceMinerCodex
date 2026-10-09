import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {layout,segments,throughNode,intersects,camera} from '../graph.mjs';
const {trees}=JSON.parse(fs.readFileSync(new URL('../../../Assets/SpaceMiner/Resources/Research/catalog.json',import.meta.url)));
const atlas=JSON.parse(fs.readFileSync(new URL('../assets/atlas.json',import.meta.url)));
for(const tree of trees)test(tree.id+': orthogonale freie Linien, getrennte Ports, passende Gesamtansicht',()=>{
 const g=layout(tree),ports=new Set;
 for(const n of g.nodes.values())assert.ok(Number.isInteger(atlas.mapping[n.id])&&atlas.mapping[n.id]>=0&&atlas.mapping[n.id]<36);
 for(const e of g.edges){for(const [a,b]of segments(e)){assert.ok(a[0]===b[0]||a[1]===b[1]);for(const n of g.nodes.values())assert.ok(!throughNode(a,b,n),e.source+' crosses '+n.id);}for(const [id,p,side]of [[e.source,e.start,'out'],[e.target,e.end,'in']]){const key=id+side+p.join(',');assert.ok(!ports.has(key)||(e.sharedBus&&side==='out'),'duplicate port');ports.add(key);}}
 for(let i=0;i<g.edges.length;i++)for(let j=i+1;j<g.edges.length;j++)for(const [a,b]of segments(g.edges[i]))for(const [c,d]of segments(g.edges[j]))assert.ok((g.edges[i].sharedBus&&g.edges[j].sharedBus&&g.edges[i].source===g.edges[j].source)||!intersects(a,b,c,d),'wire crossing');
 for(const [w,h]of [[300,420],[900,600],[1800,900]]){const base=camera(g,w,h);assert.equal(96*base.scale,72);const max=camera(g,w,h,1,-1e6,-1e6);assert.equal(max.pan,max.minPan);assert.equal(max.panY,max.minPanY);assert.equal(96*max.scale,108);}
});
