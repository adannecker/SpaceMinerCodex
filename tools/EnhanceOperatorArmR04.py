"""First additive space-workwear revision. Preserves the complete source arm and UVs."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Design/Models/OperatorArm/Revision-R04';OUT.mkdir(exist_ok=True)
SOURCE=ROOT/'Design/Models/OperatorArm/Revision-R03/OperatorArm-R03.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));body=next(o for o in bpy.context.scene.objects if o.type=='MESH');body.name='Continuous_Suit_Original'
bpy.context.view_layer.objects.active=body;body.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
def material(name,color,metal=.0,rough=.45,emit=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if emit:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emit
 return m
titanium=material('Satin titanium',(.12,.18,.23),.8,.36);graphite=material('Graphite seals',(.018,.028,.04),.2,.58);cyan=material('Cyan status light',(.015,.65,.85),.3,.25,1);amber=material('Amber safety marking',(.95,.38,.045),.2,.38);screen=material('HUD dark glass',(.008,.025,.04),.35,.18)
d=Vector((.90,0,-.436)).normalized();w=Vector((.436,0,.90)).normalized();N=Vector((0,-1,0))
def surface(x,z,lift):
 hit,pos,normal,face=body.ray_cast(Vector((x,-2,z)),Vector((0,1,0)))
 if not hit:raise RuntimeError(f'No arm surface at {x},{z}')
 return pos+N*lift
def plate(name,x,z,length,width,mat,lift=.004,thick=.004):
 # Cut corners keep an industrial silhouette; each perimeter point follows the source surface.
 points=[(-.5,-.35),(-.35,-.5),(.35,-.5),(.5,-.35),(.5,.35),(.35,.5),(-.35,.5),(-.5,.35)]
 top=[]
 for a,b in points:
  v=Vector((x,0,z))+d*a*length+w*b*width;top.append(surface(v.x,v.z,lift))
 vertices=top+[v-N*thick for v in top];faces=[tuple(range(8)),tuple(reversed(range(8,16)))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update();obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj);obj.data.materials.append(mat)
 bevel=obj.modifiers.new('Machined edge','BEVEL');bevel.width=.0015;bevel.segments=2
 return obj
plate('Forearm protective panel',-.065,.328,.145,.069,graphite,.004,.004)
plate('Titanium forearm plate',-.066,.33,.130,.055,titanium,.008,.004)
plate('Forearm lower rail',.015,.235,.105,.014,titanium,.005,.003)
plate('Forearm cyan strip',-.065,.33,.068,.004,cyan,.014,.001)
for x,z in [(-.105,.35),(-.015,.302)]:plate('Amber panel locator',x,z,.012,.006,amber,.014,.001)
plate('Hand dorsal graphite plate',.29,.155,.072,.073,graphite,.004,.004)
plate('Hand titanium shield',.291,.156,.050,.047,titanium,.008,.004)
plate('Hand cyan locator',.29,.155,.023,.004,cyan,.014,.001)
# Small independent display accent strips follow the existing cuff rather than replacing sleeve geometry.
# Keep the source display completely readable; no polygon overlay across its glass.
for x,z in [(-.115,.36),(-.02,.315),(.275,.17),(.31,.14)]:
 p=surface(x,z,.014);bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.003,depth=.002,location=p,rotation=(math.pi/2,0,0));bolt=bpy.context.object;bolt.name='Recessed service fastener';bolt.data.materials.append(graphite)
plate('Upper sleeve service badge',-.34,.49,.065,.035,graphite,.005,.003)
plate('Upper sleeve amber identifier',-.34,.49,.040,.006,amber,.010,.002)
original_vertices=len(body.data.vertices);original_faces=len(body.data.polygons)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'OperatorArm-R04.blend'))
bpy.ops.export_scene.gltf(filepath=str(OUT/'OperatorArm-R04-Preview.glb'),export_format='GLB',export_yup=True,export_apply=True)
report={'source':str(SOURCE.relative_to(ROOT)),'sourceGLBSha256':hashlib.sha256((SOURCE.with_suffix('.glb')).read_bytes()).hexdigest(),'originalVertices':original_vertices,'originalTriangles':original_faces,'addedObjects':len([o for o in bpy.context.scene.objects if o.type=='MESH'])-1,'rigged':False,'method':'Additive surface-conforming plates; original vertices, topology, UVs and material retained.'};(OUT/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.resolution_x=1100;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.15,.19,1)
target=Vector((0,0,.34))
for pos,power in [((0,-3,3),450),((2,1,2),500),((-2,-1,1),220)]:
 ld=bpy.data.lights.new('Softbox','AREA');ld.energy=power;ld.size=3;o=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
camd=bpy.data.cameras.new('Inspection');cam=bpy.data.objects.new('Inspection',camd);scene.collection.objects.link(cam);scene.camera=cam;camd.type='ORTHO';camd.ortho_scale=1.18;cam.location=(0,-3,.34);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'front.png');bpy.ops.render.render(write_still=True)
print('OPERATOR ARM R04 COMPLETE',flush=True)
