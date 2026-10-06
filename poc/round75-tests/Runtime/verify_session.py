"""Independent actual driver timeline for owned session pause/stop; expected before native."""
import argparse,hashlib,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21';TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def fixture(runtime,terrain):
    cases=[]
    def put(name,maximum,stop=-1,manual=[],targets=[],pauses=[],correction=None):
        updates=stop+1 if stop>=0 else maximum;final=updates-1;actualpauses=[dict(At=a,Value=v) for a,v in pauses]+[dict(At=final,Value=False)]
        c=dict(Name=name,Start=[7.8,7.8],Maximum=maximum,StopAt=stop,Updates=updates,Manual=[dict(At=a,X=x,Z=z) for a,x,z in manual],Targets=[dict(At=a,X=x,Z=z) for a,x,z in targets],IntentPauses=[dict(At=a,Value=v) for a,v in pauses],Pauses=actualpauses,Clears=[final],Speeds=[],Speed=3,Radius=.01,Step=10000,Sparse=False)
        if correction:c['Correction']=correction
        cases.append(c)
    put('manual-pause-budget-stop',12,manual=[(0,1,0)],pauses=[(2,True),(6,False)])
    put('point-pause-request-stop',20,stop=8,targets=[(0,8.5,8.5)],pauses=[(2,True),(5,False)])
    put('request-stop-while-paused',20,stop=5,manual=[(0,1,1)],pauses=[(2,True)])
    put('point-budget-final-zero',12,targets=[(0,8.5,8.5)])
    put('manual-correction-resume-stop',16,manual=[(0,1,1)],pauses=[(2,True),(8,False)],correction=dict(At=5,Position=[8.1,7.65]))
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],Cases=cases)
def compute(source):
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'));wanted,_=peer['compute'](source)
    for c,w in zip(source['Cases'],wanted):
        paused=False
        for i,row in enumerate(w['Rows']):
            for p in c['IntentPauses']:
                if p['At']==i:paused=p['Value']
            stopped=i==c['Updates']-1
            row['Session']=dict(CompletedUpdates=i+1,Paused=False if stopped else paused,StopRequested=stopped,Stopped=stopped,IsFaulted=False,FaultType=None)
    return wanted
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for k in ['runtime','terrain','out']:p.add_argument('--'+k,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT);source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted=compute(source)
    if a.mode=='generate':guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(dict(Cases=wanted),separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('SESSION_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text());assert actual['Cases']==wanted
    count=sum(len(c['Rows']) for c in wanted);assert count==55 and all(c['Rows'][-1]['Speed']==0 and c['Rows'][-1]['Mode']=='Idle' for c in wanted)
    report=dict(Passed=True,Cases=5,ComparedUpdates=count,SuccessfulZeroStops=5,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentGroundedF32PoseSpeedBasisClockAndSession=True,NoExtraFinalTick=True,OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('OFFLINE_SESSION_CONTROLLER_PASS',json.dumps(report))
if __name__=='__main__':main()
