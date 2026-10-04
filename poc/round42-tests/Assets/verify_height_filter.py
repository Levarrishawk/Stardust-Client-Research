"""Independent FHGT raw-frame and Fraction/integer finite-query oracle."""
import contextlib,importlib.util,json,struct,itertools,random,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
c=load('arithmetic','poc/round36-tests/Rendering/verify_circle.py');a=c.a
raw=load('frames','poc/round38-tests/Assets/verify_bpln_input.py');m=raw.m
INPUT=dict(MaximumInputBytes=65536,MaximumNodes=64,MaximumDepth=8,MaximumStringBytes=4096,MaximumDecodedCopyBytes=65536)
QUERY=dict(MaximumSamples=65536,MaximumQueryWork=131072)
def encoded(v='0002',lo=0,hi=0x40000000,selector=0,amount=0x3f000000,enabled=1,h=None,data=None):
 values=[lo,hi] if v=='0000' else [0xffffffff,lo,hi] if v=='0001' else [lo,hi,selector,amount]
 return raw.form(b'FHGT',raw.form(v.encode(),(raw.header(b'0001',name=b'synthetic',enabled=enabled) if h is None else h)+raw.chunk(b'DATA',raw.words(values) if data is None else data)))
def snapshot(blob):
 v,=raw.records(blob,12,len(blob));tag=v[1][:4].decode();base=dict(Version=tag,ClientVersionRecognized=tag in ('0000','0001','0002'),HasDecodedFields=False,DecodedCopyBytes=0,Words=[],Lower=None,Upper=None,Prefix=None,Selector=None,Amount=None,HeaderVersion=None,Enabled=None,NameSha=None,LegacySha=None,EncodedBytes=len(blob),Sha256=raw.sha(blob))
 if not base['ClientVersionRecognized']:return base
 hdr,d=raw.records(blob,v[2]+12,v[3]);hv,=raw.records(blob,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
 if ht not in ('0000','0001'):return base
 hd,=raw.records(blob,hv[2]+12,hv[3]);h=hd[1];end=h.index(0,4);name=h[4:end];legacy=h[end+1:];ws=list(struct.unpack('<'+'I'*(len(d[1])//4),d[1]));start=1 if tag=='0001' else 0
 base.update(HasDecodedFields=True,DecodedCopyBytes=len(d[1])+len(name)+len(legacy),Words=ws,Lower=ws[start],Upper=ws[start+1],Prefix=ws[0] if tag=='0001' else None,Selector=ws[2] if tag=='0002' else None,Amount=ws[3] if tag=='0002' else None,HeaderVersion=ht,Enabled=struct.unpack_from('<I',h)[0],NameSha=raw.sha(name),LegacySha=raw.sha(legacy))
 return base
def evaluate(f,premature=False):
 p=f['Precision'];limits=f['Limits']
 if p not in (24,53,64) or not 0<=limits['MaximumSamples']<=1048576 or not 0<=limits['MaximumQueryWork']<=2097152:return dict(Reject='Options')
 if f['Version'] not in ('0000','0001','0002'):return dict(Reject='Input')
 active=not(f['Action']=='evaluate' and not f['Enabled']);work=1 if f['Action']=='query' else 2 if active else 0
 if limits['MaximumSamples']<1 or limits['MaximumQueryWork']<work:return dict(Reject='Input')
 if not active:return dict(Reject='None',Skipped=True,Influence=None,Query=None,Branch=None,Amount=None,Span=None,Work=0)
 amount=f['Amount'] if f['Version']=='0002' else 0
 if not all(a.finite(b) for b in [f['Lower'],f['Upper'],f['Height'],amount]):return dict(Reject='Input')
 av=a.scalar(amount)
 if av[0]<0:amount=0
 elif av[0]>1:amount=0x3f800000
 def add(x,y):return a.precision(a.add(x,y),p)
 def sub(x,y):return add(x,a.neg(y))
 def mul(x,y):return a.precision(a.mul(x,y),p)
 def div(x,y):
  if not y[0]:raise ValueError('zero division')
  neg=(x[0]<0 or (not x[0] and x[1]))^(y[0]<0 or (not y[0] and y[1]))
  return a.precision((x[0]/y[0],neg if not x[0] else False),p)
 def store(x):
  bits=a.bits(x)
  if not a.finite(bits):raise ValueError('finite projection')
  return bits
 try:
  lo,hi,h=map(a.scalar,[f['Lower'],f['Upper'],f['Height']]);spanbits=None
  if h[0]<=lo[0] or h[0]>=hi[0]:u=a.scalar(0);branch=0
  else:
   span=mul(mul(sub(hi,lo),a.scalar(amount)),a.scalar(0x3f000000));spanbits=store(span)
   if add(lo,span)[0]>h[0]:u=div(sub(h,lo),span);branch=1
   elif sub(hi,span)[0]<h[0]:u=div(sub(hi,h),span);branch=2
   else:u=a.scalar(0x3f800000);branch=3
  projection=store(u)
  if premature:u=a.scalar(projection)
  selector=f['Selector'] if f['Action']=='query' or f['Version']=='0002' else 0
  if selector==0:shaped=u
  elif selector==1:shaped=mul(u,u)
  elif selector==2:shaped=c.sqrt_round(u,p)
  elif selector==3:shaped=mul(mul(sub(a.scalar(0x40400000),add(u,u)),u),u)
  else:shaped=a.scalar(0)
  return dict(Reject='None',Skipped=False,Influence=store(shaped),Query=projection,Branch=branch,Amount=amount,Span=spanbits,Work=work)
 except ValueError:return dict(Reject='Input')
def fixtures():
 rs=[];qs=[];b=c.fbits
 def rawcase(blob,reject='None',limits=None):
  rs.append(dict(Name='raw-'+str(len(rs)),Hex=blob.hex(),Limits=limits or INPUT,Expected=dict(Reject=reject,**({'Snapshot':snapshot(blob)} if reject=='None' else {}))))
 for v in ['0000','0001','0002']:
  blob=encoded(v)
  for hv in [b'0000',b'0001']:rawcase(encoded(v,h=raw.header(hv,name=b'\xff\x80',enabled=0xffffffff)))
  rawcase(blob)
  for n in range(len(blob)):rawcase(blob[:n],'Input')
  for n in range((2 if v=='0000' else 3 if v=='0001' else 4)*4):rawcase(encoded(v,data=bytes(n)),'Input')
  rawcase(encoded(v,data=raw.words([0]*((2 if v=='0000' else 3 if v=='0001' else 4)+1))),'Input')
  for bits in [0,0x80000000,1,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffffffff]:rawcase(encoded(v,lo=bits,hi=bits,selector=bits,amount=bits))
  exact={**INPUT,'MaximumInputBytes':len(blob),'MaximumStringBytes':9,'MaximumDecodedCopyBytes':snapshot(blob)['DecodedCopyBytes']};rawcase(blob,limits=exact)
  for key in ['MaximumInputBytes','MaximumStringBytes','MaximumDecodedCopyBytes']:rawcase(blob,'Input',{**exact,key:exact[key]-1})
  rawcase(blob,'Input',{**INPUT,'MaximumNodes':1});rawcase(blob,'Input',{**INPUT,'MaximumDepth':1})
 rawcase(encoded('0099'));rawcase(encoded(h=raw.form(b'IHDR',raw.form(b'0099',raw.chunk(b'DATA',b'opaque')))))
 rawcase(raw.form(b'WRNG',raw.form(b'0002',raw.header()+raw.chunk(b'DATA',bytes(16)))),'Input')
 rawcase(raw.form(b'FHGT',raw.form(b'0002',raw.chunk(b'DATA',bytes(16))+raw.header())),'Input')
 rawcase(raw.form(b'FHGT',raw.form(b'0002',raw.header()+raw.chunk(b'DATA',bytes(16))+raw.chunk(b'XTRA',b'x'))),'Input')
 for key,values in [('MaximumInputBytes',[23,4194305]),('MaximumNodes',[0,65537]),('MaximumDepth',[0,65]),('MaximumStringBytes',[-1,65537]),('MaximumDecodedCopyBytes',[-1,4194305])]:
  for value in values:rawcase(encoded(),'Options',{**INPUT,key:value})
 def query(lo,hi,h,amount=0x3f000000,selector=0,p=24,v='0002',enabled=1,action='evaluate',limits=None):
  f=dict(Name='query-'+str(len(qs)),Lower=lo,Upper=hi,Height=h,Amount=amount,Selector=selector,Precision=p,Version=v,Enabled=enabled,Action=action,Limits=limits or QUERY)
  f['Hex']=encoded(v,lo,hi,selector,amount,enabled).hex();f['Expected']=evaluate(f);qs.append(f)
 for lo,hi in [(0,b(2)),(b(2),0),(0,0),(0x80000000,0),(b(-2),b(2)),(1,9),(b(10000000),b(10000002)),(0xff7fffff,0x7f7fffff)]:
  for h in [lo,hi,0,b(.25),b(.5),b(1),b(1.5),b(1.75),b(3),1,2,8,0x80000000]:
   for amount in [0,0x80000000,b(.1),b(.5),b(1),b(-.5),b(2),1]:
    for selector in [0,1,2,3,0xffffffff]:
     for p in [24,53,64]:query(lo,hi,h,amount,selector,p)
 rng=random.Random(4203)
 for k in range(120):
  lo=rng.uniform(-100,100);hi=lo+rng.uniform(.001,100);height=rng.uniform(lo,hi);amount=rng.uniform(0,1)
  for p in [24,53,64]:
   for sel in [0,1,2,3]:query(b(lo),b(hi),b(height),b(amount),sel,p)
 for v in ['0000','0001','0002','0099']:
  for p in [24,53,64]:query(0,b(2),b(.25),selector=3,p=p,v=v)
 for bits in [0x7f800000,0xff800000,0x7fc12345]:
  query(bits,b(2),b(1));query(0,bits,b(1));query(0,b(2),bits);query(0,b(2),b(1),amount=bits)
  query(bits,bits,bits,amount=bits,enabled=0,limits={**QUERY,'MaximumQueryWork':0});query(bits,bits,bits,amount=bits,enabled=0,action='query')
 for lim in [{**QUERY,'MaximumSamples':0},{**QUERY,'MaximumQueryWork':1},{**QUERY,'MaximumQueryWork':2}]:query(0,b(2),b(1),limits=lim)
 for key,values in [('MaximumSamples',[-1,1048577]),('MaximumQueryWork',[-1,2097153])]:
  for value in values:query(0,b(2),b(1),limits={**QUERY,key:value})
 query(0,b(2),b(1),p=23);query(0,b(2),b(1),action='query',limits={**QUERY,'MaximumQueryWork':1})
 return dict(RawCases=rs,QueryCases=qs)
def original(root,cfg):
 assert m.sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 assert priority==42 and len(payload)==1188446 and m.sha(payload)==m.SOURCE
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);generator=payload[section[2]:section[3]]
 assert section[2]==210 and len(generator)==139608 and m.sha(generator)==m.GENERATOR
 rules=[r for r in m.metadata(generator)['Rules'] if r['Section']['FormType']=='FHGT'];first=None
 if rules:
  r=rules[0];s=r['Section'];start=s['Offset'];first=dict(LayerIndex=r['LayerIndex'],ChildOrdinal=r['ChildOrdinal'],Offset=start,Snapshot=snapshot(generator[start:start+s['EncodedBytes']]))
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=42,Count=len(rules),First=first)

ANCHORS={12023491: ('push', '0x46484754'), 12023498: ('call', '0xb633e0'), 12023503: ('mov', 'dword ptr [esi + 0x1c], 0'), 12023510: ('mov', 'dword ptr [esi + 0x20], 0'), 12023517: ('mov', 'dword ptr [esi], 0x15ed6e0'), 12023590: ('jmp', '0xb63410'), 11940851: ('mov', 'dword ptr [edx + 0x14], eax'), 11940854: ('mov', 'dword ptr [edx + 0x18], eax'), 11952086: ('mov', 'dword ptr [ecx + 0x14], eax'), 11952102: ('mov', 'dword ptr [ecx + 0x18], eax'), 12023606: ('mov', 'edx, dword ptr [ecx + 0x18]'), 12023609: ('mov', 'eax, dword ptr [ebp + 0x18]'), 12023612: ('mov', 'eax, dword ptr [eax + 0x18]'), 12023627: ('mov', 'edx, dword ptr [eax]'), 12023629: ('imul', 'edx, dword ptr [ebp + 0x14]'), 12023633: ('mov', 'eax, dword ptr [eax + 8]'), 12023636: ('add', 'edx, dword ptr [ebp + 0x10]'), 12023642: ('fld', 'dword ptr [eax + edx*4]'), 12023645: ('fstp', 'dword ptr [ebp + 0x18]'), 12023656: ('test', 'ah, 0x41'), 12023659: ('jne', '0xb7777a'), 12023669: ('test', 'ah, 5'), 12023672: ('jnp', '0xb77786'), 12023689: ('fsub', 'dword ptr [ebp + 0x14]'), 12023692: ('fmul', 'dword ptr [ebp - 8]'), 12023695: ('fmul', 'dword ptr [0x15dc5e4]'), 12023704: ('fadd', 'st(1)'), 12023706: ('fcomp', 'dword ptr [ebp + 0x18]'), 12023711: ('test', 'ah, 0x41'), 12023714: ('jne', '0xb777b2'), 12023719: ('fsub', 'dword ptr [ebp + 0x14]'), 12023722: ('fdivrp', 'st(1)'), 12023733: ('fsub', 'st(1)'), 12023735: ('fcomp', 'dword ptr [ebp + 0x18]'), 12023740: ('test', 'ah, 5'), 12023743: ('jp', '0xb777cf'), 12023748: ('fsub', 'dword ptr [ebp + 0x18]'), 12023751: ('fdivrp', 'st(1)'), 12023759: ('fstp', 'st(0)'), 12023761: ('fld', 'dword ptr [0x15dc6f8]'), 12023803: ('sub', 'eax, 0x30303030'), 12023808: ('je', '0xb7788d'), 12023815: ('je', '0xb7787d'), 12023818: ('je', '0xb7786d'), 12023920: ('call', '0xb779b0'), 12023936: ('call', '0xb77920'), 12023952: ('call', '0xb778a0'), 12023997: ('call', '0xb62fa0'), 12024026: ('call', '0xa94f00'), 12024039: ('mov', 'dword ptr [edi + 0x1c], ecx'), 12024045: ('call', '0xa94f00'), 12024062: ('mov', 'dword ptr [edi + 0x20], eax'), 12024125: ('call', '0xb62fa0'), 12024154: ('call', '0xa94f00'), 12024167: ('call', '0xa94f00'), 12024183: ('mov', 'dword ptr [edi + 0x1c], edx'), 12024186: ('call', '0xa94f00'), 12024196: ('mov', 'dword ptr [edi + 0x20], ecx'), 12024269: ('call', '0xb62fa0'), 12024298: ('call', '0xa94f00'), 12024311: ('mov', 'dword ptr [edi + 0x1c], ecx'), 12024317: ('call', '0xa94f00'), 12024333: ('mov', 'dword ptr [edi + 0x20], eax'), 12024336: ('call', '0xa94f00'), 12024347: ('call', '0xb65fd0'), 12024360: ('call', '0xa94f00'), 12024376: ('test', 'ah, 5'), 12024379: ('jp', '0xb77a46'), 12024381: ('mov', 'dword ptr [ebp + 8], 0'), 12024401: ('test', 'ah, 0x41'), 12024404: ('jne', '0xb77a5f'), 12024406: ('mov', 'dword ptr [ebp + 8], 0x3f800000'), 12024429: ('call', 'dword ptr [edx + 0x10]'), 12024534: ('call', '0xa94870'), 12024547: ('call', '0xa94870'), 12024566: ('call', '0xa94870'), 12024585: ('call', '0xa94870'), 12024630: ('mov', 'dword ptr [ecx + 0x1c], eax'), 12024646: ('mov', 'dword ptr [ecx + 0x20], eax'), 12034058: ('cmp', 'eax, 0x46484754'), 12034362: ('push', '0x24'), 12034388: ('call', '0xb776c0'), 12034411: ('call', 'dword ptr [edx + 8]'), 11944102: ('push', 'ebx'), 11944137: ('fmulp', 'st(1)'), 11944141: ('fsqrt', ''), 11944147: ('fadd', 'st(1)'), 11944149: ('fsubr', 'dword ptr [0x15dd850]'), 11944155: ('fmul', 'st(1)'), 11944157: ('fmulp', 'st(1)')}
def static(exe,packages):
 sys.path.insert(0,packages);data=Path(exe).read_bytes();assert hashlib.sha256(data).hexdigest()==c.EXPECTED
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,pair in ANCHORS.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==pair,(hex(va),pair,(i.mnemonic,i.op_str))
 col=struct.unpack_from('<I',image,0x15ed6dc-base)[0];assert col==0x1626358
 descriptor=struct.unpack_from('<I',image,col-base+12)[0];assert descriptor==0x18b8a78 and image[descriptor-base+8:descriptor-base+8+len(b'.?AVFilterHeight@@\0')]==b'.?AVFilterHeight@@\0'
 assert struct.unpack_from('<6I',image,0x15ed6e0-base)==(0xb776f0,0xb62f90,0xb777e0,0xb77aa0,0xb65fe0,0xb77730)
 for start,end,n in [(0xb778a0,0xb77918,2),(0xb77920,0xb779a5,3),(0xb779b0,0xb77a90,4)]:
  ins=list(md.disasm(image[start-base:end-base],start));assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ins)==n
 query=list(md.disasm(image[0xb77730-base:0xb777db-base],0xb77730));assert not any(i.mnemonic=='call' for i in query)
 assert sum(i.mnemonic=='fstp' for i in query)==2
 assert struct.unpack_from('<I',image,0x15dc5e4-base)[0]==0x3f000000
 assert struct.unpack_from('<4I',image,0xb642b0-base)==(0xb640e9,0xb640c7,0xb640cd,0xb640d1)
 # Direct shared height-grid source and query-before-affector argument order.
 for va,pair in {0xb7657b:('mov','eax, dword ptr [ebx + 0x18]'),0xb76586:('mov','eax, dword ptr [eax + 8]'),0xb6417a:('push','ebx'),0xb6418c:('call','dword ptr [edx + 0x10]'),0xb640b8:('call','dword ptr [edx + 0x14]')}.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==pair
 status=lambda x,y:1 if x<y else 0x40 if x==y else 0
 odd=lambda x:x.bit_count()%2==1;cases=0
 for x,y in itertools.product(range(3),repeat=2):
  assert bool(status(x,y)&0x41)==(x<=y);cases+=1
  assert odd(status(x,y)&5)==(x<y);cases+=1
 return dict(ExecutableSha256=m.EXE,InstructionAnchors=len(ANCHORS),HeightSourceCallerAnchors=5,FiniteStatusCases=cases,Vtable='015ED6E0',Query='00B77730',QueryCalls=0,OriginalExecution=False)
if __name__=='__main__':
 if sys.argv[1]=='generate':
  fs=fixtures();Path(sys.argv[2]).write_text(json.dumps(fs,separators=(',',':')));print('FHGT',len(fs['RawCases']),'raw;',len(fs['QueryCases']),'queries')
 else:
  native=json.loads(Path(sys.argv[2]).read_text());fs=fixtures()
  for key in ['RawCases','QueryCases']:
   assert len(native[key])==len(fs[key])
   for f,n in zip(fs[key],native[key]):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  assert native['OwnershipChecks']>=20 and native['ArgumentNegatives']==5
  original_result=original(Path(sys.argv[5]),Path(sys.argv[6]));assert native['Original']==original_result
  groups={};valid=[]
  for f in fs['QueryCases']:
   if f['Expected']['Reject']=='None' and not f['Expected']['Skipped']:
    valid.append(f);key=(f['Lower'],f['Upper'],f['Height'],f['Amount'],f['Selector'],f['Version'],f['Enabled'],f['Action']);groups.setdefault(key,set()).add(f['Expected']['Influence'])
  result=dict(Passed=True,RawCases=len(fs['RawCases']),RawRejections=sum(f['Expected']['Reject']!='None' for f in fs['RawCases']),QueryCases=len(fs['QueryCases']),QueryRejections=sum(f['Expected']['Reject']!='None' for f in fs['QueryCases']),OwnershipChecks=native['OwnershipChecks'],ArgumentNegatives=5,Static=static(sys.argv[3],sys.argv[4]),QueryPrecisionSensitiveInputs=sum(len(v)>1 for v in groups.values()),PrematureProjectionCases=sum(evaluate(f,True)!=f['Expected'] for f in valid),OriginalRawOccurrences=1 if original_result['First'] is not None else 0)
  print(json.dumps(result,indent=2))
