"""Independent limits state/caption/retirement rows before actual Godot runs."""
import argparse,hashlib,json,os,runpy,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
SHA=lambda b:hashlib.sha256(b).hexdigest()
def fixtures():
 states=[dict(MaximumUpdates=u,MaximumClickAttempts=m,RemainingClickAttempts=c,GameplayEnabled=g,Paused=p) for u,m,c,g,p in [(1000,32,32,True,False),(1000,32,1,True,False),(1000,32,0,True,False),(1000,32,0,False,True),(6000,128,128,True,False),(6000,128,0,False,False),(1,1,0,True,True),(3,1,1,False,False)]]
 captions=[format(s['MaximumUpdates']*.01,'.1f')+' simulation seconds | Terrain clicks remaining: '+str(s['RemainingClickAttempts'])+' | Limit keeps movement active' for s in states]
 rows=[dict(Index=i,Caption=c,Actual=c,RefreshCount=i+1,Reads=i+1) for i,c in enumerate(captions)]
 kinds=['updates-zero','updates-large','clicks-zero','clicks-large','remaining-negative','remaining-large','null-state','source-throw','source-reentry','source-dispose-reentry','source-cancel','initial-cancel','owner-exit','source-owner-free','layer-free','label-free','label-captures','second-configure','unconfigured-refresh','off-thread','detached-configure']
 failures=[]
 for k in kinds:
  initial=k not in ['unconfigured-refresh','detached-configure'];read=(2 if k.startswith(('updates','clicks','remaining','source')) or k=='null-state' else 1) if initial else 0
  failures.append(dict(Kind=k,Reads=read,RefreshCount=int(initial),RetirementCount=1,Caption=captions[0] if initial else ''))
 teardown=[dict(Kind=k,OwnerAlive=k not in ['parent-free','parent-queue-free'],Reads=1,RefreshCount=1,RetirementCount=1,Caption=captions[0]) for k in ['parent-free','parent-queue-free','layer-detached-free','layer-queue-free','interface-dispose']]
 return states,dict(Rows=rows,Failures=failures,Teardown=teardown)
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert SHA(a.exe.read_bytes()).upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400
 legacy=runpy.run_path(str(ROOT/'poc/round73-tests/Rendering/verify_picker.py'))
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round78/frozen');raw=(frozen/'baseline.json').read_bytes();assert SHA(raw)=='fae3830a886e9cb4bfd6639c6337052539a35e77c84dd9c5dadf62212f32650f';archive=frozen/'baseline.zip';assert SHA(archive.read_bytes())=='4a11a66f312fd654aae850dc94c3214b58130068ce7896108e31dc78a5d275d4';by={x['path']:x for x in json.loads(raw)['files']};assert len(by)==4537
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4537 and set(z.namelist())==set(by)
   for name in ['poc/Stardust.Godot/TerrainOfflinePlaygroundHud.cs','poc/round73-tests/Rendering/verify_picker.py']:
    entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000;raw=z.read(name);assert SHA(raw)==by[name]['sha256'] and len(raw)==by[name]['bytes'] and (ROOT/name).read_bytes()==raw
    if name.endswith('.cs'):(out/Path(name).name).write_bytes(raw)
  states,expected=fixtures();expected['LegacyCaption']=legacy['fixtures']()[1][0]
  for name,value in [('limits-fixtures.json',states),('limits-expected.json',expected)]: (out/name).write_text(json.dumps(value,indent=2)+'\n')
  print('Independent eight limits captions/21 failure rows/five native teardown outcomes before native');return
 n=json.loads(a.native.read_text());e=json.loads((out/'limits-expected.json').read_text());assert n['Passed'] and n['InputPassThrough'] and n['BorrowedSurvival']
 for k in e:assert n[k]==e[k],(k,n[k],e[k])
 assert (out/'limits-expected.json').stat().st_mtime<a.native.stat().st_mtime
 receipt=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedCaptions=8,FailureCases=21,NativeTeardownCases=5,DefaultCaptionRegression=True,ActualInputPassThrough=True,BorrowedSurvival=True)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
