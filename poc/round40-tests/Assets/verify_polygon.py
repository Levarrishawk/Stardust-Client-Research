"""Independent Fraction/integer BPOL ordered parity/distance/shaping oracle."""
import contextlib,importlib.util,itertools,json,random,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
c=load('circle_reference','poc/round36-tests/Rendering/verify_circle.py');a=c.a
raw=load('bpol_reference','poc/round37-tests/Assets/verify_bpol_input.py')
DEFAULT=dict(MaximumPoints=8192,MaximumSamples=65536,MaximumPointVisits=1048576)
def encode(f):
 h=raw.header(b'0001',name=b'synthetic',enabled=f['Enabled'])
 return raw.polygon(f['Version'].encode(),h,points=f['Points'],meta=[f['EncodedSelector'],f['Amount'],0xffffffff,0x7f812345,0x40000000]).hex()
def evaluate(f,spill_before_shape=False):
 cp,p=f['CachePrecision'],f['Precision'];lim=f['Limits'];points=f['Points'];n=len(points)
 if cp not in (24,53,64) or p not in (24,53,64):return dict(Reject='Options')
 if not 0<=lim['MaximumPoints']<=65536 or not 0<=lim['MaximumSamples']<=1048576 or not 0<=lim['MaximumPointVisits']<=67108864:return dict(Reject='Options')
 if f['Version'] not in ('0000','0001','0002','0003','0004','0005','0006'):return dict(Reject='Input')
 if n>lim['MaximumPoints'] or lim['MaximumSamples']<1:return dict(Reject='Input')
 if f['Action']=='evaluate' and not f['Enabled']:
  return dict(Reject='None',Skipped=True,Influence=None,Query=None,Branch=None,Bounds=None,Amount=None,Crossings=None,Square=None,Minimum=None,Projections=None,Charge=0)
 if 4*n>lim['MaximumPointVisits']:return dict(Reject='Input')
 amount=f['Amount'] if f['Version']>='0002' else 0;xy=f['Coordinates']
 if not all(a.finite(b) for point in points for b in point) or not all(a.finite(b) for b in xy+[amount]):return dict(Reject='Input')
 bounds=[0x7f7fffff,0x7f7fffff,0xff7fffff,0xff7fffff]
 for point in points:
  for j in (0,1):
   if a.scalar(point[j])[0]<a.scalar(bounds[j])[0]:bounds[j]=point[j]
   if a.scalar(point[j])[0]>a.scalar(bounds[j+2])[0]:bounds[j+2]=point[j]
 def add(x,y):return a.precision(a.add(x,y),p)
 def sub(x,y):return add(x,a.neg(y))
 def mul(x,y):return a.precision(a.mul(x,y),p)
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
  x,y=map(a.scalar,xy);av=a.scalar(amount);crossings=square=minimum_bits=projections=None
  contained=all(min(a.scalar(bounds[j])[0],a.scalar(bounds[j+2])[0])<=a.scalar(xy[j])[0]<=max(a.scalar(bounds[j])[0],a.scalar(bounds[j+2])[0]) for j in (0,1))
  if not contained:branch=0;u=a.scalar(0)
  else:
   crossings=0
   for i in range(n):
    cx,cy=map(a.scalar,points[i]);px,py=map(a.scalar,points[i-1])
    if cy[0]<=y[0]<py[0] or py[0]<=y[0]<cy[0]:
     intersection=add(div(mul(sub(y,cy),sub(px,cx)),sub(py,cy)),cx)
     if intersection[0]>x[0]:crossings+=1
   if not crossings&1:branch=1;u=a.scalar(0)
   elif not av[0]:branch=2;u=a.scalar(0x3f800000)
   else:
    minimum=mul(av,av);square=store(minimum)
    for point in points:
     vx,vy=map(a.scalar,point);minimum=select(minimum,dist(sub(x,vx),sub(y,vy)))
    projections=0
    for i in range(n):
     ax,ay=map(a.scalar,points[i-1]);bx,by=map(a.scalar,points[i])
     nx,ny,dy,dx=map(a.scalar,[store(sub(ax,bx)),store(sub(ay,by)),store(sub(by,ay)),store(sub(bx,ax))])
     dot=add(mul(sub(x,ax),dx),mul(sub(y,ay),dy));length=add(mul(ny,ny),mul(nx,nx))
     parameter=div(dot,length);t=a.scalar(store(parameter))
     if parameter[0]<0 or t[0]>1:continue
     projections+=1;qx=add(mul(dx,t),ax);qy=add(mul(dy,t),ay)
     minimum=select(minimum,dist(sub(x,qx),sub(y,qy)))
    epsilon=a.scalar(0x38d1b717)
    full=sub(a.scalar(square),epsilon)[0]<=minimum[0]<=add(a.scalar(square),epsilon)[0]
    u=a.scalar(0x3f800000) if full else div(c.sqrt_round(minimum,p),av);branch=4 if full else 3;minimum_bits=store(minimum)
  projection=store(u)
  if spill_before_shape:u=a.scalar(projection)
  sel=f['Selector']
  if sel==0:shaped=u
  elif sel==1:shaped=mul(u,u)
  elif sel==2:shaped=c.sqrt_round(u,p)
  elif sel==3:shaped=mul(mul(sub(a.scalar(0x40400000),add(u,u)),u),u)
  else:shaped=a.scalar(0)
  return dict(Reject='None',Skipped=False,Influence=store(shaped),Query=projection,Branch=branch,Bounds=bounds,Amount=amount,Crossings=crossings,Square=square,Minimum=minimum_bits,Projections=projections,Charge=4*n)
 except ValueError:return dict(Reject='Input')
def fixtures():
 out=[];b=c.fbits
 def emit(points,xy,amount=0,selector=0,enabled=1,version='0005',action='evaluate',cp=24,p=24,limits=None):
  f=dict(Name=str(len(out)),Points=[list(q) for q in points],Coordinates=list(xy),Amount=amount,Selector=selector,EncodedSelector=selector,Enabled=enabled,Version=version,Action=action,CachePrecision=cp,Precision=p,Limits=limits or DEFAULT)
  if action=='query':f['EncodedSelector']=0
  if version in ('0000','0001') and action=='evaluate':f['Selector']=0
  f['RuleHex']=encode(f);f['Expected']=evaluate(f);out.append(f)
 square=[(0,0),(b(2),0),(b(2),b(2)),(0,b(2))]
 shapes=[square,list(reversed(square)),[(0,0),(b(3),0),(b(1),b(3))],[(0,0),(b(3),0),(b(3),b(1)),(b(1),b(1)),(b(1),b(3)),(0,b(3))],[(0,0),(b(2),b(2)),(0,b(2)),(b(2),0)],square+[square[0]],square[:2]+[square[1]]+square[2:],[],[(0,0)],[(0,0),(b(2),b(2))],[(0,0),(b(1),b(1)),(b(2),b(2))],[(0x80000000,0),(0,0x80000000),(1,1)]]
 coords=[(0,0),(0x80000000,0),(b(.01),b(.01)),(b(.25),b(.5)),(b(1),b(1)),(b(2),b(1)),(b(1),b(2)),(b(2),b(2)),(b(2)-1,b(1)),(b(2)+1,b(1)),(b(-.01),b(.5))]
 for p in (24,53,64):
  for points in shapes:
   for xy in coords:
    for amt in (0,0x80000000,b(.1),b(.5),b(1),b(-.5),b(3),1):
     for sel in (0,1,2,3,0xffffffff):emit(points,xy,amt,sel,cp=p,p=p)
  for version in ('0000','0001','0002','0003','0004','0005','0006','0099'):
   for enabled in (0,1,0xffffffff):emit(square,(b(.1),b(.25)),b(.7),3,enabled,version,cp=p,p=p)
  for bad in (0x7f800000,0xff800000,0x7fc12345,0x7f812345):
   for enabled in (0,1):
    for location in range(5):
     points=list(square);xy=[b(.25),b(.25)];amt=b(.5)
     if location<2:points[0]=tuple(bad if j==location else points[0][j] for j in (0,1))
     elif location<4:xy[location-2]=bad
     else:amt=bad
     emit(points,xy,amt,2,enabled,cp=p,p=p)
   emit([(bad,0),*square[1:]],(0,0),0,2,0,action='query',cp=p,p=p)
  emit([(0xff7fffff,0),(0x7f7fffff,0),(0x7f7fffff,b(2)),(0xff7fffff,b(2))],(0,b(1)),b(.5),cp=p,p=p)
  emit(square,(b(.1),b(.1)),0x7f7fffff,cp=p,p=p)
 rng=random.Random(4001)
 for _ in range(45):
  scale=2.0**rng.randint(-70,60);ox,oy=rng.uniform(-3,3)*scale,rng.uniform(-3,3)*scale
  points=[(b(ox),b(oy)),(b(ox+scale),b(oy)),(b(ox+1.3*scale),b(oy+1.2*scale)),(b(ox+.2*scale),b(oy+1.8*scale))]
  xy=(b(ox+rng.uniform(.01,.7)*scale),b(oy+rng.uniform(.01,.7)*scale));amt=b(rng.uniform(.1,1)*scale)
  for cp in (24,53,64):
   for p in (24,53,64):
    for sel in (0,1,2,3):emit(points,xy,amt,sel,cp=cp,p=p)
 for bad in (0,23,25,52,54,63,65):
  emit(square,(0,0),enabled=0,p=bad);emit(square,(0,0),enabled=0,cp=bad)
 for cp in (24,53,64):
  for p in (24,53,64):
   for sel in (0,1,2,3,0xffffffff):emit(square,(b(.1),b(.2)),b(.7),sel,0,action='query',cp=cp,p=p)
 # Explicit lower/left boundary membership and tolerance-band straddles.
 for points in (square,list(reversed(square))):
  for p in (24,53,64):
   for xy in ((b(1),0),(0,b(1))):
    for amt in (0,b(.5),b(-.5)):
     for sel in (0,2,3):emit(points,xy,amt,sel,cp=p,p=p)
 for cp in (24,53,64):
  for p in (24,53,64):
   for delta in (-2,-1,0,1,2):
    for amt in (b(.5),b(-.5)):
     for sel in (0,2,3):emit(square,(b(.4999)+delta,b(1)),amt,sel,cp=cp,p=p)
 exact=dict(MaximumPoints=4,MaximumSamples=1,MaximumPointVisits=16)
 emit(square,(b(.25),b(.25)),b(.5),limits=exact)
 for key in exact:emit(square,(0,0),limits={**exact,key:exact[key]-1})
 for key,values in [('MaximumPoints',(-1,65537)),('MaximumSamples',(-1,1048577)),('MaximumPointVisits',(-1,67108865))]:
  for value in values:emit(square,(0,0),enabled=0,limits={**DEFAULT,key:value})
 emit(square,(0x7fc12345,0),0x7f800000,enabled=0,limits={**DEFAULT,'MaximumPointVisits':0})
 emit([], (0,0),limits=dict(MaximumPoints=0,MaximumSamples=1,MaximumPointVisits=0))
 emit([(0,0)]*8193,(0,0))
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
 rules=[r for r in m.metadata(generator)['Rules'] if r['Section']['FormType']=='BPOL'];assert len(rules)==57
 first=rules[0];part=first['Section'];start=part['Offset'];meta=raw.metadata(generator[start:start+part['EncodedBytes']])
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=42,Count=57,First=dict(LayerIndex=first['LayerIndex'],ChildOrdinal=first['ChildOrdinal'],Offset=start,Version=meta['Version'],HeaderVersion=meta['Header']['Version'],EnabledBits=meta['Header']['EnabledBits'],PointBits=[[p['FirstBits'],p['SecondBits']] for p in meta['Points']],MetadataWordBits=meta['MetadataWordBits'],EncodedBytes=meta['EncodedBytes'],Sha256=meta['Sha256']))
def status_checks():
 # Finite x87 C0/C3 flags: less=1, equal=0x40, greater=0; JP means even parity.
 status=lambda a,b:1 if a<b else 0x40 if a==b else 0
 jp=lambda ah,mask:(ah&mask).bit_count()%2==0
 count=0
 for cy,py,y in itertools.product(range(3),repeat=3):
  first=not jp(status(cy,y),0x41) and not jp(status(y,py),5)
  second=not jp(status(py,y),0x41) and not jp(status(y,cy),5)
  assert (first or second)==(cy<=y<py or py<=y<cy);count+=1
 for left,right in itertools.product(range(3),repeat=2):
  ah=status(left,right)
  assert (ah&0x41==0)==(left>right);count+=1
  assert (not jp(ah,5))==(left<right);count+=1
  assert jp(ah,5)==(left>=right);count+=1
  assert (ah&0x41!=0)==(left<=right);count+=1
 for lower_value,upper_projection in itertools.product((-1,0,1,2),repeat=2):
  skipped=(status(lower_value,0)&1)!=0 or jp(status(upper_projection,1),0x41)
  assert skipped==(lower_value<0 or upper_projection>1);count+=1
 for value in (-2,-1,0,1,2):
  full=(status(value,-1)&1)==0 and not jp(status(value,1),0x41)
  assert full==(-1<=value<=1);count+=1
 for value in (-1,0,1):
  assert (not jp(status(value,0),0x44))==(value==0);count+=1
 return count
def static(exe,packages):
 import hashlib,struct
 data=Path(exe).read_bytes();assert hashlib.sha256(data).hexdigest()==c.EXPECTED;sys.path.insert(0,packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 anchors={0xb738b0:('mov','eax, 0x7f7fffff'),0xb738bb:('mov','eax, 0xff7fffff'),0xb738e4:('jp','0xb738e9'),0xb738e6:('fst','dword ptr [ecx + 0x28]'),0xb738f3:('jp','0xb738fa'),0xb73902:('jne','0xb73909'),0xb73913:('jne','0xb7391a'),0xb73d31:('jne','0xb73da5'),0xb73d62:('jp','0xb73da5'),0xb73da5:('fld','dword ptr [0x15dc6a0]'),0xb73dbd:('jle','0xb73e5b'),0xb73dea:('jp','0xb73dfc'),0xb73dfa:('jnp','0xb73e1a'),0xb73e0c:('jp','0xb73e45'),0xb73e18:('jp','0xb73e45'),0xb73e1d:('fsub','dword ptr [esi]'),0xb73e22:('fsub','dword ptr [esi - 4]'),0xb73e25:('fmulp','st(1)'),0xb73e29:('fsub','dword ptr [esi]'),0xb73e2b:('fdivp','st(1)'),0xb73e2d:('fadd','dword ptr [esi - 4]'),0xb73e30:('fcomp','dword ptr [ebp + 8]'),0xb73e38:('jne','0xb73e45'),0xb73e3f:('sete','al'),0xb73e48:('lea','edi, [ecx + esi]'),0xb73e4b:('add','esi, 8'),0xb73e78:('test','ah, 0x44'),0xb73e7b:('jnp','0xb73fc3'),0xb73e89:('fmulp','st(1)'),0xb73e8d:('fst','dword ptr [ebp - 0x20]'),0xb73e92:('fsubr','dword ptr [ebp + 8]'),0xb73e98:('fsubr','dword ptr [ebp + 0xc]'),0xb73ea3:('faddp','st(1)'),0xb73ea5:('fst','dword ptr [ebp - 0x1c]'),0xb73eb3:('jp','0xb73eba'),0xb73eb7:('fld','dword ptr [ebp - 0x1c]'),0xb73ed3:('fstp','dword ptr [ebp - 8]'),0xb73ee1:('fstp','dword ptr [ebp - 0x10]'),0xb73ef0:('fstp','dword ptr [ebp - 0x14]'),0xb73ef8:('fstp','dword ptr [ebp - 0xc]'),0xb73efe:('fstp','dword ptr [ebp - 0x1c]'),0xb73f04:('fstp','dword ptr [ebp - 0x18]'),0xb73f19:('faddp','st(1)'),0xb73f27:('faddp','st(1)'),0xb73f29:('fdivp','st(1)'),0xb73f2b:('fst','dword ptr [ebp - 0xc]'),0xb73f2e:('fcomp','dword ptr [0x15dc6a0]'),0xb73f39:('jne','0xb73f82'),0xb73f3b:('fld','dword ptr [ebp - 0xc]'),0xb73f49:('jp','0xb73f82'),0xb73f4e:('fmul','dword ptr [ebp - 0xc]'),0xb73f5a:('fmul','dword ptr [ebp - 0xc]'),0xb73f6b:('faddp','st(1)'),0xb73f6d:('fst','dword ptr [ebp - 0x1c]'),0xb73f7b:('jp','0xb73f82'),0xb73f7f:('fld','dword ptr [ebp - 0x1c]'),0xb73f8e:('fsub','dword ptr [0x15e4f7c]'),0xb73f9d:('jne','0xb73fb3'),0xb73fa2:('fadd','dword ptr [0x15e4f7c]'),0xb73fb1:('jnp','0xb73fc1'),0xb73fb3:('fsqrt',''),0xb73fb7:('fdiv','dword ptr [ebx + 0x18]'),0xb73fc3:('fld','dword ptr [0x15dc6f8]'),0xb65fc6:('mov','dword ptr [ecx + 0x18], eax')}
 anchors.update({0xb73baa:('call','0xb736e0'),0xb63333:('mov','dword ptr [edx + 0x14], eax'),0xb63336:('mov','dword ptr [edx + 0x18], eax'),0xb63f8e:('call','0xb62f20'),0xb63faf:('call','dword ptr [edx + 0x24]'),0xb63fb2:('cmp','edi, 3'),0xb63fb5:('ja','0xb63fd8'),0xb63fc0:('fmulp','st(1)'),0xb63fc4:('fsqrt',''),0xb63fca:('fadd','st(1)'),0xb63fcc:('fsubr','dword ptr [0x15dd850]'),0xb63fd2:('fmul','st(1)'),0xb63fd4:('fmulp','st(1)'),0xb63fda:('fld','dword ptr [0x15dc6a0]'),0xb63fe0:('fst','dword ptr [ebp - 0x34]')})
 anchors.update({0xb73de7:('test','ah, 0x41'),0xb73df7:('test','ah, 5'),0xb73e09:('test','ah, 0x41'),0xb73e15:('test','ah, 5'),0xb73e35:('test','ah, 0x41'),0xb73eb0:('test','ah, 5'),0xb73f36:('test','ah, 1'),0xb73f46:('test','ah, 0x41'),0xb73f9a:('test','ah, 1'),0xb73fae:('test','ah, 0x41'),0xb738e1:('test','ah, 5'),0xb738f0:('test','ah, 5'),0xb738ff:('test','ah, 0x41'),0xb73910:('test','ah, 0x41')})
 for va,pair in anchors.items():
  ins=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (ins.mnemonic,ins.op_str)==pair,(hex(va),pair,(ins.mnemonic,ins.op_str))
 def instructions(start,end):return list(md.disasm(image[start-base:end-base],start))
 query=instructions(0xb73d00,0xb73fd0);assert not any(i.mnemonic=='call' for i in query)
 assert not any(i.mnemonic.startswith(('fadd','fsub','fmul','fdiv','fsqrt')) for i in instructions(0xb738b0,0xb73925))
 assert not any(i.mnemonic=='fst' or i.mnemonic=='fstp' for i in instructions(0xb73de0,0xb73e45))
 assert struct.unpack_from('<I',image,0x15e4f7c-base)[0]==0x38d1b717
 assert struct.unpack_from('<4I',image,0xb642a0-base)==(0xb63fe0,0xb63fbe,0xb63fc4,0xb63fc8)
 assert struct.unpack_from('<I',image,0x15dd850-base)[0]==0x40400000
 assert struct.unpack_from('<I',image,0x15ed458-base)[0]==0xb73d00 and struct.unpack_from('<I',image,0x15ed46c-base)[0]==0xb738b0
 return dict(sha256=c.EXPECTED,anchors=len(anchors),FiniteStatusCases=status_checks(),ToleranceBits=0x38d1b717,queryCalls=0,cacheArithmeticInstructions=0,rawInput=raw.static(Path(exe)))
if __name__=='__main__':
 if sys.argv[1]=='generate':
  fs=fixtures();Path(sys.argv[2]).write_text(json.dumps(dict(Cases=fs),separators=(',',':')));print('Polygon vectors',len(fs))
 else:
  native=json.loads(Path(sys.argv[2]).read_text());fs=fixtures();assert len(fs)==len(native['Cases'])
  for f,n in zip(fs,native['Cases']):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  assert native['OwnershipChecks']>=30 and native['ArgumentNegatives']==6
  expected=original(Path(sys.argv[5]),Path(sys.argv[6]));assert native['Original']==expected
  valid=[f for f in fs if f['Expected']['Reject']=='None' and not f['Expected']['Skipped']];groups={};cache_groups={}
  for f in valid:
   key=(tuple(map(tuple,f['Points'])),tuple(f['Coordinates']),f['Amount'],f['Selector'],f['Version'],f['Action'],f['Enabled'],f['EncodedSelector'],f['CachePrecision']);groups.setdefault(key,set()).add(f['Expected']['Influence'])
   key=key[:-1]+(f['Precision'],);cache_groups.setdefault(key,set()).add(tuple(f['Expected']['Bounds']))
  result=dict(passed=True,vectors=len(fs),rejections=sum(f['Expected']['Reject']!='None' for f in fs),OwnershipChecks=native['OwnershipChecks'],ArgumentNegatives=6,static=static(sys.argv[3],sys.argv[4]),queryPrecisionSensitiveInputs=sum(len(v)>1 for v in groups.values()),cachePrecisionSensitiveInputs=sum(len(v)>1 for v in cache_groups.values()),unspilledShapingSensitiveCases=sum(evaluate(f,True).get('Influence')!=f['Expected']['Influence'] for f in valid),originalRawOccurrences=1)
  assert result['queryPrecisionSensitiveInputs']>0 and result['cachePrecisionSensitiveInputs']==0 and result['unspilledShapingSensitiveCases']>0
  print(json.dumps(result,indent=2))
