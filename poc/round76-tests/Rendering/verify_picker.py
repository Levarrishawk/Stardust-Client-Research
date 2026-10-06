"""Independent modern focus policy, synthetic Window signals and callback prefixes."""
import argparse,hashlib,json,os,runpy,stat,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
SHA=lambda b:hashlib.sha256(b).hexdigest()
def fixtures():
 actions=['Hold','Loss','Loss','Gain','Gain','FreshHold','Loss','Gain','Close','Loss','Gain'];rows=[];trace=[]
 for initial in [False,True]:
  paused=initial;enabled=True;held=runheld=0;suspended=False;prior=None;closed=False;reads=game=pauses=transitions=exits=enters=0
  for kind in actions:
   if kind in ['Hold','FreshHold']:
    held=1;runheld=4;trace+=['manual:0,-1','run:1']
   elif kind=='Close':closed=True
   else:
    focused=kind=='Gain'
    if focused:enters+=1
    else:exits+=1
    if not closed and focused!= (not suspended):
     if not focused:
      reads+=1;prior=paused;trace+=['read:'+str(int(paused))]
     game+=1;trace+=['game:'+str(int(focused))]
     if not focused and held:trace+=['manual:0,0']
     if not focused and runheld:trace+=['run:0']
     enabled=focused;held=runheld=0
     pauses+=1;paused=prior if focused else True;trace+=['pause:'+str(int(paused))]
     suspended=not focused;transitions+=1
   rows.append(dict(InitialPaused=initial,Kind=kind,Paused=paused,ManualEnabled=enabled,RunEnabled=enabled,ManualHeld=held,RunHeld=runheld,PriorPaused=prior,Suspended=suspended,Reads=reads,GameplayCalls=game,PauseCalls=pauses,Transitions=transitions,OwnConnections=0 if closed else 2,ExternalExit=exits,ExternalEnter=enters))
 failures=[]
 kinds=['source-throw','game-throw','pause-throw','gain-game-throw','gain-pause-throw','source-reentry','source-cancel','game-reentry','pause-reentry','source-dispose-reentry','game-cancel','pause-cancel','initial-cancel','lifetime-cancel','owner-exit','callback-owner-free','callback-window-free','off-thread','configure-cancel','wrong-owner','window-exit','signal-source-throw']
 for k in kinds:
  terminal=k not in ['owner-exit','window-exit','off-thread','configure-cancel','wrong-owner'];gain=k.startswith('gain');before=k in ['initial-cancel','lifetime-cancel'];source=k.startswith('source') or k=='signal-source-throw';game_stage=k.startswith('game') or k=='gain-game-throw' or k in ['callback-owner-free','callback-window-free'];pause_stage=k.startswith('pause') or k=='gain-pause-throw'
  read=int(terminal and not gain and not before);games=int(terminal and not source and not before);pauses=int(pause_stage)
  diagnostic=None
  if terminal:
   error='System.IO.IOException' if k.endswith('throw') else 'System.OperationCanceledException' if k.endswith('cancel') else 'System.InvalidOperationException'
   prior=None if before or k in ['source-throw','signal-source-throw'] else False
   diagnostic=dict(Kind='Gain' if gain else 'Loss',PriorPaused=prior,GameplayMayHaveRun=bool(games),GameplayReturned=bool(games) and k not in ['game-throw','gain-game-throw'],PauseMayHaveRun=bool(pauses),PauseReturned=bool(pauses) and k not in ['pause-throw','gain-pause-throw'],StateCommitted=False,FailureType=error,FromWindowSignal=k=='signal-source-throw')
  failures.append(dict(Kind=k,Reads=read,GameplayCalls=games,PauseCalls=pauses,Suspended=gain,Transitions=int(gain),Faulted=terminal,Diagnostic=diagnostic))
 teardown=[dict(Kind=k,OwnerAlive=k in ['detached-free','attached-free','interface-dispose'],HostAlive=False,Callbacks=0,External=1,Closed=True,OwnConnections=0) for k in ['detached-free','attached-free','parent-free','parent-queue-free','interface-dispose']]
 return dict(Actions=actions),dict(Rows=rows,Trace=trace,Failures=failures,Teardown=teardown)
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert SHA(a.exe.read_bytes()).upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400
 legacy=runpy.run_path(str(ROOT/'poc/round75-tests/Rendering/verify_picker.py'))
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round76/frozen');raw=(frozen/'baseline.json').read_bytes();assert SHA(raw)=='873d99dd4dcefb51300e68942dbf31e5bd71d24c593fbd9e2ceb6f3ba497fa97';archive=frozen/'baseline.zip';assert SHA(archive.read_bytes())=='2887d1c027dff45fcb834e72228359219f52528985e0631470b9734f7926a845'
  by={x['path']:x for x in json.loads(raw)['files']};assert len(by)==4469
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4469 and set(z.namelist())==set(by)
   names=['TerrainManualInputHost.cs','TerrainRunInputHost.cs','TerrainOfflineSessionInputHost.cs','TerrainOfflineSessionInputHostVerification.cs','TerrainOfflinePlaygroundHud.cs','TerrainCameraOrbitHost.cs','TerrainOfflinePlaygroundHudVerification.cs']
   for name in ['poc/Stardust.Godot/'+n for n in names]+['poc/round75-tests/Rendering/verify_picker.py']:
    entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000;raw=z.read(name);assert SHA(raw)==by[name]['sha256'] and len(raw)==by[name]['bytes'] and (ROOT/name).read_bytes()==raw
    if name.endswith('.cs'):(out/Path(name).name).write_bytes(raw)
  actions,expected=fixtures();oldactions,oldexpected=legacy['fixtures']();oldout=out/'legacy-input';oldout.mkdir()
  for path,value in [(out/'focus-fixtures.json',actions),(out/'focus-expected.json',expected),(out/'regression-fixtures.json',oldactions),(oldout/'input-expected.json',oldexpected)]:path.write_text(json.dumps(value,indent=2)+'\n')
  print('Independent22 focus rows/22 failure prefixes/five native teardown outcomes before native');return
 if a.native.stem=='regression':
  dest=out/'legacy-input/default-input.json';dest.write_bytes(a.native.read_bytes());sys.argv=[sys.argv[0],'--exe',str(a.exe),'--out',str(dest.parent),'--native',str(dest)];legacy['main']();receipt=json.loads((dest.parent/'default-input-receipt.json').read_text());(out/'regression-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');return
 n=json.loads(a.native.read_text());e=json.loads((out/'focus-expected.json').read_text());assert n['Passed'] and n['SyntheticWindowSignals']
 for k in e:assert n[k]==e[k],(k,n[k],e[k])
 assert (out/'focus-expected.json').stat().st_mtime<a.native.stat().st_mtime
 receipt=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,SyntheticWindowSignals=True,ComparedFocusRows=22,FailureCases=22,NativeTeardownCases=5,ActualManualRunHosts=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
