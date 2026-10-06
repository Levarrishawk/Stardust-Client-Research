"""Independent exact integer frame-slot/drop conservation reference; no engine or motion seed."""
import argparse,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def fixture():
    cases=[]
    def put(name,frames,step=10000,perframe=8,total=2000,framecap=250000):cases.append(dict(Name=name,Frames=frames,Step=step,PerFrame=perframe,TotalCap=total,FrameCap=framecap))
    put('100Hz-to-total-cap',[10000]*2100)
    put('60Hz-exact-integer-partitions',[16666,16667,16667]*450)
    put('varied-zero-late-8-slot-drop',[0,9999,1,250000,16667,0,5000,5000]*200)
    put('fraction-and-seven-slot-cap',[10000,9999,0,1,250000,5000,4999,1],total=7)
    put('one-slot-frame-cap',[250000,0,1000]*100,step=1000,perframe=1,total=100)
    put('maximum-step-and-6000-cap',[250000]*2401,step=100000,perframe=32,total=6000)
    return dict(Cases=cases)
def compute(source):
    cases=[]
    for c in source['Cases']:
        remainder=reserved=dropped_total=elapsed_total=completed=skipped=0;rows=[]
        for index,elapsed in enumerate(c['Frames']):
            assert 0<=elapsed<=c['FrameCap'];elapsed_total+=elapsed;available=remainder+elapsed;due,rem=divmod(available,c['Step'])
            issued=min(due,c['PerFrame'],c['TotalCap']-reserved);dropped=(due-issued)*c['Step'];reserved+=issued;dropped_total+=dropped;remainder=rem
            assert elapsed_total==reserved*c['Step']+dropped_total+remainder
            if index%17==0:skipped+=issued
            else:completed+=issued
            rows.append(dict(Steps=issued,StepMicroseconds=c['Step'],TotalReservedSteps=reserved,RemainderMicroseconds=remainder,DroppedMicroseconds=dropped,TotalDroppedMicroseconds=dropped_total,Exhausted=reserved==c['TotalCap']))
        assert skipped>0 and completed+skipped==reserved
        cases.append(dict(Name=c['Name'],Rows=rows,CallerCompleted=completed,CallerSkipped=skipped))
    return cases
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify']);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT);source=fixture();wanted=compute(source)
    if a.mode=='generate':guard(out.with_suffix('.expected.json'),ROOT).write_text(json.dumps(dict(Cases=wanted),separators=(',',':'))+'\n',encoding='utf-8');out.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8');print('FRAME_EXPECTED_BEFORE_NATIVE');return
    assert a.native;actual=json.loads(a.native.read_text());assert actual['Cases']==wanted
    frames=sum(len(c['Rows']) for c in wanted);reserved=sum(c['Rows'][-1]['TotalReservedSteps'] for c in wanted);dropped=sum(c['Rows'][-1]['TotalDroppedMicroseconds'] for c in wanted)
    assert frames==7759 and all(c['Rows'][-1]['Exhausted'] for c in wanted) and dropped>0
    report=dict(Passed=True,Cases=6,ComparedFrames=frames,TotalReservedAcrossCases=reserved,TotalDroppedMicrosecondsAcrossCases=dropped,NativeChecks=actual['NativeChecks'],NegativeChecks=actual['NegativeChecks'],ExpectedGeneratedBeforeNative=True,IndependentIntegerReceiptsAndConservation=True,CallerCompletionSeparate=True,ExhaustedFractionalCarryAndDrop=True,OutputSeedUsed=False,OriginalExecuted=False,EngineOrRowsUsed=False)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('OFFLINE_FRAME_CLOCK_PASS',json.dumps(report))
if __name__=='__main__':main()
