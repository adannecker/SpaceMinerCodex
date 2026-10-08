from pathlib import Path
import bpy,math,random
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'docs/Art/Mira3D'
bpy.ops.wm.open_mainfile(filepath=str(out/'Mira-Portrait-v08.blend'))
top=1.43784;random.seed(24)
for o in list(bpy.context.scene.objects):
    if o.type=='CURVE' and any(n in o.name for n in ['waved','filament','tousled','fine swept']):bpy.data.objects.remove(o,do_unlink=True)
# A quiet underlayer stays below the shaped locks; it never supplies the fringe silhouette.
hair=next(o for o in bpy.context.scene.objects if 'short03' in o.name)
for v in hair.data.vertices:
    center=Vector((0,-.015,top-.070));v.co=center+(v.co-center)*1.01
mat=bpy.data.materials.new('Mira sculpted chestnut locks');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.032,.021,.015,1);p.inputs['Roughness'].default_value=.58;p.inputs['Specular IOR Level'].default_value=.22
nodes=mat.node_tree.nodes;links=mat.node_tree.links
tex=nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=1
uvnode=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(210,4,1)
links.new(uvnode.outputs['UV'],mapping.inputs[0]);links.new(mapping.outputs[0],tex.inputs['Vector'])
bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.00018
links.new(tex.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],p.inputs['Normal'])
strandmat=bpy.data.materials.new('Mira subtle hair filaments');strandmat.use_nodes=True
p=strandmat.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.028,.019,.013,1);p.inputs['Roughness'].default_value=.66

def spline(points,count=48):
    pts=[Vector((x,y,top+z)) for x,y,z in points];ext=[pts[0]]+pts+[pts[-1]];outp=[]
    for j in range(count):
        v=j/(count-1)*(len(pts)-1);i=min(int(v),len(pts)-2);t=v-i;a,b,c,d=ext[i:i+4]
        outp.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
    return outp

def lock(name,controls,width,depth):
    path=spline(controls);verts=[];faces=[];uvs=[];frames=[]
    for j,co in enumerate(path):
        t=j/(len(path)-1);tangent=(path[min(j+1,len(path)-1)]-path[max(0,j-1)]).normalized()
        radial=(co-Vector((0,-.015,top-.07))).normalized();side=tangent.cross(radial).normalized();up=side.cross(tangent).normalized()
        taper=max(.025,min(1,t*8,(1-t)*5))**.6
        frames.append((co,side,up,taper))
        for k in range(12):
            angle=2*math.pi*k/12;groove=1+.025*math.cos(angle*12)
            verts.append(tuple(co+side*(math.cos(angle)*width*.5*taper*groove)+up*(math.sin(angle)*depth*.5*taper)))
            uvs.append((k/12,t))
            if j:faces.append(((j-1)*12+k,(j-1)*12+(k+1)%12,j*12+(k+1)%12,j*12+k))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();uv=me.uv_layers.new()
    for f in me.polygons:
        f.use_smooth=True
        for li in f.loop_indices:uv.data[li].uv=uvs[me.loops[li].vertex_index]
    o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.data.materials.append(mat);o.hide_render=True
    for k in range(85):
        angle=(k/84)*math.pi*2;points=[]
        for co,side,up,taper in frames:
            points.append(co+side*(math.cos(angle)*width*.5*taper)+up*(math.sin(angle)*depth*.5*taper+.00010))
        cu=bpy.data.curves.new(name+' filament','CURVE');cu.dimensions='3D';cu.bevel_depth=.000055;cu.bevel_resolution=0
        s=cu.splines.new('POLY');s.points.add(len(points)-1)
        for b,co in zip(s.points,points):b.co=(*co,1)
        oo=bpy.data.objects.new(name+' filament %d'%k,cu);bpy.context.collection.objects.link(oo);oo.data.materials.append(strandmat)

# Hand-directed main sweep: lifted roots, broad S, loose outward terminal curl.
for i in range(22):
    a=(i/21-.5);off=a*.045;rise=.006*math.sin(i*1.7)
    controls=[(.041+off,-.005,.005),(.019+off,-.036,.014+rise),(-.018+off,-.075,.005+rise),(-.060+off,-.105,-.003+rise),(-.087+off*.5,-.102,-.043),(-.096+off*.2,-.086,-.056+.012*math.sin(i))]
    lock('Mira sculpted sweep %02d'%i,controls,.014+random.random()*.004,.0045)
# Small upward crown curls prevent a smooth helmet outline.
for i in range(8):
    x=-.048+i*.012
    controls=[(x,.025,-.005),(x+.006,.014,.013),(x+.019,-.011,.022+random.uniform(-.004,.005)),(x+.018,-.032,.012),(x+.006,-.046,.009)]
    lock('Mira crown curl %02d'%i,controls,.013,.004)
# Loose side waves around/behind the ears. Front stems keep the forehead open.
for sign in [-1,1]:
    for i in range(14):
        back=i/13*.066;y=-.039+back
        controls=[(sign*.042,y,.001),(sign*.072,y-.008,-.021),(sign*.083,y-.003,-.065),(sign*.078,y+.012,-.110),(sign*.074,y+.016,-.143),(sign*.087,y+.026,-.133)]
        lock('Mira side wave %d %02d'%(sign,i),controls,.014,.004)
# Back waves fill the silhouette at the nape without a solid outer cap.
for i in range(10):
    x=(i/9-.5)*.105
    lock('Mira nape wave %02d'%i,[(x,.04,.002),(x+.008,.071,-.034),(x-.004,.067,-.082),(x+.008,.052,-.145),(x+.019,.054,-.138)],.019,.005)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'Mira-Portrait-v11.blend'))
scene=bpy.context.scene;scene.cycles.samples=48;scene.render.filepath=str(out/'Mira-Portrait-v11-Side.png');bpy.ops.render.render(write_still=True)
print('MIRA V11 STATIC SCULPTED PORTRAIT READY',flush=True)
