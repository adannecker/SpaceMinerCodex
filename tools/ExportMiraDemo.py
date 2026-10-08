"""Export evaluated head/shoulder geometry with matching facial morphs to Unity JSON."""
from pathlib import Path
import bpy,json
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'docs/Art/Mira3D/Mira-Portrait-v10.blend'))
body=bpy.data.objects['Mira_AnatomicalBase']
out=root/'Assets/SpaceMiner/AvatarLab/Model3D';out.mkdir(parents=True,exist_ok=True)
keys=body.data.shape_keys.key_blocks
print('FACIAL KEYS',list(keys.keys()))
def key(pattern):
    return next((k for k in keys if pattern.lower() in k.name.lower()),None)
facekeys=[('JawOpen',key('jawOpen')),('BlinkLeft',key('eyeBlinkLeft')),('BlinkRight',key('eyeBlinkRight')),('Round',key('viseme_O'))]
if facekeys[0][1] is None: facekeys[0]=('JawOpen',key('viseme_aa'))
assert all(k is not None for n,k in facekeys[:3]), 'Missing mouth/eye shape keys'
# Consolidate decorative curves by material to avoid hundreds of draw calls.
groups={}
for obj in list(bpy.context.scene.objects):
    if obj.type=='CURVE':
        obj.data.resolution_u=1 if 'strand' in obj.data.materials[0].name else 3
        obj.data.bevel_resolution=0
        groups.setdefault(obj.data.materials[0].name,[]).append(obj)
for name,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join()
    bpy.context.object.name='Mira detail '+name.split('|')[-1].strip()

def geometry(obj, source=None):
    bpy.context.view_layer.update()
    evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
    mesh.calc_loop_triangles()
    matrix=evaluated.matrix_world
    coords=[matrix@v.co for v in mesh.vertices]
    if source is not None:
        positions=[[coords[i].x,coords[i].z,-coords[i].y] for i in source]
        evaluated.to_mesh_clear()
        return {'vertices':positions,'source':source}
    top=max(v.z for v in coords)
    # Only head/shoulders are exported, not a full unseen body or helper geometry.
    floor=1.15
    if obj==body: floor=top-.48
    bodytop=max((body.evaluated_get(bpy.context.evaluated_depsgraph_get()).matrix_world@v.co).z for v in body.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices)
    floor=bodytop-.48
    triangles=[t for t in mesh.loop_triangles if all(coords[v].z>=floor for v in t.vertices)]
    indices=[];positions=[];normals=[];uv=[];mapping={};source=[]
    uvdata=mesh.uv_layers.active.data if mesh.uv_layers.active else None
    for tri in triangles:
        # MakeHuman's cornea uses the lower-right atlas swatch and needs a
        # separate transparent material. Omit that shell in this first demo
        # so it does not hide the iris behind an opaque blue surface.
        if 'high-poly' in obj.name and uvdata and all(uvdata[i].uv.x>.8 and uvdata[i].uv.y<.2 for i in tri.loops):
            continue
        # Split UV seams and material boundaries while retaining stable topology for morphs.
        for vi,li in zip(tri.vertices,tri.loops):
            tuv=uvdata[li].uv[:] if uvdata else (0,0)
            token=(vi,tuple(round(x,6) for x in tuv))
            if token not in mapping:
                mapping[token]=len(positions);v=coords[vi];n=matrix.to_3x3()@mesh.vertices[vi].normal
                positions.append([v.x,v.z,-v.y]);normals.append([n.x,n.z,-n.y]);uv.append(list(tuv));source.append(vi)
            indices.append(mapping[token])
    evaluated.to_mesh_clear()
    return {'vertices':positions,'normals':normals,'uv':uv,'triangles':indices,'source':source}
meshes=[];materials=[]
for obj in list(bpy.context.scene.objects):
    if obj.type!='MESH':continue
    geo=geometry(obj)
    if not geo['triangles']:continue
    geo['name']=obj.name.replace('|','-');geo['morphs']=[]
    if obj==body:
        for name,k in facekeys:
            if k is None:continue
            original=k.value;k.value=1;shape=geometry(obj,geo['source']);k.value=original
            assert shape['source']==geo['source'],'Morph topology mismatch'
            delta=[[b-a for a,b in zip(v,w)] for v,w in zip(geo['vertices'],shape['vertices'])]
            geo['morphs'].append({'name':name,'delta':delta})
    if 'teeth_base' in obj.name or 'Mira_teeth_new' in obj.name:
        # Compress crown height slightly and carry the lower row with the jaw.
        split=1.269
        for v in geo['vertices']:
            if v[1]<split:v[1]=split+(v[1]-split)*.82
        delta=[[0,-.019 if v[1]<split else 0,-.003 if v[1]<split else 0] for v in geo['vertices']]
        geo['morphs'].append({'name':'JawOpen','delta':delta})
    if 'short03' in obj.name:
        for v in geo['vertices']:
            weight=max(0,min(1,(v[1]-1.33)/.10))
            v[1]+=weight*.004*__import__('math').sin(v[0]*120+v[2]*55)
            if v[1]>1.33:v[1]+=.004
            v[0]+=weight*.002*__import__('math').sin(v[1]*100+v[2]*70)
    mat=next((m for m in obj.data.materials if m),None)
    geo['color']=[1,1,1,1];geo['roughness']=.5
    geo['texture']='';geo['cutout']=any(s in obj.name.lower() for s in ['hair','lash','brow','short04','short03'])
    if mat and mat.use_nodes:
        shader=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if shader:
            geo['roughness']=shader.inputs['Roughness'].default_value
            if not shader.inputs['Base Color'].is_linked:geo['color']=list(shader.inputs['Base Color'].default_value)
            else:
                multiply=next((n for n in mat.node_tree.nodes if n.type=='MIX_RGB' and n.blend_type=='MULTIPLY'),None)
                if multiply:geo['color']=list(multiply.inputs[2].default_value)
            if 'short03' in obj.name:geo['color']=[.55,.50,.46,1]
        images=[n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
        diffuse=next((im for im in images if any(t in im.name.lower() for t in ['diff','texture','hair','brown','eye','young'])),images[0] if images else None)
        if diffuse:
            filename=obj.name.replace(' ','_')+'.png';diffuse.filepath_raw=str(out/filename);diffuse.file_format='PNG';diffuse.save()
            geo['texture']=filename
    del geo['source'];meshes.append(geo)
assert meshes and meshes[0]
report={'meshes':meshes,'source':'MakeHuman/MPFB core CC0','headTop':max(v[1] for m in meshes for v in m['vertices'])}
for m in meshes:
    for field in ('vertices','normals'):
        m[field]=[dict(zip(('x','y','z'),v)) for v in m[field]]
    m['uv']=[dict(zip(('x','y'),v)) for v in m['uv']]
    for shape in m['morphs']:shape['delta']=[dict(zip(('x','y','z'),v)) for v in shape['delta']]
(out/'mira-geometry.json').write_text(json.dumps(report,separators=(',',':')),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'docs/Art/Mira3D/Mira-Demo-Authoring.blend'))
print('MIRA DEMO EXPORTED',[(m['name'],len(m['vertices']),m['texture']) for m in meshes])
