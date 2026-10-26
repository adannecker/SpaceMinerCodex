"""Export approved clean R04 as canonical Unity mesh data, no demo telemetry."""
from pathlib import Path
import bpy,bmesh,json
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/SpaceMiner/Art/TouchConsoleR04';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Design/Models/TouchConsole/Revision-R04/TouchConsole-R04.blend'))
groups={}
for o in bpy.data.objects:
    if o.type!='MESH':continue
    mesh=o.data.copy();bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update()
    normals=o.matrix_world.to_3x3().inverted().transposed()
    for face in mesh.polygons:
        mat=o.data.materials[face.material_index];name=mat.name
        g=groups.setdefault(name,{'name':name,'vertices':[],'normals':[],'uv':[],'triangles':[]})
        for li in face.loop_indices:
            p=o.matrix_world@mesh.vertices[mesh.loops[li].vertex_index].co;n=(normals@mesh.corner_normals[li].vector).normalized();uv=mesh.uv_layers.active.data[li].uv if mesh.uv_layers.active else (0,0)
            g['triangles'].append(len(g['vertices'])//3);g['vertices']+=list((p.x,p.z,-p.y));g['normals']+=list((n.x,n.z,-n.y));g['uv']+=list(uv)
    bpy.data.meshes.remove(mesh)
(OUT/'geometry.json').write_text(json.dumps({'groups':list(groups.values())},separators=(',',':')))
print('R04 RUNTIME EXPORTED',[(k,len(v['triangles'])//3) for k,v in groups.items()],flush=True)
