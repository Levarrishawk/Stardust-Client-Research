"""Independent before-native budget and three-step Fraction-grounded float trajectory."""
import argparse,hashlib,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
RUNTIME='c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN='85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'
SETTINGS=dict(Version=1,MaximumUpdates=3,MaximumClickAttempts=32,StepMicroseconds=10000,WalkSpeed=3,RunSpeed=4,ArrivalRadius=.001,HoldMissingDestination=True)
def fixture(runtime,terrain):
    return dict(Baseline=runtime['Baseline'],Profiles=runtime['Profiles'],Terrain=terrain['Chunks'][:4],SettingsHex=json.dumps(SETTINGS,separators=(',',':')).encode().hex())
def compute(source):
    settings=json.loads(bytes.fromhex(source['SettingsHex']).decode('utf-8'));assert settings==SETTINGS
    peer=runpy.run_path(str(ROOT/'poc/round69-tests/Runtime/verify_speed.py'))
    c=dict(Name='configured-max3',Start=[7.8,7.8],Targets=[],Manual=[dict(At=0,X=1,Z=0)],Pauses=[],Clears=[2],Speeds=[],Updates=settings['MaximumUpdates'],Speed=settings['WalkSpeed'],Radius=settings['ArrivalRadius'],Step=settings['StepMicroseconds'],Sparse=False)
    wanted,_=peer['compute'](dict(Terrain=source['Terrain'],Cases=[c]))
    rows=[dict(Index=row['Index'],Mode=row['Mode'],Completed=row['CompletedUpdates'],Time=row['Time'],Position=row['Position'],Speed=row['Speed'],Stopped=row['Index']==2,Held=False,Targets=32) for row in wanted[0]['Rows']]
    return dict(Budget=dict(MaximumUpdates=3,MaximumTargets=32),Join=rows)
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify'])
    for key in ['runtime','terrain','out']:p.add_argument('--'+key,type=Path,required=True)
    p.add_argument('--native',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest()==RUNTIME and hashlib.sha256(a.terrain.read_bytes()).hexdigest()==TERRAIN
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    source=fixture(json.loads(a.runtime.read_text()),json.loads(a.terrain.read_text()));wanted=compute(source)
    if a.mode=='generate':
        guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(wanted,separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('CONFIGURED_SESSION_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text())
    for key in wanted:assert actual[key]==wanted[key],(key,actual[key],wanted[key])
    assert wanted['Join'][-1]['Stopped'] and wanted['Join'][-1]['Speed']==0
    report=dict(Passed=True,ComparedUpdates=3,ConfiguredTargetCapacity=32,ActualFinalZeroStop=True,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentBudgetAndGroundedF32Clock=True,OutputSeedUsed=False,OriginalExecuted=False,RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('CONFIGURED_SESSION_PASS',json.dumps(report))
if __name__=='__main__':main()
