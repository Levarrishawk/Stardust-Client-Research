"""Check unscripted configured budget completion and its actual visible outcome snapshot."""
import importlib.util,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p,t,log,out=sys.argv[1:];out=Path(out);base=out.with_suffix('.owners.json');assert not out.exists() and not base.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve())
subprocess.run([sys.executable,str(ROOT/'poc/round78-tests/Integration/verify_short_owners.py'),p,t,log,str(base)],check=True)
value=json.loads(base.read_text());text=Path(log).read_text(encoding='utf-8-sig')
s=importlib.util.spec_from_file_location('shortoutcome79_prior76',ROOT/'poc/round76-tests/Integration/idle_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
completion,=m.records(text,'offline_completion_summary=');assert completion==dict(Reason='BudgetCompleted',MaximumUpdates=3,CompletedUpdates=3,AttemptedUpdates=3,Stopped=True,Faulted=False,Mode='Idle',Speed=0,TargetActive=False)
roots,=m.records(text,'offline_root_completion_summary=');assert roots==dict(Samples=3,Queued=0,Drained=0,NextSequence=0,SessionFaulted=False,TransportState='Established',QueueCount=0,QueueBytes=0,PendingMessages=0,RetainedPackets=0,CleanDrain=True)
caption,=m.records(text,'offline_outcome_caption=');assert caption==dict(Reason='BudgetCompleted',Caption='Offline preview outcome\nReason: Budget completed\nUpdates: 3 / 3\nOffline root frames: 0\nBuffers: empty',Source='ActualControllerAndRootSnapshot',SocketCreated=False)
cleanup,=m.records(text,'offline_outcome_hud_cleanup=');assert cleanup==dict(Retired=True,RetirementCount=1,RefreshCount=1,BorrowedOwnerPreserved=True)
assert not m.records(text,'offline_outcome_capture_failed=')
value.update(PrimitiveCompletionReason='BudgetCompleted',PrimitiveRootCleanDrain=True,OutcomeCaptionVerified=True,OutcomeOwnerRetired=True)
out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
