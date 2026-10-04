"""Independent Fraction/integer oracle; synthetic queries, exact store and unspilled compare receipts."""
import argparse,importlib.util,itertools,json,random,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('circle_oracle',ROOT/'poc/round36-tests/Rendering/verify_circle.py');c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)
a=c.a
one=a.scalar(0x3f800000);zero=a.scalar(0)
def add(x,y,p):return a.precision(a.add(x,y),p)
def sub(x,y,p):return add(x,a.neg(y),p)
def mul(x,y,p):return a.precision(a.mul(x,y),p)
def div(x,y,p):
 if not y[0]:raise ValueError('division')
 return a.precision((x[0]/y[0],(x[0]==0) and ((x[0]<0 or x[1])!=(y[0]<0 or y[1]))),p)
def store(x):
 b=a.bits(x)
 if not a.finite(b):raise ValueError('finite store')
 return b
def circle(q):
 p=q['Precision'];cp=q['CachePrecision'];g=list(map(a.scalar,q['Geometry']));xy=list(map(a.scalar,q['Coordinates']));amount=a.scalar(q['Amount'])
 if amount[0]<0:amount=zero
 elif amount[0]>1:amount=one
 rs=store(mul(g[2],g[2],cp));dx=sub(g[0],xy[0],p);dy=sub(g[1],xy[1],p);ds=store(add(mul(dy,dy,p),mul(dx,dx,p),p));distance=a.scalar(ds);cache=a.scalar(rs)
 if distance[0]>cache[0]:return zero
 inner=mul(sub(one,amount,p),g[2],p);sq=mul(inner,inner,p)
 return one if distance[0]<=sq[0] else sub(one,div(sub(distance,sq,p),sub(cache,sq,p),p),p)
def rectangle(q):
 p=q['Precision'];cp=q['CachePrecision'];g=list(q['Geometry']);xy=list(map(a.scalar,q['Coordinates']));amount=a.scalar(q['Amount'])
 if amount[0]<0:amount=zero
 elif amount[0]>1:amount=one
 for j in (0,1):
  if a.scalar(g[j])[0]>a.scalar(g[j+2])[0]:g[j],g[j+2]=g[j+2],g[j]
 def extents(p):
  first=store(sub(a.scalar(g[2]),a.scalar(g[0]),p));second=sub(a.scalar(g[3]),a.scalar(g[1]),p)
  return a.scalar(store(second)) if second[0]<a.scalar(first)[0] else a.scalar(first)
 def inset(p):return mul(mul(amount,extents(p),p),a.scalar(0x3f000000),p)
 t=inset(cp);inner=[store(add(t,a.scalar(g[0]),cp)),store(add(t,a.scalar(g[1]),cp)),store(sub(a.scalar(g[2]),t,cp)),store(sub(a.scalar(g[3]),t,cp))]
 def inside(b):return all(min(a.scalar(b[j])[0],a.scalar(b[j+2])[0])<=xy[j][0]<=max(a.scalar(b[j])[0],a.scalar(b[j+2])[0]) for j in (0,1))
 if not inside(g):return zero
 if not amount[0] or inside(inner):return one
 edges=[store(sub(xy[0],a.scalar(g[0]),p)),store(sub(a.scalar(g[2]),xy[0],p)),store(sub(xy[1],a.scalar(g[1]),p)),store(sub(a.scalar(g[3]),xy[1],p))];den=inset(p);edge=den
 for b in edges:
  if a.scalar(b)[0]<edge[0]:edge=a.scalar(b)
 return div(edge,den,p)
def query(q,premature=False):
 if q is None:return None,None
 if q['Precision'] not in (24,53,64):raise IndexError('Options')
 if q['Type']=='Raw':
  if not a.finite(q['Bits']):raise ValueError('nonfinite raw')
  u=a.scalar(q['Bits']);source=0;raw=q['Bits']
 else:
  if q['CachePrecision'] not in (24,53,64):raise IndexError('Options')
  if not all(a.finite(x) for x in q['Geometry']+q['Coordinates']+[q['Amount']]):raise ValueError('nonfinite carrier')
  u=circle(q) if q['Type']=='Circle' else rectangle(q);source=1 if q['Type']=='Circle' else 2;raw=None
 inspection=store(u)
 return (a.scalar(inspection) if premature else u),dict(Source=source,Precision=q['Precision'],RawBits=raw,ProjectionBits=inspection)
def shape(u,sel,p):
 if sel==0:return u
 if sel==1:return mul(u,u,p)
 if sel==2:return c.sqrt_round(u,p)
 if sel==3:return mul(mul(sub(a.scalar(0x40400000),add(u,u,p),p),u,p),u,p)
 return zero
def evaluate(f,premature=False,spilled_compare=False):
 try:
  p,pp=f['Combine'],f['Product'];limit=f.get('Limits',[256,256,65536]);maxq=f.get('CreateMaximumQueries',256)
  if p not in (24,53,64) or pp not in (24,53,64) or not 0<=maxq<=4096 or not 0<=limit[0]<=65536 or not 0<=limit[1]<=4096 or not 0<=limit[2]<=1048576:raise IndexError('Options')
  # Carrier construction happens before sample construction in the public fixture runner.
  boundaries=[(e,*query(e['Query'],premature)) for e in f['Boundaries']];filters=[(e,*query(e['Query'],premature)) for e in f['Filters']]
  n=len(boundaries)+len(filters)
  if n>maxq or not a.finite(f['Incoming']) or any(e['Enabled'] and u is None for e,u,m in boundaries+filters):raise ValueError('shape/input')
  if limit[0]<1 or n>limit[1] or n>limit[2]:raise ValueError('budget')
  acc=0;any_boundary=False;bv=[];fv=[];be=fe=False
  def visit(group,out,boundary):
   nonlocal acc,any_boundary
   early=False
   for i,(e,u,meta) in enumerate(group):
    old=acc;candidate=None;replaced=False;projection=None if meta is None else meta['ProjectionBits']
    if e['Enabled']:
     if boundary:any_boundary=True
     shaped=shape(u,e['Selector'],p);candidate=store(shaped);operand=a.scalar(candidate) if spilled_compare else shaped
     replaced=a.scalar(old)[0]<operand[0] if boundary else operand[0]<a.scalar(old)[0]
     if replaced:acc=candidate
     early=a.scalar(acc)[0]==(1 if boundary else 0)
    out.append(dict(Index=i,Enabled=e['Enabled'],SelectorBits=e['Selector'],QueryProjectionBits=projection,CandidateBits=candidate,OldAccumulatorBits=old,SelectedBits=acc,Replaced=replaced,EarlyExit=early))
    if early:break
   return early
  if f['BoundaryGate']>0:be=visit(boundaries,bv,True)
  fallback=not any_boundary
  if fallback:acc=0x3f800000
  if f['InvertBoundary']:acc=store(sub(one,a.scalar(acc),p))
  first=a.scalar(acc)[0]>0
  if first:
   if f['FilterGate']>0:fe=visit(filters,fv,False)
   if f['InvertFilter']:acc=store(sub(one,a.scalar(acc),p))
  product=store(mul(a.scalar(acc),a.scalar(f['Incoming']),pp));localpositive=first and a.scalar(acc)[0]>0
  return dict(Name=f['Name'],Reject='None',LocalInfluenceBits=acc,ProductBits=product,BoundaryFallback=fallback,BoundaryEarlyExit=be,FilterEarlyExit=fe,BoundaryPositive=first,LocalPositive=localpositive,AffectorEligible=localpositive,ProductPositive=a.scalar(product)[0]>0,BoundaryVisits=bv,FilterVisits=fv,BoundaryInputs=[m for e,u,m in boundaries],FilterInputs=[m for e,u,m in filters])
 except IndexError:return dict(Name=f['Name'],Reject='Options')
 except (ValueError,ZeroDivisionError):return dict(Name=f['Name'],Reject='Input')
def raw(bits,p=24):return dict(Type='Raw',Bits=bits,Precision=p)
def entry(bits=0,sel=0,enabled=True,p=24,q=None):return dict(Enabled=enabled,Selector=sel,Query=raw(bits,p) if q is None and enabled else q)
def fb(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def form(tag,data):return b'FORM'+struct.pack('>I',len(data)+4)+tag.encode()+data
def chunk(tag,data):return tag.encode()+struct.pack('>I',len(data))+data
def encode(q):
 header=form('IHDR',form('0001',chunk('DATA',struct.pack('<I',1)+b'synthetic\0')))
 words=q['Geometry']+[0,q['Amount']]
 return form('BCIR' if q['Type']=='Circle' else 'BREC',form('0002',header+chunk('DATA',struct.pack('<'+'I'*len(words),*words)))).hex()
def fixtures():
 out=[]
 def emit(b,f,inc=0x3f800000,p=24,pp=24,bg=1,fg=1,ib=False,iff=False,limits=None,maxq=256):
  v=dict(Name=str(len(out)),Boundaries=b,Filters=f,Incoming=inc,Combine=p,Product=pp,BoundaryGate=bg,FilterGate=fg,InvertBoundary=ib,InvertFilter=iff,CreateMaximumQueries=maxq)
  if limits is not None:v['Limits']=limits
  for e in b+f:
   q=e['Query']
   if q is not None and q['Type']!='Raw':q['RuleHex']=encode(q)
  out.append(v)
 values=[0,0x80000000,1,0x80000001,0x00800000,0x3eaaaaab,0x3f000001,0x3f7fffff,0x3f800000,0x3f800001,0x40000000,0xbf800000,0x7f7fffff,0xff7fffff]
 parents=[0,0x80000000,0xbf800000,1,0x80000001,0x3f800000,0x3f000001,0x7f7fffff]
 for p,pp in itertools.product((24,53,64),repeat=2):
  for i,bits in enumerate(values):
   for sel in (0,1,2,3,4,0xffffffff):
    for ib,iff in itertools.product((False,True),repeat=2):emit([entry(enabled=False),entry(bits,sel),entry(0x40000000)],[entry(values[(i+5)%len(values)],sel),entry(enabled=False),entry(0)],parents[(i+sel)%len(parents)],p,pp,ib=ib,iff=iff)
  for bg,fg,enabled,ib,iff in itertools.product((-1,0,1),(-1,0,1),(False,True),(False,True),(False,True)):emit([entry(0x3f000000,enabled=enabled)],[entry(0x3e800000,enabled=enabled)],p=p,pp=pp,bg=bg,fg=fg,ib=ib,iff=iff)
  # Exact-one/zero early stop, overshoot, order, invalid late shaping and signed zeros.
  for order in itertools.permutations([0x3f800000,0x40000000,0x3f000000]):emit([entry(x) for x in order],[entry(0x3f800000)],p=p,pp=pp)
  emit([entry(0x3f800000),entry(0x7f7fffff,1)],[entry(0x80000000),entry(0xbf800000,2)],p=p,pp=pp)
  emit([],[],0xbf800000,p,pp,iff=True)
  emit([entry(0x40000000)],[],0xbf800000,p,pp,ib=True)
  for old in (0x3f000000,0x3f7fffff,0x3f800000):
   for x in (0x3eaaaaab,0x3f000001,0x3f7fffff):emit([entry(old),entry(x,3)],[entry(old),entry(x,3)],p=p,pp=pp)
 rng=random.Random(4007)
 for i in range(1800):
  bits=lambda:rng.getrandbits(32)&0xfeffffff
  b=[entry(bits(),rng.choice([0,1,2,3,4]),rng.randrange(5)!=0) for j in range(rng.randrange(5))]
  f=[entry(bits(),rng.choice([0,1,2,3,0xffffffff]),rng.randrange(5)!=0) for j in range(rng.randrange(5))]
  emit(b,f,rng.choice(parents),rng.choice([24,53,64]),rng.choice([24,53,64]),rng.choice([-1,0,1]),rng.choice([-1,0,1]),bool(rng.randrange(2)),bool(rng.randrange(2)))
 # Fresh circle/rectangle values remain unspilled across all query/cache/combine policies.
 for kind in ('Circle','Rectangle'):
  for cp,qp,p,pp,sel in itertools.product((24,53,64),(24,53,64),(24,53,64),(24,53,64),(0,1,2,3)):
   g=[0,0,0x3f800000] if kind=='Circle' else [0,0,fb(3),fb(2)]
   xy=[fb(.8),fb(.1)] if kind=='Circle' else [fb(.2),fb(.7)]
   q=dict(Type=kind,Precision=qp,CachePrecision=cp,Geometry=g,Coordinates=xy,Amount=fb(.7))
   emit([entry(q=q,sel=sel)],[entry(q=q,sel=(sel+1)%4)],p=p,pp=pp)
 # Literal equal-projection pairs distinguish comparing the unspilled shaped value.
 for p in (24,53,64):
  for bits,sel in itertools.product((0x3eaaaaab,0x3f000001,0x3f19999a),(1,2,3)):
   old=store(shape(a.scalar(bits),sel,p))
   emit([entry(old),entry(bits,sel)],[entry(bits,sel)],p=p,pp=p)
 # Modern preflight, precise budgets and malformed operands.
 for bad in (0x7f800000,0xff800000,0x7fc12345):
  emit([entry(bad)],[],bg=0);emit([],[],bad)
 for p in (0,23,25,52,54,63,65):emit([],[],p=p);emit([],[],pp=p);emit([entry(0,p=p)],[])
 for l in ([0,2,2],[1,1,2],[1,2,1],[1,2,2],[-1,2,2],[65537,2,2],[1,-1,2],[1,4097,2],[1,2,-1],[1,2,1048577]):emit([entry(0x3f000000)],[entry(0x3f000000)],limits=l)
 emit([dict(Enabled=True,Selector=0,Query=None)],[])
 emit([entry(0x3f800000),dict(Enabled=True,Selector=0,Query=None)],[])
 for cap in (-1,0,1,2,4097):emit([entry(0x3f000000)],[entry(0x3f000000)],maxq=cap)
 emit([entry(0x7f7fffff)],[],0x7f7fffff)
 return out

def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('--native',type=Path);args=p.parse_args()
 assert 'local-private' in args.fixtures.parts and 'local-private' in args.receipt.parts
 from static_layer import verify
 static=verify(args.exe,args.packages);fs=fixtures();expected=[evaluate(f) for f in fs]
 premature=sum(evaluate(f,True)!=e for f,e in zip(fs,expected));spilled=sum(evaluate(f,False,True)!=e for f,e in zip(fs,expected))
 assert premature>0 and spilled>0,(premature,spilled)
 result=dict(Static=static,Cases=len(fs),Accepted=sum(e['Reject']=='None' for e in expected),Rejected=sum(e['Reject']!='None' for e in expected),PrematureQueryProjectionWitnesses=premature,SpilledCompareWitnesses=spilled,SyntheticOnly=True)
 args.fixtures.parent.mkdir(parents=True,exist_ok=True)
 if args.native:
  native=json.loads(args.native.read_text());assert len(native['Cases'])==len(expected)
  for f,want,got in zip(fs,expected,native['Cases']):assert got==want,(f,want,got)
  assert native['OwnershipChecks']>=53;result['OwnershipChecks']=native['OwnershipChecks']
 else:args.fixtures.write_text(json.dumps(fs,separators=(',',':')))
 args.receipt.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()



