"""BCIR finite-domain Fraction/isqrt oracle, synthetic fixtures, hash-first static checks."""
import argparse,importlib.util,json,math,random,struct,sys,hashlib
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('ahcn_oracle',ROOT/'poc/round34-tests/Assets/verify_ahcn_operation.py');a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
EXPECTED='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
def sqrt_round(x,p):
 v,z=x
 if v<0:raise ValueError('negative sqrt')
 if not v:return x
 quantum=Fraction(2)**(a.floor(v)//2-p+1)
 scaled=v/(quantum*quantum);q=math.isqrt(scaled.numerator//scaled.denominator)
 midpoint=Fraction(2*q+1,2)**2
 if scaled>midpoint or (scaled==midpoint and q&1):q+=1
 return q*quantum,False

def evaluate(f,spill_before_shape=False):
 p=f['Precision'];cache_p=f['CachePrecision'];sel=f['Selector'];active=f['Enabled'];geometry=f['Geometry'];xy=f['Coordinates']
 if p not in (24,53,64) or cache_p not in (24,53,64):return {'Reject':'Options'}
 if f['Action']=='evaluate' and not active:return {'Reject':'None','Skipped':True,'Influence':None,'Query':None,'RadiusSquared':None,'DistanceSquared':None,'Amount':None,'Branch':None}
 if not all(a.finite(x) for x in geometry+xy):return {'Reject':'Input'}
 amount=f['Amount'] if f['Version']=='0002' else 0
 if not a.finite(amount):return {'Reject':'Input'}
 av=a.scalar(amount)
 if av[0]<0:amount=0
 elif av[0]>1:amount=0x3f800000
 av=a.scalar(amount);c1,c2,r=map(a.scalar,geometry);x,y=map(a.scalar,xy)
 add=lambda x,y:a.precision(a.add(x,y),p)
 sub=lambda x,y:add(x,a.neg(y))
 mul=lambda x,y:a.precision(a.mul(x,y),p)
 div=lambda x,y:a.precision((x[0]/y[0],False),p)
 rs=a.bits(a.precision(a.mul(r,r),cache_p))
 if not a.finite(rs):return {'Reject':'Input'}
 dx,dy=sub(c1,x),sub(c2,y)
 ds=a.bits(add(mul(dy,dy),mul(dx,dx)))
 if not a.finite(ds):return {'Reject':'Input'}
 d,cache=a.scalar(ds),a.scalar(rs);one=a.scalar(0x3f800000)
 if d[0]>cache[0]:u=a.scalar(0);branch=0
 else:
  inner=mul(sub(one,av),r);sq=mul(inner,inner)
  if d[0]<=sq[0]:u=one;branch=1
  else:u=sub(one,div(sub(d,sq),sub(cache,sq)));branch=2
 projection=a.bits(u)
 if spill_before_shape:u=a.scalar(projection)
 if sel==0:shaped=u
 elif sel==1:shaped=mul(u,u)
 elif sel==2:shaped=sqrt_round(u,p)
 elif sel==3:shaped=mul(mul(sub(a.scalar(0x40400000),add(u,u)),u),u)
 else:shaped=a.scalar(0)
 return dict(Reject='None',Skipped=False,Influence=a.bits(shaped),Query=projection,RadiusSquared=rs,DistanceSquared=ds,Amount=amount,Branch=branch)

def form(tag,children):return b'FORM'+struct.pack('>I',len(children)+4)+tag.encode()+children
def chunk(tag,b):return tag.encode()+struct.pack('>I',len(b))+b
def encode(f):
 h=form('IHDR',form('0001',chunk('DATA',struct.pack('<I',f['Enabled'])+b'synthetic\0')))
 words=f['Geometry']
 if f['Version']=='0001':words=[0xdeadbeef]+words
 elif f['Version']=='0002':words=words+[f['EncodedSelector'],f['Amount']]
 return form('BCIR',form(f['Version'],h+chunk('DATA',struct.pack('<'+'I'*len(words),*words)))).hex()
def fbits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def fixtures():
 out=[]
 def emit(g,xy,amount=0,selector=0,enabled=1,version='0002',action='evaluate',precision=24,cache_precision=None):
  f=dict(Name=str(len(out)),Geometry=list(g),Coordinates=list(xy),Amount=amount,Selector=selector,EncodedSelector=selector,Enabled=enabled,Version=version,Action=action,Precision=precision,CachePrecision=precision if cache_precision is None else cache_precision)
  if action=='query':f['EncodedSelector']=0
  if version!='0002' and action=='evaluate':f['Selector']=0
  f['RuleHex']=encode(f);f['Expected']=evaluate(f);out.append(f)
 for p in (24,53,64):
  for r in (0,0x80000000,1,0x00800000,0x3f000001,0x3f800000,0xbf800000,0x40000000,0x41000000,0x5f000000,0x7f7fffff):
   for x in (0,1,0x3f000000,0x3f7fffff,0x3f800000,0x3f800001,r):
    for amount in (0,0x80000000,0x3e800000,0x3f000000,0x3f7fffff,0x3f800000,0x3f800001,0xbf800000):
     for sel in (0,1,2,3,4,0xffffffff):emit((0,0,r),(x,0),amount,sel,precision=p)
  for version in ('0000','0001','0002'):
   for enabled in (0,1,0xffffffff):
    for sel in (0,2,0x80000000):emit((0x3f800000,0xbf800000,0xc0000000),(0x3fc00000,0xbf000000),0x3f000000,sel,enabled,version,precision=p)
  for bad in (0x7f800000,0xff800000,0x7fc12345,0x7f800001):
   for enabled in (0,1):
    for position in range(6):
     g=[0,0,0x3f800000];xy=[0,0];amt=0x3f000000
     if position<3:g[position]=bad
     elif position<5:xy[position-3]=bad
     else:amt=bad
     emit(g,xy,amt,2,enabled,precision=p)
   emit((bad,0,0x3f800000),(0,0),0,2,0,action='query',precision=p)
 for p in (0,23,25,52,54,63,65):emit((0,0,0x3f800000),(0,0),enabled=0,precision=p)
 rng=random.Random(3601)
 for i in range(300):
  radius=fbits(2**rng.randint(-70,60)*rng.uniform(.5,2));rv=struct.unpack('<f',struct.pack('<I',radius))[0]
  g=[fbits(rng.uniform(-3,3)*rv),fbits(rng.uniform(-3,3)*rv),radius]
  xy=[fbits(struct.unpack('<f',struct.pack('<I',g[j]))[0]+rng.uniform(-1.05,1.05)*rv) for j in (0,1)]
  amt=fbits(rng.random())
  for p in (24,53,64):
   for sel in (0,1,2,3):emit(g,xy,amt,sel,precision=p)
 # Cross-phase subnormal cache inputs: seek differing loader stores independently.
 cache_rng=random.Random(3602);sensitive=[]
 for i in range(20000):
  raw=(62<<23)|cache_rng.randrange(1<<23);r=a.scalar(raw)
  cache24=a.bits(a.precision(a.mul(r,r),24));cache53=a.bits(a.precision(a.mul(r,r),53))
  if cache24!=cache53:sensitive.append(raw)
  if len(sensitive)>=12:break
 assert sensitive, 'Cross-phase cache sensitivity vectors missing.'
 for radius in sensitive:
  for cp in (24,53,64):
   for qp in (24,53,64):
    for amt in (0,0x3f000000,0x3f800000):
     for sel in (0,2,3):emit((0,0,radius),(radius,0),amt,sel,precision=qp,cache_precision=cp)
 for cp in (0,23,65):emit((0,0,0x3f800000),(0,0),enabled=0,cache_precision=cp)
 # Direct Query ignores disabled header; explicit Shape selector can differ from encoded selector.
 for p in (24,53,64):
  for sel in (0,1,2,3,0xffffffff):emit((0,0,0x3f800000),(0x3f400000,0),0x3f000000,sel,0,action='query',precision=p)
 return out

def sqrt_fixtures():
 out=[]
 for p in (24,53,64):
  for raw in [0,0x80000000,1,2,0x7fffff]+[(e<<23)|fraction for e in range(1,255) for fraction in (0,1,0x3fffff,0x7fffff)]:
   value,z=a.scalar(raw);answer=sqrt_round((value,z),p)
   out.append(dict(Precision=p,RawBits=raw,Mantissa=None,Exponent=None,Expected=dict(Numerator=str(answer[0].numerator),Denominator=str(answer[0].denominator),NegativeZero=answer[1],Bits=a.bits(answer))))
  for parity in (0,1):
   q=(1<<(p-1))+parity;mantissa=(2*q+1)**2;exponent=-2*p
   value=Fraction(mantissa)*Fraction(2)**exponent;answer=sqrt_round((value,False),p)
   out.append(dict(Precision=p,RawBits=None,Mantissa=str(mantissa),Exponent=exponent,Expected=dict(Numerator=str(answer[0].numerator),Denominator=str(answer[0].denominator),NegativeZero=False,Bits=a.bits(answer))))
 return out

def static(exe,packages):
 raw=Path(exe).read_bytes()
 if hashlib.sha256(raw).hexdigest()!=EXPECTED:raise SystemExit('Executable identity mismatch; decoding not started.')
 sys.path.insert(0,packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=raw,fast_load=True);b=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 anchors={0xb63333:('mov','dword ptr [edx + 0x14], eax'),0xb63336:('mov','dword ptr [edx + 0x18], eax'),0xb65fc6:('mov','dword ptr [ecx + 0x18], eax'),0xb726d2:('fmul','st(1)'),0xb726db:('fstp','dword ptr [edi + 0x28]'),0xb7277f:('fmul','st(1)'),0xb72788:('fstp','dword ptr [edi + 0x28]'),0xb72818:('mov','dword ptr [esi + 0x24], edx'),0xb723cc:('fabs',''),0xb72436:('fsub','dword ptr [ebp + 8]'),0xb7243c:('fsub','dword ptr [ebp + 0xc]'),0xb72441:('fmul','st(1)'),0xb72445:('fmul','st(3)'),0xb72447:('faddp','st(1)'),0xb72449:('fstp','dword ptr [ebp + 8]'),0xb72453:('fcomp','dword ptr [ecx + 0x28]'),0xb72458:('test','ah, 0x41'),0xb7245b:('jne','0xb72467'),0xb7246a:('fsubr','dword ptr [0x15dc6f8]'),0xb72470:('fmul','dword ptr [ecx + 0x24]'),0xb72475:('fmulp','st(1)'),0xb7247a:('fcomp','st(1)'),0xb72481:('jp','0xb7248f'),0xb72492:('fsub','st(1)'),0xb72497:('fsub','st(2)'),0xb72499:('fdivp','st(1)'),0xb7249b:('fsubr','dword ptr [0x15dc6f8]'),0xb724a1:('fstp','st(1)'),0xb72822:('fmul','st(1)'),0xb7282a:('fstp','dword ptr [esi + 0x28]'),0xb7285a:('jp','0xb72865'),0xb72873:('jne','0xb7287e'),0xb7288c:('call','dword ptr [edx + 0x10]'),0xb63f8e:('call','0xb62f20'),0xb63f95:('je','0xb64010'),0xb63faf:('call','dword ptr [edx + 0x24]'),0xb63fb2:('cmp','edi, 3'),0xb63fb5:('ja','0xb63fd8'),0xb63fc0:('fmulp','st(1)'),0xb63fc4:('fsqrt',''),0xb63fca:('fadd','st(1)'),0xb63fcc:('fsubr','dword ptr [0x15dd850]'),0xb63fd2:('fmul','st(1)'),0xb63fd4:('fmulp','st(1)'),0xb63fda:('fld','dword ptr [0x15dc6a0]'),0xb63fe0:('fst','dword ptr [ebp - 0x34]')}
 for va,pair in anchors.items():
  i=next(md.disasm(b[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==pair
 query=list(md.disasm(b[0xb72430-base:0xb724a7-base],0xb72430));assert sum(i.mnemonic=='fstp' and 'dword' in i.op_str for i in query)==1 and not any(i.mnemonic=='fsqrt' for i in query)
 assert struct.unpack_from('<4I',b,0xb642a0-base)==(0xb63fe0,0xb63fbe,0xb63fc4,0xb63fc8)
 for va,value in [(0x15dc6a0,0),(0x15dc6f8,0x3f800000),(0x15dd850,0x40400000)]:assert struct.unpack_from('<I',b,va-base)[0]==value
 assert struct.unpack_from('<I',b,0x15ed3a0-base)[0]==0xb72430
 return dict(sha256=EXPECTED,anchors=len(anchors),selectorTable=4,constants=3,activeRuntimePrecision='Unproven')

if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('--exe',required=True);ap.add_argument('--packages',required=True);ap.add_argument('--fixtures',required=True);ap.add_argument('--receipt',required=True);ap.add_argument('--native');args=ap.parse_args()
 st=static(args.exe,args.packages);fs=fixtures();sq=sqrt_fixtures();Path(args.fixtures).write_text(json.dumps(dict(Cases=fs,SquareRoots=sq),separators=(',',':'))+'\n')
 result=dict(static=st,squareRootVectors=len(sq),vectors=len(fs),rejections=sum(f['Expected']['Reject']!='None' for f in fs),sqrtModel='Fraction/isqrt exact midpoint rounding')
 if args.native:
  native=json.loads(Path(args.native).read_text());assert len(native['Cases'])==len(fs)
  for f,n in zip(fs,native['Cases']):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  assert native['SquareRoots']==[v['Expected'] for v in sq]
  assert native['BatchChecks']>=14 and native['ArgumentNegatives']>=4;result['native']=dict(BatchChecks=native['BatchChecks'],ArgumentNegatives=native['ArgumentNegatives'])
  groups={}
  for f in fs:
   if f['Expected']['Reject']=='None' and not f['Expected']['Skipped']:groups.setdefault((tuple(f['Geometry']),tuple(f['Coordinates']),f['Amount'],f['Selector'],f['CachePrecision']),set()).add(f['Expected']['Influence'])
  result['precisionSensitiveInputs']=sum(len(v)>1 for v in groups.values());assert result['precisionSensitiveInputs']>0
  phase_groups={}
  for f in fs:
   if f['Expected']['Reject']=='None' and not f['Expected']['Skipped']:phase_groups.setdefault((tuple(f['Geometry']),tuple(f['Coordinates']),f['Amount'],f['Selector'],f['Precision']),set()).add(f['Expected']['Influence'])
  result['cachePhaseSensitiveInputs']=sum(len(v)>1 for v in phase_groups.values());assert result['cachePhaseSensitiveInputs']>0
  result['unspilledShapingSensitiveCases']=sum(f['Expected']['Reject']=='None' and not f['Expected']['Skipped'] and evaluate(f,True)['Influence']!=f['Expected']['Influence'] for f in fs);assert result['unspilledShapingSensitiveCases']>0
 Path(args.receipt).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
