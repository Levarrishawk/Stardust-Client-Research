"""Independent double/F32 native pivot following and frozen grounded accepted-position reference."""
import argparse,hashlib,json,math,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21';TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def value(x):return struct.unpack('<f',struct.pack('<I',x))[0]
def f(x):return value(bits(x))
def fixture(runtime,terrain):
    pure=[]
    def put(name,positions,offset=[.125,2,-.25],initial=[0,0,0],rebases={},step=10000,response=.1,clock=0):
        events=[]
        for i,p in enumerate(positions):
            for new in rebases.get(i,[]):events.append(dict(Kind='Rebase',Time=clock+i*step,Position=list(map(f,new))))
            events.append(dict(Kind='Advance',Time=clock+(i+1)*step,Position=list(map(f,p))))
        pure.append(dict(Name=name,Initial=list(map(f,initial)),Offset=list(map(f,offset)),InitialClock=clock,Step=step,Response=response,Updates=len(positions),Events=events))
    put('step-settle',[[10,2,-4]]*12)
    put('moving-player',[[i*.3,i*.1,-i*.2] for i in range(1,17)])
    put('pause-idle-settle',[[i,0,i*.5] for i in range(1,5)]+[[4,0,2]]*8)
    put('rebase-unchanged-clock',[[i*.2,1,-i*.1] for i in range(12)],rebases={3:[[50,10,-50]],8:[[2,1,3],[2,1,3]]})
    put('highbits-near-bound',[[value(0x49742360)+i*.0625,-999990+i*.125,value(0x3F800001)] for i in range(8)],offset=[.125,-.25,.0625],initial=[999990,-999990,value(0x3F7FFFFF)],step=1000,clock=1234567)
    driver=dict(Name='actual-grounded-follow',Start=[7.8,7.8],Targets=[dict(At=12,X=8.5,Z=8.5)],Manual=[dict(At=0,X=1,Z=0),dict(At=10,X=1,Z=1),dict(At=16,X=0,Z=0)],Clears=[],Pauses=[dict(At=3,Value=True),dict(At=8,Value=False)],Correction=dict(At=5,Position=[8.1,7.65]),Updates=20,Speed=3,Radius=.01,Step=10000,Sparse=False,Speeds=[dict(At=6,Value=4),dict(At=14,Value=3)])
    return dict(Pure=pure,Driver=driver,DriverOffset=[.125,1.25,-.25],Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4])
class Follow:
    def __init__(self,position,offset,clock=0,step=10000,response=.1):
        self.offset=list(map(f,offset));self.pivot=self.target(position);self.time=clock;self.step=step;self.factor=math.exp(-(step/1000000)/response);self.updates=self.rebases=0
    def target(self,p):
        target=[f(v)+o for v,o in zip(p,self.offset)];assert all(math.isfinite(v) and abs(v)<=1000000 for v in target);return list(map(f,target))
    def event(self,p,time,rebase=False):
        exact=[f(v)+o for v,o in zip(p,self.offset)];assert all(math.isfinite(v) and abs(v)<=1000000 for v in exact)
        if rebase:assert time==self.time;self.pivot=list(map(f,exact));self.rebases+=1
        else:
            assert time==self.time+self.step;self.pivot=[f(t+(old-t)*self.factor) for t,old in zip(exact,self.pivot)];self.updates+=1;self.time=time
        return dict(Pivot=list(map(bits,self.pivot)),Target=list(map(bits,exact)),Time=self.time,Updates=self.updates,Rebases=self.rebases)
def compute(source):
    pure=[]
    for c in source['Pure']:
        follow=Follow(c['Initial'],c['Offset'],c['InitialClock'],c['Step'],c['Response']);rows=[follow.event(e['Position'],e['Time'],e['Kind']=='Rebase') for e in c['Events']];pure.append(dict(Name=c['Name'],Rows=rows))
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'));input_source=source|dict(Cases=[source['Driver']]);wanted,_=peer['compute'](input_source)
    surface=runpy.run_path(str(ROOT/'poc/round62-tests/Rendering/verify_surface.py'));q,reference,n,m=surface['load']();chunks=[q.enrich(c,reference.expected(c,n,m),reference,n,m) for c in source['Terrain']]
    for c in chunks:
        for p in c['Patches']:p['NormalBits']=[[bits(float(v)) for v in row] for row in p['Normals']]
    x,z=map(f,source['Driver']['Start']);hit=surface['hit'](dict(Chunks=chunks),[surface['b64'](x),surface['b64'](z)],surface['child_options']({}));initial=[x,f(hit['Height']+.25),z]
    follow=Follow(initial,source['DriverOffset']);driver=[]
    for i,row in enumerate(wanted[0]['Rows']):
        c=source['Driver'].get('Correction',{})
        if c.get('At')==i:
            x,z=map(f,c['Position']);rebased=follow.event([x,99,z],i*source['Driver']['Step'],True);driver.append(dict(Kind='Rebase',Index=i,Follow=rebased,Driver=None))
        followed=follow.event(list(map(value,row['Position'])),row['Time']);driver.append(dict(Kind='Advance',Index=i,Follow=followed,Driver=dict(Position=row['Position'],Speed=row['Speed'],Time=row['Time'],Mode=row['Mode'])))
    return dict(Pure=pure,Driver=driver)
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for k in ['runtime','terrain','out']:p.add_argument('--'+k,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted=compute(source)
    if a.mode=='generate':guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(wanted,separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('FOLLOW_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text())
    assert actual['Pure']==wanted['Pure'],('Pure',actual['Pure'],wanted['Pure']);assert actual['Driver']==wanted['Driver'],('Driver',actual['Driver'],wanted['Driver'])
    actions=sum(len(c['Rows']) for c in wanted['Pure']);assert actions==63 and len(wanted['Driver'])==21 and actual['NegativeChecks']>=600
    report=dict(Passed=True,PureCases=5,PureUpdates=60,PureRebases=3,ComparedPureActions=63,ActualGroundedDriverUpdates=20,DriverRebases=1,ComparedDriverActions=21,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentDoubleF32PivotTargetClock=True,IndependentGroundedAcceptedPositions=True,PublicationComparison='exact F32 bits/integer',OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('CAMERA_PIVOT_FOLLOW_PASS',json.dumps(report))
if __name__=='__main__':main()
