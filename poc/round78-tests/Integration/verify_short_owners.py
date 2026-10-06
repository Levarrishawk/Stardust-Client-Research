"""Reuse the independent short-budget oracle and check all configured owner cleanup."""
import importlib.util,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p,t,log,out=sys.argv[1:];out=Path(out);base=out.with_suffix('.budget.json')
assert not out.exists() and not base.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve())
subprocess.run([sys.executable,str(ROOT/'poc/round77-tests/Integration/verify_short_budget.py'),p,t,log,str(base)],check=True)
value=json.loads(base.read_text());text=Path(log).read_text(encoding='utf-8-sig')
s=importlib.util.spec_from_file_location('shortowners78_prior76',ROOT/'poc/round76-tests/Integration/idle_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
ready,=m.records(text,'configured_session_owner_ready=');assert ready==dict(MaximumUpdates=3,MaximumClicks=128,ActualOwnedPointDriver=True,MatchingSettings=True,SocketCreated=False)
session,=m.records(text,'configured_session_owner_cleanup=');assert session==dict(Disposed=True,OwnedPointDisposed=True,OwnedControllerDisposed=True,BorrowedWorldPreserved=True,BorrowedTerrainPreserved=True)
root,=m.records(text,'configured_root_owner_cleanup=');assert root==dict(Disposed=True,Closed=True,Samples=3,Drained=0,HistoryPreserved=True,BorrowedQueuePreserved=True,PendingMessages=0,RetainedPackets=0)
hud,=m.records(text,'configured_limits_hud_cleanup=');assert hud==dict(Retired=True,RetirementCount=1,RefreshCount=4,BorrowedOwnerPreserved=True)
value.update(ConfiguredSessionOwner=True,OfflineRootOwner=True,LimitsDisplayOwner=True,OwnedCleanupOrderVerified=True)
out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
