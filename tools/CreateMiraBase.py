"""Create an editable, licensed anatomical starting point; not the final Mira likeness."""
from pathlib import Path
import zipfile, json
import bpy
from mathutils import Vector
from bl_ext.spaceminer.mpfb.services.humanservice import HumanService
from bl_ext.spaceminer.mpfb.services.targetservice import TargetService
from bl_ext.spaceminer.mpfb.services.assetservice import AssetService
from bl_ext.spaceminer.mpfb.services.locationservice import LocationService
from bl_ext.spaceminer.mpfb.entities.objectproperties import HumanObjectProperties

root = Path(__file__).resolve().parents[1]
tools = root / 'work/AvatarTools'
data = Path(LocationService.get_user_data())
for name in ('makehuman_system_assets_cc0', 'faceunits01', 'visemes02'):
    with zipfile.ZipFile(tools / (name + '.zip')) as archive:
        archive.extractall(data)
AssetService.rescan_pack_metadata()
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
body = HumanService.create_human()
body.name = 'Mira_AnatomicalBase'
for key,value in {'gender':0.0, 'age':.35, 'weight':.43, 'muscle':.45}.items():
    HumanObjectProperties.set_value(key, value, entity_reference=body)
TargetService.reapply_macro_details(body)
HumanService.set_character_skin(AssetService.find_asset_absolute_path('young_caucasian_female.mhmat',asset_subdir='skins'),body,skin_type='GAMEENGINE')
HumanService.add_builtin_rig(body,'game_engine')
for subdir,name,kind in [('eyes','high-poly.mhclo','Eyes'),('eyebrows','eyebrow001.mhclo','Eyebrows'),
                         ('eyelashes','eyelashes01.mhclo','Eyelashes'),('teeth','teeth_base.mhclo','Teeth'),
                         ('tongue','tongue01.mhclo','Tongue'),('hair','short04.mhclo','Hair'),
                         ('clothes','female_casualsuit01.mhclo','Clothes')]:
    path=AssetService.find_asset_absolute_path(name,asset_subdir=subdir)
    if not path: raise RuntimeError('Required asset missing: '+name)
    HumanService.add_mhclo_asset(path,body,asset_type=kind,material_type='GAMEENGINE')
for directory in ('faceunits','visemes'):
    for target in (data/'targets'/directory).glob('*.target'):
        TargetService.load_target(body,str(target),weight=0)
for obj in bpy.context.scene.objects:
    if obj.type=='MESH':
        for face in obj.data.polygons: face.use_smooth=True
for mat in bpy.data.materials:
    if mat.use_nodes:
        bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if bsdf and 'Skin' in mat.name: bsdf.inputs['Roughness'].default_value=.48
world=bpy.context.scene.world or bpy.data.worlds.new('Mira studio')
bpy.context.scene.world=world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.035,.05,.075,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.35
evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
head_z=max((evaluated.matrix_world @ v.co).z for v in evaluated.data.vertices)
look=Vector((0,0,head_z-.23))
bpy.ops.object.camera_add(location=(.28,-1.6,head_z-.20)); camera=bpy.context.object
camera.rotation_euler=(look-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=.65
bpy.context.scene.camera=camera
for name,pos,power,color,size in [('Key',(-1,-1.1,head_z+.3),45,(1,.89,.8),1.2),
                                ('Fill',(1,-.5,head_z),25,(.65,.85,1),1.0),
                                ('Rim',(0,.7,head_z+.1),35,(.2,.8,1),.8)]:
    bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object
    light.name=name;light.data.energy=power;light.data.color=color;light.data.shape='DISK';light.data.size=size
    light.rotation_euler=(look-light.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=900;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
output=root/'docs/Art/Mira3D';output.mkdir(parents=True,exist_ok=True)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(output/'Mira-AnatomicalBase.blend'))
scene.render.filepath=str(output/'Mira-AnatomicalBase.png'); bpy.ops.render.render(write_still=True)
report={'status':'anatomical starting point, not final Mira likeness','blender':bpy.app.version_string,
        'vertices':len(body.data.vertices),'shape_keys':len(body.data.shape_keys.key_blocks),
        'core_asset_license':'CC0','source':'https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html'}
(output/'base-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MIRA ANATOMICAL BASE CREATED',report)
