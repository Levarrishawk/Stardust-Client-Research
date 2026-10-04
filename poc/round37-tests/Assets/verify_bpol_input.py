"""Independent BPOL byte schemas, point budgets, client instruction anchors and pinned original input."""
import contextlib,importlib.util,json,struct,sys
from pathlib import Path
p=Path(__file__).resolve().parents[2]/'round33-tests/Assets/verify_tgen_input.py'
s=importlib.util.spec_from_file_location('tgen',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
sha=m.sha;form=m.form;chunk=m.chunk;words=m.words;header=m.header;records=m.records
DEFAULT=dict(MaximumInputBytes=262144,MaximumNodes=64,MaximumDepth=8,MaximumPoints=8192,MaximumStringBytes=4096,MaximumTotalStringBytes=8192,MaximumDecodedCopyBytes=262144)
VERS=[f'{v:04d}'.encode() for v in range(7)]
POINTS=[(0x80000000,0x7f812345),(0xbf800000,0x3f800000),(0x7fc12345,1)]
def data(v=b'0005',points=POINTS,name=b'\xfftail',count=None,meta=None):
 pts=b''.join(words(p) for p in points)
 fw=[0xffffffff,0xff800000,0x80000000,0x7f812345,0x80000000] if meta is None else meta
 if v==b'0000':return pts
 if v==b'0001':return words([0xdeadbeef])+pts
 if v==b'0002':return words(fw[:2])+pts
 if v==b'0003':return words(fw[:4])+name+b'\0'+pts
 if v==b'0004':return words(fw)+name+b'\0'+pts
 if v in (b'0005',b'0006'):return words([len(points) if count is None else count])+pts+words(fw+([0x12345678] if v==b'0006' else []))+name+b'\0'
 return b'opaque'
def polygon(v=b'0005',h=None,raw=None,**kwargs):return form(b'BPOL',form(v,(header(b'0000') if h is None else h)+chunk(b'DATA',data(v,**kwargs) if raw is None else raw)))
def fixtures():
 out=[]
 def add(n,b,reject='None',limits=None):out.append(dict(Name=n,Hex=b.hex().upper(),Reject=reject,Limits=limits or DEFAULT))
 full=polygon();add('owned-raw',full)
 for v in VERS:
  for hv in (b'0000',b'0001'):add(v.decode()+'-'+hv.decode(),polygon(v,header(hv,name=b'\xff\x80')))
  for n in range(4):add('point-count-'+v.decode()+str(n),polygon(v,points=POINTS[:n]))
  add('repeated-closed-'+v.decode(),polygon(v,points=POINTS+[POINTS[0]]))
  for bits in (0,0x80000000,1,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffffffff):add('raw-'+v.decode()+str(bits),polygon(v,points=[(bits,bits)],meta=[bits]*5))
  raw=data(v);prefix={b'0000':0,b'0001':4,b'0002':8,b'0003':22,b'0004':26}.get(v)
  for size in range(len(raw)):
   valid=prefix is not None and size>=prefix and (size-prefix)%8==0
   add('data-short-'+v.decode()+str(size),polygon(v,raw=raw[:size]),'None' if valid else 'Input')
  # Implicit layouts accept additional complete points, but reject partial pairs.
  for extra in (1,7):add('partial-pair-'+v.decode()+str(extra),polygon(v,raw=raw+bytes(extra)),'Input')
  add('extra-whole-pair-'+v.decode(),polygon(v,raw=raw+bytes(8)),'None' if prefix is not None else 'Input')
  add('wrong-order-'+v.decode(),form(b'BPOL',form(v,chunk(b'DATA',raw)+header())),'Input')
  add('extra-record-'+v.decode(),form(b'BPOL',form(v,header()+chunk(b'DATA',raw)+chunk(b'XTRA',b'x'))),'Input')
 for v in (b'0005',b'0006'):
  for count in (2,4,8193,0x7fffffff,0x80000000,0xffffffff):add('count-mismatch-'+v.decode()+str(count),polygon(v,count=count),'Input')
  add('zero-count-positive-tail-'+v.decode(),polygon(v,points=[],count=0))
 add('opaque-version',polygon(b'0099'));add('opaque-0007',polygon(b'0007'));add('opaque-empty-version',form(b'BPOL',form(b'ABCD')))
 add('opaque-header',polygon(h=header(b'0099',raw=b'anything')))
 add('opaque-header-data',polygon(h=header(b'0099',raw=b'anything'),raw=b''))
 add('opaque-nested',form(b'BPOL',form(b'0099',form(b'ZZZZ',chunk(b'DATA',b'\x80\0')))))
 add('empty-names',polygon(h=header(name=b''),name=b''));add('disabled',polygon(h=header(enabled=0)))
 for i in range(len(full)):add('truncation-'+str(i),full[:i],'Input')
 for n,b in [('trailing',full+b'\0'),('two-roots',full+full),('wrong-root',form(b'BPLN',full[12:])),('no-version',form(b'BPOL')),('two-versions',form(b'BPOL',form(b'0005')+form(b'0005'))),('leaf-version',form(b'BPOL',chunk(b'0005',b''))),('missing-header',form(b'BPOL',form(b'0005',chunk(b'DATA',data())))),('form-data',form(b'BPOL',form(b'0005',header()+form(b'DATA')))),('wrong-data-tag',form(b'BPOL',form(b'0005',header()+chunk(b'ADTA',data()))))]:add(n,b,'Input')
 for hv in (b'0000',b'0001'):
  raw=words([1])+b'ab\0'+(b'xyz' if hv==b'0000' else b'')
  for size in range(len(raw)):add('header-short-'+hv.decode()+str(size),polygon(h=header(hv,raw=raw[:size])),'Input')
  add('header-extra-'+hv.decode(),polygon(h=header(hv,raw=raw+b'x')),'Input')
  add('header-no-nul-'+hv.decode(),polygon(h=header(hv,raw=words([1])+b'aaaaaa')),'Input')
 add('ihdr-no-version',polygon(h=form(b'IHDR')),'Input');add('ihdr-two-versions',polygon(h=form(b'IHDR',form(b'0001')+form(b'0001'))),'Input')
 add('ihdr-missing-data',polygon(h=form(b'IHDR',form(b'0001'))),'Input');add('ihdr-duplicate-data',polygon(h=form(b'IHDR',form(b'0001',chunk(b'DATA',b'1234\0')*2))),'Input')
 for v in (b'0003',b'0004',b'0005',b'0006'):
  prefix=data(v,points=[],name=b'')[:-1];add('extension-no-nul-'+v.decode(),polygon(v,raw=prefix+b'xxx'),'Input')
 add('extension-string-cap',polygon(h=header(name=b''),name=b'123456'),'Input',{**DEFAULT,'MaximumStringBytes':5})
 add('header-string-cap',polygon(h=header(name=b'123456'),name=b''),'Input',{**DEFAULT,'MaximumStringBytes':5})
 huge=bytearray(full);struct.pack_into('>I',huge,4,0xffffffff);add('huge-size',huge,'Input')
 bad=bytearray(full);bad[8]=0;add('invalid-ascii-tag',bad,'Input')
 nodes,depth=m.dimensions(full);exact={**DEFAULT,'MaximumInputBytes':len(full),'MaximumNodes':nodes,'MaximumDepth':depth,'MaximumPoints':3,'MaximumStringBytes':5,'MaximumTotalStringBytes':12,'MaximumDecodedCopyBytes':61}
 add('exact-limits',full,limits=exact)
 for k in exact:add('one-less-'+k,full,'Input',{**exact,k:exact[k]-1})
 for k,lo,hi in [('MaximumInputBytes',23,8388609),('MaximumNodes',0,65537),('MaximumDepth',0,65),('MaximumPoints',-1,262145),('MaximumStringBytes',-1,65537),('MaximumTotalStringBytes',-1,131073),('MaximumDecodedCopyBytes',-1,8388609)]:
  for value in (lo,hi):add('invalid-option-'+k+str(value),full,'Options',{**DEFAULT,k:value})
 for v in VERS:
  add('point-cap-'+v.decode(),polygon(v),'Input',{**DEFAULT,'MaximumPoints':2})
  add('zero-point-cap-'+v.decode(),polygon(v,points=[]),limits={**DEFAULT,'MaximumPoints':0})
 add('empty-name-zero-cap',polygon(h=header(name=b''),name=b''),limits={**DEFAULT,'MaximumStringBytes':0,'MaximumTotalStringBytes':2})
 add('empty-name-one-total',polygon(h=header(name=b''),name=b''),'Input',{**DEFAULT,'MaximumStringBytes':0,'MaximumTotalStringBytes':1})
 add('opaque-zero-decoded-caps',polygon(b'0099'),limits={**DEFAULT,'MaximumPoints':0,'MaximumStringBytes':0,'MaximumTotalStringBytes':0,'MaximumDecodedCopyBytes':0})
 add('opaque-node-cap',polygon(b'0099'),'Input',{**DEFAULT,'MaximumNodes':2})
 add('opaque-depth-cap',form(b'BPOL',form(b'0099',form(b'ZZZZ'))),'Input',{**DEFAULT,'MaximumDepth':1})
 add('implicit-large-point-cap',polygon(b'0000',points=[(0,0)]*8193),'Input')
 out[0]['SectionGeneratorHex']=m.encode(last=form(b'LYRS',m.layer(children=full))).hex().upper()
 return out

def metadata(b):
 root,=records(b,0,len(b));assert root[0]==b'FORM' and root[1][:4]==b'BPOL'
 v,=records(b,12,len(b));tag=v[1][:4].decode();h=None;meta=None;pts=[];name=b'';strings=copies=0;mw=[];finite=None
 if tag.encode() in VERS:
  hdr,d=records(b,v[2]+12,v[3]);hv,=records(b,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
  if ht in ('0000','0001'):
   hd,=records(b,hv[2]+12,hv[3]);raw=hd[1];enabled=struct.unpack_from('<I',raw)[0];end=raw.index(0,4);hn=raw[4:end];hl=raw[end+1:];strings=len(hn)+1
   h=dict(Version=ht,EnabledBits=enabled,IsEnabled=enabled!=0,NameSha256=sha(hn),LegacySha256=sha(hl))
   raw=d[1];cursor=0;prefix=count=ft=fa=flag=s1=s2=discard=None
   def word():
    nonlocal cursor
    value=struct.unpack_from('<I',raw,cursor)[0];cursor+=4;mw.append(value);return value
   def point(n):
    nonlocal cursor
    finite=lambda bits:bits&0x7f800000!=0x7f800000
    for _ in range(n):
     first,second=struct.unpack_from('<II',raw,cursor);cursor+=8;pts.append(dict(FirstBits=first,SecondBits=second,IsFinite=finite(first) and finite(second)))
   if tag in ('0005','0006'):count=word();point(count)
   if tag=='0001':prefix=word()
   if tag>='0002':ft=word();fa=word()
   if tag>='0003':
    flag=word();s1=word()
    if tag!='0003':s2=word()
    if tag=='0006':discard=word()
    end=raw.index(0,cursor);name=raw[cursor:end];cursor=end+1;strings+=len(name)+1
   if tag<'0005':assert (len(raw)-cursor)%8==0;point((len(raw)-cursor)//8)
   assert cursor==len(raw);finite=all(p['IsFinite'] for p in pts)
   meta=dict(LegacyPrefixBits=prefix,SerializedPointCountBits=count,FeatheringTypeBits=ft,FeatheringAmountBits=fa,ExtensionFlagBits=flag,ExtensionScalarFirstBits=s1,ExtensionScalarSecondBits=s2,LegacyBeforeNameBits=discard,HasFiniteSerializedFeatheringAmount=fa&0x7f800000!=0x7f800000 if fa is not None else None,IsExtensionFlagSet=flag!=0 if flag is not None else None)
   copies=8*len(pts)+4*len(mw)+len(hn)+len(hl)+len(name)
 return dict(Version=tag,ClientVersionRecognized=tag.encode() in VERS,HasDecodedFields=meta is not None,HasSerializedExtension=meta is not None and meta['ExtensionFlagBits'] is not None,HasFinitePoints=finite,DecodedStringBytes=strings,DecodedCopyBytes=copies,Header=h,Metadata=meta,Points=pts,ExtensionNameSha256=sha(name),MetadataWordBits=mw,EncodedBytes=len(b),Sha256=sha(b))

def original(root,cfg):
 assert sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   raw=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if raw is not None:break
  else:raise AssertionError('missing pinned terrain')
 assert priority==42 and len(raw)==1188446 and sha(raw)==m.SOURCE
 v,=records(raw,12,len(raw));d,g,maps=records(raw,v[2]+12,v[3]);b=raw[g[2]:g[3]];assert g[2]==210 and len(b)==139608 and sha(b)==m.GENERATOR
 polygons=[]
 for rule in m.metadata(b)['Rules']:
  sec=rule['Section']
  if sec['FormType']=='BPOL':
   a=sec['Offset'];e=a+sec['EncodedBytes'];polygons.append(dict(LayerIndex=rule['LayerIndex'],ChildOrdinal=rule['ChildOrdinal'],Offset=a,Metadata=metadata(b[a:e])))
 assert len(polygons)==57 and all(c['Metadata']['Version']=='0005' for c in polygons)
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=priority,SourceBytes=len(raw),GeneratorBytes=len(b),Polygons=polygons)

def static(exe):
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 assert sha(exe.read_bytes())==m.EXE;p=pefile.PE(str(exe),fast_load=True);b=p.get_memory_mapped_image();base=p.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 def ins(a,e):return list(md.disasm(b[a-base:e-base],a))
 checks=0
 def has(a,e,mn,op):
  nonlocal checks
  assert any(i.mnemonic==mn and i.op_str==op for i in ins(a,e)),(hex(a),mn,op);checks+=1
 has(0xb79df0,0xb79fd3,'call','0xb73ba0');has(0xb79df0,0xb79fd3,'cmp','eax, 0x42504f4c');has(0xb73ba0,0xb73bcb,'mov','dword ptr [esi], 0x15ed434')
 assert [struct.unpack_from('<I',b,a-base)[0] for a in (0x15ed43c,0x15ed440,0x15ed444,0x15ed458,0x15ed46c)]==[0xb73fe0,0xb74910,0xb65fc0,0xb73d00,0xb738b0]
 assert list(struct.unpack_from('<IIIIIII',b,0xb7412c-base))==[0xb74013,0xb7402a,0xb74041,0xb74058,0xb7406f,0xb74086,0xb7409d]
 has(0xb73fe0,0xb74129,'cmp','eax, 6');has(0xb73fe0,0xb74129,'ja','0xb740b4');assert sum(i.mnemonic=='call' and i.op_str=='dword ptr [eax + 0x38]' for i in ins(0xb73fe0,0xb74129))==8
 ranges=[(0xb74150,0xb7421a,2),(0xb74220,0xb742f7,3),(0xb74300,0xb743ec,4),(0xb743f0,0xb74514,6),(0xb74520,0xb74654,7),(0xb74660,0xb747a2,8),(0xb747b0,0xb748ff,9)]
 for index,(a,e,n) in enumerate(ranges):
  ii=ins(a,e);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ii)==n
  assert sum(i.mnemonic=='push' and i.op_str=='4' for i in ii)==n
  assert sum(i.mnemonic=='mov' and i.op_str.startswith('dword ptr [') and '*8],' in i.op_str for i in ii)==1
  assert sum(i.mnemonic=='mov' and i.op_str.startswith('dword ptr [') and '*8 + 4],' in i.op_str for i in ii)==1
  has(a,e,'call','0xb62fa0');has(a,e,'push','0x44415441');has(0xb73fe0,0xb74129,'call',hex(a));has(a,e,'call','0xb750e0')
  if index<5:has(a,e,'call','0xa94ee0');has(a,e,'push','8')
  else:assert not any(i.mnemonic=='call' and i.op_str=='0xa94ee0' for i in ii);has(a,e,'test','eax, eax')
  if index>=2:has(a,e,'call','0xb65fb0');has(a,e,'call','dword ptr [eax + 0x10]');assert not any(i.mnemonic.startswith('fcom') for i in ii)
  if index>=3:has(a,e,'call','0xa95200')
 has(0xa94ee0,0xa94efe,'idiv','dword ptr [ebp + 8]');has(0xa94f00,0xa94f45,'rep movsd','dword ptr es:[edi], dword ptr [esi]')
 #0001 discarded prefix precedes remaining/8, then paired point reads.
 ii=ins(0xb74220,0xb742f7);reads=[i.address for i in ii if i.mnemonic=='call' and i.op_str=='0xa94f00'];division=next(i.address for i in ii if i.mnemonic=='call' and i.op_str=='0xa94ee0');assert reads[0]<division<reads[1]<reads[2]
 #0006 discarded four-byte word is read before the final NUL string.
 ii=ins(0xb747b0,0xb748ff);reads=[i.address for i in ii if i.mnemonic=='call' and i.op_str=='0xa94f00'];string=next(i.address for i in ii if i.mnemonic=='call' and i.op_str=='0xa95200');assert reads[-1]==0xb748ce and reads[-2]==0xb748bb and reads[-1]<string
 has(0xb73740,0xb7379e,'call','0xac1660');has(0xb73740,0xb7379e,'call','0xb63350');has(0xb73c00,0xb73c60,'call','0xac1660');has(0xb73c00,0xb73c60,'call','0xb73740')
 has(0xb738b0,0xb73925,'mov','eax, 0x7f7fffff');has(0xb738b0,0xb73925,'mov','eax, 0xff7fffff');has(0xb738b0,0xb73925,'fld','dword ptr [eax + edx*8 + 4]')
 has(0xb65fc0,0xb65fcb,'mov','dword ptr [ecx + 0x18], eax');has(0xb63d20,0xb6428f,'call','0xb62f20');has(0xb63d20,0xb6428f,'call','dword ptr [edx + 0x24]')
 old=ins(0xb63070,0xb6311b);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in old)==4
 for address in (0xb630d2,0xb630df,0xb630ec):assert any(i.address==address and i.mnemonic=='push' and i.op_str=='1' for i in old)
 return dict(ExecutableSha256=m.EXE,InstructionAnchors=checks,VersionReadSites=[r[2] for r in ranges],Vtable='015ED434',Loader='00B73FE0',DerivedCountVersions=[0,1,2,3,4],ExplicitCountVersions=[5,6])
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2));print('BPOL fixtures:',len(fixtures()))
 else:
  root,cfg,log=map(Path,sys.argv[1:]);lines=log.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);fs=fixtures();assert len(fs)==len(r['Cases'])
  for f,a in zip(fs,r['Cases']):
   assert (a['Name'],a['Reject'])==(f['Name'],f['Reject'])
   if f['Reject']=='None':assert a['Metadata']==metadata(bytes.fromhex(f['Hex'])),f['Name']
  assert r['OwnershipChecks']==19 and r['Original']==original(root,cfg)
  print(json.dumps(dict(passed=True,cases=len(fs),accepted=sum(f['Reject']=='None' for f in fs),rejected=sum(f['Reject']!='None' for f in fs),ownership_checks=19,static=static(root/'Stardust.exe'),original=dict(Polygons=len(r['Original']['Polygons']),Versions=['0005'],SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR)),indent=2))
