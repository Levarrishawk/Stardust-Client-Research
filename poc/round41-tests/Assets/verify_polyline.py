"""Independent Fraction/integer BPLN open-segment/cache/shaping oracle."""
import contextlib,importlib.util,itertools,json,random,sys,hashlib,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
c=load('circle_reference','poc/round36-tests/Rendering/verify_circle.py');a=c.a
raw=load('bpln_reference','poc/round38-tests/Assets/verify_bpln_input.py')
DEFAULT=dict(MaximumPoints=8192,MaximumSamples=65536,MaximumPointVisits=1048576)
def encode(f):
 return raw.polyline(f['Version'].encode(),raw.header(b'0001',name=b'synthetic',enabled=f['Enabled']),points=f['Points'],meta=[f['EncodedSelector'],f['Amount'],f['Width']]).hex()
def evaluate(f,spill_before_shape=False,spill_dy_length=False,spill_initial_square=False):
 cp,p=f['CachePrecision'],f['Precision'];lim=f['Limits'];points=f['Points'];n=len(points)
 if cp not in (24,53,64) or p not in (24,53,64):return dict(Reject='Options')
 if not 0<=lim['MaximumPoints']<=65536 or not 0<=lim['MaximumSamples']<=1048576 or not 0<=lim['MaximumPointVisits']<=67108864:return dict(Reject='Options')
 if f['Version'] not in ('0000','0001') or n>lim['MaximumPoints'] or lim['MaximumSamples']<1:return dict(Reject='Input')
 if f['Action']=='evaluate' and not f['Enabled']:return dict(Reject='None',Skipped=True,Influence=None,Query=None,Branch=None,Bounds=None,Amount=None,Square=None,Minimum=None,Projections=None,Charge=0)
 if 4*n>lim['MaximumPointVisits']:return dict(Reject='Input')
 if not all(a.finite(b) for pt in points for b in pt) or not all(a.finite(b) for b in f['Coordinates']+[f['Amount'],f['Width']]):return dict(Reject='Input')
 def add(x,y,k=p):return a.precision(a.add(x,y),k)
 def sub(x,y,k=p):return add(x,a.neg(y),k)
 def mul(x,y,k=p):return a.precision(a.mul(x,y),k)
 def div(x,y):
  if not y[0]:raise ValueError('zero division')
  negative=(x[0]<0 or (not x[0] and x[1]))^(y[0]<0 or (not y[0] and y[1]))
  return a.precision((x[0]/y[0],negative if not x[0] else False),p)
 def store(x):
  b=a.bits(x)
  if not a.finite(b):raise ValueError('finite spill')
  return b
 def dist(dx,dy):return add(mul(dy,dy),mul(dx,dx))
 def select(current,candidate):
  bits=store(candidate)
  return a.scalar(bits) if candidate[0]<current[0] else current
 try:
  bounds=[0x7f7fffff,0x7f7fffff,0xff7fffff,0xff7fffff];w=a.scalar(f['Width']);av=a.scalar(f['Amount']);one=a.scalar(0x3f800000)
  for pt in points:
   for j in (0,1):
    if a.scalar(pt[j])[0]<a.scalar(bounds[j])[0]:bounds[j]=pt[j]
    if a.scalar(pt[j])[0]>a.scalar(bounds[j+2])[0]:bounds[j+2]=pt[j]
  bounds=[store(sub(a.scalar(bounds[0]),w,cp)),store(sub(a.scalar(bounds[1]),w,cp)),store(add(a.scalar(bounds[2]),w,cp)),store(add(a.scalar(bounds[3]),w,cp))]
  x,y=map(a.scalar,f['Coordinates']);square=minimum_bits=projections=None
  contained=all(min(a.scalar(bounds[j])[0],a.scalar(bounds[j+2])[0])<=a.scalar(f['Coordinates'][j])[0]<=max(a.scalar(bounds[j])[0],a.scalar(bounds[j+2])[0]) for j in (0,1))
  if not contained:branch=0;u=a.scalar(0)
  else:
   minimum=mul(w,w);square=store(minimum)
   if spill_initial_square:minimum=a.scalar(square)
   for pt in points:
    vx,vy=map(a.scalar,pt);minimum=select(minimum,dist(sub(x,vx),sub(y,vy)))
   projections=0
   for i in range(1,n):
    ax,ay=map(a.scalar,points[i-1]);bx,by=map(a.scalar,points[i]);dx=a.scalar(store(sub(bx,ax)));dy_raw=sub(by,ay);dy=a.scalar(store(dy_raw))
    dot=add(mul(sub(y,ay),dy),mul(sub(x,ax),dx));length=add(mul(dy if spill_dy_length else dy_raw,dy if spill_dy_length else dy_raw),mul(dx,dx))
    t=a.scalar(store(div(dot,length)))
    if t[0]<0 or t[0]>1:continue
    projections+=1;qx=add(mul(dx,t),ax);qy=add(mul(dy,t),ay);minimum=select(minimum,dist(sub(x,qx),sub(y,qy)))
   minimum_bits=store(minimum)
   if minimum[0]>=a.scalar(square)[0]:branch=1;u=a.scalar(0)
   else:
    threshold=mul(sub(one,av),w);stored=a.scalar(store(threshold));threshold_square=mul(threshold,stored)
    if minimum[0]<threshold_square[0]:branch=2;u=one
    else:branch=3;u=sub(one,div(sub(c.sqrt_round(minimum,p),stored),sub(w,stored)))
  projection=store(u)
  if spill_before_shape:u=a.scalar(projection)
  sel=f['Selector']
  if sel==0:shaped=u
  elif sel==1:shaped=mul(u,u)
  elif sel==2:shaped=c.sqrt_round(u,p)
  elif sel==3:shaped=mul(mul(sub(a.scalar(0x40400000),add(u,u)),u),u)
  else:shaped=a.scalar(0)
  return dict(Reject='None',Skipped=False,Influence=store(shaped),Query=projection,Branch=branch,Bounds=bounds,Amount=f['Amount'],Square=square,Minimum=minimum_bits,Projections=projections,Charge=4*n)
 except ValueError:return dict(Reject='Input')
def fixtures():
 out=[];b=c.fbits
 def emit(points,xy,width=0x3f800000,amount=0,selector=0,enabled=1,version='0001',action='evaluate',cp=24,p=24,limits=None):
  f=dict(Name='bpln-'+str(len(out)),Points=points,Coordinates=list(xy),Width=width,Amount=amount,Selector=selector,EncodedSelector=selector,Enabled=enabled,Version=version,Action=action,CachePrecision=cp,Precision=p,Limits=limits or DEFAULT)
  f['RuleHex']=encode(f);f['Expected']=evaluate(f);out.append(f)
 shapes=[[],[(0,0)],[(0,0),(b(2),0)],[(0,0),(b(2),0),(b(2),b(2))],[(0,0),(b(2),0),(b(2),b(2)),(0,b(2))],[(0,0),(b(2),0),(b(2),b(2)),(0,0)],[(0,0),(0,0)],[(0,0),(b(2),0),(b(2),0)],[(0x80000000,0),(0,0x80000000)],[(1,1),(3,7)]]
 shapes += [list(reversed(shapes[2])),list(reversed(shapes[3])),list(reversed(shapes[4]))]
 for pts in shapes:
  for xy in [(0,0),(b(.5),b(.5)),(b(1),0),(b(1),b(1)),(b(2),0),(b(3),0),(b(-1),0),(b(.75),b(.75)),(b(2.5),b(2)),(1,1)]:
   for width in [0,0x80000000,b(.5),b(1),b(-.5),1]:
    for amount in [0,0x80000000,b(.5),b(1),b(-.5),b(2),1]:
     for selector in [0,1,2,3,0xffffffff]:
      for p in [24,53,64]:emit(pts,xy,width,amount,selector,p=p)
 # Exact dy spill witness: (2**25)-1 is retained in 53/64 but rounds to 2**25 in F32.
 for pts in [[(0,b(1)),(0,b(33554432))],[(0,b(33554432)),(0,b(1))]]:
  for cp,p in itertools.product([24,53,64],repeat=2):
   for selector in [0,1,2,3]:emit(pts,(0,b(16777216)),b(2),b(1),selector,cp=cp,p=p)
 # Non-power-of-two width exposes the retained initial square and threshold stores.
 for width in [b(.1),b(1.1),b(-.1)]:
  wf=a.scalar(width)[0]
  for amount in [0,b(.1),b(.5),b(1),b(-.5),b(2),1]:
   threshold=float((1-a.scalar(amount)[0])*wf)
   xs=[0,width,b(-float(wf)),b(threshold)]
   edge=b(threshold)
   if edge not in (0,0x80000000):xs += [edge-1,edge+1]
   for pts in [[],[(0,0)]]:
    for x in xs:
     for cp,p in itertools.product([24,53,64],repeat=2):
      for selector in [0,2,3]:emit(pts,(x,0),width,amount,selector,cp=cp,p=p)
 rng=random.Random(4103)
 for k in range(90):
  scale=2.0**rng.randrange(-15,16);pts=[(b(rng.uniform(-2,2)*scale),b(rng.uniform(-2,2)*scale)) for _ in range(rng.randrange(1,6))]
  xy=(b(rng.uniform(-2,2)*scale),b(rng.uniform(-2,2)*scale))
  for cp,p in itertools.product([24,53,64],repeat=2):
   for selector in [0,1,3]:emit(pts,xy,b(scale),b(.37),selector,cp=cp,p=p)
 square=shapes[4]
 for version in ['0000','0001','0099']:
  for cp,p in itertools.product([24,53,64],repeat=2):emit(square,(0,0),version=version,cp=cp,p=p)
 for bits in [0x7f800000,0xff800000,0x7fc12345]:
  for p in [24,53,64]:
   emit([(bits,0)],(0,0),p=p);emit(square,(bits,0),p=p);emit(square,(0,0),width=bits,p=p);emit(square,(0,0),amount=bits,p=p)
 emit(square,(0x7fc12345,0),width=0x7f800000,amount=0x7f800000,enabled=0,limits={**DEFAULT,'MaximumPointVisits':0})
 for limits in [dict(MaximumPoints=4,MaximumSamples=1,MaximumPointVisits=16),dict(MaximumPoints=3,MaximumSamples=1,MaximumPointVisits=16),dict(MaximumPoints=4,MaximumSamples=0,MaximumPointVisits=16),dict(MaximumPoints=4,MaximumSamples=1,MaximumPointVisits=15)]:emit(square,(0,0),limits=limits)
 for key,values in [('MaximumPoints',[-1,65537]),('MaximumSamples',[-1,1048577]),('MaximumPointVisits',[-1,67108865])]:
  for v in values:emit(square,(0,0),limits={**DEFAULT,key:v})
 emit([], (0,0),limits=dict(MaximumPoints=0,MaximumSamples=1,MaximumPointVisits=0));emit([(0,0)]*8193,(0,0))
 for width in [0x7f7fffff,0xff7fffff]:emit(square,(0,0),width=width)
 emit([(0xff7fffff,0),(0x7f7fffff,0)],(0,0),width=b(.5));emit([(0,0)],(0,0),width=b(1e20));emit([(0,0)],(0,0),amount=0x7f7fffff)
 emit(square,(0,0),cp=23);emit(square,(0,0),p=65)
 return out
def original(root,cfg):
 m=raw.m;assert m.sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
  else:raise AssertionError('Pinned terrain missing')
 assert priority==42 and len(payload)==1188446 and m.sha(payload)==m.SOURCE
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);generator=payload[section[2]:section[3]]
 assert section[2]==210 and len(generator)==139608 and m.sha(generator)==m.GENERATOR
 rules=[r for r in m.metadata(generator)['Rules'] if r['Section']['FormType']=='BPLN'];assert len(rules)==34
 first=rules[0];part=first['Section'];start=part['Offset'];meta=raw.metadata(generator[start:start+part['EncodedBytes']])
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=42,Count=34,First=dict(LayerIndex=first['LayerIndex'],ChildOrdinal=first['ChildOrdinal'],Offset=start,Version=meta['Version'],HeaderVersion=meta['Header']['Version'],EnabledBits=meta['Header']['EnabledBits'],PointBits=[[p['FirstBits'],p['SecondBits']] for p in meta['Points']],MetadataWordBits=meta['MetadataWordBits'],EncodedBytes=meta['EncodedBytes'],Sha256=meta['Sha256']))
def status_checks():
 status=lambda a,b:1 if a<b else 0x40 if a==b else 0
 jp=lambda ah,mask:(ah&mask).bit_count()%2==0
 count=0
 for x,y in itertools.product(range(3),repeat=2):
  assert (not jp(status(x,y),5))==(x<y);count+=1
  assert jp(status(x,y),5)==(x>=y);count+=1
 for t in [-1,0,1,2]:
  assert ((status(t,0)&1)!=0 or jp(status(t,1),0x41))==(t<0 or t>1);count+=1
 return count
def static(exe,packages):
 sys.path.insert(0,packages);data=Path(exe).read_bytes();assert hashlib.sha256(data).hexdigest()==c.EXPECTED
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 anchors={0xb74aa0:('call','0xb738b0'),0xb74aa8:('fsub','dword ptr [ecx + 0x38]'),0xb74aab:('fstp','dword ptr [ecx + 0x28]'),0xb74ab1:('fadd','dword ptr [ecx + 0x38]'),0xb74ab4:('fstp','dword ptr [ecx + 0x30]'),0xb74aba:('fsub','dword ptr [ecx + 0x38]'),0xb74abd:('fstp','dword ptr [ecx + 0x2c]'),0xb74ac3:('fadd','dword ptr [ecx + 0x38]'),0xb74ac6:('fstp','dword ptr [ecx + 0x34]'),0xb74b93:('fld','dword ptr [ecx + 0x38]'),0xb74b97:('mov','esi, dword ptr [ecx + 0x24]'),0xb74b9e:('fmulp','st(1)'),0xb74ba0:('fst','dword ptr [ebp - 0x1c]'),0xb74bc3:('faddp','st(1)'),0xb74bc5:('fst','dword ptr [ebp - 0x18]'),0xb74bd0:('test','ah, 5'),0xb74bd3:('jp','0xb74bda'),0xb74bd7:('fld','dword ptr [ebp - 0x18]'),0xb74be1:('lea','eax, [esi - 1]'),0xb74be6:('jle','0xb74c9f'),0xb74bef:('add','edx, 8'),0xb74c08:('fstp','dword ptr [ebp - 8]'),0xb74c14:('fst','dword ptr [ebp - 0x10]'),0xb74c29:('faddp','st(1)'),0xb74c2b:('fld','st(1)'),0xb74c2d:('fmul','st(2)'),0xb74c35:('faddp','st(1)'),0xb74c37:('fdivp','st(1)'),0xb74c39:('fstp','dword ptr [ebp - 4]'),0xb74c3e:('fld','dword ptr [ebp - 4]'),0xb74c4c:('jne','0xb74c95'),0xb74c4e:('fld','dword ptr [ebp - 4]'),0xb74c5c:('jp','0xb74c95'),0xb74c80:('fst','dword ptr [ebp - 0x10]'),0xb74c8e:('jp','0xb74c95'),0xb74c92:('fld','dword ptr [ebp - 0x10]'),0xb74c95:('add','edx, 8'),0xb74c99:('jne','0xb74bf4'),0xb74c9f:('fcom','dword ptr [ebp - 0x1c]'),0xb74ca8:('jp','0xb74cee'),0xb74caa:('fld','dword ptr [ecx + 0x18]'),0xb74cad:('fsubr','dword ptr [0x15dc6f8]'),0xb74cb3:('fmul','dword ptr [ecx + 0x38]'),0xb74cb6:('fst','dword ptr [ebp + 8]'),0xb74cb9:('fmul','dword ptr [ebp + 8]'),0xb74cbc:('fld','st(1)'),0xb74cc5:('jp','0xb74cd5'),0xb74cd5:('fsqrt',''),0xb74cd7:('fsub','dword ptr [ebp + 8]'),0xb74cdd:('fsub','dword ptr [ebp + 8]'),0xb74ce0:('fdivp','st(1)'),0xb74ce2:('fsubr','dword ptr [0x15dc6f8]')}
 anchors.update({0xb74af8:('test','ah, 1'),0xb74b35:('test','ah, 0x41'),0xb74b46:('test','ah, 1'),0xb74b68:('test','ah, 0x41'),0xb74b79:('test','ah, 1'),0xb74b8a:('test','ah, 0x41'),0xb74c49:('test','ah, 1'),0xb74c59:('test','ah, 0x41'),0xb74c8b:('test','ah, 5'),0xb74ca5:('test','ah, 5'),0xb74cc2:('test','ah, 5'),0xb74a88:('mov','dword ptr [ecx + 0x38], eax'),0xb74a8b:('call','dword ptr [edx + 0x38]'),0xb738b0:('mov','eax, 0x7f7fffff'),0xb738bb:('mov','eax, 0xff7fffff'),0xb738e1:('test','ah, 5'),0xb738f0:('test','ah, 5'),0xb738ff:('test','ah, 0x41'),0xb73910:('test','ah, 0x41'),0xb63f8e:('call','0xb62f20'),0xb63faf:('call','dword ptr [edx + 0x24]'),0xb63fb2:('cmp','edi, 3'),0xb63fb5:('ja','0xb63fd8'),0xb63fc0:('fmulp','st(1)'),0xb63fc4:('fsqrt',''),0xb63fca:('fadd','st(1)'),0xb63fcc:('fsubr','dword ptr [0x15dd850]'),0xb63fd2:('fmul','st(1)'),0xb63fd4:('fmulp','st(1)'),0xb63fda:('fld','dword ptr [0x15dc6a0]'),0xb63fe0:('fst','dword ptr [ebp - 0x34]')})
 assert struct.unpack_from('<4I',image,0xb642a0-base)==(0xb63fe0,0xb63fbe,0xb63fc4,0xb63fc8)
 assert struct.unpack_from('<I',image,0x15dd850-base)[0]==0x40400000
 for va,pair in anchors.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==pair,(hex(va),pair,(i.mnemonic,i.op_str))
 query=list(md.disasm(image[0xb74ad0-base:0xb74cfa-base],0xb74ad0));assert not any(i.mnemonic=='call' for i in query)
 assert struct.unpack_from('<I',image,0x15dc6a0-base)[0]==0 and struct.unpack_from('<I',image,0x15dc6f8-base)[0]==0x3f800000
 return dict(sha256=c.EXPECTED,anchors=len(anchors),FiniteStatusCases=status_checks(),queryCalls=0,rawInput=raw.static(Path(exe)))
if __name__=='__main__':
 if sys.argv[1]=='generate':
  fs=fixtures();Path(sys.argv[2]).write_text(json.dumps(dict(Cases=fs),separators=(',',':')));print('Polyline vectors',len(fs))
 else:
  native=json.loads(Path(sys.argv[2]).read_text());fs=fixtures();assert len(fs)==len(native['Cases'])
  for f,n in zip(fs,native['Cases']):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  assert native['OwnershipChecks']>=30 and native['ArgumentNegatives']==6
  expected=original(Path(sys.argv[5]),Path(sys.argv[6]));assert native['Original']==expected
  valid=[f for f in fs if f['Expected']['Reject']=='None' and not f['Expected']['Skipped']];groups={};cache_groups={}
  for f in valid:
   key=(tuple(map(tuple,f['Points'])),tuple(f['Coordinates']),f['Amount'],f['Width'],f['Selector'],f['Version'],f['Action'],f['Enabled'],f['EncodedSelector'],f['CachePrecision']);groups.setdefault(key,set()).add(f['Expected']['Influence'])
   key=key[:-1]+(f['Precision'],);cache_groups.setdefault(key,set()).add(tuple(f['Expected']['Bounds']))
  result=dict(passed=True,vectors=len(fs),rejections=sum(f['Expected']['Reject']!='None' for f in fs),OwnershipChecks=native['OwnershipChecks'],ArgumentNegatives=6,static=static(sys.argv[3],sys.argv[4]),queryPrecisionSensitiveInputs=sum(len(v)>1 for v in groups.values()),cachePrecisionSensitiveInputs=sum(len(v)>1 for v in cache_groups.values()),retainedSquareSensitiveCases=sum(evaluate(f,False,False,True)!=f['Expected'] for f in valid),retainedDeltaSensitiveCases=sum(evaluate(f,False,True)!=f['Expected'] for f in valid),unspilledShapingSensitiveCases=sum(evaluate(f,True).get('Influence')!=f['Expected']['Influence'] for f in valid),originalRawOccurrences=1)
  assert result['retainedSquareSensitiveCases']>0 and result['retainedDeltaSensitiveCases']>0 and result['queryPrecisionSensitiveInputs']>0 and result['cachePrecisionSensitiveInputs']>=0 and result['unspilledShapingSensitiveCases']>0
  print(json.dumps(result,indent=2))
