"""Non-destructive local cuff relocation on the exported Tripo retry mesh."""
import bpy,bmesh,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Design/Models/OperatorArm/Revision-R01';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Design/Models/OperatorArm/Original/Tripo-H31-Retry.blend'))
body=next(o for o in bpy.context.scene.objects if o.type=='MESH');body.name='OperatorArm_Suit'
bpy.context.view_layer.objects.active=body;body.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
mod=body.modifiers.new('Preview mesh reduction','DECIMATE');mod.ratio=.12;bpy.ops.object.modifier_apply(modifier=mod.name)
bm=bmesh.new();bm.from_mesh(body.data)
# The raised display sits above the diagonal sleeve surface in the source's X/Z plane.
def cuff(p):return .075<p.x<.475 and p.z>(.38*p.x+.238)
for v in bm.verts:v.select=cuff(v.co)
for f in bm.faces:f.select=all(v.select for v in f.verts)
bm.to_mesh(body.data);bm.free()
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_mode(type='FACE');bpy.ops.mesh.separate(type='SELECTED');bpy.ops.object.mode_set(mode='OBJECT')
parts=[o for o in bpy.context.scene.objects if o.type=='MESH' and o!=body];assert len(parts)==1
hud=parts[0];hud.name='OperatorArm_HUD_Forearm'
coords=[v.co.copy() for v in hud.data.vertices];center=sum(coords,Vector())/len(coords)
rotation=Matrix.Rotation(math.radians(84),4,'Y');target=Vector((-.17,0,.48))
for v in hud.data.vertices:v.co=rotation.to_3x3()@(v.co-center)+target
# Close the extraction and display bases. Caps retain the source material; a small backing hides cut edges.
for obj in (body,hud):
 bm=bmesh.new();bm.from_mesh(obj.data);edges=[e for e in bm.edges if e.is_boundary];bmesh.ops.holes_fill(bm,edges=edges,sides=0);bm.to_mesh(obj.data);bm.free();obj.data.update()
# Independent low-profile graphite backplate beneath the relocated module.
mat=bpy.data.materials.new('Cuff graphite backing');mat.diffuse_color=(.035,.055,.075,1);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.035,.055,.075,1);bs.inputs['Metallic'].default_value=.6;bs.inputs['Roughness'].default_value=.46
bpy.ops.mesh.primitive_cube_add(size=1,location=target);base=bpy.context.object;base.name='HUD_Cuff_Backing';base.dimensions=(.27,.17,.025);base.rotation_euler[1]=math.radians(-28);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);base.data.materials.append(mat);bevel=base.modifiers.new('Manufactured edge','BEVEL');bevel.width=.008;bevel.segments=3;bpy.context.view_layer.objects.active=base;bpy.ops.object.modifier_apply(modifier=bevel.name)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'OperatorArm-R01.blend'))
bpy.ops.export_scene.gltf(filepath=str(OUT/'OperatorArm-R01.glb'),export_format='GLB',export_yup=True)
report={'source':'Tripo-H31-Retry.glb','sourceSha256':hashlib.sha256((ROOT/'Design/Models/OperatorArm/Original/Tripo-H31-Retry.glb').read_bytes()).hexdigest(),'hudVertices':len(hud.data.vertices),'sourceHudCenter':list(center),'newHudCenter':list(target),'triangles':sum(len(o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),'rigged':False,'notes':['Reduced preview mesh; original preserved.','HUD separated geometrically, caps require visual inspection.','Retry mesh used because initial version was not locally exported.']}
(OUT/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.resolution_x=1100;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.15,.19,1)
for pos,power in [((0,-3,3),450),((2,1,2),500),((-2,-1,1),220)]:
 d=bpy.data.lights.new('Softbox','AREA');d.energy=power;d.size=3;o=bpy.data.objects.new('Softbox',d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Inspection');cam=bpy.data.objects.new('Inspection',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=1.35
for name,pos in [('front',(0,-3,.5)),('hud',(-1,-2,1.7))]:
 cam.location=pos;cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('OPERATOR ARM R01 EXPORTED',flush=True)
