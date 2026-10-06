"""Independent exact-Fraction plane/Gram barycentric ray oracle; expectations precede native execution."""
import argparse,hashlib,importlib.util,json,math,os,stat,struct
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ORIGINAL='d1adcbb5de1759386c3ece620d1a8cd2d04ba30f35f9515cf21efa1eb99fbaa0'
def b64(v):return struct.unpack('<Q',struct.pack('<d',v))[0]
def d64(v):return struct.unpack('<d',struct.pack('<Q',v))[0]
def f32(v):return Fraction(struct.unpack('<f',struct.pack('<I',v))[0])
def ray(o,d=(0,-2,0),lo=0,hi=100):return dict(O=list(map(b64,o)),D=list(map(b64,d)),Min=b64(lo),Max=b64(hi))
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def load():
 p=ROOT/'poc/round62-tests/Rendering/verify_surface.py';s=importlib.util.spec_from_file_location('frozen62_ray_reference',p);q=importlib.util.module_from_spec(s);s.loader.exec_module(q);return q
SPECS=[('MaximumChunks',16,16),('MaximumPatches',256,256),('MaximumTriangles',2048,2048),('MaximumQueries',4096,4096),('MaximumBuildCopyBytes',1048576,4194304),('MaximumBuildArrayBytes',2097152,8388608),('MaximumBuildWork',1048576,16777216),('MaximumQueryCopyBytes',1048576,4194304),('MaximumQueryArrayBytes',2097152,8388608),('MaximumQueryWork',2147483648,4294967296),('MaximumTriangleTests',8388608,8388608)]
def options(c):
 l={k:d for k,d,h in SPECS};l.update(dict(MaximumComponent=1e6,MaximumParameter=1e9,MinimumInterpolatedNormalLength=1e-6));l.update(c.get('Limits',{}))
 for key,bits in [('MaximumComponent','ComponentBits'),('MaximumParameter','ParameterBits'),('MinimumInterpolatedNormalLength','NormalBits')]:
  if bits in c:l[key]=d64(c[bits])
 for k,d,h in SPECS:
  if not 0<=l[k]<=h:raise ValueError('Integer options')
 for k,h in [('MaximumComponent',1e9),('MaximumParameter',1e12),('MinimumInterpolatedNormalLength',1e9)]:
  if not math.isfinite(l[k]) or not 0<l[k]<=h:raise ValueError('Floating options')
 return l
def charges(s,q):
 z=q.charges(s);p=z['PatchCount'];t=z['TotalTriangles'];k=z['ChunkCount'];copies=216*p+6*t
 return dict(ChunkCount=k,PatchCount=p,TotalTriangles=t,OwnCopyBytes=copies,OwnArrayBytes=512+256*p+copies,OwnWork=128+64*k+128*p+128*t,BorrowedSurfaceArrayReservation=z['AggregateArrayBytes']+z['BorrowedSetArrayReservation'],NormalBuilds=0,GeometryBuilds=0,SurfaceBuilds=0,SetAdmissions=0,OriginalCollisionParity=False)
def reservation(n,c):return dict(Queries=n,CopyBytes=64*n,ArrayBytes=256+256*n,Work=128+64*n+n*(16*c['PatchCount']+128*c['TotalTriangles']),TriangleTestBound=n*c['TotalTriangles'])
def triangles(s):
 result=[]
 for ci,c in enumerate(s['Chunks']):
  for pi,p in enumerate(c['Patches']):
   pos=[tuple(map(f32,v)) for v in p['Positions']];normal=[tuple(map(f32,v)) for v in p['NormalBits']]
   for ti in range(len(p['Indices'])//3):
    ids=p['Indices'][3*ti:3*ti+3];a,b,cpos=[pos[i] for i in ids];e=sub(b,a);f=sub(cpos,a);n=cross(e,f)
    result.append((ci,pi,p['Pattern'],ti,ids,a,e,f,n,[normal[i] for i in ids]))
 return result
def hit(tris,r,l):
 o=tuple(Fraction(d64(v)) for v in r['O']);d=tuple(Fraction(d64(v)) for v in r['D']);lo=Fraction(d64(r['Min']));hi=Fraction(d64(r['Max']));best=None
 for ci,pi,pattern,ti,ids,a,e,f,n,normals in tris:
  den=dot(n,d)
  if not den:continue
  t=dot(n,sub(a,o))/den
  if t<lo or t>hi or (best is not None and t>=best[0]):continue
  p=tuple(x+t*y for x,y in zip(o,d));v=sub(p,a)
  # Independent 3D Gram solve rather than native largest-component projection.
  ee=dot(e,e);ef=dot(e,f);ff=dot(f,f);ve=dot(v,e);vf=dot(v,f);det=ee*ff-ef*ef
  if not det:continue
  wb=(ve*ff-vf*ef)/det;wc=(vf*ee-ve*ef)/det;wa=1-wb-wc
  if any(w<0 or w>1 for w in (wa,wb,wc)):continue
  if any(abs(x)>l['MaximumComponent'] for x in p):raise ValueError('Late position')
  best=(t,ci,pi,pattern,ti,ids,p,(wa,wb,wc),normals)
 if best is None:return None
 t,ci,pi,pattern,ti,ids,p,weights,normals=best;v=[sum(w*n[j] for w,n in zip(weights,normals)) for j in range(3)];length=math.hypot(*map(float,v))
 if length<=l['MinimumInterpolatedNormalLength']:raise ValueError('Late shading normal')
 return dict(Chunk=ci,Patch=pi,PatternIndex=pattern,TriangleIndex=ti,VertexA=ids[0],VertexB=ids[1],VertexC=ids[2],T=float(t),Position=list(map(float,p)),WeightA=float(weights[0]),WeightB=float(weights[1]),WeightC=float(weights[2]),Normal=[float(x)/length for x in v])
def expected(c,s,q,tris):
 try:
  l=options(c);cost=charges(s,q);res=reservation(len(c['Rays']),cost)
  if c.get('CancelBuild') or c.get('CancelQuery'):raise ValueError('Precancellation')
  for k,need in [('MaximumChunks',cost['ChunkCount']),('MaximumPatches',cost['PatchCount']),('MaximumTriangles',cost['TotalTriangles']),('MaximumBuildCopyBytes',cost['OwnCopyBytes']),('MaximumBuildArrayBytes',cost['OwnArrayBytes']),('MaximumBuildWork',cost['OwnWork']),('MaximumQueries',res['Queries']),('MaximumQueryCopyBytes',res['CopyBytes']),('MaximumQueryArrayBytes',res['ArrayBytes']),('MaximumQueryWork',res['Work']),('MaximumTriangleTests',res['TriangleTestBound'])]:
   if l[k]<need:raise ValueError('Reservation')
  if any(abs(f32(v))>l['MaximumComponent'] for chunk in s['Chunks'] for p in chunk['Patches'] for row in p['Positions'] for v in row):raise ValueError('Position preflight')
  for r in c['Rays']:
   vals=[d64(x) for x in r['O']+r['D']];lo=d64(r['Min']);hi=d64(r['Max'])
   if any(not math.isfinite(v) or abs(v)>l['MaximumComponent'] for v in vals) or not any(d64(v)!=0 for v in r['D']) or not math.isfinite(lo) or not math.isfinite(hi) or not 0<=lo<=hi<=l['MaximumParameter']:raise ValueError('Ray preflight')
  cache={};hits=[]
  for r in c['Rays']:
   k=json.dumps(r,sort_keys=True)
   if k not in cache:cache[k]=hit(tris,r,l)
   hits.append(cache[k])
  return dict(Name=c['Name'],Reject='None',Charges=cost,Reservation=res,Hits=hits)
 except (ValueError,OverflowError):return dict(Name=c['Name'],Reject='Input')
def generate(q):
 a,b,c,d=q.load();pool,selected,sets,_=q.generate(a,b,c,d);cases=[]
 def emit(si,rays,**kw):v=dict(Name=str(len(cases)),Set=si,Rays=rays,**kw);cases.append(v);return v
 for si,s in enumerate(sets):
  rays=[ray((-1000,100,-1000)),ray((0,100,0)),ray((8,100,8))]
  for chunk in s['Chunks'][:2]:
   p=chunk['Patches'][0];av,bv,cv=[list(map(f32,p['Positions'][i])) for i in p['Indices'][:3]];point=[float((av[j]+bv[j]+2*cv[j])/4) for j in range(3)]
   rays.extend([ray((point[0],point[1]+32,point[2])),ray((point[0],point[1]-32,point[2]),(0,4,0)),ray((point[0]-8,point[1]+16,point[2]-4),(1,-2,.5))])
  emit(si,rays)
 # Two flat disjoint chunks at heights 4 and 0: horizontal traversal plus descent has two hits.
 flat=q.copy(pool[-1]);flat['Request']['First']=0;flat['Heights']=[q.b32(4)]*len(flat['Heights']);flat['Name']=str(len(pool));pool.append(flat);id0=len(pool)-1
 low=q.copy(flat);low['Request']['First']=1;low['Heights']=[0]*len(low['Heights']);low['Name']=str(len(pool));pool.append(low);id1=len(pool)-1
 for ids in ([id1,id0],[id0,id1]):
  selected.append(dict(Ids=ids,Edge=0,Normal=0));values=[]
  for i in ids:
   v=a.enrich(pool[i],b.expected(pool[i],c,d),b,c,d)
   for p in v['Patches']:p['NormalBits']=[[q.b32(float(x)) for x in row] for row in p['Normals']]
   values.append(v)
  z=a.totals(values,False);sets.append(dict(Chunks=values,BorrowedArray=z['OwnArrayBytes']+z['BorrowedArrayReservation']));emit(len(sets)-1,[ray((-4,8,4),(2,-1,0)),ray((20,-4,4),(-2,1,0))],MultipleNearest=True)
 flatset=len(sets)-2
 emit(flatset,[ray((4,8,4),(0,-2,0),2,2),ray((4,8,4),(0,-2,0),0,2),ray((4,8,4),(0,-2,0),2,10),ray((4,8,4),(0,-2,0),0,math.nextafter(2,0)),ray((4,8,4),(0,-2,0),math.nextafter(2,math.inf),10),ray((4,4,4),(1,0,0)),ray((4,8,4),(1,0,0)),ray((4,4,4),(0,-2,0),0,0),ray((-0.,8,-0.),(-0.,-2,-0.)),ray((4,4,4),(0,-1e-300,0),0,1),ray((4,8,4),(0,-1e-300,0),0,1),ray((4,4,4),(0,-math.ulp(0.),0),0,1)])
 # Horizontal rays aimed at genuinely sloped geometry interiors.
 for si in (0,1,2):
  tr=triangles(sets[si]);p=tr[0];point=tuple(float(p[5][j]+p[6][j]/4+p[7][j]/2) for j in range(3))
  emit(si,[ray((point[0]-2,point[1],point[2]),(1,0,0)),ray((point[0],point[1],point[2]-2),(0,0,1))])
 emit(0,[]);emit(0,[],Limits=dict(MaximumQueries=0,MaximumQueryCopyBytes=0,MaximumQueryArrayBytes=256,MaximumQueryWork=128,MaximumTriangleTests=0))
 valid=ray((.5,100,.75));emit(0,[valid],CancelBuild=True);emit(0,[valid],CancelQuery=True)
 for bad in (math.nan,math.inf,-math.inf,1000001,-1000001):
  for part in ('O','D'):
   for axis in range(3):r=q.copy(valid);r[part][axis]=b64(bad);emit(0,[valid,r])
 for direction in ((0,0,0),(-0.,0,-0.)):emit(0,[ray((0,10,0),direction)])
 for lo,hi in ((-1,1),(2,1),(0,1e9+1),(math.nan,1),(0,math.inf)):emit(0,[ray((0,10,0),lo=lo,hi=hi)])
 emit(0,[ray((.5,100,.75),(1e9,-1e9,1e9))],Limits=dict(MaximumComponent=1e9,MaximumParameter=1e12))
 cost=charges(sets[0],q);res=reservation(2,cost);exact=dict(MaximumChunks=cost['ChunkCount'],MaximumPatches=cost['PatchCount'],MaximumTriangles=cost['TotalTriangles'],MaximumQueries=2,MaximumBuildCopyBytes=cost['OwnCopyBytes'],MaximumBuildArrayBytes=cost['OwnArrayBytes'],MaximumBuildWork=cost['OwnWork'],MaximumQueryCopyBytes=res['CopyBytes'],MaximumQueryArrayBytes=res['ArrayBytes'],MaximumQueryWork=res['Work'],MaximumTriangleTests=res['TriangleTestBound'])
 emit(0,[valid,valid],Limits=exact)
 for key,default,hard in SPECS:
  for l in ({key:-1},{key:hard+1},dict(exact,**{key:exact[key]-1})):emit(0,[valid,valid],Limits=l)
 for field,hard in [('ComponentBits',1e9),('ParameterBits',1e12),('NormalBits',1e9)]:
  for v in (0,-1,math.nan,math.inf,hard+1):emit(0,[],**{field:b64(v)})
 emit(0,[valid,valid],Limits=dict(MinimumInterpolatedNormalLength=100))
 late=next(i for i,v in enumerate(selected) if v["Ids"]==[len(pool)-3,0]);emit(late,[ray((-15.5,100,.75)),valid],Limits=dict(MinimumInterpolatedNormalLength=.999999999),LateFailure=True)
 largest=max(range(len(sets)),key=lambda i:(charges(sets[i],q)['TotalTriangles'],charges(sets[i],q)['ChunkCount']));maximum=reservation(4096,charges(sets[largest],q));rr=[valid]*4096
 big=charges(sets[largest],q);full=dict(MaximumChunks=big["ChunkCount"],MaximumPatches=big["PatchCount"],MaximumTriangles=big["TotalTriangles"],MaximumQueries=4096,MaximumBuildCopyBytes=big["OwnCopyBytes"],MaximumBuildArrayBytes=big["OwnArrayBytes"],MaximumBuildWork=big["OwnWork"],MaximumQueryCopyBytes=maximum["CopyBytes"],MaximumQueryArrayBytes=maximum["ArrayBytes"],MaximumQueryWork=maximum["Work"],MaximumTriangleTests=maximum["TriangleTestBound"]);emit(largest,rr,Limits=full)
 for key in full:emit(largest,rr,Limits=dict(full,**{key:full[key]-1}))
 for key in ('MaximumTriangleTests','MaximumQueryWork'):emit(largest,rr,Limits={key:maximum['TriangleTestBound' if key=='MaximumTriangleTests' else 'Work']-1})
 emit(0,[valid]*4097)
 return pool,selected,sets,cases

def compare(g,w):
 assert g['Name']==w['Name'] and g['Reject']==w['Reject'],(g['Name'],g['Reject'],w['Reject'])
 if w['Reject']!='None':return 0,0
 assert g['Charges']==w['Charges'],(g['Name'],'charges',g['Charges'],w['Charges']);assert g['Reservation']==w['Reservation'],(g['Name'],'reservation')
 hits=misses=0
 for a,b in zip(g['Hits'],w['Hits'],strict=True):
  if b is None:assert a is None,(g['Name'],'expected miss',a);misses+=1;continue
  assert a is not None,(g['Name'],'expected hit',b);hits+=1
  for key in ('Chunk','Patch','PatternIndex','TriangleIndex','VertexA','VertexB','VertexC'):assert a[key]==b[key],(g['Name'],key,a[key],b[key])
  for key in ('T','WeightA','WeightB','WeightC'):assert math.isclose(a[key],b[key],abs_tol=1e-9,rel_tol=2e-11),(g['Name'],key,a[key],b[key])
  for x,y in zip(a['Position'],b['Position']):assert math.isclose(x,y,abs_tol=1e-9,rel_tol=2e-11),(g['Name'],'position',x,y)
  for x,y in zip(a['Normal'],b['Normal']):assert abs(x-y)<=3e-6,(g['Name'],'normal',x,y)
 return hits,misses
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--exe',type=Path,required=True);parser.add_argument('--original',type=Path,required=True);parser.add_argument('--out',type=Path,required=True);parser.add_argument('--native',type=Path);a=parser.parse_args()
 if hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()!=EXE or hashlib.sha256(a.original.read_bytes()).hexdigest()!=ORIGINAL:raise ValueError('Pinned input identity before imports')
 a.out=Path(os.path.abspath(a.out))
 if not a.out.is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output')
  if not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory ancestor')
 if a.native:
  native=json.loads(a.native.read_text());want=json.loads((a.out/'ray-expected.json').read_text());assert len(native['Cases'])==len(want['Cases']);h=mi=0
  for g,w in zip(native['Cases'],want['Cases']):x,y=compare(g,w);h+=x;mi+=y
  g=dict(Name='original',Reject='None',**native['Original']);w=dict(Name='original',Reject='None',**want['Original']);x,y=compare(g,w);h+=x;mi+=y
  ok=[v for v in want['Cases'] if v['Reject']=='None'];receipt=dict(Passed=True,Cases=len(want['Cases']),Accepted=len(ok),Rejected=len(want['Cases'])-len(ok),ComparedHits=h,ComparedMisses=mi,OwnershipChecks=native['OwnershipChecks'],IndependentFractionReference=True,MultipleIntersectionProbes=4,NormalTolerance='3e-6',PositionWeightTAbsTolerance='1e-9',PositionWeightTRelativeTolerance='2e-11',OriginalRays=len(w['Hits']),OriginalHeightNormalComparisons=400,OriginalFixtureSha256=ORIGINAL,OriginalCollisionParity=False,OriginalCharges=g['Charges'],MaxTestedChunks=max(v['Charges']['ChunkCount'] for v in ok),MaxTestedPatches=max(v['Charges']['PatchCount'] for v in ok),MaxTestedTriangles=max(v['Charges']['TotalTriangles'] for v in ok),MaxTestedQueries=max(v['Reservation']['Queries'] for v in ok),MaxTriangleTestBound=max(v['Reservation']['TriangleTestBound'] for v in ok),MaxQueryWork=max(v['Reservation']['Work'] for v in ok))
  (a.out/'ray-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 q=load();pool,selected,sets,cases=generate(q);tr=[triangles(s) for s in sets];expectedCases=[expected(c,sets[c['Set']],q,tr[c['Set']]) for c in cases];assert expectedCases[0]['Reject']=='None'
 multiple=0
 for case,w in zip(cases,expectedCases):
  if not case.get('MultipleNearest'):continue
  assert w['Reject']=='None';l=options(case)
  for r,first in zip(case['Rays'],w['Hits']):
   assert first is not None;later=dict(r,Min=b64(math.nextafter(first['T'],math.inf)));second=hit(tr[case['Set']],later,l)
   assert second is not None and second['T']>first['T'] and second['Chunk']!=first['Chunk'];multiple+=1
 assert multiple==4
 original=json.loads(a.original.read_text());orig=dict(Chunks=[dict(Patches=c['Patches']) for c in original['Expected']],BorrowedArray=71360+256+128*4+4*400);ot=triangles(orig)
 points=[(-16,-16),(-8,-8),(0,0),(8,0),(8,8),(-.5,-.75),(1,1)]+[(x,z) for x in (-7.5,-4,-.5,.5,4,7.5) for z in (-7.5,-4,-.5,.5,4,7.5)]
 rays=[ray((x,100,z)) for x,z in points]+[ray((x,-100,z),(0,2,0)) for x,z in points];oc=dict(Name='original',Set=0,Rays=rays);o=expected(oc,orig,q,ot);assert o['Reject']=='None';o.pop('Name');o.pop('Reject')
 (a.out/'ray-fixtures.json').write_text(json.dumps(dict(Chunks=pool,Sets=selected,Cases=cases,Original=dict(Rays=rays))),encoding='utf-8');(a.out/'ray-expected.json').write_text(json.dumps(dict(Cases=expectedCases,Original=o)),encoding='utf-8');print(json.dumps(dict(Cases=len(cases),Chunks=len(pool),Sets=len(sets))))
if __name__=='__main__':main()
