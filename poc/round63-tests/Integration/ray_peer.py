"""Independent rational ray/triangle reference over sealed stored terrain geometry."""
import importlib.util,json,math,sys
from fractions import Fraction as F
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('sealed63_surface',ROOT/'poc/round62-tests/Integration/surface_query_peer.py')
surface=importlib.util.module_from_spec(spec);spec.loader.exec_module(surface)

def subtract(a,b):return [x-y for x,y in zip(a,b)]
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def dot(a,b):return sum(x*y for x,y in zip(a,b))

def rays():
 result=[dict(Origin=[x,20,z],Direction=[0,-1,0],Minimum=0,Maximum=40) for z in range(-8,9,2) for x in range(-8,9,2)]
 result += [dict(Origin=[x,-20,z],Direction=[0,2,0],Minimum=0,Maximum=20) for z in (-6,0,6) for x in (-6,0,6)]
 result += [dict(Origin=[x,20,z],Direction=[.125,-2,.25],Minimum=0,Maximum=20) for x in (-6,-2,2,6) for z in (-6,2)]
 result += [dict(Origin=[x,20,z],Direction=[0,-1,0],Minimum=0,Maximum=40) for x,z in ((-10,0),(10,0),(0,-10),(0,10))]
 result += [dict(Origin=[0,20,0],Direction=[1,0,0],Minimum=0,Maximum=40),dict(Origin=[0,20,0],Direction=[0,-1,0],Minimum=0,Maximum=1)]
 return result

def expected(geometries,ray):
 origin=list(map(F,ray['Origin']));direction=list(map(F,ray['Direction']));lo=F(ray['Minimum']);hi=F(ray['Maximum']);best=None
 for g in geometries:
  positions=[[F(surface.f(v)) for v in row] for row in g['Positions']]
  normals=[[surface.f(v) for v in row] for row in g['Normals']]
  for triangle in range(len(g['Indices'])//3):
   ia,ib,ic=g['Indices'][3*triangle:3*triangle+3];a,b,c=[positions[i] for i in (ia,ib,ic)]
   ab=subtract(b,a);ac=subtract(c,a);plane=cross(ab,ac);den=dot(plane,direction)
   if den==0:continue
   t=dot(plane,subtract(a,origin))/den
   if t<lo or t>hi or (best is not None and t>=best['_Parameter']):continue
   position=[o+t*d for o,d in zip(origin,direction)];ap=subtract(position,a)
   # Independent Gram-system barycentric solve, rather than the native ray determinant path.
   aa=dot(ab,ab);bb=dot(ac,ac);cc=dot(ab,ac);ad=dot(ap,ab);bd=dot(ap,ac);det=aa*bb-cc*cc
   if det==0:continue
   wb=(bb*ad-cc*bd)/det;wc=(aa*bd-cc*ad)/det;wa=1-wb-wc
   if min(wa,wb,wc)<0:continue
   weights=[float(wa),float(wb),float(wc)];normal=[sum(weights[k]*normals[idx][axis] for k,idx in enumerate((ia,ib,ic))) for axis in range(3)]
   length=math.sqrt(sum(v*v for v in normal));assert math.isfinite(length) and length>0
   best=dict(ChunkIndex=g['ChunkIndex'],PatchIndex=g['PatchIndex'],TriangleIndex=triangle,VertexA=ia,VertexB=ib,VertexC=ic,Parameter=float(t),Position=list(map(float,position)),Weights=weights,Normal=[v/length for v in normal],_Parameter=t)
 if best is not None:best.pop('_Parameter')
 return best

def compare(geometries,rows):
 hits=misses=0
 for row in rows:
  ray={k:row[k] for k in ('Origin','Direction','Minimum','Maximum')};want=expected(geometries,ray);got=row['Hit']
  assert (want is None)==(got is None),(ray,want,got)
  if want is None:misses+=1;continue
  hits+=1
  for key in ('ChunkIndex','PatchIndex','TriangleIndex','VertexA','VertexB','VertexC'):assert got[key]==want[key],(key,ray,want,got)
  assert math.isclose(got['Parameter'],want['Parameter'],rel_tol=2e-11,abs_tol=1e-9),(ray,want,got)
  for key in ('Position','Weights','Normal'):
   assert len(got[key])==3
   assert all(math.isclose(a,b,rel_tol=2e-11,abs_tol=1e-9) for a,b in zip(got[key],want[key])),(key,ray,want,got)
  assert got.get('ActualReferences',True) and not got.get('OriginalCollisionParity',False)
 return dict(Passed=True,Rays=len(rows),Hits=hits,Misses=misses,OriginalCollisionParity=False)

if __name__=='__main__':
 fixture,observations,out=sys.argv[1:4];d=json.loads(Path(fixture).read_text());g=json.loads(Path(observations).read_text());result=compare(surface.fixture_geometry(d),g)
 p=Path(out);assert not p.exists() and p.resolve().is_relative_to((ROOT/'local-private').resolve());p.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
