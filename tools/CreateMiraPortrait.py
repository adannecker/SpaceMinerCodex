"""Reference-led Mira portrait revision; native editable geometry, no generated picture."""
from pathlib import Path
import bpy, math, json, random
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from bl_ext.spaceminer.mpfb.services.humanservice import HumanService
from bl_ext.spaceminer.mpfb.services.targetservice import TargetService
from bl_ext.spaceminer.mpfb.services.assetservice import AssetService

root=Path(__file__).resolve().parents[1]
output=root/'docs/Art/Mira3D'; output.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(output/'Mira-AnatomicalBase.blend'))
body=bpy.data.objects['Mira_AnatomicalBase']
targetroot=root/'work/AvatarTools/extensions/spaceminer/mpfb/data/targets'
settings={'head/head-oval':.18,'chin/chin-width-decr':.18,
          'chin/chin-height-decr':.08,'cheek/l-cheek-volume-incr':.20,
          'cheek/r-cheek-volume-incr':.18,'cheek/l-cheek-bones-incr':.12,
          'cheek/r-cheek-bones-incr':.12,'nose/nose-width1-decr':.15,
          'nose/nose-point-width-decr':.10,'mouth/mouth-upperlip-volume-incr':.22,
          'mouth/mouth-lowerlip-volume-incr':.16,'mouth/mouth-angles-up':.12,
          'eyes/l-eye-scale-incr':.07,'eyes/r-eye-scale-incr':.07}
for name,value in settings.items():
    path=targetroot/(name+'.target.gz')
    assert path.exists(),path
    TargetService.load_target(body,str(path),weight=value,name='MiraDesign_'+path.stem)
keys=body.data.shape_keys.key_blocks
for pattern,value in [('mouthSmileLeft',.20),('mouthSmileRight',.17),('jawOpen',0)]:
    for k in keys:
        if pattern.lower() in k.name.lower():k.value=value
HumanService.refit(body)
for obj in list(bpy.context.scene.objects):
    if 'short04' in obj.name or 'female_casualsuit01' in obj.name:
        bpy.data.objects.remove(obj,do_unlink=True)
for mod in list(body.modifiers):
    if mod.name.startswith('Delete.'):
        body.modifiers.remove(mod)
HumanService.add_mhclo_asset(AssetService.find_asset_absolute_path('short03.mhclo',asset_subdir='hair'),body,asset_type='Hair',material_type='GAMEENGINE')
deps=bpy.context.evaluated_depsgraph_get()
ev=body.evaluated_get(deps); mesh=ev.to_mesh()
top=max((ev.matrix_world@v.co).z for v in mesh.vertices)
ev.to_mesh_clear()
print('MIRA HEAD TOP',top,flush=True)

def material(name,color,rough=.5,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m

cloth=material('Mira | graphite woven suit',(.023,.035,.047),.65)
nodes=cloth.node_tree.nodes;links=cloth.node_tree.links
weave=nodes.new('ShaderNodeTexNoise');weave.inputs['Scale'].default_value=450
weave.inputs['Detail'].default_value=2
fabricbump=nodes.new('ShaderNodeBump');fabricbump.inputs['Strength'].default_value=.20
fabricbump.inputs['Distance'].default_value=.00015
links.new(weave.outputs['Fac'],fabricbump.inputs['Height'])
links.new(fabricbump.outputs['Normal'],nodes.get('Principled BSDF').inputs['Normal'])
lining=material('Mira | collar lining',(.014,.020,.027),.48)
trim=material('Mira | brushed alloy seams',(.13,.19,.21),.42,.5)
cyan=material('Mira | cyan inlay',(.10,.51,.55),.34,.15)
p=cyan.node_tree.nodes.get('Principled BSDF')
p.inputs['Emission Color'].default_value=(.06,.50,.59,1);p.inputs['Emission Strength'].default_value=.65

def curve(name,points,radius,mat):
    c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=12
    c.bevel_depth=radius;c.bevel_resolution=3
    s=c.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for b,co in zip(s.bezier_points,points):b.co=co;b.handle_left_type='AUTO';b.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);o.data.materials.append(mat)
    return o

# Tailored shoulders and open standing collar, separate editable objects.
verts=[];faces=[];rings=64
levels=[(top-.46,.27,.105),(top-.35,.225,.102),(top-.30,.145,.075),
        (top-.265,.085,.080),(top-.215,.074,.075)]
for z,rx,ry in levels:
    for i in range(rings):
        a=2*math.pi*i/rings
        # Open front collar with a V notch; the head and throat stay visible.
        notch=.014*max(0,-math.sin(a))**12 if z>top-.27 else 0
        verts.append((rx*math.cos(a),ry*1.3*math.sin(a)-.045,z-notch))
for j in range(len(levels)-1):
    for i in range(rings):
        n=(i+1)%rings;faces.append((j*rings+i,j*rings+n,(j+1)*rings+n,(j+1)*rings+i))
me=bpy.data.meshes.new('Mira tailored jacket');me.from_pydata(verts,[],faces);me.update()
o=bpy.data.objects.new('Mira_Jacket',me);bpy.context.collection.objects.link(o);o.data.materials.append(cloth)
for f in me.polygons:f.use_smooth=True
solid=o.modifiers.new('Fabric thickness','SOLIDIFY');solid.thickness=.003
sub=o.modifiers.new('Tailored silhouette','SUBSURF');sub.levels=2
bpy.context.view_layer.update();coattree=BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get())
def coat_point(point):
    x,_,z=point
    for _ in range(10):
        hit,n,_,_=coattree.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        if hit is not None:return hit+n*.0008
        x*=.96
    raise RuntimeError('Piping missed coat: '+str(point))
for sign in [-1,1]:
    curve('Mira collar piping '+str(sign),[coat_point(v) for v in [(sign*.043,-.082,top-.235),(sign*.049,-.092,top-.27),
          (sign*.087,-.122,top-.307),(sign*.17,-.142,top-.34)]],.0008,cyan)
    curve('Mira shoulder seam '+str(sign),[coat_point(v) for v in [(sign*.07,-.130,top-.29),(sign*.115,-.131,top-.31),
          (sign*.195,-.121,top-.335),(sign*.25,-.074,top-.40)]],.0009,trim)

# Facial inlays follow the sculpted skin surface via ray projection.
bpy.context.view_layer.update();tree=BVHTree.FromObject(body,bpy.context.evaluated_depsgraph_get())
def skin_point(x,z):
    hit=None
    for _ in range(20):
        hit,normal,_,_=tree.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        if hit is not None:break
        x*=.94
    if hit is None:raise RuntimeError('Facial inlay missed skin: '+str((x,z)))
    return hit+normal*.0009
for sign in [-1,1]:
    # Fine temple/cheek contour, adapted from MiraSketch rather than a face grid.
    path=[(sign*.067,top-.085),(sign*.069,top-.12),(sign*.067,top-.154),
          (sign*.057,top-.187),(sign*.046,top-.215)]
    curve('Mira facial inlay '+str(sign),[skin_point(x,z) for x,z in path],.00050,cyan)
    path=[(sign*.068,top-.13),(sign*.058,top-.145),(sign*.061,top-.163)]
    curve('Mira temple branch '+str(sign),[skin_point(x,z) for x,z in path],.00037,cyan)

# Materials: restrained skin scattering and microstructure, wet eyes, matte hair.
for obj in bpy.context.scene.objects:
    if obj.type!='MESH':continue
    for face in obj.data.polygons:face.use_smooth=True
    for m in obj.data.materials:
        if not m or not m.use_nodes:continue
        p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if not p:continue
        if obj==body:
            p.inputs['Roughness'].default_value=.48
            p.inputs['Subsurface Weight'].default_value=.09
            p.inputs['Subsurface Radius'].default_value=(1,.35,.18)
            p.inputs['Subsurface Scale'].default_value=.008
            nodes=m.node_tree.nodes;links=m.node_tree.links
            noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=850
            noise.inputs['Detail'].default_value=2
            bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16
            bump.inputs['Distance'].default_value=.00012
            links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],p.inputs['Normal'])
        elif 'high-poly' in obj.name:
            p.inputs['Roughness'].default_value=.20
            p.inputs['Specular IOR Level'].default_value=.30
        elif 'short03' in obj.name:
            p.inputs['Roughness'].default_value=.66
            p.inputs['Specular IOR Level'].default_value=.18
            nodes=m.node_tree.nodes;links=m.node_tree.links
            original=p.inputs['Base Color'].links[0].from_socket if p.inputs['Base Color'].is_linked else None
            if original:
                multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
                multiply.inputs[2].default_value=(.30,.24,.21,1)
                links.new(original,multiply.inputs[1]);links.new(multiply.outputs[0],p.inputs['Base Color'])
            # Slightly shorten the lower/back silhouette for the sketch's cut.
            for v in obj.data.vertices:
                if v.co.z<top-.20:v.co.z=top-.20+(v.co.z-(top-.20))*.65
                if v.co.y<-.075 and v.co.x<.025 and top-.14<v.co.z<top-.045:
                    v.co.z=top-.045+(v.co.z-(top-.045))*.43

# Portrait crop under the tailored collar; the complete source body and its
# facial shape keys remain editable. Indexing is taken before masking.
masks=[m for m in body.modifiers if m.type=='MASK']
for m in masks:m.show_viewport=False
bpy.context.view_layer.update()
ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());raw=ev.to_mesh()
keep=[]
for i,v in enumerate(raw.vertices):
    co=ev.matrix_world@v.co
    if co.z>=top-.245 and (co.z>=top-.21 or abs(co.x)<.055):keep.append(i)
ev.to_mesh_clear()
for m in masks:m.show_viewport=True
group=body.vertex_groups.new(name='Mira portrait neck crop');group.add(keep,1,'REPLACE')
mask=body.modifiers.new('Mira portrait crop','MASK');mask.vertex_group=group.name

# Fine geometric strands break the smooth stock-hair silhouette.
hair=next(o for o in bpy.context.scene.objects if o.type=='MESH' and 'short03' in o.name)
bpy.context.view_layer.update();hairtree=BVHTree.FromObject(hair,bpy.context.evaluated_depsgraph_get())
strandmat=material('Mira | individual dark chestnut strands',(.028,.014,.009),.58)
random.seed(23)
strand_count=0
for i in range(14):
    pts=[];offset=(i/54-.5)*.027
    for j in range(25):
        t=j/24;x=.037-.110*t+offset*(.5+t*.5)
        z=top-.018-.039*t+.007*math.sin(t*math.pi)+offset*.25
        hit,n,_,_=hairtree.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        if hit is not None:pts.append(hit+n*.00015)
    if len(pts)>20:
        strand_count+=1
        strand=curve('Mira fine swept strand %02d'%i,pts,.000045,strandmat)
        for b in strand.data.splines[0].bezier_points:b.handle_left_type='VECTOR';b.handle_right_type='VECTOR'

# Keep the modelling rig and targets but exclude the unused body below shoulders from render.
for obj in bpy.context.scene.objects:
    if obj.type=='ARMATURE':obj.hide_render=True

scene=bpy.context.scene
for obj in list(scene.objects):
    if obj.type in ('LIGHT','CAMERA'):bpy.data.objects.remove(obj,do_unlink=True)
world=scene.world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.042,.048,.057,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.32
look=Vector((0,-.01,top-.19))
for name,pos,power,color,size in [('Soft key',(-.55,-.75,top+.35),18,(1,.88,.78),.8),
                                 ('Eye fill',(.38,-.65,top+.05),6,(.80,.89,1),.65),
                                 ('Hair rim',(.38,.25,top+.25),10,(.54,.77,.85),.6)]:
    bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.name=name
    l.data.energy=power;l.data.color=color;l.data.shape='DISK';l.data.size=size
    l.rotation_euler=(look-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.16,-1.10,top-.135));cam=bpy.context.object
cam.name='Mira portrait camera';cam.data.type='ORTHO';cam.data.ortho_scale=.47
cam.rotation_euler=(look-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(output/'Mira-Portrait-v02.blend'))
report={'status':'reference-led portrait revision, awaiting user review','reference':'Assets/SpaceMiner/AvatarLab/MiraSketch.png',
        'targets':settings,'hair':'MakeHuman short03, CC0, shortened lower silhouette',
        'custom_assets':['tailored jacket','collar piping','projected facial inlays'],
        'fine_geometric_strands':strand_count,
        'skin':'texture + procedural micro-bump + subsurface scattering','render':'Cycles 48 samples, denoised',
        'animation':'existing shape keys preserved; phoneme sync unchanged'}
(output/'portrait-v02-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene.render.filepath=str(output/'Mira-Portrait-v02.png');bpy.ops.render.render(write_still=True)
cam.location=(.43,-.94,top-.135);cam.rotation_euler=(look-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(output/'Mira-Portrait-v02-Side.png');bpy.ops.render.render(write_still=True)
print('MIRA PORTRAIT V02 RENDERED',flush=True)
