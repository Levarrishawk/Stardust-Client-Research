"""Synthetic independent geometry + Fraction camera-ray oracle; hit values never seed expectations."""
import argparse,hashlib,importlib.util,json,math,os,stat,struct
from decimal import Decimal
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def module(path,name):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def generate():
 q=module(ROOT/'poc/round62-tests/Rendering/verify_surface.py','frozen62_picker');a,b,c,d=q.load();pool,_,_,_=q.generate(a,b,c,d);base=q.copy(pool[0]);base['Profiles']=[24]*4;base['Heights']=[0]*len(base['Heights']);base['Mask']=0;base['Request']['First']=0;base['Request']['Second']=0
 flat=[base,q.copy(base)];flat[1]['Request']['First']=1;slope=[q.copy(base)]
 # Independent coordinate model assigns authored sloped input heights to actual stored grid coordinates.
 grid=d['coord'](dict(Name='slope',Request=slope[0]['Request'],Profiles=[24]*4,Limits=d['producer']['DEFAULT']));w=grid['Parameters']['Count'];xs=grid['Position']['First'];zs=grid['Position']['Second']
 slope[0]['Heights']=[q.b32(float(q.f32(xs[x])/4+q.f32(zs[z])/8)) for z in range(w) for x in range(w)]
 models=[]
 for chunks in (flat,slope):
  values=[]
  for chunk in chunks:
   v=a.enrich(chunk,b.expected(chunk,c,d),b,c,d)
   for p in v['Patches']:p['NormalBits']=[[q.b32(float(Decimal(x))) for x in row] for row in p['Normals']]
   values.append(v)
  models.append(dict(Chunks=[dict(Patches=[{k:p[k] for k in ('Positions','NormalBits','Indices','Pattern')} for p in v['Patches']]) for v in values]))
 dense=[]
 for z in range(4):
  for x in range(4):
   v=q.copy(base);v['Request'].update(Tiles=4,ChunkWidthBits=q.b32(16),First=x,Second=z);grid=d['coord'](dict(Name='dense',Request=v['Request'],Profiles=[24]*4,Limits=d['producer']['DEFAULT']));v['Heights']=[0]*(grid['Parameters']['Count']**2);dense.append(v)
 return dict(Flat=flat,Slope=slope,Dense=dense),models

def check(native,models):
 q=module(ROOT/'poc/round63-tests/Rendering/verify_ray.py','frozen63_picker');tris=[q.triangles(s) for s in models];hits=misses=0;projectionChecks=0
 for s in native['Samples']:
  assert 0<=s['Pixel'][0]<s['Viewport'][0] and 0<=s['Pixel'][1]<s['Viewport'][1]
  assert s['NativeOrigin']==[s['EngineOrigin'][0],s['EngineOrigin'][1],-s['EngineOrigin'][2]] and s['NativeDirection']==[s['EngineDirection'][0],s['EngineDirection'][1],-s['EngineDirection'][2]]
  width,height=s['Viewport'];x=2*s['Pixel'][0]/width-1;y=1-2*s['Pixel'][1]/height;basis=s['Basis'];pos=s['CameraPosition'];near=s['Near']
  if s['Projection']=='Perspective':
   origin=pos;scale=math.tan(math.radians(s['Fov'])/2);local=(x*width/height*scale,y*scale,-1);length=math.hypot(*local);local=tuple(v/length for v in local)
   direction=[sum(basis[j][i]*local[j] for j in range(3)) for i in range(3)]
  else:
   local=(x*s['Size']*width/height/2,y*s['Size']/2,-near);origin=[pos[i]+sum(basis[j][i]*local[j] for j in range(3)) for i in range(3)];direction=[-v for v in basis[2]]
  for got,want in zip(s['EngineOrigin'],origin):assert abs(got-want)<5e-5,(s['Label'],'projection-origin',got,want)
  for got,want in zip(s['EngineDirection'],direction):assert abs(got-want)<5e-6,(s['Label'],'projection-direction',got,want)
  projectionChecks+=1
  r=q.ray(s['NativeOrigin'],s['NativeDirection'],0,s['MaximumT']);expected=q.hit(tris[s['Model']],r,dict(MaximumComponent=1e6,MinimumInterpolatedNormalLength=1e-6));got=s['Hit']
  if expected is None:assert got is None,(s['Label'],'miss',got);misses+=1;continue
  assert got is not None,(s['Label'],'missing hit',expected);hits+=1
  for key in ('Chunk','Patch','TriangleIndex','VertexA','VertexB','VertexC'):assert got[key]==expected[key],(s['Label'],key,got[key],expected[key])
  assert math.isclose(got['T'],expected['T'],abs_tol=1e-8,rel_tol=2e-10)
  for gotv,wantv in zip(got['Position'],expected['Position']):assert math.isclose(gotv,wantv,abs_tol=1e-8,rel_tol=2e-10)
  for gotv,key in zip(got['Weights'],('WeightA','WeightB','WeightC')):assert abs(gotv-expected[key])<1e-8
  for gotv,wantv in zip(got['Normal'],expected['Normal']):assert abs(gotv-wantv)<3e-6
  assert s['Marker']['Visible'];ep=[expected['Position'][0],expected['Position'][1],-expected['Position'][2]]
  for gotv,wantv in zip(s['Marker']['Position'],ep):assert abs(gotv-wantv)<3e-6
 assert native['OwnedResourcesAfter']==0 and native['BorrowedSurvival'] and native['AttemptsMaximum']==256 and not native['OriginalPickingParity']
 return dict(Hits=hits,Misses=misses,ProjectionChecks=projectionChecks,NativeChecks=native['Checks'],Gpu=native['Gpu'])
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args();assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==EXE
 a.out=Path(os.path.abspath(a.out));assert a.out.is_relative_to(ROOT/'local-private')
 for ancestor in (a.out,*a.out.parents):
  try:i=ancestor.lstat()
  except FileNotFoundError:continue
  assert not stat.S_ISLNK(i.st_mode) and not(getattr(i,'st_file_attributes',0)&0x400) and stat.S_ISDIR(i.st_mode)
 if not a.native:
  fixtures,models=generate();(a.out/'picker-fixtures.json').write_text(json.dumps(fixtures),encoding='utf-8');(a.out/'picker-independent-model.json').write_text(json.dumps(models),encoding='utf-8');print('PASS independent synthetic camera geometry');return
 n=json.loads(a.native.read_text());receipt=dict(Passed=True,IndependentFractionGeometry=True,IndependentProjectionAndReflection=True,NativeSeededExpectedValues=False,OriginalPickingParity=False,**check(n,json.loads((a.out/'picker-independent-model.json').read_text())))
 a.native.with_name(a.native.stem+'-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt))
if __name__=='__main__':main()
