"""Independently verify a pinned portal cell scene and its complete static geometry graph."""
import argparse,contextlib,hashlib,subprocess,re,io,struct,json,math
from pathlib import Path
from cmpa_corpus_inventory import archives,parse as component
from dtla_corpus_inventory import parse as detail
from verify_textured_mesh_bundle import Index,name
from verify_dds_pixels import inspect as dds
from verify_customizable_shaders import parse as customizable
from verify_portal_layout import inspect as portal
from verify_static_mesh_slice import inspect as mesh,nodes
from verify_floor_mesh import inspect as floor
from verify_path_graph import inspect as path_graph
from verify_floor_queries import read_geometry,reference,f32,cross,sub

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('member');p.add_argument('sha256');p.add_argument('core_dll',type=Path)
    p.add_argument('--appearance-cells');p.add_argument('--floors',action='store_true');p.add_argument('--paths',action='store_true');p.add_argument('--textures',action='store_true');p.add_argument('--authored-defaults',action='store_true');p.add_argument('--surface-paths',action='store_true');p.add_argument('--materials-only',action='store_true');p.add_argument('--source-manifest',type=Path);a=p.parse_args()
    if a.materials_only and (a.floors or a.paths or a.surface_paths):p.error('Material-only probe cannot request floor/path traversal')
    if a.source_manifest:a.source_manifest.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve())
    a.textures=a.textures or a.authored_defaults or a.materials_only;a.paths=a.paths or a.textures and not a.materials_only or a.surface_paths;a.floors=a.floors or a.paths;sources={};meshes={};floors={}
    selected=None if a.appearance_cells is None else [int(c) for c in a.appearance_cells.split(',')]
    if selected is not None and (len(selected)!=len(set(selected)) or any(c<0 for c in selected)):p.error('Invalid selected cell set')
    assert not(a.surface_paths and a.textures),'Choose surface/path or material comparison'
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        def read(path):
            if path in sources:return sources[path][1]
            for priority,index in indices:
                value=index.lookup(path)
                if value is not None:
                    assert value,'Masked source';sources[path]=(priority,value);return value
            raise ValueError('Missing source '+path)
        root=read(a.member);assert hashlib.sha256(root).hexdigest().upper()==a.sha256.upper()
        layout=portal(root)
        if selected is not None:assert all(c<len(layout['cells']) for c in selected)
        def walk(path,parents=()):
            if not path.startswith('appearance/'):path='appearance/'+path
            assert path not in parents and len(parents)<32
            value=read(path);family=value[8:12]
            if family==b'MESH':meshes[path]=mesh(value);return
            if family==b'DTLA':children=detail(value)[3]
            elif family==b'CMPA':children=[name for name,_ in component(value)[1]]
            elif family==b'APT ':children=[nodes(value)[0][1][0][1][0][1].rstrip(b'\0').decode('ascii')]
            else:raise ValueError('Unsupported scene family '+repr(family))
            for child in children:walk(child,parents+(path,))
        for ci,cell in enumerate(layout['cells']):
            if selected is not None and ci not in selected:continue
            if cell['appearance']:walk(cell['appearance'])
        if a.floors:
            for ci,cell in enumerate(layout['cells']):
                if cell['floor']:floors[ci]=floor(read(cell['floor']))[0]
        geometry_source_count=len(sources);material_meshes=set();texture_paths=set();expected_mips={};omissions=[];effect_lines=set()
        if a.textures:
            for mesh_path in meshes:
                root_mesh,=nodes(read(mesh_path));version,=root_mesh[1];sps=version[1][1];sps_version,=sps[1]
                shaders=dict.fromkeys(name(group[1][0][1]) for group in sps_version[1][1:])
                mesh_textures=set();mesh_effect_lines=set();supported=True
                for shader_path in shaders:
                    shader_bytes=read(shader_path);root_shader,=nodes(shader_bytes);overrides={}
                    if root_shader[0]==b'CSHD':
                        if not a.authored_defaults:omissions.append((mesh_path,'CSHD'));supported=False;break
                        description=customizable(shader_bytes)
                        for tag,first,count,variable,flag,default in description['selections']:overrides[tag]=description['choices'][first+min(max(default,0),count-1)]
                        cv,=root_shader[1];root_shader=cv[1][0]
                    assert root_shader[0]==b'SSHT'
                    shader_version,=root_shader[1]
                    effect,=[value for tag,value in shader_version[1] if tag==b'NAME'];effect_path=name(effect)
                    if '/' not in effect_path:effect_path='effect/'+effect_path
                    root_effect,=nodes(read(effect_path));assert root_effect[0]==b'EFCT'
                    effect_version,=root_effect[1];assert effect_version[0]==b'0001'
                    for ii,(_,implementation) in enumerate(effect_version[1][1:]):
                        iv,=implementation;assert iv[0] in (b'0004',b'0005',b'0006',b'0007')
                        info=next(value for tag,value in iv[1] if tag==b'DATA');phase=struct.unpack_from('<I',info,1)[0]
                        mesh_effect_lines.add(f'portal_effect={effect_path} implementation={ii} version={iv[0].decode()} phase={phase:08X} flag5={info[5] if len(info)==7 else "absent"} flag6={info[6] if len(info)==7 else "absent"}')
                        pi=0
                        for tag,passes in iv[1]:
                            if tag==b'PASS':
                                pv,=passes;assert pv[0] in (b'0008',b'0009');payload=pv[1][0][1]
                                # Independent serialized order: 0008 has no 4D or 64/68/6C/70 values.
                                offsets=[(o,1) for o in (0x10,0x14,0x18,0x19,0x1a,0x1c,0x20,0x24,0x28,0x2c,0x30)]+[(0x34,4),(0x38,1),(0x3c,1),(0x40,4),(0x44,4),(0x48,4),(0x4c,1)]
                                if pv[0]==b'0009':offsets.append((0x4d,1))
                                offsets += [(0x50,4)]+[(o,1) for o in (0x54,0x58,0x5c,0x60)]
                                if pv[0]==b'0009':offsets += [(o,1) for o in (0x64,0x68,0x6c,0x70)]
                                offsets += [(o,4) for o in (0x74,0x78,0x7c)]
                                cursor=1;fields=[]
                                for offset,width in offsets:
                                    value=int.from_bytes(payload[cursor:cursor+width],'little');cursor+=width;fields.append(f'{offset:02X}:{value:08X}')
                                assert cursor==len(payload)
                                mesh_effect_lines.add(f'portal_effect_pass={effect_path} implementation={ii} pass={pi} version={pv[0].decode()} stages={payload[0]} fields={",".join(fields)}');pi+=1
                    txms=next((value for tag,value in shader_version[1] if tag==b'TXMS'),[])
                    for _,children in txms:
                        binding,=children;texture,=[value for tag,value in binding[1] if tag==b'NAME']
                        binding_tag=struct.unpack_from('<I',binding[1][0][1])[0]
                        path=overrides.get(binding_tag,name(texture));read(path);mesh_textures.add(path)
                if supported:material_meshes.add(mesh_path);texture_paths.update(mesh_textures);effect_lines.update(mesh_effect_lines)
            for path in texture_paths:
                with contextlib.redirect_stdout(io.StringIO()):hashes=dds(read(path))
                for level,digest in enumerate(hashes):expected_mips[(path,level)]=digest
        command=['dotnet',str(a.core_dll),'--portal-materials-defaults' if a.materials_only and a.authored_defaults else '--portal-materials' if a.materials_only else '--portal-surface-paths' if a.surface_paths else '--portal-textures-defaults' if a.authored_defaults else '--portal-textures' if a.textures else '--portal-paths' if a.paths else '--portal-floors' if a.floors else '--portal-scene',str(a.root),'--configuration',str(a.configuration),'--member',a.member,'--expected-sha256',a.sha256]
        if selected is not None:command+=['--appearance-cells',a.appearance_cells]
        output=subprocess.run(command,capture_output=True,text=True,check=True,timeout=60).stdout
        if a.surface_paths:
            observed={(r['cell'],r['probe']):r['hit'] for r in (json.loads(line.split('=',1)[1]) for line in output.splitlines() if line.startswith('surface_path='))};expected={}
            for ci,cell in enumerate(layout['cells']):
                if not cell['floor']:continue
                data=read(cell['floor']);verts,tris=read_geometry(data);root_floor,=nodes(data);version,=root_floor[1];graph=next((value for tag,value in version[1] if tag==b'PGRF'),None)
                ns=path_graph(graph)[1] if graph is not None else None
                lo=f32(min(v[1] for v in verts)-1) if verts else 0;hi=f32(max(v[1] for v in verts)+1) if verts else 0
                for ti,(pa,pb,pc,tid,non_solid) in enumerate(tris):
                    if non_solid or cross(sub(pb,pa),sub(pc,pa))[1]==0:continue
                    x=f32((pa[0]+pb[0]+pc[0])/3);z=f32((pa[2]+pb[2]+pc[2])/3);hit=reference(tris,(x,hi,z),(x,lo,z),False)
                    if hit is None:expected[ci,ti]=None;continue
                    point=tuple(f32(v) for v in hit['position']);node=min(range(len(ns)),key=lambda i:sum((ns[i][4+j]-point[j])**2 for j in range(3))) if ns else None
                    expected[ci,ti]=dict(cell=ci,triangle=hit['triangle'],node=node,status='MissingCellGraph' if ns is None else 'EmptyCellGraph' if not ns else 'Selected',position=point)
            assert observed.keys()==expected.keys(),'Surface/path probe cardinality'
            for key,e in expected.items():
                n=observed[key]
                if e is None:assert n is None
                else:
                    assert n is not None and all(e[k]==n[k] for k in ('cell','triangle','node','status')),(key,e,n)
                    assert all(math.isclose(x,y,rel_tol=1e-6,abs_tol=1e-5) for x,y in zip(e['position'],n['position']))
            print(f'PASS independent_surface_path probes={len(expected)} hits={sum(v is not None for v in expected.values())} same_cell_nodes={sum(v is not None and v["node"] is not None for v in expected.values())}')
        loaded=sum(bool(c['appearance']) and (selected is None or ci in selected) for ci,c in enumerate(layout['cells']))
        assert f"portal_scene cells={len(layout['cells'])} loaded={loaded} sources={geometry_source_count} diagnostics=0" in output
        if a.textures:
            actual_effect_lines=set(line for line in output.splitlines() if line.startswith(('portal_effect=','portal_effect_pass=')))
            assert effect_lines==actual_effect_lines,('Effect field disagreement',sorted(effect_lines-actual_effect_lines)[:1],sorted(actual_effect_lines-effect_lines)[:1])
            actual={(path,int(level)):digest for path,level,digest in re.findall(r'^portal_texture=(\S+) mip=(\d+) sha256=([0-9A-F]{64})$',output,re.MULTILINE)}
            assert actual==expected_mips,'Texture pixel/mip disagreement'
            assert set(re.findall(r'^portal_material_mesh=(\S+)$',output,re.MULTILINE))==material_meshes
            assert f'portal_materials sources={len(sources)} meshes={len(material_meshes)} textures={len(texture_paths)} mips={len(expected_mips)} diagnostics={len(omissions)}' in output
            print(f'PASS textured_meshes={len(material_meshes)} textures={len(texture_paths)} matched_mips={len(expected_mips)} effect_records={len(effect_lines)} explicit_customizable_omissions={len(omissions)}')
        for path,(priority,value) in sources.items():
            assert f'source={path} priority={priority} bytes={len(value)} sha256={hashlib.sha256(value).hexdigest().upper()}' in output
        for path,value in meshes.items():
            assert f"portal_mesh={path} vertices={value['vertices']} triangles={value['triangles']} geometry_sha256={value['geometry_sha256']} sorted_sha256={value['sorted_indices_sha256']}" in output
        if a.paths:
            graphs={}
            def read_graph(data):
                r,=nodes(data);v,=r[1];g=next((value for tag,value in v[1] if tag==b'PGRF'),None)
                return path_graph(g)[0] if g is not None else None
            graphs['layout']=read_graph(root)
            for ci,cell in enumerate(layout['cells']):
                if cell['floor']:graphs[ci]=read_graph(read(cell['floor']))
            for key,value in graphs.items():
                if value is not None:assert f"portal_graph={key} nodes={value['nodes']} edges={value['edges']} sha256={value['geometry_sha256']}" in output
            print(f'PASS path_graphs={sum(v is not None for v in graphs.values())}')
        if a.floors:
            assert f'portal_floors count={len(floors)}' in output
            for ci,value in floors.items():assert f"portal_floor={ci} vertices={value['vertices']} triangles={value['triangles']} sha256={value['geometry_sha256']}" in output
            print(f'PASS floors={len(floors)} triangles={sum(v["triangles"] for v in floors.values())}')
        if a.source_manifest:
            a.source_manifest.parent.mkdir(parents=True,exist_ok=True)
            a.source_manifest.write_text(json.dumps([dict(Path=path,Priority=priority,Bytes=len(value),Sha256=hashlib.sha256(value).hexdigest().upper()) for path,(priority,value) in sorted(sources.items())],indent=2)+'\n',encoding='utf-8')
        print(f"PASS cells={len(layout['cells'])} loaded={loaded} sources={len(sources)} mesh_templates={len(meshes)} vertices={sum(v['vertices'] for v in meshes.values())} triangles={sum(v['triangles'] for v in meshes.values())} sorted_variants={sum(v['sorted_variants'] for v in meshes.values())} sha256={a.sha256.upper()}")
if __name__=='__main__':main()
