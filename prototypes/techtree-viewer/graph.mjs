export const SIZE=96;
const GAP=238, PITCH=124, PAD=38;
export function layout(tree, gap=GAP){
 const nodes=new Map(tree.nodes.map(n=>[n.id,{...n,children:[]}]));
 const roots=[];
 const primary=new Map;
 for(const node of nodes.values()){
  const parent=node.parents.find(id=>nodes.has(id));
  if(parent){nodes.get(parent).children.push(node.id);primary.set(node.id,parent);}
  else roots.push(node.id);
 }
 let leaf=0;
 const bundled=new Set;
 function place(id,depth){
  const n=nodes.get(id);
  if(n.children.length>=3&&n.children.every(child=>nodes.get(child).children.length===0&&nodes.get(child).parents.length===1)){
   n.x=PAD+depth*gap;n.y=PAD+leaf*PITCH;n.w=n.h=SIZE;
   n.children.forEach((child,i)=>{const c=nodes.get(child);c.x=n.x+gap+i*(SIZE+40);c.y=n.y+PITCH;c.w=c.h=SIZE;});
   bundled.add(id);leaf+=2;return n.y;
  }
  const ys=n.children.map(child=>place(child,depth+1));
  n.x=PAD+depth*gap;n.y=ys.length?(ys[0]+ys.at(-1))/2:PAD+leaf++*PITCH;
  n.w=n.h=SIZE;
  return n.y;
 }
 roots.forEach(id=>place(id,0));
 const edges=[];
 for(const node of nodes.values())for(const parent of node.parents)if(nodes.has(parent))edges.push({source:parent,target:node.id,primary:primary.get(node.id)===parent});
 const outputs=new Map,inputs=new Map;
 for(const n of nodes.values()){
  outputs.set(n.id,edges.filter(e=>e.source===n.id).sort((a,b)=>nodes.get(a.target).y-nodes.get(b.target).y));
  inputs.set(n.id,edges.filter(e=>e.target===n.id).sort((a,b)=>nodes.get(a.source).y-nodes.get(b.source).y));
 }
 for(const e of edges){
  const a=nodes.get(e.source),b=nodes.get(e.target);
  if(bundled.has(a.id)){
   e.sharedBus=true;e.start=[a.x+SIZE,a.y+SIZE/2];e.end=[b.x+SIZE/2,b.y];
   e.points=[e.start,[e.end[0],e.start[1]],e.end];continue;
  }
  const out=outputs.get(a.id),incoming=inputs.get(b.id);
  const sy=out.length===1&&Math.abs(a.y-b.y)<.1?a.y+SIZE/2:a.y+16+(out.indexOf(e)+1)*(SIZE-32)/(out.length+1);
  const ty=b.y+16+(incoming.indexOf(e)+1)*(SIZE-32)/(incoming.length+1);
  e.start=[a.x+SIZE,sy];e.end=[b.x,ty];
  const i=out.indexOf(e),middle=(out.length-1)/2;
  const bus=a.x+SIZE+26+(middle-Math.abs(i-middle))*17;
  e.points=Math.abs(sy-ty)<.1?[e.start,e.end]:[e.start,[bus,sy],[bus,ty],e.end];
 }
 // Route the occasional additional input independently, around cards and primary wires.
 const reserved=edges.filter(e=>e.primary).flatMap(segments);
 for(const e of edges.filter(e=>!e.primary)){
  e.points=route(e,nodes,reserved);
  reserved.push(...segments(e));
 }
 const width=Math.max(...[...nodes.values()].map(n=>n.x+SIZE))+PAD;
 const height=Math.max(...[...nodes.values()].map(n=>n.y+SIZE))+PAD;
 return {nodes,edges,width,height,roots};
}
export function segments(edge){return edge.points.slice(1).map((p,i)=>[edge.points[i],p]);}
export function throughNode(a,b,n){
 if(a[1]===b[1])return a[1]>n.y+.01&&a[1]<n.y+n.h-.01&&Math.max(a[0],b[0])>n.x+.01&&Math.min(a[0],b[0])<n.x+n.w-.01;
 return a[0]>n.x+.01&&a[0]<n.x+n.w-.01&&Math.max(a[1],b[1])>n.y+.01&&Math.min(a[1],b[1])<n.y+n.h-.01;
}
export function intersects(a,b,c,d){
 if(a[1]===b[1]&&c[1]===d[1])return a[1]===c[1]&&Math.max(Math.min(a[0],b[0]),Math.min(c[0],d[0]))<Math.min(Math.max(a[0],b[0]),Math.max(c[0],d[0]))-.01;
 if(a[0]===b[0]&&c[0]===d[0])return a[0]===c[0]&&Math.max(Math.min(a[1],b[1]),Math.min(c[1],d[1]))<Math.min(Math.max(a[1],b[1]),Math.max(c[1],d[1]))-.01;
 if(a[0]===b[0])[a,b,c,d]=[c,d,a,b];
 return c[0]>Math.min(a[0],b[0])+.01&&c[0]<Math.max(a[0],b[0])-.01&&a[1]>Math.min(c[1],d[1])+.01&&a[1]<Math.max(c[1],d[1])-.01;
}
function route(e,nodes,reserved){
 const start=[e.start[0]+12,e.start[1]],end=[e.end[0]-12,e.end[1]];
 const xs=new Set([start[0],end[0],12]),ys=new Set([start[1],end[1],12]);
 for(const n of nodes.values()){xs.add(n.x-20);xs.add(n.x+n.w+20);ys.add(n.y-20);ys.add(n.y+n.h+20);}
 const X=[...xs].sort((a,b)=>a-b),Y=[...ys].sort((a,b)=>a-b);
 const startKey=X.indexOf(start[0])+','+Y.indexOf(start[1]),goalKey=X.indexOf(end[0])+','+Y.indexOf(end[1]);
 const distances=new Map([[startKey,0]]),previous=new Map;
 const open=[{key:startKey,cost:0}];
 while(open.length){
  open.sort((a,b)=>b.cost-a.cost);const {key,cost}=open.pop();
  if(cost!==distances.get(key))continue;
  if(key===goalKey)break;
  const [ix,iy]=key.split(',').map(Number),a=[X[ix],Y[iy]];
  for(const [nx,ny] of [[ix-1,iy],[ix+1,iy],[ix,iy-1],[ix,iy+1]]){
   if(nx<0||ny<0||nx>=X.length||ny>=Y.length)continue;
   const b=[X[nx],Y[ny]];
   if([...nodes.values()].some(n=>throughNode(a,b,{...n,x:n.x-5,y:n.y-5,w:n.w+10,h:n.h+10})))continue;
   if(reserved.some(([c,d])=>intersects(a,b,c,d)))continue;
   const next=nx+','+ny,nextCost=cost+Math.abs(a[0]-b[0])+Math.abs(a[1]-b[1]);
   if(nextCost<(distances.get(next)??Infinity)){distances.set(next,nextCost);previous.set(next,key);open.push({key:next,cost:nextCost});}
  }
 }
 if(!distances.has(goalKey))throw Error('Keine freie Connectorroute: '+e.source+' → '+e.target);
 const points=[];for(let k=goalKey;k;k=previous.get(k)){const [ix,iy]=k.split(',').map(Number);points.push([X[ix],Y[iy]]);}
 points.reverse();return simplify([e.start,...points,e.end]);
}
function simplify(points){
 return points.filter((p,i)=>i===0||i===points.length-1||!((points[i-1][0]===p[0]&&p[0]===points[i+1][0])||(points[i-1][1]===p[1]&&p[1]===points[i+1][1])));
}
export function camera(graph,width,height,zoom=.5,pan=0,panY=0){
 const scale=.75*(.5+Math.max(0,Math.min(1,zoom)));
 const minPan=Math.min(0,width-graph.width*scale);
 pan=Math.max(minPan,Math.min(0,pan));
 const minPanY=Math.min(0,height-graph.height*scale);panY=Math.max(minPanY,Math.min(0,panY));
 return {scale,pan,panY,x:Math.max(0,(width-graph.width*scale)/2)+pan,y:Math.max(0,(height-graph.height*scale)/2)+panY,minPan,minPanY};
}
