"""Synthetic TGEN vectors; independent raw structure and one original input oracle."""
import contextlib,hashlib,json,struct,sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
SOURCE='6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
GENERATOR='B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32'
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
DEFAULT=dict(MaximumInputBytes=8388608,MaximumNodes=50000,MaximumDepth=64,MaximumLayers=4096,MaximumLayerDepth=32,MaximumRules=32768,MaximumGroupRecords=16384,MaximumStringBytes=4096,MaximumTotalStringBytes=1048576,MaximumSectionBytes=4194304)
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def chunk(t,b):return t+struct.pack('>I',len(b))+b
def form(t,b=b''):return chunk(b'FORM',t+b)
def words(v):return struct.pack('<'+'I'*len(v),*v)
def header(version=b'0001',enabled=0xffffffff,name=b'synth',raw=None):
 raw=words([enabled])+name+b'\0'+(b'\x11\x80\xff' if version==b'0000' else b'') if raw is None else raw
 return form(b'IHDR',form(version,chunk(b'DATA',raw)))
def ahcn(version=b'0000',h=None,raw=None):return form(b'AHCN',form(version,(header() if h is None else h)+chunk(b'DATA',words([0xfffffffe,0x80000000]) if raw is None else raw)))
def layer(version=b'0003',h=None,adta=None,children=b''):
 n=2 if version==b'0001' else 4 if version==b'0004' else 3
 raw=words([0xffffffff,0,7,0x7fc00001][:n])+(b'layer\0' if version in (b'0003',b'0004') else b'') if adta is None else adta
 return form(b'LAYR',form(version,(header() if h is None else h)+chunk(b'ADTA',raw)+children))
TAGS=[b'SGRP',b'FGRP',b'RGRP',b'EGRP',b'MGRP'];VERS=[b'0006',b'0008',b'0003',b'0002',b'0000']
GROUPS=[form(t,form(v,form(b'UFAM',chunk(b'DATA',bytes([i]))))) for i,(t,v) in enumerate(zip(TAGS,VERS))]
RULE=ahcn(h=header(b'0000'));UNKNOWN=form(b'ZZZZ',form(b'0099',chunk(b'DATA',b'odd')))
LAYERS=form(b'LYRS',layer(children=RULE+UNKNOWN+layer(b'0001',children=ahcn())))
def encode(groups=None,last=LAYERS):return form(b'TGEN',form(b'0000',b''.join(GROUPS if groups is None else groups)+last))
def fixtures():
 out=[]
 def add(n,b,reject='None',limits=None):out.append(dict(Name=n,Hex=b.hex().upper(),Reject=reject,Limits=limits or DEFAULT))
 full=encode();add('owned-nested',full);add('no-layers',encode(last=b''));add('empty-lyrs',encode(last=form(b'LYRS')));add('optional-bitmap',encode(groups=GROUPS+[form(b'MGRP',form(b'0000'))]));add('unknown-group-version',encode(groups=[form(b'SGRP',form(b'0099',UNKNOWN))]+GROUPS[1:]));add('unsupported-layer',encode(last=form(b'LYRS',form(b'LAYR',form(b'0009',UNKNOWN)))));add('legacy-layer-opaque',encode(last=form(b'LYRS',form(b'LAYR',form(b'0000',UNKNOWN)))))
 for v in (b'0001',b'0002',b'0003',b'0004'):add('layer-'+v.decode(),encode(last=form(b'LYRS',layer(v,children=ahcn()))))
 add('unsupported-ahcn',encode(last=form(b'LYRS',layer(children=ahcn(b'0009')))));add('unknown-inherited-header',encode(last=form(b'LYRS',layer(children=ahcn(h=header(b'0009'))))))
 add('unknown-layer-header',encode(last=form(b'LYRS',layer(h=header(b'0009')))))
 for op,ht in ((0,0),(1,0x7fc12345),(2,0x7f800000),(0x80000000,0xff800000),(0xffffffff,0xbf800000)):
  add('raw-operation-height-'+str(op),encode(last=form(b'LYRS',layer(children=ahcn(raw=words([op,ht]))))))
 add('raw-byte-name',encode(last=form(b'LYRS',layer(h=header(name=b'\xff\x80')))))
 for i in range(len(full)):add('truncated-'+str(i),full[:i],'Input')
 add('trailing-byte',full+b'\0','Input');add('second-root',full+form(b'TGEN'),'Input')
 add('wrong-root',form(b'PTAT',full[12:]),'Input');add('wrong-version',form(b'TGEN',form(b'0001',b''.join(GROUPS))),'Input');add('duplicate-version',form(b'TGEN',full[12:]+full[12:]),'Input')
 add('missing-group',encode(groups=GROUPS[1:]),'Input');add('swapped-groups',encode(groups=[GROUPS[1],GROUPS[0]]+GROUPS[2:]),'Input');add('third-mgrp',encode(groups=GROUPS+[GROUPS[-1],GROUPS[-1]]),'Input');add('trailing-top-record',encode(last=LAYERS+UNKNOWN),'Input');add('direct-layer',encode(last=layer()),'Input');add('lyrs-wrong-child',encode(last=form(b'LYRS',UNKNOWN)),'Input');add('group-no-version',encode(groups=[form(b'SGRP')]+GROUPS[1:]),'Input')
 for raw in (bytes(7),bytes(9)):add('ahcn-data-length-'+str(len(raw)),encode(last=form(b'LYRS',layer(children=ahcn(raw=raw)))),'Input')
 add('ahcn-order',encode(last=form(b'LYRS',layer(children=form(b'AHCN',form(b'0000',chunk(b'DATA',bytes(8))+header()))))),'Input')
 add('ahcn-duplicate-version',encode(last=form(b'LYRS',layer(children=form(b'AHCN',form(b'0000')+form(b'0000'))))),'Input')
 for v in (b'0000',b'0001'):
  proper=words([1])+b'h\0'+(b'abc' if v==b'0000' else b'')
  for i in range(len(proper)):add('header-'+v.decode()+'-short-'+str(i),encode(last=form(b'LYRS',layer(children=ahcn(h=header(v,raw=proper[:i]))))),'Input')
  add('header-'+v.decode()+'-extra',encode(last=form(b'LYRS',layer(children=ahcn(h=header(v,raw=proper+b'x'))))),'Input')
 for v in (b'0001',b'0002',b'0003',b'0004'):
  n=2 if v==b'0001' else 4 if v==b'0004' else 3;adta=bytes(n*4)+(b'layer\0' if v in (b'0003',b'0004') else b'')
  for i in range(len(adta)):add('adta-'+v.decode()+'-short-'+str(i),encode(last=form(b'LYRS',layer(v,adta=adta[:i]))),'Input')
  add('adta-'+v.decode()+'-extra',encode(last=form(b'LYRS',layer(v,adta=adta+b'x'))),'Input')
 add('layer-order',encode(last=form(b'LYRS',form(b'LAYR',form(b'0003',chunk(b'ADTA',bytes(12)+b'\0')+header())))),'Input')
 huge=bytearray(full);struct.pack_into('>I',huge,4,0xffffffff);add('huge-extent',huge,'Input')
 # Exact native-independent structural budgets are derived from a literal recursive IFF walk.
 nodes,depth=dimensions(full);meta=metadata(full)
 exact={**DEFAULT,'MaximumInputBytes':len(full),'MaximumNodes':nodes,'MaximumDepth':depth,'MaximumLayers':len(meta['Layers']),'MaximumLayerDepth':1,'MaximumRules':len(meta['Rules']),'MaximumGroupRecords':5,'MaximumStringBytes':5,'MaximumTotalStringBytes':meta['DecodedStringBytes'],'MaximumSectionBytes':len(LAYERS)}
 add('exact-limits',full,limits=exact)
 for k in exact:
  if exact[k]!=DEFAULT[k]:add('one-less-'+k,full,'Input',{**exact,k:exact[k]-1})
 for k,low,high in [('MaximumInputBytes',23,67108865),('MaximumNodes',0,1000001),('MaximumDepth',0,129),('MaximumLayers',-1,65537),('MaximumLayerDepth',-1,65),('MaximumRules',-1,262145),('MaximumGroupRecords',-1,262145),('MaximumStringBytes',-1,65537),('MaximumTotalStringBytes',-1,16777217),('MaximumSectionBytes',11,33554433)]:
  for v in (low,high):add('bad-option-'+k+str(v),full,'Options',{**DEFAULT,k:v})
 return out

def records(b,start,end):
 while start<end:
  assert end-start>=8;t=b[start:start+4];size=struct.unpack_from('>I',b,start+4)[0];stop=start+8+size;assert stop<=end
  yield t,b[start+8:stop],start,stop;start=stop
 assert start==end

def dimensions(b):
 count=0;maximum=0
 def walk(start,end,depth):
  nonlocal count,maximum
  for t,p,a,e in records(b,start,end):
   count+=1;maximum=max(maximum,depth)
   if t==b'FORM':walk(a+12,e,depth+1)
 walk(0,len(b),0);return count,maximum

def metadata(b):
 def section(r):
  t,p,a,e=r;cs=list(records(b,a+12,e)) if t==b'FORM' else []
  return dict(Tag=t.decode(),FormType=p[:4].decode() if t==b'FORM' else None,Version=cs[0][1][:4].decode() if len(cs)==1 and cs[0][0]==b'FORM' else None,Offset=a,EncodedBytes=e-a,Sha256=sha(b[a:e]))
 strings=0
 def hd(r):
  nonlocal strings
  vs,=records(b,r[2]+12,r[3]);version=vs[1][:4].decode()
  if version not in ('0000','0001'):return None
  d,=records(b,vs[2]+12,vs[3]);raw=d[1];enabled=struct.unpack_from('<I',raw)[0];end=raw.index(0,4);name=raw[4:end];legacy=raw[end+1:];assert len(legacy)==(3 if version=='0000' else 0);strings+=len(name)+1
  return dict(Version=version,EnabledBits=enabled,NameSha256=sha(name),LegacySha256=sha(legacy))
 top,=records(b,0,len(b));assert top[1][:4]==b'TGEN';v,=records(b,12,len(b));assert v[1][:4]==b'0000';cs=list(records(b,24,len(b)));groups=[];layers=[];rules=[]
 roles=['Shader','Flora','Radial','Environment','FractalMap','Bitmap'];ranges=[range(7),range(1,9),range(4),range(3),range(1),range(1)]
 n=6 if len(cs)>5 and cs[5][1][:4]==b'MGRP' else 5
 for i,r in enumerate(cs[:n]):
  ver,=records(b,r[2]+12,r[3]);version=ver[1][:4].decode();rows=list(records(b,ver[2]+12,ver[3]));groups.append(dict(Role=roles[i],Section=section(r),ClientVersionRecognized=version in [f'{x:04d}' for x in ranges[i]],RecordCount=len(rows)))
 def layer(r,parent,depth):
  nonlocal strings
  index=len(layers);ver,=records(b,r[2]+12,r[3]);version=ver[1][:4].decode();child=list(records(b,ver[2]+12,ver[3]));h=None;flags=[];desc=b'';children=[]
  if version in ('0001','0002','0003','0004'):
   h=hd(child[0])
   if h is not None:
    raw=child[1][1];count=2 if version=='0001' else 4 if version=='0004' else 3;flags=list(struct.unpack_from('<'+'I'*count,raw));cursor=4*count
    if version in ('0003','0004'):end=raw.index(0,cursor);desc=raw[cursor:end];cursor=end+1;strings+=len(desc)+1
    assert cursor==len(raw);children=[section(x) for x in child[2:]]
  layers.append(dict(Index=index,ParentIndex=parent,Depth=depth,Section=section(r),Header=h,AdtaWordBits=flags,DescriptionSha256=sha(desc),Children=children))
  if h is None:return
  for ordinal,x in enumerate(child[2:],2):
   if x[0]==b'FORM' and x[1][:4]==b'LAYR':layer(x,index,depth+1)
   else:
    height=None
    if x[0]==b'FORM' and x[1][:4]==b'AHCN':
     av,=records(b,x[2]+12,x[3]);parts=list(records(b,av[2]+12,av[3]))
     if av[1][:4]==b'0000':
      hdr=hd(parts[0])
      if hdr is not None:op,ht=struct.unpack('<II',parts[1][1]);height=dict(Header=hdr,OperationBits=op,HeightBits=ht)
    rules.append(dict(LayerIndex=index,ChildOrdinal=ordinal,Section=section(x),HeightConstant=height))
 if len(cs)>n:
  for r in records(b,cs[n][2]+12,cs[n][3]):layer(r,None,0)
 return dict(GeneratorBytes=len(b),GeneratorSha256=sha(b),Groups=groups,Layers=layers,Rules=rules,GroupRecordCount=sum(x['RecordCount'] for x in groups),DecodedStringBytes=strings,HeightConstantCount=sum(x['HeightConstant'] is not None for x in rules))

def original(root,cfg):
 assert sha(cfg.read_bytes())==CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in archives(root,cfg):
   raw=Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if raw is not None:break
  else:raise AssertionError('terrain/tatooine.trn')
 assert priority==42 and len(raw)==1188446 and sha(raw)==SOURCE
 v,=records(raw,12,len(raw));d,g,m=records(raw,v[2]+12,v[3]);b=raw[g[2]:g[3]];assert g[2]==210 and len(b)==139608 and sha(b)==GENERATOR
 meta=metadata(b);assert len(meta['Layers'])==257 and meta['HeightConstantCount']==87
 return dict(Root=root.as_posix(),Configuration=cfg.as_posix(),ConfigSha256=CONFIG,Path='terrain/tatooine.trn',Priority=priority,SourceBytes=len(raw),SourceSha256=SOURCE,GeneratorOffset=g[2],Metadata=meta)

def static(exe):
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 assert sha(exe.read_bytes())==EXE;pe=pefile.PE(str(exe),fast_load=True);b=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 def ins(a,e):return list(md.disasm(b[a-base:e-base],a))
 def has(a,e,mn,op):assert any(i.mnemonic==mn and i.op_str==op for i in ins(a,e))
 t=ins(0xb65cc0,0xb65db4);targets=[i.op_str for i in t if i.mnemonic=='call'];sequence=[hex(x) for x in (0xb69c90,0xb67300,0xb6c1a0,0xb6d990,0xb75920,0xb7ae80)];assert [x for x in targets if x in sequence]==sequence
 for a,e,tag in [(0xb69c90,0xb69da0,'0x53475250'),(0xb67300,0xb67420,'0x46475250'),(0xb6c1a0,0xb6c250,'0x52475250'),(0xb6d990,0xb6da10,'0x45475250'),(0xb75920,0xb75970,'0x4d475250'),(0xb7ae80,0xb7aed0,'0x4d475250')]:has(a,e,'push',tag)
 has(0xb7ae80,0xb7aed0,'je','0xb7aec6');has(0xb65cc0,0xb65db4,'push','0x4c595253')
 has(0xb647c0,0xb6484b,'cmp','eax, 4');has(0xb7a240,0xb7a850,'call','0xb764b0');has(0xb764b0,0xb764d1,'mov','dword ptr [esi], 0x15ed678')
 assert struct.unpack_from('<I',b,0x15ed680-base)[0]==0xb76650
 has(0xb76650,0xb766e1,'cmp','eax, 0x30303030');has(0xb766f0,0xb76768,'call','0xb62fa0')
 r=ins(0xb766f0,0xb76768);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in r)==2
 has(0xb766f0,0xb76768,'mov','dword ptr [edi + 0x14], ecx');has(0xb766f0,0xb76768,'mov','dword ptr [edi + 0x18], eax')
 old=ins(0xb63070,0xb6311b);assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in old)==4
 for address in (0xb630d2,0xb630df,0xb630ec):assert any(i.address==address and i.mnemonic=='push' and i.op_str=='1' for i in old)
 for a,e,count in [(0xb64a30,0xb64ad7,2),(0xb64ae0,0xb64ba9,3),(0xb64bb0,0xb64c94,3),(0xb64ca0,0xb64d94,4)]:assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ins(a,e))==count
 return dict(ExecutableSha256=EXE,OrderedGroupCalls=6,AhcnVtable='015ED678',AhcnLoader='00B766F0',AhcnDataWords=2,LayerVersions=[1,2,3,4],HeaderLegacyDiscardBytes=3)

if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('TGEN fixtures:',len(fixtures()))
 else:
  root,cfg,log=map(Path,sys.argv[1:]);lines=log.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);fs=fixtures();assert len(fs)==len(r['Cases'])
  for f,a in zip(fs,r['Cases']):
   assert a['Name']==f['Name'] and a['Reject']==f['Reject']
   if f['Reject']=='None':assert a['Metadata']==metadata(bytes.fromhex(f['Hex'])),f['Name']
  assert r['OwnershipChecks']==10 and r['Original']==original(root,cfg)
  print(json.dumps(dict(passed=True,cases=len(fs),accepted=sum(f['Reject']=='None' for f in fs),ownership_checks=10,static=static(root/'Stardust.exe'),original=dict(SourceSha256=SOURCE,GeneratorSha256=GENERATOR,Groups=[(g['Role'],g['Section']['Version'],g['RecordCount']) for g in r['Original']['Metadata']['Groups']],Layers=len(r['Original']['Metadata']['Layers']),Rules=len(r['Original']['Metadata']['Rules']),HeightConstants=r['Original']['Metadata']['HeightConstantCount'],GroupRecords=r['Original']['Metadata']['GroupRecordCount'],DecodedStringBytes=r['Original']['Metadata']['DecodedStringBytes'])),indent=2))
