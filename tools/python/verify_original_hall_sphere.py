"""Verify bounded original hall extraction, floor planes and contact by independent convex distance.

Consumes private native probe output and pinned unchanged assets. Emits aggregates.
"""
import argparse
import collections
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes, inspect
from verify_triangle_box_sweeps import cross
from verify_triangle_sphere_sweeps import earliest
from verify_floor_walks import project, f32


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('native_output',type=Path)
    p.add_argument('--configuration-sha256',required=True);a=p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()==a.configuration_sha256.upper()
    assert a.native_output.stat().st_size<=1048576
    sources={};queries=[];geometry=None
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('hall_sphere_source='):
            r=json.loads(line.split('=',1)[1]);assert r['Path'] not in sources;sources[r['Path']]=r
        elif line.startswith('hall_sphere_query='):queries.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('hall_sphere_geometry='):assert geometry is None;geometry=json.loads(line.split('=',1)[1])
    assert geometry is not None and len(queries)==16
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        def read(path):
            for priority,index in indices:
                data=index.lookup(path)
                if data is not None:
                    pin=sources[path]
                    assert priority==pin['Priority'] and len(data)==pin['Bytes'] and hashlib.sha256(data).hexdigest().upper()==pin['Sha256']
                    return data
            raise ValueError('Required original source missing')
        data=read(geometry['mesh']);inspect(data)
        assert hashlib.sha256(data).hexdigest().upper()=='8050A71B294AFEC1DD9845062C006A459C726FABDA2549CE52EA3A5BABD3E74B'
        root,=nodes(data);version,=root[1];sps,=version[1][1][1]
        triangles=[];source_count=0;degenerate=0
        for _,group in sps[1][1:]:
            for pv,primitive in group[2:]:
                _,vtxa,indx,*_=primitive;vv,=vtxa[1];vi,vd=vv[1];_,n=struct.unpack('<II',vi[1]);stride=len(vd[1])//n
                vertices=[struct.unpack_from('<3f',vd[1],i*stride) for i in range(n)]
                count,=struct.unpack_from('<I',indx[1]);values=struct.unpack_from('<'+('I' if pv==b'0000' else 'H')*count,indx[1],4)
                for i in range(0,count,3):
                    points=[vertices[values[i+j]&65535] for j in range(3)];u=[points[1][j]-points[0][j] for j in range(3)];v=[points[2][j]-points[0][j] for j in range(3)]
                    if cross(u,v)==(0,0,0):degenerate+=1
                    else:triangles.append((source_count,points))
                    source_count+=1
        canonical=struct.pack('<4i',source_count,source_count,degenerate,len(triangles))
        for tid,points in triangles:canonical+=struct.pack('<i9f',tid,*(v for point in points for v in point))
        assert (source_count,degenerate,len(triangles))==(1984,0,1984)
        assert hashlib.sha256(canonical).hexdigest().upper()==geometry['sha256']
        floor_root,=nodes(read(geometry['floor']));floor_version,=floor_root[1];positions,records=floor_version[1][:2];positions,records=positions[1],records[1]
        if floor_version[0]==b'0006':positions,records=positions[4:],records[4:]
        vertices=list(struct.iter_unpack('<3f',positions));floor_rows=[struct.unpack_from('<3iI3i3f4Bi3i',records,i) for i in range(0,len(records),60)]
    outcomes=collections.Counter();distance_tests=0;plane_checks=0;max_error=0
    for i,row in enumerate(queries):
        assert row['index']==i and row['reusable'] and row['repeat_stable'] and row['repeat_count']==100 and row['retreat_accepted']
        offset=[f32(v) for v in row['center_offset']];radius=f32(row['radius']);assert offset==[0,1,0] and radius in [f32(.5),f32(.65)]
        best=None;segments=row['segments'];assert 1<=len(segments)<=3
        for si,s in enumerate(segments):
            assert s['first']<=s['last']<=row['path_parameter']
            points=[vertices[index] for index in floor_rows[s['triangle']][:3]]
            for weight in (0,.5,1):
                point=[s['start'][j]*(1-weight)+s['end'][j]*weight for j in range(3)]
                assert abs(project(points,point)[1]-point[1])<1e-4;plane_checks+=1
            if si:
                assert s['first']==segments[si-1]['last'] and s['triangle'] in floor_rows[segments[si-1]['triangle']][4:7]
                assert max(abs(x-y) for x,y in zip(s['start'],segments[si-1]['end']))<1e-6
            start=[s['start'][j]+offset[j] for j in range(3)];end=[s['end'][j]+offset[j] for j in range(3)]
            lo=[min(start[j],end[j])-radius for j in range(3)];hi=[max(start[j],end[j])+radius for j in range(3)]
            for tid,triangle in triangles:
                # Conservative AABB rejection saves distance work, without altering geometry.
                if any(max(v[j] for v in triangle)<lo[j] or min(v[j] for v in triangle)>hi[j] for j in range(3)):continue
                distance_tests+=1;t=earliest(dict(triangle=triangle,start=start,end=end,radius=radius))
                if t is None:continue
                parameter=s['first']+(s['last']-s['first'])*t
                if best is None or parameter<best[0]:best=(parameter,si,tid,[start[j]+(end[j]-start[j])*t for j in range(3)])
        if best is None:
            assert row['status']=='FloorStopped' and row['contact'] is None,(i,row)
            assert abs(row['parameter']-row['path_parameter'])<1e-12
        else:
            parameter,segment,tid,center=best;hit=row['contact'];assert hit is not None
            error=abs(hit['parameter']-parameter);max_error=max(max_error,error)
            assert row['status']=='BoxStopped' and hit['status']=='Blocked' and hit['segment']==segment and hit['id']==tid,(i,best,row)
            assert error<1e-8 and max(abs(a-b) for a,b in zip(center,hit['center']))<1e-6,(i,best,row)
            feet=[center[j]-offset[j] for j in range(3)]
            assert sum((f32(v)-w)**2 for v,w in zip(row['position'],feet))<1.1e-8
        outcomes[row['status']]+=1
    assert outcomes['BoxStopped']==0 and outcomes['FloorStopped']==16
    print(json.dumps(dict(original_assets=True,configuration_sha256=a.configuration_sha256.upper(),mesh_sha256=sources[geometry['mesh']]['Sha256'],floor_sha256=sources[geometry['floor']]['Sha256'],collision_sha256=geometry['sha256'],triangles=len(triangles),degenerate=degenerate,queries=len(queries),distance_tests=distance_tests,floor_plane_checks=plane_checks,outcomes=outcomes,stable_repeats=1600,accepted_retreats=16,maximum_parameter_error=max_error),indent=2))


if __name__=='__main__':main()
