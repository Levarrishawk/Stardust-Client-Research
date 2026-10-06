"""Independent attempt rows and binary32 targets, frozen Fraction geometry/projection oracle."""
import argparse,hashlib,json,os,runpy,stat,struct,sys,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
SHA=lambda b:hashlib.sha256(b).hexdigest()
def fixtures():
 bits=lambda x:struct.unpack('<I',struct.pack('<f',x))[0];f32=lambda x:struct.unpack('<f',struct.pack('<f',x))[0]
 rows=[];targets=0
 for i in range(32):
  hit=i!=7;targets+=int(hit);p=4 if i%2==0 else 6
  rows.append(dict(Index=i,Attempts=i+1,Remaining=31-i,Exhausted=i==31,PickerAttempts=i+1,HostAccepted=i+1,HostHandled=i+1,ClockCalls=i+1,Hit=hit,TargetRevision=targets,SuccessfulTargets=targets,TargetXBits=bits(p) if hit else None,TargetZBits=bits(p) if hit else None))
 return dict(Rows=rows,Manual=dict(Updates=2,Clock=20000,Mode='Idle',XBits=bits(f32(4+f32(f32(2)*f32(.01)))),ZBits=bits(4),SpeedBits=bits(0)))
def stage(out):
 frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round77/frozen');raw=(frozen/'baseline.json').read_bytes();assert SHA(raw)=='00f4d8e82b7d84df3f00b539d649d48a9e39aaca0edbe360c440c2386a21e5fa';archive=frozen/'baseline.zip';assert SHA(archive.read_bytes())=='0e5378488a7244bfd3a2e0da0de0710d93ad1eca20a6f0601872aa5870963960';by={x['path']:x for x in json.loads(raw)['files']};assert len(by)==4504;identities=[]
 borrowers={'poc/Stardust.Godot/'+n for n in ['TerrainCameraPicker.cs','TerrainPatchVisual.cs','TerrainPointTargetInputVerification.cs','TerrainPointTargetInputHostVerification.cs']};oracles={'poc/round'+str(n)+'-tests/Rendering/'+file for n,file in [(67,'verify_picker.py'),(66,'verify_picker.py'),(65,'verify_picker.py'),(63,'verify_ray.py')]}
 with zipfile.ZipFile(archive) as z:
  assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4504 and set(z.namelist())==set(by)
  for name,row in by.items():
   assert ':' not in name and '\\' not in name and all(v not in ('','.','..') for v in name.split('/'))
   if not(name.startswith('poc/Stardust.Core/') or name in borrowers or name in oracles):continue
   e=z.getinfo(name);assert not e.is_dir() and ((e.external_attr>>16)&0o170000)!=0o120000;raw=z.read(name);assert SHA(raw)==row['sha256'] and len(raw)==row['bytes'] and (ROOT/name).read_bytes()==raw
   if name in oracles:continue
   dest=out/'frozen-core'/Path(name).relative_to('poc/Stardust.Core') if name.startswith('poc/Stardust.Core/') else out/Path(name).name;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw);identities.append(row)
 (out/'frozen-source-identity.json').write_text(json.dumps(identities,indent=2)+'\n')
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert SHA(a.exe.read_bytes()).upper()==EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:s=ancestor.lstat()
  except FileNotFoundError:continue
  assert stat.S_ISDIR(s.st_mode) and not stat.S_ISLNK(s.st_mode) and not getattr(s,'st_file_attributes',0)&0x400
 legacy=runpy.run_path(str(ROOT/'poc/round66-tests/Rendering/verify_picker.py'))
 if not a.native:
  stage(out);sys.argv=[sys.argv[0],'--exe',str(a.exe),'--out',str(out)];legacy['main']();(out/'budget-expected.json').write_text(json.dumps(fixtures(),indent=2)+'\n');print('Independent32 attempt/target rows, exact manual F32 motion and frozen Fraction geometry before native');return
 if a.native.stem=='regression':legacy['main']();return
 if a.native.stem=='host-regression':runpy.run_path(str(ROOT/'poc/round67-tests/Rendering/verify_picker.py'))['main']();return
 n=json.loads(a.native.read_text());e=json.loads((out/'budget-expected.json').read_text());assert n['Passed'] and n['Declined33rd'] and n['DirectDeclined'] and n['ExactHistoryPreserved'] and n['BorrowedSurvival']
 for k in e:assert n[k]==e[k],(k,n[k],e[k])
 q=legacy['module'](ROOT/'poc/round65-tests/Rendering/verify_picker.py','frozen65');r=q.check(n,json.loads((out/'picker-independent-model.json').read_text()))
 assert (out/'budget-expected.json').stat().st_mtime<a.native.stat().st_mtime
 r.update(Passed=True,Checks=n['Checks'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedClicks=32,SuccessfulTargets=31,DeclinedViewportClick=33,NoClockReadOnDecline=True,ExactHistoryPreserved=True,ManualMoveStopUpdates=2)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
