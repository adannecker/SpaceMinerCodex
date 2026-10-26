"""Texture the original I75 Smart Mesh V02, preserving all source polygons.

python tools/MakeI75Stencils.py
work/HallExperiment/blender-4.5.14-windows-x64/blender.exe --background \
 --factory-startup --python-exit-code 1 --python tools/TextureIndoorDroneI75R02.py
Writes a packed Blender scene, UV-mapped FBX, GLB, PBR maps and real renders.
Canonical Blender coordinates: X width, Z up, nose -Y; length 0.75m.
"""
from pathlib import Path
import hashlib,json,math,time
import bpy
import numpy as np
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Design/Models/IndoorDroneI75/Surface-R02-2026-10-26'
TEX=OUT/'Textures'; ART=ROOT/'docs/Art/Drohnen/I75-Textur-R02-2026-10-26'
for directory in (OUT,TEX,ART):directory.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'Assets/SpaceMiner/Resources/IndoorDroneI75/I75_P20_V02.fbx'
N=2048; START=time.time()
def log(message):print('I75 SURFACE:',message,flush=True)
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
obj.name='I75_V02_Surface_R02'; bpy.context.view_layer.objects.active=obj
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
mesh=obj.data; mesh.name='I75_V02_OriginalGeometry_WithUV'
lo=np.min([v.co[:] for v in mesh.vertices],axis=0);hi=np.max([v.co[:] for v in mesh.vertices],axis=0)
factor=.75/(hi[1]-lo[1]);centre=(lo+hi)/2;centre[2]=lo[2]
for v in mesh.vertices:v.co=Vector((np.array(v.co[:])-centre)*factor)
mesh.update();mesh.calc_loop_triangles()
assert len(mesh.vertices)==10510 and len(mesh.polygons)==10659 and len(mesh.loop_triangles)==18312
# R02: clear the soft imported normals, preserve the core geometry, and
# add small angular replaceable corner guards/rails with actual 1.5mm chamfers.
# This changes the silhouette rather than relying on a sharper highlight alone.
if mesh.has_custom_normals:
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
bpy.ops.object.shade_smooth_by_angle(angle=math.radians(22),keep_sharp_edges=False)
weighted=obj.modifiers.new('Machined panel normals','WEIGHTED_NORMAL')
weighted.keep_sharp=True;weighted.weight=80
bpy.ops.object.modifier_apply(modifier=weighted.name)
parts=[obj]
def guard(name,location,size,bevel=.0015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=location)
    piece=bpy.context.object;piece.name=name;piece.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    modifier=piece.modifiers.new('Small machined chamfer','BEVEL');modifier.width=bevel;modifier.segments=1
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    for face in piece.data.polygons:face.use_smooth=False
    parts.append(piece)
for sx in (-1,1):
    for sy in (-1,1):
        guard('Thin corner angle / outer leg',(sx*.238,sy*.315,.25),(.006,.078,.18),.001)
        guard('Thin corner angle / end leg',(sx*.207,sy*.349,.25),(.063,.006,.18),.001)
        for zz in (.188,.314):
            bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=.0047,depth=.0035,location=(sx*.207,sy*.354,zz),rotation=(math.pi/2,0,0))
            bolt=bpy.context.object;bolt.name='Exposed steel hex fastener';parts.append(bolt)
    guard('Straight lower guard rail',(sx*.235,0,.149),(.012,.58,.022),.001)
bpy.ops.object.select_all(action='DESELECT')
for piece in parts:piece.select_set(True)
bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
mesh=obj.data;mesh.calc_loop_triangles()
log('R02 angular guard kit: %d triangles total; original body retained'%len(mesh.loop_triangles))
original_positions=np.array([v.co[:] for v in mesh.vertices],dtype=np.float32)
log('Original geometry normalized; creating UV islands')
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(58),island_margin=.0007,area_weight=.2,correct_aspect=True)
bpy.ops.object.mode_set(mode='OBJECT')
mesh.uv_layers.active.name='I75_Surface_UV';mesh.calc_loop_triangles()

# Rasterize UV triangles to canonical positions. All artwork is authored against
# the real mesh, including both long sides, roof, front and magnetic feet.
positions=np.zeros((N,N,3),np.float32); normals=positions.copy()
curvature=np.zeros((N,N),np.float32);valid=np.zeros((N,N),bool)
vcurv=np.zeros(len(mesh.vertices),np.float32)
edge_faces={}
for face in mesh.polygons:
    for key in face.edge_keys:edge_faces.setdefault(tuple(sorted(key)),[]).append(face.index)
for (a,b),faces in edge_faces.items():
    if len(faces)!=2:continue
    dot=max(-1,min(1,mesh.polygons[faces[0]].normal.dot(mesh.polygons[faces[1]].normal)))
    value=min(1,math.acos(dot)/1.2)
    vcurv[a]=max(vcurv[a],value);vcurv[b]=max(vcurv[b],value)
uv=mesh.uv_layers.active.data
for tri in mesh.loop_triangles:
    coords=np.array([uv[i].uv[:] for i in tri.loops],np.float32)*N-.5
    mn=np.maximum(np.floor(coords.min(axis=0)).astype(int),0);mx=np.minimum(np.ceil(coords.max(axis=0)).astype(int),N-1)
    if np.any(mx<mn):continue
    a,b,c=coords;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(den)<1e-7:continue
    yy,xx=np.mgrid[mn[1]:mx[1]+1,mn[0]:mx[0]+1].astype(np.float32)
    u=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
    v=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;w=1-u-v
    inside=(u>=-.0001)&(v>=-.0001)&(w>=-.0001)
    if not inside.any():continue
    region=np.s_[mn[1]:mx[1]+1,mn[0]:mx[0]+1]
    vi=list(tri.vertices);xyz=original_positions[vi]
    positions[region][inside]=(u[...,None]*xyz[0]+v[...,None]*xyz[1]+w[...,None]*xyz[2])[inside]
    normals[region][inside]=mesh.polygons[tri.polygon_index].normal[:]
    curvature[region][inside]=(u*vcurv[vi[0]]+v*vcurv[vi[1]]+w*vcurv[vi[2]])[inside]
    valid[region][inside]=True
log('UV surface coverage %.1f%%'%(valid.mean()*100))
x,y,z=positions.transpose(2,0,1);nx,ny,nz=normals.transpose(2,0,1)
rng=np.random.default_rng(261026)
grain=rng.normal(0,1,(N,N)).astype(np.float32)
noise=np.sin(x*1931+y*827+z*3981)*np.sin(y*1207-z*2293)
base=np.empty((N,N,4),np.float32);base[:]=(.025,.062,.104,1)
metal=np.full((N,N),.035,np.float32);rough=np.full((N,N),.58,np.float32)
height=np.clip(.5+grain*.025,0,1)
emission=np.zeros_like(base);emission[:,:,3]=1
def paint(mask,color,m=.15,r=.42):
    base[mask,:3]=color;metal[mask]=m;rough[mask]=r
frame=((abs(x)>.198)&((z<.226)|(abs(y)>.245)))|((abs(y)>.317)&((abs(x)>.124)|(z<.199)))
frame|=(z>.365)&(abs(x)>.162)
paint(frame,(.39,.41,.375),.045,.58)
paint((z<.133),(.033,.044,.052),.65,.43)
paint((z<.064)&(z>.024),(.47,.50,.48),.65,.33)
paint(z<.023,(.026,.036,.040),.18,.69)
hardware=(z>.375)&(abs(x)>.10)&(abs(y)>.10)
hardware|=(z>.365)&(abs(y)>.26)
hardware|=(abs(x)>.181)&(abs(y)>.263)&(z>.140)&(z<.364)
hardware|=(abs(x)>.215)&(abs(y)<.291)&(z>.127)&(z<.163)
paint(hardware,(.255,.28,.285),.97,.49)
brush=np.sin(y*1850+z*71)*np.sin(x*135+z*43)
base[hardware,:3]*=(1+brush[hardware,None]*.025)
rough[hardware]=.49+brush[hardware]*.018
height[hardware]=np.clip(.5+brush[hardware]*.025+grain[hardware]*.015,0,1)
paint((abs(x)<.12)&(y<-.318)&(z>.205)&(z<.315),(.19,.25,.29),.5,.34)
# Vent slots, recessed sensor surround, exposed tool-interface socket.
vents=(abs(x)>.195)&(y>.115)&(y<.257)&(z>.24)&(z<.305)&(abs(nx)>.6)
paint(vents&((np.floor(y/.010).astype(int)%3)==0),(.017,.026,.033),.5,.48)
sensor=(y<-.315)&(abs(x)<.105)&(z>.321)&(z<.369)
paint(sensor,(.019,.031,.037),.44,.27)
socket=(y<-.32)&(x*x+(z-.254)**2<.026**2)
paint(socket,(.027,.038,.043),.7,.25)
paint(socket&(x*x+(z-.254)**2>.021**2),(.48,.52,.53),.92,.25)
# Small functional warnings, not a continuous glowing outline.
warning=((y<-.315)&(abs(x)>.122)&(abs(x)<.199)&(z>.176)&(z<.196))
warning|=((abs(x)>.201)&(y>.254)&(y<.306)&(z>.198)&(z<.222))
stripe=(np.floor((y+x+z*1.7)/.011).astype(int)&1)==0
paint(warning,(.93,.57,.095),.08,.47)
paint(warning&stripe,(.026,.036,.041),.12,.5)
led=(abs(x)>.195)&(abs(nx)>.6)&(y>.035)&(y<.185)&(abs(z-.337)<.0017)
lens=(y<-.322)&(((x-.044)**2+(z-.346)**2<.009**2)|((x+.044)**2+(z-.346)**2<.009**2))
paint(led|lens,(.065,.69,.78),.12,.23);emission[led|lens,:3]=(.035,.65,.78)

# Readable painted service stencils, projected onto the existing side panels.
stencil=bpy.data.images.load(str(ROOT/'Design/Models/IndoorDroneI75/Surface-R01-2026-10-26/Source/I75_service-stencil.png'),check_existing=True)
sw,sh=stencil.size;st=np.array(stencil.pixels[:],np.float32).reshape(sh,sw,4)
def decal(u,v,mask):
    inside=mask&(u>=0)&(u<=1)&(v>=0)&(v<=1)
    iy=np.clip((v*(sh-1)).astype(int),0,sh-1);ix=np.clip((u*(sw-1)).astype(int),0,sw-1)
    rgba=st[iy,ix];alpha=rgba[:,:,3]*inside
    base[:,:,:3]=base[:,:,:3]*(1-alpha[:,:,None])+rgba[:,:,:3]*alpha[:,:,None]
    metal[:]=metal*(1-alpha)+alpha*.08
    rough[:]=rough*(1-alpha)+alpha*.46
decal(np.where(x>0,(y+.175)/.31,(.135-y)/.31),(z-.209)/.102,(abs(x)>.175)&(abs(nx)>.55))
# A small repeated unit designation on the roof, rotated toward the front.
decal((x+.11)/.22,(y+.11)/.095,(z>.36)&(nz>.7))
log('Industrial paint, warnings and service markings authored')

# Restrained physical wear and directional brushing. Surface curvature exposes
# metal on selected edges; the source mesh is not repaired or subdivided.
scratches=(np.sin(z*7100+y*83)>.9997)&(noise>.75)&(z>.13)
wear=(curvature>.85)&(noise>.94)&(z>.07)
wear|=scratches
wear&=valid&(~(led|lens))
base[wear,:3]=(.34,.38,.39);metal[wear]=.96;rough[wear]=.35
# Irregular abrasion on hardware, plus darker grime in contact recesses.
metal_scuff=hardware&(noise>.91)&(curvature>.8)
base[metal_scuff,:3]=(.33,.36,.355);metal[metal_scuff]=.98;rough[metal_scuff]=.30
height[metal_scuff]=np.clip(height[metal_scuff]+.025,0,1)
base[:,:,:3]*=(1+grain*.012+noise*.016)[:,:,None]
rough=np.clip(rough+grain*.012+noise*.017,.17,.8)
height=np.clip(height+scratches*.15,0,1)

def padded(array,pixels=3):
    result=array.copy();mask=valid.copy()
    for _ in range(pixels):
        expanded=mask.copy()
        for dy,dx in ((0,1),(0,-1),(1,0),(-1,0)):
            neighbor=np.roll(mask,(dy,dx),(0,1));take=(~expanded)&neighbor
            shifted=np.roll(result,(dy,dx),(0,1));result[take]=shifted[take];expanded|=take
        mask=expanded
    return result
def image(name,array,color=False,pad=True):
    if array.ndim==2:array=np.stack((array,array,array,np.ones_like(array)),axis=2)
    if pad:array=padded(array)
    im=bpy.data.images.new(name,width=N,height=N,alpha=True)
    im.colorspace_settings.name='sRGB' if color else 'Non-Color'
    im.pixels.foreach_set(np.clip(array,0,1).astype(np.float32).ravel())
    im.file_format='PNG';im.filepath_raw=str(TEX/(name+'.png'));im.save()
    return im

# True mesh occlusion bake, independent of the presentation floor and lights.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
scene.cycles.device='CPU';scene.render.bake.margin=3
mat=bpy.data.materials.new('I75_R02_PBR');mat.use_nodes=True
obj.data.materials.clear();obj.data.materials.append(mat)
nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
ao=image('I75_R02_Occlusion',np.ones((N,N),np.float32),pad=False)
target=nodes.new('ShaderNodeTexImage');target.image=ao;nodes.active=target
log('Baking actual self-occlusion')
bpy.ops.object.bake(type='AO');ao.save()
aoval=np.array(ao.pixels[:],np.float32).reshape(N,N,4)[:,:,0]
base[:,:,:3]*=(.77+.23*np.clip(aoval,0,1))[:,:,None]
grime=hardware&(aoval<.70)
base[grime,:3]*=(.65+.35*aoval[grime,None])
rough[grime]=np.clip(rough[grime]+.16,0,1)
base_image=image('I75_R02_BaseColor',base,True)
orm=np.stack((aoval,rough,metal,np.ones_like(metal)),axis=2)
orm_image=image('I75_R02_ORM',orm)
unity=np.stack((metal,metal,metal,1-rough),axis=2)
image('I75_R02_UnityMetalSmooth',unity)
em_image=image('I75_R02_Emission',emission,True)
height_image=image('I75_R02_Height',height)
def texture(im,label,location):
    node=nodes.new('ShaderNodeTexImage');node.image=im;node.label=label;node.location=location
    return node
albedo_node=texture(base_image,'PAINT + WEAR + STENCILS',(-650,250));links.new(albedo_node.outputs['Color'],bsdf.inputs['Base Color'])
orm_node=texture(orm_image,'R occlusion / G roughness / B metallic',(-650,0))
sep=nodes.new('ShaderNodeSeparateColor');sep.location=(-370,0);links.new(orm_node.outputs['Color'],sep.inputs[0])
links.new(sep.outputs['Green'],bsdf.inputs['Roughness']);links.new(sep.outputs['Blue'],bsdf.inputs['Metallic'])
em_node=texture(em_image,'STATUS INDICATORS',(-650,-230));links.new(em_node.outputs['Color'],bsdf.inputs['Emission Color']);bsdf.inputs['Emission Strength'].default_value=2
height_node=texture(height_image,'Fine coating grain',(-650,-450))
bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.00045
links.new(height_node.outputs['Color'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
normal=image('I75_R02_Normal',np.tile(np.array([.5,.5,1,1],np.float32),(N,N,1)),pad=False)
target.image=normal;nodes.active=target
log('Baking tangent-space coating normal map')
bpy.ops.object.bake(type='NORMAL',normal_space='TANGENT');normal.save()
links.remove(bsdf.inputs['Normal'].links[0]);nodes.remove(bump);nodes.remove(height_node);nodes.remove(target)
normal_node=texture(normal,'TANGENT NORMAL',(-650,-460));normal_map=nodes.new('ShaderNodeNormalMap');normal_map.location=(-340,-330)
links.new(normal_node.outputs['Color'],normal_map.inputs['Color']);links.new(normal_map.outputs['Normal'],bsdf.inputs['Normal'])
bsdf.location=(30,160);nodes.get('Material Output').location=(370,160)

# Only the original body is selected for export. The render set is separate.
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
assert np.max(np.abs(np.array([v.co[:] for v in mesh.vertices])-original_positions))==0
log('Exporting UV-mapped FBX and embedded-texture GLB')
bpy.ops.export_scene.fbx(filepath=str(OUT/'I75_V02_Surface_R02.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='RELATIVE',use_mesh_modifiers=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'I75_V02_Surface_R02.glb'),export_format='GLB',use_selection=True,
    export_apply=False,export_yup=True,export_materials='EXPORT',export_normals=True,export_texcoords=True)
obj['source']='Original Tripo P2.0 V02 core plus angular guard kit; see surface-report for total count'
obj['scale_metres']='0.75m length; nose -Y in Blender, Z up; UV and PBR finish R02'

def point_at(node,target):node.rotation_euler=(Vector(target)-node.location).to_track_quat('-Z','Y').to_euler()
def area(name,location,power,color,size,target=(0,0,.22)):
    light=bpy.data.lights.new(name,'AREA');light.energy=power;light.color=color;light.shape='DISK';light.size=size
    node=bpy.data.objects.new(name,light);scene.collection.objects.link(node);node.location=location;point_at(node,target)
def simple_material(name,color,metallic,roughness):
    material=bpy.data.materials.new(name);material.diffuse_color=(*color,1);material.use_nodes=True
    node=material.node_tree.nodes.get('Principled BSDF');node.inputs['Base Color'].default_value=(*color,1)
    node.inputs['Metallic'].default_value=metallic;node.inputs['Roughness'].default_value=roughness;return material
floor_mat=simple_material('Presentation / graphite',(.024,.034,.045),.12,.53)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));floor=bpy.context.object;floor.name='PRESENTATION / floor';floor.data.materials.append(floor_mat)
world=bpy.data.worlds.new('Cool studio');scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.17,.23,1);world.node_tree.nodes['Background'].inputs[1].default_value=.35
area('KEY / large softbox',(1.1,-.9,1.5),115,(.83,.91,1),1.4)
area('FILL / soft warm',(-.9,-.35,.8),65,(1,.90,.74),1)
area('RIM / overhead strip',(.25,1.1,1.2),150,(.47,.76,1),1.1)
camera_data=bpy.data.cameras.new('I75 product camera');camera=bpy.data.objects.new('I75 product camera',camera_data)
scene.collection.objects.link(camera);scene.camera=camera;camera_data.type='ORTHO';camera_data.ortho_scale=1.24;camera_data.lens=55
scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True
scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False;scene.view_settings.view_transform='AgX'
scene.render.resolution_x=1800;scene.render.resolution_y=1350
views=[('01-I75-R02-hero',(1.1,-1.35,.92),1.19),('02-I75-R02-rear',(-1.1,1.35,.86),1.19),('03-I75-R02-side',(1.5,.02,.62),1.13)]
renders=[]
for name,location,scale in views:
    camera.location=location;camera_data.ortho_scale=scale;point_at(camera,(0,0,.22))
    scene.render.filepath=str(ART/(name+'.png'));log('Rendering '+name);bpy.ops.render.render(write_still=True);renders.append(str(scene.render.filepath))
camera.location=views[0][1];camera_data.ortho_scale=views[0][2];point_at(camera,(0,0,.22))
for image_data in bpy.data.images:
    if image_data.source=='FILE' or image_data.filepath:image_data.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'I75_V02_Surface_R02.blend'))
report={'date':'2026-10-26','source':str(SOURCE.relative_to(ROOT)),'sourceSha256':digest(SOURCE),
    'vertices':len(mesh.vertices),'polygons':len(mesh.polygons),'triangles':len(mesh.loop_triangles),
    'dimensionsMetres':[float(v) for v in obj.dimensions],'uvLayers':len(mesh.uv_layers),'uvRasterCoverage':float(valid.mean()),
    'materialCount':len(mesh.materials),'textureResolution':[N,N],'textures':[p.name for p in TEX.glob('*.png')],
    'renders':renders,'coreGeometryRetained':True,'geometryUnchangedAfterUVAuthoring':True,'addedParts':'4 thin corner-angle sets (8 plates), 2 straight rails, 8 hex fasteners; smooth-angle22 and weighted panel normals','elapsedSeconds':time.time()-START,
    'notes':['Actual Blender renders of original V02 mesh, not generated concept art.','R02 angular guard kit and harder normals; original body retained. No rig, separate magnetic feet or Unity build replacement.','Two-dimensional stencil artwork is authored locally; no Tripo credits or external generated textures.']}
(OUT/'surface-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
log('FINISHED '+json.dumps(report))
