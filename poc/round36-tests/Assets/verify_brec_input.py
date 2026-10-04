"""Independent BREC serialized-byte fixtures, client anchors and one pinned29-record comparison."""
import contextlib,importlib.util,json,struct,sys
from pathlib import Path
p=Path(__file__).resolve().parents[2]/'round33-tests/Assets/verify_tgen_input.py'
s=importlib.util.spec_from_file_location('tgen',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
sha=m.sha;form=m.form;chunk=m.chunk;words=m.words;header=m.header;records=m.records
DEFAULT=dict(MaximumInputBytes=65536,MaximumNodes=64,MaximumDepth=8,MaximumStringBytes=4096,MaximumTotalStringBytes=8192)
COUNTS={b'0000':4,b'0001':5,b'0002':6,b'0003':10}
def rectangle(v=b'0003',h=None,data=None,name=b'\xfftail'):
 w=[0x80000000,0x7fc12345,0xbf800000,0x3f800000]
 if v==b'0001':w=[0xdeadbeef]+w
 if v in (b'0002',b'0003'):w+=[0xffffffff,0x7f800000]
 if v==b'0003':w+=[0x80000000,0xffffffff,0x7f812345,0x80000000]
 raw=words(w)+(name+b'\0' if v==b'0003' else b'') if data is None else data
 return form(b'BREC',form(v,(header(b'0000') if h is None else h)+chunk(b'DATA',raw)))
def fixtures():
 out=[]
 def add(n,b,reject='None',limits=None):out.append(dict(Name=n,Hex=b.hex().upper(),Reject=reject,Limits=limits or DEFAULT))
 full=rectangle();add('owned-raw',full)
 for v,n in COUNTS.items():
  for hv in (b'0000',b'0001'):add(v.decode()+'-'+hv.decode(),rectangle(v,header(hv,name=b'\xff\x80')))
  for bits in (0,0x80000000,1,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffffffff):add('raw-'+v.decode()+'-'+str(bits),rectangle(v,data=words([bits]*n)+(b'\0' if v==b'0003' else b'')))
  raw=[0x41200000,0x41a00000,0xc1200000,0xc1a00000]
  if v==b'0001':raw=[0xdeadbeef]+raw
  if n>5:raw+=[3,0x80000000]
  if n==10:raw+=[0,0x100,0xff800000,0x7f800000]
  add('inverted-raw-corners-'+v.decode(),rectangle(v,data=words(raw)+(b'\0' if n==10 else b'')))
  for size in range(n*4+2):
   if size!=n*4+(1 if v==b'0003' else 0):add('data-'+v.decode()+'-'+str(size),rectangle(v,data=bytes(size)),'Input')
  add('wrong-order-'+v.decode(),form(b'BREC',form(v,chunk(b'DATA',bytes(n*4))+header())),'Input')
  add('extra-record-'+v.decode(),form(b'BREC',form(v,header()+chunk(b'DATA',bytes(n*4)+(b'\0' if n==10 else b''))+chunk(b'XTRA',b'x'))),'Input')
 add('opaque-version',rectangle(b'0099'));add('opaque-0004',rectangle(b'0004'));add('opaque-empty-version',form(b'BREC',form(b'ABCD')))
 add('opaque-header',rectangle(h=header(b'0099',raw=b'anything')))
 add('opaque-nested',form(b'BREC',form(b'0099',form(b'ZZZZ',chunk(b'DATA',b'\x80\0')))))
 add('empty-names',rectangle(h=header(name=b''),name=b''));add('disabled',rectangle(h=header(enabled=0)))
 for i in range(len(full)):add('truncation-'+str(i),full[:i],'Input')
 for n,b in [('trailing',full+b'\0'),('two-roots',full+full),('wrong-root',form(b'BCIR',full[12:])),('no-version',form(b'BREC')),('two-versions',form(b'BREC',form(b'0003')+form(b'0003'))),('leaf-version',form(b'BREC',chunk(b'0003',b''))),('missing-header',form(b'BREC',form(b'0003',chunk(b'DATA',bytes(41))))),('form-data',form(b'BREC',form(b'0003',header()+form(b'DATA')))),('wrong-data-tag',form(b'BREC',form(b'0003',header()+chunk(b'ADTA',bytes(41)))))]:add(n,b,'Input')
 for hv in (b'0000',b'0001'):
  raw=words([1])+b'ab\0'+(b'xyz' if hv==b'0000' else b'')
  for size in range(len(raw)):add('header-short-'+hv.decode()+str(size),rectangle(h=header(hv,raw=raw[:size])),'Input')
  add('header-extra-'+hv.decode(),rectangle(h=header(hv,raw=raw+b'x')),'Input')
  add('header-no-nul-'+hv.decode(),rectangle(h=header(hv,raw=words([1])+b'aaaaaa')),'Input')
 add('ihdr-no-version',rectangle(h=form(b'IHDR')),'Input');add('ihdr-two-versions',rectangle(h=form(b'IHDR',form(b'0001')+form(b'0001'))),'Input')
 add('ihdr-missing-data',rectangle(h=form(b'IHDR',form(b'0001'))),'Input');add('ihdr-duplicate-data',rectangle(h=form(b'IHDR',form(b'0001',chunk(b'DATA',b'1234\0')*2))),'Input')
 add('extension-no-nul',rectangle(data=bytes(40)+b'xxxx'),'Input');add('extension-trailing-byte',rectangle(data=bytes(40)+b'a\0x'),'Input')
 add('extension-second-nul',rectangle(data=bytes(40)+b'\0\0'),'Input')
 add('extension-string-cap',rectangle(h=header(name=b''),name=b'123456'),'Input',{**DEFAULT,'MaximumStringBytes':5})
 add('header-string-cap',rectangle(h=header(name=b'123456'),name=b''),'Input',{**DEFAULT,'MaximumStringBytes':5})
 add('exact-extension-string-cap',rectangle(h=header(name=b''),name=b'12345'),limits={**DEFAULT,'MaximumStringBytes':5,'MaximumTotalStringBytes':7})
 huge=bytearray(full);struct.pack_into('>I',huge,4,0xffffffff);add('huge-size',huge,'Input')
 bad=bytearray(full);bad[8]=0;add('invalid-ascii-tag',bad,'Input')
 nodes,depth=m.dimensions(full);exact={**DEFAULT,'MaximumInputBytes':len(full),'MaximumNodes':nodes,'MaximumDepth':depth,'MaximumStringBytes':5,'MaximumTotalStringBytes':12}
 add('exact-limits',full,limits=exact)
 for k in exact:add('one-less-'+k,full,'Input',{**exact,k:exact[k]-1})
 for k,lo,hi in [('MaximumInputBytes',23,4194305),('MaximumNodes',0,65537),('MaximumDepth',0,65),('MaximumStringBytes',-1,65537),('MaximumTotalStringBytes',-1,131073)]:
  for value in (lo,hi):add('invalid-option-'+k+str(value),full,'Options',{**DEFAULT,k:value})
 add('empty-name-zero-cap',rectangle(h=header(name=b''),name=b''),limits={**DEFAULT,'MaximumStringBytes':0,'MaximumTotalStringBytes':2})
 add('empty-name-one-total',rectangle(h=header(name=b''),name=b''),'Input',{**DEFAULT,'MaximumStringBytes':0,'MaximumTotalStringBytes':1})
 add('opaque-zero-string-cap',rectangle(b'0099'),limits={**DEFAULT,'MaximumStringBytes':0,'MaximumTotalStringBytes':0})
 add('opaque-node-cap',rectangle(b'0099'),'Input',{**DEFAULT,'MaximumNodes':2})
 add('opaque-depth-cap',form(b'BREC',form(b'0099',form(b'ZZZZ'))),'Input',{**DEFAULT,'MaximumDepth':1})
 out[0]['SectionGeneratorHex']=m.encode(last=form(b'LYRS',m.layer(children=full))).hex().upper()
 return out

def metadata(b):
 root,=records(b,0,len(b));assert root[0]==b'FORM' and root[1][:4]==b'BREC'
 v,=records(b,12,len(b));tag=v[1][:4].decode();h=None;fields=None;w=[];name=b'';strings=0
 if tag.encode() in COUNTS:
  hdr,d=records(b,v[2]+12,v[3]);hv,=records(b,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
  if ht in ('0000','0001'):
   hd,=records(b,hv[2]+12,hv[3]);raw=hd[1];enabled=struct.unpack_from('<I',raw)[0];end=raw.index(0,4);strings=end-4+1
   h=dict(Version=ht,EnabledBits=enabled,IsEnabled=enabled!=0,NameSha256=sha(raw[4:end]),LegacySha256=sha(raw[end+1:]))
   count=COUNTS[tag.encode()];w=list(struct.unpack_from('<'+'I'*count,d[1]));start=1 if tag=='0001' else 0
   a,c,z,q=w[start:start+4];ft=w[4] if tag in ('0002','0003') else None;fa=w[5] if ft is not None else None
   ext=w[6:] if tag=='0003' else [None]*4
   if tag=='0003':stop=d[1].index(0,40);name=d[1][40:stop];strings+=len(name)+1
   finite=lambda bits:bits&0x7f800000!=0x7f800000
   fields=dict(CornerFirstABits=a,CornerSecondABits=c,CornerFirstBBits=z,CornerSecondBBits=q,LegacyPrefixBits=w[0] if tag=='0001' else None,FeatheringTypeBits=ft,FeatheringAmountBits=fa,ExtensionFlagFirstBits=ext[0],ExtensionFlagSecondBits=ext[1],ExtensionScalarFirstBits=ext[2],ExtensionScalarSecondBits=ext[3],HasFiniteGeometry=all(map(finite,(a,c,z,q))),HasFiniteSerializedFeatheringAmount=finite(fa) if fa is not None else None,IsExtensionFlagFirstSet=ext[0]!=0 if ext[0] is not None else None,IsExtensionFlagSecondSet=ext[1]!=0 if ext[1] is not None else None)
 return dict(Version=tag,ClientVersionRecognized=tag.encode() in COUNTS,HasDecodedFields=fields is not None,HasSerializedExtension=fields is not None and tag=='0003',DecodedStringBytes=strings,Header=h,Fields=fields,ExtensionNameSha256=sha(name),DataWordBits=w,EncodedBytes=len(b),Sha256=sha(b))

def original(root,cfg):
 assert sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   raw=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if raw is not None:break
  else:raise AssertionError('missing pinned terrain')
 assert priority==42 and len(raw)==1188446 and sha(raw)==m.SOURCE
 v,=records(raw,12,len(raw));d,g,maps=records(raw,v[2]+12,v[3]);b=raw[g[2]:g[3]];assert g[2]==210 and len(b)==139608 and sha(b)==m.GENERATOR
 rectangles=[]
 for rule in m.metadata(b)['Rules']:
  sec=rule['Section']
  if sec['FormType']=='BREC':
   a=sec['Offset'];e=a+sec['EncodedBytes'];rectangles.append(dict(LayerIndex=rule['LayerIndex'],ChildOrdinal=rule['ChildOrdinal'],Offset=a,Metadata=metadata(b[a:e])))
 assert len(rectangles)==29 and all(c['Metadata']['Version']=='0003' for c in rectangles)
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=priority,SourceBytes=len(raw),GeneratorBytes=len(b),Rectangles=rectangles)

def static(exe):
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 assert sha(exe.read_bytes())==m.EXE;p=pefile.PE(str(exe),fast_load=True);b=p.get_memory_mapped_image();base=p.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 def ins(a,e):return list(md.disasm(b[a-base:e-base],a))
 checks=0
 def has(a,e,mn,op):
  nonlocal checks
  assert any(i.mnemonic==mn and i.op_str==op for i in ins(a,e)),(hex(a),mn,op);checks+=1
 has(0xb79df0,0xb79fd3,'call','0xb72960');has(0xb79df0,0xb79fd3,'cmp','eax, 0x42524543');has(0xb72960,0xb729c0,'mov','dword ptr [esi], 0x15ed3b8')
 assert [struct.unpack_from('<I',b,a-base)[0] for a in (0x15ed3c0,0x15ed3c4,0x15ed3c8,0x15ed3dc)]==[0xb730f0,0xb735d0,0xb72aa0,0xb72bc0]
 assert list(struct.unpack_from('<IIII',b,0xb731e8-base))==[0xb7311f,0xb73136,0xb7314d,0xb73164]
 has(0xb730f0,0xb731e4,'cmp','eax, 3');has(0xb730f0,0xb731e4,'ja','0xb7317b')
 for a,e,n in [(0xb73200,0xb7329e,4),(0xb732a0,0xb7334b,5),(0xb73350,0xb73456,6),(0xb73460,0xb735c6,10)]:
  ii=ins(a,e);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ii)==n
  assert sum(i.mnemonic=='push' and i.op_str=='4' for i in ii)==n
  has(a,e,'call','0xb62fa0');has(a,e,'push','0x44415441');has(0xb730f0,0xb731e4,'call',hex(a))
  assert not any(i.mnemonic=='call' and i.op_str=='0xb72a70' for i in ii)
 ii=ins(0xb732a0,0xb7334b);reads=[i.address for i in ii if i.mnemonic=='call' and i.op_str=='0xa94f00'];stores=[i.address for i in ii if i.mnemonic=='mov' and i.op_str.startswith('dword ptr [edi +')]
 assert reads[0]<reads[1]<stores[0]<reads[2]<stores[1]<reads[3]<stores[2]<reads[4]<stores[3]
 assert sum(i.mnemonic=='call' and i.op_str=='0xb72ac0' for i in ins(0xb730f0,0xb731e4))==5
 for a,e in [(0xb73350,0xb73456),(0xb73460,0xb735c6)]:
  has(a,e,'call','0xb65fb0');has(a,e,'call','dword ptr [eax + 0x10]');has(a,e,'mov','dword ptr [ebp + 8], 0');has(a,e,'mov','dword ptr [ebp + 8], 0x3f800000')
 for op in ('byte ptr [edi + 0x58], al','byte ptr [edi + 0x59], dl','dword ptr [edi + 0x5c], ecx','dword ptr [edi + 0x60], eax','dword ptr [edi + 0x64], eax'):has(0xb73460,0xb735c6,'mov',op)
 has(0xb73460,0xb735c6,'call','0xa95200');has(0xa95200,0xa95269,'cmp','byte ptr [ebx + esi], 0');has(0xa95200,0xa95269,'call','0xac15e0')
 has(0xb72aa0,0xb72ab9,'call','0xb65fc0');has(0xb72aa0,0xb72ab9,'call','0xb72ac0');has(0xb72a70,0xb72a98,'call','0xb72ac0')
 for op in ('dword ptr [ecx + 0x1c], eax','dword ptr [ecx + 0x20], edx'):has(0xb72ac0,0xb72b67,'mov',op)
 has(0xb72ac0,0xb72b67,'fstp','dword ptr [ecx + 0x24]');has(0xb72ac0,0xb72b67,'fstp','dword ptr [ecx + 0x28]')
 has(0xb729f0,0xb72a50,'call','0xac1660');has(0xb729f0,0xb72a50,'call','0xb63350')
 for op in ('dword ptr [esi + 0x1c]','dword ptr [esi + 0x20]'):has(0xb72bc0,0xb72e46,'fld',op)
 has(0xb63d20,0xb6428f,'call','0xb62f20');has(0xb63d20,0xb6428f,'call','dword ptr [edx + 0x24]')
 old=ins(0xb63070,0xb6311b);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in old)==4
 for address in (0xb630d2,0xb630df,0xb630ec):assert any(i.address==address and i.mnemonic=='push' and i.op_str=='1' for i in old)
 assert struct.unpack_from('<I',b,0x15dc6a0-base)[0]==0 and struct.unpack_from('<I',b,0x15dc6f8-base)[0]==0x3f800000
 return dict(ExecutableSha256=m.EXE,InstructionAnchors=checks,VersionDataWords=[4,5,6,10],Vtable='015ED3B8',Loader='00B730F0')
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2));print('BREC fixtures:',len(fixtures()))
 else:
  root,cfg,log=map(Path,sys.argv[1:]);lines=log.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);fs=fixtures();assert len(fs)==len(r['Cases'])
  for f,a in zip(fs,r['Cases']):
   assert (a['Name'],a['Reject'])==(f['Name'],f['Reject'])
   if f['Reject']=='None':assert a['Metadata']==metadata(bytes.fromhex(f['Hex'])),f['Name']
  assert r['OwnershipChecks']==17 and r['Original']==original(root,cfg)
  print(json.dumps(dict(passed=True,cases=len(fs),accepted=sum(f['Reject']=='None' for f in fs),rejected=sum(f['Reject']!='None' for f in fs),ownership_checks=17,static=static(root/'Stardust.exe'),original=dict(Rectangles=len(r['Original']['Rectangles']),Versions=['0003'],SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR)),indent=2))
