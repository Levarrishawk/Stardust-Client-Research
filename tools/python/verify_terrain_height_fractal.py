"""AHFR/0003 independent raw-byte and finite Fraction consumer oracle; static identity gated."""
import contextlib,importlib.util,json,struct,random,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
c=load('circle','poc/round36-tests/Rendering/verify_circle.py');a=c.a
raw=load('frames','poc/round38-tests/Assets/verify_bpln_input.py');m=raw.m
INPUT=dict(MaximumInputBytes=65536,MaximumNodes=64,MaximumDepth=8,MaximumStringBytes=4096,MaximumDecodedCopyBytes=65536)
def encoded(family=0,op=0,scale=0x3f800000,enabled=1,v='0003',h=None,data=None):
 payload=raw.words([family,op,scale]) if data is None else data
 child=raw.form(b'DATA',raw.chunk(b'PARM',payload))
 return raw.form(b'AHFR',raw.form(v.encode(),(raw.header(b'0001',name=b'synthetic',enabled=enabled) if h is None else h)+child))
def snapshot(blob):
 v,=raw.records(blob,12,len(blob));tag=v[1][:4].decode();base=dict(Version=tag,ClientVersionRecognized=tag in ('0000','0001','0002','0003'),HasDecodedFields=False,DecodedCopyBytes=0,Words=[],Family=None,Operation=None,Scale=None,HeaderVersion=None,Enabled=None,NameSha=None,LegacySha=None,EncodedBytes=len(blob),Sha256=raw.sha(blob))
 if tag!='0003':return base
 hdr,df=raw.records(blob,v[2]+12,v[3]);hv,=raw.records(blob,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
 if ht not in ('0000','0001'):return base
 hd,=raw.records(blob,hv[2]+12,hv[3]);h=hd[1];end=h.index(0,4);name=h[4:end];legacy=h[end+1:];parm,=raw.records(blob,df[2]+12,df[3]);ws=list(struct.unpack('<3I',parm[1]))
 base.update(HasDecodedFields=True,DecodedCopyBytes=12+len(name)+len(legacy),Words=ws,Family=ws[0],Operation=ws[1],Scale=ws[2],HeaderVersion=ht,Enabled=struct.unpack_from('<I',h)[0],NameSha=raw.sha(name),LegacySha=raw.sha(legacy))
 return base

def evaluate(f,spill=False):
 p=f['Precision'];h=f['Height'];w=f['Influence'];x=f['Fractal'];scale=f['Scale'];op=f['Operation']
 if p not in (24,53,64):return dict(Reject='Options')
 if not f['Enabled']:return dict(Reject='None',Output=h,Outcome=1)
 if not a.finite(w):return dict(Reject='Input')
 if a.scalar(w)[0]<=0:return dict(Reject='None',Output=h,Outcome=2)
 if not all(a.finite(v) for v in [h,x,scale]):return dict(Reject='Input')
 add=lambda x,y:a.precision(a.add(x,y),p)
 sub=lambda x,y:add(x,a.neg(y))
 mul=lambda x,y:a.precision(a.mul(x,y),p)
 hh,ww=map(a.scalar,[h,w]);t=mul(a.scalar(x),a.scalar(scale))
 if spill:
  tb=a.bits(t)
  if not a.finite(tb):return dict(Reject='Input')
  t=a.scalar(tb)
 if op==4:return dict(Reject='None',Output=h,Outcome=3)
 if op==1:r=add(hh,mul(t,ww))
 elif op==2:r=sub(hh,mul(t,ww))
 elif op==3:r=add(hh,mul(sub(mul(t,hh),hh),ww))
 else:r=add(hh,mul(sub(t,hh),ww))
 out=a.bits(r)
 return dict(Reject='None',Output=out,Outcome=0) if a.finite(out) else dict(Reject='Input')

def fixtures():
 rs=[];qs=[]
 def r(blob,reject='None',limits=None):rs.append(dict(Name='raw-'+str(len(rs)),Hex=blob.hex(),Limits=limits or INPUT,Expected=dict(Reject=reject,**({'Snapshot':snapshot(blob)} if reject=='None' else {}))))
 blob=encoded();r(blob)
 for n in range(len(blob)):r(blob[:n],'Input')
 for n in range(12):r(encoded(data=bytes(n)),'Input')
 r(encoded(data=bytes(16)),'Input')
 for v in ['0000','0001','0002','0099']:r(encoded(v=v))
 for hv in [b'0000',b'0001']:r(encoded(h=raw.header(hv,name=b'\xff\x80',enabled=0xffffffff)))
 r(encoded(h=raw.form(b'IHDR',raw.form(b'0099',raw.chunk(b'DATA',b'opaque')))))
 for bits in [0,0x80000000,1,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffffffff]:r(encoded(bits,bits,bits))
 exact={**INPUT,'MaximumInputBytes':len(blob),'MaximumStringBytes':9,'MaximumDecodedCopyBytes':21};r(blob,limits=exact)
 for key in ['MaximumInputBytes','MaximumStringBytes','MaximumDecodedCopyBytes']:r(blob,'Input',{**exact,key:exact[key]-1})
 for key,values in [('MaximumInputBytes',[23,4194305]),('MaximumNodes',[0,65537]),('MaximumDepth',[0,65]),('MaximumStringBytes',[-1,65537]),('MaximumDecodedCopyBytes',[-1,4194305])]:
  for value in values:r(blob,'Options',{**INPUT,key:value})
 for limits in [{**INPUT,'MaximumNodes':1},{**INPUT,'MaximumDepth':1}]:r(blob,'Input',limits)
 for bad in [raw.form(b'WRNG',raw.form(b'0003',raw.header())),raw.form(b'AHFR',raw.form(b'0003',raw.header()+raw.chunk(b'DATA',bytes(12)))),raw.form(b'AHFR',raw.form(b'0003',raw.form(b'DATA',raw.chunk(b'PARM',bytes(12)))+raw.header())),raw.form(b'AHFR',raw.form(b'0003',raw.header()+raw.form(b'DATA',raw.chunk(b'WRNG',bytes(12)))))]:r(bad,'Input')
 def q(h,w,x,s,op,p=24,enabled=1):
  f=dict(Name='query-'+str(len(qs)),Height=h,Influence=w,Fractal=x,Scale=s,Operation=op,Precision=p,Enabled=enabled,RuleHex=encoded(7,op,s,enabled).hex());f['Expected']=evaluate(f);qs.append(f)
 values=[0,0x80000000,1,0x00800000,0x3f000000,0x3f7fffff,0x3f800000,0x3f800001,0xbf800000,0x40000000,0x4b800000,0x7f7fffff]
 for p in [24,53,64]:
  for op in [0,1,2,3,4,5,0xffffffff]:
   for i,h in enumerate(values):
    for w in [0,0x80000000,0xbf800000,0x3e800000,0x3f800000,0x40000000]:q(h,w,values[-i-1],values[(i+3)%len(values)],op,p)
   q(0x3f800000,0x4b800000,0x3f800001,0x3f7fffff,op,p)
  for bad in [0x7f800000,0xff800000,0x7fc12345,0x7f800001]:
   for enabled in [0,1]:
    for index in range(4):
     args=[0x3f800000,0x3f000000,0x3f800000,0x3f800000];args[index]=bad;q(*args,0,p,enabled)
 for p in [0,23,25,52,54,63,65]:q(0,0,0,0,0,p,0)
 rng=random.Random(4501)
 for i in range(400):
  args=[rng.getrandbits(32)&0xfeffffff for _ in range(4)]
  for p in [24,53,64]:
   for op in [0,1,2,3,4,0xffffffff]:q(*args,op,p)
 return dict(RawCases=rs,Queries=qs)

def original(root,cfg):
 assert m.sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 assert priority==42 and len(payload)==1188446 and m.sha(payload)==m.SOURCE
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);generator=payload[section[2]:section[3]]
 assert section[2]==210 and len(generator)==139608 and m.sha(generator)==m.GENERATOR
 rules=[r for r in m.metadata(generator)['Rules'] if r['Section']['FormType']=='AHFR'];first=None
 if rules:
  r=rules[0];s=r['Section'];start=s['Offset'];first=dict(LayerIndex=r['LayerIndex'],ChildOrdinal=r['ChildOrdinal'],Offset=start,Snapshot=snapshot(generator[start:start+s['EncodedBytes']]))
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=42,Count=len(rules),First=first)


ANCHORS={12019683: ('push', '0x41484652'), 12019690: ('call', '0xb63440'), 12019697: ('mov', 'dword ptr [esi + 0x14], eax'), 12019706: ('mov', 'dword ptr [esi + 0x18], 0xffffffff'), 12019713: ('mov', 'dword ptr [esi + 0x20], 0x3f800000'), 12019720: ('mov', 'dword ptr [esi], 0x15ed698'), 12019798: ('mov', 'dword ptr [ecx + 0x14], 0'), 12019805: ('jmp', '0xb63460'), 12019830: ('mov', 'dword ptr [ecx + 0x24], eax'), 12019846: ('mov', 'dword ptr [ecx + 0x1c], eax'), 12019862: ('mov', 'dword ptr [ecx + 0x20], eax'), 12019875: ('fld', 'dword ptr [ebp + 0x18]'), 12019879: ('fcomp', 'dword ptr [0x15dc6a0]'), 12019885: ('mov', 'esi, ecx'), 12019887: ('fnstsw', 'ax'), 12019889: ('test', 'ah, 0x41'), 12019892: ('jne', '0xb76962'), 12019898: ('mov', 'eax, dword ptr [esi + 0x1c]'), 12019901: ('cmp', 'dword ptr [esi + 0x18], eax'), 12019904: ('je', '0xb768d4'), 12019906: ('mov', 'dword ptr [esi + 0x18], eax'), 12019910: ('mov', 'eax, dword ptr [ebp + 0x1c]'), 12019913: ('mov', 'ecx, dword ptr [eax + 0x58]'), 12019916: ('call', '0xb75b70'), 12019921: ('mov', 'dword ptr [esi + 0x14], eax'), 12019924: ('mov', 'ecx, dword ptr [ebp + 0xc]'), 12019927: ('mov', 'edx, dword ptr [ebp + 8]'), 12019931: ('mov', 'ebx, dword ptr [ebp + 0x10]'), 12019935: ('mov', 'edi, dword ptr [ebp + 0x14]'), 12019941: ('mov', 'ecx, dword ptr [esi + 0x14]'), 12019945: ('call', '0x13e7c00'), 12019950: ('fmul', 'dword ptr [esi + 0x20]'), 12019953: ('mov', 'eax, dword ptr [ebp + 0x1c]'), 12019956: ('mov', 'eax, dword ptr [eax + 0x18]'), 12019959: ('mov', 'ecx, dword ptr [eax]'), 12019961: ('mov', 'edx, dword ptr [eax + 8]'), 12019967: ('mov', 'esi, dword ptr [esi + 0x24]'), 12019975: ('cmp', 'eax, 3'), 12019978: ('fld', 'dword ptr [edx + ecx*4]'), 12019981: ('fst', 'dword ptr [ebp + 0x14]'), 12019984: ('ja', '0xb76944'), 12019986: ('jmp', 'dword ptr [eax*4 + 0xb76968]'), 12019993: ('fxch', 'st(1)'), 12019995: ('jmp', '0xb76948'), 12019997: ('fxch', 'st(1)'), 12019999: ('fmul', 'dword ptr [ebp + 0x18]'), 12020002: ('fsubp', 'st(1)'), 12020004: ('jmp', '0xb7694d'), 12020006: ('fld', 'st(0)'), 12020008: ('fmulp', 'st(2)'), 12020010: ('fxch', 'st(1)'), 12020012: ('jmp', '0xb76946'), 12020014: ('fstp', 'st(0)'), 12020021: ('fstp', 'st(0)'), 12020023: ('call', '0xaa1d80'), 12020028: ('fld', 'dword ptr [ebp + 0x14]'), 12020034: ('jmp', '0xb7694d'), 12020036: ('fxch', 'st(1)'), 12020038: ('fsub', 'st(1)'), 12020040: ('fmul', 'dword ptr [ebp + 0x18]'), 12020043: ('faddp', 'st(1)'), 12020045: ('mov', 'eax, dword ptr [ebp + 0x1c]'), 12020048: ('mov', 'eax, dword ptr [eax + 0x18]'), 12020051: ('mov', 'ecx, dword ptr [eax]'), 12020053: ('mov', 'edx, dword ptr [eax + 8]'), 12020063: ('fstp', 'dword ptr [edx + ecx*4]'), 12020096: ('mov', 'al, 1'), 12020139: ('add', 'eax, 0xcfcfcfd0'), 12020144: ('cmp', 'eax, 3'), 12020163: ('call', '0xb76a80'), 12020183: ('call', '0xb76bc0'), 12020203: ('call', '0xb76dc0'), 12020219: ('call', '0xb76ee0'), 12021291: ('call', '0x13e8410'), 12021313: ('call', '0xb76040'), 12021361: ('call', '0xb76870'), 12021385: ('call', '0xb76890'), 12021526: ('push', '0x5041524d'), 12021557: ('call', '0xb76880'), 12021581: ('call', '0xb76870'), 12021605: ('call', '0xb76890'), 12016503: ('call', '0xb75de0'), 12016510: ('call', '0xb757a0'), 20872323: ('fld', 'dword ptr [ebx + 0xc]'), 12021738: ('call', '0xa94870'), 12021757: ('call', '0xa94870'), 12021776: ('call', '0xa94870'), 12017142: ('je', '0xb75dff'), 12017144: ('mov', 'eax, dword ptr [eax + 0x14]'), 12017151: ('xor', 'eax, eax'), 12015520: ('lea', 'eax, [ecx + 8]'), 20873189: ('fst', 'dword ptr [ebx + 0xc]'), 12035734: ('cmp', 'eax, 0x41484652'), 12035757: ('push', '0x28'), 12035783: ('call', '0xb767e0'), 12035813: ('call', 'dword ptr [eax + 0x1c]')}
def identity(data):
 if hashlib.sha256(data).hexdigest()!=c.EXPECTED:raise ValueError("Executable identity mismatch before VA interpretation")

def static(exe,packages):
 data=Path(exe).read_bytes();identity(data)
 sys.path.insert(0,packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,pair in ANCHORS.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==pair,(hex(va),pair,(i.mnemonic,i.op_str))
 col=struct.unpack_from('<I',image,0x15ed694-base)[0];assert col==0x16262c0
 td=struct.unpack_from('<I',image,col-base+12)[0];name=b'.?AVAffectorHeightFractal@@\0';assert td==0x18b8a30 and image[td-base+8:td-base+8+len(name)]==name
 assert struct.unpack_from('<8I',image,0x15ed698-base)==(0xb76820,0xb62f90,0xb62fa0,0xb76fa0,0xb768a0,0xb76980,0xb63480,0xb76990)
 assert struct.unpack_from('<4I',image,0xb76a6c-base)==(0xb769bc,0xb769d0,0xb769e4,0xb769f8)
 assert struct.unpack_from('<4I',image,0xb76968-base)==(0xb76919,0xb7691d,0xb76926,0xb7692e)
 assert struct.unpack_from('<I',image,0x15dc6a0-base)[0]==0
 ins=list(md.disasm(image[0xb76ee0-base:0xb76f93-base],0xb76ee0));assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ins)==3
 negative=0
 for offset in [0,1,len(data)//2,len(data)-1]:
  changed=bytearray(data);changed[offset]^=1
  try:identity(changed)
  except ValueError:negative+=1
  else:raise AssertionError('wrong identity accepted')
 for changed in [b'',data[:-1],data+b'\0',bytes(64)]:
  try:identity(changed)
  except ValueError:negative+=1
  else:raise AssertionError('wrong identity accepted')
 return dict(ExecutableSha256=m.EXE,InstructionAnchors=len(ANCHORS),WrongIdentityNegatives=negative,Vtable='015ED698',Process='00B768A0',TypedVersion='0003',ParmWords=3,OperationTargets=[f'{v:08X}' for v in struct.unpack_from('<4I',image,0xb76968-base)],OriginalExecution=False,OriginalSamplerParity=False)

if __name__=='__main__':
 if sys.argv[1]=='generate':
  fs=fixtures();Path(sys.argv[2]).write_text(json.dumps(fs,separators=(',',':')));print('AHFR fixtures',len(fs['RawCases']),len(fs['Queries']))
 else:
  native=json.loads(Path(sys.argv[2]).read_text());fs=fixtures()
  assert len(native['RawCases'])==len(fs['RawCases']) and len(native['Queries'])==len(fs['Queries'])
  for f,n in zip(fs['RawCases'],native['RawCases']):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  for f,n in zip(fs['Queries'],native['Queries']):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  assert native['OwnershipChecks']>=30 and native['ArgumentNegatives']==1
  original_result=original(Path(sys.argv[5]),Path(sys.argv[6]));assert native['Original']==original_result
  witnesses=sum(evaluate(f)!=evaluate(f,True) for f in fs['Queries'] if f['Expected']['Reject']=='None');assert witnesses>0
  print(json.dumps(dict(Passed=True,RawCases=len(fs['RawCases']),RawRejections=sum(f['Expected']['Reject']!='None' for f in fs['RawCases']),SuppliedConsumers=len(fs['Queries']),ConsumerRejections=sum(f['Expected']['Reject']!='None' for f in fs['Queries']),PrematureTargetSpillWitnesses=witnesses,OwnershipChecks=native['OwnershipChecks'],ArgumentNegatives=1,Static=static(sys.argv[3],sys.argv[4]),OriginalRawOccurrences=1,OriginalFirst=original_result['First']),indent=2))
