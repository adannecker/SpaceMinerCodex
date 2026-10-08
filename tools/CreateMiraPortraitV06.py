"""Reference-led Mira portrait revision; native editable geometry, no generated picture."""
from pathlib import Path
import bpy, bmesh, math, json, random
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
settings={'head/head-oval':.40,'chin/chin-width-decr':.40,
          'chin/chin-height-decr':.05,'cheek/l-cheek-volume-incr':.05,
          'cheek/r-cheek-volume-incr':.05,'cheek/l-cheek-bones-incr':.48,
          'cheek/r-cheek-bones-incr':.46,'nose/nose-width1-decr':.20,
          'nose/nose-width2-decr':.10,'nose/nose-point-width-decr':.13,
          'nose/nose-scale-vert-incr':.10,'nose/nose-scale-depth-incr':.16,
          'nose/nose-curve-concave':.08,'nose/nose-point-down':.06,
          'mouth/mouth-upperlip-volume-incr':.25,
          'mouth/mouth-lowerlip-volume-incr':.19,'mouth/mouth-angles-up':.30,
          'eyes/l-eye-scale-incr':.10,'eyes/r-eye-scale-incr':.10,
          'eyes/l-eye-height3-decr':.10,'eyes/r-eye-height3-decr':.10,
          'eyes/l-eye-corner2-up':.12,'eyes/r-eye-corner2-up':.12,
          'eyebrows/eyebrows-angle-up':.16,
          'cheek/l-cheek-trans-up':.10,'cheek/r-cheek-trans-up':.10,
          'cheek/l-cheek-inner-decr':.08,'cheek/r-cheek-inner-decr':.08,
          'neck/neck-scale-horiz-decr':.07,
          'torso/measure-shoulder-dist-decr':.07,
          'arms/l-upperarm-shoulder-muscle-decr':.05,
          'arms/r-upperarm-shoulder-muscle-decr':.05}
for name,value in settings.items():
    path=targetroot/(name+'.target.gz')
    assert path.exists(),path
    TargetService.load_target(body,str(path),weight=value,name='MiraDesign_'+path.stem)
keys=body.data.shape_keys.key_blocks
for pattern,value in [('mouthSmileLeft',.42),('mouthSmileRight',.32),('jawOpen',0)]:
    for k in keys:
        if pattern.lower() in k.name.lower():k.value=value
HumanService.refit(body)
for obj in list(bpy.context.scene.objects):
    if 'short04' in obj.name or 'female_casualsuit01' in obj.name or 'eyebrow001' in obj.name or 'eyelashes01' in obj.name:
        bpy.data.objects.remove(obj,do_unlink=True)
for mod in list(body.modifiers):
    if mod.name.startswith('Delete.'):
        body.modifiers.remove(mod)
HumanService.add_mhclo_asset(AssetService.find_asset_absolute_path('short03.mhclo',asset_subdir='hair'),body,asset_type='Hair',material_type='GAMEENGINE')
HumanService.add_mhclo_asset(AssetService.find_asset_absolute_path('eyebrow005.mhclo',asset_subdir='eyebrows'),body,asset_type='Eyebrows',material_type='GAMEENGINE')
HumanService.add_mhclo_asset(AssetService.find_asset_absolute_path('eyelashes04.mhclo',asset_subdir='eyelashes'),body,asset_type='Eyelashes',material_type='GAMEENGINE')

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

# Derive the garment directly from the evaluated anatomy. This preserves the
# neck/shoulder attachment instead of estimating an oversized elliptical cone.
bpy.context.view_layer.update();ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get())
raw=ev.to_mesh();me=raw.copy();me.name='Mira anatomy fitted jacket'
for v in me.vertices:v.co=ev.matrix_world@v.co
ev.to_mesh_clear()
bm=bmesh.new();bm.from_mesh(me)
upper=top-.217;lower=top-.425
for origin,normal,clear_outer,clear_inner in [((0,0,upper),(0,0,1),True,False),
        ((0,0,lower),(0,0,1),False,True),((.195,0,0),(1,0,0),True,False),
        ((-.195,0,0),(1,0,0),False,True)]:
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,
        plane_co=origin,plane_no=normal,clear_outer=clear_outer,clear_inner=clear_inner)
bm.normal_update()
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,
    plane_co=(0,0,0),plane_no=(1,0,0),clear_outer=False,clear_inner=False)
bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=1,use_grid_fill=True)
for v in bm.verts:
    v.co.x*=1.045
    v.co.y=-.045+(v.co.y+.045)*1.10
    flare=max(0,min(1,(v.co.z-(upper-.035))/.035))
    radial=Vector((v.co.x,v.co.y+.045,0))
    if radial.length: v.co+=radial.normalized()*(.0045*flare)

# Cut a real, broad front V instead of stretching a tiny notch in the rim.
v_depth=.067;v_halfwidth=.031
def neckline(co):
    front=max(0,min(1,(-.025-co.y)/.05))
    return upper-v_depth*max(0,1-abs(co.x)/v_halfwidth)*front
clipped=[]
for face in bm.faces:
    polygon=[v.co.copy() for v in face.verts];result=[]
    for a,b in zip(polygon,polygon[1:]+polygon[:1]):
        da=a.z-neckline(a);db=b.z-neckline(b)
        if da<=0:result.append(a)
        if (da<=0)!=(db<=0):result.append(a+(b-a)*(da/(da-db)))
    if len(result)>=3:clipped.append(result)
clean=bpy.data.meshes.new('Mira fitted garment clean surface')
points=[];polys=[]
for polygon in clipped:
    polys.append(list(range(len(points),len(points)+len(polygon))));points.extend(tuple(v) for v in polygon)
clean.from_pydata(points,[],polys);bm.free()
weld=bmesh.new();weld.from_mesh(clean)
bmesh.ops.remove_doubles(weld,verts=list(weld.verts),dist=.000002)
bmesh.ops.recalc_face_normals(weld,faces=list(weld.faces));weld.to_mesh(clean);weld.free()
# Raise the lateral/back collar into a short open stand collar.
for v in clean.vertices:
    near_rim=max(0,min(1,(v.co.z-(upper-.040))/.040))
    side=max(0,min(1,(abs(v.co.x)-.017)/.035))
    back=max(0,min(1,(v.co.y+.075)/.055))
    v.co.z+=.013*near_rim*max(side,back)
clean.update();me=clean
o=bpy.data.objects.new('Mira_Jacket',me);bpy.context.collection.objects.link(o)
o.data.materials.append(cloth);o.data.materials.append(lining)
for f in me.polygons:f.use_smooth=True
solid=o.modifiers.new('Fabric thickness','SOLIDIFY');solid.thickness=.0012
solid.material_offset=1;solid.material_offset_rim=1
# The fitted surface already follows the anatomy. Subdivision would shrink
# it through the skin and create visible intersections.
bpy.context.view_layer.update();coattree=BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get())
def coat_point(point):
    x,_,z=point
    for _ in range(10):
        hit,n,_,_=coattree.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        if hit is not None:return hit+n*.0008
        x*=.96
    raise RuntimeError('Piping missed coat: '+str(point))
for sign in [-1,1]:
    points=[]
    for x in [.030,.025,.019,.012,.005,0]:
        z=upper-v_depth*max(0,1-x/v_halfwidth)-.0010
        points.append(coat_point((sign*x,-.1,z)))
    curve('Mira V collar edge '+str(sign),points,.00055,cyan)
    curve('Mira shoulder seam '+str(sign),[coat_point(v) for v in [(sign*.055,-.130,top-.29),(sign*.082,-.131,top-.31),
          (sign*.13,-.121,top-.335),(sign*.175,-.074,top-.385)]],.00075,trim)

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
            nodes=m.node_tree.nodes;links=m.node_tree.links
            original=p.inputs['Base Color'].links[0].from_socket if p.inputs['Base Color'].is_linked else None
            if original:
                tint=nodes.new('ShaderNodeHueSaturation');tint.inputs['Hue'].default_value=.70
                tint.inputs['Saturation'].default_value=.35
                links.new(original,tint.inputs['Color']);links.new(tint.outputs[0],p.inputs['Base Color'])
        elif 'eyebrow' in obj.name:
            nodes=m.node_tree.nodes;links=m.node_tree.links
            original=p.inputs['Base Color'].links[0].from_socket if p.inputs['Base Color'].is_linked else None
            if original:
                dark=nodes.new('ShaderNodeMixRGB');dark.blend_type='MULTIPLY';dark.inputs[0].default_value=1
                dark.inputs[2].default_value=(.40,.36,.33,1)
                links.new(original,dark.inputs[1]);links.new(dark.outputs[0],p.inputs['Base Color'])
        elif 'short03' in obj.name:
            p.inputs['Roughness'].default_value=.66
            p.inputs['Specular IOR Level'].default_value=.18
            nodes=m.node_tree.nodes;links=m.node_tree.links
            original=p.inputs['Base Color'].links[0].from_socket if p.inputs['Base Color'].is_linked else None
            if original:
                multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
                multiply.inputs[2].default_value=(.15,.16,.17,1)
                links.new(original,multiply.inputs[1]);links.new(multiply.outputs[0],p.inputs['Base Color'])
            # Slightly shorten the lower/back silhouette for the sketch's cut.
            for v in obj.data.vertices:
                if v.co.z<top-.20:v.co.z=top-.20+(v.co.z-(top-.20))*.68
                if v.co.y<-.075 and top-.14<v.co.z<top-.045:
                    v.co.z=top-.045+(v.co.z-(top-.045))*.48
                    v.co.x-=.014*max(0,1-abs(v.co.x)/.09)
                if v.co.z>top-.045:
                    v.co.z+=.004+.003*math.sin(v.co.x*65+v.co.y*27)
                # Coherent waves break up the helmet-like stock surface.
                weight=max(0,min(1,(v.co.z-(top-.11))/.08))
                wave=.0005*math.sin(v.co.x*180+v.co.y*72)
                v.co.z+=weight*wave
                v.co.y-=weight*.0008*math.sin(v.co.x*180+v.co.y*72)
            noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=220
            bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18
            bump.inputs['Distance'].default_value=.00035
            links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],p.inputs['Normal'])

# Portrait crop under the tailored collar; the complete source body and its
# facial shape keys remain editable. Indexing is taken before masking.
masks=[m for m in body.modifiers if m.type=='MASK']
for m in masks:m.show_viewport=False
bpy.context.view_layer.update()
ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());raw=ev.to_mesh()
keep=[]
for i,v in enumerate(raw.vertices):
    co=ev.matrix_world@v.co
    # Skin beneath the opaque outfit is not exported/rendered. Keep enough
    # throat under the shallow front notch for a continuous neck connection.
    if co.z>=upper-v_depth-.012 and (co.z>=upper or abs(co.x)<.085):keep.append(i)
ev.to_mesh_clear()
for m in masks:m.show_viewport=True
group=body.vertex_groups.new(name='Mira portrait neck crop');group.add(keep,1,'REPLACE')
mask=body.modifiers.new('Mira portrait crop','MASK');mask.vertex_group=group.name

# Fine geometric strands break the smooth stock-hair silhouette.
hair=next(o for o in bpy.context.scene.objects if o.type=='MESH' and 'short03' in o.name)
bpy.context.view_layer.update();hairtree=BVHTree.FromObject(hair,bpy.context.evaluated_depsgraph_get())
strandmat=material('Mira | individual dark chestnut strands',(.009,.004,.002),.85)
random.seed(23)
strand_count=0
# Separate waved locks with taper and fine filaments, rather than a flat bundle.
for i in range(58):
    pts=[];offset=(i/57-.5)*.065
    length=random.uniform(.55,1.12);lift=random.uniform(.0003,.0014)
    zspread=random.uniform(-.035,.018)
    phase=random.uniform(0,math.pi*2)
    for j in range(26):
        t=j/25
        x=.050-.126*t*length+offset
        z=top-.014-.045*t+.013*math.sin(math.pi*t)+offset*.40+zspread
        hit,n,_,_=hairtree.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        if hit is not None:
            wave=lift*math.sin(math.pi*t)**2
            pts.append(hit+n*(.0005+wave)+Vector((.0015*math.sin(t*7+phase),0,.001*math.sin(t*9+phase))))
    if len(pts)>17:
        strand_count+=1
        strand=curve('Mira waved lock %02d'%i,pts,random.uniform(.000055,.00010),strandmat)
        for j,b in enumerate(strand.data.splines[0].bezier_points):
            t=j/(len(pts)-1);b.radius=max(.08,math.sin(math.pi*t)**.6)
        for k in range(2):
            fine=curve('Mira lock filament %02d-%d'%(i,k),
                [v+Vector(((k-1)*.00010,-.00008,.00005)) for v in pts],.000020,strandmat)
            for j,b in enumerate(fine.data.splines[0].bezier_points):
                b.radius=max(.05,math.sin(math.pi*j/(len(pts)-1)))

# Keep the modelling rig and targets but exclude the unused body below shoulders from render.
for obj in bpy.context.scene.objects:
    if obj.type=='ARMATURE':obj.hide_render=True

scene=bpy.context.scene
for obj in list(scene.objects):
    if obj.type in ('LIGHT','CAMERA'):bpy.data.objects.remove(obj,do_unlink=True)
world=scene.world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.042,.048,.057,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.32
look=Vector((0,-.01,top-.15))
for name,pos,power,color,size in [('Soft key',(-.55,-.75,top+.35),18,(1,.88,.78),.8),
                                 ('Eye fill',(.38,-.65,top+.05),6,(.80,.89,1),.65),
                                 ('Hair rim',(.38,.25,top+.25),10,(.54,.77,.85),.6)]:
    bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.name=name
    l.data.energy=power;l.data.color=color;l.data.shape='DISK';l.data.size=size
    l.rotation_euler=(look-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.16,-1.10,top-.12));cam=bpy.context.object
cam.name='Mira portrait camera';cam.data.type='ORTHO';cam.data.ortho_scale=.40
cam.rotation_euler=(look-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(output/'Mira-Portrait-v06.blend'))
report={'status':'reference-led portrait revision, awaiting user review','reference':'Assets/SpaceMiner/AvatarLab/MiraSketch.png',
        'targets':settings,'hair':'MakeHuman short03, CC0, raised and swept fringe, uneven crown and micro-bump',
        'custom_assets':['anatomy fitted jacket, real front V and 4.5mm upper rim flare','dark inner collar lining','collar piping','projected facial inlays'],
        'collar':{'v_depth_m':v_depth,'v_halfwidth_m':v_halfwidth,'upper_flare_m':.0045,'stand_collar_raise_m':.013,'fabric_m':.0012},
        'waved_geometric_locks':strand_count,'eyelashes':'MakeHuman eyelashes04 CC0, refitted to current face',
        'skin':'texture + procedural micro-bump + subsurface scattering','render':'Cycles 48 samples, denoised',
        'animation':'existing shape keys preserved; phoneme sync unchanged'}
(output/'portrait-v06-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene.render.filepath=str(output/'Mira-Portrait-v06.png');bpy.ops.render.render(write_still=True)
cam.location=(.43,-.94,top-.12);cam.rotation_euler=(look-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(output/'Mira-Portrait-v06-Side.png');bpy.ops.render.render(write_still=True)
print('MIRA PORTRAIT V06 RENDERED',flush=True)
