"""Independent binary32 manual intent and binary32 speed-cap reference and rational supplied-terrain reference; no native seed."""
import argparse, hashlib, json, math, runpy, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def value(x):return struct.unpack('<f',struct.pack('<I',x))[0]
def f(x):return value(bits(x))

def manual_velocity(direction,speed):
    x,z=direction;length=math.sqrt(x*x+z*z);scale=speed/length if length>1 else speed;vx,vz=f(x*scale),f(z*scale);adjust=0
    def magnitude():return f(math.sqrt(f(f(vx*vx)+f(vz*vz))))
    while adjust<4 and magnitude()>speed:
        vx=value(bits(vx)-1) if vx else vx;vz=value(bits(vz)-1) if vz else vz;adjust+=1
    assert magnitude()<=speed
    return vx,vz,adjust

def fixture(runtime,terrain):
    cases=[]
    def put(name,manual,targets=[],pauses=[],clears=[],updates=20,speed=2,**kw):
        c=dict(Name=name,Start=[7.8,7.8],Targets=[dict(At=a,X=x,Z=z) for a,x,z in targets],Manual=[dict(At=a,X=x,Z=z) for a,x,z in manual],Clears=clears,Pauses=[dict(At=a,Value=v) for a,v in pauses],Updates=updates,Speed=speed,Radius=.01,Step=10000,Sparse=False);c.update(kw);cases.append(c)
    put('manual-speed-axis',[(0,1,0),(6,1,1),(12,0,0)],updates=16,Speeds=[dict(At=0,Value=3),dict(At=3,Value=4),dict(At=6,Value=1),dict(At=9,Value=10)])
    put('point-speed-distance',[],targets=[(0,8.5,8.5)],updates=24,Speeds=[dict(At=0,Value=3),dict(At=4,Value=4),dict(At=8,Value=1),dict(At=12,Value=10),dict(At=19,Value=3)])
    put('paused-correct-speed',[(0,1,1),(4,-1,1)],pauses=[(2,True),(6,False)],updates=14,Correction=dict(At=3,Position=[8.1,7.65]),Speeds=[dict(At=0,Value=3),dict(At=3,Value=4),dict(At=7,Value=1)])
    put('point-manual-clear-speed',[(3,1,0),(9,0,-1)],targets=[(0,8.5,8.5),(6,8.5,8.5)],clears=[8,12],updates=14,Speeds=[dict(At=0,Value=4),dict(At=3,Value=1),dict(At=6,Value=3),dict(At=10,Value=4)])
    put('analog-speed',[(0,.5,0),(4,0,-.25),(8,0,0)],updates=12,Speeds=[dict(At=0,Value=3),dict(At=2,Value=4),dict(At=6,Value=1),dict(At=8,Value=10)])
    witness=next((f(x/100),f(z/100)) for x in range(1,101) for z in range(1,101) if manual_velocity((f(x/100),f(z/100)),10)[2]>0)
    put('maximum-speed-change',[(0,*witness),(4,1,1),(8,-1,-1),(12,0,0)],updates=16,Speeds=[dict(At=0,Value=10),dict(At=6,Value=3),dict(At=10,Value=4),dict(At=14,Value=1)])
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],Cases=cases)

def expected(c,sets,surface,stats):
    x,z=map(f,c['Start']);radius=f(c['Radius']);speed=f(c['Speed']);seconds=f(c['Step']/1000000)
    selected=sets[c['Sparse']];target=None;targets=0;rows=[];paused=False;manual=(0.0,0.0);xrow=[bits(1),0,0];zrow=[0,0,bits(1)]
    def height(x,z):return surface['hit'](selected,[surface['b64'](x),surface['b64'](z)],surface['child_options']({}))
    y=f(height(x,z)['Height']+.25)
    for i in range(c['Updates']):
        for change in c['Speeds']:
            if change['At']==i:speed=f(change['Value'])
        for t in c['Targets']:
            if t['At']==i:target=(f(t['X']),f(t['Z']));targets+=1;manual=(0.0,0.0)
        for entry in c['Manual']:
            if entry['At']==i:manual=(f(entry['X']),f(entry['Z']));target=None
        if i in c['Clears']:target=None;manual=(0.0,0.0)
        for p in c['Pauses']:
            if p['At']==i:paused=p['Value']
        if c.get('Correction',{}).get('At')==i:
            x,z=map(f,c['Correction']['Position']);y=99;xrow=[bits(1),0,0];zrow=[0,0,bits(1)]
        dx,dz=(target[0]-x,target[1]-z) if target else (0,0);distance=math.sqrt(dx*dx+dz*dz);vx=vz=0.0
        if manual!=(0.0,0.0) and not paused:
            vx,vz,adjust=manual_velocity(manual,speed);stats['ULPAdjustments']+=adjust
        elif target and not paused and distance>radius:
            travel=min(speed*seconds,distance-radius*.5)*(1-1/1048576)
            vx,vz=f(dx/distance*travel/seconds),f(dz/distance*travel/seconds)
            for attempt in range(8):
                nx,nz=f(x+f(vx*seconds)),f(z+f(vz*seconds));projection=(nx-x)*dx+(nz-z)*dz
                if 0<=projection<=distance*distance:break
                vx,vz=f(vx*.5),f(vz*.5)
            else:vx=vz=0.0
        nx,nz=f(x+f(vx*seconds)),f(z+f(vz*seconds));hit=height(nx,nz)
        if hit is None:
            rows.append(dict(Index=i,Mode='Faulted',AttemptedUpdates=i+1,CompletedUpdates=i,SuccessfulTargets=targets,Time=(i+1)*c['Step'],Position=[bits(x),bits(y),bits(z)],Speed=rows[-1]['Speed'] if rows else 0,X=xrow,Y=[0,bits(1),0],Z=zrow,Manual=[bits(manual[0]),bits(manual[1])],MovementSpeed=bits(speed)));break
        magnitude=f(math.sqrt(f(f(vx*vx)+f(vz*vz))))
        if magnitude>0:xrow=[bits(f(vz/magnitude)),0,bits(f(vx/magnitude))];zrow=[bits(-f(vx/magnitude)),0,bits(f(vz/magnitude))]
        # Check target-plane overshoot from rounded coordinates independently of native output.
        assert (nx-x)*dx+(nz-z)*dz<=distance*distance
        x,z=nx,nz;y=f(hit['Height']+.25);remaining=math.hypot(target[0]-x,target[1]-z) if target else 0
        rows.append(dict(Index=i,Mode='Paused' if paused else 'Manual' if manual!=(0.0,0.0) else 'Idle' if target is None else 'Arrived' if remaining<=radius else 'Moving',AttemptedUpdates=i+1,CompletedUpdates=i+1,SuccessfulTargets=targets,Time=(i+1)*c['Step'],Position=[bits(x),bits(y),bits(z)],Speed=bits(magnitude),X=xrow,Y=[0,bits(1),0],Z=zrow,Manual=[bits(manual[0]),bits(manual[1])],MovementSpeed=bits(speed)))
    return rows

def compute(source):
    surface=runpy.run_path(str(ROOT/'poc/round62-tests/Rendering/verify_surface.py'));q,reference,n,m=surface['load']()
    chunks=[q.enrich(c,reference.expected(c,n,m),reference,n,m) for c in source['Terrain']]
    for chunk in chunks:
        for patch in chunk['Patches']:patch['NormalBits']=[[bits(float(x)) for x in row] for row in patch['Normals']]
    stats=dict(ULPAdjustments=0);wanted=[dict(Name=c['Name'],Rows=expected(c,[dict(Chunks=chunks)],surface,stats)) for c in source['Cases']]
    return wanted,stats

def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for k in ['runtime','terrain','out']:p.add_argument('--'+k,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted,stats=compute(source)
    if a.mode=='generate':
        peer=guard(out.with_suffix('.expected.json'),ROOT);peer.write_text(json.dumps(dict(Cases=wanted,Stats=stats),separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('PINNED_MOVEMENT_SPEED_EXPECTED_BEFORE_NATIVE',stats);return
    assert a.native;actual=json.loads(a.native.read_text());assert len(actual['Cases'])==6;count=0
    for got,want in zip(actual['Cases'],wanted):
        assert got==want,(got['Name'],got,want);count+=len(want['Rows'])
    assert count==96 and stats['ULPAdjustments']>0 and actual['NegativeChecks']>=100
    report=dict(Passed=True,Cases=6,ComparedUpdates=count,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],IndependentF32GeometryPoseSpeedClock=True,ExpectedGeneratedBeforeNative=True,ULPAdjustments=stats['ULPAdjustments'],AnalogUnitDiskPolicy=True,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN,OutputSeedUsed=False,OriginalExecuted=False)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('MOVEMENT_SPEED_REFERENCE_PASS',json.dumps(report))

if __name__=='__main__':main()
