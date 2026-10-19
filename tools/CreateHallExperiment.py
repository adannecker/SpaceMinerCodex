"""Author the isolated, metre-scale station material experiment in portable Blender.
Run: blender --background --python-exit-code 1 --python tools/CreateHallExperiment.py
"""
from pathlib import Path
import math, json, random
import bpy
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/SpaceMiner/Art/HallExperiment'
ART = ROOT / 'docs/Art/HallExperiment'
OUT.mkdir(parents=True, exist_ok=True)
ART.mkdir(parents=True, exist_ok=True)
random.seed(721)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
COLORS = {
    'Paint_Ivory': (.69,.73,.72), 'Paint_Navy': (.075,.16,.21),
    'Steel_Dark': (.055,.073,.083), 'Steel_Brush': (.34,.40,.43),
    'Rubber': (.018,.024,.029), 'Safety_Yellow': (.93,.52,.075),
    'Light_Warm': (1,.83,.58), 'Signal_Cyan': (.08,.67,.83),
    'Signal_Amber': (1,.28,.035), 'Screen': (.025,.10,.13),
    'Label': (.83,.88,.86), 'Glass': (.12,.28,.34),
    'Collision': (.3,.1,.1)
}
MATS = {}
for name, color in COLORS.items():
    m = bpy.data.materials.new(name); m.diffuse_color=(*color,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Metallic'].default_value=.75 if name.startswith('Steel') else .18
    bs.inputs['Roughness'].default_value=.38 if name.startswith('Steel') else .57
    if name in ('Light_Warm','Signal_Cyan','Signal_Amber'):
        bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=2
    if name.startswith('Paint'):
        tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Textures/PaintedAlloy_Albedo.png'),check_existing=True)
        mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(*color,1)
        m.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
    MATS[name]=m

counter=0
def finish(obj, group, mat, bevel=.012, parent=None):
    global counter
    counter+=1;obj.name=f'{group}__{mat}__{counter:04d}'
    obj.data.materials.append(MATS[mat])
    if parent:obj.parent=parent
    if obj.type=='MESH':
        # Metre-based projection; panel gaps hide texture boundaries.
        uv=obj.data.uv_layers.active or obj.data.uv_layers.new()
        offset=random.random()*3
        for poly in obj.data.polygons:
            axis=max(range(3),key=lambda i:abs(poly.normal[i]));axes=[i for i in range(3) if i!=axis]
            for li in poly.loop_indices:
                co=obj.data.vertices[obj.data.loops[li].vertex_index].co
                uv.data[li].uv=(co[axes[0]]*.6+offset,co[axes[1]]*.6+offset)
        if bevel:
            mod=obj.modifiers.new('Manufactured edge radius','BEVEL');mod.width=bevel;mod.segments=2
            mod=obj.modifiers.new('Weighted panel normals','WEIGHTED_NORMAL');mod.keep_sharp=True
    return obj

def box(group,mat,pos,size,bevel=.012,angle=0,parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.rotation_euler.z=math.radians(angle)
    return finish(o,group,mat,bevel,parent)

def polar(r,angle,z=0):
    a=math.radians(angle);return Vector((r*math.sin(a),r*math.cos(a)-38,z))

def arc(group,mat,ri,ro,a,b,z0,z1,bevel=.008):
    steps=max(1,math.ceil(abs(b-a)/2));verts=[]
    for i in range(steps+1):
        angle=a+(b-a)*i/steps
        for r,z in ((ri,z0),(ro,z0),(ro,z1),(ri,z1)):verts.append(polar(r,angle,z))
    faces=[(0,3,2,1)]
    for i in range(steps):
        j=i*4
        for k in range(4):faces.append((j+k,j+(k+1)%4,j+4+(k+1)%4,j+4+k))
    j=steps*4;faces.append((j,j+1,j+2,j+3))
    mesh=bpy.data.meshes.new(group);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(group,mesh);scene.collection.objects.link(o)
    return finish(o,group,mat,bevel)

def tube(group,mat,a,b,radius=.035):
    a,b=Vector(a),Vector(b);delta=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=radius,depth=delta.length,location=(a+b)/2)
    o=bpy.context.object;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return finish(o,group,mat,.006)

def text(label,pos,size=.18,angle=0,mat='Label',parent=None):
    # Text local X follows the wall tangent, local Y points up, normal faces inward.
    curve=bpy.data.curves.new('Industrial lettering','FONT');curve.body=label;curve.align_x='CENTER';curve.size=size;curve.extrude=.0008
    o=bpy.data.objects.new('Lettering',curve);scene.collection.objects.link(o)
    a=math.radians(angle);right=Vector((math.cos(a),-math.sin(a),0));up=Vector((0,0,1));normal=right.cross(up)
    o.matrix_world=Matrix.Translation(Vector(pos))@Matrix((right,up,normal)).transposed().to_4x4()
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o.select_set(False)
    return finish(o,'Lettering',mat,0,parent)

def bolt(pos,axis=(0,-1,0),radius=.028,parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=radius,depth=.018,location=pos)
    o=bpy.context.object;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    return finish(o,'Fasteners','Steel_Brush',.002,parent)

# Continuous load-bearing deck plus individually replaceable floor cassettes.
arc('COL_Floor','Collision',37.5,59,-13.7,13.7,-.35,-.015,0)
arc('DeckBacking','Steel_Dark',38,54,-13.5,13.5,-.10,-.02)
for row in range(8):
    for col in range(14):
        a=-13.5+col*27/14;b=a+27/14
        arc('DeckCassettes','Steel_Dark',38+row*2+.022,40+row*2-.022,a+.035,b-.035,-.02,.005)
        # Small machined corner details break up the floor at a human scale.
        if row%2==0 and col%2==0:
            p=polar(38+row*2+.20,a+.25,.025)
            box('DeckLocks','Steel_Brush',p,(.09,.12,.014),.008,angle=-a)
for side in (-1,1):
    arc('PedestrianLine','Safety_Yellow',38.15,53.5,side*10-.055,side*10+.055,.009,.013,0)
    for r in (39,42,45,48,51):
        p=polar(r,side*9.65,.016);box('LaneDashes','Label',p,(.045,.7,.007),0,angle=-side*9.65)
for r in (41.1,47.1,52.0):
    arc('BayStopLine','Safety_Yellow',r,r+.085,-8.5,8.5,.011,.016,0)

# Radial side walls, with layered lower armour, removable upper cassettes and ribs.
for side in (-1,1):
    angle=side*13.5
    box('COL_Side','Collision',polar(46,angle,2.75),(.25,16.3,5.5),0,-angle)
    for n in range(8):
        r=39+n*2
        box('WallBacking','Rubber',polar(r,angle,2.75),(.17,1.98,5.5),.015,-angle)
        inset=angle-side*.12
        box('LowerArmour','Paint_Navy',polar(r,inset,.83),(.12,1.85,1.55),.035,-angle)
        box('WallCassettes','Paint_Ivory',polar(r,inset,2.9),(.12,1.85,2.25),.035,-angle)
        box('UpperService','Steel_Dark',polar(r,inset,4.65),(.12,1.85,1.03),.020,-angle)
        box('WaistRail','Steel_Brush',polar(r,angle-side*.23,1.65),(.08,1.94,.065),.014,-angle)
        for height in (1.95,3.85):
            for rr in (r-.77,r+.77):bolt(polar(rr,angle-side*.24,height),axis=(-side,0,0),radius=.023)
        # Proper louvre blades with depth, not a vent picture painted on a wall.
        if n in (1,4,6):
            for j in range(9):box('VentBlades','Steel_Brush',polar(r,angle-side*.19,4.34+j*.065),(.08,1.35,.025),.004,-angle)
    for n in range(5):
        r=38+n*4
        box('StructuralRibs','Steel_Brush',polar(r,angle-side*.20,2.75),(.32,.20,5.5),.035,-angle)
        box('RibBase','Paint_Navy',polar(r,angle-side*.23,.35),(.40,.42,.7),.035,-angle)
    # Long exposed utility runs and real tray cross-members.
    for z,mat in ((4.08,'Safety_Yellow'),(4.19,'Steel_Brush'),(4.29,'Paint_Navy')):
        tube('UtilityPipes',mat,polar(38.4,angle-side*.6,z),polar(53.4,angle-side*.6,z),.036)
    for n in range(16):
        box('CableTray','Steel_Dark',polar(38.5+n,angle-side*.8,4.86),(.46,.035,.075),.005,-angle)
    for offset in (.55,1.0):tube('CableTray','Steel_Brush',polar(38.1,angle-side*offset,4.88),polar(53.8,angle-side*offset,4.88),.022)

# Ceiling cassettes and a deliberately visible overhead gantry structure.
for row in range(4):
    for col in range(7):
        a=-13.5+col*27/7
        arc('Ceiling','Paint_Navy',38+row*4+.035,42+row*4-.035,a+.06,a+27/7-.06,5.5,5.68,.012)
for r in (38.4,42.3,46.3,50.3,53.6):
    arc('OverheadRibs','Steel_Brush',r-.1,r+.1,-13.4,13.4,5.23,5.48,.018)
    for angle in (-8,8):
        p=polar(r,angle,5.16)
        box('LampHousing','Steel_Dark',p,(.50,1.6,.16),.045,-angle)
        box('LampDiffuser','Light_Warm',p+Vector((0,0,-.09)),(.33,1.38,.025),.018,-angle)
for angle in (-3.8,3.8):
    tube('CraneTrack','Steel_Dark',polar(38.3,angle,4.99),polar(53.5,angle,4.99),.085)
box('CraneBridge','Safety_Yellow',(0,8.5,4.80),(6.1,.60,.32),.045)
box('HoistMotor','Steel_Dark',(-1.7,8.5,4.53),(.85,.85,.48),.065)
tube('HoistCable','Steel_Brush',(-1.7,8.5,4.3),(-1.7,8.5,3.8),.018)

# Front bulkhead with the existing three 3.2 x 3.4 metre portals at +/-6.5 degrees.
previous=-13.5
for i,angle in enumerate((-6.5,0,6.5),1):
    a,b=angle-1.7,angle+1.7
    arc('FrontBulkhead','Paint_Ivory',53.9,54.15,previous,a,0,5.5)
    arc('COL_Front','Collision',53.9,54.15,previous,a,0,5.5,0)
    arc('FrontHeader','Paint_Navy',53.85,54.2,a,b,3.4,5.5)
    arc('COL_Header','Collision',53.85,54.2,a,b,3.4,5.5,0)
    previous=b
    center=polar(53.65,angle,1.7)
    bpy.ops.object.empty_add(location=center);frame=bpy.context.object;frame.name=f'Door_{i:02d}';frame.rotation_euler.z=-math.radians(angle)
    for sign in (-1,1):
        box('DoorJamb','Steel_Brush',(sign*1.78,0,0),(.30,.60,3.6),.035,parent=frame)
        box('DoorSeal','Rubber',(sign*1.62,-.08,0),(.055,.40,3.4),.010,parent=frame)
        box('PortalSignal','Signal_Cyan' if i==2 else 'Signal_Amber',(sign*1.82,-.32,.35),(.045,.035,1.7),.010,parent=frame)
        for z in (-1.25,0,1.25):bolt((sign*1.78,-.315,z),radius=.031,parent=frame)
        bpy.ops.object.empty_add();leaf=bpy.context.object;leaf.name=f'Door_{i:02d}_'+('Left' if sign<0 else 'Right');leaf.parent=frame;leaf.location=(sign*.80,0,0)
        box('DoorPanel','Paint_Navy',(0,0,0),(1.57,.23,3.32),.045,parent=leaf)
        box('DoorInset','Paint_Ivory',(0,-.14,.47),(1.28,.055,1.31),.055,parent=leaf)
        box('DoorToe','Steel_Brush',(0,-.15,-1.33),(1.28,.045,.20),.025,parent=leaf)
        box('DoorHandle','Steel_Brush',(-sign*.48,-.20,-.2),(.045,.09,.40),.015,parent=leaf)
        # Directional safety bars are modelled, so their shading follows the scene.
        for n in range(6):box('DoorWarning','Safety_Yellow',(-.57+n*.22,-.17,-.79),(.11,.025,.18),.003,parent=leaf)
    box('DoorLintel','Steel_Brush',(0,0,1.83),(3.85,.68,.3),.045,parent=frame)
    text(f'SCHLEUSE  {i:02d}',polar(53.52,angle,4.35),.32,angle)
    text('AUSSENDOCK  /  SERVICE',polar(53.52,angle,3.94),.13,angle,'Signal_Cyan')
    # A small separated control pedestal next to each portal.
    panel=polar(52.85,angle+2.45,1.16)
    box('DoorConsole','Paint_Navy',panel,(.62,.30,1.34),.07,-angle)
    box('DoorDisplay','Screen',panel+Vector((0,-.17,.32)),(.44,.035,.35),.025,-angle)
    for j in range(3):box('DoorDisplayLines','Signal_Cyan',panel+Vector((0,-.194,.43-j*.095)),(.28,.006,.016),.002,-angle)
arc('FrontBulkhead','Paint_Ivory',53.9,54.15,previous,13.5,0,5.5)
arc('COL_Front','Collision',53.9,54.15,previous,13.5,0,5.5,0)

# Entry frame and a view back into a short ring connector.
for side in (-1,1):
    arc('RearBulkhead','Paint_Navy',37.8,38.1,-13.5 if side<0 else 5,-5 if side<0 else 13.5,0,5.5)
    arc('COL_Rear','Collision',37.8,38.1,-13.5 if side<0 else 5,-5 if side<0 else 13.5,0,5.5,0)
box('RearLintel','Steel_Brush',(0,-.10,4.9),(6.9,.6,1.2),.05)
box('ConnectorDeck','Steel_Dark',(0,-2.1,-.18),(6.7,4.4,.34),.015)
box('COL_Connector','Collision',(0,-2.1,-.18),(6.7,4.4,.34),0)
box('ConnectorBack','Paint_Navy',(0,-4.3,2.75),(6.7,.25,5.5),.02)
box('COL_ConnectorBack','Collision',(0,-4.3,2.75),(6.7,.25,5.5),0)
for side in (-1,1):
    box('ConnectorWall','Paint_Navy',(side*3.45,-2.1,2.75),(.2,4.4,5.5),.02)
    box('COL_ConnectorWall','Collision',(side*3.45,-2.1,2.75),(.2,4.4,5.5),0)
box('ConnectorRoof','Paint_Navy',(0,-2.1,5.6),(7,4.5,.2),.02)
text('RING 01  /  S02', (0,-4.14,2.5),.38,180,'Signal_Cyan').name='ConnectorSign__Signal_Cyan__Entry'

# Workbench, tool drawers, wall services and a dedicated drone inspection cradle.
for side in (-1,1):
    x=side*7.0;y=5.7
    box('Workbench','Steel_Dark',(x,y,.94),(1.6,4.5,.16),.06)
    box('COL_Workbench','Collision',(x,y,.52),(1.7,4.5,1.08),0)
    for yy in (y-1.5,y+1.5):
        box('ToolCabinet','Paint_Navy',(x,yy,.45),(1.38,1.25,.86),.06)
        for z in (.16,.35,.54,.73):
            box('ToolDrawer','Paint_Ivory',(x-side*.72,yy,z),(.035,1.07,.15),.018)
            box('DrawerHandle','Steel_Brush',(x-side*.76,yy,z),(.045,.55,.025),.008)
    for j in range(4):box('BenchTools','Steel_Brush',(x+random.uniform(-.4,.4),y-1+j*.55,1.05),(.18,.40,.06),.018,angle=j*14)
    for z in (1.3,1.6):tube('UtilityStands','Steel_Dark',(x+side*.7,y-2,z),(x+side*.7,y+2,z),.024)
box('DroneStand','Paint_Navy',(-4.0,10.1,.40),(3.3,3.4,.80),.08)
box('COL_DroneStand','Collision',(-4.0,10.1,.40),(3.3,3.4,.80),0)
box('DroneStandTop','Steel_Brush',(-4.0,10.1,.84),(3.45,3.55,.09),.055)
for sign in (-1,1):box('CradleGuide','Rubber',(-4+sign*1.1,10.1,.94),(.12,2.7,.12),.025)
text('M-01',(-4,8.30,.4),.20,0,'Signal_Cyan')
for x in (8.0,9.6):
    box('ServiceCabinet','Paint_Ivory',(x,13.8,1.05),(1.25,.70,2.1),.07)
    box('COL_ServiceCabinet','Collision',(x,13.8,1.05),(1.25,.70,2.1),0)
    box('CabinetFace','Paint_Navy',(x,13.40,1.25),(1.05,.065,1.3),.035)
    box('CabinetStatus','Signal_Cyan',(x-.4,13.35,1.6),(.025,.025,.40),.005)
    for j in range(5):box('CabinetVents','Steel_Dark',(x,13.35,.3+j*.07),(.78,.025,.024),.004)

# Outside inspection apron; it belongs to this experiment, not a new station expansion.
arc('OuterApron','Steel_Dark',54.15,58.8,-13,13,-.2,-.02)
for angle in (-12.9,12.9):
    tube('ApronRail','Steel_Brush',polar(54.2,angle,1.1),polar(58.8,angle,1.1),.045)
    box('COL_ApronSide','Collision',polar(56.5,angle,.7),(.18,5,1.4),0,-angle)
arc('ApronRail','Steel_Dark',58.65,58.8,-13,13,0,1.05)
arc('COL_ApronEnd','Collision',58.65,58.8,-13,13,0,1.15,0)

# Apply fabrication modifiers, then group static geometry by system and material.
all_meshes=[o for o in scene.objects if o.type=='MESH']
for o in all_meshes:
    bpy.context.view_layer.objects.active=o
    for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
groups={}
for o in all_meshes:
    if o.parent or o.name.startswith('COL_'):continue
    key='__'.join(o.name.split('__')[:2]);groups.setdefault(key,[]).append(o)
for key,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();objects[0].name=key+'__Batch'

# Preserve editability in the native source, without external texture-path dependencies.
scene.render.engine='BLENDER_EEVEE_NEXT'
scene.world.color=(.08,.10,.14)
for o in scene.objects:
    if o.name.startswith('COL_'):o.hide_render=True
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Station-Hall-Experiment-v01.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(OUT/'StationHall-v01.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
                        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
                        bake_anim=False,add_leaf_bones=False,path_mode='STRIP',use_custom_props=False)
report={'blender':bpy.app.version_string,'units':'metres','innerRadius':38,'outerRadius':54,'halfAngleDegrees':13.5,
        'height':5.5,'doorWidth':3.2,'doorHeight':3.4,'doors':3,'originalObjects':counter,
        'exportedMeshObjects':sum(o.type=='MESH' for o in scene.objects),'vertices':sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'),
        'materials':list(COLORS),'aiAlbedo':'Textures/PaintedAlloy_Albedo.png','campaignChanged':False}
(ART/'model-report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('HALL MODEL GENERATED',json.dumps(report))
