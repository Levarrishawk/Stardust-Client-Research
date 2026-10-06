"""Round67 viewport host camera/hit Fraction and target binary32 oracle, using frozen65 generators."""
import argparse,hashlib,json,os,stat,struct,importlib.util,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def module(path,name):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
 q=module(ROOT/'poc/round65-tests/Rendering/verify_picker.py','accepted65');assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==q.EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:i=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(i.st_mode) and not(getattr(i,'st_file_attributes',0)&0x400) and stat.S_ISDIR(i.st_mode)
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round67/frozen')
  raw=(frozen/'baseline.json').read_bytes();assert hashlib.sha256(raw).hexdigest()=='d209e9122ef2609ba8b0b9834d206ba86a5a2368fc6ef5adcc74fdafdd96b53a'
  archive=frozen/'baseline.zip';assert hashlib.sha256(archive.read_bytes()).hexdigest()=='46210994c72f854bff8fda41bfbdb83521c04b30cf94a9f24d5878a9bbf47745'
  rows=json.loads(raw)['files'];assert len(rows)==4157;names={r['path'] for r in rows};identity=[]
  borrowers={'poc/Stardust.Godot/'+name for name in ('TerrainCameraPicker.cs','TerrainPointTargetInputAdapter.cs','TerrainPatchVisual.cs')}
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4157 and set(z.namelist())==names
   for row in rows:
    name=row['path'];assert ':' not in name and '\\' not in name and not name.startswith('/') and all(v not in ('','.','..') for v in name.split('/'))
    if not(name.startswith('poc/Stardust.Core/') or name in borrowers):continue
    entry=z.getinfo(name);assert not entry.is_dir() and ((entry.external_attr>>16)&0o170000)!=0o120000
    data=z.read(name);assert len(data)==row['bytes'] and hashlib.sha256(data).hexdigest()==row['sha256']
    dest=out/'frozen-core'/name.removeprefix('poc/Stardust.Core/') if name.startswith('poc/Stardust.Core/') else out/'frozen-godot'/Path(name).name
    dest.parent.mkdir(parents=True,exist_ok=True)
    with dest.open('xb') as stream:stream.write(data)
    identity.append(dict(path=name,sha256=row['sha256']))
  (out/'frozen-source-identity.json').write_text(json.dumps(identity,indent=2)+'\n')
  f,m=q.generate()
  # Explicit pinned accepted typed-world fixture; only compact synthetic baseline/catalog metadata consumed.
  source=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round65/runtime-cases.json')
  raw=source.read_bytes();assert hashlib.sha256(raw).hexdigest()=='2eb2eea17e552c25c37f7b82bf859ff2f3dd254c38b6cf5182021b51134e108b'
  world=json.loads(raw);f['Baseline']=world['Baseline'];f['RootSha256']=world['Profiles'][0]['RootSha256']
  (out/'picker-fixtures.json').write_text(json.dumps(f));(out/'picker-independent-model.json').write_text(json.dumps(m));return
 n=json.loads(a.native.read_text());models=json.loads((out/'picker-independent-model.json').read_text());r=q.check(n,models)
 bits=lambda x:struct.unpack('<I',struct.pack('<f',x))[0]
 clocks={'fresh-hit':0,'retarget':0,'miss':0,'current-clock':10000,'final-visible':10000};assert [x['Label'] for x in n['Samples']]==list(clocks)
 rayref=module(ROOT/'poc/round63-tests/Rendering/verify_ray.py','accepted63_target');triangles=[rayref.triangles(m) for m in models]
 for row in n['Targets']:
  sample=n['Samples'][row['Sample']];expected=rayref.hit(triangles[sample['Model']],rayref.ray(sample['NativeOrigin'],sample['NativeDirection'],0,sample['MaximumT']),dict(MaximumComponent=1e6,MinimumInterpolatedNormalLength=1e-6));assert expected is not None
  assert row['XBits']==bits(float(expected['Position'][0])) and row['ZBits']==bits(float(expected['Position'][2]))
  assert row['Clock']==clocks[sample['Label']] and row['ExactReferences']
 assert n['HostBorrowedSurvival'] and n['ClockReentryBeforeChild'] and n['EventFaultDisabled'] and n['ExitTreeClosed'] and n['AdmissionChecks']>=10
 assert n['ViewportEvents']>=12 and n['HandledEvents']==5 and n['NativePrefixPreserved']
 r.update(Passed=True,TargetF32Checks=len(n['Targets']),IndependentFractionGeometry=True,IndependentProjectionAndReflection=True,NativeSeededExpectedValues=False)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
