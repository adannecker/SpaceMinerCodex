"""Lower leading edge; lengthen the single planar glass within the 1m chassis."""
from pathlib import Path
import bpy, math, json
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Design/Models/TouchConsole/Revision-R02';OUT=ROOT/'Design/Models/TouchConsole/Revision-R03';OUT.mkdir(exist_ok=True)
t=math.radians(12);up=Vector((0,math.sin(t),math.cos(t)));normal=Vector((0,math.cos(t),-math.sin(t)))
def B(v):return Vector((v[0],-v[2],v[1]))
def C(v):return Vector((v.x,v.z,-v.y))
old=Vector((0,1.002,.01));new=Vector((0,1.0,0))
def revise(source,dest,preview):
    bpy.ops.wm.open_mainfile(filepath=str(source));bpy.context.preferences.filepaths.save_version=0
    for o in bpy.data.objects:
        if o.type!='MESH':continue
        screen=o.name.startswith('TouchGlass') or o.name.startswith('PREVIEW_UI') or o.name.startswith('R02 screen') or o.name.startswith('R02 display')
        rail=o.name.startswith('R02_Frame') and min((o.matrix_world@Vector(p)).z for p in o.bound_box)>.80
        inv=o.matrix_world.inverted()
        for v in o.data.vertices:
            p=C(o.matrix_world@v.co)
            if screen:
                d=p-old;along=d.dot(up);depth=d.dot(normal)
                p=new+Vector((p.x,0,0))+up*(along*.94/.79)+normal*depth
            elif rail:
                # Front shoulder drops into a distinct short chamfer; rear height stays.
                p.y-=.035*max(0,min(1,(.45-p.z)/.94))
            else:continue
            v.co=inv@B(p)
        o.data.update()
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/dest),compress=True)
    if not preview:return
    bpy.ops.export_scene.gltf(filepath=str(OUT/'TouchConsole-R03.glb'),export_format='GLB',export_yup=True,export_apply=True)
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
    scene.render.resolution_x=1300;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.17,.21,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
    for name,pos,power in [('Key',(-2,3,-2.7),550),('Fill',(2,2,-1),350),('Rim',(-1,3,2),600)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=2.5;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=B(pos);o.rotation_euler=(B((0,.6,0))-o.location).to_track_quat('-Z','Y').to_euler()
    d=bpy.data.cameras.new('R03');cam=bpy.data.objects.new('R03',d);scene.collection.objects.link(cam);scene.camera=cam;d.lens=57
    cam.location=B((2.1,1.8,-2.9));cam.rotation_euler=(B((0,.58,0))-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/'TouchConsole-R03.png');bpy.ops.render.render(write_still=True)
    glass=next(o for o in bpy.data.objects if o.name.startswith('TouchGlass'))
    pts=[C(glass.matrix_world@v.co) for v in glass.data.vertices]
    ys=[v.y for v in pts];zs=[v.z for v in pts]
    report={'glassFrontHeight':min(ys),'glassRearHeight':max(ys),'glassDepth':max(zs)-min(zs),'glassSlopeLength':.94,'tiltDegrees':12,'chassisNominal':[1.50,1.14,1.00],'screenPlanar':True,'unityIntegration':False}
    (OUT/'report.json').write_text(json.dumps(report,indent=2))
revise(BASE/'TouchConsole-R02.blend','TouchConsole-R03.blend',False)
revise(BASE/'TouchConsole-R02-DisplayPreview-Final.blend','TouchConsole-R03-DisplayPreview.blend',True)
print('R03 DONE',flush=True)
