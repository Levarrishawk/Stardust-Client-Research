"""Independent target/transition oracle plus actual atomic plan host receipts."""
import importlib.util,json,sys,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('target57',ROOT/'poc/round57-tests/Integration/nominal_target_host_peer.py');old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
def counters(g,d):
    model=runpy.run_path(str(ROOT/'poc/round58-tests/Runtime/verify_plan.py'))
    rate=dict(Magnitude=d['Rate']['Magnitude'],Exponent=d['Rate']['Exponent'],Negative=d['Rate']['IsNegative'])
    steps=[dict(Run=s['RunSpeedBits'],Walk=s['WalkSpeedBits'],Flag=s['RunFlag'],Direction=s['DirectionWordsInRecoveredOrder'],Factor=dict(Magnitude=s['Factor']['Magnitude'],Exponent=s['Factor']['Exponent'],Negative=s['Factor']['IsNegative']),Seconds=d['SecondsBits'],Rate=rate) for s in d['Steps']]
    reference=model['evaluate'](dict(Name='host',Initial=d['InitialSpeedBits'],Precision=d['Precision'],Steps=steps))
    assert reference['Reject']=='None'
    for key,want in [('ReservedCopyBytes',reference['Copy']),('ReservedWork',reference['Reserved']),('ActualTargetWork',reference['ActualTarget']),('ActualTransitionWork',reference['ActualTransition'])]:assert g[key]==want,(key,g[key],want)

    for key,value in {'StepCount':60,'TargetEvaluations':60,'TransitionEvaluations':60,'RatesUsed':39,'Equalities':21,'Clamps':3,'TargetF32Stores':120,'TransitionF32Stores':42,'ReservedCopyBytes':38400,'ActualReferences':True,'OriginalEffectiveMovementParity':False}.items():assert g[key]==value,(key,g)
    assert g['ActualWork']==g['ActualTargetWork']+g['ActualTransitionWork']<=g['ReservedWork']
def verify_native(d,a):
    result=old.verify_native(d,a);counters(a['Plan'],d);result.update(AtomicPlan=True,ActualReferences=True);return result
def verify_host(d,t):
    result=old.verify_host(d,t);rows=old.old.records(t,'nominal_speed_plan_ready=');assert len(rows)==1;counters(rows[0],d);result.update(AtomicPlan=True,ActualReferences=True);return result
def verify_negative(d,t):
    assert d['Steps'][-1]['DirectionWordsInRecoveredOrder'][2]==0x7fc00000
    assert old.old.records(t,'nominal_speed_preflight_failure=')==[dict(CompletedSteps=0,DriverConstructed=False,MotionPublication=False)]
    for prefix in ['nominal_speed_plan_ready=','nominal_target_state=','nominal_speed_ready=','nominal_speed_state=']:assert not old.old.records(t,prefix)
    c=old.old.records(t,'pump_failure_cleanup=');assert len(c)==1 and c[0]['calls']==c[0]['closed']==3 and c[0]['drained'] and c[0]['actors']==0
    return dict(Passed=True,PublishedPlanSteps=0,NoNewMotionPublication=True,Actors=0,ArchivesDrained=True)
if __name__=='__main__':
    action,fixture=sys.argv[1:3];assert Path.cwd().resolve()==ROOT
    if action=='generate':old.old.fresh(fixture).write_text(json.dumps(old.generate(),indent=2)+'\n',encoding='utf-8')
    else:
        d=json.loads(Path(fixture).read_text());assert action=='negative' or d==old.generate()
        result=verify_native(d,json.loads(Path(sys.argv[3]).read_text())) if action=='native' else (verify_negative(d,Path(sys.argv[3]).read_text(encoding='utf-8-sig')) if action=='negative' else verify_host(d,Path(sys.argv[3]).read_text(encoding='utf-8-sig')))
        old.old.fresh(sys.argv[4]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
