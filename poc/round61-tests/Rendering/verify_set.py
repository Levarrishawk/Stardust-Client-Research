"""Independent Fraction physical-region/shared-point oracle over frozen modern receipts."""
import argparse,hashlib,importlib.util,itertools,json,os,stat,struct
from decimal import Decimal
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ORIGINAL='d1adcbb5de1759386c3ece620d1a8cd2d04ba30f35f9515cf21efa1eb99fbaa0'
def load():
 p=ROOT/'poc/round60-tests/Rendering/verify_patch.py';s=importlib.util.spec_from_file_location('frozen60_set_reference',p);f=importlib.util.module_from_spec(s);s.loader.exec_module(f);n=f.load('frozen59_set_reference','poc/round59-tests/Rendering/verify_normals.py');m=f.prepare(n);return f,n,m
def word(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def number(v):return Fraction(struct.unpack('<f',struct.pack('<I',v))[0])
def clone(v):return json.loads(json.dumps(v))
SPECS=[('MaximumChunks',16,16),('MaximumAxisSamples',32,32),('MaximumTotalCells',4096,4096),('MaximumTotalPatches',256,256),('MaximumSelectedSamples',2304,2304),('MaximumCopyBytes',262144,1048576),('MaximumArrayBytes',1048576,4194304),('MaximumWork',2097152,16777216),('MaximumBorrowedCopyReservation',4194304,16777216),('MaximumBorrowedArrayReservation',8388608,33554432),('MaximumBorrowedWorkReservation',16777216,67108864)]
def totals(chunks,strict):
 k=len(chunks);n=sum(c['Count'] for c in chunks);p=sum(c['PatchCount'] for c in chunks);widths=[c['Width'] for c in chunks];w=max(widths,default=0);channels=4*n if strict else 0
 return dict(Count=k,TotalCells=n,TotalPatches=p,SelectedSamples=9*p,OwnCopyBytes=8*k+channels,OwnArrayBytes=256+128*k+channels,OwnWork=128+128*k+16*sum(widths)+channels+k*(k-1)//2*(256+64*w),BorrowedCopyReservation=sum(c['AggregateCopyBytes'] for c in chunks),BorrowedArrayReservation=sum(c['AggregateArrayBytes'] for c in chunks),BorrowedWorkReservation=sum(c['AggregateWork'] for c in chunks))
def expected(case,chunks):
 try:
  edge=case['Edge'];normal=case['Normal']
  if edge not in (0,1) or normal not in (0,1) or edge==0 and normal!=0 or case.get('Cancel') or 'Foreign' in case or 'Null' in case:raise ValueError('Options/identity/cancel')
  values=[chunks[i] for i in case['Ids']];caps={k:d for k,d,h in SPECS};caps.update(case.get('Limits',{}))
  for k,d,h in SPECS:
   if not 0<=caps[k]<=h:raise ValueError('Cap range')
  t=totals(values,edge==1)
  for k,needed in [('MaximumChunks',t['Count']),('MaximumAxisSamples',max((v['Width'] for v in values),default=0)),('MaximumTotalCells',t['TotalCells']),('MaximumTotalPatches',t['TotalPatches']),('MaximumSelectedSamples',t['SelectedSamples']),('MaximumCopyBytes',t['OwnCopyBytes']),('MaximumArrayBytes',t['OwnArrayBytes']),('MaximumWork',t['OwnWork']),('MaximumBorrowedCopyReservation',t['BorrowedCopyReservation']),('MaximumBorrowedArrayReservation',t['BorrowedArrayReservation']),('MaximumBorrowedWorkReservation',t['BorrowedWorkReservation'])]:
   if needed>caps[k]:raise ValueError('Preflight')
  edges=corners=compared=0
  for a,b in itertools.combinations(values,2):
   if a['Request']==b['Request']:raise ValueError('Duplicate key')
   ax0,ax1,az0,az1=a['Bounds'];bx0,bx1,bz0,bz1=b['Bounds'];xl=max(ax0,bx0);xh=min(ax1,bx1);zl=max(az0,bz0);zh=min(az1,bz1)
   if xl<xh and zl<zh:raise ValueError('Interior overlap')
   xt=ax1==bx0 or bx1==ax0;zt=az1==bz0 or bz1==az0
   shared=None
   if xt and zl<zh:
    edges+=1;x=ax1 if ax1==bx0 else ax0;shared=lambda point:point[0]==x and zl<=point[1]<=zh;ends=((x,zl),(x,zh))
   elif zt and xl<xh:
    edges+=1;z=az1 if az1==bz0 else az0;shared=lambda point:point[1]==z and xl<=point[0]<=xh;ends=((xl,z),(xh,z))
   elif xt and zt:
    corners+=1;x=ax1 if ax1==bx0 else ax0;z=az1 if az1==bz0 else az0;shared=lambda point:point==(x,z);ends=((x,z),)
   if edge==1 and shared is not None:
    aa={k:v for k,v in a['Points'].items() if shared(k)};bb={k:v for k,v in b['Points'].items() if shared(k)}
    if set(aa)!=set(bb) or not all(k in aa for k in ends):raise ValueError('Incompatible/missing active edge samples')
    for key in aa:
     av=aa[key];bv=bb[key]
     if av[:3]!=bv[:3] or normal==1 and av[3]!=bv[3]:raise ValueError('Raw shared position/height/normal words')
     compared+=1
  t.update(SharedEdgePairs=edges,SharedCornerPairs=corners,SharedSampleComparisons=compared,NormalBuilds=0,GeometryBuilds=0,OriginalNormalParity=False,OriginalTerrainParity=False,SignedZeroHeightWordsAreDistinct=True,Edge=edge,Normal=normal)
  return dict(Name=case['Name'],Reject='None',Receipt=t)
 except (ValueError,IndexError):return dict(Name=case['Name'],Reject='Input')
def enrich(c,out,f,n,m):
 assert out['Reject']=='None',c['Name'];grid=m['coord'](dict(Name=c['Name'],Request=c['Request'],Profiles=c['Profiles'],Limits=m['producer']['DEFAULT']));x=grid['Position']['First'];z=grid['Position']['Second'];w=out['Width'];begin=c['Request']['Margin'];end=begin+2*c['Request']['Tiles'];points={}
 normals=[[word(float(Decimal(v))) for v in ns] for ns in out['Normals']]
 for row in range(begin,end+1):
  for col in range(begin,end+1):
   i=row*w+col;points[(number(x[col]),number(z[row]))]=(x[col],z[row],out['Stored'][i],normals[i])
 out.update(Request=c['Request'],Bounds=(number(x[begin]),number(x[end]),number(z[begin]),number(z[end])),Points=points,X=x,Z=z);return out

def generate(f,n,m):
 pool=[];cases=[]
 def chunk(first=0,second=0,width=8,tiles=2,margin=3,profiles=(24,24,24,24),kind='plane',mask=0,scale=1):
  req=dict(ChunkWidthBits=word(width),Tiles=tiles,Margin=margin,First=first,Second=second,Scale=scale);grid=m['coord'](dict(Name='seed',Request=req,Profiles=list(profiles),Limits=m['producer']['DEFAULT']));w=grid['Parameters']['Count'];h=[]
  for row in range(w):
   for col in range(w):
    x=number(grid['Position']['First'][col]);z=number(grid['Position']['Second'][row]);value=x/8-z/4+Fraction(1,2)
    if kind=='wave':value+=x*x/128+z*z/256
    h.append(m['a'].bits((value,False)) if kind!='zero' else 0x80000000)
  c=dict(Name=str(len(pool)),Request=req,Profiles=list(profiles),Mask=mask,ScaleBits=word(1),Heights=h,Normals=[[0,word(1),0]]*(w*w),Colors=[(0x80402010+i*0x010307)&0xffffffff for i in range(w*w)],Limits=dict(MaximumGridSamples=1048576,MaximumPatches=1024,MaximumSelectedSamples=9216));pool.append(c);return len(pool)-1
 def emit(ids,edge=1,normal=0,**kw):cases.append(dict(Name=str(len(cases)),Ids=ids,Edge=edge,Normal=normal,**kw));return cases[-1]
 base=[chunk(x,z,kind='wave') for z in (0,1) for x in (0,1)]
 emit(base,normal=1)
 for order in itertools.permutations(base):emit(list(order),normal=1)
 emit(base,edge=0);emit([]);emit([],edge=0);
 emit([],Limits=dict(MaximumChunks=0,MaximumAxisSamples=0,MaximumTotalCells=0,MaximumTotalPatches=0,MaximumSelectedSamples=0,MaximumCopyBytes=0,MaximumArrayBytes=256,MaximumWork=128,MaximumBorrowedCopyReservation=0,MaximumBorrowedArrayReservation=0,MaximumBorrowedWorkReservation=0));emit([],Limits=dict(MaximumArrayBytes=255));emit([],Limits=dict(MaximumWork=127));emit([base[0],base[3]],normal=1);far=chunk(5,5);emit([0,far]);emit([0,far],edge=0)
 for mask in range(16):emit([chunk(0,0,mask=mask),chunk(1,0,mask=mask)],normal=1)
 for profile in itertools.product((24,53,64),repeat=4):
  a=chunk(0,0,width=1.000001,tiles=1,margin=1,profiles=profile);b=chunk(1,0,width=1.000001,tiles=1,margin=1,profiles=tuple(reversed(profile)));emit([a,b]);emit([b,a],edge=0)
 coarse=chunk(1,0,tiles=1,margin=2);emit([0,coarse]);emit([0,coarse],edge=0)
 partial=chunk(2,0,width=4,tiles=1,margin=2);emit([0,partial],normal=1)
 missing=chunk(2,1,width=4,tiles=1,margin=2,scale=3);emit([0,missing]);emit([0,missing],edge=0)
 other=chunk(0,0,width=16);emit([0,other]);emit([0,other],edge=0)
 emit([0,0]);emit([0,0],edge=0)
 padding=clone(pool[1]);padding['Name']=str(len(pool));w=10
 for row in range(w):padding['Heights'][row*w+2]=word(float(number(padding['Heights'][row*w+2])+Fraction(1,64)))
 pool.append(padding);pad=len(pool)-1;emit([0,pad]);emit([0,pad],normal=1)
 mismatch=clone(pool[3]);mismatch['Name']=str(len(pool));mismatch['Heights'][33]=word(float(number(mismatch['Heights'][33])+Fraction(1,64)));pool.append(mismatch);bad=len(pool)-1
 emit([0,1,2,bad],LateMismatch=True);emit([0,bad]);emit([0,1,2,bad],edge=0)
 negative=[chunk(0,2,kind='zero'),chunk(1,2,kind='zero')];pool[negative[1]]['VerticalOriginBits']=0x80000000;emit(negative);emit(negative,edge=0)
 emit(base,Foreign=0);emit(base,Null=3);emit(base,Cancel=True);emit(base,edge=2);emit(base,normal=2);emit(base,edge=0,normal=1)
 largest=[chunk(x,z,tiles=8,margin=8) for z in (0,1) for x in (0,1)];emit(largest,normal=1,Limits=dict(MaximumTotalCells=4096,MaximumTotalPatches=256,MaximumSelectedSamples=2304,MaximumAxisSamples=32))
 sixteen=[chunk(x,z,tiles=4,margin=1) for z in range(4) for x in range(4)];emit(sixteen,normal=1,Limits=dict(MaximumChunks=16,MaximumTotalPatches=256,MaximumSelectedSamples=2304));emit(sixteen+[far]);emit(largest+[far])
 return pool,cases,base

def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--original',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
 if hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()!=EXE:raise ValueError('Executable identity before model imports')
 if hashlib.sha256(a.original.read_bytes()).hexdigest()!=ORIGINAL:raise ValueError('Sealed original-layer fixture identity')
 a.out=Path(os.path.abspath(a.out))
 if not a.out.is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output rejected')
  if not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory output ancestor')
 if a.native:
  native=json.loads(a.native.read_text());want=json.loads((a.out/'set-expected.json').read_text());assert native['Cases']==want
  pool=json.loads((a.out/'pool-reference.json').read_text());assert len(native['Pool'])==len(pool)
  for got,ref in zip(native['Pool'],pool):
   for key in ('X','Z','Stored'):assert got[key]==ref[key],key
   for av,bv in zip(got['Normals'],ref['Normals']):
    for x,y in zip(av,bv):assert abs(float(number(x))-float(Decimal(y)))<=3e-6
  capture=native['OwnedCapture'];assert capture['Checks']==256 and capture['Accepted']+capture['Rejected']==256 and capture['AllAcceptedCapturesValid'] and not capture['AtomicCallerCaptureClaimed']
  o=native['Original'];r=o['Receipt'];assert r['Count']==4 and r['SharedEdgePairs']==4 and r['SharedCornerPairs']==2 and r['SharedSampleComparisons']==22 and r['NormalBuilds']==r['GeometryBuilds']==0 and o['HeightsCompared']==o['NormalsCompared']==400
  accepted=[c for c in want if c['Reject']=='None'];receipt=dict(Passed=True,Cases=len(want),Accepted=len(accepted),Rejected=len(want)-len(accepted),PoolChunks=len(pool),OwnershipChecks=native['OwnershipChecks'],OwnedCapture=capture,SharedEdgePairs=sum(c['Receipt']['SharedEdgePairs'] for c in accepted),SharedCornerPairs=sum(c['Receipt']['SharedCornerPairs'] for c in accepted),SharedSampleComparisons=sum(c['Receipt']['SharedSampleComparisons'] for c in accepted),Original=o,OriginalFixtureSha256=ORIGINAL,NormalTolerance='3e-6',OriginalNormalParity=False,OriginalTerrainParity=False)
  (a.out/'set-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 f,n,m=load();pool,cases,base=generate(f,n,m);values=[enrich(c,f.expected(c,n,m),f,n,m) for c in pool]
 t=totals([values[i] for i in base],True);capvalues=[4,10,t['TotalCells'],t['TotalPatches'],t['SelectedSamples'],t['OwnCopyBytes'],t['OwnArrayBytes'],t['OwnWork'],t['BorrowedCopyReservation'],t['BorrowedArrayReservation'],t['BorrowedWorkReservation']];exact=dict(zip((k for k,d,h in SPECS),capvalues))
 cases.append(dict(Name=str(len(cases)),Ids=base,Edge=1,Normal=1,Limits=exact))
 for k,d,h in SPECS:
  for limits in ({k:-1},{k:h+1},dict(exact,**{k:exact[k]-1})):
   cases.append(dict(Name=str(len(cases)),Ids=base,Edge=1,Normal=0,Limits=limits))
 want=[expected(c,values) for c in cases];assert want[0]['Reject']=='None'
 (a.out/'set-fixtures.json').write_text(json.dumps(dict(Chunks=pool,Cases=cases)),encoding='utf-8');(a.out/'set-expected.json').write_text(json.dumps(want),encoding='utf-8');(a.out/'pool-reference.json').write_text(json.dumps([{k:c[k] for k in ('X','Z','Stored','Normals')} for c in values]),encoding='utf-8');print(json.dumps(dict(Cases=len(cases),Chunks=len(pool))))
if __name__=='__main__':main()
