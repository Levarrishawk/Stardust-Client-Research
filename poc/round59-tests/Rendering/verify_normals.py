"""Independent Fraction stored-coordinate/height model and 100-digit Decimal modern normal reference."""
import argparse,hashlib,itertools,json,os,stat,struct
from decimal import Decimal,localcontext
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
POLICY='ModernBinary64CentralDifference'
def setup():
 path=ROOT/'poc/round39-tests/Rendering/verify_patch.py';ns={'__file__':str(path)}
 exec(compile(path.read_text().split('\np=argparse.ArgumentParser();')[0],str(path),'exec'),ns);return ns
def word(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def scalar(bits):return Fraction(struct.unpack('<f',struct.pack('<I',bits))[0])
def decimal(v):return Decimal(v.numerator)/Decimal(v.denominator)
def account(w):return [8*w+4*w*w,256+8*w+16*w*w,128+4*w+80*w*w]
def expected(c,m):
 try:
  source=m['expected'](c)
  if source['Reject']!='None':raise ValueError('Source')
  w=source['GridWidth'];n=w*w;a=account(w)
  spec=[('MaximumAxisSamples',32,32),('MaximumCells',1024,1024),('MaximumCopyBytes',65536,1048576),('MaximumArrayBytes',131072,2097152),('MaximumWork',262144,4194304)]
  for k,d,maximum in spec:
   if not 0<=c.get(k,d)<=maximum:raise ValueError('Limit range')
  if c.get('Policy',0)!=0 or c.get('Cancel'):raise ValueError('Policy/cancel')
  if not 2<=w<=32 or w>c.get('MaximumAxisSamples',32) or n>c.get('MaximumCells',1024):raise ValueError('Shape')
  for (k,d,_),v in zip(spec[2:],a):
   if v>c.get(k,d):raise ValueError('Preflight')
  grid=m['coord'](dict(Name=c['Name'],Request=c['Request'],Profiles=c['Profiles'],Limits=m['producer']['DEFAULT']))
  x=grid['Position']['First'];z=grid['Position']['Second'];h=source['PositionHeights'];normals=[]
  with localcontext() as context:
   context.prec=100
   for r in range(w):
    for col in range(w):
     left=max(col-1,0);right=min(col+1,w-1);prev=max(r-1,0);nxt=min(r+1,w-1)
     dx=scalar(x[right])-scalar(x[left]);dz=scalar(z[nxt])-scalar(z[prev])
     if dx<=0 or dz<=0:raise ValueError('Span')
     gx=(scalar(h[r*w+right])-scalar(h[r*w+left]))/dx;gz=(scalar(h[nxt*w+col])-scalar(h[prev*w+col]))/dz
     length=(decimal(gx*gx+gz*gz)+1).sqrt();normals.append([str(-decimal(gx)/length),str(1/length),str(-decimal(gz)/length)])
  return dict(Name=c['Name'],Reject='None',Width=w,Count=n,Policy=POLICY,OriginalNormalParity=False,X=x,Z=z,StoredHeights=h,Normals=normals,LogicalCopyBytes=a[0],LogicalArrayBytes=a[1],LogicalWork=a[2],SourceIndices=[p['Source'] for p in source['Patches']],LayerX=grid['Layer']['First'],LayerZ=grid['Layer']['Second'])
 except (ValueError,OverflowError,ZeroDivisionError,IndexError):return dict(Name=c['Name'],Reject='Input')
def generate(m):
 cases=[]
 def request(width=8,tiles=2,margin=3,first=0,second=0,scale=1):return dict(ChunkWidthBits=word(width),Tiles=tiles,Margin=margin,First=first,Second=second,Scale=scale)
 def case(req=None,profiles=(24,24,24,24),kind='plane',mask=0):
  req=req or request();count=max(0,2*(req['Tiles']+req['Margin']));count=min(count,128);size=count*count
  grid=m['coord'](dict(Name='seed',Request=req,Profiles=list(profiles),Limits=m['producer']['DEFAULT']));h=[]
  for i in range(size):
   row,col=divmod(i,count)
   if kind=='zero':bits=0x80000000 if i%2 else 0
   elif kind=='extreme':bits=word(1000000 if (row+col)%2 else -1000000)
   elif grid['Reject']=='None':
    x=scalar(grid['Position']['First'][col]);z=scalar(grid['Position']['Second'][row]);v=x/8-z/4+Fraction(1,2)
    if kind=='wave':v+=Fraction(col*col*3+row*row*row-5,64)
    bits=m['a'].bits((v,False))
   else:bits=0
   h.append(bits)
  c=dict(Name=str(len(cases)),Request=req,Profiles=list(profiles),Mask=mask,ScaleBits=word(1),Heights=h,Normals=[[0,word(1),0]]*size,Colors=[0xff4080c0]*size,Limits=dict(MaximumGridSamples=1048576,MaximumPatches=1024,MaximumSelectedSamples=9216));cases.append(c);return c
 for profiles in itertools.product((24,53,64),repeat=4):
  for kind in ('plane','wave','zero'):case(request(width=struct.unpack('<f',struct.pack('<I',0x3f800001))[0],tiles=1,margin=1,first=-3,second=4,scale=3),profiles,kind)
 for mask in range(16):case(mask=mask)
 for width in (.000008,.000001,1e-20):case(request(width=width,tiles=1,margin=1),kind='extreme')
 for kind in ('plane','wave','extreme'):case(request(width=8,tiles=8,margin=8),kind=kind)
 case(request(margin=14,tiles=3));case(request(first=200000));case(request(width=3.14159,first=10000),kind='wave')
 for key,value in (('MaximumAxisSamples',0),('MaximumAxisSamples',33),('MaximumCells',99),('MaximumCells',1025),('MaximumCopyBytes',1048577),('MaximumArrayBytes',2097153),('MaximumWork',4194305),('Policy',1),('Cancel',True)):
  c=case();c[key]=value
 c=case();a=account(10);c.update(dict(zip(('MaximumCopyBytes','MaximumArrayBytes','MaximumWork'),a)))
 for key in ('MaximumCopyBytes','MaximumArrayBytes','MaximumWork'):
  c=case();c.update(dict(zip(('MaximumCopyBytes','MaximumArrayBytes','MaximumWork'),a)));c[key]-=1
 for bits in (0x7fc12345,0x7f800000,0xff800000,word(1000001)):
  c=case();c['Heights'][-1]=bits
 for req in (request(margin=0),request(width=0),request(scale=0)):case(req)
 for first in (0,1):c=case(request(first=first));c['SeamPair']=True
 for i,c in enumerate(cases):c['Name']=str(i)
 return cases
def compare(native,e):
 assert len(native['Cases'])==len(e);vectors=upward=zero=nonuniform=axis_difference=0
 for got,want in zip(native['Cases'],e):
  assert got['Name']==want['Name'] and got['Reject']==want['Reject'],(got['Name'],got['Reject'],want['Reject'])
  if want['Reject']!='None':continue
  for k in want:
   if k not in ('Normals','LayerX','LayerZ'):assert got[k]==want[k],(got['Name'],k)
  if want['X']!=want['LayerX'] or want['Z']!=want['LayerZ']:axis_difference+=1
  if len({scalar(b)-scalar(a) for a,b in zip(want['X'],want['X'][1:])})>1:nonuniform+=1
  assert len(got['Normals'])==want['Count']
  for bits,reference in zip(got['Normals'],want['Normals']):
   v=[scalar(b) for b in bits]
   with localcontext() as context:
    context.prec=100
    for value,ref in zip(v,reference):assert abs(decimal(value)-Decimal(ref))<=Decimal('3e-6'),(got['Name'],value,ref)
   assert abs(sum(float(x*x) for x in v)-1)<=3e-6 and v[1]>0
   upward+=1;zero+=sum((b&0x7fffffff)==0 and b>>31 for b in bits);vectors+=1
 assert axis_difference>0 and nonuniform>0 and zero>0
 return dict(Vectors=vectors,UpwardUnitVectors=upward,NegativeZeroComponents=zero,NonuniformAxisReceipts=nonuniform,PositionLayerAxisDifferenceReceipts=axis_difference)
def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
 if hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()!=EXE:raise ValueError('Executable identity before imports')
 a.out=Path(os.path.abspath(a.out))
 if not a.out.is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output rejected')
  if not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory output ancestor')
 m=setup()
 if a.native:
  e=json.loads((a.out/'normal-expected.json').read_text());native=json.loads(a.native.read_text());counts=compare(native,e)
  receipt=dict(Passed=True,Cases=len(e),Accepted=sum(c['Reject']=='None' for c in e),Rejected=sum(c['Reject']!='None' for c in e),DecimalPrecision=100,AbsoluteF32VectorTolerance='3e-6',Policy=POLICY,OriginalNormalParity=False,OwnershipChecks=native['OwnershipChecks'],ActivePaddedSeamChecks=native['ActivePaddedSeamChecks'],**counts)
  (a.out/'normal-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 cases=generate(m);e=[expected(c,m) for c in cases];assert e[0]['Reject']=='None'
 (a.out/'normal-fixtures.json').write_text(json.dumps(dict(Cases=cases)),encoding='utf-8');(a.out/'normal-expected.json').write_text(json.dumps(e),encoding='utf-8');print(json.dumps(dict(Cases=len(e))))
if __name__=='__main__':main()
