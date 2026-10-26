"""Rebuild the authored battery-bay insert using existing portable Blender.

  work/HallExperiment/blender-4.5.14-windows-x64/blender.exe --background \
    --python-exit-code 1 --python tools/CreateBatteryBay.py

No downloaded models, generated artwork, external texture dependencies or add-ons.
Canonical asset coordinates: +X width, +Y up, +Z into bay, front -Z, pivot
at floor/front centre. Blender geometry uses (x,-z,y), exported -Z forward/Y up.
"""
from pathlib import Path
import math
import json
import uuid
import re
import hashlib
import sys
import bpy
import bmesh
import numpy as np
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/SpaceMiner/Resources/BatteryBay'
ART = ROOT / 'Design/Models/BatteryBay'
PREVIEW = ROOT / 'Logs/BatteryBayAsset'
for directory in (OUT, ART, OUT/'Textures', OUT/'Materials', PREVIEW):
    directory.mkdir(parents=True, exist_ok=True)
SEED = 251025
N = 2048
TILE = N//4
SWATCHES = [
    ('Graphite', (.080,.105,.125), .30,.54),
    ('Slate', (.195,.245,.275), .34,.49),
    ('MachinedAlloy', (.52,.57,.59), .92,.29),
    ('CoolantBlue', (.035,.235,.34), .20,.43),
    ('SafetyAmber', (.88,.41,.055), .16,.57),
    ('Elastomer', (.018,.026,.031), .02,.76),
    ('CeramicGrey', (.48,.52,.50), .11,.61),
    ('Recess', (.027,.043,.052), .18,.68),
    ('Label', (.72,.77,.76), .08,.63),
    ('BlackText', (.013,.019,.023), .02,.77),
    ('WarningStripes', (.78,.39,.06), .14,.57),
    ('Copper', (.43,.22,.095), .90,.37),
    ('Titanium', (.30,.35,.37), .88,.38),
    ('Vent', (.050,.068,.077), .75,.51),
    ('Diffuser', (.66,.76,.77), .06,.50),
    ('Anodised', (.085,.125,.15), .80,.40),
]
TAGS = {s[0]: i for i,s in enumerate(SWATCHES)}
MATS = {}
GROUPS = {}
LOD = 0
COUNTER = 0
ALL_ROOTS = []


def stable_guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'SpaceMiner/BatteryBay/'+path.as_posix()).hex


def write_meta(path, body):
    meta = Path(str(path)+'.meta')
    if meta.exists():
        old = re.search(r'^guid: ([a-f0-9]+)', meta.read_text(), re.M)
        guid = old.group(1) if old else stable_guid(path.relative_to(ROOT))
    else:
        guid = stable_guid(path.relative_to(ROOT))
    meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+body, encoding='utf-8')
    return guid


def image_from_array(name, data, srgb):
    path = OUT/'Textures'/name
    im = bpy.data.images.new(name, width=N, height=N, alpha=True)
    im.colorspace_settings.name = 'sRGB' if srgb else 'Non-Color'
    im.pixels.foreach_set(np.asarray(data, dtype=np.float32).ravel())
    im.filepath_raw = str(path)
    im.file_format = 'PNG'
    im.save()
    return im


def make_atlas():
    rng = np.random.default_rng(SEED)
    albedo = np.ones((N,N,4), np.float32)
    metal_smooth = albedo.copy()
    metal_rough = albedo.copy()
    normals = albedo.copy()
    yy,xx = np.mgrid[0:TILE,0:TILE].astype(np.float32)
    u,v = xx/(TILE-1),yy/(TILE-1)
    edge = np.minimum.reduce((u,1-u,v,1-v))
    for index,(name,color,metal,rough) in enumerate(SWATCHES):
        row,col = divmod(index,4)
        noise = rng.normal(0,1,(TILE,TILE)).astype(np.float32)
        grain = .014*noise + .006*np.sin(xx*2.7 + yy*.3)
        broad = .012*np.sin(xx*.035)*np.cos(yy*.025)
        wear = np.clip((.018-edge)/.018,0,1)*(.04+.05*(noise>.35))
        # Fine directional abrasions, restrained so the hardware stays maintained.
        scratches = np.zeros((TILE,TILE), np.float32)
        for _ in range(85):
            x,y = rng.integers(5,TILE-6,2)
            length = int(rng.integers(2,24))
            scratches[y:y+1,x:min(TILE-2,x+length)] = float(rng.uniform(.015,.05))
        shade = 1+grain+broad
        rgb = np.asarray(color)[None,None,:]*shade[:,:,None]
        if name not in ('Elastomer','BlackText','Recess','Label','Diffuser'):
            rgb += (wear+scratches)[:,:,None]
        if name == 'WarningStripes':
            stripe = ((np.floor((xx+yy)/54).astype(int)&1)==0)
            rgb[stripe] = np.asarray(SWATCHES[9][1])*shade[stripe,None]
        region = np.s_[row*TILE:(row+1)*TILE,col*TILE:(col+1)*TILE]
        albedo[region][:,:,:3] = np.clip(rgb,0,1)
        smooth = np.clip(1-rough+grain*.35-wear*.3,.05,.95)
        metal_smooth[region][:,:,:3] = metal
        metal_smooth[region][:,:,3] = smooth
        metal_rough[region][:,:,0] = metal
        metal_rough[region][:,:,1] = 1-smooth
        metal_rough[region][:,:,2] = 1
        # Tangent-space micrograin with a neutral +Y convention; no faux bolts.
        nx = noise*.009 + np.sin(xx*2.1)*.008
        ny = np.roll(noise,1,axis=1)*.009
        normals[region][:,:,0] = .5+nx
        normals[region][:,:,1] = .5+ny
        normals[region][:,:,2] = np.sqrt(np.maximum(0,1-(nx*2)**2-(ny*2)**2))*.5+.5
    images = {
        'albedo':image_from_array('BatteryBay_Albedo.png',albedo,True),
        'normal':image_from_array('BatteryBay_Normal.png',normals,False),
        'metal':image_from_array('BatteryBay_MetalSmooth.png',metal_smooth,False),
        'rough':image_from_array('BatteryBay_MetalRough.png',metal_rough,False),
    }
    surface = bpy.data.materials.new('BatteryBay_Surface'); surface.use_nodes=True
    nodes=surface.node_tree.nodes; links=surface.node_tree.links
    bs=nodes.get('Principled BSDF')
    for key in ('albedo','normal','metal'):
        node=nodes.new('ShaderNodeTexImage');node.image=images[key];node.interpolation='Linear'
        if key=='albedo':links.new(node.outputs['Color'],bs.inputs['Base Color'])
        if key=='normal':
            normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.32
            links.new(node.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],bs.inputs['Normal'])
        if key=='metal':
            sep=nodes.new('ShaderNodeSeparateColor');links.new(node.outputs['Color'],sep.inputs[0])
            links.new(sep.outputs['Red'],bs.inputs['Metallic'])
            invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1
            links.new(node.outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],bs.inputs['Roughness'])
    MATS['surface']=surface
    for name,color,rough in [('Display',(.012,.033,.045),.24),('Status',(.07,.11,.12),.33)]:
        m=bpy.data.materials.new('BatteryBay_'+name);m.use_nodes=True
        bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
        bs.inputs['Metallic'].default_value=.18;bs.inputs['Roughness'].default_value=rough
        m.diffuse_color=(*color,1);MATS[name.lower()]=m


def B(point):
    return Vector((point[0],-point[2],point[1]))


def finish(obj, group, tag, bevel=0):
    global COUNTER
    COUNTER+=1;obj.name=f'{group}__{tag}__{COUNTER:04}'
    obj.data.materials.append(MATS[tag] if tag in MATS else MATS['surface'])
    bpy.context.view_layer.objects.active=obj
    if bevel and LOD<2:
        mod=obj.modifiers.new('Fabricated edge radius','BEVEL');mod.width=bevel
        mod.segments=2 if LOD==0 and bevel>=.018 else 1;mod.affect='EDGES'
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # UV each manufactured part independently into a shared material atlas tile.
    tile=TAGS.get(tag,0); row,col=divmod(tile,4)
    uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='SurfaceAtlas')
    xyz=np.array([tuple(vertex.co) for vertex in obj.data.vertices])
    lo,hi=xyz.min(0),xyz.max(0);span=np.maximum(hi-lo,1e-6)
    for face in obj.data.polygons:
        axis=max(range(3),key=lambda i:abs(face.normal[i]));a,b=[i for i in range(3) if i!=axis]
        for li in face.loop_indices:
            co=xyz[obj.data.loops[li].vertex_index]
            pu,pv=(co[a]-lo[a])/span[a],(co[b]-lo[b])/span[b]
            uv.data[li].uv=((col+.008+pu*.984)/4,(row+.008+pv*.984)/4)
        face.use_smooth=True
    mod=obj.modifiers.new('Machined face normals','WEIGHTED_NORMAL');mod.weight=60;mod.keep_sharp=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    GROUPS.setdefault(group,[]).append(obj)
    return obj


def box(group,tag,point,size,bevel=.012):
    bpy.ops.mesh.primitive_cube_add(size=1,location=B(point))
    obj=bpy.context.object;obj.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(obj,group,tag,bevel)


def tube(group,tag,points,radius=.025,sides=None):
    sides=sides or (12 if LOD==0 else 8)
    centres=[B(p) for p in points];verts=[];last=None
    for i,p in enumerate(centres):
        tangent=(centres[min(i+1,len(centres)-1)]-centres[max(0,i-1)]).normalized()
        right=tangent.cross(Vector((0,0,1))) if last is None else last-tangent*last.dot(tangent)
        if right.length<.001:right=tangent.cross(Vector((0,1,0)))
        right.normalize();normal=tangent.cross(right).normalized();last=right
        for n in range(sides):
            angle=n*math.tau/sides;verts.append(p+radius*(math.cos(angle)*right+math.sin(angle)*normal))
    faces=[tuple(reversed(range(sides))),tuple((len(centres)-1)*sides+i for i in range(sides))]
    for i in range(len(centres)-1):
        for n in range(sides):
            a=i*sides+n;b=i*sides+(n+1)%sides;faces.append((a,b,b+sides,a+sides))
    mesh=bpy.data.meshes.new(group+' pipe');mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(group,mesh);bpy.context.scene.collection.objects.link(obj)
    return finish(obj,group,tag,0)


def bolt(group,p,axis='front',radius=.018):
    if LOD>0:return
    delta=(0,0,.014) if axis=='front' else (.014,0,0)
    tube(group,'MachinedAlloy',[tuple(p[i]-delta[i]*.5 for i in range(3)),tuple(p[i]+delta[i]*.5 for i in range(3))],radius,6)
    # Recessed central socket rather than a floating dark disc.
    if axis=='front':
        box(group,'Recess',(p[0],p[1],p[2]-.0078),(radius*.63,radius*.30,.0015),0)


def text(group,label,p,size=.06,tag='Label'):
    if LOD>0:return
    curve=bpy.data.curves.new('Permanent safety label','FONT');curve.body=label;curve.align_x='CENTER'
    curve.align_y='CENTER';curve.size=size;curve.extrude=0
    obj=bpy.data.objects.new('Engraved label',curve);bpy.context.scene.collection.objects.link(obj)
    # Blender text XY -> canonical X/Y, normal toward the accessible front -Z.
    right,up,normal=Vector((-1,0,0)),Vector((0,0,1)),Vector((0,1,0))
    obj.matrix_world=Matrix.Translation(B(p))@Matrix((right,up,normal)).transposed().to_4x4()
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH');return finish(obj,group,tag,0)


def portal():
    def points(w,low,high,c):
        return [(-w/2+c,low),(w/2-c,low),(w/2,low+c),(w/2,high-c),
                (w/2-c,high),(-w/2+c,high),(-w/2,high-c),(-w/2,low+c)]
    outer=points(2.30,0,3.22,.19);inner=points(1.98,.15,2.86,.14)
    vertices=[]
    for z in (0,.18):
        for ring in (outer,inner):vertices.extend(B((x,y,z)) for x,y in ring)
    faces=[]
    for n in range(8):
        j=(n+1)%8
        faces.extend([(n,j,j+16,n+16),(n+8,n+24,j+24,j+8),
                      (n,n+8,j+8,j),(n+16,j+16,j+24,n+24)])
    mesh=bpy.data.meshes.new('Octagonal structural portal');mesh.from_pydata(vertices,[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
    obj=bpy.data.objects.new('Frame',mesh);bpy.context.scene.collection.objects.link(obj)
    finish(obj,'Frame','Slate',.016)
    # Real bolted mounting pads and a protected blank status panel.
    for x in (-1.045,1.045):
        for y in (.37,1.28,2.28,2.91):bolt('Frame',(x,y,-.007))
    box('Frame','Recess',(0,3.015,.004),(1.38,.235,.055),.024)
    box('ActiveDisplay','display',(0,3.015,-.026),(1.24,.160,.006),.011)
    for x in (-.91,.91):
        box('Frame','Graphite',(x,3.015,.005),(.24,.13,.05),.017)
        box('StatusLenses','status',(x,3.015,-.025),(.18,.067,.007),.011)


def cassette(number,x):
    group=f'Rack/Cassette{number:02}'
    # Four independently serviceable modules; front handles and latches face -Z.
    box(group,'Graphite',(x,1.065,2.045),(.423,1.62,1.39),.052)
    box(group,'Recess',(x,1.065,1.338),(.389,1.48,.035),.025)
    box(group,'Slate',(x,1.065,1.306),(.345,1.375,.058),.035)
    # Machined corner shoes continue over the lid and below the cassette.
    for side in (-1,1):
        box(group,'MachinedAlloy',(x+side*.152,1.065,1.265),(.042,1.405,.056),.009)
        box(group,'Titanium',(x+side*.152,1.88,2.01),(.047,.057,1.27),.009)
        box(group,'Titanium',(x+side*.152,.255,2.01),(.047,.065,1.27),.008)
        box(group,'Elastomer',(x+side*.15,.205,2.08),(.09,.10,1.2),.017)
        if LOD<2:
            # U-shaped accessible pull handle with two mounting blocks.
            handle_x=x+side*.094
            tube(group,'Elastomer',[(handle_x,.70,1.255),(handle_x,.70,1.17),(handle_x,.755,1.135),
                (handle_x,1.39,1.135),(handle_x,1.445,1.17),(handle_x,1.445,1.255)],.021)
            for y in (.70,1.445):box(group,'MachinedAlloy',(handle_x,y,1.26),(.062,.068,.065),.009)
        for y in (.43,1.69):
            box(group,'Titanium',(x+side*.117,y,1.263),(.095,.097,.038),.012)
            box(group,'SafetyAmber',(x+side*.117,y,1.238),(.034,.063,.015),.005)
            bolt(group,(x+side*.117,y,1.224),radius=.009)
    box(group,'Label',(x,1.57,1.269),(.155,.076,.004),.003)
    text(group,f'C{number:02}',(x,1.57,1.265),.045,'BlackText')
    box(group,'WarningStripes',(x,.544,1.27),(.17,.045,.004),0)
    text(group,'PULL',(x,1.065,1.264),.038)
    if LOD<2:
        for n in range(5 if LOD==0 else 3):
            box(group,'Vent',(x,.831+n*.076,1.273),(.042,.028,.012),.004)
        # Recessed side cooling fins are geometry, not a photograph.
        for side in (-1,1):
            box(group,'Recess',(x+side*.213,1.08,2.07),(.014,1.10,.84),.008)
            for n in range(7 if LOD==0 else 4):
                box(group,'Titanium',(x+side*.225,.66+n*.13,2.07),(.022,.032,.77),.004)


def build(level):
    global LOD,GROUPS,COUNTER
    LOD=level;GROUPS={};COUNTER=0
    portal()
    box('Housing','Graphite',(0,.055,1.66),(2.22,.11,2.96),.026)
    box('Housing','Slate',(0,3.10,1.66),(2.22,.13,2.96),.027)
    box('Housing','Graphite',(0,1.60,3.085),(2.22,3.00,.11),.025)
    for side in (-1,1):
        box('Housing','Graphite',(side*1.08,1.60,1.66),(.08,3.00,2.96),.020)
        for z in (.53,1.50,2.52):
            box('Housing','Slate',(side*1.025,1.59,z),(.023,2.69,.85),.017)
            box('Frame','MachinedAlloy',(side*1.006,1.60,z+.45),(.070,2.90,.064),.012)
            for y in (.36,2.73):bolt('Housing',(side*.998,y,z),'side',.018)
        # Blue/orange enclosed utility runs fixed to the housing, useful in empty slots too.
        tube('Housing','CoolantBlue',[(side*.93,.30,2.95),(side*.93,2.68,2.95),(side*.92,2.79,2.84),
                (side*.92,2.79,.48)],.033)
        tube('Housing','SafetyAmber',[(side*.84,.30,2.96),(side*.84,2.59,2.96),(side*.84,2.69,2.84),
                (side*.84,2.69,.51)],.022)
        if LOD<2:
            for z in (.60,1.47,2.40):
                box('Housing','Titanium',(side*.925,2.79,z),(.11,.10,.052),.008)
                box('Housing','Titanium',(side*.84,2.69,z),(.075,.075,.045),.007)
        box('Housing','MachinedAlloy',(side*.84,.22,.72),(.025,.025,1.04),.004)
    # Bolted rear structure, rail-guided floor and integral overhead work diffuser.
    box('Housing','Slate',(0,2.31,2.986),(1.83,.92,.065),.025)
    box('Housing','Recess',(0,.15,.70),(1.82,.045,1.04),.012)
    for i in range(17 if LOD==0 else 9):
        box('Housing','Titanium',(0,.177,.25+i*(.90/(16 if LOD==0 else 8))),(1.78,.016,.014),.002)
    box('Housing','WarningStripes',(0,.181,.204),(1.86,.004,.075),0)
    box('Housing','Graphite',(0,2.985,1.00),(1.30,.08,.28),.019)
    box('Housing','Diffuser',(0,2.939,1.00),(1.16,.014,.19),.011)
    for x in (-.75,-.25,.25,.75):
        for side in (-1,1):
            box('Rack/Backplane','Titanium',(x+side*.17,.184,2.08),(.039,.075,1.61),.007)
            box('Rack/Backplane','MachinedAlloy',(x+side*.17,.204,1.27),(.075,.060,.12),.006)
    box('Rack/Backplane','Recess',(0,.145,2.08),(1.98,.1,1.70),.014)
    for i,x in enumerate((-.72,-.24,.24,.72),1):cassette(i,x)
    # Touch-safe removable busbar cover, separate contactor and disconnect handles.
    box('Rack/CoveredBusbar','Graphite',(0,2.27,2.843),(1.77,.51,.17),.036)
    box('Rack/CoveredBusbar','Titanium',(0,2.272,2.740),(1.64,.397,.040),.016)
    box('Rack/CoveredBusbar','SafetyAmber',(-.63,2.27,2.713),(.075,.32,.012),.005)
    box('Rack/CoveredBusbar','Label',(0,2.325,2.713),(.85,.104,.004),.003)
    text('Rack/CoveredBusbar','ISOLATE BEFORE SERVICE',(0,2.325,2.708),.056,'BlackText')
    for x in (-.73,.73):
        for y in (2.12,2.43):bolt('Rack/CoveredBusbar',(x,y,2.708),radius=.016)
    for x in (-.46,0,.46):
        box('Rack/Backplane','Recess',(x,2.71,2.858),(.32,.25,.18),.020)
        if LOD<2:
            for n in range(5):box('Rack/Backplane','Titanium',(x,2.64+n*.035,2.756),(.25,.012,.028),.003)
    for side in (-1,1):
        group='Rack/CoolingLoop'
        for y in (.46,2.03):
            tube(group,'CoolantBlue',[(side*.92,y,2.94),(side*.92,y,2.80),(side*.90,y,2.75)],.034)
            tube(group,'MachinedAlloy',[(side*.9,y,2.77),(side*.9,y,2.71)],.052,8)
            tube(group,'Elastomer',[(side*.90,y,2.708),(side*.90,y,2.67)],.037,10)
        tube(group,'CoolantBlue',[(side*.9,.46,2.67),(side*.9,1.80,2.67),(side*.88,2.03,2.67)],.026)
        box(group,'Slate',(side*.90,2.0,2.63),(.12,.18,.09),.015)
    root=bpy.data.objects.new(f'BatteryBay_LOD{level}',None);bpy.context.scene.collection.objects.link(root)
    rack=bpy.data.objects.new('Rack',None);bpy.context.scene.collection.objects.link(rack);rack.parent=root
    for group,objects in GROUPS.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        obj=objects[0]
        obj.name=group.rsplit('/',1)[-1];obj.parent=rack if group.startswith('Rack/') else root
        # Each group's transform is identity; coordinates remain in asset space.
        world=obj.matrix_world.copy();obj.matrix_world=Matrix.Identity(4)
        obj.data.transform(world);obj.data.update()
        mod=obj.modifiers.new('Export triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
    return root


def descendants(root):
    return [root]+list(root.children_recursive)


def summarize(root):
    vertices=[];objects=[]
    for obj in root.children_recursive:
        if obj.type!='MESH':continue
        coords=[obj.matrix_world@v.co for v in obj.data.vertices]
        # Convert evaluated Blender vertices back to the documented Unity axes.
        canonical=[(p.x,p.z,-p.y) for p in coords];vertices.extend(canonical)
        objects.append({'name':obj.name,'parent':obj.parent.name,'vertices':len(obj.data.vertices),
                        'triangles':len(obj.data.polygons),'materials':[m.name for m in obj.data.materials]})
    data=np.asarray(vertices);lo=data.min(0);hi=data.max(0)
    if not np.isfinite(data).all():raise RuntimeError('Non-finite authored mesh')
    if lo[0]<-1.176 or hi[0]>1.176 or lo[1]<-.001 or hi[1]>3.301 or lo[2]<-.081 or hi[2]>3.201:
        raise RuntimeError('Asset outside agreed envelope: '+str((lo,hi)))
    triangles=sum(o['triangles'] for o in objects)
    if triangles>35000:raise RuntimeError('LOD triangle budget exceeded: '+str(triangles))
    return {'name':root.name,'boundsMin':lo.tolist(),'boundsMax':hi.tolist(),'size':(hi-lo).tolist(),
            'triangles':triangles,'vertices':len(vertices),'meshObjects':len(objects),'groups':objects}


def export(root,level):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in descendants(root):obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(OUT/f'BatteryBay_LOD{level}.fbx'),use_selection=True,
        object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',
        use_custom_props=False,use_mesh_modifiers=False)


def unity_metadata():
    folder_body='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    for directory in (OUT,OUT/'Textures',OUT/'Materials'):write_meta(directory,folder_body)
    texture_guids={}
    for file in (OUT/'Textures').glob('*.png'):
        template=ROOT/'Assets/SpaceMiner/Art/HallExperiment/Textures'/('MicroNormal.png.meta' if 'Normal' in file.name else 'PaintedAlloy_Albedo.png.meta')
        body=template.read_text().split('TextureImporter:',1)[1]
        body=body.replace('sRGBTexture: 1','sRGBTexture: '+('1' if 'Albedo' in file.name else '0'))
        body=body.replace('textureCompression: 2','textureCompression: 1')
        texture_guids[file.name]=write_meta(file,'TextureImporter:'+body)
    mat_guids={}
    for key in ('Surface','Display','Status'):
        source=ROOT/'Assets/SpaceMiner/Art/HallExperiment/Materials'/('Paint_Navy.mat' if key=='Surface' else 'Screen.mat')
        content=source.read_text().replace('m_Name: '+('Paint_Navy' if key=='Surface' else 'Screen'),'m_Name: BatteryBay_'+key)
        content=content.replace('m_EnableInstancingVariants: 0','m_EnableInstancingVariants: 1')
        if key=='Surface':
            content=content.replace('15a6d5d60f2b49040b1f1da07fb456a8',texture_guids['BatteryBay_Normal.png'])
            content=content.replace('3456279997014f5086ca8cb4d9565fd9',texture_guids['BatteryBay_Albedo.png'])
            content=content.replace('a08e40cde49020245864febadf79a802',texture_guids['BatteryBay_MetalSmooth.png'])
            content=content.replace('_BumpScale: 0.22','_BumpScale: 0.32')
            content=content.replace('_Color: {r: 0.12, g: 0.25, b: 0.32, a: 1}','_Color: {r: 1, g: 1, b: 1, a: 1}')
        else:
            color='0.012, g: 0.033, b: 0.045' if key=='Display' else '0.07, g: 0.11, b: 0.12'
            content=content.replace('_Color: {r: 0.018, g: 0.067, b: 0.09, a: 1}','_Color: {r: '+color+', a: 1}')
        path=OUT/'Materials'/f'BatteryBay_{key}.mat';path.write_text(content,encoding='utf-8')
        mat_guids[key]=write_meta(path,'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    for file in OUT.glob('*.fbx'):
        content=(ROOT/'Assets/SpaceMiner/Art/HallExperiment/StationHall-v01.fbx.meta').read_text().split('ModelImporter:',1)[1]
        mapping='\n'
        for key in ('Surface','Display','Status'):
            mapping+='  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: BatteryBay_'+key+'\n    second: {fileID: 2100000, guid: '+mat_guids[key]+', type: 2}\n'
        content=content.replace('  externalObjects: {}\n','  externalObjects:'+mapping,1)
        content=content.replace('preserveHierarchy: 0','preserveHierarchy: 1')
        write_meta(file,'ModelImporter:'+content)


def area(name,point,power,color,size,target):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);bpy.context.scene.collection.objects.link(obj);obj.location=B(point)
    obj.rotation_euler=(B(target)-obj.location).to_track_quat('-Z','Y').to_euler()


def render_views(root):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=40
    scene.cycles.use_denoising=True;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
    scene.world.color=(.08,.10,.13)
    world=scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.17,.22,1)
    world.node_tree.nodes['Background'].inputs[1].default_value=.35
    camera_data=bpy.data.cameras.new('Asset inspection camera');camera=bpy.data.objects.new('Asset inspection camera',camera_data)
    scene.collection.objects.link(camera);scene.camera=camera;camera_data.clip_start=.02;camera_data.clip_end=100
    area('Soft key',(-3.5,5,-3.5),1100,(.77,.87,1),4,(0,1.4,1.6))
    area('Warm fill',(3.5,3.4,-2.5),900,(1,.79,.57),3,(0,1.4,1.4))
    area('Rear rim',(0,5,4.2),1000,(.60,.78,1),3,(0,1.5,1.5))
    area('Integral service diffuser',(0,2.87,1.0),55,(.72,.88,1),1.1,(0,1.1,2))
    floor_mat=bpy.data.materials.new('Preview stage only');floor_mat.diffuse_color=(.055,.073,.088,1)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.007));floor=bpy.context.object;floor.name='PREVIEW stage, not exported'
    floor.data.materials.append(floor_mat)
    scene.render.resolution_x=1600;scene.render.resolution_y=1400
    def shoot(name,eye,target,lens):
        camera.location=B(eye);camera.rotation_euler=(B(target)-camera.location).to_track_quat('-Z','Y').to_euler()
        camera_data.lens=lens;scene.render.filepath=str(PREVIEW/name);bpy.ops.render.render(write_still=True)
    shoot('01-installed-three-quarter.png',(2.7,2.65,-5.0),(0,1.45,1.50),48)
    shoot('02-front-service-view.png',(.48,1.75,-2.6),(0,1.55,1.70),40)
    for obj in root.children_recursive:
        if obj.parent and (obj.parent.name=='Rack' or obj.name=='Rack'):obj.hide_render=True
    shoot('03-empty-frame-housing.png',(3.6,2.6,-3.6),(0,1.50,1.6),43)
    for obj in root.children_recursive:obj.hide_render=False
    shoot('04-cassette-detail.png',(.50,1.47,.35),(-.15,1.15,1.80),45)


def main():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for material in list(bpy.data.materials):bpy.data.materials.remove(material,do_unlink=True)
    for image in list(bpy.data.images):
        if image.name not in ('Render Result','Viewer Node'):bpy.data.images.remove(image,do_unlink=True)
    bpy.context.preferences.filepaths.save_version=0
    scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    make_atlas();reports=[]
    for level in range(3):
        root=build(level);ALL_ROOTS.append(root);reports.append(summarize(root));export(root,level)
        for obj in root.children_recursive:
            obj['semantic_name']=obj.name;obj.name=f'SourceLOD{level}_{obj.name}'
        if level:
            for obj in descendants(root):obj.hide_render=True;obj.hide_viewport=True
    for obj in ALL_ROOTS[0].children_recursive:obj.name=obj['semantic_name']
    report={'generator':'tools/CreateBatteryBay.py','blender':bpy.app.version_string,'seed':SEED,
            'coordinateSystem':'Unity local +X width +Y up +Z depth, front -Z, front-centre floor pivot',
            'canonicalToBlender':'(x,-z,y), FBX -Z forward / Y up','lods':reports,'materials':3,'atlasResolution':[N,N],
            'textureChannels':{'Albedo':'sRGB RGB, alpha1','Normal':'tangent +Y normal, linear',
                               'MetalSmooth':'linear red=metal, alpha=smoothness (Unity Standard)',
                               'MetalRough':'linear red=metal, green=roughness, blue=1'},
            'runtimeGroups':{'permanent':['Frame','Housing'],'installed':'Rack',
                             'blankDisplay':'ActiveDisplay','neutralLenses':'StatusLenses'},
            'displayPlane':{'centre':[0,3.015,-.026],'width':1.24,'height':.160,'normal':[0,0,-1]},
            'cassetteHandlesFrontZ':1.114,'frontServiceClearanceM':1.10,
            'noCapacityOrChargeText':True,'noColliders':True,'previewFixturesNotExported':True,
            'notes':'Authored mesh/PBR asset only. No cell chemistry, kWh, active/off state, bank logic or Unity runtime acceptance.'}
    for file in sorted(OUT.rglob('*')):
        if file.is_file() and file.suffix!='.meta':report.setdefault('sha256',{})[str(file.relative_to(ROOT))]=hashlib.sha256(file.read_bytes()).hexdigest()
    (ART/'model-report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    # Source has editable mesh groups and packed textures; lights/stage below are previews only.
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ART/'BatteryBay.blend'))
    unity_metadata()
    if '--skip-render' not in sys.argv:render_views(ALL_ROOTS[0])
    print('BATTERY BAY GENERATED',json.dumps(report),flush=True)


if __name__=='__main__':
    if '--render-only' in sys.argv:
        bpy.ops.wm.open_mainfile(filepath=str(ART/'BatteryBay.blend'))
        render_views(bpy.data.objects['BatteryBay_LOD0'])
    else:main()
