"""Retain the prior independent behavior oracle and verify actual primitive outcome snapshots."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('outcome79_prior77',ROOT/'poc/round77-tests/Integration/configured_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
p,t,fixture,log,out=sys.argv[1:];p,t,fixture=[json.loads(Path(x).read_text()) for x in (p,t,fixture)];text=Path(log).read_text(encoding='utf-8-sig')
value=m.verify(p,t,fixture,text)
records=m.old.records
owner,=records(text,'configured_session_owner_cleanup=');assert owner==dict(Disposed=True,OwnedPointDisposed=True,OwnedControllerDisposed=True,BorrowedWorldPreserved=True,BorrowedTerrainPreserved=True)
owner,=records(text,'configured_root_owner_cleanup=');assert owner==dict(Disposed=True,Closed=True,Samples=401,Drained=39,HistoryPreserved=True,BorrowedQueuePreserved=True,PendingMessages=0,RetainedPackets=0)
owner,=records(text,'configured_limits_hud_cleanup=');assert owner==dict(Retired=True,RetirementCount=1,RefreshCount=402,BorrowedOwnerPreserved=True)
completion,=records(text,'offline_completion_summary=');assert completion==dict(Reason='FinishRequested',MaximumUpdates=6000,CompletedUpdates=401,AttemptedUpdates=401,Stopped=True,Faulted=False,Mode='Idle',Speed=0,TargetActive=False)
roots,=records(text,'offline_root_completion_summary=');assert roots==dict(Samples=401,Queued=39,Drained=39,NextSequence=39,SessionFaulted=False,TransportState='Established',QueueCount=0,QueueBytes=0,PendingMessages=0,RetainedPackets=0,CleanDrain=True)
caption,=records(text,'offline_outcome_caption=');assert caption==dict(Reason='FinishRequested',Caption='Offline preview outcome\nReason: Finished with Escape\nUpdates: 401 / 6000\nOffline root frames: 39\nBuffers: empty',Source='ActualControllerAndRootSnapshot',SocketCreated=False)
cleanup,=records(text,'offline_outcome_hud_cleanup=');assert cleanup==dict(Retired=True,RetirementCount=1,RefreshCount=1,BorrowedOwnerPreserved=True)
assert not records(text,'offline_outcome_capture_failed=')
value.update(PrimitiveCompletionReason='FinishRequested',PrimitiveRootCleanDrain=True,OutcomeCaptionVerified=True,OutcomeOwnerRetired=True)
out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
