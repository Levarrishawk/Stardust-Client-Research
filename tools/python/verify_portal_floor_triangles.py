"""Independent synthetic portal slope/ownership and LP triangle contact verification."""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import random
import subprocess

from verify_portal_floor_boxes import transform, transform32, inverse32
from verify_triangle_box_sweeps import earliest
from verify_floor_path_boxes import f32


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('output',type=Path);p.add_argument('native_dll',type=Path);a=p.parse_args()
    a.output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve())
    a.output.mkdir(parents=True,exist_ok=True)
    rng=random.Random(2026100102)
    rotations=[[[1,0,0],[0,1,0],[0,0,1]],[[0,0,1],[0,1,0],[-1,0,0]],[[-1,0,0],[0,1,0],[0,0,-1]],[[0,0,-1],[0,1,0],[1,0,0]]]
    fixtures=[]
    for i in range(512):
        cell=i%2;sx=f32(rng.uniform(.7,1.3)*(1 if cell else -1));sz=f32(rng.uniform(.15,.85))
        ex=f32(rng.uniform(.7,1.3)*(-1 if cell else 1));ez=f32(rng.uniform(.15,.85));x=f32(rng.uniform(-1.6,1.6))
        owner=rotations[i%4]+[[30,4,-11] if i%8<4 else [0,0,0]]
        triangles=[] if i%4==0 else [[[x,-2,-1],[x,3,-1],[x,-2,2]],[[x,3,-1],[x,3,2],[x,-2,2]]]
        fixtures.append(dict(cell=cell,triangle=int(sx*.5+sz>(1 if cell else 0)),start=[sx,f32(sx*.5),sz],end=transform32([ex,0,ez],owner),owner=owner,half=[f32(.15),f32(.4),f32(.15)],triangles=triangles,collider_cell=(i//2)%2,permit=i%5!=0))
    file=a.output/'fixtures.json';file.write_text(json.dumps(fixtures,separators=(',',':')),encoding='utf-8');pin=hashlib.sha256(file.read_bytes()).hexdigest().upper()
    run=subprocess.run(['dotnet',str(a.native_dll),'--portal-floor-triangles',str(file),'--expected-sha256',pin],capture_output=True,text=True,check=True,timeout=120)
    (a.output/'native.txt').write_text(run.stdout,encoding='utf-8')
    native=[json.loads(line.split('=',1)[1]) for line in run.stdout.splitlines() if line.startswith('portal_triangle=')]
    assert len(native)==len(fixtures)
    counts=collections.Counter();transfers=collections.Counter();max_error=0
    for i,(row,result) in enumerate(zip(fixtures,native)):
        result['local']=[f32(v) for v in result['local']]
        owner=row['owner'];start=row['start'];end=inverse32(row['end'],owner)
        seam=-start[0]/(end[0]-start[0]);z=start[2]+(end[2]-start[2])*seam
        bridge=inverse32(transform32([0,0,f32(z)],owner),owner)
        legs=[(start,[0,0,z],0,seam,row['cell'])]
        if row['permit']:legs.append(([0,0,bridge[2]],[end[0],end[0]*.5,end[2]],seam,1,1-row['cell']))
        best=None
        for first,last,lo,hi,cell in legs:
            # LP solves triangle barycentrics and time directly, independently of SAT.
            body_start=transform(first,owner);body_end=transform(last,owner)
            body_start[1]+=row['half'][1];body_end[1]+=row['half'][1]
            for tid,triangle in enumerate(row['triangles']):
                t=earliest(dict(triangle=[transform32(v,owner) for v in triangle],start=body_start,end=body_end,half=row['half']))
                if t is None:continue
                parameter=lo+(hi-lo)*t
                center=[body_start[j]+(body_end[j]-body_start[j])*t for j in range(3)]
                if best is None or parameter<best[0]:best=(parameter,cell,tid,center)
        assert result['index']==i and result['reusable'],(i,result)
        if best is None:
            assert result['contact'] is None and result['status']==('Accepted' if row['permit'] else 'FloorStopped'),(i,row,result)
            assert result['cell']==(1-row['cell'] if row['permit'] else row['cell']) and result['transfers']==int(row['permit']),(i,result)
            assert abs(result['parameter']-(1 if row['permit'] else seam))<1e-7
        else:
            parameter,cell,tid,center=best;contact=result['contact']
            assert contact is not None and contact['cell']==row['collider_cell'] and contact['path_cell']==cell,(i,best,result)
            error=abs(contact['parameter']-parameter);max_error=max(max_error,error)
            assert error<1e-7 and max(abs(a-b) for a,b in zip(center,contact['center']))<1e-6,(i,best,result)
            if result['status']=='ContactLandingUnsupported':
                assert result['cell']==row['cell'] and result['transfers']==0 and result['parameter']==0 and result['local']==start,(i,result)
            else:
                assert result['status']=='BoxStopped' and result['cell']==cell and result['transfers']==int(cell!=row['cell']),(i,best,result)
                assert abs(result['parameter']-parameter)<1e-7 and abs(result['local'][1]-result['local'][0]*.5)<1e-6,(i,result)
                # Parameter-zero initial overlap holds the validated floor start.
                if parameter==0:assert result['local']==start,(i,result)
                else:
                    world=[f32(v) for v in result['world']];feet=[center[j]-(row['half'][1] if j==1 else 0) for j in range(3)]
                    assert sum((a-b)**2 for a,b in zip(world,feet))<1.1e-8,(i,best,result)
        counts[result['status']]+=1;transfers[str(result['transfers'])]+=1
    summary=dict(synthetic=True,seed=2026100102,queries=len(fixtures),owner_poses=8,fixtures_sha256=pin,outcomes=counts,transfer_counts=transfers,maximum_parameter_error=max_error)
    (a.output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print('PASS independent portal/LP triangle reference: '+json.dumps(summary,sort_keys=True))


if __name__=='__main__':main()
