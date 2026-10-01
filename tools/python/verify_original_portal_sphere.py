"""Independent complete selected-mesh extraction and original portal sphere contact distance checks."""
import argparse
import collections
import contextlib
import hashlib
import json
from pathlib import Path
import re
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes, indexed_triangles
from verify_triangle_box_sweeps import cross
from verify_triangle_sphere_sweeps import earliest
from verify_floor_walks import f32, project
from verify_portal_floor_boxes import transform, transform32


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('native_output',type=Path)
    p.add_argument('--configuration-sha256',required=True);a=p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()==a.configuration_sha256.upper()
    assert a.native_output.stat().st_size<=1048576
    sources={};geometries={};queries=[];bounds=[]
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('scene_sphere_source='):
            r=json.loads(line.split('=',1)[1]);assert r['Path'] not in sources;sources[r['Path']]=r
        elif line.startswith('scene_sphere_geometry='):
            r=json.loads(line.split('=',1)[1]);assert r['cell'] not in geometries;geometries[r['cell']]=r
        elif line.startswith('scene_sphere_query='):queries.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('scene_sphere_bounds '):bounds.append({key:int(value) for key,value in re.findall(r'(\w+)=(\d+)',line)})
    assert set(geometries)=={5,6,7} and len(queries)==8 and len(bounds)==2
    triangles={};floors={}
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        def read(path):
            for priority,index in indices:
                data=index.lookup(path)
                if data is not None:
                    pin=sources[path];assert priority==pin['Priority'] and len(data)==pin['Bytes'] and hashlib.sha256(data).hexdigest().upper()==pin['Sha256']
                    return data
            raise ValueError('Original source missing')
        for cell,g in geometries.items():
            raw=indexed_triangles(read(g['mesh']));selected=[];degenerate=0
            for tid,triangle in raw:
                u=[triangle[1][j]-triangle[0][j] for j in range(3)];v=[triangle[2][j]-triangle[0][j] for j in range(3)]
                if cross(u,v)==(0,0,0):degenerate+=1
                else:selected.append((tid,triangle))
            canonical=struct.pack('<4i',len(raw),len(raw),degenerate,len(selected))
            for tid,points in selected:canonical+=struct.pack('<i9f',tid,*(v for point in points for v in point))
            assert g['source']==g['eligible']==len(raw) and g['degenerate']==degenerate and g['triangles']==len(selected)
            assert hashlib.sha256(canonical).hexdigest().upper()==g['sha256'];triangles[cell]=selected
            root,=nodes(read(g['floor']));version,=root[1];positions,records=version[1][:2];positions,records=positions[1],records[1]
            if version[0]==b'0006':positions,records=positions[4:],records[4:]
            vertices=list(struct.iter_unpack('<3f',positions));rows=[struct.unpack_from('<3iI3i3f4Bi3i',records,i) for i in range(0,len(records),60)];floors[cell]=(vertices,rows)
    total=sum(len(t) for t in triangles.values());assert total==42986
    outcomes=collections.Counter();distance_tests=0;plane_checks=0;maximum_error=0;repeats=0
    for i,row in enumerate(queries):
        assert row['index']==i and row['transfers']==1 and row['cell'] in (5,7) and row['reverse_status']=='Accepted' and row['reverse_cell']==6 and row['reverse_transfers']==1 and row['repeat_stable']
        assert max(abs(f32(v)-f32(w)) for v,w in zip(row['start'],row['reverse_position']))<1e-4
        offset=[f32(v) for v in row['center_offset']];radius=f32(row['radius']);pose=row['pose'];assert offset==[0,1,0] and radius==.5
        world_triangles={cell:[(tid,[transform32(v,pose) for v in points]) for tid,points in source] for cell,source in triangles.items()}
        legs=row['floors'];assert len(legs)==2 and legs[0]['cell']==6 and legs[1]['cell']==row['cell'];source_parameter=legs[0]['parameter'];hits=[]
        for leg,part in enumerate(legs):
            vertices,rows=floors[part['cell']]
            for si,s in enumerate(part['segments']):
                points=[vertices[v] for v in rows[s['triangle']][:3]]
                for weight in (0,.5,1):
                    v=[s['start'][j]*(1-weight)+s['end'][j]*weight for j in range(3)];assert abs(project(points,v)[1]-v[1])<1e-4;plane_checks+=1
                start=[v+offset[j] for j,v in enumerate(transform(s['start'],pose))];end=[v+offset[j] for j,v in enumerate(transform(s['end'],pose))]
                lo=[min(start[j],end[j])-radius for j in range(3)];hi=[max(start[j],end[j])+radius for j in range(3)]
                for cell,candidates in world_triangles.items():
                    for tid,triangle in candidates:
                        if any(max(v[j] for v in triangle)<lo[j] or min(v[j] for v in triangle)>hi[j] for j in range(3)):continue
                        distance_tests+=1;t=earliest(dict(triangle=triangle,start=start,end=end,radius=radius))
                        if t is None:continue
                        local=s['first']+(s['last']-s['first'])*t;parameter=local if leg==0 else source_parameter+(1-source_parameter)*local
                        hits.append((parameter,cell,part['cell'],tid,[start[j]+(end[j]-start[j])*t for j in range(3)]))
        if not hits:
            assert row['status']=='Accepted' and row['parameter']==1 and row['contact'] is None,(i,row)
        else:
            best=min(h[0] for h in hits);hit=row['contact'];assert hit is not None and row['status']=='BoxStopped'
            error=abs(hit['parameter']-best);maximum_error=max(maximum_error,error);assert error<1e-8,(i,best,row)
            matching=[h for h in hits if abs(h[0]-best)<1e-8 and h[1:4]==(hit['cell'],hit['path_cell'],hit['id'])]
            assert matching and min(max(abs(a-b) for a,b in zip(h[4],hit['center'])) for h in matching)<1e-6,(i,best,row)
            assert hit['status']=='Blocked'
        assert row['repeat_count']==100;repeats+=row['repeat_count']
        outcomes[row['status']]+=1
    assert outcomes==dict(Accepted=8)
    for b in bounds:assert b['queries']==32 and b['source']==total and b['candidates']==938 and b['exhaustive']==total*32 and b['contacts']==16
    print(json.dumps(dict(original_assets=True,configuration_sha256=a.configuration_sha256.upper(),triangles=total,degenerate=0,queries=len(queries),owner_poses=2,outcomes=outcomes,stable_repeats=repeats,accepted_reverse_transfers=8,distance_tests=distance_tests,floor_plane_checks=plane_checks,maximum_parameter_error=maximum_error,bounds_queries=64,bounds_candidates=sum(b['candidates'] for b in bounds),exhaustive_tests=sum(b['exhaustive'] for b in bounds),geometry_sha256={str(c):g['sha256'] for c,g in geometries.items()}),indent=2))


if __name__=='__main__':main()
