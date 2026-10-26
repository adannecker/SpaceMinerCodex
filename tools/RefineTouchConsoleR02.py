"""Non-destructive concept-focused console revision; Blender 4.5, no Unity build."""
from pathlib import Path
import bpy, math, json, importlib.util
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('console_base',ROOT/'tools/CreateTouchConsole.py')
c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)
OUT=ROOT/'Design/Models/TouchConsole/Revision-R02';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Design/Models/TouchConsole/TouchConsole.blend'))
bpy.context.preferences.filepaths.save_version=0
# Work only on the highest-detail source. Prior model and runtime exports stay intact.
for o in list(bpy.data.objects):
    if o.name.startswith('TouchConsole_LOD') and o.name!='TouchConsole_LOD0':
        for ch in list(o.children_recursive):bpy.data.objects.remove(ch,do_unlink=True)
        bpy.data.objects.remove(o,do_unlink=True)
root=bpy.data.objects.get('TouchConsole_LOD0')
for o in list(root.children_recursive):o.parent=None
bpy.data.objects.remove(root,do_unlink=True)
for key,name in [('surface','TouchConsole_Surface'),('Display','TouchConsole_Display'),('Plaque','TouchConsole_Plaque')]:c.MATS[key]=bpy.data.materials[name]
c.LOD=0;c.PART=5000
# Replace rounded monolithic structural rails with faceted manufactured sections.
for o in list(bpy.data.objects):
    if o.type!='MESH':continue
    if o.name.startswith('AlloyFrame') or o.name.startswith('FrontPlaque') or (o.name.startswith('ServicePanels') and o.location.z>.78 and o.location.y>.47):
        bpy.data.objects.remove(o,do_unlink=True)
for s in (-1,1):
    # Long polygon boot, rubber sole, raised front/rear toe caps.
    boot=[(-.50,.018),(.50,.018),(.50,.077),(.46,.116),(-.44,.116),(-.50,.078)]
    c.extrude_profile('R02_Frame','SideAlloy',s*.663,.174,boot,.004)
    for z in (-.419,.416):
        toe=[(z-.079,.042),(z+.079,.042),(z+.079,.092),(z+.052,.129),(z-.052,.129),(z-.079,.094)]
        c.extrude_profile('R02_Frame','WornEdge',s*.663,.176,toe,.003)
    # Chamfered upright with visible changes of width and segmented shoulder.
    front=[(-.497,.125),(-.497,.729),(-.480,.805),(-.420,.884),(-.350,.862),(-.350,.167),(-.392,.125)]
    c.extrude_profile('R02_Frame','Alloy',s*.682,.127,front,.004)
    rear=[(.351,.125),(.351,1.056),(.389,1.132),(.451,1.132),(.495,1.081),(.495,.125)]
    c.extrude_profile('R02_Frame','SideAlloy',s*.682,.127,rear,.004)
    # Broad sloping cheek with sharply articulated front bend.
    cheek=[(-.491,.818),(-.491,.908),(-.427,.970),(.349,1.139),(.450,1.139),(.486,1.095),(.475,1.049),(.345,1.036),(-.368,.879)]
    c.extrude_profile('R02_Frame','Alloy',s*.681,.138,cheek,.0035)
    # Inset dark seam and narrow worn edge highlight on each structural cheek.
    c.slope('R02_Details','WornEdge',(s*.618,1.009,.01),(.009,.007,.795),.001)
    for z in (-.327,.339):
        c.slope('R02_Details','Recess',(s*.681,1.009+z*math.tan(c.TILT)+.022,z),(.139,.003,.006),0)
    for z in (-.422,-.306,.345):
        c.bolt((s*.681,1.009+z*math.tan(c.TILT)+.029,z),'slope',.007)
    for y in (.195,.688):c.bolt((s*.687,y,-.500),'front',.006)
    # Distinct side access hatch and inset border, protected cooling gills.
    for y in (.205,.735):
        c.box('R02_Details','Hinge',(s*.695,y,-.220),(.011,.040,.078),.002)
    for z in (-.300,.250):
        for y in (.22,.73):c.bolt((s*.697,y,z),'side',.005)
    for j in range(7):
        c.box('R02_Details','Vent',(s*.699,.200+j*.014,-.225),(.004,.004,.105),.0007)
    # Tiny stamped service data plaques on side, not operating controls.
    c.box('R02_Details','PlaqueBorder',(s*.698,.451,.190),(.004,.070,.096),.001)
# Short front central plaque and independent serial instead of long blank banner.
c.box('R02_Frame','Alloy',(0,.847,-.455),(1.252,.126,.066),.004)
c.box('R02_Details','Recess',(0,.844,-.490),(1.126,.092,.003),.002)
c.box('R02_Plaque','Plaque',(0,.843,-.494),(.865,.076,.004),.002)
for x in (-.415,.415):
    for y in (.817,.868):c.bolt((x,y,-.498),'front',.003)
c.box('R02_Details','PlaqueBorder',(.558,.843,-.493),(.108,.071,.004),.002)
c.box('R02_Plaque','Plaque',(.558,.843,-.496),(.095,.058,.003),.001)
c.slope('R02_Frame','Alloy',(0,1.105,.455),(1.247,.024,.041),.002)
# Raised angular lower kick plate, distinct from flat cabinet.
kick=[(-.417,.116),(-.417,.147),(-.392,.198),(-.369,.224),(-.342,.224),(-.342,.116)]
c.extrude_profile('R02_Details','FootPaint',0,.79,kick,.003)
# Fix the original atlas alpha default: image textures ignore alpha unless explicitly stored.
mat=c.MATS['surface'];ns=mat.node_tree.nodes;ln=mat.node_tree.links;bs=ns.get('Principled BSDF')
for link in list(ln):
    if link.to_socket==bs.inputs['Roughness']:ln.remove(link)
tex=ns.new('ShaderNodeTexImage');tex.image=bpy.data.images.get('TouchConsole_MetalRough.png')
if tex.image is None:
    tex.image=bpy.data.images.load(str(ROOT/'Assets/SpaceMiner/Resources/TouchConsole/Textures/TouchConsole_MetalRough.png'))
sep=ns.new('ShaderNodeSeparateColor');ln.new(tex.outputs['Color'],sep.inputs[0]);ln.new(sep.outputs['Green'],bs.inputs['Roughness'])
# Micro-scale grain and broad subtle surface variation, not camera noise.
coord=ns.new('ShaderNodeTexCoord');noise=ns.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=165;noise.inputs['Detail'].default_value=2
ln.new(coord.outputs['Generated'],noise.inputs['Vector'])
bump=ns.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.00035
ln.new(noise.outputs['Fac'],bump.inputs['Height']);ln.new(bump.outputs['Normal'],bs.inputs['Normal'])
# Native source remains a clean reusable housing, before the explicit UI fixture.
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'TouchConsole-R02.blend'),compress=True)
c.preview_ui()
# Detailed screen pictograms built from editable vector geometry, no concept pasted on model.
cyan=c.emissive('R02 cyan',(.025,.48,.64),1.1);white=c.emissive('R02 white',(.59,.72,.77),1)
green=c.emissive('R02 green',(.04,.55,.32),1);dark=c.emissive('R02 card',(.004,.025,.038),.6)
def poly(name,pts,mat,depth=.0118):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata([c.B(c.SCREEN-c.RIGHT*x+c.UP*y+c.NORMAL*depth) for x,y in pts],[],[tuple(reversed(range(len(pts))))]);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(o);o.data.materials.append(mat)
def rect(x,y,w,h,mat):poly('R02 screen detail',[(x-w/2,y-h/2),(x+w/2,y-h/2),(x+w/2,y+h/2),(x-w/2,y+h/2)],mat)
def line(a,b,width=.0018,mat=cyan):
    dx,dy=b[0]-a[0],b[1]-a[1];l=math.hypot(dx,dy);nx,ny=-dy/l*width/2,dx/l*width/2
    poly('R02 display line',[(a[0]+nx,a[1]+ny),(b[0]+nx,b[1]+ny),(b[0]-nx,b[1]-ny),(a[0]-nx,a[1]-ny)],mat,.0121)
def battery(x,y,fill):
    for a,b in [((x-.025,y-.047),(x-.025,y+.047)),((x+.025,y-.047),(x+.025,y+.047)),((x-.025,y-.047),(x+.025,y-.047)),((x-.025,y+.047),(x+.025,y+.047))]:line(a,b,.0023)
    rect(x,y+.052,.020,.006,cyan)
    for j in range(4):rect(x,y-.033+j*.022,.039,.014,green if j<fill else cyan)
# Hide coarse bar-chart primitives in the reactor card only; replace with a technical reactor icon.
rect(-.385,.067,.30,.12,dark)
for x in (-.404,-.366):line((x,.018),(x,.115),.004)
for y in (.026,.049,.075,.103):
    line((-.421,y),(-.349,y),.003)
    line((-.421,y),(-.411,y+.006),.0015);line((-.359,y+.006),(-.349,y),.0015)
rect(-.385,.068,.015,.085,cyan)
# Solar panel icon and diagnostic waveform, battery cells and lower-card borders.
for i in range(5):line((-.135+i*.016,.022),(-.120+i*.016,.092),.0012)
for j in range(4):line((-.135+j*.005,.022+j*.023),(-.071+j*.005,.022+j*.023),.0012)
battery(.284,.066,2)
for x in (-.395,-.045):
    for y in (-.108,-.333):line((x-.163,y),(x+.163,y),.001)
    for xx in (x-.163,x+.163):line((xx,-.108),(xx,-.333),.001)
battery(-.263,-.236,2);battery(.086,-.236,0)
# Replace original front text with smaller precisely centred type plus serial.
label=bpy.data.objects.get('PREVIEW plaque label not exported');label.data.size=.039;label.location=c.B((0,.843,-.4985))
curve=bpy.data.curves.new('Serial legend','FONT');curve.body='P-05';curve.size=.022;curve.align_x='CENTER';curve.align_y='CENTER'
o=bpy.data.objects.new('R02 serial P05',curve);bpy.context.scene.collection.objects.link(o);o.location=c.B((.558,.843,-.499));o.rotation_euler=(math.pi/2,0,math.pi);o.data.materials.append(white)
# Keep explicit demo words. This is not live telemetry.
# Convert type to exportable meshes and emit actual native GLB, with metallic-roughness texture.
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type=='FONT':o.select_set(True)
if bpy.context.selected_objects:
    bpy.context.view_layer.objects.active=bpy.context.selected_objects[0];bpy.ops.object.convert(target='MESH')
bpy.ops.export_scene.gltf(filepath=str(OUT/'TouchConsole-R02.glb'),export_format='GLB',export_yup=True,export_apply=True)
meshes=[o for o in bpy.data.objects if o.type=='MESH'];triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
mins=[min(v[i] for v in points) for i in range(3)];maxs=[max(v[i] for v in points) for i in range(3)]
(OUT/'report.json').write_text(json.dumps({'triangles':triangles,'dimensionsBlender':[maxs[i]-mins[i] for i in range(3)],'revision':'R02','previewUi':'static example, not live telemetry','sourcePreserved':True,'unityIntegration':False},indent=2))
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'TouchConsole-R02-DisplayPreview-Final.blend'),compress=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1300;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.17,.21,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
c.area('R02 key',(-2,3,-2.7),550,(.85,.94,1),2.5,(0,.6,0));c.area('R02 fill',(2,2,-1),350,(1,.89,.77),2,(0,.6,0));c.area('R02 rim',(-1,3,2),600,(.70,.85,1),2,(0,.7,0))
d=bpy.data.cameras.new('R02 camera');cam=bpy.data.objects.new('R02 camera',d);scene.collection.objects.link(cam);scene.camera=cam
cam.location=c.B((2.1,1.8,-2.9));cam.rotation_euler=(c.B((0,.58,0))-cam.location).to_track_quat('-Z','Y').to_euler();d.lens=57
scene.render.filepath=str(OUT/'TouchConsole-R02.png');bpy.ops.render.render(write_still=True)
print('CONSOLE R02 DONE',triangles,flush=True)
