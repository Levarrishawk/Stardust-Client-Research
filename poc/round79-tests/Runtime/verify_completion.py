"""Before-native actual-prefix completion classification from independent F32 terrain trajectories."""
import argparse,hashlib,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def fixture(runtime,terrain):
    def case(name,maximum,updates,stop=-1,point=False,fault=False,override=False,start=[7.8,7.8]):
        return dict(Name=name,Maximum=maximum,Updates=updates,StopAt=stop,Point=point,Fault=fault,Override=override,Start=start,Hold=not fault)
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],Cases=[case('initial',4,0),case('budget',3,3,override=True),case('requested',4,2,stop=1,override=True),case('fault',4,2,fault=True,override=True,start=[15.95,4]),case('incomplete',4,1,point=True,override=True)])
def report(maximum,row,stopped=False,faulted=False,target=False,finish=False):
    reason=2 if faulted else 3 if not stopped else 1 if finish or row['CompletedUpdates']<maximum else 0
    return dict(Reason=reason,MaximumUpdates=maximum,CompletedUpdates=row['CompletedUpdates'],AttemptedUpdates=row['AttemptedUpdates'],Stopped=stopped,Faulted=faulted,Mode=row['Mode'],Speed=row['Speed'],TargetActive=target,FinishRequested=finish)
def compute(source):
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'));models=[]
    for c in source['Cases']:
        models.append(dict(Name=c['Name'],Start=c['Start'],Targets=[dict(At=0,X=8.5,Z=7.8)] if c['Point'] else [],Manual=[] if c['Point'] else [dict(At=0,X=1,Z=0)],Pauses=[],Clears=[c['StopAt']] if c['StopAt']>=0 else [c['Maximum']-1] if c['Updates']==c['Maximum'] else [],Speeds=[],Updates=c['Updates'],Speed=3,Radius=.001,Step=10000,Sparse=False))
    traces,_=peer['compute'](dict(Terrain=source['Terrain'],Cases=models));wanted=[]
    for c,t in zip(source['Cases'],traces):
        records=[];maximum=c['Maximum']
        if c['Name']=='initial':records.append(dict(Name='initial',Completion=report(maximum,dict(CompletedUpdates=0,AttemptedUpdates=0,Mode='Idle',Speed=0))))
        else:
            if c['StopAt']>=0:records.append(dict(Name='pending-stop',Completion=report(maximum,t['Rows'][c['StopAt']-1],finish=True)))
            stopped=c['Name'] in ['budget','requested'];last=t['Rows'][-1]
            records.append(dict(Name='actual',Completion=report(maximum,last,stopped=stopped,faulted=c['Fault'],target=c['Point'])))
            if c['Override']:records.append(dict(Name='explicit-finish',Completion=report(maximum,last,stopped=stopped,faulted=c['Fault'],target=c['Point'],finish=True)))
        wanted.append(dict(Name=c['Name'],Records=records))
    return wanted
def normalize(actual):
    for c in actual:
        for row in c['Records']:row['Completion']['Speed']=bits(row['Completion']['Speed'])
    return actual
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for key in ['runtime','terrain','out']:p.add_argument('--'+key,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted=compute(source)
    if a.mode=='generate':
        guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(dict(Cases=wanted),separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('COMPLETION_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text());got=normalize(actual['Cases'])
    for g,w in zip(got,wanted):assert g==w,(g,w)
    assert len(got)==len(wanted)==5 and {row['Completion']['Reason'] for c in got for row in c['Records']}=={0,1,2,3}
    report_value=dict(Passed=True,Cases=5,ComparedReports=sum(len(c['Records']) for c in wanted),AllFourReasons=True,ActualFaultCompleted=1,ActualFaultAttempted=2,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentGroundedF32PrefixAndClassification=True,PrimitiveSerialization=True,OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN)
    out.write_text(json.dumps(report_value,indent=2)+'\n',encoding='utf-8');print('SESSION_COMPLETION_PASS',json.dumps(report_value))
if __name__=='__main__':main()
