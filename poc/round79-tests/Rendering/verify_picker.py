"""Independent outcome state/caption/retirement rows before actual Godot runs."""
import argparse,hashlib,json,os,runpy,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
SHA=lambda b:hashlib.sha256(b).hexdigest()
def fixtures():
 states=[dict(Reason=r,CompletedUpdates=c,MaximumUpdates=m,RootFrames=f,QueueCount=q,PendingMessages=p,RetainedPackets=b) for r,c,m,f,q,p,b in [('BudgetCompleted',1000,1000,90,0,0,0),('FinishRequested',601,1000,53,0,0,0),('Faulted',40,1000,4,1,0,0),('Incomplete',0,1,0,0,1,0),('BudgetCompleted',6000,6000,6000,0,0,6000),('Faulted',0,1,0,8,8,6000),('Incomplete',0,6000,0,0,0,0),('FinishRequested',1,1,1,0,0,1)]]
 reasons={'BudgetCompleted':'Budget completed','FinishRequested':'Finished with Escape','Faulted':'Stopped after an error','Incomplete':'Incomplete'}
 captions=['Offline preview outcome\nReason: '+reasons[s['Reason']]+'\nUpdates: '+str(s['CompletedUpdates'])+' / '+str(s['MaximumUpdates'])+'\nOffline root frames: '+str(s['RootFrames'])+'\nBuffers: '+('empty' if s['QueueCount']==s['PendingMessages']==s['RetainedPackets']==0 else 'remaining') for s in states]
 rows=[dict(Index=i,Caption=c,Actual=c,RefreshCount=i+1,Reads=i+1) for i,c in enumerate(captions)]
 kinds=['updates-zero','updates-large','completed-negative','completed-large','frames-large','queue-negative','queue-large','pending-negative','pending-large','retained-negative','retained-large','reason-unknown','reason-null','null-state','source-throw','source-reentry','source-dispose-reentry','source-cancel','initial-cancel','owner-exit','source-owner-free','layer-free','panel-free','label-free','panel-captures','label-captures','second-configure','unconfigured-refresh','off-thread','detached-configure']
 failures=[]
 for k in kinds:
  initial=k not in ['unconfigured-refresh','detached-configure'];read=(2 if k.startswith(('updates','completed','frames','queue','pending','retained','reason','source')) or k=='null-state' else 1) if initial else 0
  failures.append(dict(Kind=k,Reads=read,RefreshCount=int(initial),RetirementCount=1,Caption=captions[0] if initial else ''))
 teardown=[dict(Kind=k,OwnerAlive=k not in ['parent-free','parent-queue-free'],Reads=1,RefreshCount=1,RetirementCount=1,Caption=captions[0]) for k in ['parent-free','parent-queue-free','layer-detached-free','layer-queue-free','interface-dispose']]
 return states,dict(Rows=rows,Failures=failures,Teardown=teardown,LimitsCaption='10.0 simulation seconds | Terrain clicks remaining: 32 | Limit keeps movement active')
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert SHA(a.exe.read_bytes()).upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400
 legacy=runpy.run_path(str(ROOT/'poc/round73-tests/Rendering/verify_picker.py'))
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round79/frozen');raw=(frozen/'baseline.json').read_bytes();assert SHA(raw)=='2374023d216de29bf1f02fc0d2ae62dfc967bf54c9e4b614878844e39ebf4ac0';archive=frozen/'baseline.zip';assert SHA(archive.read_bytes())=='85fbdfff12aba49b002217479e5066c9b9806df3c33447c89afad815525c0a9f';by={x['path']:x for x in json.loads(raw)['files']};assert len(by)==4571
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4571 and set(z.namelist())==set(by)
   for name in ['poc/Stardust.Godot/TerrainOfflinePlaygroundHud.cs','poc/Stardust.Godot/TerrainOfflineLimitsHud.cs','poc/round73-tests/Rendering/verify_picker.py']:
    entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000;raw=z.read(name);assert SHA(raw)==by[name]['sha256'] and len(raw)==by[name]['bytes'] and (ROOT/name).read_bytes()==raw
    if name.endswith('.cs'):(out/Path(name).name).write_bytes(raw)
  states,expected=fixtures();expected['LegacyCaption']=legacy['fixtures']()[1][0]
  for name,value in [('outcome-fixtures.json',states),('outcome-expected.json',expected)]: (out/name).write_text(json.dumps(value,indent=2)+'\n')
  print('Independent eight outcome captions/30 failure rows/five native teardown outcomes before native');return
 n=json.loads(a.native.read_text());e=json.loads((out/'outcome-expected.json').read_text());assert n['Passed'] and n['InputPassThrough'] and n['BorrowedSurvival']
 for k in e:assert n[k]==e[k],(k,n[k],e[k])
 assert (out/'outcome-expected.json').stat().st_mtime<a.native.stat().st_mtime
 receipt=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedCaptions=8,FailureCases=30,NativeTeardownCases=5,DefaultCaptionRegression=True,ActualInputPassThrough=True,BorrowedSurvival=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
