import bpy,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'Design/Models/OperatorArm';out.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(out/'Original/Tripo-H31-Retry.blend'))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
scene.render.resolution_x=1100;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.15,.19,1)
for pos,power in [((0,-3,3),450),((2,1,2),500),((-2,-1,1),220)]:
 d=bpy.data.lights.new('Softbox','AREA');d.energy=power;d.size=3;o=bpy.data.objects.new('Softbox',d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Inspection');cam=bpy.data.objects.new('Inspection',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=1.35
for name,pos in [('front',(0,-3,.5)),('back',(0,3,.5))]:
 cam.location=pos;cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
