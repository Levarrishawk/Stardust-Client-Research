"""Independent PTAT fixtures, static loader anchors, and one pinned overlay sample."""
import contextlib, hashlib, json, struct, sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
SOURCE='6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
DEFAULT=dict(MaximumInputBytes=16777216,MaximumDataBytes=65536,MaximumGeneratorBytes=8388608,MaximumStringBytes=4096,MaximumNodes=100000,MaximumDepth=32,MaximumTilesPerChunk=4096,MapLimits=None)
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def chunk(t,b):return t+struct.pack('>I',len(b))+b
def form(t,b=b''):return chunk(b'FORM',t+b)
def words(v):return struct.pack('<'+'I'*len(v),*v)
P=[0x46800000,0x42000000,16,7,0x80000000,0x41000000,0x46304000]
BRANCH=b'a\0b\0'+words([0x7fc00001])+b'c\0'+words([2])+b'd\0'+words([3])+b'e\0'+words([4,0xffffffff])+b'f\0'
META=[0x80000000,0x7fc12345,0x7f800000,0xffffffff]+list(range(16))
GEN=form(b'TGEN',form(b'0000',chunk(b'UNKN',b'odd')))
MAP=form(b'0001',chunk(b'DATA',words([0x46800000,0x41000000,1,2]))+chunk(b'WMAP',b'\x01\x80')+chunk(b'SMAP',b'\x40\x02'))
def data(version=b'0014',p=None,name=b'test',shader=b'water',tail=b''):
 return name+b'\0'+words(P[:6] if p is None else p[:6])+shader+b'\0'+words([P[6] if p is None else p[6]])+(BRANCH if version==b'0013' else b'')+words(META)+tail
def encode(version=b'0014',raw=None,parts=None):
 raw=data(version) if raw is None else raw
 return form(b'PTAT',form(version,b''.join(parts or [chunk(b'DATA',raw),GEN,MAP])))
def fixtures():
 out=[]
 def add(n,b,reject='None',limits=None):out.append(dict(Name=n,Hex=b.hex().upper(),Reject=reject,Limits=limits or DEFAULT))
 full=encode();add('v14',full);add('v13',encode(b'0013'));add('unknown-tail',encode(raw=data(tail=b'\x00\xff\x7f')));add('raw-byte-strings',encode(raw=data(name=b'\xff\xfe',shader=b'\x80')));add('empty-map',encode(parts=[chunk(b'DATA',data()),GEN,form(b'0000')]))
 p=P.copy();p[3]=0;p[4]=0x7fc12345;p[5]=0xff800000;add('inactive-water-raw-bits',encode(raw=data(p=p)))
 p=P.copy();p[0]=p[1]=p[6]=1;add('positive-subnormal',encode(raw=data(p=p)))
 for ver in (b'0013',b'0014'):
  raw=data(ver)
  for i in range(len(raw)):add(ver.decode()+'-short-data-'+str(i),encode(ver,raw[:i]),'Input')
 for i in range(len(full)):add('truncated-envelope-'+str(i),full[:i],'Input')
 add('trailing-byte',full+b'\0','Input');add('second-root',full+form(b'PTAT'),'Input')
 for i,parts in enumerate(([GEN,chunk(b'DATA',data()),MAP],[chunk(b'DATA',data()),MAP,GEN],[chunk(b'DATA',data()),GEN],[chunk(b'DATA',data()),GEN,MAP,MAP],[chunk(b'DATX',data()),GEN,MAP],[form(b'DATA'),GEN,MAP],[chunk(b'DATA',data()),form(b'TGEN',form(b'0001')),MAP],[chunk(b'DATA',data()),form(b'TGEN'),MAP],[chunk(b'DATA',data()),GEN,form(b'MAPS',MAP)],[chunk(b'DATA',data()),GEN,form(b'0002')],[chunk(b'DATA',data()),GEN,form(b'0001',b'bad')])):add('wrong-structure-'+str(i),encode(parts=parts),'Input')
 add('unknown-version',encode(b'0015'),'Input');add('wrong-root',form(b'MPTA',full[12:]),'Input');add('duplicate-version',form(b'PTAT',full[12:]+full[12:]),'Input')
 huge=bytearray(full);struct.pack_into('>I',huge,4,0xffffffff);add('huge-extent',huge,'Input')
 add('missing-name-nul',encode(raw=b'X'*len(data())),'Input');add('v13-missing-branch',encode(b'0013',data()),'Input')
 for i in (0,1,6):
  for bad in (0,0x80000000,0xbf800000,0x7f800000,0xff800000,0x7fc12345):
   p=P.copy();p[i]=bad;add('bad-finite-'+str(i)+'-'+str(bad),encode(raw=data(p=p)),'Input')
 for i,bad in ((2,0),(2,0xffffffff),(4,0x7f800000),(4,0x7fc00000),(5,0),(5,0xbf800000),(5,0x7f800000)):
  p=P.copy();p[i]=bad;add('bad-active-'+str(i)+'-'+str(bad),encode(raw=data(p=p)),'Input')
 exact={**DEFAULT,'MaximumInputBytes':len(full),'MaximumDataBytes':len(data()),'MaximumGeneratorBytes':len(GEN),'MaximumStringBytes':5,'MaximumNodes':10,'MaximumDepth':4,'MaximumTilesPerChunk':16}
 add('exact-limits',full,limits=exact)
 for k in ('MaximumInputBytes','MaximumDataBytes','MaximumGeneratorBytes','MaximumStringBytes','MaximumNodes','MaximumDepth','MaximumTilesPerChunk'):add('one-less-'+k,full,'Input',{**exact,k:exact[k]-1})
 for k,vs in [('MaximumInputBytes',[35,67108865]),('MaximumDataBytes',[0,1048577]),('MaximumGeneratorBytes',[11,33554433]),('MaximumStringBytes',[-1,65537]),('MaximumNodes',[0,1000001]),('MaximumDepth',[0,129]),('MaximumTilesPerChunk',[0,1048577])]:
  for v in vs:add('bad-option-'+k+str(v),full,'Options',{**DEFAULT,k:v})
 add('map-budget',full,'Input',{**DEFAULT,'MapLimits':dict(MaximumInputBytes=16777216,MaximumBitDimension=1,MaximumBytesPerRow=1,MaximumMaskBytes=2)})
 add('bad-map-options',full,'Options',{**DEFAULT,'MapLimits':dict(MaximumInputBytes=0)})
 return out

def records(b,start,end):
 while start<end:
  assert end-start>=8;t=b[start:start+4];size=struct.unpack_from('>I',b,start+4)[0];stop=start+8+size;assert stop<=end
  yield t,b[start+8:stop],start,stop;start=stop
 assert start==end

def metadata(b):
 top,=records(b,0,len(b));assert top[0]==b'FORM' and top[1][:4]==b'PTAT'
 v,=records(b,12,len(b));version=v[1][:4].decode();assert version in ('0013','0014')
 d,g,m=list(records(b,v[2]+12,v[3]));assert d[0]==b'DATA' and g[1][:4]==b'TGEN' and m[1][:4] in (b'0000',b'0001')
 raw=d[1];cursor=0
 def string():
  nonlocal cursor
  end=raw.index(0,cursor);value=raw[cursor:end];cursor=end+1;return value
 def word():
  nonlocal cursor
  value=struct.unpack_from('<I',raw,cursor)[0];cursor+=4;return value
 name=string();p=[word() for _ in range(6)];shader=string();p.append(word());start=cursor
 if version=='0013':
  string();string()
  for _ in range(3):word();string()
  word();word();string()
 branch=raw[start:cursor];block=[word() for _ in range(20)];tail=raw[cursor:]
 mapraw=b[m[2]:m[3]];mapversion=m[1][:4].decode();w=s=b'';mapdata=[]
 if mapversion=='0001':
  md,mw,ms=records(mapraw,12,len(mapraw));mapdata=list(struct.unpack('<4I',md[1]));w,s=mw[1],ms[1];assert len(w)==len(s)==mapdata[2]*mapdata[3]
 return dict(Version=version,Prefix=p,MetadataWords=block,TerrainNameHex=name.hex().upper(),WaterShaderHex=shader.hex().upper(),DataBytes=len(raw),DataSha256=sha(raw),GeneratorBytes=g[3]-g[2],GeneratorSha256=sha(b[g[2]:g[3]]),Version13BranchSha256=sha(branch),UnknownTailBytes=len(tail),UnknownTailSha256=sha(tail),MapVersion=mapversion,MapData=mapdata,WmapSha256=sha(w),SmapSha256=sha(s)),dict(DataOffset=d[2],GeneratorOffset=g[2],MapOffset=m[2],MapBytes=len(mapraw),MapSha256=sha(mapraw))

def original(root,cfg):
 assert sha(cfg.read_bytes())==CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in archives(root,cfg):
   index=Index(stack.enter_context(path.open('rb')));raw=index.lookup('terrain/tatooine.trn')
   if raw is not None:break
  else:raise AssertionError('terrain/tatooine.trn')
 assert priority==42 and len(raw)==1188446 and sha(raw)==SOURCE
 meta,offsets=metadata(raw)
 return dict(Root=root.as_posix(),Configuration=cfg.as_posix(),ConfigSha256=CONFIG,Path='terrain/tatooine.trn',Priority=priority,SourceBytes=len(raw),SourceSha256=SOURCE,Metadata=meta,**offsets)

def static(exe):
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 assert sha(exe.read_bytes())==EXE
 pe=pefile.PE(str(exe),fast_load=True);mapped=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 def instructions(start,end):return list(md.disasm(mapped[start-base:end-base],start))
 a=instructions(0xb6eab0,0xb6ee95);b=instructions(0xb6eea0,0xb6f1b3)
 def calls(x,target):return sum(i.mnemonic=='call' and i.op_str==hex(target) for i in x)
 assert calls(a,0xa94f00)==32 and calls(b,0xa94f00)==27
 assert calls(a,0xa95200)==calls(b,0xa95200)==2 and calls(a,0xa95270)==6 and calls(b,0xa95270)==0
 for x in (a,b):assert calls(x,0xb65c50)==calls(x,0xb9a470)==1
 for x,register in ((a,'edi'),(b,'esi')):
  for offset in range(0x44,0x94,4):assert any(i.mnemonic=='mov' and i.op_str.startswith('dword ptr ['+register+' + '+hex(offset)+'],') for i in x)
 getters=instructions(0xb6e570,0xb6e6b0)
 for index in range(20):
  address=0xb6e570+index*16;offset=0x44+index*4
  expected=('mov','eax, dword ptr [ecx + '+hex(offset)+']') if index%5==4 else ('fld','dword ptr [ecx + '+hex(offset)+']')
  assert any(i.address==address and (i.mnemonic,i.op_str)==expected for i in getters)
 reader=instructions(0xa94f00,0xa94f45);assert any(i.address==0xa94f2f and i.mnemonic=='rep movsd' for i in reader)
 gate=instructions(0xb6e980,0xb6eaa6);assert any(i.address==0xb6e9d0 and i.op_str=='eax, 0x30303133' for i in gate)
 gen=instructions(0xb65c50,0xb65cb0);assert any(i.mnemonic=='cmp' and i.op_str=='eax, 0x30303030' for i in gen)
 return dict(ExecutableSha256=EXE,Version13WordReads=32,Version14WordReads=27,Version13DiscardStrings=6,CommonOwnedStrings=2,GeneratorVersion='0000',MetadataWordStores=40,MetadataTypedAccessors=20,RawLittleEndianCopy=True)

if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('PTAT fixtures:',len(fixtures()))
 else:
  root,cfg,log=map(Path,sys.argv[1:]);lines=log.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);fs=fixtures();assert len(r['Cases'])==len(fs)
  for f,a in zip(fs,r['Cases']):
   assert a['Name']==f['Name'] and a['Reject']==f['Reject']
   if f['Reject']=='None':assert a['Metadata']==metadata(bytes.fromhex(f['Hex']))[0],f['Name']
  assert r['OwnershipChecks']==9 and r['AccessorRejections']==2
  assert r['Original']==original(root,cfg)
  print(json.dumps(dict(passed=True,cases=len(fs),accepted=sum(f['Reject']=='None' for f in fs),ownership_checks=9,accessor_rejections=2,static=static(root/'Stardust.exe'),original=r['Original']),indent=2))
