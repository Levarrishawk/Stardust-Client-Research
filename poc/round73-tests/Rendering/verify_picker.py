"""Independent authored HUD text expectations before native execution; no source-derived assets."""
import argparse,hashlib,json,os,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
TITLE='Offline preview\nModern supplied movement'
CONTROLS='WASD move  |  Shift run\nRight-drag orbit  |  Wheel zoom  |  Left-click target'
def fixtures():
 states=[dict(Mode=m,Speed=s,Updates=u,MaximumUpdates=maximum,RemainingUpdates=maximum-u,TargetActive=target,Offline=True)
  for m,s,u,maximum,target in [('Idle',0,0,120,False),('Moving',2.5,12,120,True),('Moving',4,40,120,True),
   ('Manual',4,45,120,False),('Paused',0,45,120,True),('Arrived',0,120,120,False),('Manual',10,6000,6000,False),('Idle',0,0,1,False)]]
 captions=[TITLE+'\n'+s['Mode']+'  |  Speed '+format(s['Speed'],'.2f')+'\nPreview time remaining: '+format(s['RemainingUpdates']*.01,'.1f')+'s\nTarget: '+('active' if s['TargetActive'] else 'inactive')+'\n'+CONTROLS for s in states]
 return states,captions
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:i=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(i.st_mode) and not(getattr(i,'st_file_attributes',0)&0x400) and stat.S_ISDIR(i.st_mode)
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round73/frozen');raw=(frozen/'baseline.json').read_bytes();assert hashlib.sha256(raw).hexdigest()=='138f9b7dcbbc43bf7be0a564beb5ffd0771747307f1c72ee108d46a0fef742a5';archive=frozen/'baseline.zip';assert hashlib.sha256(archive.read_bytes()).hexdigest()=='5848eb383951c9a452a3ddf013c23e30132ed8876579101273f277b1d7f302ea'
  rows=json.loads(raw)['files'];assert len(rows)==4371;by={r['path']:r for r in rows}
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4371 and set(z.namelist())==set(by)
   name='poc/Stardust.Godot/TerrainCameraOrbitHost.cs';entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000
   raw=z.read(name);assert hashlib.sha256(raw).hexdigest()==by[name]['sha256'] and len(raw)==by[name]['bytes'] and (ROOT/name).read_bytes()==raw
   (out/Path(name).name).write_bytes(raw)
  states,expected=fixtures();(out/'orbit-fixtures.json').write_text(json.dumps(states,indent=2)+'\n');(out/'hud-expected.json').write_text(json.dumps(expected,indent=2)+'\n');print('Eight independent HUD text/state expectations before native');return
 n=json.loads(a.native.read_text());expected=json.loads((out/'hud-expected.json').read_text());assert n['Passed'] and n['InputPassThrough'] and n['BorrowedSurvival'] and len(n['Failures'])==21 and len(n['Rows'])==len(expected)==8
 for i,(got,text) in enumerate(zip(n['Rows'],expected)):assert got==dict(Index=i,Caption=text,Actual=text,RefreshCount=i+1),(i,got,text)
 r=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedCaptions=8,FailureCases=21,ActualInputPassThrough=True,BorrowedSurvival=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
