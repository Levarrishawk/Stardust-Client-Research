"""Synthetic supplied surface oracle: independent exact rational containment/height plus double sqrt normal."""
import json,math,sys
from fractions import Fraction as F
from pathlib import Path
PATTERNS=[
[0,1,2,0,2,3,0,3,4,0,4,5,0,5,6,0,6,7,0,7,8,0,8,1],
[0,1,2,0,2,4,0,4,5,0,5,6,0,6,7,0,7,8,0,8,1],
[0,2,4,0,4,5,0,5,6,0,6,7,0,7,8,0,8,2],
[0,2,3,0,3,4,0,4,5,0,5,6,0,6,7,0,7,8,0,8,2],
[0,2,3,0,3,4,0,4,5,0,5,6,0,6,8,0,8,2],
[0,1,2,0,2,3,0,3,4,0,4,5,0,5,6,0,6,8,0,8,1],
[0,1,2,0,2,3,0,3,4,0,4,6,0,6,8,0,8,1],
[0,1,2,0,2,3,0,3,4,0,4,6,0,6,7,0,7,8,0,8,1],
[0,1,2,0,2,4,0,4,6,0,6,7,0,7,8,0,8,1]]
OFFSETS=[(1,1),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0),(0,0)]
BASE=[(.5,.5),(0,.5),(0,1),(.5,1),(1,1),(1,.5),(1,0),(.5,0),(0,0)]
def select(count,col,row,mask):
 last=count-1
 if row==0 and mask&4:return 4 if col==0 and mask&2 else 6 if col==last and mask&8 else 5
 if col==0 and mask&2:return 2 if row==last and mask&1 else 3
 if row==last and mask&1:return 8 if col==last and mask&8 else 1
 return 7 if col==last and mask&8 else 0

def generate():
 cases=[];points=[(i/8,j/8) for i in range(-2,11) for j in range(-2,11)]+[(math.nextafter(0,-math.inf),.25),(math.nextafter(0,math.inf),.25),(math.nextafter(1,math.inf),.25),(math.nextafter(1,0),.25)]
 for mode in ('flat','slope','nonplanar','reversed','offset'):
  for pattern in range(9):
   p=[];n=[]
   for i,(x,z) in enumerate(BASE):
    if mode=='flat':y=3;normal=(0,2,0)
    elif mode=='slope':y=2*x-3*z+1;normal=(-2,1,3)
    elif mode=='nonplanar':y=[3,1,8,-3,2,5,0,12,-9][i]/16;normal=((i%3-1)/64,1+(i%2)/4,((i*2)%3-1)/64)
    elif mode=='reversed':z=-z;y=3;normal=(0,-2,0)
    else:x=64+4*x;z=-32+4*z;y=x/8-z/4;normal=(-.125,1,.25)
    p.append([x,y,z]);n.append(normal)
   q=[(x,-z) for x,z in points] if mode=='reversed' else [(64+4*x,-32+4*z) for x,z in points] if mode=='offset' else points
   cases.append(dict(Name=f'{mode}_{pattern}',Pattern=pattern,Positions=p,Normals=n,Locations=q))
 overlap=[(0,0),(-1,0),(-1,1),(1,1),(1,0),(-1,-1),(-1,0),(1,1),(1,-1)]
 cases.append(dict(Name='overlap_first',Pattern=0,Positions=[[x,i/16,z] for i,(x,z) in enumerate(overlap)],Normals=[[0,1,0]]*9,Locations=[(i/8,j/8) for i in range(-9,10) for j in range(-9,10)]))
 seams=[]
 for count in range(1,4):
  for row in range(count):
   for col in range(count):
    for mask in range(16):
     border=1;ox=border+2*col;oz=border+2*row;width=border+2*count+1;height=width+1
     p=[[(ox+x)/2,((ox+x)-2*(oz+z))/16,(oz+z)/2] for x,z in OFFSETS]
     seams.append(dict(Name=f'seam_{count}_{col}_{row}_{mask}',Count=count,Column=col,Row=row,Border=border,Mask=mask,Width=width,Height=height,Pattern=select(count,col,row,mask),Positions=p,Normals=[[-.125,1,.25]]*9,Locations=[(ox/2+.5,oz/2+.5),(ox/2+.25,oz/2+.25),(ox/2,oz/2+.5),(ox/2-.125,oz/2+.5)]))
 return json.loads(json.dumps(dict(Synthetic=True,Cases=cases,Seams=seams)))

def cross(a,b):return a[0]*b[1]-a[1]*b[0]
def reference(c,q):
 p=[[F(v) for v in x] for x in c['Positions']];norm=[[F(v) for v in x] for x in c['Normals']];q=[F(v) for v in q];idx=PATTERNS[c['Pattern']]
 for t in range(len(idx)//3):
  ids=idx[3*t:3*t+3];a,b,d=[p[i] for i in ids];area=cross([b[0]-a[0],b[2]-a[2]],[d[0]-a[0],d[2]-a[2]])
  weights=[cross([b[0]-q[0],b[2]-q[1]],[d[0]-q[0],d[2]-q[1]])/area,cross([d[0]-q[0],d[2]-q[1]],[a[0]-q[0],a[2]-q[1]])/area,cross([a[0]-q[0],a[2]-q[1]],[b[0]-q[0],b[2]-q[1]])/area]
  if any(w<0 for w in weights):continue
  h=sum(w*p[i][1] for w,i in zip(weights,ids));n=[float(sum(w*norm[i][axis] for w,i in zip(weights,ids))) for axis in range(3)];length=math.hypot(*n);n=[v/length for v in n]
  return dict(TriangleIndex=t,Vertices=ids,Weights=[float(v) for v in weights],Height=float(h),Normal=n)
 return None
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8',newline='\n')
 else:
  f=json.loads((Path(__file__).parent/'supplied-fixtures.json').read_text());assert f==generate();n=json.loads(Path(sys.argv[2]).read_text());assert n['Checks']==10131 and n['NegativeChecks']==44;rows={r['Name']:r for r in n['Records']};assert len(rows)==len(n['Records'])==len(f['Cases'])+len(f['Seams']);hits=outside=0
  for c in f['Cases']+f['Seams']:
   r=rows[c['Name']];assert r['Pattern']==c['Pattern'] and r['TriangleCount']==len(PATTERNS[c['Pattern']])//3 and r['LogicalOwnedBytes']==216+2*len(PATTERNS[c['Pattern']])+8*r['TriangleCount'];assert len(r['Hits'])==len(c['Locations'])
   for q,actual in zip(c['Locations'],r['Hits']):
    expected=reference(c,q)
    if expected is None:assert actual is None,(c['Name'],q,actual);outside+=1;continue
    assert actual is not None,(c['Name'],q,expected);assert actual['PatternIndex']==c['Pattern'] and actual['TriangleIndex']==expected['TriangleIndex'] and [actual[k] for k in ('VertexA','VertexB','VertexC')]==expected['Vertices'],(c['Name'],q,actual,expected)
    for key,val in zip(('WeightA','WeightB','WeightC'),expected['Weights']):assert math.isclose(actual[key],val,rel_tol=2e-13,abs_tol=2e-13),(c['Name'],q,key,actual[key],val)
    assert math.isclose(actual['Height'],expected['Height'],rel_tol=2e-13,abs_tol=2e-13),(c['Name'],q,'height',actual,expected)
    assert all(math.isclose(actual['Normal'][key],val,rel_tol=2e-13,abs_tol=2e-13) for key,val in zip(('X','Y','Z'),expected['Normal']));hits+=1
  print(json.dumps(dict(passed=True,native_checks=n['Checks'],negative_checks=n['NegativeChecks'],records=len(rows),geometry_cases=len(f['Cases']),seam_cases=len(f['Seams']),hits=hits,outside=outside,original_execution=False),indent=2))
