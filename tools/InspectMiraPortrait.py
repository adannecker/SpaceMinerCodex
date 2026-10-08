from pathlib import Path
import bpy,json
r=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(r/'docs/Art/Mira3D/Mira-Portrait-v02.blend'))
for o in bpy.context.scene.objects:
    if o.type!='MESH':continue
    ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
    co=[ev.matrix_world@v.co for v in me.vertices]
    print(o.name, 'bounds', [[min(v[i] for v in co),max(v[i] for v in co)] for i in range(3)],'materials', [m.name for m in o.data.materials if m],flush=True)
    if o.name=='Mira_AnatomicalBase':print('MODIFIERS',[(m.name,m.type,getattr(m,'vertex_group','')) for m in o.modifiers],flush=True)
    ev.to_mesh_clear()
