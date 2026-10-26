"""Author the approved all-touch station console as editable Blender/PBR assets.

Run existing portable Blender --background --python-exit-code 1 --python
tools/CreateTouchConsole.py [-- --skip-render]. No network/add-ons required.
The concept drawing is a shape/material reference, never an exported texture.
Canonical Unity coordinates: +X width, +Y up, front -Z, floor/centre pivot.
Native mesh geometry is (x,-z,y); FBX export is -Z forward / +Y up.
"""
from pathlib import Path
import sys, math, hashlib, json, re, uuid
import bpy
import numpy as np
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/SpaceMiner/Resources/TouchConsole'
ART=ROOT/'Design/Models/TouchConsole'
RENDERS=ART/'Renders'
for directory in (OUT,OUT/'Textures',OUT/'Materials',ART,RENDERS):directory.mkdir(parents=True,exist_ok=True)
SEED=261026; N=2048; TILE=N//4; TILT=math.radians(12)
RIGHT=Vector((1,0,0)); UP=Vector((0,math.sin(TILT),math.cos(TILT))); NORMAL=Vector((0,math.cos(TILT),-math.sin(TILT)))
SCREEN=Vector((0,1.002,.01)); SCREEN_SIZE=(1.23,.79)
SWATCHES=[
 ('Graphite',(.036,.047,.055),.30,.58),
 ('Slate',(.075,.093,.102),.36,.54),
 ('Alloy',(.42,.445,.45),.86,.34),
 ('SideAlloy',(.32,.34,.342),.83,.40),
 ('WornEdge',(.46,.48,.475),.88,.32),
 ('Recess',(.008,.014,.018),.14,.72),
 ('Rubber',(.011,.017,.019),.02,.84),
 ('Vent',(.018,.025,.028),.67,.49),
 ('Socket',(.027,.034,.035),.70,.40),
 ('Bolt',(.26,.28,.28),.90,.31),
 ('FootPaint',(.028,.035,.038),.43,.62),
 ('RearPanel',(.045,.058,.065),.32,.58),
 ('Hinge',(.13,.16,.165),.75,.40),
 ('ScreenBezel',(.012,.024,.029),.61,.26),
 ('PlaqueBorder',(.27,.30,.305),.84,.31),
 ('Blank',(.015,.020,.023),.30,.50),
]
TAGS={s[0]:i for i,s in enumerate(SWATCHES)}
MATS={}; GROUPS={}; LOD=0; PART=0; ROOTS=[]

def B(p):return Vector((p[0],-p[2],p[1]))

def meta(path,body):
    mp=Path(str(path)+'.meta')
    found=re.search(r'^guid: ([a-f0-9]+)',mp.read_text(encoding='utf-8'),re.M) if mp.exists() else None
    guid=found.group(1) if found else uuid.uuid5(uuid.NAMESPACE_URL,'SpaceMiner/TouchConsole/'+path.relative_to(ROOT).as_posix()).hex
    mp.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+body,encoding='utf-8');return guid

def image_array(name,pixels,srgb):
    im=bpy.data.images.new(name,width=N,height=N,alpha=True)
    im.colorspace_settings.name='sRGB' if srgb else 'Non-Color'
    im.pixels.foreach_set(np.asarray(pixels,dtype=np.float32).ravel());im.filepath_raw=str(OUT/'Textures'/name)
    im.file_format='PNG';im.save();return im

def materials():
    rng=np.random.default_rng(SEED)
    albedo=np.ones((N,N,4),np.float32);metal=albedo.copy();roughmap=albedo.copy();normal=albedo.copy()
    yy,xx=np.mgrid[0:TILE,0:TILE].astype(np.float32);u,v=xx/(TILE-1),yy/(TILE-1)
    edge=np.minimum.reduce((u,1-u,v,1-v))
    for i,(name,color,m,r) in enumerate(SWATCHES):
        noise=rng.normal(0,1,(TILE,TILE)).astype(np.float32)
        brushed=np.sin(xx*2.2+np.sin(yy*.035)*.7)*.021+noise*.011
        mottled=.015*np.sin(xx*.035)*np.sin(yy*.023)+.008*np.cos(xx*.011+yy*.014)
        wear=np.clip((.010-edge)/.010,0,1)*(.035+.055*(noise>.2))
        scratches=np.zeros((TILE,TILE),np.float32)
        for k in range(48):
            px,py=rng.integers(3,TILE-4,2);length=int(rng.integers(2,21))
            scratches[py:py+1,px:min(TILE-2,px+length)]=rng.uniform(.012,.036)
        rgb=np.array(color)[None,None,:]*(1+brushed+mottled)[:,:,None]
        if name not in ('Rubber','Recess','Socket','Blank','ScreenBezel'):rgb+=(wear+scratches)[:,:,None]
        reg=np.s_[(i//4)*TILE:(i//4+1)*TILE,(i%4)*TILE:(i%4+1)*TILE]
        albedo[reg][:,:,:3]=np.clip(rgb,0,1)
        smooth=np.clip(1-r+brushed*.38-wear*.24,.08,.92)
        metal[reg][:,:,:3]=m;metal[reg][:,:,3]=smooth
        roughmap[reg][:,:,0]=m;roughmap[reg][:,:,1]=1-smooth;roughmap[reg][:,:,2]=1
        nx=noise*.004+np.sin(xx*2.2)*.006;ny=np.roll(noise,1,axis=0)*.004
        normal[reg][:,:,0]=.5+nx;normal[reg][:,:,1]=.5+ny
        normal[reg][:,:,2]=np.sqrt(np.maximum(0,1-(nx*2)**2-(ny*2)**2))*.5+.5
    imgs={
      'albedo':image_array('TouchConsole_Albedo.png',albedo,True),
      'normal':image_array('TouchConsole_Normal.png',normal,False),
      'metal':image_array('TouchConsole_MetalSmooth.png',metal,False),
      'rough':image_array('TouchConsole_MetalRough.png',roughmap,False)}
    mat=bpy.data.materials.new('TouchConsole_Surface');mat.use_nodes=True
    ns=mat.node_tree.nodes;links=mat.node_tree.links;bs=ns.get('Principled BSDF')
    tex=ns.new('ShaderNodeTexImage');tex.image=imgs['albedo'];links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    tex=ns.new('ShaderNodeTexImage');tex.image=imgs['normal'];n=ns.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.26
    links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs[0],bs.inputs['Normal'])
    tex=ns.new('ShaderNodeTexImage');tex.image=imgs['metal'];sep=ns.new('ShaderNodeSeparateColor');links.new(tex.outputs['Color'],sep.inputs[0])
    links.new(sep.outputs['Red'],bs.inputs['Metallic']);inv=ns.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
    links.new(tex.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness']);MATS['surface']=mat
    for key,c,metallic,r in [('Display',(.006,.021,.027),.18,.20),('Plaque',(.012,.018,.020),.34,.43)]:
        mat=bpy.data.materials.new('TouchConsole_'+key);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF')
        bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Metallic'].default_value=metallic;bs.inputs['Roughness'].default_value=r
        mat.diffuse_color=(*c,1);MATS[key]=mat

def finish(obj,group,tag,bevel=.006):
    global PART
    PART+=1;obj.name=f'{group}__{tag}__{PART:04}';obj.data.materials.append(MATS.get(tag,MATS['surface']))
    bpy.context.view_layer.objects.active=obj
    if bevel and LOD<2:
        b=obj.modifiers.new('Fabricated softened edge','BEVEL');b.width=bevel;b.segments=(3 if bevel>=.012 else 2) if LOD==0 else 1
        bpy.ops.object.modifier_apply(modifier=b.name)
    # Plane project each manufactured face into an inset shared-atlas tile.
    tile=TAGS.get(tag,0);row,col=divmod(tile,4)
    uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='SurfaceAtlas')
    points=np.array([tuple(v.co) for v in obj.data.vertices]);lo,hi=points.min(0),points.max(0);span=np.maximum(hi-lo,1e-7)
    for face in obj.data.polygons:
        face.use_smooth=True;axis=max(range(3),key=lambda i:abs(face.normal[i]));a,b=[i for i in range(3) if i!=axis]
        for li in face.loop_indices:
            p=points[obj.data.loops[li].vertex_index];u,v=(p[a]-lo[a])/span[a],(p[b]-lo[b])/span[b]
            uv.data[li].uv=((col+.006+u*.988)/4,(row+.006+v*.988)/4)
    try:
        n=obj.modifiers.new('Weighted machined normals','WEIGHTED_NORMAL');n.weight=65;n.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=n.name)
    except RuntimeError:pass
    GROUPS.setdefault(group,[]).append(obj);return obj

def box(group,tag,p,size,bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=B(p));obj=bpy.context.object
    obj.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(obj,group,tag,bevel)

def slope(group,tag,p,size,bevel=.006):
    # size=(width, normal thickness, distance along slope).
    bpy.ops.mesh.primitive_cube_add(size=1,location=B(p));obj=bpy.context.object
    obj.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.rotation_euler.x=-TILT;bpy.ops.object.transform_apply(location=False,rotation=True,scale=False)
    return finish(obj,group,tag,bevel)

def extrude_profile(group,tag,x,width,zy,bevel=.007):
    verts=[]
    for a in (x-width/2,x+width/2):verts.extend([B((a,y,z)) for z,y in zy])
    n=len(zy);faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    for i in range(n):j=(i+1)%n;faces.append((i,j,j+n,i+n))
    mesh=bpy.data.meshes.new(group);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(group,mesh);bpy.context.scene.collection.objects.link(obj)
    # Ensure all authored volumes have outward, coherent normals before bevels.
    import bmesh
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
    return finish(obj,group,tag,bevel)

def bolt(p,axis='front',radius=.008):
    if LOD==2:return
    direction={'front':(0,0,-1),'side':(1,0,0),'back':(0,0,1),'slope':tuple(NORMAL)}[axis]
    direction=Vector(direction);loc=B(p)
    bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=radius,depth=.004,location=loc)
    obj=bpy.context.object;obj.rotation_euler=B(direction).to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);finish(obj,'Fasteners','Bolt',.0006 if LOD==0 else 0)
    if LOD==0:
        bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=radius*.37,depth=.0008,location=B(Vector(p)+direction*.0023))
        obj=bpy.context.object;obj.rotation_euler=B(direction).to_track_quat('Z','Y').to_euler()
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);finish(obj,'Fasteners','Socket',0)

def build(level):
    global GROUPS,LOD,PART
    LOD=level;GROUPS={};PART=0
    # Continuous dark pressure-compatible cabinet, visibly recessed under the front lip.
    # Roof stays below the recessed glass seat; the live surface must remain exposed.
    body=[(-.366,.105),(.424,.105),(.424,1.026),(.365,1.039),(-.366,.884)]
    extrude_profile('Chassis','Graphite',0,1.302,body,.016)
    box('Chassis','FootPaint',(0,.111,.015),(1.30,.082,.86),.016)
    box('Chassis','Rubber',(0,.034,.018),(1.20,.036,.79),.009)
    # Closed load-bearing front with a lower kick recess; never an open frame.
    box('ServicePanels','Recess',(0,.480,-.373),(1.23,.655,.022),.010)
    box('ServicePanels','Graphite',(0,.492,-.390),(1.174,.594,.024),.012)
    box('ServicePanels','Slate',(0,.181,-.378),(1.212,.115,.030),.010)
    box('ServicePanels','Recess',(0,.238,-.407),(1.13,.006,.004),0)
    # Small notch-like kick protector follows the concept's lower angled motif.
    kick=[(-.400,.120),(-.400,.158),(-.381,.206),(-.345,.206),(-.345,.120)]
    extrude_profile('ServicePanels','FootPaint',0,.73,kick,.007)
    for side in (-1,1):
        # Full-depth boot and front/rear vertical metal structural cheeks.
        x=side*.663
        box('AlloyFrame','SideAlloy',(x,.043,.0),(.174,.086,1.00),.020)
        box('Chassis','FootPaint',(x,.015,.0),(.157,.030,.965),.009)
        box('AlloyFrame','Alloy',(side*.686,.468,-.421),(.128,.764,.148),.016)
        box('AlloyFrame','SideAlloy',(side*.686,.562,.412),(.128,.952,.148),.015)
        # Rear shoulder cap reaches the documented overall 1.14m envelope.
        box('AlloyFrame','Alloy',(side*.686,1.103,.421),(.128,.074,.148),.011)
        # Silver sloped arms: continuous along screen side, with dark service seams.
        rail=[(-.493,.847),(-.493,.932),(.350,1.122),(.456,1.125),(.486,1.082),(.486,1.036),(.367,1.015),(-.345,.854)]
        extrude_profile('AlloyFrame','Alloy',side*.681,.138,rail,.012)
        # Graphite side panel set in from metal rails. Flat industrial plates, no greeble clutter.
        panel=[(-.338,.154),(.333,.154),(.333,.972),(-.338,.823)]
        extrude_profile('ServicePanels','Slate',side*.655,.024,panel,.008)
        inner=[(-.308,.201),(.302,.201),(.302,.942),(-.308,.805)]
        extrude_profile('ServicePanels','Graphite',side*.671,.010,inner,.004)
        # Perimeter service strips, real recessed louvre at foot, and a vertical flush access handle.
        if level<2:
            box('ServicePanels','Recess',(side*.681,.253,-.255),(.004,.116,.145),.003)
            for j in range(5 if level==0 else 3):
                box('ServicePanels','Vent',(side*.684,.211+j*.020,-.255),(.004,.008,.128),.001)
            box('ServicePanels','Recess',(side*.683,.324,.275),(.004,.190,.025),.002)
            box('ServicePanels','Hinge',(side*.687,.324,.275),(.004,.142,.010),.002)
            # Side rail joints reveal separate assembled pieces without physical controls.
            for z in (-.327,.343):
                y=.992+z*math.tan(TILT)
                slope('ServicePanels','Recess',(side*.681,y+.016,z),(.137,.0018,.007),0)
            for z,y in [(-.305,.22),(-.305,.76),(.296,.25),(.296,.90)]:bolt((side*.689,y,z),'side')
            for z in (-.438,-.305,.340):
                y=1.002+z*math.tan(TILT)+.029;bolt((side*.681,y,z),'slope',.008)
            for y in (.163,.730):bolt((side*.687,y,-.497),'front',.007)
            # Chunky assembled foot end plates are industrial mounts, not buttons.
            box('AlloyFrame','WornEdge',(side*.663,.078,-.410),(.165,.018,.15),.006)
    # Full-width upper front fascia, glass seat, rear cross-member.
    box('AlloyFrame','Alloy',(0,.847,-.455),(1.252,.126,.066),.012)
    box('ServicePanels','Recess',(0,.841,-.490),(1.11,.098,.002),.006)
    box('FrontPlaque','Plaque',(0,.841,-.493),(1.07,.088,.003),.005)
    for x in (-.515,.515):
        for y in (.808,.874):bolt((x,y,-.496),'front',.004)
    # Numeric serial label blank provided separately; text comes from actual runtime function.
    box('ServicePanels','PlaqueBorder',(.586,.841,-.492),(.070,.066,.003),.003)
    box('FrontPlaque','Plaque',(.586,.841,-.495),(.063,.056,.002),.002)
    # Top black seat, narrow silver sealing edge and blank dark touch glass.
    slope('Chassis','ScreenBezel',SCREEN-NORMAL*.014,(1.359,.031,.907),.009)
    slope('AlloyFrame','SideAlloy',SCREEN-NORMAL*.006,(1.323,.015,.870),.009)
    slope('ServicePanels','Rubber',SCREEN+NORMAL*.003,(1.294,.009,.839),.008)
    slope('TouchGlass','Display',SCREEN+NORMAL*.007,(SCREEN_SIZE[0],.006,SCREEN_SIZE[1]),.006)
    # Thin understated rear trim, no large protruding hood.
    slope('AlloyFrame','Alloy',(0,1.105,.455),(1.247,.027,.041),.005)
    # Closed rear service access panel, seams and connector covers, not exposed wires.
    box('ServicePanels','Recess',(0,.547,.428),(1.209,.819,.014),.009)
    box('ServicePanels','RearPanel',(0,.546,.439),(1.165,.768,.014),.011)
    if level<2:
        box('ServicePanels','Recess',(0,.432,.448),(1.098,.008,.002),0)
        box('ServicePanels','Recess',(.377,.243,.449),(.208,.097,.004),.004)
        box('ServicePanels','Slate',(.377,.243,.453),(.182,.074,.006),.004)
        for x in (-.54,.54):
            for y in (.194,.891):bolt((x,y,.449),'back')
        for x in (-.403,-.287,-.171,-.055,.061,.177,.293):
            box('ServicePanels','Recess',(x,.293,.449),(.057,.116,.004),.002)
            if level==0:
                for j in range(6):box('ServicePanels','Vent',(x,.250+j*.017,.453),(.054,.008,.005),.001)
    root=bpy.data.objects.new('TouchConsole_LOD'+str(level),None);bpy.context.scene.collection.objects.link(root)
    for group,objects in GROUPS.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        obj=objects[0];obj.name=group;obj.parent=root;world=obj.matrix_world.copy();obj.matrix_world=Matrix.Identity(4)
        obj.data.transform(world);obj.data.update()
        tri=obj.modifiers.new('Export triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
        # Triangles retain weighted corner normals and authored UVs.
    return root

def descendants(root):return [root]+list(root.children_recursive)

def summarize(root):
    points=[];groups=[]
    for obj in root.children_recursive:
        if obj.type!='MESH':continue
        for vertex in obj.data.vertices:
            p=obj.matrix_world@vertex.co;points.append((p.x,p.z,-p.y))
        groups.append({'name':obj.name,'vertices':len(obj.data.vertices),'triangles':sum(len(f.vertices)-2 for f in obj.data.polygons),
                       'materials':[m.name for m in obj.data.materials]})
    points=np.asarray(points);lo,hi=points.min(0),points.max(0)
    if not np.isfinite(points).all():raise RuntimeError('Nonfinite asset')
    if np.any(lo<np.array([-.751,-.001,-.501])) or np.any(hi>np.array([.751,1.141,.501])):raise RuntimeError('Envelope '+str((lo,hi)))
    return {'name':root.name,'boundsMin':lo.tolist(),'boundsMax':hi.tolist(),'size':(hi-lo).tolist(),
            'triangles':sum(g['triangles'] for g in groups),'vertices':len(points),'meshObjects':len(groups),'groups':groups}

def export(root,level):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in descendants(root):obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(OUT/f'TouchConsole_LOD{level}.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
       apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,
       add_leaf_bones=False,path_mode='STRIP',use_custom_props=False,use_mesh_modifiers=False)

def unity_metadata():
    folders='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    for d in (OUT,OUT/'Textures',OUT/'Materials'):meta(d,folders)
    texture_guids={}
    for f in (OUT/'Textures').glob('*.png'):
        template=ROOT/'Assets/SpaceMiner/Art/HallExperiment/Textures'/('MicroNormal.png.meta' if 'Normal' in f.name else 'PaintedAlloy_Albedo.png.meta')
        body=template.read_text(encoding='utf-8').split('TextureImporter:',1)[1]
        body=body.replace('sRGBTexture: 1','sRGBTexture: '+('1' if 'Albedo' in f.name else '0')).replace('textureCompression: 2','textureCompression: 1')
        texture_guids[f.name]=meta(f,'TextureImporter:'+body)
    mat_guids={}
    for key in ('Surface','Display','Plaque'):
        source=ROOT/'Assets/SpaceMiner/Art/HallExperiment/Materials'/('Paint_Navy.mat' if key=='Surface' else 'Screen.mat')
        text=source.read_text(encoding='utf-8').replace('m_Name: '+('Paint_Navy' if key=='Surface' else 'Screen'),'m_Name: TouchConsole_'+key)
        text=text.replace('m_EnableInstancingVariants: 0','m_EnableInstancingVariants: 1')
        if key=='Surface':
            for old,new in [('15a6d5d60f2b49040b1f1da07fb456a8','TouchConsole_Normal.png'),('3456279997014f5086ca8cb4d9565fd9','TouchConsole_Albedo.png'),('a08e40cde49020245864febadf79a802','TouchConsole_MetalSmooth.png')]:text=text.replace(old,texture_guids[new])
            text=text.replace('_BumpScale: 0.22','_BumpScale: 0.26').replace('_Color: {r: 0.12, g: 0.25, b: 0.32, a: 1}','_Color: {r: 1, g: 1, b: 1, a: 1}')
        else:
            c='0.006, g: 0.021, b: 0.027' if key=='Display' else '0.012, g: 0.018, b: 0.020'
            text=text.replace('_Color: {r: 0.018, g: 0.067, b: 0.09, a: 1}','_Color: {r: '+c+', a: 1}')
            text=text.replace('_Glossiness: 0.7','_Glossiness: '+('0.80' if key=='Display' else '0.57'))
        f=OUT/'Materials'/f'TouchConsole_{key}.mat';f.write_text(text,encoding='utf-8')
        mat_guids[key]=meta(f,'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    for f in OUT.glob('*.fbx'):
        body=(ROOT/'Assets/SpaceMiner/Art/HallExperiment/StationHall-v01.fbx.meta').read_text(encoding='utf-8').split('ModelImporter:',1)[1]
        mapping='\n'
        for key in ('Surface','Display','Plaque'):
            mapping+='  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: TouchConsole_'+key+'\n    second: {fileID: 2100000, guid: '+mat_guids[key]+', type: 2}\n'
        body=body.replace('  externalObjects: {}\n','  externalObjects:'+mapping,1).replace('preserveHierarchy: 0','preserveHierarchy: 1')
        meta(f,'ModelImporter:'+body)

def roundtrip():
    reports=[]
    for level in range(3):
        before=set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(OUT/f'TouchConsole_LOD{level}.fbx'),use_custom_normals=True)
        added=set(bpy.data.objects)-before;points=[];triangles=0;uvok=True
        for obj in added:
            if obj.type!='MESH':continue
            triangles+=sum(len(p.vertices)-2 for p in obj.data.polygons);uvok=uvok and bool(obj.data.uv_layers)
            for v in obj.data.vertices:
                p=obj.matrix_world@v.co;points.append((p.x,p.z,-p.y))
        arr=np.asarray(points);lo,hi=arr.min(0),arr.max(0)
        if not uvok or not np.isfinite(arr).all():raise RuntimeError('FBX roundtrip missing UV/nonfinite')
        if np.max(np.abs((hi-lo)-np.array([1.5,1.14,1.])))>.01:raise RuntimeError('FBX scale mismatch '+str(hi-lo))
        reports.append({'lod':level,'triangles':triangles,'boundsMin':lo.tolist(),'boundsMax':hi.tolist(),'uvLayersPresent':uvok,'finite':True})
        for obj in added:bpy.data.objects.remove(obj,do_unlink=True)
    return reports

def emissive(name,c,strength=1):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    if strength>0:
        # Display pixels are self-lit: they must not receive studio/specular light.
        nodes=mat.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission')
        em.inputs['Color'].default_value=(*c,1);em.inputs['Strength'].default_value=strength
        mat.node_tree.links.new(em.outputs['Emission'],out.inputs['Surface'])
    else:
        bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Roughness'].default_value=.52
    return mat

def preview_ui():
    # Explicit demonstration fixture: authored text/shapes only, never the generated concept.
    cyan=emissive('PREVIEW cyan',(.03,.48,.62),1.3);white=emissive('PREVIEW white',(.54,.69,.73),1.0)
    green=emissive('PREVIEW green',(.045,.55,.34),1.1);black=emissive('PREVIEW panel',(.003,.019,.028),.3)
    def plate(x,v,w,h,mat,offset=None):
        offset=offset if offset is not None else (.0111 if mat==black else .0112)
        p=SCREEN-RIGHT*x+UP*v+NORMAL*offset
        # Zero-thickness pixel plane. Layer spacing is 0.1mm and casts no shadows.
        coords=[B(p-RIGHT*w/2-UP*h/2),B(p+RIGHT*w/2-UP*h/2),B(p+RIGHT*w/2+UP*h/2),B(p-RIGHT*w/2+UP*h/2)]
        mesh=bpy.data.meshes.new('PREVIEW screen pixels');mesh.from_pydata(coords,[],[(3,2,1,0)]);mesh.update()
        o=bpy.data.objects.new('PREVIEW_UI_fixture_not_exported',mesh);bpy.context.scene.collection.objects.link(o)
        o.data.materials.append(mat);o.visible_shadow=False;return o
    def line(x,v,w,h,mat=cyan):return plate(x,v,w,h,mat)
    def txt(label,x,v,height=.029,mat=white):
        curve=bpy.data.curves.new('PREVIEW fixture text','FONT');curve.body=label;curve.size=height;curve.align_x='LEFT';curve.align_y='CENTER';curve.extrude=0
        o=bpy.data.objects.new('PREVIEW_UI_fixture_not_exported',curve);bpy.context.scene.collection.objects.link(o)
        o.location=B(SCREEN-RIGHT*x+UP*v+NORMAL*.0113);o.visible_shadow=False
        # Facing the authored -Z front: viewer's right is canonical -X.
        # This is a proper right-handed orthonormal text basis, not a reflection.
        o.rotation_euler=Matrix((B(-RIGHT),B(UP),B(NORMAL))).transposed().to_euler();o.data.materials.append(mat)
    plate(0,0,1.23,.79,black,.0110)
    txt('STROMVERSORGUNG',-.565,.333,.035,cyan);txt('P-05',.465,.333,.022,cyan)
    line(0,.293,1.13,.0015)
    for x,label,value in [(-.385,'ERZEUGUNG','1,65 kW'),(0,'VERBRAUCH','1,69 kW'),(.385,'AKKU','12 / 24 kWh')]:
        plate(x,.126,.359,.275,black)
        for dx in (-.1795,.1795):line(x+dx,.126,.0012,.275)
        line(x,.263,.359,.0012);line(x,-.011,.359,.0012)
        txt(label,x-.155,.228,.022,cyan);txt(value,x-.155,.169,.037)
        if x==-.385:
            for j in range(4):
                line(x-.128+j*.043,.073,.027,.075+(.012*j),cyan)
        elif x==0:
            for j in range(11):line(x-.145+j*.029,.051+math.sin(j*.5)*.017,.021,.003)
        else:
            line(x-.10,.069,.067,.095,green);line(x+.033,.069,.157,.016,cyan)
    txt('BATTERIE-BANKS',-.559,-.066,.024,cyan)
    for x,label,value,m in [(-.395,'01 AKTIV','50 %',green),(-.045,'02 AUS','UNBEKANNT',cyan)]:
        plate(x,-.223,.33,.226,black);txt(label,x-.143,-.153,.023,m);txt(value,x-.143,-.211,.029)
        line(x-.064,-.284,.16,.012,m)
    txt('NETZ / QUELLEN',.185,-.066,.023,cyan)
    txt('Reaktor     1,64 kW',.185,-.135,.022)
    txt('Solar       0,01 kW',.185,-.182,.022)
    txt('4 MODULE AKTIV',.185,-.248,.021,cyan)
    txt('LIVE-DISPLAY / DEMO',.185,-.306,.017,cyan)
    # Actual runtime front label is blank in all FBXs; preview text is separate.
    curve=bpy.data.curves.new('PREVIEW plaque text','FONT');curve.body='STROMVERSORGUNG';curve.size=.048;curve.align_x='CENTER';curve.align_y='CENTER'
    o=bpy.data.objects.new('PREVIEW plaque label not exported',curve);bpy.context.scene.collection.objects.link(o)
    o.location=B((0,.841,-.4947));o.rotation_euler=(math.pi/2,0,math.pi);o.data.materials.append(white);o.visible_shadow=False

def area(name,p,power,c,size,target):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=c;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);bpy.context.scene.collection.objects.link(o);o.location=B(p)
    o.rotation_euler=(B(target)-o.location).to_track_quat('-Z','Y').to_euler()

def render(root):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
    scene.render.threads_mode='FIXED';scene.render.threads=8
    scene.render.resolution_percentage=100;scene.render.resolution_x=1600;scene.render.resolution_y=1280
    scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.14,.18,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.28
    area('Studio large soft key',(-2.3,3.5,-2.3),420,(.82,.91,1),2.7,(0,.6,0))
    area('Studio neutral warm fill',(2.8,2.7,-1.4),300,(1,.90,.77),2.1,(0,.6,0))
    area('Studio rear edge',(-.8,2.7,2.2),530,(.64,.79,1),2.4,(0,.7,0))
    d=bpy.data.cameras.new('Asset inspection camera');camera=bpy.data.objects.new('Asset inspection camera',d);scene.collection.objects.link(camera)
    scene.camera=camera;d.clip_start=.01;d.clip_end=100;d.lens=53
    stage=emissive('PREVIEW matte stage',(.026,.035,.044),0)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.003));bpy.context.object.name='PREVIEW stage not exported';bpy.context.object.data.materials.append(stage)
    def shoot(filename,eye,target,lens=53):
        camera.location=B(eye);camera.rotation_euler=(B(target)-camera.location).to_track_quat('-Z','Y').to_euler();d.lens=lens
        scene.render.filepath=str(RENDERS/filename);bpy.ops.render.render(write_still=True)
    if '--preview-only' not in sys.argv:
        shoot('01-authored-blank-three-quarter.png',(2.2,2.0,-2.75),(0,.58,0),53)
        shoot('02-authored-front.png',(0,1.56,-3.0),(0,.57,0),57)
        shoot('03-authored-side-rear.png',(-2.25,1.43,2.58),(0,.58,0),54)
    preview_ui();shoot('04-demo-screen-fixture-not-gameplay.png',(1.78,2.05,-2.38),(0,.67,0),56)
    shoot('05-demo-front-fixture-not-gameplay.png',(0,1.62,-3.08),(0,.60,0),59)
    d.type='ORTHO';d.ortho_scale=1.55
    shoot('06-demo-side-fixture-not-gameplay.png',(3,.57,0),(0,.57,0),55)
    d.type='PERSP';d.lens=56;camera.location=B((1.78,2.05,-2.38))
    camera.rotation_euler=(B((0,.67,0))-camera.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.wm.save_as_mainfile(filepath=str(ART/'TouchConsole-Preview.blend'),compress=True)

def main():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials):bpy.data.materials.remove(m,do_unlink=True)
    bpy.context.preferences.filepaths.save_version=0;scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    materials();reports=[]
    for level in range(3):
        root=build(level);ROOTS.append(root);reports.append(summarize(root));export(root,level)
        for o in root.children_recursive:o['semantic_name']=o.name;o.name=f'SourceLOD{level}_{o.name}'
        if level:
            for o in descendants(root):o.hide_render=True;o.hide_viewport=True
    for o in ROOTS[0].children_recursive:o.name=o['semantic_name']
    if not (reports[0]['triangles']>reports[1]['triangles']>reports[2]['triangles']):raise RuntimeError('Nondecreasing LOD')
    rt=roundtrip();unity_metadata()
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ART/'TouchConsole.blend'),compress=True)
    report={
      'generator':'tools/CreateTouchConsole.py','blender':bpy.app.version_string,'seed':SEED,
      'reference':'docs/Art/Station/Steuerpult-Touch-2026-10-26-v2.png',
      'coordinateSystem':'Unity local +X width, +Y up, front -Z, floor/centre pivot',
      'canonicalToBlender':'(x,-z,y), FBX -Z forward / +Y up','lods':reports,'roundtrip':rt,
      'atlasResolution':[N,N],'materials':['TouchConsole_Surface','TouchConsole_Display','TouchConsole_Plaque'],
      'runtimeGroups':{'permanent':['Chassis','AlloyFrame','ServicePanels','Fasteners'],'screen':'TouchGlass','frontLabel':'FrontPlaque'},
      'screen':{'centre':list(SCREEN+NORMAL*.011),'width':1.23,'height':.79,'right':list(RIGHT),'up':list(UP),'normal':list(NORMAL),
                'pitchDegrees':12,'glassCentre':list(SCREEN+NORMAL*.007),'halfNormalThickness':.003,'overlayClearanceAboveGlassM':.001},
      'frontPlaque':{'centre':[0,.841,-.498],'width':.98,'height':.075,'right':[1,0,0],'up':[0,1,0],'normal':[0,0,-1]},
      'serialPlaque':{'centre':[.586,.841,-.498],'width':.056,'height':.047,'normal':[0,0,-1]},
      'screenIsBlankForLiveUi':True,'frontPlaqueBlankForRuntimeText':True,'noPhysicalButtons':True,
      'noColliders':True,'previewsAreNotGameplay':True,'previewFixtureNotExported':True,
      'textureChannels':{'Albedo':'sRGB RGB','Normal':'linear tangent +Y','MetalSmooth':'linear red metal, alpha smoothness',
                         'MetalRough':'linear red metal, green roughness'},
      'notes':'Authored model/PBR only. Runtime screen must use live real function. No plan/save mutation or Unity build verification in generator.'}
    for f in [Path(__file__),ART/'TouchConsole.blend']+sorted(OUT.rglob('*')):
        if f.is_file():report.setdefault('sha256',{})[f.relative_to(ROOT).as_posix()]=hashlib.sha256(f.read_bytes()).hexdigest()
    (ART/'model-report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    (ART/'README.md').write_text('# Touch-Steuerpult\n\n'+
      'Native Blender/PBR-Umsetzung des freigegebenen Touch-Konzepts. Erzeugen mit dem vorhandenen portablen Blender und `tools/CreateTouchConsole.py`. '+
      'Native `.blend` bleibt außerhalb von Assets; Unity importiert drei FBX-LODs mit gemeinsamem 2048²-PBR-Atlas. '+
      'Vorderseite −Z, Bodenmittelpunkt; Schnittstellen und tatsächliche Meshmaße im `model-report.json`. '+
      'Glas und Frontplakette sind absichtlich blank für echte Runtime-Oberfläche und Beschriftung. '+
      'Renders/04 ist eine ausdrücklich getrennte Demo-UI-Fixture, kein Spielbild und keine exportierte Bildtextur. '+
      'Alle Oberflächen selbst prozedural verfasst, keine heruntergeladenen Modelle oder Konzeptbildtextur.\n',encoding='utf-8')
    if '--skip-render' not in sys.argv:
        render(ROOTS[0]);record_preview_hashes()
    print('TOUCH CONSOLE GENERATED '+json.dumps({'lodTriangles':[r['triangles'] for r in reports],'bounds':reports[0]['size'],'roundtrip':rt}),flush=True)

def record_preview_hashes():
    rp=ART/'model-report.json';report=json.loads(rp.read_text(encoding='utf-8'))
    for f in [Path(__file__),ART/'TouchConsole.blend',ART/'TouchConsole-Preview.blend']+sorted(RENDERS.glob('*.png')):
        if f.is_file():report.setdefault('sha256',{})[f.relative_to(ROOT).as_posix()]=hashlib.sha256(f.read_bytes()).hexdigest()
    report['nativeBlendCompressed']=True;report['previewBlendCompressed']=True
    report['previewTextIsGeometry']=True;report['previewValuesAreExplicitDemoFixture']=True
    rp.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')

if __name__=='__main__':
    if '--render-only' in sys.argv:
        bpy.ops.wm.open_mainfile(filepath=str(ART/'TouchConsole.blend'))
        MATS['surface']=bpy.data.materials['TouchConsole_Surface'];MATS['Display']=bpy.data.materials['TouchConsole_Display'];MATS['Plaque']=bpy.data.materials['TouchConsole_Plaque']
        bpy.context.preferences.filepaths.save_version=0
        bpy.ops.wm.save_as_mainfile(filepath=str(ART/'TouchConsole.blend'),compress=True)
        render(bpy.data.objects['TouchConsole_LOD0']);record_preview_hashes()
    else:main()
