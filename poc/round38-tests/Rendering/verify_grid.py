"""Exact conditional coordinate graph oracle, synthetic requests, SHA-first static proof."""
import argparse,importlib.util,itertools,json,random,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
math=load('fraction_math',ROOT/'poc/round34-tests/Assets/verify_ahcn_operation.py')
static=load('coordinate_static',ROOT/'tools/python/verify_terrain_sample_coordinates.py')
def fb(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def integer(i):return math.Fraction(i),False
DEFAULT=dict(MaximumCount=256,MaximumPairs=65536)
def evaluate(f):
 r=f['Request'];lp,bp,qp,pp=f['Profiles'];limits=f['Limits'];result=dict(Name=f['Name'],Reject='Input')
 if any(p not in (24,53,64) for p in f['Profiles']) or not 2<=limits['MaximumCount']<=1024 or not 4<=limits['MaximumPairs']<=1048576:return dict(Name=f['Name'],Reject='Options')
 width=r['ChunkWidthBits'];tiles=r['Tiles'];margin=r['Margin'];count=2*(tiles+margin)
 if not math.finite(width) or width&0x80000000 or width&0x7fffffff==0 or tiles<=0 or r['Scale']<=0:return result
 if not -2147483648<=tiles+margin<=2147483647 or not -2147483648<=count<=2147483647 or count<=0 or count>limits['MaximumCount'] or count*count>limits['MaximumPairs']:return result
 def store(value):
  b=math.bits(value)
  if not math.finite(b):raise ValueError('nonfinite store')
  return b
 def mul(x,y,p):return math.precision(math.mul(x,y),p)
 def add(x,y,p):return math.precision(math.add(x,y),p)
 try:
  w=math.scalar(width);derived=store(math.precision((w[0]/tiles,False),lp));scale=store(integer(r['Scale']))
  step=store(mul(mul(integer(r['Scale']),math.scalar(derived),bp),math.scalar(0x3f000000),bp))
  mb=store(mul(integer(margin),math.scalar(step),bp))
  def origin(i):return store(add(mul(integer(i),w,bp),math.neg(math.scalar(mb)),bp))
  first,second=origin(r['First']),origin(r['Second'])
  def axes(p,row_spill):
   x=[];z=[]
   for i in range(count):
    row=math.scalar(store(integer(i))) if row_spill else integer(i)
    x.append(store(add(mul(integer(i),math.scalar(step),p),math.scalar(first),p)))
    z.append(store(add(mul(row,math.scalar(step),p),math.scalar(second),p)))
   xi=all(math.scalar(a)[0]<math.scalar(b)[0] for a,b in zip(x,x[1:]));zi=all(math.scalar(a)[0]<math.scalar(b)[0] for a,b in zip(z,z[1:]))
   return dict(First=x,Second=z,Pairs=[dict(Column=i,Row=j,FirstBits=x[i],SecondBits=z[j]) for j in range(count) for i in range(count)],FirstStrictlyIncreasing=xi,SecondStrictlyIncreasing=zi,HasStrictlyIncreasingDisplayAxes=xi and zi)
  layer,position=axes(qp,True),axes(pp,False)
 except ValueError:return result
 return dict(Name=f['Name'],Reject='None',Request=r,Policies=dict(MetadataLoading=lp,ChunkBuilding=bp,LayerCoordinates=qp,PositionCoordinates=pp),Parameters=dict(Count=count,DerivedSpacingBits=derived,ScaleStoredBits=scale,StepBits=step,MarginBits=mb,FirstOriginBits=first,VerticalOriginBits=0,SecondOriginBits=second),Layer=layer,Position=position)

def fixtures():
 fs=[]
 def emit(r,p=(24,53,64,24),limits=None):fs.append(dict(Name=str(len(fs)),Request=r,Profiles=list(p),Limits=limits or DEFAULT))
 def request(w=0x41000000,t=2,m=1,x=-2,z=3,s=1):return dict(ChunkWidthBits=w,Tiles=t,Margin=m,First=x,Second=z,Scale=s)
 shapes=[request(),request(t=3,m=-2,x=-17,z=16,s=3),request(w=0x3f800001,t=7,m=-5,x=16777217,z=-16777217,s=16777217),request(w=0x3eaaaaab,t=3,m=-1,x=2147483647,z=-2147483648,s=2147483647),request(w=1,t=2,m=0,x=-1,z=0),request(w=0x00800001,t=3,m=-1,x=-2147483648,z=2147483647,s=3),request(w=0x3f800000,t=2,m=0,x=2147483647,z=-2147483648),request(w=0x3f800001,t=16777217,m=-16777215,x=3,z=-3),request(w=0x7f7fffff,t=1,m=0,x=0,z=0,s=1),request(w=0x7f000000,t=1,m=0,x=3,z=0),request(w=0x3f000001,t=2,m=-1,x=0,z=-1,s=3),request(w=0x1f3c0aab,t=3,m=-1,x=17,z=-19,s=31)]
 for p in itertools.product((24,53,64),repeat=4):
  for r in shapes:emit(r,p)
 rng=random.Random(3801)
 for i in range(120):
  w=(rng.randint(1,249)<<23)|rng.randrange(1<<23);t=rng.choice((1,3,7,16777217,2147483647));m=2-t
  r=request(w,t,m,rng.randint(-2147483648,2147483647),rng.randint(-2147483648,2147483647),rng.choice((1,3,16777217,2147483647)))
  for p in itertools.product((24,53,64),repeat=4):emit(r,p)
 emit(request(w=0x7f000000,t=1,m=0,x=1,z=0,s=3))
 for w in (0,0x80000000,0xbf800000,0x7f800000,0xff800000,0x7fc12345):emit(request(w))
 for t,m,s in [(0,1,1),(-1,2,1),(2,1,0),(2,1,-1),(1,-1,1),(1,-2,1),(2147483647,1,1),(1,2147483646,1),(2147483647,-2147483648,1)]:emit(request(t=t,m=m,s=s))
 for j in range(4):
  for bad in (0,23,25,52,54,63,65):
   p=[24]*4;p[j]=bad;emit(request(),p)
 for cap in [dict(MaximumCount=1,MaximumPairs=36),dict(MaximumCount=1025,MaximumPairs=36),dict(MaximumCount=6,MaximumPairs=3),dict(MaximumCount=6,MaximumPairs=1048577),dict(MaximumCount=5,MaximumPairs=36),dict(MaximumCount=6,MaximumPairs=35),dict(MaximumCount=6,MaximumPairs=36)]:emit(request(),limits=cap)
 return fs

p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('--native',type=Path);args=p.parse_args()
st=static.verify(args.exe,args.packages)
# Additional aligned anchors pin exact loads, unused scale FST, and checked-modern count source.
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
pe=pefile.PE(data=args.exe.read_bytes(),fast_load=True);b=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
extra={0x84a5a9:('fst','dword ptr [ebp - 0x18]'),0x84a5ac:('add','eax, ebx'),0x84a5b0:('shl','eax, 1'),0x84a5c0:('mov','dword ptr [ebp - 0x24], 0'),0xb65790:('fild','dword ptr [ebp - 0x14]'),0xb657a8:('fild','dword ptr [ebp - 0x10]'),0x84937c:('call','0xb5d7c0'),0xb5d7f1:('mov','dword ptr [esi + 0x58], 3'),0xb5d7f8:('mov','dword ptr [esi + 0x5c], 2')}
for va,want in extra.items():
 ins=next(md.disasm(b[va-base:va-base+15],va,count=1));assert (ins.mnemonic,ins.op_str)==want
fs=fixtures();args.fixtures.write_text(json.dumps(dict(Cases=fs),separators=(',',':')));expected=[evaluate(f) for f in fs]
receipt=dict(Static=st,AdditionalAnchors=len(extra),ProfileCombinations=81,Cases=len(fs),Accepted=sum(f['Reject']=='None' for f in expected),Rejected=sum(f['Reject']!='None' for f in expected),SyntheticRequests=True,OriginalCoordinatesCompared=False)
if args.native:
 native=json.loads(args.native.read_text(encoding='utf-8-sig'));assert len(native['Cases'])==len(expected)
 for want,got in zip(expected,native['Cases']):assert want==got,(want['Name'],want,got)
 receipt['OwnershipChecks']=native['OwnershipChecks'];assert native['OwnershipChecks']>=35
 receipt['DuplicateAxisCases']=sum(f['Reject']=='None' and not f['Layer']['HasStrictlyIncreasingDisplayAxes'] for f in expected)
 receipt['LayerPositionDifferentCases']=sum(f['Reject']=='None' and (f['Layer']['First']!=f['Position']['First'] or f['Layer']['Second']!=f['Position']['Second']) for f in expected)
 groups={}
 for f,e in zip(fs,expected):
  if e['Reject']=='None':groups.setdefault(tuple(f['Request'].values()),set()).add(json.dumps(dict(Parameters=e['Parameters'],Layer=e['Layer'],Position=e['Position']),sort_keys=True))
 receipt['PrecisionSensitiveRequests']=sum(len(v)>1 for v in groups.values());assert receipt['PrecisionSensitiveRequests']>0 and receipt['LayerPositionDifferentCases']>0
args.receipt.write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
