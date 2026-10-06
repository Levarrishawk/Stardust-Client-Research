"""Frozen80 real-GUI fixture plus isolated fullhost source compilation, no original execution."""
import argparse,hashlib,json,os,runpy,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
SHA=lambda b:hashlib.sha256(b).hexdigest()
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert SHA(a.exe.read_bytes()).upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round82/frozen');raw=(frozen/'baseline.json').read_bytes();assert SHA(raw)=='e142a5e7e1c2846a9f7727a1fe05c82a0aab7735b47aff0167ccc7604ac01ddf';archive=frozen/'baseline.zip';assert SHA(archive.read_bytes())=='a4a13af44ae9b35c42eb87f7f1a361f11c7e810fc63ce2f65147e7cc84e29990';by={x['path']:x for x in json.loads(raw)['files']};assert len(by)==4687
  changed={'poc/Stardust.Godot/PersistentOfflinePlayground.cs','poc/Stardust.Godot/TerrainOfflineRestartHud.cs'};added={'poc/Stardust.Godot/PersistentOfflinePlaygroundInspection.cs'}
  source_receipts=[]
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4687 and set(z.namelist())==set(by)
   for name in ['poc/Stardust.Godot/TerrainOfflineRestartHud.cs','poc/Stardust.Godot/TerrainOfflineRestartHudVerification.cs','poc/Stardust.Godot/TerrainOfflineOutcomeHud.cs','poc/round80-tests/Rendering/verify_picker.py']:
    raw=z.read(name);assert SHA(raw)==by[name]['sha256'] and len(raw)==by[name]['bytes'] and (name in changed or (ROOT/name).read_bytes()==raw)
    if name.endswith('.cs'):(out/Path(name).name).write_bytes((ROOT/name).read_bytes() if name in changed else raw)
   model=runpy.run_path(str(ROOT/'poc/round80-tests/Rendering/verify_picker.py'));states,expected=model['fixtures']()
   for name in set(by)|added:
    if not name.startswith(('poc/Stardust.Core/','poc/Stardust.Godot/')) or Path(name).suffix not in ['.cs','.csproj']:continue
    raw=(ROOT/name).read_bytes()
    if name not in changed|added:assert SHA(raw)==by[name]['sha256'] and len(raw)==by[name]['bytes']
    path=out/'fullhost'/Path(name).relative_to('poc');path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw);source_receipts.append(dict(Path=name,Sha256=SHA(raw)))
  shell_expected=[dict(Kind=k,Started=int(k in ['published-reentry','invalid-startup','cleanup-throw']),Retired=int(k in ['published-reentry','invalid-startup']),Events=int(k in ['published-reentry','invalid-startup','cleanup-throw']),Factories=1,Terminal=True,CandidateAlive=k=='cleanup-throw',BorrowAlive=k=='attached-factory',ValidOutcome=False if k in ['published-reentry','invalid-startup','cleanup-throw'] else None,Restartable=False if k in ['published-reentry','invalid-startup','cleanup-throw'] else None,CleanupSucceeded=k!='cleanup-throw' if k in ['published-reentry','invalid-startup','cleanup-throw'] else None) for k in ['factory-throw','attached-factory','factory-reentry','published-reentry','invalid-startup','cleanup-throw']]
  (out/'shell-expected.json').write_text(json.dumps(shell_expected,indent=2)+'\n')
  (out/'shell-profile.json').write_text(json.dumps(dict(Version=13,Policy='SuppliedConfiguredOfflinePlayground'))+'\n')
  (out/'fullhost-sources.json').write_text(json.dumps(sorted(source_receipts,key=lambda r:r['Path']),indent=2)+'\n')
  for name,value in [('restart-fixtures.json',states),('restart-expected.json',expected)]: (out/name).write_text(json.dumps(value,indent=2)+'\n')
  runpy.run_path(str(Path(__file__).with_name('inspection_oracle.py')))['generate'](out)
  print('Independent frozen80 six actualGUI rows/two owners/22 failures/five teardown before native; fullhost sources staged privately');return
 n=json.loads(a.native.read_text());e=json.loads((out/'restart-expected.json').read_text());assert n['Passed'] and n['ActualGuiButton'] and n['OutsidePassThrough'] and n['BorrowedSurvival']
 for k in e:assert n[k]==e[k],k
 assert (out/'restart-expected.json').stat().st_mtime<a.native.stat().st_mtime
 receipt=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,SuccessiveOwners=2,GuiRows=6,FailureCases=22,NativeTeardownCases=5,ActualGuiButton=True,OutsidePassThrough=True,BorrowedSurvival=True,Scope='Frozen80 HUD regression; new fullhost compiled separately; full lifecycle join belongs to lead')
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
