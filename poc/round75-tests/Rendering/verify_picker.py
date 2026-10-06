"""Independent modern session action/prefix model generated before native Godot runs."""
import argparse,hashlib,json,os,runpy,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
SHA=lambda b:hashlib.sha256(b).hexdigest()
def fixtures():
 def key(k='P',pressed=True,echo=False):return dict(Type='Key',Key=k,Pressed=pressed,Echo=echo)
 actions=[key(),key(echo=True),key(pressed=False),key('W'),dict(Type='Mouse'),dict(Type='Gui',Enabled=True),key(),key('Escape'),dict(Type='Gui',Enabled=False),key(),key('Escape',False),key('Escape',echo=True),key('Escape'),key('Escape'),key()]
 rows=[];paused=False;pauses=finish=reads=accepted=captured=0;gui=False;finished=False
 for i,a in enumerate(actions):
  consumed=None
  if a['Type']=='Gui':gui=a['Enabled']
  else:
   consumed=False
   if gui and a['Type']=='Key':captured+=1;consumed=True
   elif a['Type']=='Key' and a['Pressed'] and not a['Echo'] and not finished:
    if a['Key']=='P':paused=not paused;pauses+=1;reads+=1;accepted+=1;consumed=True
    elif a['Key']=='Escape':finish+=1;finished=True;accepted+=1;consumed=True
  rows.append(dict(Index=i,Paused=paused,PauseCalls=pauses,FinishCalls=finish,StateReads=reads,Accepted=accepted,Handled=accepted,FinishRequested=finished,GuiCaptured=captured,Consumed=consumed))
 failures=[]
 kinds=['source-throw','pause-throw','finish-throw','source-reentry','pause-reentry','finish-reentry','source-dispose-reentry','pause-cancel','initial-cancel','owner-exit','host-exit','callback-owner-exit','callback-owner-free','off-thread','configure-cancel','wrong-owner']
 for k in kinds:
  terminal=k not in ['owner-exit','host-exit','off-thread','configure-cancel','wrong-owner']
  read=1 if k.startswith(('source','pause')) or k in ['callback-owner-exit','callback-owner-free'] else 0
  sink=1 if k.startswith('pause') or k in ['callback-owner-exit','callback-owner-free'] else 0
  stop=int(k.startswith('finish'))
  diagnostic=None
  if terminal:
   diagnostic=dict(Kind='Finish' if stop else 'Pause',RequestedPause=True if read and k!='source-throw' else None,SinkMayHaveRun=bool(sink or stop),SinkReturned=bool(sink or stop) and not k.endswith('throw'),StateCommitted=False,FailureType='System.IO.IOException' if k.endswith('throw') else 'System.OperationCanceledException' if k.endswith('cancel') else 'System.InvalidOperationException',FromGodotEvent=False)
  failures.append(dict(Kind=k,Reads=read,PauseCalls=sink,FinishCalls=stop,Faulted=terminal,FinishRequested=bool(stop),Accepted=0,Diagnostic=diagnostic))
 caption='Offline preview\nMovement controls\nMoving  |  Speed 4.00\nPreview time remaining: 9.8s\nTarget: active\nWASD move  |  Shift run\nRight-drag orbit  |  Wheel zoom  |  Left-click target\nP pause  |  Escape finish'
 teardown=[dict(Kind=k,OwnerAlive=k in ['detached-free','attached-free','interface-dispose'],HostAlive=False,OwnerChildren=0 if k in ['detached-free','attached-free','interface-dispose'] else None,Closed=True,Faulted=False,Reads=0,PauseCalls=0,FinishCalls=0,Accepted=0) for k in ['detached-free','attached-free','parent-free','parent-queue-free','interface-dispose']]
 return dict(Actions=actions),dict(Rows=rows,Failures=failures,Teardown=teardown,InteractiveCaption=caption)
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert SHA(a.exe.read_bytes()).upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400
 legacy=runpy.run_path(str(ROOT/'poc/round73-tests/Rendering/verify_picker.py'))
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round75/frozen');raw=(frozen/'baseline.json').read_bytes();assert SHA(raw)=='6956cc00d990625861fb705822d2be3e98f5f94c6ab827b6964f8a489f6dcc57';archive=frozen/'baseline.zip';assert SHA(archive.read_bytes())=='4b0ca114aaaebe129d4d433424dd9b1d76f30e4a36875715abb88c025e93290d'
  by={x['path']:x for x in json.loads(raw)['files']};assert len(by)==4436
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4436 and set(z.namelist())==set(by)
   for name in ['poc/Stardust.Godot/TerrainCameraOrbitHost.cs','poc/Stardust.Godot/TerrainOfflinePlaygroundHudVerification.cs','poc/round73-tests/Rendering/verify_picker.py']:
    entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000;raw=z.read(name);assert SHA(raw)==by[name]['sha256'] and len(raw)==by[name]['bytes'] and (ROOT/name).read_bytes()==raw
    if name.endswith('.cs'):(out/Path(name).name).write_bytes(raw)
  actions,expected=fixtures();states,captions=legacy['fixtures']()
  for name,value in [('input-fixtures.json',actions),('input-expected.json',expected),('regression-fixtures.json',states),('hud-expected.json',captions)]: (out/name).write_text(json.dumps(value,indent=2)+'\n')
  print('Independent15 actions/16 failure prefixes/eight default HUD captions before native');return
 if a.native.stem=='regression':legacy['main']();return
 n=json.loads(a.native.read_text());e=json.loads((out/'input-expected.json').read_text());assert n['Passed'] and n['BorrowedSurvival']
 for k in e:assert n[k]==e[k],(k,n[k],e[k])
 assert (out/'input-expected.json').stat().st_mtime<a.native.stat().st_mtime
 receipt=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedActions=15,FailureCases=16,NativeTeardownCases=5,GuiCaptured=2,PauseCalls=2,FinishCalls=1,BorrowedSurvival=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
