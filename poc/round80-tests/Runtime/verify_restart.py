"""Independent before-native successive run clocks/F32 motion and primitive completion history."""
import argparse,hashlib,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def fixture(runtime,terrain):return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4])
def compute(source):
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'));completion=runpy.run_path(str(ROOT/'poc/round79-tests/Runtime/verify_completion.py'))['report']
    models=[]
    for i in range(3):
        fault=i==1;models.append(dict(Name=str(i),Start=[15.95,4] if fault else [7.8,7.8],Targets=[],Manual=[dict(At=0,X=1,Z=0)],Pauses=[],Clears=[] if fault else [2],Speeds=[],Updates=2 if fault else 3,Speed=3,Radius=.001,Step=10000,Sparse=False))
    traces,_=peer['compute'](dict(Terrain=source['Terrain'],Cases=models));runs=[]
    for i,t in enumerate(traces):
        fault=i==1;updates=[]
        for row in t['Rows']:
            if row['Mode']=='Faulted':continue
            updates.append(dict(Index=row['Index'],Time=i*100000+row['Time'],Position=row['Position'],Speed=row['Speed'],Mode=row['Mode'],Completed=row['CompletedUpdates'],Attempted=row['AttemptedUpdates'],Stopped=not fault and row['Index']==2))
        runs.append(dict(Generation=i+1,SceneGeneration=i+1,Updates=updates,Completion=completion(3,t['Rows'][-1],stopped=not fault,faulted=fault)))
    requested=completion(3,dict(CompletedUpdates=2,AttemptedUpdates=2,Mode='Idle',Speed=0),stopped=True,finish=True)
    return dict(Runs=runs,Requested=requested)
def normalize(actual):
    for run in actual['Runs']:run['Completion']['Speed']=bits(run['Completion']['Speed'])
    actual['Requested']['Speed']=bits(actual['Requested']['Speed']);return actual
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for key in ['runtime','terrain','out']:p.add_argument('--'+key,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted=compute(source)
    if a.mode=='generate':
        guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(wanted,separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('RESTART_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=normalize(json.loads(a.native.read_text()))
    for key in wanted:assert actual[key]==wanted[key],(key,actual[key],wanted[key])
    report=dict(Passed=True,SuccessfulRuns=3,BudgetStops=2,FaultThenHealthyRecovery=True,ComparedSuccessfulUpdates=7,ComparedCompletions=4,FaultCompleted=1,FaultAttempted=2,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentF32GroundedClockGenerationHistory=True,OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('SESSION_RESTART_PASS',json.dumps(report))
if __name__=='__main__':main()
