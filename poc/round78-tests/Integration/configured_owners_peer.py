"""Verify unchanged configured behavior and the new explicit owner teardown receipts."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('owners78_prior77',ROOT/'poc/round77-tests/Integration/configured_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
p,t,fixture,log,out=sys.argv[1:];p,t,fixture=[json.loads(Path(x).read_text()) for x in (p,t,fixture)];text=Path(log).read_text(encoding='utf-8-sig')
value=m.verify(p,t,fixture,text)
ready,=m.old.records(text,'configured_session_owner_ready=');assert ready==dict(MaximumUpdates=6000,MaximumClicks=32,ActualOwnedPointDriver=True,MatchingSettings=True,SocketCreated=False)
session,=m.old.records(text,'configured_session_owner_cleanup=');assert session==dict(Disposed=True,OwnedPointDisposed=True,OwnedControllerDisposed=True,BorrowedWorldPreserved=True,BorrowedTerrainPreserved=True)
root,=m.old.records(text,'configured_root_owner_cleanup=');assert root==dict(Disposed=True,Closed=True,Samples=401,Drained=39,HistoryPreserved=True,BorrowedQueuePreserved=True,PendingMessages=0,RetainedPackets=0)
hud,=m.old.records(text,'configured_limits_hud_cleanup=');assert hud==dict(Retired=True,RetirementCount=1,RefreshCount=402,BorrowedOwnerPreserved=True)
value.update(ConfiguredSessionOwner=True,OfflineRootOwner=True,LimitsDisplayOwner=True,OwnedCleanupOrderVerified=True)
out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
