"""Authored expected inspection snapshots/control histories, generated before native."""
import json,sys
from pathlib import Path
MODES=['loading','active','idle','reject','terminal','cap','teardown']
def status(mode,after=False,final=False):
 max_runs=1 if mode=='cap' else 2;started=2 if mode=='idle' and after else 1
 report=mode in ['idle','reject','cap','terminal'];terminal=mode=='terminal';present=mode in ['loading','active','terminal','teardown'] or mode=='idle' and after;ready=present and mode!='loading'
 state='Terminal' if terminal else 'Active' if ready else 'Loading' if present else 'RunLimit' if mode=='cap' else 'Idle'
 n=dict(State=state,StartedRuns=started,MaximumRuns=max_runs,RemainingRuns=max_runs-started,ChildPresent=present,CurrentReady=ready,Terminal=terminal,Closing=False,RestartEnabled=mode in ['idle','reject'] and not after or mode=='reject',CloseEnabled=True,Reason='Faulted' if terminal else 'BudgetCompleted' if report else None,CompletedUpdates=2 if terminal else 3 if report else None,AttemptedUpdates=3 if report else None,Faulted=terminal if report else None,CleanupSucceeded=not terminal if report else None,Actors=1 if terminal else 0 if report else None,OutstandingArchiveJobs=1 if terminal else 0 if report else None,ArchiveCalls=3 if report else None,ArchiveClosed=2 if terminal else 3 if report else None,RootFrames=1 if report else None,FailureType='SyntheticFailure' if terminal else None,CleanupFailureTypes='SyntheticCleanupFailure' if terminal else '' if report else None,CloseRequests=0,WindowCloseConnected=True,QuitIssued=False)
 if final:
  n.update(ChildPresent=False,CurrentReady=False,RestartEnabled=False,CloseEnabled=False,WindowCloseConnected=False)
  if mode=='teardown':n.update(State='Idle')
  else:
   n.update(State='Closing',Closing=True,CloseRequests=1,QuitIssued=True)
   if mode in ['loading','active','idle']:
    n.update(Reason='Incomplete',CompletedUpdates=0,AttemptedUpdates=0,Faulted=False,CleanupSucceeded=True,Actors=0,OutstandingArchiveJobs=0,ArchiveCalls=0,ArchiveClosed=0,RootFrames=0,FailureType='System.OperationCanceledException',CleanupFailureTypes='')
 return n

def caption(s):
 def n(v):return str(v) if v is not None else 'awaiting report'
 reason={'BudgetCompleted':'Budget completed','FinishRequested':'Finished with Escape','Faulted':'Stopped after an error','Incomplete':'Incomplete'}.get(s['Reason'],'No completed report')
 available='Closing: draining the preview before exit.' if s['Closing'] else 'Restart blocked. Close exits the preview.' if s['Terminal'] else 'Preview active. Close drains and exits.' if s['ChildPresent'] else 'Run limit reached. Restart is disabled.' if s['RemainingRuns']==0 else 'Restart available. Close exits the preview.'
 return '\n'.join(['Offline preview inspection | '+s['State'],f"Runs started: {s['StartedRuns']} / {s['MaximumRuns']} | Remaining: {s['RemainingRuns']}",'Last run: '+reason+' | Fault: '+('unknown' if s['Faulted'] is None else 'yes' if s['Faulted'] else 'no'),f"Last updates: {n(s['CompletedUpdates'])} | Attempted: {n(s['AttemptedUpdates'])}",'Cleanup: '+('awaiting report' if s['CleanupSucceeded'] is None else 'drained' if s['CleanupSucceeded'] else 'failed')+f" | Actors: {n(s['Actors'])} | Jobs: {n(s['OutstandingArchiveJobs'])}",f"Archives closed: {n(s['ArchiveClosed'])} / {n(s['ArchiveCalls'])} | Root frames: {n(s['RootFrames'])}",available])
def expected(mode,close,gpu=False):
 s=status(mode);rows=[dict(Phase='initial',Status=s,Caption=caption(s),Factories=1,RestartRequests=0,RestartAccepted=0)]
 if mode in ['idle','reject','terminal','cap']:
  after=status(mode,True);rows.append(dict(Phase='after-restart',Status=after,Caption=caption(after),Factories=2 if mode in ['idle','reject'] else 1,RestartRequests=1 if mode in ['idle','reject'] else 0,RestartAccepted=1 if mode=='idle' else 0))
 checks=(6 if mode=='teardown' else 5 if close=='signal' else 8 if mode=='idle' else 7 if mode in ['reject','terminal','cap'] else 6)+int(gpu)
 return dict(Passed=True,Checks=checks,Gpu=gpu,Mode=mode,CloseMode=close,Rows=rows,Final=status(mode,mode=='idle',True),Factories=2 if mode in ['idle','reject'] else 1,SyntheticCloseSignal=close=='signal' or mode=='teardown',PhysicalOsClose=False,Source='SyntheticAuthoredCompletionAndReadyFixtures',Caption=caption(s))
def equal_types(a,b):
 assert type(a) is type(b),(a,b)
 if isinstance(a,dict):
  assert set(a)==set(b)
  for k in a:equal_types(a[k],b[k])
 elif isinstance(a,list):
  assert len(a)==len(b)
  for x,y in zip(a,b):equal_types(x,y)
 else:assert a==b,(a,b)
def generate(root):
 cases=[(m,'signal' if m=='teardown' else 'gui',False) for m in MODES]+[('active','signal',False)]+[(m,'gui',True) for m in ['cap','idle','terminal']]
 for mode,close,gpu in cases:
  name=mode+'-'+close+('-gpu' if gpu else '-headless');(root/(name+'-expected.json')).write_text(json.dumps(expected(mode,close,gpu),indent=2)+'\n')
def verify(path):
 n=json.loads(path.read_text());expect=path.with_name(path.stem+'-expected.json');e=json.loads(expect.read_text());equal_types(n,e);assert expect.stat().st_mtime<path.stat().st_mtime
 r=dict(Passed=True,Checks=n['Checks'],Mode=n['Mode'],CloseMode=n['CloseMode'],Gpu=n['Gpu'],IndependentBeforeNative=True,PhysicalOsClose=False,SyntheticCloseSignal=n['SyntheticCloseSignal'],Source=n['Source']);path.with_name(path.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':
 if sys.argv[1]=='--generate':generate(Path(sys.argv[2]))
 else:verify(Path(sys.argv[1]))
