"""Run with the project's portable Blender, after the official archives are unpacked."""
from pathlib import Path
import shutil
import json
import bpy

root = Path(__file__).resolve().parents[1]
tools = root / "work/AvatarTools"
repository = tools / "extensions/spaceminer"
repository.mkdir(parents=True, exist_ok=True)
source = tools / "mpfb2-2.0.17/src/mpfb"
shutil.copytree(source, repository / "mpfb", dirs_exist_ok=True)
repo = next((item for item in bpy.context.preferences.extensions.repos if item.module == "spaceminer"), None)
if repo is None:
    repo = bpy.context.preferences.extensions.repos.new(
        name="SpaceMiner portable tools", module="spaceminer", custom_directory=str(repository))
repo.use_remote_url = False
bpy.ops.preferences.addon_enable(module="bl_ext.spaceminer.mpfb")
preferences = bpy.context.preferences.addons["bl_ext.spaceminer.mpfb"].preferences
preferences.mpfb_user_data = str(tools / "mpfb-user")
bpy.ops.wm.save_userpref()
from bl_ext.spaceminer.mpfb.services.humanservice import HumanService
body = HumanService.create_human()
assert len(body.data.vertices) > 10000
report = {"blender": bpy.app.version_string, "mpfb": "2.0.17", "vertices": len(body.data.vertices),
          "repository": str(repository), "config": bpy.utils.user_resource('CONFIG')}
(tools / "setup-report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print("MIRA PORTABLE TOOLS CHECK PASSED", report)
