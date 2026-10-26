import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'Design/Models/OperatorArm/Original/Tripo-H31-Retry.glb'))
report=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 coords=[o.matrix_world@v.co for v in o.data.vertices]
 report.append({'name':o.name,'vertices':len(coords),'polygons':len(o.data.polygons),'min':[min(p[k] for p in coords) for k in range(3)],'max':[max(p[k] for p in coords) for k in range(3)],'materials':[m.name for m in o.data.materials]})
print('ARM_REPORT '+json.dumps(report),flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Design/Models/OperatorArm/Original/Tripo-H31-Retry.blend'))
