"""Check one declared contradictory settings profile is rejected before world work."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p,log,out=sys.argv[1:];p=json.loads(Path(p).read_text());text=Path(log).read_text(encoding='utf-8-sig')
assert p['Version']==13 and p['SpeedBits']==1077936129 and p['WalkSpeedBits']==1077936128 and p['PlaygroundSettings']['WalkSpeed']==3
s=importlib.util.spec_from_file_location('contradiction78_prior76',ROOT/'poc/round76-tests/Integration/idle_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
assert 'System.IO.InvalidDataException: Typed settings and adapter fields agree' in text and 'LoadPointTargetArguments' in text
row,=m.records(text,'pump_failure_cleanup=');assert row==dict(calls=0,closed=0,drained=True,expectedInputLoss=False,DriverFault=None,actors=0)
for key in ('resource_state=','point_target_ready=','point_target_state=','configured_session_owner_ready=','soe_root_transport_ready=','point_outbound_root='):assert not m.records(text,key)
assert 'PASS: pumped point_target=' not in text
for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','resource still in use'):assert bad not in text
value=dict(Passed=True,ExpectedAdmissionFailure=True,BeforeWorldCreation=True,ResourceLoads=0,Actors=0,CompletedUpdates=0,OneBitContradiction=True,RollbackClaimed=False)
out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
