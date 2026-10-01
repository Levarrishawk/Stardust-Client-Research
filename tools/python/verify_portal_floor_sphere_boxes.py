"""Independent synthetic two-owner slope/seam and sphere against captured solid boxes reference."""
import argparse,collections,hashlib,json,math,random,subprocess
from pathlib import Path
from verify_floor_path_boxes import f32

def transform(p,t):return [sum(t[i][j]*p[j] for j in range(3))+t[3][i] for i in range(3)]
def dot32(a,b):return f32(f32(f32(a[0]*b[0])+f32(a[1]*b[1]))+f32(a[2]*b[2]))
def transform32(p,t):return [f32(dot32(t[i],p)+t[3][i]) for i in range(3)]
def inverse32(p,t):
    delta=[f32(p[i]-t[3][i]) for i in range(3)]
    return [dot32([t[j][i] for j in range(3)],delta) for i in range(3)]
def compose(a,b):
    return [[f32(sum(a[i][k]*b[k][j] for k in range(3))) for j in range(3)] for i in range(3)]+[[f32(v) for v in transform(b[3],a)]]


from verify_solid_box_sphere import earliest as sphere_entry

def reference_path(box,times,points):
    offset=box['center_offset'];radius=box['radius'];pose=box['pose']
    centers=[[v+offset[j] for j,v in enumerate(p)] for p in points]
    if box['allowed'] is True:return dict(status='Clear')
    def local(p):return [sum(pose[k][j]*(p[k]-pose[3][k]) for k in range(3)) for j in range(3)]
    def outward(p):return [v-max(box['minimum'][j],min(box['maximum'][j],v)) for j,v in enumerate(p)]
    for index,(start,end) in enumerate(zip(centers,centers[1:])):
        a,b=local(start),local(end);n=outward(a);distance=math.sqrt(sum(v*v for v in n));delta=[y-x for x,y in zip(a,b)]
        strict=distance<radius-1e-9
        if distance<=radius and not strict and sum(x*y for x,y in zip(n,delta))>=0:continue
        t=sphere_entry(dict(box,start=start,end=end))
        if t is None:continue
        center=[x+(y-x)*t for x,y in zip(start,end)];inside=outward(local(center));length=math.sqrt(sum(v*v for v in inside))
        if length:
            normal=[v/length for v in inside]
        else:
            position=local(center);faces=[(position[j]-box['minimum'][j],j,-1) for j in range(3)]+[(box['maximum'][j]-position[j],j,1) for j in range(3)]
            _,axis,sign=min(faces);normal=[sign if j==axis else 0 for j in range(3)]
        world=[sum(pose[j][k]*normal[k] for k in range(3)) for j in range(3)];norm=math.sqrt(sum(v*v for v in world));world=[v/norm for v in world]
        status='PermissionUnknown' if box['allowed'] is None else 'InitialOverlap' if t==0 and strict else 'Blocked'
        return dict(status=status,parameter=times[index]+(times[index+1]-times[index])*t,center=center,normal=world)
    return dict(status='Clear')

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);args=p.parse_args()
    args.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());args.private_output.mkdir(parents=True,exist_ok=True)
    rng=random.Random(2026100114);fixtures=[]
    rotations=[[[1,0,0],[0,1,0],[0,0,1]],[[0,0,1],[0,1,0],[-1,0,0]],[[-1,0,0],[0,1,0],[0,0,-1]],[[0,0,-1],[0,1,0],[1,0,0]]]
    for i in range(512):
        cell=i%2;sx=f32(rng.uniform(.7,1.3)*(1 if cell else -1));sz=f32(rng.uniform(.15,.85));ex=f32(rng.uniform(.7,1.3)*(-1 if cell else 1));ez=f32(rng.uniform(.15,.85))
        owner=rotations[i%4]+[[30,4,-11] if i%8<4 else [0,0,0]];start=[sx,f32(sx*.5),sz]
        boxes=[]
        for bid in range(i%4):
            bcell=(cell+bid)%2;center=[rng.uniform(-.35,.35) if bid%3==0 else rng.uniform(.35,1.1)*(1 if bcell else -1),rng.uniform(-.2,.8),rng.uniform(.1,.9)]
            yaw=rng.uniform(-math.pi,math.pi);c,s=math.cos(yaw),math.sin(yaw);local=[[f32(c),0,f32(s)],[0,1,0],[f32(-s),0,f32(c)],[f32(v) for v in center]]
            width=[rng.uniform(.04,.3),rng.uniform(.2,.7),rng.uniform(.1,.4)]
            boxes.append(dict(cell=bcell,id=bid,minimum=[f32(-v) for v in width],maximum=[f32(v) for v in width],pose=compose(owner,local),allowed=[False,False,None,True][(i+bid)%4]))
        permission=[[True,True],[False,True],[None,True],[True,False],[True,None]][i%5]
        triangle=int((sx/2 if cell else (sx+2)/2)+sz>1)
        fixtures.append(dict(cell=cell,triangle=triangle,start=start,end=transform32([ex,0,ez],owner),owner=owner,center_offset=[f32(.02*(i%3-1)),f32(.4),f32(.015*(i%5-2))],radius=f32(.15),boxes=boxes,permission=permission))
    fixture=args.private_output/'fixtures.json';fixture.write_text(json.dumps(fixtures,separators=(',',':')),encoding='utf-8');digest=hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    run=subprocess.run(['dotnet',str(args.core_dll),'--portal-floor-sphere-boxes',str(fixture),'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=120)
    (args.private_output/'native-portal-boxes.txt').write_text(run.stdout,encoding='utf-8');native=[json.loads(line.split("=",1)[1]) for line in run.stdout.splitlines() if line.startswith('portal_sphere_box=')]
    assert len(native)==len(fixtures);counts=collections.Counter();ownership=collections.Counter()
    for i,(row,result) in enumerate(zip(fixtures,native)):
        owner=row['owner'];start=row['start'];end=inverse32(row['end'],owner);seam=-start[0]/(end[0]-start[0]);z=start[2]+(end[2]-start[2])*seam
        precise_source=transform([0,0,z],owner);source_end32=transform32([0,0,f32(z)],owner)
        # Existing ABI bridge projects onto the reversed target edge; quarter-turn axes preserve the scalar exactly.
        bridge=inverse32(source_end32,owner);target_end=transform([end[0],end[0]*.5,end[2]],owner)
        permit=row['permission']==[True,True]
        def leg_points(a,b,first,last,cell):
            # Each original two-triangle rectangle has one diagonal; include its start-contact policy boundary.
            level=1 if cell else 0;den=(b[0]-a[0])*.5+b[2]-a[2]
            crossing=(level-a[0]*.5-a[2])/den if den else None
            fractions=[0]+([crossing] if crossing is not None and 0<crossing<1 else [])+[1]
            times=[first+(last-first)*t for t in fractions]
            points=[transform([a[0]+(b[0]-a[0])*t,(a[0]+(b[0]-a[0])*t)*.5,a[2]+(b[2]-a[2])*t],owner) for t in fractions]
            return times,points,cell
        legs=[leg_points(start,[0,0,z],0,seam,row['cell'])]
        if permit:legs.append(leg_points([0,0,bridge[2]],end,seam,1,1-row['cell']))
        best=None
        for times,points,cell in legs:
            for box in row['boxes']:
                if not permit and box['cell']!=row['cell']:continue
                hit=reference_path(dict(box,center_offset=row['center_offset'],radius=row['radius']),times,points)
                if hit['status']=='Clear':continue
                if best is None or hit['parameter']<best[0]['parameter']:best=(hit,box,cell)
        assert result['index']==i and result['reusable'],(i,'unusable result',result)
        result['world']=[f32(v) for v in result['world']];result['local']=[f32(v) for v in result['local']]
        if best is None:
            assert result['contact'] is None and result['status']==('Accepted' if permit else 'FloorStopped'),(i,row,result)
            expected_status='Accepted' if permit else 'PermissionUnknown' if None in row['permission'] else 'EntryDenied' if not row['permission'][0] else 'PortalBlocked'
            assert result['movement']==expected_status and result['cell']==(1-row['cell'] if permit else row['cell']) and result['transfers']==int(permit),(i,row,result)
            terminal=target_end if permit else source_end32
            assert abs(result['parameter']-(1 if permit else seam))<=1e-7 and max(abs(a-b) for a,b in zip(result['world'],terminal))<=1e-5,(i,terminal,result)
        else:
            hit,box,cell=best;contact=result['contact'];assert contact is not None
            assert contact['cell']==box['cell'] and contact['id']==box['id'] and contact['path_cell']==cell and contact['status']==hit['status'],(i,best,result)
            assert abs(contact['parameter']-hit['parameter'])<=1e-7 and max(abs(a-b) for a,b in zip(contact['center'],hit['center']))<=1e-6,(i,hit,result)
            if result['status']=='ContactLandingUnsupported':
                assert result['cell']==row['cell'] and result['transfers']==0 and result['parameter']==0 and result['local']==start,(i,'bad precision hold',result)
            else:
                seam_hold=cell!=row['cell'] and hit['parameter']==seam
                assert result['status']=='BoxStopped' and result['cell']==(row['cell'] if seam_hold else cell) and result['transfers']==int(cell!=row['cell'] and not seam_hold),(i,best,result)
                if seam_hold:assert result['diagnostic']=='TargetSeamOverlap'
                feet=[hit['center'][j]-row['center_offset'][j] for j in range(3)];delta=[result['world'][j]-feet[j] for j in range(3)]
                assert sum(v*v for v in delta)<=1.0001e-8 and sum(delta[j]*hit['normal'][j] for j in range(3))>=-1.001e-6,(i,'world contact budget',hit,result)
                assert abs(result['local'][1]-result['local'][0]*.5)<=1e-6 and abs(result['parameter']-hit['parameter'])<=1e-7,(i,'floor plane/parameter',result)
        counts[result['status']]+=1;ownership[str(result['transfers'])]+=1
    summary=dict(synthetic=True,seed=2026100114,fixtures_sha256=digest,queries=len(fixtures),owner_poses=8,outcomes=counts,transfer_counts=ownership)
    (args.private_output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print('PASS independent slope/seam/permission/solid-sphere reference: '+json.dumps(summary,sort_keys=True))

if __name__=='__main__':main()
