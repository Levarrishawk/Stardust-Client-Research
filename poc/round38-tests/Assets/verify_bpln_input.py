"""Independent BPLN schemas, bounded synthetic fixtures and pinned static/corpus oracle."""
import contextlib,importlib.util,json,struct,sys
from pathlib import Path
p=Path(__file__).resolve().parents[2]/'round33-tests/Assets/verify_tgen_input.py'
s=importlib.util.spec_from_file_location('tgen',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
sha=m.sha;form=m.form;chunk=m.chunk;words=m.words;header=m.header;records=m.records
DEFAULT=dict(MaximumInputBytes=262144,MaximumNodes=64,MaximumDepth=8,MaximumPoints=8192,MaximumStringBytes=4096,MaximumTotalStringBytes=8192,MaximumDecodedCopyBytes=262144)
VERS=[b'0000',b'0001']
POINTS=[(0x80000000,0x7f812345),(0xbf800000,0x3f800000),(0x7fc12345,1)]
def data(v=b'0001',points=POINTS,count=None,meta=None):
 pts=b''.join(words(p) for p in points);fw=[0xffffffff,0xff800000,0x80000000] if meta is None else meta
 if v==b'0000':return words(fw)+pts
 if v==b'0001':return words([len(points) if count is None else count])+pts+words(fw)
 return b'opaque'
def polyline(v=b'0001',h=None,raw=None,**kwargs):return form(b'BPLN',form(v,(header(b'0000') if h is None else h)+chunk(b'DATA',data(v,**kwargs) if raw is None else raw)))
def fixtures():
 out=[]
 def add(n,b,reject='None',limits=None):out.append(dict(Name=n,Hex=b.hex().upper(),Reject=reject,Limits=limits or DEFAULT))
 full=polyline();add('owned-raw',full)
 for v in VERS:
  for hv in VERS:add(v.decode()+'-'+hv.decode(),polyline(v,header(hv,name=b'\xff\x80')))
  for n in range(4):add('point-count-'+v.decode()+str(n),polyline(v,points=POINTS[:n]))
  add('repeated-closed-'+v.decode(),polyline(v,points=POINTS+[POINTS[0]]))
  for bits in (0,0x80000000,1,0x7f800000,0xff800000,0x7fc12345,0x7f812345,0xffffffff):add('raw-'+v.decode()+str(bits),polyline(v,points=[(bits,bits)],meta=[bits]*3))
  add('finite-points-nonfinite-width-'+v.decode(),polyline(v,points=[(0,0x80000000)],meta=[0x80000000,0x3f800000,0x7f812345]))
  add('nonfinite-points-finite-metadata-'+v.decode(),polyline(v,points=[(0x7f800000,0)],meta=[0xffffffff,0,0xbf800000]))
  raw=data(v)
  for size in range(len(raw)):
   valid=v==b'0000' and size>=12 and (size-12)%8==0
   add('data-short-'+v.decode()+str(size),polyline(v,raw=raw[:size]),'None' if valid else 'Input')
  for extra in (1,7):add('partial-pair-'+v.decode()+str(extra),polyline(v,raw=raw+bytes(extra)),'Input')
  add('extra-whole-pair-'+v.decode(),polyline(v,raw=raw+bytes(8)),'None' if v==b'0000' else 'Input')
  add('wrong-order-'+v.decode(),form(b'BPLN',form(v,chunk(b'DATA',raw)+header())),'Input')
  add('extra-record-'+v.decode(),form(b'BPLN',form(v,header()+chunk(b'DATA',raw)+chunk(b'XTRA',b'x'))),'Input')
 for count in (2,4,8193,0x7fffffff,0x80000000,0xffffffff):add('count-mismatch-'+str(count),polyline(count=count),'Input')
 add('zero-count-positive-tail',polyline(points=[],count=0))
 for v in (b'0002',b'0005',b'0099'):add('opaque-'+v.decode(),polyline(v))
 add('opaque-empty-version',form(b'BPLN',form(b'ABCD')))
 add('opaque-header',polyline(h=header(b'0099',raw=b'anything')))
 add('opaque-header-data',polyline(h=header(b'0099',raw=b'anything'),raw=b''))
 add('opaque-nested',form(b'BPLN',form(b'0099',form(b'ZZZZ',chunk(b'DATA',b'\x80\0')))))
 add('empty-name',polyline(h=header(name=b'')));add('disabled',polyline(h=header(enabled=0)))
 for i in range(len(full)):add('truncation-'+str(i),full[:i],'Input')
 for n,b in [('trailing',full+b'\0'),('two-roots',full+full),('wrong-root',form(b'BPOL',full[12:])),('no-version',form(b'BPLN')),('two-versions',form(b'BPLN',form(b'0001')*2)),('leaf-version',form(b'BPLN',chunk(b'0001',b''))),('missing-header',form(b'BPLN',form(b'0001',chunk(b'DATA',data())))),('form-data',form(b'BPLN',form(b'0001',header()+form(b'DATA')))),('wrong-data-tag',form(b'BPLN',form(b'0001',header()+chunk(b'ADTA',data()))))]:add(n,b,'Input')
 for hv in VERS:
  raw=words([1])+b'ab\0'+(b'xyz' if hv==b'0000' else b'')
  for size in range(len(raw)):add('header-short-'+hv.decode()+str(size),polyline(h=header(hv,raw=raw[:size])),'Input')
  add('header-extra-'+hv.decode(),polyline(h=header(hv,raw=raw+b'x')),'Input')
  add('header-no-nul-'+hv.decode(),polyline(h=header(hv,raw=words([1])+b'aaaaaa')),'Input')
 for n,h in [('no-version',form(b'IHDR')),('two-versions',form(b'IHDR',form(b'0001')*2)),('missing-data',form(b'IHDR',form(b'0001'))),('duplicate-data',form(b'IHDR',form(b'0001',chunk(b'DATA',b'1234\0')*2)))]:add('ihdr-'+n,polyline(h=h),'Input')
 add('header-string-cap',polyline(h=header(name=b'123456')),'Input',{**DEFAULT,'MaximumStringBytes':5})
 huge=bytearray(full);struct.pack_into('>I',huge,4,0xffffffff);add('huge-size',huge,'Input')
 bad=bytearray(full);bad[8]=0;add('invalid-ascii-tag',bad,'Input')
 nodes,depth=m.dimensions(full);exact={**DEFAULT,'MaximumInputBytes':len(full),'MaximumNodes':nodes,'MaximumDepth':depth,'MaximumPoints':3,'MaximumStringBytes':5,'MaximumTotalStringBytes':6,'MaximumDecodedCopyBytes':48}
 add('exact-limits',full,limits=exact)
 add('exact-implicit-copy',polyline(b'0000'),limits={**DEFAULT,'MaximumDecodedCopyBytes':44})
 add('one-less-implicit-copy',polyline(b'0000'),'Input',{**DEFAULT,'MaximumDecodedCopyBytes':43})
 for k in exact:add('one-less-'+k,full,'Input',{**exact,k:exact[k]-1})
 for k,lo,hi in [('MaximumInputBytes',23,8388609),('MaximumNodes',0,65537),('MaximumDepth',0,65),('MaximumPoints',-1,262145),('MaximumStringBytes',-1,65537),('MaximumTotalStringBytes',-1,131073),('MaximumDecodedCopyBytes',-1,8388609)]:
  for value in (lo,hi):add('invalid-option-'+k+str(value),full,'Options',{**DEFAULT,k:value})
 for v in VERS:
  add('point-cap-'+v.decode(),polyline(v),'Input',{**DEFAULT,'MaximumPoints':2})
  add('zero-point-cap-'+v.decode(),polyline(v,points=[]),limits={**DEFAULT,'MaximumPoints':0})
 add('empty-name-zero-cap',polyline(h=header(name=b'')),limits={**DEFAULT,'MaximumStringBytes':0,'MaximumTotalStringBytes':1})
 add('empty-name-zero-total',polyline(h=header(name=b'')),'Input',{**DEFAULT,'MaximumStringBytes':0,'MaximumTotalStringBytes':0})
 add('opaque-zero-decoded-caps',polyline(b'0099'),limits={**DEFAULT,'MaximumPoints':0,'MaximumStringBytes':0,'MaximumTotalStringBytes':0,'MaximumDecodedCopyBytes':0})
 add('opaque-node-cap',polyline(b'0099'),'Input',{**DEFAULT,'MaximumNodes':2})
 add('opaque-depth-cap',form(b'BPLN',form(b'0099',form(b'ZZZZ'))),'Input',{**DEFAULT,'MaximumDepth':1})
 add('implicit-large-point-cap',polyline(b'0000',points=[(0,0)]*8193),'Input')
 out[0]['SectionGeneratorHex']=m.encode(last=form(b'LYRS',m.layer(children=full))).hex().upper()
 return out
def metadata(b):
 root,=records(b,0,len(b));assert root[0]==b'FORM' and root[1][:4]==b'BPLN'
 v,=records(b,12,len(b));tag=v[1][:4].decode();h=None;meta=None;pts=[];strings=copies=0;mw=[];finite=None
 if tag.encode() in VERS:
  hdr,d=records(b,v[2]+12,v[3]);hv,=records(b,hdr[2]+12,hdr[3]);ht=hv[1][:4].decode()
  if ht in ('0000','0001'):
   hd,=records(b,hv[2]+12,hv[3]);raw=hd[1];enabled=struct.unpack_from('<I',raw)[0];end=raw.index(0,4);hn=raw[4:end];hl=raw[end+1:];strings=len(hn)+1
   h=dict(Version=ht,EnabledBits=enabled,IsEnabled=enabled!=0,NameSha256=sha(hn),LegacySha256=sha(hl))
   raw=d[1];count=None
   if tag=='0000':
    ft,fa,width=struct.unpack_from('<III',raw);pairs=raw[12:];mw=[ft,fa,width]
   else:
    count,=struct.unpack_from('<I',raw);pairs=raw[4:4+8*count];ft,fa,width=struct.unpack_from('<III',raw,4+8*count);assert len(raw)==16+8*count;mw=[count,ft,fa,width]
   assert len(pairs)%8==0
   finite_bits=lambda bits:bits&0x7f800000!=0x7f800000
   pts=[dict(FirstBits=a,SecondBits=z,IsFinite=finite_bits(a) and finite_bits(z)) for a,z in struct.iter_unpack('<II',pairs)]
   finite=all(p['IsFinite'] for p in pts)
   meta=dict(SerializedPointCountBits=count,FeatheringTypeBits=ft,FeatheringAmountBits=fa,WidthBits=width,HasFiniteSerializedFeatheringAmount=finite_bits(fa),HasFiniteWidth=finite_bits(width))
   copies=len(pairs)+4*len(mw)+len(hn)+len(hl)
 return dict(Version=tag,ClientVersionRecognized=tag.encode() in VERS,HasDecodedFields=meta is not None,HasFinitePoints=finite,DecodedStringBytes=strings,DecodedCopyBytes=copies,Header=h,Metadata=meta,Points=pts,MetadataWordBits=mw,EncodedBytes=len(b),Sha256=sha(b))
def original(root,cfg):
 assert sha(cfg.read_bytes())==m.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,cfg):
   raw=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if raw is not None:break
  else:raise AssertionError('missing pinned terrain')
 assert priority==42 and len(raw)==1188446 and sha(raw)==m.SOURCE
 v,=records(raw,12,len(raw));d,g,maps=records(raw,v[2]+12,v[3]);b=raw[g[2]:g[3]];assert g[2]==210 and len(b)==139608 and sha(b)==m.GENERATOR
 polylines=[]
 for rule in m.metadata(b)['Rules']:
  sec=rule['Section']
  if sec['FormType']=='BPLN':
   a=sec['Offset'];e=a+sec['EncodedBytes'];polylines.append(dict(LayerIndex=rule['LayerIndex'],ChildOrdinal=rule['ChildOrdinal'],Offset=a,Metadata=metadata(b[a:e])))
 assert len(polylines)==34 and all(c['Metadata']['Version']=='0001' for c in polylines)
 return dict(ConfigSha256=m.CONFIG,SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=priority,SourceBytes=len(raw),GeneratorBytes=len(b),Polylines=polylines)
def static(exe):
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 assert sha(exe.read_bytes())==m.EXE;p=pefile.PE(str(exe),fast_load=True);b=p.get_memory_mapped_image();base=p.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 def ins(a,e):return list(md.disasm(b[a-base:e-base],a))
 checks=0
 def has(a,e,mn,op):
  nonlocal checks
  assert any(i.mnemonic==mn and i.op_str==op for i in ins(a,e)),(hex(a),mn,op);checks+=1
 has(0xb79df0,0xb79fd3,'cmp','eax, 0x42504c4e');has(0xb79e48,0xb79e93,'push','0x3c');has(0xb79e48,0xb79e93,'call','0xb74a20');has(0xb79e48,0xb79e93,'call','dword ptr [edx + 8]')
 has(0xb74a20,0xb74a40,'push','3');has(0xb74a20,0xb74a40,'push','0x42504c4e');has(0xb74a20,0xb74a40,'call','0xb736e0');has(0xb74a20,0xb74a40,'mov','dword ptr [esi + 0x38], 0x40000000');has(0xb74a20,0xb74a40,'mov','dword ptr [esi], 0x15ed474')
 assert [struct.unpack_from('<I',b,0x15ed474-base+o)[0] for o in (0,8,12,16,36,56)]==[0xb74a40,0xb74d00,0xb75000,0xb65fc0,0xb74ad0,0xb74aa0]
 has(0xb74d00,0xb74dc3,'sub','eax, 0x30303030');has(0xb74d00,0xb74dc3,'je','0xb74dae');has(0xb74d00,0xb74dc3,'dec','eax');has(0xb74d00,0xb74dc3,'je','0xb74d97')
 assert sum(i.mnemonic=='call' and i.op_str=='dword ptr [eax + 0x38]' for i in ins(0xb74d00,0xb74dc3))==3
 for a,e,n in [(0xb74dd0,0xb74ed4,5),(0xb74ee0,0xb74ff2,6)]:
  ii=ins(a,e);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ii)==n;assert sum(i.mnemonic=='push' and i.op_str=='4' for i in ii)==n
  has(0xb74d00,0xb74dc3,'call',hex(a));has(a,e,'call','0xb62fa0');has(a,e,'push','0x44415441');has(a,e,'call','0xb65fb0');has(a,e,'call','dword ptr [eax + 0x10]');has(a,e,'call','0xb74a80');has(a,e,'call','0xb750e0')
  assert not any(i.mnemonic.startswith('fcom') or (i.mnemonic=='call' and i.op_str=='0xa95200') for i in ii)
  assert sum(i.mnemonic=='mov' and i.op_str.startswith('dword ptr [') and '*8],' in i.op_str for i in ii)==1
  assert sum(i.mnemonic=='mov' and i.op_str.startswith('dword ptr [') and '*8 + 4],' in i.op_str for i in ii)==1
 has(0xb74dd0,0xb74ed4,'call','0xa94ee0');has(0xb74dd0,0xb74ed4,'push','8');has(0xb74ee0,0xb74ff2,'jle','0xb74f88');assert not any(i.mnemonic=='call' and i.op_str=='0xa94ee0' for i in ins(0xb74ee0,0xb74ff2))
 # Prove relative metadata/point read order in both layouts.
 for a,e,expected in [(0xb74dd0,0xb74ed4,[0xb74e0d,0xb74e25,0xb74e3d,0xb74e68,0xb74e7b]),(0xb74ee0,0xb74ff2,[0xb74f1d,0xb74f38,0xb74f4b,0xb74f90,0xb74fa8,0xb74fc0])]:assert [i.address for i in ins(a,e) if i.mnemonic=='call' and i.op_str=='0xa94f00']==expected
 has(0xb74a80,0xb74a90,'mov','dword ptr [ecx + 0x38], eax');has(0xb74a80,0xb74a90,'call','dword ptr [edx + 0x38]')
 has(0xb74aa0,0xb74acf,'call','0xb738b0')
 for op in ('dword ptr [ecx + 0x28]','dword ptr [ecx + 0x34]'):has(0xb74aa0,0xb74acf,'fstp',op)
 assert sum(i.mnemonic=='fsub' and i.op_str=='dword ptr [ecx + 0x38]' for i in ins(0xb74aa0,0xb74acf))==2
 assert sum(i.mnemonic=='fadd' and i.op_str=='dword ptr [ecx + 0x38]' for i in ins(0xb74aa0,0xb74acf))==2
 has(0xb74a40,0xb74a5f,'call','0xb74a70');has(0xb74a40,0xb74a5f,'call','0xac1640');has(0xb74a70,0xb74a7b,'jmp','0xb73740')
 has(0xb73740,0xb7379e,'call','0xac1660');has(0xb73740,0xb7379e,'call','0xb63350');has(0xb65fc0,0xb65fcb,'mov','dword ptr [ecx + 0x18], eax')
 has(0xa94ee0,0xa94efe,'idiv','dword ptr [ebp + 8]');has(0xa94f00,0xa94f45,'rep movsd','dword ptr es:[edi], dword ptr [esi]')
 has(0xb738b0,0xb73925,'mov','eax, 0x7f7fffff');has(0xb738b0,0xb73925,'fld','dword ptr [eax + edx*8 + 4]')
 has(0xb63d20,0xb6428f,'call','0xb62f20');has(0xb63d20,0xb6428f,'call','dword ptr [edx + 0x24]')
 old=ins(0xb63070,0xb6311b);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in old)==4
 for address in (0xb630d2,0xb630df,0xb630ec):assert any(i.address==address and i.mnemonic=='push' and i.op_str=='1' for i in old)
 return dict(ExecutableSha256=m.EXE,InstructionAnchors=checks,VersionReadSites=[5,6],Vtable='015ED474',Loader='00B74D00',DerivedCountVersions=[0],ExplicitCountVersions=[1])
if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2));print('BPLN fixtures:',len(fixtures()))
 else:
  root,cfg,log=map(Path,sys.argv[1:]);lines=log.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);fs=fixtures();assert len(fs)==len(r['Cases'])
  for f,a in zip(fs,r['Cases']):
   assert (a['Name'],a['Reject'])==(f['Name'],f['Reject'])
   if f['Reject']=='None':assert a['Metadata']==metadata(bytes.fromhex(f['Hex'])),f['Name']
  assert r['OwnershipChecks']==18 and r['Original']==original(root,cfg)
  print(json.dumps(dict(passed=True,cases=len(fs),accepted=sum(f['Reject']=='None' for f in fs),rejected=sum(f['Reject']!='None' for f in fs),ownership_checks=18,static=static(root/'Stardust.exe'),original=dict(Polylines=len(r['Original']['Polylines']),Versions=['0001'],SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR)),indent=2))
