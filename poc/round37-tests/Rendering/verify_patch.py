"""Synthetic circle -> AHCN -> exact topology oracle; no original assets or process execution."""
import argparse,importlib.util,json,struct,itertools,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('circle',ROOT/'poc/round36-tests/Rendering/verify_circle.py');c=importlib.util.module_from_spec(s);s.loader.exec_module(c);a=c.a;m=a.m
OFFSETS=[(1,1),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0),(0,0)]
RINGS=[[1,2,3,4,5,6,7,8,1],[1,2,4,5,6,7,8,1],[2,4,5,6,7,8,2],[2,3,4,5,6,7,8,2],[2,3,4,5,6,8,2],[1,2,3,4,5,6,8,1],[1,2,3,4,6,8,1],[1,2,3,4,6,7,8,1],[1,2,4,6,7,8,1]]
def pattern(n,x,y,mask):
 if y==0 and mask&4:return 4 if x==0 and mask&2 else 6 if x==n-1 and mask&8 else 5
 if x==0 and mask&2:return 2 if y==n-1 and mask&1 else 3
 if y==n-1 and mask&1:return 8 if x==n-1 and mask&8 else 1
 return 7 if x==n-1 and mask&8 else 0

def expected(f):
 heights=[];influences=[];skipped=[];outcomes=[]
 for y in f['Second']:
  for x in f['First']:
   k=len(heights);i=c.evaluate(dict(Precision=f['QueryPrecision'],CachePrecision=f['CachePrecision'],Selector=f['Selector'],Enabled=f['CircleEnabled'],Geometry=f['Geometry'],Coordinates=[x,y],Amount=f['Amount'],Version=f['Version'],Action='evaluate'))
   if i['Reject']!='None':return dict(Name=f['Name'],Reject=i['Reject'])
   w=i['Influence'];skip=i['Skipped'];influences.append(w);skipped.append(skip)
   if skip:out=f['Initial'][k];why=None
   else:
    reject,out,why=a.evaluate(f['HeightEnabled'],f['Operation'],f['Constant'],f['Initial'][k],w,f['HeightPrecision'])
    if reject!='None':return dict(Name=f['Name'],Reject=reject)
   if not a.finite(out) or abs(a.scalar(out)[0])>1000000:return dict(Name=f['Name'],Reject='Input')
   heights.append(out);outcomes.append(why)
 n=f['Cells'];width=2*n+1;patches=[]
 for y in range(n):
  for x in range(n):
   p=pattern(n,x,y,f['Mask']);source=[(2*y+dy)*width+2*x+dx for dx,dy in OFFSETS]
   patches.append(dict(Pattern=p,Source=source,Indices=[i for l,r in zip(RINGS[p],RINGS[p][1:]) for i in (0,l,r)],
    Positions=[[f['First'][i%width],heights[i],f['Second'][i//width]] for i in source],Normals=[[0,0x3f800000,0]]*9,Colors=[0xFF4080C0]*9))
 return dict(Name=f['Name'],Reject='None',Heights=heights,Influences=influences,Skipped=skipped,Outcomes=outcomes,Patches=patches)

def fixtures():
 cases=[]
 def emit(cp,qp,hp,selector,op,n=2,mask=0,version='0002',ce=1,he=1,g=None,amount=0x3f000000,constant=0x3eaaaaab,base=0x3f000001):
  axis=[c.fbits((i-n)*.5) for i in range(2*n+1)];initial=[base]*(len(axis)**2)
  f=dict(Name=str(len(cases)),CachePrecision=cp,QueryPrecision=qp,HeightPrecision=hp,Selector=selector if version=='0002' else 0,Operation=op,Cells=n,Mask=mask,Version=version,CircleEnabled=ce,HeightEnabled=he,Geometry=g or [0,0,0x3f800001],Amount=amount,Constant=constant,First=axis,Second=axis,Initial=initial)
  f['CircleHex']=c.encode(dict(Enabled=ce,Version=version,Geometry=f['Geometry'],EncodedSelector=selector,Amount=amount))
  rule=a.rule(he,op,constant)
  groups=b''.join(m.form(t,m.form(v)) for t,v in zip(m.TAGS,m.VERS))
  f['GeneratorHex']=m.form(b'TGEN',m.form(b'0000',groups+m.form(b'LYRS',m.layer(b'0001',children=rule)))).hex()
  cases.append(f)
 for cp,qp,hp in itertools.product((24,53,64),repeat=3):
  for selector in (0,1,2,3,4,0xffffffff):
   for op in (0,1,2,3,4,5,0xffffffff):emit(cp,qp,hp,selector,op,mask=(selector+op)%16)
 for mask in range(16):
  for n in (1,3):emit(24,53,64,3,1,n,mask)
 for version in ('0000','0001','0002'):
  for ce,he in itertools.product((0,1),repeat=2):emit(53,64,24,2,3,version=version,ce=ce,he=he)
 for radius in (0,0x80000000,0xbf800001):emit(64,24,53,2,0,g=[0,0,radius])
 for amount in (0,0x80000000,0xbf800000,0x3f800000,0x3f800001):emit(24,53,64,3,0,amount=amount)
 for bad in (0x7f800000,0xff800000,0x7fc12345):
  for ce in (0,1):emit(24,53,64,0,1,ce=ce,g=[bad,0,0x3f800000])
  for he in (0,1):emit(24,53,64,0,1,he=he,constant=bad)
 emit(53,53,53,0,1,constant=0x7f7fffff)
 return cases

p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',required=True);p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--native',type=Path);p.add_argument('--receipt',type=Path,required=True);args=p.parse_args()
static_circle=c.static(args.exe,args.packages);static_height=a.static(args.exe)
fs=fixtures();args.fixtures.write_text(json.dumps(dict(Cases=fs)));ex=[expected(f) for f in fs]
receipt=dict(Cases=len(fs),Accepted=sum(f['Reject']=='None' for f in ex),Rejected=sum(f['Reject']!='None' for f in ex),SyntheticOnly=True,Profiles=27,StaticCircle=static_circle,StaticHeight=static_height)
if args.native:
 native=json.loads(args.native.read_text(encoding='utf-8-sig'));assert len(native['Cases'])==len(ex)
 for want,got in zip(ex,native['Cases']):assert want==got,(want['Name'],want,got)
 assert native['OwnershipChecks']==35;receipt['OwnershipChecks']=native['OwnershipChecks'];receipt['ExactGridSamples']=sum(len(v.get('Heights',[])) for v in ex);receipt['ExactPatches']=sum(len(v.get('Patches',[])) for v in ex)
args.receipt.write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
