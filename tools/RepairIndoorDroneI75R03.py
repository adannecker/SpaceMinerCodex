"""I75 R03: closed chassis, restrained PBR, crisp physical labels and tool mount.

Run with Blender 4.5.14: --background --threads 6 --python-exit-code 1
 --python tools/RepairIndoorDroneI75R03.py
Sources and R02 are read-only. No Unity assignments or campaign data changed.
"""
from pathlib import Path
import bpy, bmesh, hashlib, json, math, time, argparse, sys
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Design/Models/IndoorDroneI75/Surface-R03-2026-10-26'
ART = ROOT/'docs/Art/Drohnen/I75-Textur-R03-2026-10-26'
SOURCE = ROOT/'Design/Models/IndoorDroneI75/Surface-R02-2026-10-26/I75_V02_Surface_R02.blend'
for p in (OUT, ART): p.mkdir(parents=True, exist_ok=True)
args = argparse.ArgumentParser()
args.add_argument('--skip-render', action='store_true')
options = args.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
start = time.time()
def log(s): print('I75 R03:', s, flush=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
source_hash = sha(SOURCE)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
body = bpy.data.objects.get('I75_V02_Surface_R02')
assert body is not None
body.name = 'I75_R03_OuterPanels'
for o in list(bpy.data.objects):
    if o != body: bpy.data.objects.remove(o, do_unlink=True)
scene = bpy.context.scene
parts = [body]

def material(name, color, metal=0, rough=.5, emission=None):
    m = bpy.data.materials.new(name); m.use_nodes = True
    m.diffuse_color = (*color, 1)
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    if emission:
        p.inputs['Emission Color'].default_value = (*emission, 1)
        p.inputs['Emission Strength'].default_value = .65
    return m

navy = material('R03 / matte blue graphite paint', (.038, .060, .079), .24, .52)
frame = material('R03 / warm light alloy paint', (.32, .345, .323), .22, .5)
steel = material('R03 / machined titanium', (.26, .30, .32), .86, .37)
dark = material('R03 / recessed graphite', (.013, .020, .025), .45, .56)
rubber = material('R03 / sealed joints', (.009, .013, .016), 0, .76)
white = material('R03 / durable ivory lettering', (.69, .76, .77), .08, .5)
amber = material('R03 / safety amber', (.77, .40, .055), .12, .5)
glass = material('R03 / optical glass', (.009, .022, .030), .34, .14)
cyan = material('R03 / small status indicator', (.025, .32, .39), .08, .25, (.02, .34, .40))
materials = [navy, frame, steel, dark, rubber, glass]
body.data.materials.clear()
for m in materials: body.data.materials.append(m)
for p in body.data.polygons:
    x,y,z = p.center
    # Polygon.center requires update; calculate explicitly from source vertices.
    c = sum((body.data.vertices[i].co for i in p.vertices), Vector()) / len(p.vertices)
    x,y,z = c
    idx = 0
    if (abs(x)>.198 and (z<.226 or abs(y)>.245)) or (abs(y)>.317 and (abs(x)>.124 or z<.199)): idx=1
    if z>.365 and abs(x)>.162: idx=1
    if z<.133: idx=3
    if .024<z<.064: idx=2
    if z<.023: idx=4
    if ((z>.375 and abs(x)>.10 and abs(y)>.10) or (z>.365 and abs(y)>.26)
        or (abs(x)>.181 and abs(y)>.263 and .14<z<.364)
        or (abs(x)>.215 and abs(y)<.291 and .127<z<.163)): idx=2
    if y<-.315 and abs(x)<.105 and .321<z<.369: idx=3
    p.material_index=idx

def topo(obj):
    bm=bmesh.new(); bm.from_mesh(obj.data)
    r={'vertices':len(bm.verts), 'faces':len(bm.faces),
       'boundaryEdges':sum(e.is_boundary for e in bm.edges),
       'nonManifoldEdges':sum(not e.is_manifold for e in bm.edges)}
    bm.free(); return r

before=topo(body)
# Close small, planar open patch loops, retaining the characteristic outer skin.
bm=bmesh.new(); bm.from_mesh(body.data)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000015)
edges=[e for e in bm.edges if e.is_boundary]
remaining=set(v for e in edges for v in e.verts); filled=0
while remaining:
    v=remaining.pop(); connected={v}; stack=[v]
    while stack:
        vv=stack.pop()
        for e in vv.link_edges:
            if not e.is_boundary: continue
            n=e.other_vert(vv)
            if n in remaining:
                remaining.remove(n); connected.add(n); stack.append(n)
    bounds=[max(v.co[i] for v in connected)-min(v.co[i] for v in connected) for i in range(3)]
    selected=list({e for v in connected for e in v.link_edges if e.is_boundary})
    if selected and min(bounds)<.0002 and max(bounds)<.12 and all(sum(e.is_boundary for e in v.link_edges)==2 for v in connected):
        filled += len(bmesh.ops.holes_fill(bm, edges=selected, sides=0).get('faces',[]))
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(body.data); bm.free(); body.data.update()
bpy.context.view_layer.objects.active=body
body.select_set(True)
solid=body.modifiers.new('Real panel thickness and sealed exposed rims', 'SOLIDIFY')
solid.thickness=.0012; solid.offset=-1; solid.use_rim=True
bpy.ops.object.modifier_apply(modifier=solid.name)
log('Original boundary edges %d; small planar caps %d; skin after thickness %s' % (before['boundaryEdges'],filled,topo(body)))

def box(name, center, size, mat, bevel=.0015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    o=bpy.context.object; o.name=name; o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        b=o.modifiers.new('Machined chamfer','BEVEL'); b.width=bevel; b.segments=2
        bpy.ops.object.modifier_apply(modifier=b.name)
    o.data.materials.append(mat); parts.append(o)
    return o

def cylinder(name, center, radius, depth, mat, direction=(0,-1,0), vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center)
    o=bpy.context.object; o.name=name
    o.rotation_euler=Vector(direction).to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    b=o.modifiers.new('Safe edge','BEVEL');b.width=min(.0007,depth*.15);b.segments=2
    bpy.ops.object.modifier_apply(modifier=b.name)
    for p in o.data.polygons: p.use_smooth=len(p.vertices)==4
    o.data.materials.append(mat);parts.append(o);return o

def lettering(name, text, origin, size, xaxis, yaxis, mat=white, maxwidth=None):
    curve=bpy.data.curves.new(name,'FONT');curve.body=text;curve.size=size
    curve.extrude=.000045;curve.resolution_u=8;curve.space_character=1.08
    font=Path('C:/Windows/Fonts/arialbd.ttf')
    if font.exists():curve.font=bpy.data.fonts.load(str(font),check_existing=True)
    o=bpy.data.objects.new(name,curve);scene.collection.objects.link(o)
    xaxis,yaxis=Vector(xaxis),Vector(yaxis);normal=xaxis.cross(yaxis)
    o.rotation_euler=Matrix((xaxis,yaxis,normal)).transposed().to_euler();o.location=origin
    bpy.context.view_layer.update()
    if maxwidth and o.dimensions.length>0:
        width=max(v[0] for v in o.bound_box)-min(v[0] for v in o.bound_box)
        if width>maxwidth:o.scale.x=maxwidth/width
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH');o=bpy.context.object
    o.data.materials.append(mat);parts.append(o);return o

# A genuinely closed body under the decorative panel seams. No two-sided-material trick.
chassis=box('R03 sealed structural chassis', (0,0,.249), (.392,.684,.258), navy, .017)
belly=box('R03 closed removable belly pan', (0,0,.116), (.402,.688,.014), steel, .009)
box('R03 belly service hatch', (0,.025,.1065), (.228,.36,.006), navy,.004)
for sx in (-1,1):
    box('R03 continuous side backing '+str(sx), (sx*.202,0,.238), (.007,.622,.215),navy,.003)
    for yy in (-.266,.268):
        cylinder('Belly captive screw',(sx*.171,yy,.1075),.0035,.002,steel,(0,0,-1),6)
box('R03 closed rear lower apron',(0,.349,.145),(.366,.015,.054),steel,.004)
rear=box('R03 sealed rear equipment cover',(0,.369,.254),(.244,.012,.174),navy,.007)
for zz in (.202,.217,.232):
    box('Rear blind cooling channel',(0,.376,zz),(.148,.002,.004),dark,.0007)
for sx in (-1,1):
    for zz in (.19,.317):cylinder('Rear cover fastener',(sx*.106,.377,zz),.0038,.002,steel,(0,1,0),6)

# Tool interface on the centre of the actual front, using a blind magnetic mating face.
box('R03 front coupler mounting plate',(0,-.369,.254),(.144,.018,.151),steel,.007)
cylinder('Tool mount octagonal frame',(0,-.383,.254),.063,.012,steel,vertices=8)
cylinder('Tool mount isolator',(0,-.390,.254),.054,.004,rubber)
cylinder('Blind magnetic mating face',(0,-.394,.254),.045,.005,dark)
cylinder('Central locating register',(0,-.398,.254),.022,.005,steel,vertices=48)
for i in range(6):
    a=2*math.pi*i/6;xx=.037*math.cos(a);zz=.254+.037*math.sin(a)
    tile=box('Magnetic pole %02d'%i,(xx,-.3975,zz),(.015,.003,.009),steel,.0008)
    tile.rotation_euler.y=-a
for i in range(3):
    a=2*math.pi*i/3+math.pi/6
    cylinder('Captive tool flange screw %02d'%i,(.054*math.cos(a),-.391,.254+.054*math.sin(a)),.003,.003,dark,vertices=6)
box('Tool mount keyed alignment stop',(0,-.402,.278),(.009,.006,.004),amber,.0006)
lettering('Tool mount identification','MAG / TOOL',(-.039,-.3795,.187),.009,(1,0,0),(0,0,1),maxwidth=.079)

# Distinct camera optics replace the previous oversized luminous texture blobs.
box('Front optical assembly',(0,-.348,.349),(.180,.019,.045),dark,.004)
for xx in (-.044,.044):
    cylinder('Camera protective collar',(xx,-.362,.349),.014,.009,steel)
    cylinder('Recessed camera glass',(xx,-.367,.349),.0108,.002,glass)
    cylinder('Small lens centre',(xx,-.3685,.349),.0058,.001,dark)
for xx in (-.077,.077):
    box('Small cyan status lamp',(xx,-.359,.350),(.004,.002,.008),cyan,.0007)

# Separate flat service plaques: no lettering across folds, no mirrored underside text.
bpy.context.view_layer.update()
label_notes=[]
for sx in (-1,1):
    hits=[]
    for yy in (-.145,-.03,.105):
        for zz in (.227,.255,.285):
            hit,pt,n,idx=body.ray_cast((sx*.8,yy,zz),(-sx,0,0))
            if hit:hits.append(abs(pt.x))
    surface=max(hits) if hits else .218
    side_x=sx*(surface+.002)
    plate=box('Side service plate '+str(sx),(side_x,-.02,.255),(.003,.292,.071),dark,.0012)
    # Text u runs left-to-right when viewed from each respective side.
    xaxis=(0,sx,0);yaxis=(0,0,1);u0=-.147 if sx>0 else .107
    face_x=side_x+sx*.0017
    lettering('Side I75 '+str(sx),'I-75',(face_x,u0,.266),.028,xaxis,yaxis,maxwidth=.087)
    lettering('Side systems '+str(sx),'STATION SYSTEMS',(face_x,u0+sx*.101,.278),.008,xaxis,yaxis,maxwidth=.166)
    lettering('Side utility '+str(sx),'MODULAR UTILITY / 02',(face_x,u0+sx*.101,.263),.008,xaxis,yaxis,maxwidth=.166)
    box('Side plaque amber rule '+str(sx),(face_x,-.02,.252),(.0003,.263,.0013),amber,.0001)
    lettering('Side service '+str(sx),'SERVICE ACCESS',(face_x,u0,.232),.009,xaxis,yaxis,maxwidth=.15)
    label_notes.append({'side':sx,'surfaceX':surface,'plateX':side_x,'textGeometry':True})

# Small roof label located above the measured roof, away from handles.
roof_hits=[]
for xx in (-.067,0,.067):
    for yy in (-.075,-.025,.025):
        hit,pt,n,idx=body.ray_cast((xx,yy,1),(0,0,-1))
        if hit:roof_hits.append(pt.z)
roof_z=max(roof_hits)+.002
box('Roof identification plaque',(0,-.025,roof_z),(.164,.116,.003),dark,.002)
lettering('Roof identification','I-75',(-.064,-.022,roof_z+.0017),.031,(1,0,0),(0,1,0),maxwidth=.125)
box('Roof plaque rule',(0,-.028,roof_z+.0017),(.128,.0014,.0003),amber,.0001)
lettering('Roof unit','SERVICE UNIT 02',(-.064,-.058,roof_z+.0017),.0085,(1,0,0),(0,1,0),maxwidth=.128)

# Check the closed chassis independent of the decorative imported skin and text.
assert topo(chassis)['nonManifoldEdges']==0
assert topo(belly)['nonManifoldEdges']==0
assert topo(rear)['nonManifoldEdges']==0
sealed_parts=[chassis,belly,rear]
bpy.context.view_layer.update()
bm=bmesh.new()
for o in sealed_parts:
    temp=bmesh.new();temp.from_mesh(o.data);bmesh.ops.transform(temp,matrix=o.matrix_world,verts=list(temp.verts))
    me=bpy.data.meshes.new('probe');temp.to_mesh(me);bm.from_mesh(me);bpy.data.meshes.remove(me);temp.free()
tree=BVHTree.FromBMesh(bm);bm.free()
ray_checks=[]
for axis in range(3):
    for sign in (-1,1):
        for a in (-.06,0,.06):
            for b in (-.06,0,.06):
                target=Vector((0,0,.249));other=[i for i in range(3) if i!=axis]
                target[other[0]]+=a;target[other[1]]+=b
                origin=target.copy();origin[axis]+=sign
                hit,n,idx,d=tree.ray_cast(origin,(target-origin).normalized())
                ok=hit is not None and d<1
                assert ok,('closed chassis probe',axis,sign,a,b)
                ray_checks.append({'axis':axis,'side':sign,'offset':[a,b],'hit':ok})

# UVs for new hard-surface parts and existing skin remain editable. PBR is
# explicitly material-based and all service lettering is real mesh geometry.
for o in parts:
    if o.type!='MESH':continue
    o.data.calc_loop_triangles()
body_after=topo(body)
counts={'triangles':sum(len(o.data.loop_triangles) for o in parts),'objects':len(parts)}
log('EXPORT '+json.dumps(counts))
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.gltf(filepath=str(OUT/'I75_V02_Surface_R03.glb'),export_format='GLB',use_selection=True,
    export_yup=True,export_apply=True,export_normals=True,export_materials='EXPORT',export_texcoords=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'I75_V02_Surface_R03.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='RELATIVE')

# Product views and a true underside view without the presentation floor.
def point_at(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
def area(name,location,power,color,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=location;point_at(o,(0,0,.23));return o
world=bpy.data.worlds.new('R03 neutral studio');scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.15,.19,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.6
area('R03 key',(1,-1,1.6),100,(.9,.95,1),1.25)
area('R03 fill',(-1,-.3,.8),75,(1,.95,.87),1.2)
area('R03 rear',(.3,1.2,1.3),110,(.75,.86,1),1)
area('R03 underside',(-.3,-.2,-.9),55,(.85,.91,1),1)
cam_data=bpy.data.cameras.new('R03 product camera');cam=bpy.data.objects.new('R03 product camera',cam_data)
scene.collection.objects.link(cam);scene.camera=cam;cam_data.type='ORTHO';cam_data.ortho_scale=1.10
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1500;scene.render.resolution_y=1150;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
views=[('01-hero',(1,-1.35,.87),1.12),('02-front-tool-mount',(0,-1.6,.33),.63),
       ('03-right-side',(1.5,0,.255),.94),('04-left-side',(-1.5,0,.255),.94),
       ('05-roof',(.01,-.01,1.8),.94),('06-belly',(.01,-.01,-1.4),.94),
       ('07-rear',(0,1.5,.25),.69),('08-rear-oblique',(-1,1.3,.7),1.12)]
renders=[]
if not options.skip_render:
    for name,loc,scale in views:
        cam.location=loc;point_at(cam,(0,0,.23));cam_data.ortho_scale=scale
        scene.render.filepath=str(ART/(name+'.png'));log('RENDER '+name)
        bpy.ops.render.render(write_still=True);renders.append(str(Path(scene.render.filepath).relative_to(ROOT)))
cam.location=views[0][1];point_at(cam,(0,0,.23));cam_data.ortho_scale=views[0][2]
for im in bpy.data.images:
    # Discard stale R02 maps; these are intentionally not used by R03 materials.
    if im.users==0:bpy.data.images.remove(im)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'I75_V02_Surface_R03.blend'))
assert sha(SOURCE)==source_hash
report={'date':'2026-10-26','source':str(SOURCE.relative_to(ROOT)),'sourceSha256':source_hash,
    'sourceUnchanged':True,'sourceTopology':before,'smallPlanarCapsAdded':filled,'panelTopologyAfter':body_after,
    'counts':counts,'chassisTopology':topo(chassis),'bellyTopology':topo(belly),'rearCoverTopology':topo(rear),
    'closedChassisRayChecks':ray_checks,'labels':label_notes,'roofPlaqueZ':roof_z,
    'labelRendering':'Geometric lettering on separate service plaques; no bitmap labels or stochastic wear maps',
    'toolCoupler':'Blind magnetic face, six visible poles, central register and keyed alignment; geometry only, no physics/workflow',
    'materialCount':len({m.name for o in parts for m in o.data.materials}),'renders':renders,
    'elapsedSeconds':time.time()-start,'unityAssignmentChanged':False,
    'limits':['Outer imported panel skin retains non-manifold intersections where present; closed independent chassis blocks through-body views.',
              'No magnetic force simulation, working tool attachment, rig, mass/strength certification, LOD or Unity acceptance.']}
(OUT/'surface-report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
log('FINISHED '+json.dumps({'triangles':counts['triangles'],'rays':len(ray_checks),'renders':len(renders)}))
