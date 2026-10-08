from pathlib import Path
import bpy,math,random
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'docs/Art/Mira3D'
bpy.ops.wm.open_mainfile(filepath=str(out/'Mira-Portrait-v08.blend'))
body=bpy.data.objects['Mira_AnatomicalBase'];top=1.43784
for o in list(bpy.context.scene.objects):
    if any(n in o.name for n in ['short03','waved lock','lock filament','tousled','fine swept','teeth_base']):bpy.data.objects.remove(o,do_unlink=True)
mat=bpy.data.materials.new('Mira layered hair cards');mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.68;p.inputs['Specular IOR Level'].default_value=.22
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/'Assets/SpaceMiner/AvatarLab/Model3D/MiraStaticHairLocks.png'))
mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);mat.node_tree.links.new(tex.outputs['Alpha'],p.inputs['Alpha']);mat.surface_render_method='DITHERED'
vertices=[];polys=[];uvs=[];random.seed(71)
def card(path,width):
    start=len(vertices)
    for j,co in enumerate(path):
        t=j/(len(path)-1);tangent=path[min(j+1,len(path)-1)]-path[max(0,j-1)]
        radial=co-Vector((0,-.018,top-.076));side=tangent.cross(radial).normalized()
        w=width*(.9+.15*math.sin(t*4))*max(.04,min(1,(1-t)*6))
        for sign,u in [(-1,0),(1,1)]:vertices.append(tuple(co+side*w*.5*sign));uvs.append((u,t))
        if j:polys.append((start+2*j-2,start+2*j-1,start+2*j+1,start+2*j))
# Layered radial crown/back/temple locks. Each ends independently.
for i in range(155):
    theta=i/155*math.pi*2;phi0=random.uniform(.06,.45);end=random.uniform(1.65,2.15)
    # The forehead remains open; short swept locks cover the front separately.
    if math.sin(theta)<-.45:end=random.uniform(.8,1.2)
    path=[];lift=random.uniform(.008,.021)
    for j in range(13):
        t=j/12;phi=phi0+(end-phi0)*t;a=theta+.30*math.sin(t*math.pi)+.08*math.sin(t*8+i)
        rx=.078+lift*math.sin(t*math.pi);ry=.099+lift*.6*math.sin(t*math.pi)
        co=Vector((rx*math.sin(phi)*math.cos(a),-.018+ry*math.sin(phi)*math.sin(a),top-.076+.101*math.cos(phi)))
        co.z+=.007*math.sin(t*8+i)*math.sin(math.pi*t)
        path.append(co)
    card(path,random.uniform(.014,.024))
# Front sweep wraps around the scalp instead of forming a floating flat sheet.
for i in range(70):
    spread=(i/69-.5);path=[];endphi=random.uniform(1.18,1.6)
    for j in range(15):
        t=j/14;phi=.18+(endphi-.18)*t
        theta=-1.05-1.22*t+spread*.8+.12*math.sin(t*7+i)
        loft=.006+.016*math.sin(math.pi*t)+.004*math.sin(t*9+i)
        path.append(Vector(((.079+loft)*math.sin(phi)*math.cos(theta),-.018+(.101+loft)*math.sin(phi)*math.sin(theta),top-.076+(.103+loft)*math.cos(phi))))
    card(path,random.uniform(.009,.015))
mesh=bpy.data.meshes.new('Layered tousled locks');mesh.from_pydata(vertices,[],polys);mesh.update()
uv=mesh.uv_layers.new(name='UVMap')
for poly in mesh.polygons:
    poly.use_smooth=True
    for li in poly.loop_indices:uv.data[li].uv=uvs[mesh.loops[li].vertex_index]
o=bpy.data.objects.new('Mira_StaticHairCards',mesh);bpy.context.collection.objects.link(o);o.data.materials.append(mat)
# Rounded enamel crowns, deliberately separate upper/lower rows.
enamel=bpy.data.materials.new('Mira natural ivory enamel');enamel.use_nodes=True
p=enamel.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.67,.64,.57,1);p.inputs['Roughness'].default_value=.34
parts=[]
for row in [0,1]:
    for i in range(10):
        x=(i-4.5)*.0049;y=-.110+.038*(abs(x)/.027)**2
        height=.006 if row==0 else .0045;z=top-.163 if row==0 else top-.173
        bpy.ops.mesh.primitive_cube_add(size=1,location=(x,y,z));o=bpy.context.object;o.name='Tooth crown'
        o.scale=(.0046,.005,height);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        bevel=o.modifiers.new('Rounded enamel edge','BEVEL');bevel.width=.00075;bevel.segments=3
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=bevel.name)
        o.data.materials.append(enamel);parts.append(o)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();bpy.context.object.name='Mira_teeth_new'
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/'Mira-Portrait-v10.blend'))
scene=bpy.context.scene;scene.cycles.samples=32
scene.render.filepath=str(out/'Mira-Portrait-v10-Side.png');bpy.ops.render.render(write_still=True)
print('MIRA V10 NEW HAIR AND TEETH READY',flush=True)
