"""Original four-detail closure plus independent CKAT sampled hierarchy/weighted-position audit."""
import argparse
import contextlib
import hashlib
import json
import math
import struct
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_skeletal_lod_selection import audit
from verify_skeletal_descriptors import sat
from verify_skeleton_bind_pose import skeletons, quaternion, rotation, multiply, pose
from verify_compressed_locomotion import verify_bank_routes
from ckat_float32_overshoot_probe import build_table, f32


def samples(data, frame, table):
    root,=nodes(data); assert root[0]==b'CKAT'; version,=root[1]; assert version[0]==b'0001'; fields=dict(version[1])
    rate,end,*_=struct.unpack('<f6h',fields[b'INFO']); assert 0<=frame<=end
    def packed(word,selectors):
        values=[]
        for axis,(value,selector) in enumerate(zip(((word>>21)&2047,(word>>10)&2047,word&1023),selectors)):
            assert 128<=selector<=254
            basis,scale11,scale10=table[selector]; sign=512 if axis==2 else 1024; offset=(value&(sign-1))*(scale10 if axis==2 else scale11)
            values.append(f32(basis-offset if value&sign else basis+offset))
        values.append(f32(math.sqrt(max(0,1-sum(v*v for v in values))))); norm=math.sqrt(sum(v*v for v in values)); return [f32(v/norm) for v in values]
    def interval(times):
        assert times and all(b>a for a,b in zip(times,times[1:]))
        if frame<=times[0]:return 0,0,0
        if frame>=times[-1]:return len(times)-1,len(times)-1,0
        low=max(i for i,t in enumerate(times) if t<=frame)
        return (low,low,0) if times[low]==frame else (low,low+1,f32((frame-times[low])/(times[low+1]-times[low])))
    def slerp(a,b,t):
        dot=sum(x*y for x,y in zip(a,b))
        if dot<0:b=[-v for v in b]; dot=-dot
        dot=min(1,dot)
        if dot>.999999:q=[(1-t)*x+t*y for x,y in zip(a,b)]
        else:
            angle=math.acos(dot); q=[(math.sin((1-t)*angle)*x+math.sin(t*angle)*y)/math.sin(angle) for x,y in zip(a,b)]
        norm=math.sqrt(sum(v*v for v in q)); return [v/norm for v in q]
    rotations=[]
    for tag,raw in fields[b'AROT']:
        assert tag==b'QCHN'; selectors=raw[2:5]; keys=list(struct.iter_unpack('<HI',raw[5:])); low,high,t=interval([time for time,_ in keys]); a=packed(keys[low][1],selectors); b=packed(keys[high][1],selectors); rotations.append(a if low==high else slerp(a,b,t))
    static=[packed(word,(x,y,z)) for x,y,z,word in struct.iter_unpack('<BBBI',fields[b'SROT'])]
    translations=[]
    for tag,raw in fields[b'ATRN']:
        keys=list(struct.iter_unpack('<Hf',raw[2:])); low,high,t=interval([time for time,_ in keys]); translations.append(keys[low][1] if low==high else (1-t)*keys[low][1]+t*keys[high][1])
    fixed=[v for v, in struct.iter_unpack('<f',fields[b'STRN'])]; result={}
    for tag,raw in fields[b'XFRM']:
        stop=raw.index(0); name=raw[:stop].decode('ascii'); animated,rot,mask,x,y,z=struct.unpack('<BHB3H',raw[stop+1:]); q=(rotations if animated else static)[rot]
        p=[(translations if mask&(8<<axis) else fixed)[index] for axis,index in enumerate((x,y,z))]; assert name.lower() not in result; result[name.lower()]=(q,p)
    return result


def hierarchy(raw,appearance):
    joints=[]
    for reference in appearance['Skeletons']:
        path=reference['Path']; basic=skeletons(raw[path])[0]; names,parents,arrays=basic; parent=-1
        if reference['AttachmentTransform']: parent,=[i for i,j in enumerate(joints) if j['name'].lower()==reference['AttachmentTransform'].lower()]
        inverses=pose(basic,None if parent<0 else joints[parent]['inverse']); start=len(joints)
        for source,name in enumerate(names):
            q=quaternion(quaternion(arrays[b'RPST'][source],arrays[b'BPRO'][source]),arrays[b'RPRE'][source]); local=rotation(q,arrays[b'BPTR'][source]); global_parent=parent if parents[source]<0 else start+parents[source]
            joints.append(dict(path=path,name=name.decode('ascii'),source=source,parent=global_parent,arrays=arrays,local=local,inverse=inverses[source]))
    assert len(joints)==53; return joints


def main(root,config,output):
    closure=audit(root,config,output,(0,1,2,3))
    rows={tag:[] for tag in ('lower_source','lower_descriptor','lower_clip','lower_pose','lower_positions')}
    text=output.read_text(encoding='utf-8-sig'); assert 'PASS R2-ASSETS-01:' in text
    for line in text.splitlines():
        if '=' in line:
            tag,value=line.split('=',1)
            if tag in rows: rows[tag].append(json.loads(value))
    sources={}; descriptors={}; clips={}; poses={}; positions={}
    for r in rows['lower_source']:
        key=r['detail'],r['source']['Path']; assert key not in sources; sources[key]=r['source']
    for r in rows['lower_descriptor']: assert r['detail'] not in descriptors; descriptors[r['detail']]=r
    for r in rows['lower_clip']:
        key=r['detail'],r['key']; assert key not in clips; clips[key]=r
    for r in rows['lower_pose']:
        key=r['detail'],r['key'],r['frame'],r['joint']; assert key not in poses; poses[key]=r
    for r in rows['lower_positions']:
        key=r['detail'],r['key'],r['frame'],r['path']; assert key not in positions; positions[key]=r
    assert set(descriptors)=={1,2,3} and set(clips)=={(d,k) for d in (1,2,3) for k in ('walk','run')}
    raw={}
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(root,config)]
        for (_,path),pin in sources.items():
            if path not in raw:
                for priority,index in indices:
                    data=index.lookup(path)
                    if data is not None: raw[path]=(priority,data); break
                else: raise ValueError('Missing animation closure source')
            priority,data=raw[path]; assert priority==pin['Priority'] and len(data)==pin['Bytes'] and hashlib.sha256(data).hexdigest().upper()==pin['Sha256']
    raw={p:data for p,(_,data) in raw.items()}
    image=(root/'Stardust.exe').read_bytes(); assert hashlib.sha256(image).hexdigest().upper()=='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'; table=build_table(image)
    expected_poses=set(); expected_positions=set(); maximum_matrix=maximum_position=movement=0.; vector_count=0; summaries=[]
    for detail,desc in descriptors.items():
        source_paths={p for d,p in sources if d==detail}; assert set(desc['counts'])==source_paths and set(desc['counts'].values())=={1}
        appearance=desc['Appearance']; assert appearance==sat(raw[next(p for p in source_paths if p.endswith('.sat'))]); joints=hierarchy(raw,appearance); selected=appearance['Skeletons'][0]['Path']
        routes=[]
        for key in ('walk','run'):
            r=clips[detail,key]; node,=nodes(raw[r['path']]); fields=dict(node[1][0][1]); rate,end,*_=struct.unpack('<f6h',fields[b'INFO']); speed,count=struct.unpack_from('<fh',fields[b'LOCT'])
            assert r['frames']==[0,.5,end/2,end] and r['FrameRate']==rate and r['EndFrame']==end
            routes.append(dict(r,AuthoredSpeed=speed,keys=count,poses=end*4+1,joints=53,root_motion_applied=False,event_dispatch=False))
        selected_clips=verify_bank_routes(raw,routes)
        mesh_paths={f'appearance/mesh/hum_m_{part}_l{detail}.mgn' for part in ('body','head')}
        expected_sources={p for p in raw if p.startswith('object/')}|{next(p for p in raw if p.endswith('.sat'))}|set(appearance['MeshGenerators'])|{r['Path'] for r in appearance['Skeletons']}|mesh_paths|{r['AnimationTablePath'] for r in appearance['AnimationTables']}|selected_clips|{p for p in source_paths if sources[detail,p]['Sha256']=='798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B'}
        assert source_paths==expected_sources
        for key in ('walk','run'):
            r=clips[detail,key]
            for frame in r['frames']:
                sampled=samples(raw[r['path']],frame,table); worlds=[]
                for i,j in enumerate(joints):
                    local=j['local']; source=j['source']; arrays=j['arrays']
                    if j['path']==selected and j['name'].lower() in sampled:
                        q,p=sampled[j['name'].lower()]; q=quaternion(arrays[b'RPST'][source],quaternion(quaternion([q[3],*q[:3]],arrays[b'BPRO'][source]),arrays[b'RPRE'][source])); norm=math.sqrt(sum(v*v for v in q)); local=rotation([v/norm for v in q],[a+b for a,b in zip(arrays[b'BPTR'][source],p)])
                    world=local if j['parent']<0 else multiply(worlds[j['parent']],local); worlds.append(world); pose_key=detail,key,frame,i; expected_poses.add(pose_key); native=poses[pose_key]
                    assert native['path']==j['path'] and native['source']==source
                    for name,matrix in (('local',local),('world',world)):
                        values=[v for row in matrix[:3] for v in row]; assert len(native[name])==12; error=max(abs(a-b) for a,b in zip(native[name],values)); maximum_matrix=max(maximum_matrix,error); assert error<2e-5
                for path in mesh_paths:
                    node,=nodes(raw[path]); fields=dict(node[1][0][1][:6]); declared=[p.decode('ascii') for p in fields[b'SKTM'].split(b'\0')[:-1]]; names=[n.decode('ascii') for n in fields[b'XFNM'].split(b'\0')[:-1]]
                    mapping=[]
                    for name in names:
                        index,=[i for i,j in enumerate(joints) if j['path'] in declared and j['name'].lower()==name.lower()]; mapping.append(index)
                    palette=[multiply(worlds[i],joints[i]['inverse']) for i in mapping]; points=list(struct.iter_unpack('<3f',fields[b'POSN'])); counts=struct.unpack('<'+'I'*len(points),fields[b'TWHD']); weights=list(struct.iter_unpack('<If',fields[b'TWDT'])); position_key=detail,key,frame,path; expected_positions.add(position_key); native=positions[position_key]['positions']; assert len(native)==len(points); cursor=0
                    for point,count,actual in zip(points,counts,native):
                        group=weights[cursor:cursor+count]; cursor+=count; expected=[sum(w*(sum(palette[i][c][a]*point[a] for a in range(3))+palette[i][c][3]) for i,w in group) for c in range(3)]
                        assert len(actual)==3; error=max(abs(a-b) for a,b in zip(actual,expected)); maximum_position=max(maximum_position,error); assert error<2e-5; vector_count+=1; movement=max(movement,max(abs(a-b) for a,b in zip(expected,point)))
                    assert cursor==len(weights)
        summaries.append(dict(detail=detail,animation_sources=len(source_paths),source_bytes=sum(sources[detail,p]['Bytes'] for p in source_paths)))
    assert set(poses)==expected_poses and set(positions)==expected_positions and movement>.01
    return dict(closure=closure,animation=summaries,walk_run_frames=4,joint_matrix_rows=len(poses),weighted_positions=vector_count,maximum_matrix_error=maximum_matrix,maximum_position_error=maximum_position,maximum_motion_from_bind=movement,tolerance=2e-5,explicit_skeleton_level=0,original_execution=False)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__); p.add_argument('root',type=Path); p.add_argument('configuration',type=Path); p.add_argument('native_output',type=Path); a=p.parse_args(); print(json.dumps(main(a.root,a.configuration,a.native_output),indent=2))
