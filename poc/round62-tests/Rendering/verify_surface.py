"""Independent physical triangle selection and Fraction barycentric reference for retained sets."""
import argparse,hashlib,importlib.util,json,math,os,stat,struct
from decimal import Decimal
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ORIGINAL='d1adcbb5de1759386c3ece620d1a8cd2d04ba30f35f9515cf21efa1eb99fbaa0'
def b64(v):return struct.unpack('<Q',struct.pack('<d',v))[0]
def d64(v):return struct.unpack('<d',struct.pack('<Q',v))[0]
def f32(v):return Fraction(struct.unpack('<f',struct.pack('<I',v))[0])
def b32(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def copy(v):return json.loads(json.dumps(v))
def load():
 p=ROOT/'poc/round61-tests/Rendering/verify_set.py';s=importlib.util.spec_from_file_location('frozen61_surface_reference',p);q=importlib.util.module_from_spec(s);s.loader.exec_module(q);return q,*q.load()
SPECS=[('MaximumChunks',16,16),('MaximumPatches',256,256),('MaximumTriangles',2048,2048),('MaximumQueries',4096,4096),('MaximumBuildCopyBytes',1048576,4194304),('MaximumBuildArrayBytes',2097152,8388608),('MaximumBuildWork',1048576,16777216),('MaximumQueryCopyBytes',1048576,4194304),('MaximumQueryArrayBytes',1048576,8388608),('MaximumQueryWork',1073741824,2147483648),('MaximumTriangleTests',8388608,8388608)]
def child_options(c):
 l=dict(MaximumCoordinateComponent=1000000,MinimumProjectedDoubleArea=1e-12,MinimumInterpolatedNormalLength=1e-6,MaximumQueriesPerBatch=4096,RequireCoherentWinding=True);l.update(c.get('Limits',{}).get('ChildLimits',{}))
 if 'ChildCapBits' in c:l['MaximumCoordinateComponent']=d64(c['ChildCapBits'])
 for key,hard in [('MaximumCoordinateComponent',1e9),('MinimumProjectedDoubleArea',1e18),('MinimumInterpolatedNormalLength',1e9)]:
  if not math.isfinite(l[key]) or not 0<l[key]<=hard:raise ValueError('Child options')
 if not 1<=l['MaximumQueriesPerBatch']<=65536:raise ValueError('Child batch option')
 return l

def charges(s):
 k=len(s['Chunks']);p=sum(len(c['Patches']) for c in s['Chunks']);t=sum(len(p['Indices'])//3 for c in s['Chunks'] for p in c['Patches'])
 ownarray=512+128*p;ownwork=128+64*k+64*p;cc=216*p+6*t;ca=256*p+cc+8*t;cw=64*p+128*t
 return dict(ChunkCount=k,PatchCount=p,TotalTriangles=t,ChildBuilds=p,NormalBuilds=0,GeometryBuilds=0,SetAdmissions=0,OriginalCollisionParity=False,OwnCopyBytes=0,OwnArrayBytes=ownarray,OwnWork=ownwork,ChildPositionCopyBytes=108*p,ChildNormalCopyBytes=108*p,ChildIndexCopyBytes=6*t,ChildCopyBytes=cc,ChildAreaArrayBytes=8*t,ChildArrayBytes=ca,ChildWork=cw,ChildLogicalOwnedBytes=216*p+14*t,AggregateCopyBytes=cc,AggregateArrayBytes=ownarray+ca,AggregateWork=ownwork+cw,BorrowedSetArrayReservation=s['BorrowedArray'])
def reservation(count,cost):
 return dict(Queries=count,CopyBytes=16*count,ArrayBytes=256+160*count,Work=128+16*count+count*(16*cost['PatchCount']+64*cost['TotalTriangles']),TriangleTestBound=count*cost['TotalTriangles'])
def hit(s,location,opts):
 x,z=map(lambda v:Fraction(d64(v)),location)
 for ci,c in enumerate(s['Chunks']):
  for pi,p in enumerate(c['Patches']):
   pos=[[f32(v) for v in row] for row in p['Positions']];normal=[[f32(v) for v in row] for row in p['NormalBits']]
   if not min(v[0] for v in pos)<=x<=max(v[0] for v in pos) or not min(v[2] for v in pos)<=z<=max(v[2] for v in pos):continue
   ix=p['Indices']
   for ti in range(len(ix)//3):
    ids=ix[3*ti:3*ti+3];a,b,cpos=[pos[i] for i in ids];dx=b[0]-a[0];dz=b[2]-a[2];ex=cpos[0]-a[0];ez=cpos[2]-a[2];den=dx*ez-dz*ex
    wb=((x-a[0])*ez-(z-a[2])*ex)/den;wc=(dx*(z-a[2])-dz*(x-a[0]))/den;wa=1-wb-wc
    if any(w<0 or w>1 for w in (wa,wb,wc)):continue
    weights=(wa,wb,wc);height=sum(w*pos[i][1] for w,i in zip(weights,ids));vec=[sum(w*normal[i][j] for w,i in zip(weights,ids)) for j in range(3)];length=math.hypot(*map(float,vec))
    if not math.isfinite(length) or length<=opts['MinimumInterpolatedNormalLength'] or abs(height)>opts['MaximumCoordinateComponent']:raise ValueError('Late selected result')
    return dict(Chunk=ci,Patch=pi,PatternIndex=p['Pattern'],TriangleIndex=ti,VertexA=ids[0],VertexB=ids[1],VertexC=ids[2],WeightA=float(wa),WeightB=float(wb),WeightC=float(wc),Height=float(height),Normal=[float(v)/length for v in vec])
 return None

def expected(c,sets):
 try:
  limits={k:d for k,d,h in SPECS};limits.update({k:v for k,v in c.get('Limits',{}).items() if k!='ChildLimits'})
  for k,d,h in SPECS:
   if not 0<=limits[k]<=h:raise ValueError('Parent options')
  opts=child_options(c);s=sets[c['Set']];cost=charges(s)
  if c.get('CancelBuild') or c.get('CancelQuery') or c.get('TruncateSource'):raise ValueError('Cancel/truncated supplied source')
  for key,need in [('MaximumChunks',cost['ChunkCount']),('MaximumPatches',cost['PatchCount']),('MaximumTriangles',cost['TotalTriangles']),('MaximumBuildCopyBytes',cost['AggregateCopyBytes']),('MaximumBuildArrayBytes',cost['AggregateArrayBytes']),('MaximumBuildWork',cost['AggregateWork'])]:
   if limits[key]<need:raise ValueError('Build preflight')
  for chunk in s['Chunks']:
   for p in chunk['Patches']:
    if any(abs(f32(v))>opts['MaximumCoordinateComponent'] for row in p['Positions'] for v in row):raise ValueError('Child position cap')
    pos=[[f32(v) for v in row] for row in p['Positions']]
    for ti in range(len(p['Indices'])//3):
     a,b,cp=[pos[i] for i in p['Indices'][3*ti:3*ti+3]];area=(b[0]-a[0])*(cp[2]-a[2])-(b[2]-a[2])*(cp[0]-a[0])
     if abs(area)<=opts['MinimumProjectedDoubleArea']:raise ValueError('Child area')
  count=len(c['Locations']);res=reservation(count,cost)
  for key,need in [('MaximumQueries',count),('MaximumQueryCopyBytes',res['CopyBytes']),('MaximumQueryArrayBytes',res['ArrayBytes']),('MaximumQueryWork',res['Work']),('MaximumTriangleTests',res['TriangleTestBound'])]:
   if limits[key]<need:raise ValueError('Query preflight')
  for point in c['Locations']:
   if any(not math.isfinite(d64(v)) or abs(d64(v))>opts['MaximumCoordinateComponent'] for v in point):raise ValueError('Location preflight')
  return dict(Name=c['Name'],Reject='None',Charges=cost,Reservation=res,Hits=[hit(s,point,opts) for point in c['Locations']])
 except (ValueError,IndexError,OverflowError):return dict(Name=c['Name'],Reject='Input')

def generate(q,f,n,m):
 pool,admissions,base=q.generate(f,n,m);flat=copy(pool[0]);flat['Name']=str(len(pool));flat['Request']['First']=-2;flat['Heights']=[0]*len(flat['Heights']);pool.append(flat);flatid=len(pool)-1
 values=[q.enrich(c,f.expected(c,n,m),f,n,m) for c in pool]
 for v in values:
  for p in v['Patches']:p['NormalBits']=[[b32(float(Decimal(x))) for x in row] for row in p['Normals']]
 selected=[]
 for c in admissions:
  if q.expected(c,values)['Reject']=='None':selected.append(dict(Ids=c['Ids'],Edge=c['Edge'],Normal=c['Normal']))
 selected=selected[:];selected.append(dict(Ids=[0],Edge=1,Normal=0));single=len(selected)-1;selected.append(dict(Ids=[flatid,0],Edge=0,Normal=0));late=len(selected)-1
 # Keep defaults, all masks, profile combinations, reversed ordering, gaps and maxima.
 sets=[]
 for s in selected:
  v=[values[i] for i in s['Ids']];a=q.totals(v,s['Edge']==1);sets.append(dict(Chunks=v,BorrowedArray=a['OwnArrayBytes']+a['BorrowedArrayReservation']))
 cases=[]
 def emit(si,points,**kw):c=dict(Name=str(len(cases)),Set=si,Locations=[[b64(float(x)),b64(float(z))] for x,z in points],**kw);cases.append(c);return c
 for si,s in enumerate(sets):
  points=[(-1000,-1000),(0,0),(8,0),(8,8),(.5,.75),(16,16)]
  for c in s['Chunks'][:2]:
   p=c['Patches'][0];pos=p['Positions'];points.extend([(float(f32(pos[0][0])),float(f32(pos[0][2]))),(float(f32(pos[8][0])),float(f32(pos[8][2])))])
   a,b,cp=[pos[i] for i in p['Indices'][:3]];points.append((float((f32(a[0])+f32(b[0])+f32(cp[0]))/3),float((f32(a[2])+f32(b[2])+f32(cp[2]))/3)))
  emit(si,points)
 emit(0,[]);emit(0,[],Limits=dict(MaximumQueries=0,MaximumQueryCopyBytes=0,MaximumQueryArrayBytes=256,MaximumQueryWork=128,MaximumTriangleTests=0));emit(0,[(1000000,0),(-1000000,0),(-0.0,-0.0)]);
 emit(single,[(math.nextafter(0,-math.inf),4),(math.nextafter(8,math.inf),4),(4,math.nextafter(0,-math.inf)),(4,math.nextafter(8,math.inf))]);
 largest=max(range(len(sets)),key=lambda i:charges(sets[i])['TotalTriangles']);maximum=reservation(4096,charges(sets[largest]));
 emit(largest,[(.5,.75)]*4096,Limits=dict(MaximumQueries=4096,MaximumTriangleTests=maximum['TriangleTestBound'],MaximumQueryWork=maximum['Work']));
 emit(largest,[(.5,.75)]*4096,Limits=dict(MaximumTriangleTests=maximum['TriangleTestBound']-1));
 emit(largest,[(.5,.75)]*4096,Limits=dict(MaximumQueryWork=maximum['Work']-1));
 emit(0,[(.5,.75),(8,8)],Limits=dict(ChildLimits=dict(MaximumQueriesPerBatch=1)));
 emit(single,[(.5,.75)]*4096);emit(single,[(.5,.75)]*4097)
 for bad in (float('nan'),float('inf'),-float('inf'),1000001,-1000001):emit(0,[(.5,.75),(bad,0)]);emit(0,[(.5,.75),(0,bad)])
 emit(0,[(.5,.75)],CancelBuild=True);emit(0,[(.5,.75)],CancelQuery=True);emit(0,[(.5,.75)],TruncateSource=True)
 emit(late,[(-15.5,.75),(.5,.75)],Limits=dict(ChildLimits=dict(MinimumInterpolatedNormalLength=.999999999)),LateFailure=True)
 emit(0,[(.5,.75)],Limits=dict(ChildLimits=dict(MinimumProjectedDoubleArea=1e18)))
 emit(0,[(.5,.75)],Limits=dict(ChildLimits=dict(MinimumInterpolatedNormalLength=100)))
 for bad in (0,-1,float('nan'),float('inf'),1e9+1):emit(0,[],ChildCapBits=b64(bad))
 cost=charges(sets[0]);res=reservation(2,cost);exact=dict(MaximumChunks=cost['ChunkCount'],MaximumPatches=cost['PatchCount'],MaximumTriangles=cost['TotalTriangles'],MaximumQueries=2,MaximumBuildCopyBytes=cost['AggregateCopyBytes'],MaximumBuildArrayBytes=cost['AggregateArrayBytes'],MaximumBuildWork=cost['AggregateWork'],MaximumQueryCopyBytes=res['CopyBytes'],MaximumQueryArrayBytes=res['ArrayBytes'],MaximumQueryWork=res['Work'],MaximumTriangleTests=res['TriangleTestBound'])
 emit(0,[(.5,.75),(8,8)],Limits=exact)
 for key,d,h in SPECS:
  for limits in ({key:-1},{key:h+1},dict(exact,**{key:exact[key]-1})):emit(0,[(.5,.75),(8,8)],Limits=limits)
 for child in (dict(MaximumQueriesPerBatch=0),dict(MaximumQueriesPerBatch=65537),dict(MinimumProjectedDoubleArea=0),dict(MinimumInterpolatedNormalLength=0)):emit(0,[],Limits=dict(ChildLimits=child))
 emit(0,[(.5,.75),(8,8)],Limits=dict(ChildLimits=dict(RequireCoherentWinding=False)))
 return pool,selected,sets,cases

def compare(got,want):
 assert got['Name']==want['Name'] and got['Reject']==want['Reject'],(got['Name'],got['Reject'],want['Reject'])
 if want['Reject']!='None':return 0,0
 assert got['Charges']==want['Charges'],(got['Name'],'charges');assert got['Reservation']==want['Reservation'],(got['Name'],'reservation')
 assert len(got['Hits'])==len(want['Hits']);h=mi=0
 for a,b in zip(got['Hits'],want['Hits']):
  if b is None:assert a is None,(got['Name'],'outside');mi+=1;continue
  assert a is not None,(got['Name'],'missing hit');h+=1
  for key in ('Chunk','Patch','PatternIndex','TriangleIndex','VertexA','VertexB','VertexC'):assert a[key]==b[key],(got['Name'],key,a[key],b[key])
  for key in ('WeightA','WeightB','WeightC','Height'):assert math.isclose(a[key],b[key],rel_tol=2e-11,abs_tol=1e-9),(got['Name'],key,a[key],b[key])
  for x,y in zip(a['Normal'],b['Normal']):assert abs(x-y)<=3e-6,(got['Name'],'normal',x,y)
 return h,mi

def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--original',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
 if hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()!=EXE or hashlib.sha256(a.original.read_bytes()).hexdigest()!=ORIGINAL:raise ValueError('Pinned input identity before imports')
 a.out=Path(os.path.abspath(a.out))
 if not a.out.is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output')
  if not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory ancestor')
 if a.native:
  native=json.loads(a.native.read_text());want=json.loads((a.out/'surface-expected.json').read_text());assert len(native['Cases'])==len(want['Cases']);h=mi=0
  for g,e in zip(native['Cases'],want['Cases']):x,y=compare(g,e);h+=x;mi+=y
  o=want['Original'];actual=native['Original'];assert actual['Charges']==o['Charges'];x,y=compare(dict(Name='original',Reject='None',Reservation={},**actual),dict(Name='original',Reject='None',Reservation={},**o));h+=x;mi+=y
  accepted=[c for c in want['Cases'] if c['Reject']=='None']
  receipt=dict(MaxTestedChunks=max(c['Charges']['ChunkCount'] for c in accepted),MaxTestedPatches=max(c['Charges']['PatchCount'] for c in accepted),MaxTestedTriangles=max(c['Charges']['TotalTriangles'] for c in accepted),MaxTestedQueries=max(c['Reservation']['Queries'] for c in accepted),MaxTriangleTestBound=max(c['Reservation']['TriangleTestBound'] for c in accepted),MaxQueryWork=max(c['Reservation']['Work'] for c in accepted),IndependentFractionReference=True,DecimalNormalPrecision=100,Passed=True,Cases=len(want['Cases']),Accepted=sum(c['Reject']=='None' for c in want['Cases']),Rejected=sum(c['Reject']!='None' for c in want['Cases']),ComparedHits=h,ComparedMisses=mi,OwnershipChecks=native['OwnershipChecks'],OriginalPoints=len(o['Hits']),OriginalHeightNormalComparisons=400,OriginalFixtureSha256=ORIGINAL,OriginalCollisionParity=False,NormalTolerance='3e-6',BarycentricHeightAbsTolerance='1e-9',BarycentricHeightRelativeTolerance='2e-11',OriginalCharges=actual['Charges'])
  (a.out/'surface-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 q,f,n,m=load();pool,selected,sets,cases=generate(q,f,n,m);expectedCases=[expected(c,sets) for c in cases];assert expectedCases[0]['Reject']=='None'
 original=json.loads(a.original.read_text());orig=dict(Chunks=[dict(Patches=c['Patches']) for c in original['Expected']],BorrowedArray=71360+256+128*4+4*400)
 originalPoints=[(-16,-16),(-8,-8),(0,0),(8,0),(8,8),(-.5,-.75),(1,1)]
 for x in (-7.5,-4,-.5,.5,4,7.5):
  for z in (-7.5,-4,-.5,.5,4,7.5):originalPoints.append((x,z))
 op=[[b64(x),b64(z)] for x,z in originalPoints];o=dict(Charges=charges(orig),Hits=[hit(orig,p,child_options({})) for p in op])
 (a.out/'surface-fixtures.json').write_text(json.dumps(dict(Chunks=pool,Sets=selected,Cases=cases,OriginalLocations=op)),encoding='utf-8');(a.out/'surface-expected.json').write_text(json.dumps(dict(Cases=expectedCases,Original=o)),encoding='utf-8');print(json.dumps(dict(Cases=len(cases),PoolChunks=len(pool),Sets=len(sets))))
if __name__=='__main__':main()
