"""Independent binary64 triangle oracle for retained-set host/native query observations."""
import math,struct,json,sys
from pathlib import Path

def f(bits):return struct.unpack('<f',struct.pack('<I',bits))[0]
def cross(x,z,u,v):return x*v-z*u

def expected(geometries,x,z):
 for g in geometries:
  p=[[f(c) for c in row] for row in g['Positions']];n=[[f(c) for c in row] for row in g['Normals']];ix=g['Indices']
  if x<min(v[0] for v in p) or x>max(v[0] for v in p) or z<min(v[2] for v in p) or z>max(v[2] for v in p):continue
  for t in range(len(ix)//3):
   ia,ib,ic=ix[3*t:3*t+3];a,b,c=p[ia],p[ib],p[ic];bx,bz=b[0]-a[0],b[2]-a[2];cx,cz=c[0]-a[0],c[2]-a[2];area=cross(bx,bz,cx,cz);dx,dz=x-a[0],z-a[2]
   wb=cross(dx,dz,cx,cz)/area;wc=cross(bx,bz,dx,dz)/area;wa=1-wb-wc
   if wa<0 or wb<0 or wc<0:continue
   height=a[1]+wb*(b[1]-a[1])+wc*(c[1]-a[1]);na,nb,nc=n[ia],n[ib],n[ic];v=[na[i]+wb*(nb[i]-na[i])+wc*(nc[i]-na[i]) for i in range(3)];length=math.sqrt(sum(k*k for k in v))
   return dict(ChunkIndex=g['ChunkIndex'],PatchIndex=g['PatchIndex'],PatternIndex=g['PatternIndex'],TriangleIndex=t,VertexA=ia,VertexB=ib,VertexC=ic,WeightA=wa,WeightB=wb,WeightC=wc,Height=height,Normal=dict(zip(('X','Y','Z'),(k/length for k in v))))
 return None

def compare(geometries,rows):
 hits=misses=0
 for row in rows:
  e=expected(geometries,row['X'],row['Z']);got=row['Hit'];assert (e is None)==(got is None),row
  if e is None:misses+=1;continue
  hits+=1
  for k in ('ChunkIndex','PatchIndex'):assert got[k]==e[k],(k,got,e)
  h=got['Surface']
  for k in ('PatternIndex','TriangleIndex','VertexA','VertexB','VertexC'):assert h[k]==e[k],(k,h,e)
  for k in ('WeightA','WeightB','WeightC','Height'):assert math.isclose(h[k],e[k],rel_tol=1e-12,abs_tol=1e-9),(k,h,e)
  for k in ('X','Y','Z'):assert math.isclose(h['Normal'][k],e['Normal'][k],rel_tol=1e-12,abs_tol=1e-9),(k,h,e)
 return dict(Passed=True,Queries=len(rows),Hits=hits,Misses=misses,OriginalCollisionParity=False)

def fixture_geometry(d):
 return [dict(ChunkIndex=c,PatchIndex=i,PatternIndex=p['Pattern'],Positions=p['Positions'],Normals=p['NormalBits'],Indices=p['Indices']) for c,e in enumerate(d['Expected']) for i,p in enumerate(e['Patches'])]

def fixed_locations():
 return [(x,z) for z in range(-8,9,2) for x in range(-8,9,2)]+[(-10,0),(10,0),(0,-10),(0,10)]

if __name__=='__main__':
 d=json.loads(Path(sys.argv[1]).read_text());fixture=json.loads(Path(sys.argv[3]).read_text());geometry=fixture_geometry(fixture);assert d['QueryGeometry']==geometry;assert [(r['X'],r['Z']) for r in d['SurfaceQueries']]==fixed_locations();result=compare(geometry,d['SurfaceQueries']);out=Path(sys.argv[2]);assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
