"""Export preserved first preview for the revision comparison; source untouched."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Design/Models/TouchConsole/Revision-R01';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Design/Models/TouchConsole/TouchConsole-Preview.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type=='FONT':o.select_set(True)
if bpy.context.selected_objects:
    bpy.context.view_layer.objects.active=bpy.context.selected_objects[0];bpy.ops.object.convert(target='MESH')
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type=='MESH' and not o.hide_render and 'stage not exported' not in o.name:o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT/'TouchConsole-R01.glb'),export_format='GLB',use_selection=True,export_apply=True)
print('R01 EXPORTED',flush=True)
