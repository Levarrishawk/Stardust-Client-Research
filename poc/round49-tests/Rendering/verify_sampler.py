"""Independent conditional mode0 composition of frozen46 stores and frozen47 noise."""
import argparse,importlib.util,json,struct,sys
from pathlib import Path
from fractions import Fraction
ROOT=Path(__file__).resolve().parents[3]
def load(name,relative):
 s=importlib.util.spec_from_file_location(name,ROOT/relative);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
n=load('conditional_seed','tools/python/verify_terrain_fractal_seed_noise.py')
def frame(words):return b'FORM'+struct.pack('>I',76)+b'MFRCFORM'+struct.pack('>I',64)+b'0001DATA'+struct.pack('>I',52)+struct.pack('<13I',*words)
def stored(v):
 b=n.store(v)
 if b&0x7f800000==0x7f800000:raise ValueError('Finite F32 store')
 return b
class Model:
 def __init__(self,c):
  self.c=c;self.words=n.parse(bytes.fromhex(c['Hex']));w=self.words;tp=c['TablePrecision'];cp=c['ConstructionPrecision'];n.arithmetic(tp);add,mul,div=n.arithmetic(cp)
  width=c['Width'];rows=c['Rows'];octaves=n.signed(w[5]);seed=n.signed(w[0]);self.octaves=octaves
  if not(1<=width<=128 and 1<=rows<=128 and width*rows<=1024 and 1<=octaves<=16) or w[12]!=0 or w[1]!=0 or w[3]!=0 or c.get('Sine',False) or seed==2147483647:raise ValueError('Modern eligibility')
  for i in range(6,12):n.finite_scalar(w[i])
  if not 9512+16*width*rows+136<=c.get('ByteBudget',1048576)<=1048576 or not 20000+16*octaves<=c.get('WorkBudget',1048576)<=1048576:raise ValueError('Factory budget')
  if c.get('Cancel',False):raise ValueError('Cancellation')
  weight=n.exact_int(1);total=n.exact_int(0)
  for _ in range(octaves):total=add(total,weight);weight=mul(weight,n.finite_scalar(w[7]))
  summed=stored(total);self.norm=stored(div(n.exact_int(1),n.finite_scalar(summed)));n.tables(w[0],tp)
  self.cache=[dict(Occupied=False,XBits=0,ZBits=0,ValueBits=0) for _ in range(width*rows)]
 def miss(self,r,p):
  w=self.words;add,mul,_=n.arithmetic(p)
  x=stored(mul(n.finite_scalar(r['XBits']),n.finite_scalar(w[8])));z=stored(mul(n.finite_scalar(r['ZBits']),n.finite_scalar(w[9])))
  z=stored(add(n.finite_scalar(z),n.finite_scalar(w[11])));x=stored(add(n.finite_scalar(x),n.finite_scalar(w[10])))
  frequency=amplitude=0x3f800000;total=0
  for _ in range(self.octaves):
   nz=stored(mul(n.finite_scalar(z),n.finite_scalar(frequency)));nx=stored(mul(n.finite_scalar(x),n.finite_scalar(frequency)))
   noise,_=n.noise(nx,nz,w[0],self.c['TablePrecision'],p)
   total=stored(add(mul(noise,n.finite_scalar(amplitude)),n.finite_scalar(total)))
   frequency=stored(mul(n.finite_scalar(frequency),n.finite_scalar(w[6])));amplitude=stored(mul(n.finite_scalar(amplitude),n.finite_scalar(w[7])))
  return mul(add(mul(n.finite_scalar(total),n.finite_scalar(self.norm)),n.exact_int(1)),n.finite_scalar(0x3f000000))
 def query(self,a):
  p=a['Precision'];add,_,_=n.arithmetic(p);rs=a['Requests'];count=len(rs)
  if count>1024 or not len(self.cache)*16+count*(160+self.octaves*80)<=a.get('ByteBudget',2097152)<=2097152 or not len(self.cache)+count*(self.octaves*512+64)<=a.get('WorkBudget',16777216)<=16777216 or a.get('Cancel',False):raise ValueError('Batch caps/cancellation')
  cache=[dict(c) for c in self.cache];results=[];e=n.finite_scalar(0x3727c5ac)
  for r in rs:
   col,row=r['Column'],r['Row'];x,z=n.finite_scalar(r['XBits']),n.finite_scalar(r['ZBits'])
   if not(0<=col<self.c['Width'] and 0<=row<self.c['Rows']):raise ValueError('Cell index')
   index=row*self.c['Width']+col;c=cache[index]
   hit=c['Occupied'] and add(n.finite_scalar(c['XBits']),n.negate(e))[0]<=x[0]<=add(n.finite_scalar(c['XBits']),e)[0] and add(n.finite_scalar(c['ZBits']),n.negate(e))[0]<=z[0]<=add(n.finite_scalar(c['ZBits']),e)[0]
   value=n.finite_scalar(c['ValueBits']) if hit else self.miss(r,p)
   projected=stored(value)
   if not hit:cache[index]=dict(Occupied=True,XBits=r['XBits'],ZBits=r['ZBits'],ValueBits=projected)
   results.append(dict(Origin='CacheReloadF32' if hit else 'ConditionalComputedMiss',Retained=n.fraction_text(value),NegativeZero=value[0]==0 and value[1],Projection=projected))
  self.cache=cache;return results

def expected(c):
 try:model=Model(c)
 except (ValueError,ZeroDivisionError,OverflowError,IndexError):return dict(Name=c['Name'],Reject='Input')
 actions=[]
 for a in c['Actions']:
  try:
   if a['Kind']=='Clear':
    if a.get('Cancel',False):raise ValueError('Cancelled clear')
    model.cache=[dict(Occupied=False,XBits=0,ZBits=0,ValueBits=0) for _ in model.cache];results=[]
   else:results=model.query(a)
   actions.append(dict(Reject='None',Results=results,Cache=[dict(v) for v in model.cache]))
  except (ValueError,ZeroDivisionError,OverflowError,IndexError):actions.append(dict(Reject='Input',Results=[],Cache=[dict(v) for v in model.cache]))
 return dict(Name=c['Name'],Reject='None',Normalization=model.norm,Actions=actions)
def generate(args):
 n.gate(args.exe.read_bytes(),n.EXE);n.setup();occ,raw=n.selected(args.asset_root,args.config,args.out)
 cases=[];base=[47,0,0x7fc12345,0,0x7f800000,3,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0]
 def request(x,z,col=0,row=0):return dict(Column=col,Row=row,XBits=x,ZBits=z)
 def query(points,p,**kw):return dict(Kind='Query',Precision=p,Requests=points,**kw)
 coords=[(0,0),(0x3f000001,0x3e800001),(0xc5800800,0xc5801000),(0x3f800001,0xbf800001),(1,0x80000001),(0x43800000,0xc37f0000),(0xb58637bd,0)]
 authored=[base]
 for octaves in (1,2,8,16):
  w=list(base);w[5]=octaves;w[6]=0x3fc00000;w[7]=0x3eb33333;w[8]=0x3dcccccd;w[9]=0x3e4ccccd;w[10]=0xbe800000;w[11]=0x3f000000;authored.append(w)
 for amp in (0,0xbf000000,0x3f800001):
  w=list(base);w[7]=amp;authored.append(w)
 for wi,hexraw in enumerate([raw.hex()]+[frame(w).hex() for w in authored]):
  for tp in (24,53,64):
   for cp in (24,53,64):
    for p in (24,53,64):
     actions=[]
     for x,z in coords:
      r=request(x,z);actions.extend([query([r,r],p),query([request(x,z,1,1)],p),dict(Kind='Clear')])
     actions.extend([query([request(0x3f000000,0x3e800000)],p),query([request(0x3f000001,0x3e800001)],p),query([request(0x3f000000,0x3e800000),request(0x7fc12345,0,1,0)],p),query([request(0x3f000000,0x3e800000)],p),query([request(0,0,2,0)],p),query([request(0x4f800000,0)],p),query([request(0,0)],p,Cancel=True),dict(Kind='Clear',Cancel=True)])
     cases.append(dict(Hex=hexraw,Width=2,Rows=2,TablePrecision=tp,ConstructionPrecision=cp,Actions=actions))
 # Exact inclusive cache endpoints and adjacent F32 exclusions.
 for p in (24,53,64):
  actions=[query([request(0,0)],p)]
  for x in (0x3727c5ac,0xb727c5ac,0x3727c5ad,0xb727c5ad):actions+=[query([request(x,0)],p),dict(Kind='Clear'),query([request(0,0)],p)]
  cases.append(dict(Hex=frame(base).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53,Actions=actions))
 for word,value in ((1,1),(3,0xffffffff),(12,1),(12,2),(5,0),(5,17),(5,0xffffffff),(0,0x7fffffff),(0,1340),(6,0x7f800000),(7,0x7fc12345),(8,0x7fc12345),(9,0x7f800000),(10,0x7fc12345),(11,0x7f800000),(7,0xbf800000)):
  w=list(base);w[word]=value
  if word==7 and value==0xbf800000:w[5]=2
  cases.append(dict(Hex=frame(w).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53,Actions=[]))
 for extra in (dict(Sine=True),dict(Width=0),dict(Rows=0),dict(Width=129),dict(Width=33,Rows=33),dict(ByteBudget=0),dict(WorkBudget=0),dict(ByteBudget=1048577),dict(WorkBudget=1048577),dict(TablePrecision=23),dict(ConstructionPrecision=65),dict(Cancel=True)):
  c=dict(Hex=frame(base).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53,Actions=[]);c.update(extra);cases.append(c)
 for i,c in enumerate(cases):c['Name']=str(i)
 return dict(Occurrence=occ,Cases=cases)
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for key in ('exe','asset-root','config','out'):p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native',type=Path);args=p.parse_args()
 if not args.out.resolve().is_relative_to(ROOT/'local-private'):raise ValueError('Private output only')
 d=generate(args);e=[expected(c) for c in d['Cases']];args.out.mkdir(parents=True,exist_ok=True)
 summary=dict(Cases=len(e),FactoryRejects=sum(c['Reject']=='Input' for c in e),Actions=sum(len(c.get('Actions',[])) for c in e),Queries=sum(len(a['Results']) for c in e for a in c.get('Actions',[])),ActionRejects=sum(a['Reject']=='Input' for c in e for a in c.get('Actions',[])),MissHitWitnesses=sum(len(a['Results'])==2 and a['Results'][0]['Retained']!=a['Results'][1]['Retained'] for c in e for a in c.get('Actions',[])),OriginalNoiseSamples=0)
 if args.native:
  native=json.loads(args.native.read_text());assert native['Cases']==e,'Native sampler differs from independent graph';summary.update(Passed=True,NativeChecks=native['NegativeChecks']);(args.out/'sampler-receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 else:(args.out/'sampler-fixtures.json').write_text(json.dumps(d),encoding='utf-8');(args.out/'sampler-expected.json').write_text(json.dumps(e),encoding='utf-8')
 print(json.dumps(summary))
