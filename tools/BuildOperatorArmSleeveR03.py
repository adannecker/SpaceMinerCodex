"""Author a narrow fabric-only sleeve continuation without changing accepted R03.

The old cut sleeve overlaps a real sampled ring; the other end is a rounded,
closed proximal end. Two skin influences join the cuff to the offscreen sleeve.
No chest or torso is generated. The Unity importer mirrors this left-space
derivative, along with the original R03 skin, into a native right-hand asset.
"""
import bpy, math, json, struct, hashlib
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Design/Models/OperatorArm/Game-R03'
SOURCE=OUT/'OperatorArm-R03-Game.blend'
ASSETS=ROOT/'Assets/SpaceMiner/Resources/OperatorArmR03'
original_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
source=next(o for o in bpy.context.scene.objects if o.type=='MESH')
axis=Vector((-.06,.07,.22)).normalized()
cap=[f for f in source.data.polygons if f.center.dot(axis)<.035 and f.normal.dot(-axis)>.65]
area=sum(f.area for f in cap)
center=sum((f.center*f.area for f in cap),Vector())/area
outward=sum((f.normal*f.area for f in cap),Vector()).normalized()
inward=-outward
near=center+inward*.025
shoulder=Vector((-.02,.13,-.12))
near_axis=outward
u=(Vector((1,0,0))-near_axis*near_axis.x).normalized()
v=near_axis.cross(u).normalized()
angles=64
profile=[]
for i in range(angles):
    radial=u*math.cos(i*math.tau/angles)+v*math.sin(i*math.tau/angles)
    hit,point,normal,face=source.ray_cast(near+radial*.5,-radial)
    if not hit:raise RuntimeError('Missing R03 cuff cross-section '+str(i))
    radius=(point-near).dot(radial)
    if not .045<radius<.15:raise RuntimeError('Unexpected cuff profile radius '+str(radius))
    profile.append(radius-.002)

verts=[];uv=[];weights=[];faces=[]
def add_ring(c,axis,radii,bone_weight,t):
    ux=(u-axis*u.dot(axis)).normalized();vy=axis.cross(ux).normalized()
    for i in range(angles+1):
        a=(i%angles)*math.tau/angles
        p=c+(ux*math.cos(a)+vy*math.sin(a))*radii[i%angles]
        verts.append(tuple(p));weights.append(bone_weight)
        # Reuse a plain navy woven portion of the original R03 atlas.
        # Fine repeating panels keep the weave at a similar physical texel size.
        uv.append((.270+.055*(i%16)/16,.60+.080*(t% .30)/.30))
    if len(verts)>angles+1:
        first=len(verts)-2*(angles+1)
        for i in range(angles):faces.append((first+i,first+i+1,first+i+angles+2,first+i+angles+1))

p0=near;p1=near+outward*.075
direction=(shoulder-near).normalized();p2=shoulder-direction*.065;p3=shoulder
for j in range(13):
    t=j/12;s=1-t
    c=p0*s**3+p1*3*s*s*t+p2*3*s*t*t+p3*t**3
    tangent=((p1-p0)*3*s*s+(p2-p1)*6*s*t+(p3-p2)*3*t*t).normalized()
    radii=[r*(1+.13*math.sin(t*math.pi*.5))+.0025*math.sin(t*math.pi*14+i*.3)*math.sin(t*math.pi) for i,r in enumerate(profile)]
    # Keep the cloth rigid through and beyond the rolled source cuff. Earlier
    # blending pulled the tube away before it left the cuff, exposing a black
    # wedge. Articulate only after this short, opaque fabric neck.
    blend=max(0.,(t-.42)/.58)
    w=blend*blend*(3-2*blend)
    add_ring(c,tangent,radii,w,t)
far_axis=(p3-p2).normalized()
for j in range(1,8):
    a=j/7*math.pi*.5
    radii=[r*1.13*max(.012,math.cos(a)) for r in profile]
    add_ring(shoulder+far_axis*.10*math.sin(a),far_axis,radii,1,1+j/8)
last=len(verts)-(angles+1)
faces.append(tuple(last+i for i in range(angles)))
mesh=bpy.data.meshes.new('R03 navy shoulder continuation')
mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new('SleeveContinuation',mesh);bpy.context.scene.collection.objects.link(obj)
layer=mesh.uv_layers.new(name='R03 cloth atlas patch')
for face in mesh.polygons:
    face.use_smooth=True
    loops=list(face.loop_indices)
    if len(loops)==4:
        points=[mesh.vertices[mesh.loops[li].vertex_index].co for li in loops]
        du=min(.055,(points[1]-points[0]).length*1.8)
        dv=min(.080,(points[3]-points[0]).length*1.8)
        tex=[(.270,.60),(.270+du,.60),(.270+du,.60+dv),(.270,.60+dv)]
        for li,coordinate in zip(loops,tex):layer.data[li].uv=coordinate
    else:
        for i,li in enumerate(loops):
            a=i*math.tau/len(loops);layer.data[li].uv=(.2975+.0275*math.cos(a),.64+.04*math.sin(a))
obj.data.materials.append(source.data.materials[0])
mesh.calc_loop_triangles()
# Face normals should point radially outwards. Correct the generated strip if
# its orientation is opposite to the surface of the original sleeve.
face=mesh.polygons[angles*5]
if face.normal.dot(face.center-(near+(shoulder-near)*.4))<0:
    for poly in mesh.polygons:poly.flip()
mesh.update();mesh.calc_loop_triangles()

rows=[];indices=[];lookup={}
for triangle in mesh.loop_triangles:
    for li in triangle.loops:
        vi=mesh.loops[li].vertex_index;vertex=mesh.vertices[vi];tex=layer.data[li].uv
        key=(vi,round(tex.x,7),round(tex.y,7))
        if key not in lookup:
            lookup[key]=len(rows);w=weights[vi]
            rows.append((*vertex.co,*vertex.normal,*tex,1-w,w,0.,0.,0,1,0,0))
        indices.append(lookup[key])
with (OUT/'ShoulderSleeve.bin').open('wb') as f:
    f.write(b'AR03');f.write(struct.pack('<iii',len(rows),len(indices),2))
    for row in rows:f.write(struct.pack('<12f4i',*row))
    f.write(struct.pack('<'+'i'*len(indices),*indices))
report=dict(variant='Narrow fabric-only sleeve; accepted R03 surface unchanged; no torso',
    triangles=len(indices)//3,unityVertices=len(rows),skinBones=2,
    bones=[dict(name='UpperArm',parent=-1,head=[0,0,0],tail=[-.06,.07,.22]),
           dict(name='SleeveShoulder',parent=-1,head=list(shoulder),tail=list(shoulder+far_axis*.1))],
    binding='UpperArm existing R03 bone; SleeveShoulder sibling under Skeleton, identity bind rotation; endpoint stays behind lower-right camera edge at runtime',
    originalGameBlendSha256=original_hash,capCenter=list(center),capOutwardNormal=list(outward),
    sampledJoinCenter=list(near),radialProfileMin=min(profile),radialProfileMax=max(profile),
    continuationLength=(shoulder-near).length,closedShoulder=True,closedTorsoFlare=False,
    clothUvRect=[.270,.60,.325,.68],
    markers=[dict(name='SleeveProximalEnd',parent=1,position=list(shoulder+far_axis*.10)),
             dict(name='SleeveCuffJoin',parent=0,position=list(near)),
             dict(name='SleeveCuffJoinAxis',parent=0,position=list(near+outward*.01))])
if hashlib.sha256(SOURCE.read_bytes()).hexdigest()!=original_hash:raise RuntimeError('R03 game source modified')
report['originalGameBlendHashVerified']=True
(OUT/'ShoulderSleeve-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

# Inspect the actual join with the unmodified accepted surface beside it.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
scene.render.resolution_x=1100;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Shoulder inspection studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.055,.075,.10,1)
target=Vector((.01,.0,.19))
for pos,power in [((0,3,3),400),((2,-1,2),450),((-2,1,1),200)]:
    data=bpy.data.lights.new('Softbox','AREA');data.energy=power;data.size=3
    light=bpy.data.objects.new('Softbox',data);scene.collection.objects.link(light);light.location=pos
    light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Shoulder inspection');camera=bpy.data.objects.new('Shoulder inspection',data)
scene.collection.objects.link(camera);scene.camera=camera;camera.location=(-.85,2,.65)
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=1.20
scene.render.filepath=str(OUT/'ShoulderSleeve-Join.png');bpy.ops.render.render(write_still=True)

# A native authoring file with only the new mesh and external shared PBR maps.
for other in list(bpy.context.scene.objects):
    if other!=obj:bpy.data.objects.remove(other,do_unlink=True)
for image in list(bpy.data.images):
    if image.packed_file:image.unpack(method='REMOVE')
    if 'basecolor' in image.name.lower():image.filepath=str(ASSETS/'BaseColor.jpg')
    elif 'normal' in image.name.lower():image.filepath=str(ASSETS/'Normal.png')
    elif '_rm' in image.name.lower():image.filepath=str(ASSETS/'RoughMetal.jpg')
bpy.context.preferences.filepaths.save_version=0
native=OUT/'ShoulderSleeve.blend';temporary=OUT/'ShoulderSleeve.new.blend'
if temporary.exists():temporary.unlink()
bpy.ops.wm.save_as_mainfile(filepath=str(temporary));temporary.replace(native)
print('R03_SHOULDER_SLEEVE_COMPLETE '+json.dumps(report),flush=True)
