"""Independent BCIR bytes, synthetic failures, client anchors and one pinned input."""
import contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
p=Path(__file__).resolve().parents[2]/'round33-tests/Assets/verify_tgen_input.py'
s=importlib.util.spec_from_file_location('tgen',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
sha=m.sha;form=m.form;chunk=m.chunk;words=m.words;header=m.header;records=m.records
DEFAULT=dict(MaximumInputBytes=65536,MaximumNodes=64,MaximumDepth=8,MaximumStringBytes=4096)
def circle(v=b'0002',h=None,data=None):
 w=[0x80000000,0x7fc12345,0xbf800000,0xffffffff,0x7f800000] if v==b'0002' else [0x80000000,0x3f800000,0xbf800000]
 if v==b'0001':w=[0xdeadbeef]+w
 return form(b'BCIR',form(v,(header(b'0000') if h is None else h)+chunk(b'DATA',words(w) if data is None else data)))
def fixtures():
 out=[]
 def add(n,b,reject='None',limits=None):out.append(dict(Name=n,Hex=b.hex().upper(),Reject=reject,Limits=limits or DEFAULT))
 full=circle();add('owned-raw',full)
 for v in (b'0000',b'0001',b'0002'):
  for hv in (b'0000',b'0001'):add(v.decode()+'-'+hv.decode(),circle(v,header(hv,name=b'\xff\x80')))
  n={b'0000':3,b'0001':4,b'0002':5}[v]
  for bits in (0,0x80000000,1,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffffffff):add('raw-'+v.decode()+'-'+str(bits),circle(v,data=words([bits]*n)))
  for size in range(n*4+2):
   if size!=n*4:add('data-'+v.decode()+'-'+str(size),circle(v,data=bytes(size)),'Input')
  add('wrong-order-'+v.decode(),form(b'BCIR',form(v,chunk(b'DATA',bytes(n*4))+header())),'Input')
  add('extra-data-'+v.decode(),form(b'BCIR',form(v,header()+chunk(b'DATA',bytes(n*4))+chunk(b'XTRA',b'x'))),'Input')
 add('opaque-version',circle(b'0099'));add('opaque-empty-version',form(b'BCIR',form(b'ABCD')))
 add('opaque-header',circle(h=header(b'0099',raw=b'anything')))
 add('opaque-nested',form(b'BCIR',form(b'0099',form(b'ZZZZ',chunk(b'DATA',b'\x80\0')))))
 add('empty-name',circle(h=header(name=b'')))
 add('disabled',circle(h=header(enabled=0)))
 for i in range(len(full)):add('truncation-'+str(i),full[:i],'Input')
 for n,b in [('trailing',full+b'\0'),('two-roots',full+full),('wrong-root',form(b'BREC',full[12:])),('no-version',form(b'BCIR')),('two-versions',form(b'BCIR',form(b'0002')+form(b'0002'))),('leaf-version',form(b'BCIR',chunk(b'0002',b''))),('missing-header',form(b'BCIR',form(b'0002',chunk(b'DATA',bytes(20))))),('form-data',form(b'BCIR',form(b'0002',header()+form(b'DATA')))),('wrong-data-tag',form(b'BCIR',form(b'0002',header()+chunk(b'ADTA',bytes(20)))))]:add(n,b,'Input')
 for hv in (b'0000',b'0001'):
  raw=words([1])+b'ab\0'+(b'xyz' if hv==b'0000' else b'')
  for size in range(len(raw)):add('header-short-'+hv.decode()+str(size),circle(h=header(hv,raw=raw[:size])),'Input')
  add('header-extra-'+hv.decode(),circle(h=header(hv,raw=raw+b'x')),'Input')
  add('header-no-nul-'+hv.decode(),circle(h=header(hv,raw=words([1])+b'aaaaaa')),'Input')
 add('ihdr-no-version',circle(h=form(b'IHDR')),'Input');add('ihdr-two-versions',circle(h=form(b'IHDR',form(b'0001')+form(b'0001'))),'Input')
 add('ihdr-missing-data',circle(h=form(b'IHDR',form(b'0001'))),'Input');add('ihdr-duplicate-data',circle(h=form(b'IHDR',form(b'0001',chunk(b'DATA',b'1234\0')*2))),'Input')
 huge=bytearray(full);struct.pack_into('>I',huge,4,0xffffffff);add('huge-size',huge,'Input')
 bad=bytearray(full);bad[8]=0;add('invalid-ascii-tag',bad,'Input')
 nodes,depth=m.dimensions(full);exact={**DEFAULT,'MaximumInputBytes':len(full),'MaximumNodes':nodes,'MaximumDepth':depth,'MaximumStringBytes':5}
 add('exact-limits',full,limits=exact)
 for k in exact:add('one-less-'+k,full,'Input',{**exact,k:exact[k]-1})
 for k,lo,hi in [('MaximumInputBytes',23,4194305),('MaximumNodes',0,65537),('MaximumDepth',0,65),('MaximumStringBytes',-1,65537)]:
  for value in (lo,hi):add('invalid-option-'+k+str(value),full,'Options',{**DEFAULT,k:value})
 add('empty-name-zero-cap',circle(h=header(name=b'')),limits={**DEFAULT,'MaximumStringBytes':0})
 # Opaque contents remain byte/node/depth bounded, without imposing decoded-name caps.
 add('opaque-zero-string-cap',circle(b'0099'),limits={**DEFAULT,'MaximumStringBytes':0})
 add('opaque-node-cap',circle(b'0099'),'Input',{**DEFAULT,'MaximumNodes':2})
 add('opaque-depth-cap',form(b'BCIR',form(b'0099',form(b'ZZZZ'))),'Input',{**DEFAULT,'MaximumDepth':1})
 out[0]['SectionGeneratorHex']=m.encode(last=form(b'LYRS',m.layer(children=full))).hex().upper()
 return out

def metadata(b):
 root,=records(b,0,len(b));assert root[0]==b'FORM' and root[1][:4]==b'BCIR'
 v,=records(b,12,len(b));tag=v[1][:4].decode();h=None;fields=None;w=[]
 if tag in ('0000','0001','0002'):
  hdr,d=records(b,v[2]+12,v[3]);hv,=records(b,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
  if ht in ('0000','0001'):
   hd,=records(b,hv[2]+12,hv[3]);raw=hd[1];enabled=struct.unpack_from('<I',raw)[0];end=raw.index(0,4)
   h=dict(Version=ht,EnabledBits=enabled,IsEnabled=enabled!=0,NameSha256=sha(raw[4:end]),LegacySha256=sha(raw[end+1:]))
   w=list(struct.unpack('<'+'I'*(len(d[1])//4),d[1]));start=1 if tag=='0001' else 0
   f,s,r=w[start:start+3];ft=w[3] if tag=='0002' else None;fa=w[4] if tag=='0002' else None
   finite=lambda bits:bits&0x7f800000!=0x7f800000
   fields=dict(CenterFirstBits=f,CenterSecondBits=s,RadiusBits=r,LegacyPrefixBits=w[0] if tag=='0001' else None,FeatheringTypeBits=ft,FeatheringAmountBits=fa,HasFiniteGeometry=all(map(finite,(f,s,r))),HasFiniteSerializedFeatheringAmount=finite(fa) if fa is not None else None)
 return dict(Version=tag,ClientVersionRecognized=tag in ('0000','0001','0002'),HasDecodedFields=fields is not None,Header=h,Fields=fields,DataWordBits=w,EncodedBytes=len(b),Sha256=sha(b))

def original(root,cfg):
 assert sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   raw=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if raw is not None:break
  else:raise AssertionError('missing pinned terrain')
 assert priority==42 and len(raw)==1188446 and sha(raw)==m.SOURCE
 v,=records(raw,12,len(raw));d,g,maps=records(raw,v[2]+12,v[3]);b=raw[g[2]:g[3]];assert g[2]==210 and len(b)==139608 and sha(b)==m.GENERATOR
 rules=m.metadata(b)['Rules'];circles=[]
 for rule in rules:
  sec=rule['Section']
  if sec['FormType']=='BCIR':
   a=sec['Offset'];e=a+sec['EncodedBytes'];circles.append(dict(LayerIndex=rule['LayerIndex'],ChildOrdinal=rule['ChildOrdinal'],Offset=a,Metadata=metadata(b[a:e])))
 assert len(circles)==256 and all(c['Metadata']['Version']=='0002' for c in circles)
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=priority,SourceBytes=len(raw),GeneratorBytes=len(b),Circles=circles)

def static(exe):
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 assert sha(exe.read_bytes())==m.EXE;p=pefile.PE(str(exe),fast_load=True);b=p.get_memory_mapped_image();base=p.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 def ins(a,e):return list(md.disasm(b[a-base:e-base],a))
 checks=0
 def has(a,e,mn,op):
  nonlocal checks
  assert any(i.mnemonic==mn and i.op_str==op for i in ins(a,e)),(hex(a),mn,op);checks+=1
 has(0xb79df0,0xb79fd3,'call','0xb72330');has(0xb79df0,0xb79fd3,'cmp','eax, 0x42434952');has(0xb72330,0xb72358,'mov','dword ptr [esi], 0x15ed37c')
 assert struct.unpack_from('<I',b,0x15ed384-base)[0]==0xb725a0 and struct.unpack_from('<I',b,0x15ed38c-base)[0]==0xb65fc0 and struct.unpack_from('<I',b,0x15ed3a0-base)[0]==0xb72430
 for a,e,n in [(0xb72660,0xb726f7,3),(0xb72700,0xb727a4,4),(0xb727b0,0xb728af,5)]:
  ii=ins(a,e);reads=[i for i in ii if i.mnemonic=='call' and i.op_str=='0xa94f00'];assert len(reads)==n
  assert sum(i.mnemonic=='push' and i.op_str=='4' for i in ii)==n
  has(a,e,'call','0xb62fa0');has(a,e,'push','0x44415441')
 for target in (0xb72660,0xb72700,0xb727b0):has(0xb725a0,0xb7265b,'call',hex(target))
 for op in ('dword ptr [esi + 0x1c], ecx','dword ptr [esi + 0x20], eax','dword ptr [esi + 0x24], edx'):has(0xb727b0,0xb728af,'mov',op)
 has(0xb727b0,0xb728af,'call','0xb65fb0');has(0xb727b0,0xb728af,'call','dword ptr [edx + 0x10]')
 has(0xb727b0,0xb728af,'fcomp','dword ptr [0x15dc6a0]');has(0xb727b0,0xb728af,'fcomp','dword ptr [0x15dc6f8]')
 has(0xb63320,0xb63346,'mov','dword ptr [edx + 0x14], eax');has(0xb63320,0xb63346,'mov','dword ptr [edx + 0x18], eax')
 has(0xb72390,0xb7239b,'jmp','0xb63350');has(0xb63350,0xb6335b,'jmp','0xb62ef0')
 has(0xb723c0,0xb723e5,'fabs','');assert not any(i.mnemonic=='fabs' for i in ins(0xb72660,0xb728af))
 for op in ('dword ptr [ecx + 0x1c]','dword ptr [ecx + 0x20]','dword ptr [ecx + 0x18]'):has(0xb72430,0xb724a5,'fld',op)
 has(0xb63d20,0xb6428f,'call','0xb62f20');has(0xb63d20,0xb6428f,'call','dword ptr [edx + 0x24]');has(0xb63d20,0xb6428f,'mov','edi, dword ptr [ecx + 0x14]')
 has(0xb63830,0xb638b6,'mov','dword ptr [edx + ecx*4], eax');has(0xb638c0,0xb63902,'call','dword ptr [edx]')
 # The0001 first read is overwritten by the second before any object-field store.
 vv=ins(0xb72700,0xb727a4);reads=[i.address for i in vv if i.mnemonic=='call' and i.op_str=='0xa94f00']
 stores=[i.address for i in vv if i.mnemonic=='mov' and i.op_str.startswith('dword ptr [edi +')]
 assert reads[0]<reads[1]<stores[0]<reads[2]<stores[1]<reads[3]<stores[2]
 has(0xb727b0,0xb728af,'mov','dword ptr [ebp + 8], 0');has(0xb727b0,0xb728af,'mov','dword ptr [ebp + 8], 0x3f800000')
 has(0xb65fb0,0xb65fbb,'mov','dword ptr [ecx + 0x14], eax');has(0xb65fc0,0xb65fcb,'mov','dword ptr [ecx + 0x18], eax')
 has(0xb62ef0,0xb62f0e,'call','0xac1660')
 assert list(struct.unpack_from('<IIII',b,0xb642a0-base))==[0xb63fe0,0xb63fbe,0xb63fc4,0xb63fc8]
 assert struct.unpack_from('<I',b,0x15dd850-base)[0]==0x40400000
 # Independent inherited header width evidence, no raw decompiler corpus.
 old=ins(0xb63070,0xb6311b);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in old)==4
 for address in (0xb630d2,0xb630df,0xb630ec):assert any(i.address==address and i.mnemonic=='push' and i.op_str=='1' for i in old)
 assert struct.unpack_from('<I',b,0x15dc6a0-base)[0]==0 and struct.unpack_from('<I',b,0x15dc6f8-base)[0]==0x3f800000
 return dict(ExecutableSha256=m.EXE,InstructionAnchors=checks,VersionDataWords=[3,4,5],Vtable='015ED37C',Loader='00B725A0')
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2));print('BCIR fixtures:',len(fixtures()))
 else:
  root,cfg,log=map(Path,sys.argv[1:]);lines=log.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);fs=fixtures();assert len(fs)==len(r['Cases'])
  for f,a in zip(fs,r['Cases']):
   assert (a['Name'],a['Reject'])==(f['Name'],f['Reject'])
   if f['Reject']=='None':assert a['Metadata']==metadata(bytes.fromhex(f['Hex'])),f['Name']
  assert r['OwnershipChecks']==13 and r['Original']==original(root,cfg)
  print(json.dumps(dict(passed=True,cases=len(fs),accepted=sum(f['Reject']=='None' for f in fs),rejected=sum(f['Reject']!='None' for f in fs),ownership_checks=13,static=static(root/'Stardust.exe'),original=dict(Circles=len(r['Original']['Circles']),Versions=['0002'],SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR)),indent=2))
