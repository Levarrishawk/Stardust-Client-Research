"""Independent binary32 target-clear active-zero ticking and rational supplied-terrain reference; no native seed."""
import argparse, hashlib, json, math, runpy, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def value(x):return struct.unpack('<f',struct.pack('<I',x))[0]
def f(x):return value(bits(x))

def fixture(runtime,terrain):
    cases=[]
    def put(name,start,targets,clears,pauses=[],updates=20,**kw):
        c=dict(Name=name,Start=start,Targets=[dict(At=a,X=x,Z=z) for a,x,z in targets],Clears=clears,Pauses=[dict(At=a,Value=v) for a,v in pauses],Updates=updates,Speed=2,Radius=.01,Step=50000,Sparse=False);c.update(kw);cases.append(c)
    put('moving-clear-three-idle-new',[7.8,7.8],[(0,8.5,8.5),(5,8.5,7.8)],[2])
    put('paused-clear-correct-resume-new',[7.8,7.8],[(0,8.5,8.5),(8,8.5,8.5)],[2],[(2,True),(5,False)],Correction=dict(At=3,Position=[8.1,7.65]))
    put('arrived-clear',[1.25,1.25],[(0,1.45,1.25)],[2],updates=7)
    put('idle-clear',[1.25,1.25],[],[0,2],updates=6)
    put('paused-idle-clear',[1.25,1.25],[],[0,2],[(0,True),(4,False)],updates=6)
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],Cases=cases)

def expected(c,sets,surface):
    x,z=map(f,c['Start']);radius=f(c['Radius']);speed=f(c['Speed']);seconds=f(c['Step']/1000000)
    selected=sets[c['Sparse']];target=None;targets=0;rows=[];paused=False;xrow=[bits(1),0,0];zrow=[0,0,bits(1)]
    def height(x,z):return surface['hit'](selected,[surface['b64'](x),surface['b64'](z)],surface['child_options']({}))
    y=f(height(x,z)['Height']+.25)
    for i in range(c['Updates']):
        for t in c['Targets']:
            if t['At']==i:target=(f(t['X']),f(t['Z']));targets+=1
        if i in c['Clears']:target=None
        for p in c['Pauses']:
            if p['At']==i:paused=p['Value']
        if c.get('Correction',{}).get('At')==i:
            x,z=map(f,c['Correction']['Position']);y=99;xrow=[bits(1),0,0];zrow=[0,0,bits(1)]
        dx,dz=(target[0]-x,target[1]-z) if target else (0,0);distance=math.sqrt(dx*dx+dz*dz);vx=vz=0.0
        if target and not paused and distance>radius:
            travel=min(speed*seconds,distance-radius*.5)*(1-1/1048576)
            vx,vz=f(dx/distance*travel/seconds),f(dz/distance*travel/seconds)
            for attempt in range(8):
                nx,nz=f(x+f(vx*seconds)),f(z+f(vz*seconds));projection=(nx-x)*dx+(nz-z)*dz
                if 0<=projection<=distance*distance:break
                vx,vz=f(vx*.5),f(vz*.5)
            else:vx=vz=0.0
        nx,nz=f(x+f(vx*seconds)),f(z+f(vz*seconds));hit=height(nx,nz)
        if hit is None:
            rows.append(dict(Index=i,Mode='Faulted',AttemptedUpdates=i+1,CompletedUpdates=i,SuccessfulTargets=targets,Time=(i+1)*c['Step'],Position=[bits(x),bits(y),bits(z)],Speed=rows[-1]['Speed'] if rows else 0,X=xrow,Y=[0,bits(1),0],Z=zrow));break
        magnitude=f(math.sqrt(f(f(vx*vx)+f(vz*vz))))
        if magnitude>0:xrow=[bits(f(vz/magnitude)),0,bits(f(vx/magnitude))];zrow=[bits(-f(vx/magnitude)),0,bits(f(vz/magnitude))]
        # Check target-plane overshoot from rounded coordinates independently of native output.
        assert (nx-x)*dx+(nz-z)*dz<=distance*distance
        x,z=nx,nz;y=f(hit['Height']+.25);remaining=math.hypot(target[0]-x,target[1]-z) if target else 0
        rows.append(dict(Index=i,Mode='Paused' if paused else 'Idle' if target is None else 'Arrived' if remaining<=radius else 'Moving',AttemptedUpdates=i+1,CompletedUpdates=i+1,SuccessfulTargets=targets,Time=(i+1)*c['Step'],Position=[bits(x),bits(y),bits(z)],Speed=bits(magnitude),X=xrow,Y=[0,bits(1),0],Z=zrow))
    return rows

def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for k in ['runtime','terrain','out']:p.add_argument('--'+k,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()))
    if a.mode=='generate':out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('PINNED_CLEAR_GENERATED');return
    assert a.native;surface=runpy.run_path(str(ROOT/'poc/round62-tests/Rendering/verify_surface.py'));q,reference,n,m=surface['load']()
    chunks=[q.enrich(c,reference.expected(c,n,m),reference,n,m) for c in source['Terrain']]
    for chunk in chunks:
        for patch in chunk['Patches']:patch['NormalBits']=[[bits(float(x)) for x in row] for row in patch['Normals']]
    sets=[dict(Chunks=chunks)];actual=json.loads(a.native.read_text());assert len(actual['Cases'])==5;count=0;idle=0
    for c,got in zip(source['Cases'],actual['Cases']):
        want=expected(c,sets,surface);assert got['Name']==c['Name'] and len(got['Rows'])==len(want)
        for i,(row,e) in enumerate(zip(got['Rows'],want)):
            assert row==e,(c['Name'],i,row,e);count+=1;idle+=e['Mode']=='Idle'
    assert count==59 and idle>=12 and actual['NegativeChecks']>=25
    report=dict(Passed=True,Cases=5,ComparedUpdates=count,IdleTicks=idle,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],IndependentF32GeometryPoseSpeedClock=True,TargetIdentityCheckedNatively=True,ClearNoDriverOrPublicationEffects=True,TargetBudgetNotRefunded=True,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN,OutputSeedUsed=False,OriginalExecuted=False)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('CLEAR_REFERENCE_PASS',json.dumps(report))

if __name__=='__main__':main()
