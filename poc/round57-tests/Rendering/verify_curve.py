"""Exact Fraction kernel/base and independent 100-digit Decimal ln/exp oracle; modern policy only."""
import argparse,hashlib,importlib.util,json,math,os,stat,struct,sys
from decimal import Decimal,localcontext
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
POLICY='ModernBinary64LogPowApproximation'
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def word(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def f64(bits):
 sign=-1 if bits>>63 else 1;e=(bits>>52)&2047;m=bits&((1<<52)-1)
 if e==2047:raise ValueError('Nonfinite binary64')
 if e:m|=1<<52;e-=1075
 else:e=-1074
 return sign*Fraction(m)*(Fraction(2)**e)
def dyadic(v):
 if not v:return 0,0
 m=v.numerator;e=-(v.denominator.bit_length()-1)
 while m%2==0:m//=2;e+=1
 return m,e
def dec(v):return Decimal(v.numerator)/Decimal(v.denominator)
def account(c):
 count=c['Width']*c['Rows'];return [696+176*count,10088+1632*count,20424+8289*count,1456*count,8257*count]
def expected(c,mode):
 try:
  width,rows=c['Width'],c['Rows'];count=width*rows
  spec=[('MaximumSamples',256,1024),('MaximumInputBytes',84,65536),('MaximumCopyBytes',1048576,4194304),('MaximumRouteBytes',4194304,16777216),('MaximumWork',16777216,67108864),('MaximumQueryBytes',2097152,2097152),('MaximumQueryWork',16777216,16777216)]
  for k,d,maximum in spec:
   if not 0<=c.get(k,d)<=maximum:raise ValueError('Limit policy')
  if not(1<=width<=128 and 1<=rows<=128 and count<=c.get('MaximumSamples',256) and len(c['Requests'])==count):raise ValueError('Shape')
  raw=bytes.fromhex(c['Hex'])
  if len(raw)!=84 or len(raw)>c.get('MaximumInputBytes',84):raise ValueError('Raw extent')
  a=account(c)
  for (key,default,_),required in zip(spec[2:],a):
   if required>c.get(key,default):raise ValueError('Preflight')
  if c.get('Cancel') or c.get('Policy',0)!=0:raise ValueError('Policy/cancellation')
  for key in ('Table','Construction','Query'):mode.n.arithmetic(c[key])
  for i,r in enumerate(c['Requests']):
   if r['Column']!=i%width or r['Row']!=i//width:raise ValueError('Unique row major')
   mode.n.finite_scalar(r['XBits']);mode.n.finite_scalar(r['ZBits'])
  words=list(struct.unpack_from('<13I',raw,32))
  if raw!=mode.b.frame(words):raise ValueError('Envelope')
  parameter=mode.n.finite_scalar(words[2])[0]
  if not(words[1] and not words[3] and 0<parameter<1):raise ValueError('Curve domain')
  derived=bytearray(raw);derived[36:40]=bytes(4);dw=list(words);dw[1]=0
  assert bytes(derived)==mode.b.frame(dw)
  model=mode.Model(dict(Hex=derived.hex(),Width=width,Rows=rows,TablePrecision=c['Table'],ConstructionPrecision=c['Construction']))
  cells=[];powers=0
  with localcontext() as context:
   context.prec=100;exponent=dec(parameter).ln()/Decimal('.5').ln()
   for i,r in enumerate(c['Requests']):
    retained=model.miss(r,c['Query']);basis=mode.b.stored(retained);base=mode.n.finite_scalar(basis)[0]
    if not 0<=base<=1:raise ValueError('Base domain')
    if base==0:out=Decimal(0)
    elif base==1:out=Decimal(1)
    else:out=(dec(base).ln()*exponent).exp();powers+=1
    # Independent correctly rounded Decimal-to-binary64 conversion classifies representability only.
    approximate=struct.unpack('<Q',struct.pack('<d',float(out)))[0];_,power=dyadic(f64(approximate))
    if not -512<=power<=512:raise ValueError('Frozen exact-export exponent')
    cells.append(dict(Index=i,**r,Kernel=str(retained[0]),KernelNegative=retained[1],Base=basis,ExpectedOutput=str(out),ExpectedExponent=str(exponent)))
  octaves=words[5]
  return dict(Name=c['Name'],Reject='None',OriginalSha256=sha(raw),DerivedSha256=sha(derived),OriginalHex=raw.hex().upper(),DerivedHex=derived.hex().upper(),Cells=cells,Counts=[1,1,1,count,count*octaves,count,2,powers,count-powers],LogicalCopyBytes=a[0],ReservedRouteBytes=a[1],ReservedWork=a[2],ReservedQueryBytes=a[3],ReservedQueryWork=a[4],AccountedRouteBytes=10088+count*(352+80*octaves),AccountedWork=20424+count*(97+512*octaves))
 except (ValueError,ZeroDivisionError,OverflowError,IndexError) as error:return dict(Name=c['Name'],Reject='Input',Reason=str(error))
def generate(mode,selected):
 base=[47,1,word(.5),0,0x7fc12345,3,word(1.5),word(.35),word(1),word(1),0,0,0]
 def requests(width=2,rows=2):return [dict(Column=i%width,Row=i//width,XBits=word((i%width)*.25),ZBits=word((i//width)*.375)) for i in range(width*rows)]
 def case(words=base,**kwargs):return dict(Hex=mode.b.frame(words).hex(),Width=2,Rows=2,Requests=requests(),Table=53,Construction=53,Query=53,**kwargs)
 cases=[]
 for m in range(6):
  w=list(base);w[12]=m
  for tp in (24,53,64):
   for cp in (24,53,64):
    for qp in (24,53,64):
     c=case(w);c.update(Table=tp,Construction=cp,Query=qp);cases.append(c)
 for m in range(6):
  for param in (word(.25),word(.75),word(.99),1,0x3f7fffff):
   w=list(base);w[12]=m;w[2]=param;w[1]=256;cases.append(case(w))
 # Explicit real serialized family; no curve-disabling occurs outside the owned batch.
 cases.append(case(selected['Words'],OriginalOccurrence=True))
 cap=case();cap.update(dict(zip(('MaximumCopyBytes','MaximumRouteBytes','MaximumWork','MaximumQueryBytes','MaximumQueryWork'),account(cap))));cases.append(cap)
 for key in ('MaximumCopyBytes','MaximumRouteBytes','MaximumWork','MaximumQueryBytes','MaximumQueryWork'):
  c=dict(cap);c[key]-=1;cases.append(c)
 for index,value in ((1,0),(3,1),(3,256),(2,0),(2,0x80000000),(2,word(1)),(2,word(-.5)),(2,0x7f800000),(2,0x7fc12345),(0,0x7fffffff),(0,0),(5,0),(5,17),(6,0x7f800000),(7,0x7fc12345),(8,0x7f800000),(9,0xff800000),(10,0x7fc12345),(11,0x7f800000),(12,6)):
  w=list(base);w[index]=value;cases.append(case(w))
 for key,value in (('Width',0),('Width',129),('Rows',0),('MaximumSamples',3),('MaximumSamples',1025),('MaximumInputBytes',83),('MaximumInputBytes',65537),('MaximumCopyBytes',4194305),('MaximumRouteBytes',16777217),('MaximumWork',67108865),('MaximumQueryBytes',2097153),('MaximumQueryWork',16777217),('Table',23),('Construction',65),('Query',0),('Policy',1),('Cancel',True)):
  c=case();c[key]=value;cases.append(c)
 for action in ('duplicate','missing','nonfinite','late'):
  c=case();r=c['Requests']
  if action=='duplicate':r[-1]=dict(r[0])
  elif action=='missing':r.pop()
  else:r[-1]['XBits']=0x7fc12345 if action=='nonfinite' else 0x7f7fffff
  cases.append(c)
 for action in ('tag','version','size','truncated','trailing'):
  c=case();raw=bytearray.fromhex(c['Hex'])
  if action=='tag':raw[0]^=1
  elif action=='version':raw[20:24]=b'0000'
  elif action=='size':raw[7]^=1
  elif action=='truncated':raw=raw[:-1]
  else:raw+=b'\0'
  c['Hex']=raw.hex();cases.append(c)
 # Kernel normalization rejection; negative-amplitude domain failures found with exact independent model.
 w=list(base);w[5]=2;w[7]=word(-1);cases.append(case(w))
 w=list(base);w[12]=3;w[2]=1;c=case(w)
 c['Requests']=[dict(Column=i%2,Row=i//2,XBits=word((i%2)*.00001),ZBits=word((i//2)*.00001)) for i in range(4)]
 cases.append(c)
 w=list(base);w[5]=2;w[6]=word(2);w[7]=word(-2);w[12]=3;dw=list(w);dw[1]=0
 model=mode.Model(dict(Hex=mode.b.frame(dw).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53))
 outside=[]
 for x in range(1,16):
  r=dict(Column=1,Row=1,XBits=word(x/16),ZBits=word(.375));v=mode.n.finite_scalar(mode.b.stored(model.miss(r,53)))[0]
  if v<0 or v>1:outside.append(r)
 assert outside,'Authored domain rejection witness missing'
 c=case(w);c['Requests']=[dict(Column=i%2,Row=i//2,XBits=0,ZBits=0) for i in range(4)];c['Requests'][-1]=outside[0];cases.append(c)
 opposite=list(w);opposite[12]=2;c2=case(opposite);c2['Requests']=[dict(r) for r in c['Requests']]
 inv=mode.Model(dict(Hex=mode.b.frame([opposite[0],0,*opposite[2:]]).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53))
 v1=mode.n.finite_scalar(mode.b.stored(model.miss(outside[0],53)))[0];v2=mode.n.finite_scalar(mode.b.stored(inv.miss(outside[0],53)))[0]
 assert (v1<0 and v2>1) or (v1>1 and v2<0),'Both modern base-domain ends required'
 cases.append(c2)
 # Largest admitted shape uses one octave to keep the risk slice bounded.
 w=list(base);w[5]=1;c=case(w);c.update(Width=128,Rows=8,Requests=requests(128,8),MaximumSamples=1024);cases.append(c)
 for i,c in enumerate(cases):c['Name']=str(i)
 return cases
def close(actual,expected):
 with localcontext() as context:
  context.prec=100;want=Decimal(expected);observed=dec(actual)
  assert abs(observed-want)<=abs(want)*Decimal('1e-12')+Decimal('1e-15'),(observed,want)
def compare(native,expected,mode):
 assert len(native['Cases'])==len(expected)
 outputs=0;endpoints=0;underflows=0
 for got,want in zip(native['Cases'],expected):
  assert got['Name']==want['Name'] and got['Reject']==want['Reject'],(got['Name'],got['Reject'],want['Reject'])
  if want['Reject']!='None':continue
  for key in want:
   if key not in ('Cells',):assert got[key]==want[key],(got['Name'],key,got[key],want[key])
  assert len(got['Cells'])==len(want['Cells'])
  for a,b in zip(got['Cells'],want['Cells']):
   for key in ('Index','Column','Row','XBits','ZBits','KernelNegative','Base'):assert a[key]==b[key],(got['Name'],key)
   assert Fraction(a['Kernel'])==Fraction(b['Kernel'])
   assert a['Policy']==POLICY and not a['OriginalNumericParity'] and a['Provenance']=='SuppliedWideMiss'
   output=f64(a['OutputBits']);exponent=f64(a['ExponentBits']);close(output,b['ExpectedOutput']);close(exponent,b['ExpectedExponent'])
   if output==0:assert a['OutputBits']==0,'Modern zero policy is positive zero'
   magnitude,power=dyadic(output);assert [a['Magnitude'],a['Exponent']]==[magnitude,power]
   assert Fraction(a['Converted'])==output and a['Projection']==mode.b.stored((output,False))
   if a['Base']&0x7fffffff in (0,word(1)):
    assert output==mode.n.finite_scalar(a['Base'])[0];endpoints+=1
   elif output==0:
    assert float(Decimal(b['ExpectedOutput']))==0,'Nonzero representable oracle output silently lost'
    underflows+=1
   outputs+=1
 return dict(Outputs=outputs,EndpointOutputs=endpoints,ModernTrueUnderflows=underflows)
def main():
 p=argparse.ArgumentParser()
 for key in ('exe','asset-root','config','out'):p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args()
 if sha(a.exe.read_bytes())!=EXE:raise ValueError('Executable identity rejected before model imports')
 a.out=Path(os.path.abspath(a.out))
 if not a.out.is_relative_to(ROOT/'local-private'):raise ValueError('Private output required')
 for ancestor in (a.out,*a.out.parents):
  try:info=ancestor.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(info.st_mode) or getattr(info,'st_file_attributes',0)&0x400:raise ValueError('Linked output rejected')
  if not stat.S_ISDIR(info.st_mode):raise ValueError('Non-directory output ancestor')
 if not a.out.resolve().is_relative_to((ROOT/'local-private').resolve()):raise ValueError('Private output containment required')
 mode=load('frozen_modes50_curve57','poc/round50-tests/Rendering/verify_modes.py');mode.n.setup()
 if a.native:
  d=json.loads((a.out/'curve-fixtures.json').read_text());e=json.loads((a.out/'curve-expected.json').read_text());native=json.loads(a.native.read_text());counts=compare(native,e,mode)
  assert counts['ModernTrueUnderflows']>0
  reasons=[x.get('Reason','') for x in e];assert 'Frozen exact-export exponent' in reasons and 'Base domain' in reasons
  receipt=dict(Passed=True,Cases=len(e),Rejected=sum(c['Reject']!='None' for c in e),Accepted=sum(c['Reject']=='None' for c in e),OwnershipChecks=native['OwnershipChecks'],ExactExportRejections=reasons.count('Frozen exact-export exponent'),BaseDomainRejections=reasons.count('Base domain'),DecimalPrecision=100,RelativeTolerance='1e-12',AbsoluteTolerance='1e-15',Policy=POLICY,OriginalNumericParity=False,Selected=d['Selected'],**counts)
  (a.out/'curve-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt));return
 selector=load('curve_frozen56_selector','tools/python/verify_terrain_fractal_curve_finish.py');meta,private=selector.selected(a.asset_root,a.config)
 cases=generate(mode,private);e=[expected(c,mode) for c in cases]
 assert e[0]['Reject']=='None' and e[192]['Reject']=='None','Original first-curve approximation must be admitted'
 (a.out/'curve-fixtures.json').write_text(json.dumps(dict(Selected=meta,Cases=cases)),encoding='utf-8');(a.out/'curve-expected.json').write_text(json.dumps(e),encoding='utf-8')
 print(json.dumps(dict(Cases=len(e),Rejects=sum(c['Reject']!='None' for c in e),Selected=meta)))
if __name__=='__main__':main()
