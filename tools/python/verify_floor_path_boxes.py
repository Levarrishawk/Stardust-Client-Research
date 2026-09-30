"""Independent synthetic crease-plane and oriented-box interval reference."""
import argparse, collections, hashlib, json, math, random, struct, subprocess
from pathlib import Path

def f32(x): return struct.unpack('<f',struct.pack('<f',x))[0]
def point(x,z): return [x,max(0,x+z-2),z]
def reference(row):
    start=row['start']; end=row['end']; half=row['half']; pose=row['pose']
    delta=[end[i]-start[i] for i in range(3)]
    denominator=delta[0]+delta[2]
    crossing=(2-start[0]-start[2])/denominator if denominator else None
    times=[0]+([crossing] if crossing is not None and 0<crossing<1 else [])+[1]
    points=[point(start[0]+delta[0]*t,start[2]+delta[2]*t) for t in times]
    return reference_path(row,times,points)

def reference_path(row,times,points):
    half=row['half'];pose=row['pose']
    terminal=[points[-1][0],points[-1][1]+half[1],points[-1][2]]
    clear=dict(status='Clear',parameter=1,segment=-1,center=terminal,normal=[0,0,0])
    if row['allowed'] is True: return clear
    extent=[sum(abs(pose[j][i])*half[j] for j in range(3)) for i in range(3)]
    lo=[row['minimum'][i]-extent[i] for i in range(3)]; hi=[row['maximum'][i]+extent[i] for i in range(3)]
    def local(p):
        offset=[p[i]+(half[1] if i==1 else 0)-pose[3][i] for i in range(3)]
        return [sum(pose[j][i]*offset[j] for j in range(3)) for i in range(3)]
    for index,(a,b) in enumerate(zip(points,points[1:])):
        first,last=local(a),local(b)
        if row['separating'] and any(abs(first[i]-lo[i])<=1e-5 and last[i]<first[i] or abs(first[i]-hi[i])<=1e-5 and last[i]>first[i] for i in range(3)): continue
        inside=all(lo[i]<=first[i]<=hi[i] for i in range(3))
        if row['separating'] and not inside and all(lo[i]-1e-4<=first[i]<=hi[i]+1e-4 for i in range(3)):
            touching=next(((i,-1) if abs(first[i]-lo[i])<=1e-4 and last[i]>first[i] else (i,1) for i in range(3) if abs(first[i]-lo[i])<=1e-4 and last[i]>first[i] or abs(first[i]-hi[i])<=1e-4 and last[i]<first[i]),None)
            if touching:
                return dict(status='PermissionUnknown' if row['allowed'] is None else 'InitialOverlap',parameter=times[index],segment=index,
                            center=[a[i]+(half[1] if i==1 else 0) for i in range(3)],normal=[pose[i][touching[0]]*touching[1] for i in range(3)])
        intervals=[]; failed=False
        for i in range(3):
            d=last[i]-first[i]
            if d==0:
                if not lo[i]<=first[i]<=hi[i]: failed=True; break
                intervals.append((-math.inf,math.inf,i,0)); continue
            t0,t1=(lo[i]-first[i])/d,(hi[i]-first[i])/d
            intervals.append((min(t0,t1),max(t0,t1),i,-1 if d>0 else 1))
        if failed: continue
        entry=max([0]+[i[0] for i in intervals]); leave=min([1]+[i[1] for i in intervals])
        if entry>leave: continue
        normal=[0,0,0]
        if entry>0:
            axis=next(i for i in intervals if i[0]==entry)
            normal=[pose[i][axis[2]]*axis[3] for i in range(3)]
        center=[a[i]+(b[i]-a[i])*entry+(half[1] if i==1 else 0) for i in range(3)]
        return dict(status='PermissionUnknown' if row['allowed'] is None else 'InitialOverlap' if inside else 'Blocked',
                    parameter=times[index]+(times[index+1]-times[index])*entry,segment=index,center=center,normal=normal)
    return clear

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('private_output',type=Path);parser.add_argument('core_dll',type=Path);parser.add_argument('--with-landings',action='store_true')
    args=parser.parse_args();args.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve())
    args.private_output.mkdir(parents=True,exist_ok=True);rng=random.Random(2026093015);fixtures=[]
    for i in range(6000):
        source=point(f32(rng.uniform(.1,.8)),f32(rng.uniform(.1,.8)))
        target=point(f32(rng.uniform(1.2,1.8)),f32(rng.uniform(1.2,1.8)))
        start,end=(source,target) if i%2==0 else (target,source)
        if i%17==0: end=start
        yaw,pitch=rng.uniform(-math.pi,math.pi),rng.uniform(-.7,.7)
        cy,sy,cp,sp=math.cos(yaw),math.sin(yaw),math.cos(pitch),math.sin(pitch)
        # Yaw * pitch, rows of a right-handed local-to-path rotation.
        pose=[[cy,sy*sp,sy*cp],[0,cp,-sp],[-sy,cy*sp,cy*cp]]
        half=[f32(rng.uniform(.02,.25)) for _ in range(3)]
        t=rng.random(); feet=point(start[0]+(end[0]-start[0])*t,start[2]+(end[2]-start[2])*t)
        pose.append([feet[j]+(half[1] if j==1 else 0)+rng.uniform(-.45,.45) for j in range(3)])
        width=[rng.uniform(.025,.25) for _ in range(3)]
        row=dict(triangle=0 if start[0]+start[2]<2 else 1,start=[f32(v) for v in start],end=[f32(v) for v in end],half=half,
            minimum=[f32(-v) for v in width],maximum=[f32(v) for v in width],pose=[[f32(v) for v in p] for p in pose],
            allowed=[False,False,False,None,True][i%5],separating=i%3==0)
        fixtures.append(row)
    fixture=args.private_output/'fixtures.json';fixture.write_text(json.dumps(fixtures,separators=(',',':')),encoding='utf-8')
    digest=hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    run=subprocess.run(['dotnet',str(args.core_dll),'--floor-box-walks' if args.with_landings else '--floor-path-boxes',str(fixture),'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=120)
    (args.private_output/'native-boxes.txt').write_text(run.stdout,encoding='utf-8')
    observed=[json.loads(line[9:]) for line in run.stdout.splitlines() if line.startswith('path_box=')]
    landings=[json.loads(line[15:]) for line in run.stdout.splitlines() if line.startswith('floor_box_walk=')]
    assert len(observed)==len(fixtures);counts=collections.Counter();landing_counts=collections.Counter()
    if args.with_landings: assert len(landings)==len(fixtures)
    for index,(row,native) in enumerate(zip(fixtures,observed)):
        expected=reference(row)
        assert native['index']==index and all(native[k]==expected[k] for k in ('status','segment')),(index,expected,native)
        assert abs(native['parameter']-expected['parameter'])<=1e-9,(index,expected,native)
        assert max(abs(a-b) for a,b in zip(native['center'],expected['center']))<=1e-9,(index,expected,native)
        assert max(abs(a-b) for a,b in zip(native['normal'],expected['normal']))<=1e-6,(index,expected,native)
        counts[native['status']]+=1
        if args.with_landings:
            landing=landings[index]
            if expected['status']=='Clear':
                geometric=point(row['end'][0],row['end'][2]); wanted='Accepted'; parameter=1
            else:
                geometric=[expected['center'][i]-(row['half'][1] if i==1 else 0) for i in range(3)]; wanted='BoxStopped'; parameter=expected['parameter']
            position=[f32(geometric[0]),0,f32(geometric[2])];position[1]=f32(max(0,position[0]+position[2]-2))
            displacement=[position[i]-geometric[i] for i in range(3)]
            if wanted=='BoxStopped' and (sum(v*v for v in displacement)>1e-8 or sum(displacement[i]*expected['normal'][i] for i in range(3))< -1e-6):
                wanted='ContactLandingUnsupported'; position=row['start']; parameter=0
            triangle=0 if position[0]+position[2]<2 else 1 if position[0]+position[2]>2 else row['triangle']
            assert landing['status']==wanted and landing['floor_status']=='Accepted' and landing['resume_status']=='Accepted' and landing['triangle']==triangle,(index,landing,wanted,position,triangle)
            assert max(abs(a-b) for a,b in zip(landing['position'],position))<=1e-7 and abs(landing['parameter']-parameter)<=1e-9,(index,landing,position,parameter)
            landing_counts[wanted]+=1
    summary=dict(synthetic=True,seed=2026093015,fixtures_sha256=digest,queries=len(fixtures),outcomes=counts,landings=landing_counts)
    (args.private_output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print('PASS independent crease/rotated-box interval reference: '+json.dumps(summary,sort_keys=True))

if __name__=='__main__': main()
