"""Reproducible, non-destructive gameplay derivative of the accepted Tripo R03 arm.

Run with Blender 4.5 LTS --background --python tools/BuildOperatorArmR03.py.
The high-resolution source is read only. All authored output goes to Game-R03.
"""
import bpy, json, math, hashlib, struct, sys
from pathlib import Path
from mathutils import Vector, Matrix
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Design/Models/OperatorArm/Revision-R03/OperatorArm-R03.blend'
OUT = ROOT / 'Design/Models/OperatorArm/Game-R03'
ASSETS = ROOT / 'Assets/SpaceMiner/Resources/OperatorArmR03'
OUT.mkdir(exist_ok=True); ASSETS.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
body = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
body.name = 'OperatorArm_R03_Skin'
bpy.context.view_layer.objects.active = body
body.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
SOURCE_HASH = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
D = Vector((.9, 0, -.436)).normalized()
W = Vector((.436, 0, .9)).normalized()
N = Vector((0, -1, 0))
ROT = Matrix((W, N, D))
SOURCE_ORIGIN = Vector((.35, -.09, -.62))
SCALE = .73
def canonical(p):
    return (ROT @ Vector(p) - SOURCE_ORIGIN) * SCALE
for v in body.data.vertices:
    v.co = canonical(v.co)
body.data.update()

def render(name='Inspect.png', target=(0,0,.35), camera=(0,3,.35), ortho=.9):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
    for obj in list(scene.objects):
        if obj.type in ('LIGHT','CAMERA'):bpy.data.objects.remove(obj,do_unlink=True)
    scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Arm studio');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.07,.09,.12,1)
    target=Vector(target)
    for pos,power in [((0,3,3),450),((2,-1,2),500),((-2,1,1),220)]:
        data=bpy.data.lights.new('Softbox','AREA');data.energy=power;data.size=3
        obj=bpy.data.objects.new('Softbox',data);scene.collection.objects.link(obj);obj.location=pos
        obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Inspection');cam=bpy.data.objects.new('Inspection',data)
    scene.collection.objects.link(cam);scene.camera=cam;data.type='ORTHO';data.ortho_scale=ortho
    cam.location=camera;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)

if '--inspect' in sys.argv:
    inspect_out=ROOT/'work/OperatorArmR03Inspection';inspect_out.mkdir(exist_ok=True)
    coords=np.array([v.co for v in body.data.vertices])
    np.save(inspect_out/'source-coordinates.npy',coords)
    render('Inspect.png',(-.05,.1,.38),(-.05,3,.38),.85)
    (OUT/'Inspect.png').replace(inspect_out/'Inspect.png')
    sys.exit()

# Uniform scale establishes a 78-cm sleeve-opening-to-fingertip length. Landmarks
# were measured on the accepted textured surface, not on the rejected R04.
chains = {
    'Thumb': [(-.028,.108,.550),(-.001,.070,.584),(.019,.030,.617)],
    'Index': [(-.012,.132,.615),(-.020,.141,.665),(-.024,.142,.701),(-.027,.140,.730)],
    'Middle':[(-.046,.151,.612),(-.059,.166,.668),(-.070,.170,.708),(-.075,.167,.737)],
    'Ring':  [(-.080,.162,.600),(-.092,.179,.649),(-.103,.184,.688),(-.108,.182,.714)],
    'Little':[(-.109,.163,.580),(-.120,.182,.624),(-.132,.190,.655),(-.138,.189,.676)],
}
bones = [dict(name='UpperArm',parent=-1,head=[0,0,0],tail=[-.060,.070,.220]),
         dict(name='Forearm',parent=0,head=[-.060,.070,.220],tail=[-.063,.120,.502]),
         dict(name='Wrist',parent=1,head=[-.063,.120,.502],tail=[-.057,.148,.595])]
chain_indices={}
for digit,points in chains.items():
    chain_indices[digit]=[]
    for i in range(len(points)-1):
        parent=2 if i==0 else len(bones)-1
        chain_indices[digit].append(len(bones))
        bones.append(dict(name=f'{digit}{i+1:02}',parent=parent,head=list(points[i]),tail=list(points[i+1])))

original_triangles = len(body.data.polygons)
decimate=body.modifiers.new('Gameplay quadric simplification preserving UV interpolation','DECIMATE')
decimate.ratio=45000/original_triangles
bpy.ops.object.modifier_apply(modifier=decimate.name)
for face in body.data.polygons:face.use_smooth=True
body.data.update();body.data.calc_loop_triangles()
if len(body.data.loop_triangles)>55000:raise RuntimeError('Arm exceeds gameplay triangle budget')

def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def segment(p,a,b):
    d=b-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared))
    return (p-(a+d*t)).length,t

weights=[]
for v in body.data.vertices:
    p=v.co; candidates=[]
    for digit,points in chains.items():
        ps=list(map(Vector,points));found=min((segment(p,ps[i],ps[i+1])[0],i,segment(p,ps[i],ps[i+1])[1]) for i in range(len(ps)-1))
        # A branch is eligible past its knuckle; exclude the broad dorsal palm.
        if p.z>points[0][2]-.014 and found[0]<(.029 if digit=='Thumb' else .026):candidates.append((found[0],digit,found[1],found[2]))
    if candidates:
        distance,digit,i,t=min(candidates)
        ids=chain_indices[digit];first=chains[digit][0]
        activation=smooth(first[2]-.014,first[2]+.018,p.z)
        current=ids[i];prev=2 if i==0 else ids[i-1]
        # Smooth joint weighting blends only neighbouring bones within one digit.
        if t<.35:
            current_weight=.5+.5*smooth(0,.35,t)
            pair={prev:1-current_weight,current:current_weight}
        elif t>.70 and i+1<len(ids):
            nxt=.5*smooth(.70,1,t);pair={current:1-nxt,ids[i+1]:nxt}
        else:pair={current:1}
        pair={key:value*activation for key,value in pair.items()}
        pair[2]=pair.get(2,0)+(1-activation)
    elif p.z>.47:
        wrist=smooth(.475,.523,p.z);pair={1:1-wrist,2:wrist}
    else:
        forearm=smooth(.178,.261,p.z);pair={0:1-forearm,1:forearm}
    pair={key:value for key,value in pair.items() if value>1e-7}
    total=sum(pair.values());pair={key:value/total for key,value in pair.items()}
    weights.append(pair)
for i,bone in enumerate(bones):
    group=body.vertex_groups.new(name=bone['name'])
    for vi,pair in enumerate(weights):
        if i in pair:group.add([vi],pair[i],'REPLACE')

armature_data=bpy.data.armatures.new('OperatorArm_R03_Rig')
rig=bpy.data.objects.new('OperatorArm_R03_Rig',armature_data)
bpy.context.scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;body.select_set(False);rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for data in bones:
    bone=armature_data.edit_bones.new(data['name']);bone.head=data['head'];bone.tail=data['tail']
    if data['parent']>=0:bone.parent=armature_data.edit_bones[bones[data['parent']]['name']]
    bone.align_roll(Vector((0,1,0)))
bpy.ops.object.mode_set(mode='OBJECT')
modifier=body.modifiers.new('Articulated elbow wrist and five fingers','ARMATURE');modifier.object=rig
body.parent=rig

# Pad markers sit on the palmar surface 5-8mm behind each rounded fingertip.
# Sample before posing and retain the same skinned distal parent as that surface.
markers=[]
for digit,points in chains.items():
    tip=Vector(points[-1]);tip.z-=.006
    samples=[v.co for v in body.data.vertices if (v.co.x-tip.x)**2+(v.co.z-tip.z)**2<.0055**2]
    if not samples:raise RuntimeError('Missing finger pad samples: '+digit)
    pad=min(samples,key=lambda p:p.y).copy()
    marker_name='Tip'+digit
    markers.append(dict(name=marker_name,parent=chain_indices[digit][-1],position=list(pad)))
    marker=bpy.data.objects.new(marker_name,None);bpy.context.scene.collection.objects.link(marker);marker.location=pad
    marker.parent=rig;marker.parent_type='BONE';marker.parent_bone=bones[chain_indices[digit][-1]]['name']
    # Blender bone parenting uses a tail offset; explicitly preserve rest-world pad.
    bpy.context.view_layer.update();marker.matrix_world=Matrix.Translation(pad)

# Game textures retain the source UVs, with 2K maps rather than the 4K concept.
texture_report={}
for image in list(bpy.data.images):
    if image.type!='IMAGE':continue
    image.scale(2048,2048)
    is_color='basecolor' in image.name.lower();is_normal='normal' in image.name.lower()
    name='BaseColor.jpg' if is_color else 'Normal.png' if is_normal else 'RoughMetal.jpg'
    image.filepath_raw=str(ASSETS/name);image.file_format='JPEG' if name.endswith('jpg') else 'PNG'
    image.save();texture_report[name]=dict(width=image.size[0],height=image.size[1])
    if not is_color and not is_normal:
        pixels=np.array(image.pixels[:],dtype=np.float32).reshape((-1,4))
        packed=np.ones_like(pixels);packed[:,0:3]=pixels[:,2:3];packed[:,3]=1-pixels[:,1]
        metal=bpy.data.images.new('MetalSmoothness',2048,2048,alpha=True)
        metal.colorspace_settings.name='Non-Color';metal.pixels.foreach_set(packed.ravel())
        metal.filepath_raw=str(ASSETS/'MetalSmoothness.png');metal.file_format='PNG';metal.save()
        texture_report['MetalSmoothness.png']=dict(width=2048,height=2048)

# Native Unity mesh payload. Explicit coordinates and skin bind-pose data avoid
# depending on differing FBX automatic axis/bone conversion across editors.
vertices=[];indices=[];mapping={};uv=body.data.uv_layers.active.data
for triangle in body.data.loop_triangles:
    for li in triangle.loops:
        vi=body.data.loops[li].vertex_index;v=body.data.vertices[vi];tex=uv[li].uv
        key=(vi,round(tex.x,7),round(tex.y,7))
        if key not in mapping:
            mapping[key]=len(vertices)
            pair=sorted(weights[vi].items(),key=lambda item:-item[1])[:4]
            bi=[x[0] for x in pair]+[0]*(4-len(pair));bw=[x[1] for x in pair]+[0]*(4-len(pair))
            vertices.append((*v.co,*v.normal,*tex,*bw,*bi))
        indices.append(mapping[key])
with (OUT/'ArmMesh.bin').open('wb') as f:
    f.write(b'AR03');f.write(struct.pack('<iii',len(vertices),len(indices),len(bones)))
    for v in vertices:f.write(struct.pack('<12f4i',*v))
    f.write(struct.pack('<'+'i'*len(indices),*indices))

report=dict(source=str(SOURCE.relative_to(ROOT)),sourceSha256=SOURCE_HASH,
    variant='Accepted R03; no R04 surface additions',originalTriangles=original_triangles,
    triangles=len(indices)//3,meshVertices=len(body.data.vertices),unityVertices=len(vertices),
    skinBones=len(bones),maxInfluences=max(map(len,weights)),weightsNormalized=True,
    coordinates='Unity explicit metres: +Z fingers, +Y dorsal, +X thumb; root=upper sleeve opening',
    uniformSourceScale=SCALE,boundsMin=[min(v.co[i] for v in body.data.vertices) for i in range(3)],
    boundsMax=[max(v.co[i] for v in body.data.vertices) for i in range(3)],bones=bones,markers=markers,
    textures=texture_report,meshPayload='ArmMesh.bin',
    segmentLengths=dict(upperArm=(Vector(bones[0]['tail'])-Vector(bones[0]['head'])).length,
                       forearm=(Vector(bones[1]['tail'])-Vector(bones[1]['head'])).length))
bpy.context.preferences.filepaths.save_version=0
native_path=OUT/'OperatorArm-R03-Game.blend'
temporary_native=OUT/'OperatorArm-R03-Game.new.blend'
if temporary_native.exists():temporary_native.unlink()
bpy.ops.wm.save_as_mainfile(filepath=str(temporary_native))
temporary_native.replace(native_path)
for obj in bpy.context.selected_objects:obj.select_set(False)
body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'OperatorArm-R03-Game.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,
    axis_forward='-Z',axis_up='Y')
render('RestPose.png',(-.05,.1,.38),(-.55,2.5,.75),.88)
# Demonstrate real skinned articulation (all five digits and elbow/wrist).
# Runtime uses its own rest-pose-aware two-bone reach; this is authoring proof.
rig.pose.bones['Forearm'].rotation_mode='XYZ';rig.pose.bones['Forearm'].rotation_euler.x=math.radians(26)
rig.pose.bones['Wrist'].rotation_mode='XYZ';rig.pose.bones['Wrist'].rotation_euler.x=math.radians(-16)
for digit,ids in chain_indices.items():
    for index in ids:
        bone=rig.pose.bones[bones[index]['name']];bone.rotation_mode='XYZ'
        bone.rotation_euler.x=math.radians(22 if digit in ('Thumb','Little') else 4)
render('ArticulatedPose.png',(-.04,.0,.36),(-.55,2.5,.75),.86)
report['sourceHashVerified']=hashlib.sha256(SOURCE.read_bytes()).hexdigest()==SOURCE_HASH
if not report['sourceHashVerified']:raise RuntimeError('Original R03 source changed during derivative build')
report['maxWeightNormalizationError']=max(abs(sum(pair.values())-1) for pair in weights)
report['verticesPerBone']={bone['name']:sum(i in pair and pair[i]>.05 for pair in weights) for i,bone in enumerate(bones)}
pad_bones={}
for marker in markers:
    pad=Vector(marker['position'])
    nearest=min(range(len(body.data.vertices)),key=lambda vi:(body.data.vertices[vi].co-pad).length_squared)
    dominant=max(weights[nearest],key=weights[nearest].get)
    pad_bones[marker['name']]=bones[dominant]['name']
report['fingerPadDominantBones']=pad_bones
# Prove the independent exchange file retains skin and separate finger joints.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(OUT/'OperatorArm-R03-Game.fbx'))
round_skin=next(o for o in bpy.context.scene.objects if o.type=='MESH')
round_rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
round_skin.data.calc_loop_triangles()
report['fbxRoundTrip']=dict(triangles=len(round_skin.data.loop_triangles),bones=len(round_rig.data.bones),
    armatureModifier=any(m.type=='ARMATURE' for m in round_skin.modifiers),
    namedJointsRetained=all(bone['name'] in round_rig.data.bones for bone in bones))
if report['fbxRoundTrip']['triangles']!=report['triangles'] or not report['fbxRoundTrip']['namedJointsRetained']:
    raise RuntimeError('Rigged FBX did not survive round-trip')
(OUT/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('OPERATOR_ARM_R03_GAME_COMPLETE '+json.dumps({k:report[k] for k in ('triangles','unityVertices','skinBones','segmentLengths')}),flush=True)
