"""Independent pivot pose/ray model before native, plus accepted original orbit regression."""
import argparse,hashlib,importlib.util,json,math,os,stat,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def module(path):
 s=importlib.util.spec_from_file_location('orbit70',path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
 q=module(ROOT/'poc/round70-tests/Rendering/verify_picker.py');assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==q.EXE
 out=Path(os.path.abspath(a.out));assert out.is_relative_to(ROOT/'local-private')
 for ancestor in (out,*out.parents):
  try:i=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(i.st_mode) and not(getattr(i,'st_file_attributes',0)&0x400) and stat.S_ISDIR(i.st_mode)
 if not a.native:
  frozen=Path('C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round72/frozen');raw=(frozen/'baseline.json').read_bytes();assert hashlib.sha256(raw).hexdigest()=='a42f6a8d17554a82c5eb07ad09693484febd48eadf30698458115f8d9cd96426';archive=frozen/'baseline.zip';assert hashlib.sha256(archive.read_bytes()).hexdigest()=='e4f5fd7d111d4006ee09ace0db755353274dbf00bfa5ee848df66322672a79f5'
  rows=json.loads(raw)['files'];assert len(rows)==4335;by={r['path']:r for r in rows}
  with zipfile.ZipFile(archive) as z:
   assert len(z.namelist())==len(set(z.namelist()))==len({n.casefold() for n in z.namelist()})==4335 and set(z.namelist())==set(by)
   for name in ('poc/Stardust.Godot/TerrainCameraOrbitHostVerification.cs','poc/round70-tests/Rendering/verify_picker.py'):
    raw=z.read(name);assert hashlib.sha256(raw).hexdigest()==by[name]['sha256'] and len(raw)==by[name]['bytes']
    assert (ROOT/name).read_bytes()==raw
    if name.endswith('.cs'):(out/Path(name).name).write_bytes(raw)
  pivots=[[1,2,3],[-10,4,8],[0,0,0],[500,-100,350]]
  groups=[dict(Yaw=y,Pitch=p,Distance=d,Pivots=pivots) for y,p,d in [(.2,.4,8),(-1.2,.8,12),(2.8,.2,3)]];expected=[]
  for g in groups:
   y=math.remainder(g['Yaw']-10*.005,2*math.pi);pitch=g['Pitch']+3*.005
   for i,pivot in enumerate(pivots):expected.append(dict(Pivot=pivot,Yaw=y,Pitch=pitch,Distance=g['Distance'],Attempts=i+3,Revision=i+3,Accepted=1,Handled=0,**q.pose(y,pitch,g['Distance'],pivot)))
  (out/'orbit-fixtures.json').write_text(json.dumps(dict(Groups=groups),indent=2)+'\n');(out/'pivot-expected.json').write_text(json.dumps(expected,indent=2)+'\n')
  f,e=q.fixtures();(out/'regression-fixtures.json').write_text(json.dumps(f,indent=2)+'\n');(out/'orbit-expected.json').write_text(json.dumps(e,indent=2)+'\n');print('Independent pivot and original orbit expectations before native');return
 if a.native.stem=='regression':
  import sys
  sys.argv=[str(q.__file__),'--exe',str(a.exe),'--out',str(out),'--native',str(a.native)];q.main();return
 n=json.loads(a.native.read_text());e=json.loads((out/'pivot-expected.json').read_text());assert n['Passed'] and n['BorrowedSurvival'] and n['NoRuntimeDependency'] and n['FailureCases']==7 and len(n['Rows'])==len(e)==12
 for got,want in zip(n['Rows'],e):
  for key in ('Pivot','Yaw','Pitch','Distance','Attempts','Revision','Accepted','Handled'):assert got[key]==want[key],(key,got[key],want[key])
  assert max(abs(v-w) for v,w in zip(got['Position'],want['Position']))<2e-5
  assert max(abs(v-w) for gv,wv in zip(got['Basis'],want['Basis']) for v,w in zip(gv,wv))<3e-5
  viewport=got['Viewport'];pixel=got['Pixel'];x=2*pixel[0]/viewport[0]-1;y=1-2*pixel[1]/viewport[1];t=math.tan(math.radians(50)/2);local=q.unit([x*viewport[0]/viewport[1]*t,y*t,-1]);direction=[sum(want['Basis'][j][i]*local[j] for j in range(3)) for i in range(3)]
  assert max(abs(v-w) for v,w in zip(got['RayOrigin'],want['Position']))<2e-5
  assert max(abs(v-w) for v,w in zip(got['RayDirection'],direction))<3e-5
 r=dict(Passed=True,Checks=n['Checks'],Gpu=n['Gpu'],IndependentBeforeNative=True,NativeSeededExpectedValues=False,ComparedPoses=12,ComparedRays=12,FailureCases=7)
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
if __name__=='__main__':main()
