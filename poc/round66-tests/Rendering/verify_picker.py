"""Round66 camera/hit Fraction and target binary32 oracle, using frozen65 generators."""
import argparse,hashlib,json,os,stat,struct,importlib.util
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
  f,m=q.generate()
  # Explicit pinned accepted typed-world fixture; only compact synthetic baseline/catalog metadata consumed.
  source=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round65/runtime-cases.json')
  raw=source.read_bytes();assert hashlib.sha256(raw).hexdigest()=='2eb2eea17e552c25c37f7b82bf859ff2f3dd254c38b6cf5182021b51134e108b'
  world=json.loads(raw);f['Baseline']=world['Baseline'];f['RootSha256']=world['Profiles'][0]['RootSha256']
  (out/'picker-fixtures.json').write_text(json.dumps(f));(out/'picker-independent-model.json').write_text(json.dumps(m));return
 n=json.loads(a.native.read_text());models=json.loads((out/'picker-independent-model.json').read_text());r=q.check(n,models)
 bits=lambda x:struct.unpack('<I',struct.pack('<f',x))[0]
 clocks={'fresh-hit':0,'retarget':0,'miss':0,'oblique':0,'current-clock':10000,'final-visible':10000};assert [x['Label'] for x in n['Samples']]==list(clocks)
 rayref=module(ROOT/'poc/round63-tests/Rendering/verify_ray.py','accepted63_target');triangles=[rayref.triangles(m) for m in models]
 for row in n['Targets']:
  sample=n['Samples'][row['Sample']];expected=rayref.hit(triangles[sample['Model']],rayref.ray(sample['NativeOrigin'],sample['NativeDirection'],0,sample['MaximumT']),dict(MaximumComponent=1e6,MinimumInterpolatedNormalLength=1e-6));assert expected is not None
  assert row['XBits']==bits(float(expected['Position'][0])) and row['ZBits']==bits(float(expected['Position'][2]))
  assert row['Clock']==clocks[sample['Label']] and row['ExactReferences']
 assert n['AdapterBorrowedSurvival'] and n['PrefixRejected'] and n['CapBeforePick'] and n['AdmissionChecks']>=12
 r.update(Passed=True,TargetF32Checks=len(n['Targets']),IndependentFractionGeometry=True,IndependentProjectionAndReflection=True,NativeSeededExpectedValues=False)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
