"""Independent persistent restart captions, request histories and retirement prefixes before native."""
import argparse,hashlib,json,os,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
SHA=lambda b:hashlib.sha256(b).hexdigest()
def fixtures():
 states=[dict(Reason=r,CompletedUpdates=c,MaximumUpdates=3,RootFrames=1,QueueCount=0,PendingMessages=0,RetainedPackets=0) for r,c in [('BudgetCompleted',3),('FinishRequested',2)]]
 captions=['Offline preview outcome\nReason: '+r+'\nUpdates: '+str(c)+' / 3\nOffline root frames: 1\nBuffers: empty' for r,c in [('Budget completed',3),('Finished with Escape',2)]]
 rows=[dict(Run=run,Index=i,Caption=cap,Actual=cap,Requests=min(i+1,2),Accepted=int(i>0),Callbacks=min(i+1,2),Retired=False,Disabled=i>0,Children=2) for run,cap in enumerate(captions) for i in range(3)]
 owners=[dict(Run=run,Caption=cap,Requests=2,Accepted=1,Retirements=1,Children=0) for run,cap in enumerate(captions)]
 kinds=['callback-throw','callback-reentry','callback-dispose-reentry','callback-cancel-false','callback-cancel-true','initial-cancel','owner-exit','callback-owner-free','layer-free','layer-queued','off-thread','configure-cancel','invalid-outcome','second-configure','gui-throw','outcome-label-free','outcome-panel-free','callback-label-free','callback-panel-free','button-mouse','button-focus','button-layout'];failures=[]
 for k in kinds:
  called=k.startswith('callback') or k=='gui-throw';requests=int(called);diagnostic=None
  if called or k in ['initial-cancel','layer-queued','outcome-label-free','outcome-panel-free','button-mouse','button-focus','button-layout']:
   returned=called and k not in ['callback-throw','gui-throw'];proposed=(k!='callback-cancel-false') if returned else None
   error='System.IO.IOException' if k in ['callback-throw','gui-throw'] else 'System.OperationCanceledException' if 'cancel' in k else 'System.InvalidOperationException'
   diagnostic=dict(Request=requests,CallbackInvoked=called,CallbackReturned=returned,ProposedRestart=proposed,StateCommitted=False,FailureType=error)
  failures.append(dict(Kind=k,Caption='' if k in ['configure-cancel','invalid-outcome'] else captions[0],Requests=requests,Accepted=0,Callbacks=int(called),Retirements=1,Diagnostic=diagnostic))
 teardown=[dict(Kind=k,OwnerAlive=k not in ['parent-free','parent-queue-free'],Caption=captions[0],Requests=0,Accepted=0,Callbacks=0,Retirements=1) for k in ['parent-free','parent-queue-free','layer-detached-free','layer-queue-free','interface-dispose']]
 return states,dict(Rows=rows,Owners=owners,Failures=failures,Teardown=teardown)
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert SHA(a.exe.read_bytes()).upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round80/frozen');raw=(frozen/'baseline.json').read_bytes();assert SHA(raw)=='d51ed19b0af61626ead8049942d9f12690304e308bd2bceb10126d851cf2b51b';archive=frozen/'baseline.zip';assert SHA(archive.read_bytes())=='3f7adfc1647ce05dcae6bc9967de1eb345d8178e8d572a7d0643be9799579484';by={x['path']:x for x in json.loads(raw)['files']};assert len(by)==4608
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4608 and set(z.namelist())==set(by)
   name='poc/Stardust.Godot/TerrainOfflineOutcomeHud.cs';entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000;raw=z.read(name);assert SHA(raw)==by[name]['sha256'] and len(raw)==by[name]['bytes'] and (ROOT/name).read_bytes()==raw;(out/Path(name).name).write_bytes(raw)
  states,expected=fixtures()
  for name,value in [('restart-fixtures.json',states),('restart-expected.json',expected)]: (out/name).write_text(json.dumps(value,indent=2)+'\n')
  print('Independent two captions/six GUI rows/two replacements/22 failure prefixes/five teardown outcomes before native');return
 n=json.loads(a.native.read_text());e=json.loads((out/'restart-expected.json').read_text());assert n['Passed'] and n['ActualGuiButton'] and n['OutsidePassThrough'] and n['BorrowedSurvival']
 for k in e:assert n[k]==e[k],(k,n[k],e[k])
 assert (out/'restart-expected.json').stat().st_mtime<a.native.stat().st_mtime
 receipt=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,SuccessiveOwners=2,GuiRows=6,FailureCases=22,NativeTeardownCases=5,ActualGuiButton=True,OutsidePassThrough=True,BorrowedSurvival=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
