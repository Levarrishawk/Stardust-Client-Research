"""Independent original-byte detail0/detail1 closure audit, reusing established parsers."""
import argparse
import contextlib
import hashlib
import json
import struct
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_creature_template_profile import parse
from verify_skeletal_descriptors import sat, mlod, strings
from verify_skeletal_geometry import inspect
from verify_skeleton_bind_pose import skeletons
from verify_skeletal_materials import verify

CONFIG = '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
OBJECT = 'E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
SAT = '951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C'

def audit(root, configuration, output, details=(0,1)):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper() == CONFIG
    assert output.stat().st_size < 4*1048576
    assert any(line.startswith('PASS R1-ASSETS-01:') for line in output.read_text(encoding='utf-8-sig').splitlines())
    runs = {d:dict(sources={},geometry={},materials={},textures={}) for d in details}
    material_detail = None
    for line in output.read_text(encoding='utf-8-sig').splitlines():
        if '=' not in line: continue
        tag,value = line.split('=',1)
        if tag in ('lod_materials_begin','lod_materials_end'):
            material_detail = int(value) if tag.endswith('begin') else None
        elif tag == 'lod_source':
            r=json.loads(value); s=r['source']; assert s['Path'] not in runs[r['detail']]['sources']; runs[r['detail']]['sources'][s['Path']]=s
        elif tag == 'lod_descriptor':
            r=json.loads(value); assert 'descriptor' not in runs[r['detail']]; runs[r['detail']]['descriptor']=r
        elif tag == 'lod_geometry':
            r=json.loads(value); assert r['path'] not in runs[r['detail']]['geometry']; runs[r['detail']]['geometry'][r['path']]=r
        elif tag in ('skeletal_material','skeletal_texture'):
            assert material_detail is not None
            r=json.loads(value); group='materials' if tag=='skeletal_material' else 'textures'; assert r['path'] not in runs[material_detail][group]; runs[material_detail][group][r['path']]=r
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(root,configuration)]
        raw={}
        for run in runs.values():
            for path,pin in run['sources'].items():
                if path not in raw:
                    for priority,index in indices:
                        data=index.lookup(path)
                        if data is not None: raw[path]=(priority,data); break
                    else: raise ValueError('Missing original source '+path)
                priority,data=raw[path]
                assert priority==pin['Priority'] and len(data)==pin['Bytes'] and hashlib.sha256(data).hexdigest().upper()==pin['Sha256']
    summaries=[]
    for detail,run in runs.items():
        desc=run['descriptor']; sources=run['sources']; data={p:raw[p][1] for p in sources}
        assert set(desc['counts'])==set(sources) and set(desc['counts'].values())=={1}
        assert sources[desc['RootPath']]['Sha256']==OBJECT and sources[desc['AppearancePath']]['Sha256']==SAT
        expected=set(); path=desc['RootPath']; chain=[]; resolved_appearance=None
        while path:
            assert path not in expected; expected.add(path); chain.append(path)
            node,=nodes(data[path]); path,layers=parse(node)
            value=layers['SHOT'][1].get('appearanceFilename')
            if resolved_appearance is None and value is not None and value[0]==1: resolved_appearance=strings(value[1:],1)[0]
        assert resolved_appearance==desc['AppearancePath']
        appearance=sat(data[desc['AppearancePath']]); assert appearance==desc['Appearance']; expected.add(desc['AppearancePath'])
        selected=set()
        for path in appearance['MeshGenerators']:
            expected.add(path); lod=mlod(data[path]); assert lod==desc['LodMeshes'][path]
            selected.add(lod['SerializedPaths'][:4][detail])
        assert selected==set(run['geometry']); expected |= selected
        joints=[]
        for reference in appearance['Skeletons']:
            path=reference['Path']; expected.add(path); names,parents,arrays=skeletons(data[path])[0]
            if reference['AttachmentTransform']: assert sum(j['Name'].lower()==reference['AttachmentTransform'].lower() for j in joints)==1
            joints.extend(dict(SkeletonPath=path,Name=n.decode('ascii')) for n in names)
        shader_paths=set()
        for path,r in run['geometry'].items():
            fields=r.copy()
            for key in ('detail','skinSha256','positions','weights','normals','TransformNames','SkeletonPaths','joints','jointNames','bindError'): fields.pop(key)
            assert fields==dict(path=path,**inspect(data[path]))
            node,=nodes(data[path]); version,=node[1]; parts=version[1]; prefix=dict(parts[:7]); info=struct.unpack('<9i4h',prefix[b'INFO'])
            paths=strings(prefix[b'SKTM'],info[2]); names=strings(prefix[b'XFNM'],info[3]); assert paths==r['SkeletonPaths'] and names==r['TransformNames']
            canonical=b''.join(prefix[t] for t in (b'INFO',b'SKTM',b'XFNM',b'POSN',b'TWHD',b'TWDT',b'NORM'))
            assert hashlib.sha256(canonical).hexdigest().upper()==r['skinSha256']
            assert (r['positions'],r['weights'],r['normals'])==(info[4],info[5],info[6])
            counts=list(struct.unpack('<'+'I'*info[4],prefix[b'TWHD'])); weights=list(struct.iter_unpack('<If',prefix[b'TWDT'])); assert sum(counts)==len(weights)
            cursor=0
            for count in counts:
                group=weights[cursor:cursor+count]; cursor+=count; assert 0<count<=64 and abs(sum(v for _,v in group)-1)<=.0001
                assert all(i<len(names) and 0<=v<=1 for i,v in group)
            assert r['jointNames']==joints
            mapping=[]
            for name in names:
                matches=[i for i,j in enumerate(joints) if j['SkeletonPath'] in paths and j['Name'].lower()==name.lower()]; assert len(matches)==1; mapping.append(matches[0])
            assert mapping==r['joints'] and r['bindError']<=.00001
            shader_paths.update(dict(records)[b'NAME'][:-1].decode('ascii') for tag,records in parts if tag==b'PSDT')
        assert shader_paths==set(run['materials'])
        material_result=verify(data,run['materials'],run['textures'])
        expected |= shader_paths | set(run['textures']) | {m['effect'] for m in run['materials'].values()}
        assert expected==set(sources)
        summaries.append(dict(detail=detail,sources=len(sources),source_bytes=sum(s['Bytes'] for s in sources.values()),selected=sorted(selected),positions=sum(r['positions'] for r in run['geometry'].values()),weights=sum(r['weights'] for r in run['geometry'].values()),normals=sum(r['normals'] for r in run['geometry'].values()),tangents=sum(r['tangents'] for r in run['geometry'].values()),triangles=sum(r['triangles'] for r in run['geometry'].values()),materials=material_result))
    assert len(set().union(*(set(run['geometry']) for run in runs.values())))==sum(len(run['geometry']) for run in runs.values())
    return dict(runs=summaries,source_identity=True,selection_and_joint_mapping=True,geometry_and_material_agreement=True)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__); p.add_argument('root',type=Path); p.add_argument('configuration',type=Path); p.add_argument('native_output',type=Path); a=p.parse_args()
    print(json.dumps(audit(a.root,a.configuration,a.native_output),indent=2))
