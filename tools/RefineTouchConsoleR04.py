"""User endpoint priority: leading glass edge ~60cm, rear ~110cm."""
from pathlib import Path
import bpy, math, json
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Design/Models/TouchConsole/Revision-R03';OUT=ROOT/'Design/Models/TouchConsole/Revision-R04';OUT.mkdir(exist_ok=True)
def B(v):return Vector((v[0],-v[2],v[1]))
def C(v):return Vector((v.x,v.z,-v.y))
for src,dest,preview in [('TouchConsole-R03.blend','TouchConsole-R04.blend',False),('TouchConsole-R03-DisplayPreview.blend','TouchConsole-R04-DisplayPreview.blend',True)]:
    bpy.ops.wm.open_mainfile(filepath=str(BASE/src));bpy.context.preferences.filepaths.save_version=0
    glass=next(o for o in bpy.data.objects if o.name.startswith('TouchGlass'))
    pts=[C(glass.matrix_world@v.co) for v in glass.data.vertices];front=min(v.z for v in pts);rear=max(v.z for v in pts);low=min(v.y for v in pts)
    drop=low-.60
    for o in bpy.data.objects:
        if o.type!='MESH':continue
        inv=o.matrix_world.inverted()
        for v in o.data.vertices:
            p=C(o.matrix_world@v.co)
            # All top geometry follows one affine plane; cabinet tapers toward floor.
            weight=max(0,min(1,(p.y-.10)/.68))
            p.y-=drop*(rear-p.z)/(rear-front)*weight
            v.co=inv@B(p)
        o.data.update()
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/dest),compress=True)
    if not preview:continue
    bpy.ops.export_scene.gltf(filepath=str(OUT/'TouchConsole-R04.glb'),export_format='GLB',export_yup=True,export_apply=True)
    pts=[C(glass.matrix_world@v.co) for v in glass.data.vertices];ys=[v.y for v in pts];zs=[v.z for v in pts]
    slope=math.degrees(math.atan2(max(ys)-min(ys),max(zs)-min(zs)))
    (OUT/'report.json').write_text(json.dumps({'frontHeightM':min(ys),'rearHeightM':max(ys),'screenSlopeApproxDegrees':slope,'userTargetFrontM':.60,'interpretation':'Height endpoint takes priority over ambiguous 20 percent phrase','unityIntegration':False},indent=2))
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1300;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.17,.21,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
    for name,pos,power in [('Key',(-2,3,-2.7),550),('Fill',(2,2,-1),350),('Rim',(-1,3,2),600)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=2.5;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=B(pos);o.rotation_euler=(B((0,.6,0))-o.location).to_track_quat('-Z','Y').to_euler()
    d=bpy.data.cameras.new('R04');cam=bpy.data.objects.new('R04',d);scene.collection.objects.link(cam);scene.camera=cam;d.lens=57;cam.location=B((2.1,1.8,-2.9));cam.rotation_euler=(B((0,.58,0))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'TouchConsole-R04.png');bpy.ops.render.render(write_still=True)
print('R04 DONE',flush=True)
