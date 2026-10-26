import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Design/Models/OperatorArm/Revision-R04'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'OperatorArm-R04.blend'))
bpy.ops.export_scene.gltf(filepath=str(OUT/'OperatorArm-R04-Preview.glb'),export_format='GLB',export_yup=True,export_apply=True)
from mathutils import Vector
code=(ROOT/'tools/EnhanceOperatorArmR04.py').read_text(encoding='utf-8');exec(code[code.index('scene=bpy.context.scene;'):])
