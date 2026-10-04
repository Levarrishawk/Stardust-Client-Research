"""Independent FSLP raw-frame and Fraction/integer finite-query oracle."""
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
 return raw.form(b'FSLP',raw.form(v.encode(),(raw.header(b'0001',name=b'synthetic',enabled=enabled) if h is None else h)+raw.chunk(b'DATA',raw.words(values) if data is None else data)))
def snapshot(blob):
 v,=raw.records(blob,12,len(blob));tag=v[1][:4].decode();base=dict(Version=tag,ClientVersionRecognized=tag in ('0000','0001','0002'),HasDecodedFields=False,DecodedCopyBytes=0,Words=[],First=None,Second=None,Prefix=None,Selector=None,Amount=None,HeaderVersion=None,Enabled=None,NameSha=None,LegacySha=None,EncodedBytes=len(blob),Sha256=raw.sha(blob))
 if not base['ClientVersionRecognized']:return base
 hdr,d=raw.records(blob,v[2]+12,v[3]);hv,=raw.records(blob,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
 if ht not in ('0000','0001'):return base
 hd,=raw.records(blob,hv[2]+12,hv[3]);h=hd[1];end=h.index(0,4);name=h[4:end];legacy=h[end+1:];ws=list(struct.unpack('<'+'I'*(len(d[1])//4),d[1]));start=1 if tag=='0001' else 0
 base.update(HasDecodedFields=True,DecodedCopyBytes=len(d[1])+len(name)+len(legacy),Words=ws,First=ws[start],Second=ws[start+1],Prefix=ws[0] if tag=='0001' else None,Selector=ws[2] if tag=='0002' else None,Amount=ws[3] if tag=='0002' else None,HeaderVersion=ht,Enabled=struct.unpack_from('<I',h)[0],NameSha=raw.sha(name),LegacySha=raw.sha(legacy))
 return base
def fixtures():
 rs=[];b=c.fbits
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
 rawcase(raw.form(b'FSLP',raw.form(b'0002',raw.chunk(b'DATA',bytes(16))+raw.header())),'Input')
 rawcase(raw.form(b'FSLP',raw.form(b'0002',raw.header()+raw.chunk(b'DATA',bytes(16))+raw.chunk(b'XTRA',b'x'))),'Input')
 for key,values in [('MaximumInputBytes',[23,4194305]),('MaximumNodes',[0,65537]),('MaximumDepth',[0,65]),('MaximumStringBytes',[-1,65537]),('MaximumDecodedCopyBytes',[-1,4194305])]:
  for value in values:rawcase(encoded(),'Options',{**INPUT,key:value})
 return dict(RawCases=rs)
def original(root,cfg):
 assert m.sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 assert priority==42 and len(payload)==1188446 and m.sha(payload)==m.SOURCE
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);generator=payload[section[2]:section[3]]
 assert section[2]==210 and len(generator)==139608 and m.sha(generator)==m.GENERATOR
 rules=[r for r in m.metadata(generator)['Rules'] if r['Section']['FormType']=='FSLP'];first=None
 if rules:
  r=rules[0];s=r['Section'];start=s['Offset'];first=dict(LayerIndex=r['LayerIndex'],ChildOrdinal=r['ChildOrdinal'],Offset=start,Snapshot=snapshot(generator[start:start+s['EncodedBytes']]))
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=42,Count=len(rules),First=first)

ANCHORS={12028411: ('push', '2'), 12028415: ('push', '0x46534c50'), 12028423: ('call', '0xb633e0'), 12028436: ('mov', 'dword ptr [esi + 0x1c], edi'), 12028439: ('mov', 'dword ptr [esi + 0x20], edi'), 12028442: ('mov', 'dword ptr [esi + 0x24], edi'), 12028445: ('mov', 'dword ptr [esi + 0x28], edi'), 12028448: ('mov', 'dword ptr [esi], 0x15ed72c'), 12028454: ('call', '0xb78f40'), 12028462: ('call', '0xb78fb0'), 12028550: ('jmp', '0xb63410'), 12028566: ('mov', 'eax, dword ptr [ebp + 0x18]'), 12028569: ('mov', 'edx, dword ptr [eax + 0x3c]'), 12028584: ('mov', 'eax, dword ptr [edx]'), 12028586: ('imul', 'eax, dword ptr [ebp + 0x14]'), 12028590: ('mov', 'edx, dword ptr [edx + 8]'), 12028593: ('add', 'eax, dword ptr [ebp + 0x10]'), 12028596: ('lea', 'eax, [eax + eax*2]'), 12028599: ('fld', 'dword ptr [edx + eax*4 + 4]'), 12028606: ('fstp', 'dword ptr [ebp + 0x18]'), 12028620: ('test', 'ah, 0x41'), 12028623: ('jne', '0xb78ade'), 12028633: ('test', 'ah, 5'), 12028636: ('jnp', '0xb78aea'), 12028653: ('fsub', 'dword ptr [ebp + 0x14]'), 12028656: ('fmul', 'dword ptr [ebp - 8]'), 12028659: ('fmul', 'dword ptr [0x15dc5e4]'), 12028668: ('fadd', 'st(1)'), 12028670: ('fcomp', 'dword ptr [ebp + 0x18]'), 12028675: ('test', 'ah, 0x41'), 12028678: ('jne', '0xb78b16'), 12028686: ('fdivrp', 'st(1)'), 12028697: ('fsub', 'st(1)'), 12028699: ('fcomp', 'dword ptr [ebp + 0x18]'), 12028704: ('test', 'ah, 5'), 12028707: ('jp', '0xb78b33'), 12028715: ('fdivrp', 'st(1)'), 12028725: ('fld', 'dword ptr [0x15dc6f8]'), 12028779: ('sub', 'eax, 0x30303030'), 12028784: ('je', '0xb78bfd'), 12028791: ('je', '0xb78bed'), 12028794: ('je', '0xb78bdd'), 12028896: ('call', '0xb78d70'), 12028912: ('call', '0xb78cc0'), 12028928: ('call', '0xb78c10'), 12028973: ('call', '0xb62fa0'), 12029002: ('call', '0xa94f00'), 12029007: ('fld', 'dword ptr [0x15ed6d8]'), 12029013: ('fmul', 'dword ptr [ebp + 8]'), 12029019: ('fmul', 'dword ptr [0x15dc5f0]'), 12029025: ('fstp', 'dword ptr [esp]'), 12029028: ('call', '0xb78f40'), 12029041: ('call', '0xa94f00'), 12029064: ('fstp', 'dword ptr [esp]'), 12029067: ('call', '0xb78fb0'), 12029149: ('call', '0xb62fa0'), 12029178: ('call', '0xa94f00'), 12029191: ('call', '0xa94f00'), 12029214: ('fstp', 'dword ptr [esp]'), 12029217: ('call', '0xb78f40'), 12029230: ('call', '0xa94f00'), 12029253: ('fstp', 'dword ptr [esp]'), 12029256: ('call', '0xb78fb0'), 12029325: ('call', '0xb62fa0'), 12029354: ('call', '0xa94f00'), 12029377: ('fstp', 'dword ptr [esp]'), 12029380: ('call', '0xb78f40'), 12029393: ('call', '0xa94f00'), 12029416: ('fstp', 'dword ptr [esp]'), 12029419: ('call', '0xb78fb0'), 12029432: ('call', '0xa94f00'), 12029443: ('call', '0xb65fd0'), 12029456: ('call', '0xa94f00'), 12029472: ('test', 'ah, 5'), 12029475: ('jp', '0xb78e2e'), 12029477: ('mov', 'dword ptr [ebp + 8], 0'), 12029497: ('test', 'ah, 0x41'), 12029500: ('jne', '0xb78e47'), 12029502: ('mov', 'dword ptr [ebp + 8], 0x3f800000'), 12029525: ('call', 'dword ptr [eax + 0x10]'), 12029640: ('call', '0xa94870'), 12029671: ('call', '0xa94870'), 12029690: ('call', '0xa94870'), 12029709: ('call', '0xa94870'), 12029774: ('test', 'ah, 5'), 12029777: ('jp', '0xb78f6b'), 12029785: ('fst', 'dword ptr [ecx + 0x20]'), 12029794: ('fsin', ''), 12029796: ('fstp', 'dword ptr [ecx + 0x28]'), 12029814: ('test', 'ah, 0x41'), 12029817: ('jne', '0xb78f93'), 12029825: ('fst', 'dword ptr [ecx + 0x20]'), 12029834: ('fsin', ''), 12029836: ('fstp', 'dword ptr [ecx + 0x28]'), 12029846: ('fst', 'dword ptr [ecx + 0x20]'), 12029849: ('fsubr', 'dword ptr [0x1947228]'), 12029855: ('fsin', ''), 12029857: ('fstp', 'dword ptr [ecx + 0x28]'), 12029886: ('test', 'ah, 5'), 12029889: ('jp', '0xb78fdb'), 12029897: ('fst', 'dword ptr [ecx + 0x1c]'), 12029906: ('fsin', ''), 12029908: ('fstp', 'dword ptr [ecx + 0x24]'), 12029926: ('test', 'ah, 0x41'), 12029929: ('jne', '0xb79003'), 12029937: ('fst', 'dword ptr [ecx + 0x1c]'), 12029946: ('fsin', ''), 12029948: ('fstp', 'dword ptr [ecx + 0x24]'), 12029958: ('fst', 'dword ptr [ecx + 0x1c]'), 12029961: ('fsubr', 'dword ptr [0x1947228]'), 12029967: ('fsin', ''), 12029969: ('fstp', 'dword ptr [ecx + 0x24]'), 12034433: ('cmp', 'eax, 0x46534c50'), 12034456: ('push', '0x2c'), 12034482: ('call', '0xb789e0'), 12034505: ('call', 'dword ptr [eax + 8]'), 12023408: ('fld', 'dword ptr [0x15ed6d8]'), 12023414: ('fmul', 'dword ptr [0x15dc5e4]'), 12023420: ('fstp', 'dword ptr [0x1947228]')}
def static(exe,packages):
 sys.path.insert(0,packages);data=Path(exe).read_bytes();assert hashlib.sha256(data).hexdigest()==c.EXPECTED
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,pair in ANCHORS.items():
  i=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==pair,(hex(va),pair,(i.mnemonic,i.op_str))
 col=struct.unpack_from('<I',image,0x15ed728-base)[0];assert col==0x16263f0
 td=struct.unpack_from('<I',image,col-base+12)[0];name=b'.?AVFilterSlope@@\0';assert td==0x18b8ab0 and image[td-base+8:td-base+8+len(name)]==name
 assert struct.unpack_from('<8I',image,0x15ed72c-base)==(0xb78a50,0xb62f90,0xb78b50,0xb78e80,0xb65fe0,0xb78a90,0xb79020,0xb63430)
 for start,end,n in [(0xb78c10,0xb78cb0,2),(0xb78cc0,0xb78d6d,3),(0xb78d70,0xb78e78,4)]:
  ins=list(md.disasm(image[start-base:end-base],start));assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ins)==n
 query=list(md.disasm(image[0xb78a90-base:0xb78b3f-base],0xb78a90));assert not any(i.mnemonic=='call' for i in query)
 setters=list(md.disasm(image[0xb78f40-base:0xb79016-base],0xb78f40));assert sum(i.mnemonic=='fsin' for i in setters)==6
 constants={va:struct.unpack_from('<I',image,va-base)[0] for va in [0x15ed6d8,0x15dc5f0,0x15def20,0x1947228]};assert constants=={0x15ed6d8:0x40490fdb,0x15dc5f0:0x3bb60b61,0x15def20:0x43340000,0x1947228:0}
 status=lambda x,y:1 if x<y else 0x40 if x==y else 0;odd=lambda value:value.bit_count()%2==1;cases=0
 for x,y in itertools.product(range(3),repeat=2):
  assert bool(status(x,y)&0x41)==(x<=y);cases+=1
  assert odd(status(x,y)&5)==(x<y);cases+=1
 return dict(ExecutableSha256=m.EXE,InstructionAnchors=len(ANCHORS),FiniteStatusCases=cases,Vtable='015ED72C',Query='00B78A90',QueryCalls=0,SetterFsinSites=6,Constants={f'{k:08X}':f'{v:08X}' for k,v in constants.items()},ProductionInfluence=False,OriginalExecution=False)
if __name__=='__main__':
 if sys.argv[1]=='generate':
  fs=fixtures();Path(sys.argv[2]).write_text(json.dumps(fs,separators=(',',':')));print('FSLP raw cases',len(fs['RawCases']))
 else:
  native=json.loads(Path(sys.argv[2]).read_text());fs=fixtures();assert len(native['RawCases'])==len(fs['RawCases'])
  for f,n in zip(fs['RawCases'],native['RawCases']):assert n==dict(Name=f['Name'],**f['Expected']),(f,n)
  assert native['OwnershipChecks']>=15 and native['ArgumentNegatives']==1
  original_result=original(Path(sys.argv[5]),Path(sys.argv[6]));assert native['Original']==original_result
  print(json.dumps(dict(Passed=True,RawCases=len(fs['RawCases']),RawRejections=sum(f['Expected']['Reject']!='None' for f in fs['RawCases']),OwnershipChecks=native['OwnershipChecks'],ArgumentNegatives=1,Static=static(sys.argv[3],sys.argv[4]),OriginalRawOccurrences=1 if original_result['First'] is not None else 0),indent=2))
