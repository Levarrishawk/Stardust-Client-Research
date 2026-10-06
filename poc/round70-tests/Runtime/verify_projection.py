"""Independent supplied native-camera projection plus frozen terrain trajectory oracle."""
import argparse,hashlib,json,math,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def value(x):return struct.unpack('<f',struct.pack('<I',x))[0]
def f(x):return value(bits(x))
def project(keys,forward):
    x,y=keys;fx,fy,fz=forward
    if not all(math.isfinite(v) and abs(v)<=1 for v in keys):raise ValueError('keys')
    if not all(math.isfinite(v) and abs(v)<=1000000 for v in forward):raise ValueError('forward')
    horizontal=math.sqrt(fx*fx+fz*fz)
    if horizontal<1e-6:raise ValueError('horizontal')
    fx/=horizontal;fz/=horizontal;length=math.hypot(x,y)
    if length>1:x/=length;y/=length
    dx,dz=f(fz*x-fx*y),f(-fx*x-fz*y);adjust=0
    def outside():return dx*dx+dz*dz>1 or f(math.sqrt(f(f(dx*dx)+f(dz*dz))))>1
    while adjust<4 and outside():
        dx=value(bits(dx)-1) if dx else dx;dz=value(bits(dz)-1) if dz else dz;adjust+=1
    assert not outside()
    return (dx,dz),adjust
def mapping(keys,forward):return dict(Keys=[bits(x) for x in keys],Forward=[bits(x) for x in forward])
def fixture(runtime,terrain):
    directions=[(0,0,1),(1,0,0),(0,0,-1),(-1,0,0),(3,0,4),(3,7,4),(-.3,-.8,.7),(1000000,1000000,-999999),(f(1.000001e-6),1,0)]
    keysets=[(0,-1),(1,-1),(.5,-.25),(0,0)]
    mappings=[mapping(k,fw) for fw in directions for k in keysets]
    # Exact input high bits and a norm-rounding witness selected before native.
    mappings.append(mapping((value(0x3F7FFFFF),value(0xBF000001)),(value(0x3EAAAAAB),value(0x3F000001),value(0x3F555555))))
    witness=next(mapping((1,-1),(f(x/100),0,f(z/100))) for x in range(1,101) for z in range(1,101) if project((1,-1),(f(x/100),0,f(z/100)))[1]>0)
    mappings.append(witness)
    invalid=[mapping(k,fw) for k,fw in [((1.000001,0),(0,0,1)),((0,float('nan')),(0,0,1)),((float('inf'),0),(0,0,1)),((0,0),(0,1,0)),((0,-1),(f(1e-6),0,0)),((0,-1),(0,float('nan'),1)),((0,-1),(0,0,float('inf'))),((0,-1),(1000001,0,1)),((0,-1),(0,1000001,1)),((0,-1),(value(0x7F7FFFFF),0,1)),((0,-1),(value(1),1,value(1)))]]
    cases=[]
    def put(name,events,targets=[],pauses=[],correction=None):
        manual=[]
        for at,keys,forward in events:
            entry=mapping(keys,forward);direction,_=project(tuple(map(value,entry['Keys'])),tuple(map(value,entry['Forward'])));entry.update(At=at,X=direction[0],Z=direction[1]);manual.append(entry)
        c=dict(Name=name,Start=[7.8,7.8],Targets=[dict(At=a,X=x,Z=z) for a,x,z in targets],Manual=manual,Clears=[],Pauses=[dict(At=a,Value=v) for a,v in pauses],Updates=12,Speed=3,Radius=.01,Step=10000,Sparse=False,Speeds=[dict(At=5,Value=4)])
        if correction:c['Correction']=correction
        cases.append(c)
    put('yaw-cardinal-diagonal',[(0,(0,-1),(0,0,1)),(3,(0,-1),(1,0,0)),(6,(1,-1),(3,4,4)),(9,(0,0),(-1,0,0))])
    put('analog-tilt-pause-correction',[(0,(.5,-.25),(3,7,4)),(7,(-1,-1),(-.3,-.8,.7))],pauses=[(2,True),(6,False)],correction=dict(At=3,Position=[8.1,7.65]))
    put('manual-point-handoff',[(0,(1,-1),(3,0,4)),(8,(0,0),(0,0,1))],targets=[(4,8.5,8.5)])
    put('point-manual-nearvertical',[(3,(0,-1),(f(1.000001e-6),1,0)),(6,(1,-1),(1000000,1000000,-999999)),(10,(0,0),(0,0,1))],targets=[(0,8.5,8.5)])
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],Cases=cases,Mappings=mappings,InvalidMappings=invalid)
def compute(source):
    projections=[];adjustments=0
    for e in source['Mappings']:
        v,n=project(tuple(map(value,e['Keys'])),tuple(map(value,e['Forward'])));projections.append(list(map(bits,v)));adjustments+=n
    for e in source['InvalidMappings']:
        try:project(tuple(map(value,e['Keys'])),tuple(map(value,e['Forward'])))
        except ValueError:pass
        else:raise AssertionError('invalid projection admitted')
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'))
    wanted,stats=peer['compute'](source)
    return dict(Cases=wanted,Mappings=projections),dict(ProjectionULPAdjustments=adjustments,DriverULPAdjustments=stats['ULPAdjustments'])
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for key in ['runtime','terrain','out']:p.add_argument('--'+key,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted,stats=compute(source)
    if a.mode=='generate':
        guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(dict(Expected=wanted,Stats=stats),separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('EXPECTED_BEFORE_NATIVE',len(wanted['Mappings']),stats);return
    assert a.native;actual=json.loads(a.native.read_text());assert actual['Cases']==wanted['Cases'] and actual['Mappings']==wanted['Mappings']
    count=sum(len(c['Rows']) for c in wanted['Cases']);assert count==48 and len(wanted['Mappings'])==38 and stats['ProjectionULPAdjustments']>0
    assert actual['NegativeChecks']==85
    report=dict(Passed=True,Cases=4,ComparedUpdates=count,MappingVectors=38,InvalidMappings=11,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],IndependentProjectionF32GeometryPoseSpeedBasisClock=True,ExpectedGeneratedBeforeNative=True,OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN,**stats)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('CAMERA_PROJECTION_PASS',json.dumps(report))
if __name__=='__main__':main()
