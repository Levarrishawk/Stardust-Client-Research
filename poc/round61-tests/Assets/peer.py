"""Independent SGRP0006 raw-byte model and SHA-gated selected client loader anchors."""
import contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('shader_tgen33',ROOT/'poc/round33-tests/Assets/verify_tgen_input.py');t=importlib.util.module_from_spec(spec);spec.loader.exec_module(t)
DEFAULT=dict(MaximumEncodedBytes=4194304,MaximumFamilies=4096,MaximumEntriesPerFamily=1024,MaximumTotalEntries=65536,MaximumStringBytes=999,MaximumTotalStringBytes=1048576,MaximumCopyBytes=8388608,MaximumArrayBytes=8388608,MaximumWork=16777216)
HARD=dict(MaximumEncodedBytes=8388608,MaximumFamilies=65536,MaximumEntriesPerFamily=65536,MaximumTotalEntries=262144,MaximumStringBytes=999,MaximumTotalStringBytes=8388608,MaximumCopyBytes=16777216,MaximumArrayBytes=33554432,MaximumWork=67108864)
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def sfam(key=0xffffffff,first=b'synthetic',second=b'raw',triple=b'\xff\x80\0',w0=0x7fc12345,w1=0x80000000,entries=((b'duplicate',0x7f800000),(b'duplicate',0xffffffff)),count=None):
 return t.chunk(b'SFAM',t.words([key])+first+b'\0'+second+b'\0'+triple+t.words([w0,w1,len(entries) if count is None else count])+b''.join(name+b'\0'+t.words([word]) for name,word in entries))
def group(records=b'',version=b'0006'):return t.form(b'SGRP',t.form(version,records))
def wrap(b):return t.encode(groups=[b]+t.GROUPS[1:],last=b'')
def inspect(b,limits=DEFAULT,bound=False,generator=None):
 e=len(b);mul=2 if bound else 1
 assert e<=limits['MaximumEncodedBytes'] and mul*e<=limits['MaximumCopyBytes'] and (mul+4)*e<=limits['MaximumWork']
 root,=t.records(b,0,e);assert root[0]==b'FORM' and root[1][:4]==b'SGRP';v,=t.records(b,12,e);assert v[0]==b'FORM' and v[1][:4]==b'0006'
 families=[];total=0;strings=0
 def text(payload,cursor):
  nonlocal strings
  stop=payload.index(0,cursor);value=payload[cursor:stop];assert len(value)<=limits['MaximumStringBytes'];strings+=len(value)+1;assert strings<=limits['MaximumTotalStringBytes'];return value,stop+1
 for index,(tag,p,a,end) in enumerate(t.records(b,24,e)):
  assert tag==b'SFAM';key,=struct.unpack_from('<I',p);first,q=text(p,4);second,q=text(p,q);assert len(p)-q>=15;triple=p[q:q+3];w0,w1,count=struct.unpack_from('<3I',p,q+3);q+=15
  assert count<=0x7fffffff and count<=limits['MaximumEntriesPerFamily'] and count*5<=len(p)-q;total+=count;assert total<=limits['MaximumTotalEntries'];entries=[]
  for j in range(count):
   start=q;name,q=text(p,q);word,=struct.unpack_from('<I',p,q);q+=4;entries.append(dict(Index=j,Offset=a+8+start,EncodedBytes=q-start,RawWordBits=word,StringHex=name.hex().upper(),EncodedSha256=sha(p[start:q])))
  assert q==len(p);families.append(dict(Index=index,Offset=a,EncodedBytes=end-a,FamilyKeyBits=key,RawWord0Bits=w0,RawWord1Bits=w1,EntryCountBits=count,FirstHex=first.hex().upper(),SecondHex=second.hex().upper(),TripleHex=triple.hex().upper(),EncodedSha256=sha(b[a:end]),Entries=entries))
  assert len(families)<=limits['MaximumFamilies']
 arrays=mul*e+256+128*len(families)+64*total;work=(mul+4)*e+64*len(families)+32*total
 assert arrays<=limits['MaximumArrayBytes'] and work<=limits['MaximumWork']
 return dict(Version='0006',EncodedBytes=e,SourceSha256=sha(b),TotalEntries=total,TotalStringBytes=strings,LogicalCopyBytes=mul*e,ArrayBytes=arrays,Work=work,ExportedGroupBytes=e if bound else 0,HasShaderSemantics=False,GeneratorOffset=24 if bound else None,GeneratorSha256=sha(generator) if bound else None,ActualReferences=True,Families=families)
def expected(c):
 limits={**DEFAULT,**{k:c[k] for k in DEFAULT if k in c}}
 if any(not 0<=v<=HARD[k] for k,v in limits.items()):return dict(Name=c['Name'],Reject='Options')
 if c.get('Cancel'):return dict(Name=c['Name'],Reject='Cancel')
 try:
  b=bytes.fromhex(c['Hex']);g=bytes.fromhex(c['GeneratorHex']) if c.get('Bind') else None
  return dict(Name=c['Name'],Reject='None',Snapshot=inspect(b,limits,c.get('Bind',False),g))
 except (AssertionError,ValueError,struct.error,IndexError):return dict(Name=c['Name'],Reject='Input')
ANCHORS={0xb69ca3:('push','0x53475250'),0xb69cb1:('je','0xb69d72'),0xb69cc7:('cmp','eax, 6'),0xb69cd0:('jmp','dword ptr [eax*4 + 0xb69d78]'),0xb69d64:('call','0xb6aaf0'),0xb6ab19:('push','0x30303036'),0xb6ab2a:('call','0xa94b00'),0xb6ab31:('jle','0xb6ad50'),0xb6ab45:('push','0x5346414d'),0xb6ab51:('push','4'),0xb6ab59:('call','0xa94f00'),0xb6ab78:('call','0xb69180'),0xb6ab87:('push','0x3e8'),0xb6ab9c:('call','0xa951b0'),0xb6abaf:('push','0x3e8'),0xb6abbd:('call','0xa951b0'),0xb6abd0:('push','1'),0xb6abd8:('call','0xa94f00'),0xb6abe0:('push','1'),0xb6abeb:('call','0xa94f00'),0xb6abf3:('push','1'),0xb6abfe:('call','0xa94f00'),0xb6ac14:('push','4'),0xb6ac1c:('call','0xa94f00'),0xb6ac2c:('push','4'),0xb6ac34:('call','0xa94f00'),0xb6ac44:('push','4'),0xb6ac4c:('call','0xa94f00'),0xb6ac56:('jle','0xb6acc3'),0xb6ac60:('push','0x3e8'),0xb6ac6e:('call','0xa951b0'),0xb6ac73:('push','4'),0xb6ac7b:('call','0xa94f00'),0xb6acaa:('call','0xb694c0'),0xb6acbe:('dec','dword ptr [ebp - 0x14]'),0xb6acc1:('jne','0xb6ac60'),0xb6ad33:('mov','dword ptr [edx + ecx*4], esi'),0xb6ad41:('call','0xa94da0'),0xb6ad46:('dec','dword ptr [ebp - 0x24]'),0xb6ad49:('jne','0xb6ab40'),0xb69188:('mov','dword ptr [eax], ecx'),0xb69514:('call','0xac15e0'),0xb695fe:('mov','dword ptr [edx], eax'),0xb69603:('mov','dword ptr [edx + 4], ecx'),0xb69606:('mov','dword ptr [edx + 8], eax'),0xb69610:('mov','dword ptr [ebx + 0x24], eax'),0xa951d3:('mov','byte ptr [esi], cl'),0xa951e2:('jne','0xa951d3'),0xa951e4:('inc','dword ptr [edx + 8]'),0xa94dbc:('mov','dword ptr [eax - 4], esi')}
def static(exe,packages):
 data=exe.read_bytes();assert sha(data)==t.EXE;mut=bytearray(data);mut[0]^=1
 negatives=[b'',data[:64],data[:-1],bytes(mut)];assert all(sha(x)!=t.EXE for x in negatives)
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);b=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;assert base==0x400000;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,want in ANCHORS.items():
  i=next(md.disasm(b[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==want,(hex(va),want,(i.mnemonic,i.op_str))
 assert struct.unpack_from('<7I',b,0xb69d78-base)==(0xb69cd7,0xb69cee,0xb69d05,0xb69d1c,0xb69d33,0xb69d4a,0xb69d61)
 body=list(md.disasm(b[0xb6aaf0-base:0xb6ad69-base],0xb6aaf0));assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in body)==8;assert sum(i.mnemonic=='call' and i.op_str=='0xa951b0' for i in body)==3
 text=list(md.disasm(b[0xa951b0-base:0xa951fa-base],0xa951b0));assert all('ebp + 0xc' not in i.op_str for i in text)
 return dict(ExecutableSha256=t.EXE,InstructionAnchors=len(ANCHORS),WrongIdentityNegatives=4,FreshExpandedLeads=6,LeadCap=12,VersionBody='00B6AAF0',GroupDispatcher='00B69C90',ReadWidthSites=[4,1,1,1,4,4,4,4],StringCallSites=3,OriginalExecution=False,ShaderSemantics=False)
def generate(exe,packages,root,cfg,out):
 evidence=static(exe,packages);cases=[]
 def add(name,b,**kw):
  bind=kw.get('Bind',False);cases.append(dict(Name=name,Hex=b.hex(),**({'GeneratorHex':wrap(b).hex()} if bind else {}),**kw))
 base=group(sfam()+sfam(first=b'',second=b'\xff\x80',triple=b'\x00\x01\xff',entries=()))
 add('owned-duplicates',base);add('bind-owned',base,Bind=True);add('empty',group());add('bind-empty',group(),Bind=True)
 for n in range(len(base)):add('truncate-'+str(n),base[:n])
 for version in (b'0000',b'0001',b'0002',b'0003',b'0004',b'0005',b'0007',b'0099'):add('version-'+version.decode(),group(sfam(),version));add('bind-version-'+version.decode(),group(sfam(),version),Bind=True)
 add('root-extra',base+b'\0');add('wrong-root',t.form(b'NOPE',t.form(b'0006',sfam())));add('duplicate-version',t.form(b'SGRP',t.form(b'0006',sfam())+t.form(b'0006')));add('form-family',group(t.form(b'SFAM',sfam()[8:])))
 for key in (0,1,0x80000000,0xffffffff):add('raw-key-'+str(key),group(sfam(key=key,first=b'\xff',second=b'',w0=key,w1=key,entries=((b'',key),(b'\xfe\x80',key)))))
 for count in (0xffffffff,0x80000000,0x7fffffff,65537):add('count-'+str(count),group(sfam(count=count)))
 add('trailing-family',group(t.chunk(b'SFAM',sfam()[8:]+b'x')));add('missing-first-nul',group(t.chunk(b'SFAM',t.words([1])+b'x'*10)));add('missing-second-nul',group(t.chunk(b'SFAM',t.words([1])+b'x\0'+b'y'*10)))
 prefix=t.words([1])+b'a\0b\0'
 for n in range(15):add('short-fixed-'+str(n),group(t.chunk(b'SFAM',prefix+bytes(n))))
 for n in range(4):add('short-key-'+str(n),group(t.chunk(b'SFAM',bytes(n))))
 for n in range(4):add('short-entry-word-'+str(n),group(t.chunk(b'SFAM',prefix+bytes(3)+t.words([1,2,1])+b'entry\0'+bytes(n))))
 add('minimum-entry-count',group(sfam(entries=(),count=1)));add('one-entry-missing',group(sfam(entries=((b'x',1),),count=2)));add('entry-no-nul',group(t.chunk(b'SFAM',prefix+bytes(3)+t.words([1,2,1])+b'x'*10)))
 add('max-string',group(sfam(first=b'a'*999,second=b'',entries=())));add('too-long-string',group(sfam(first=b'a'*1000,entries=())));add('late-malformed-family',group(sfam()+t.chunk(b'SFAM',b'broken')))
 bad=bytearray(base);struct.pack_into('>I',bad,4,0xffffffff);add('huge-extent',bad)
 for bound in (False,True):
  x=inspect(base,bound=bound,generator=wrap(base) if bound else None);exact=dict(MaximumEncodedBytes=len(base),MaximumFamilies=2,MaximumEntriesPerFamily=2,MaximumTotalEntries=2,MaximumStringBytes=9,MaximumTotalStringBytes=x['TotalStringBytes'],MaximumCopyBytes=x['LogicalCopyBytes'],MaximumArrayBytes=x['ArrayBytes'],MaximumWork=x['Work'])
  add('exact-'+str(bound),base,Bind=bound,**exact)
  for k,v in exact.items():add('one-less-'+str(bound)+'-'+k,base,Bind=bound,**{**exact,k:v-1})
 for k,maxval in HARD.items():add('negative-'+k,base,**{k:-1});add('above-hard-'+k,base,**{k:maxval+1})
 add('cancel',base,Cancel=True);add('bind-cancel',base,Bind=True,Cancel=True);add('precopy-zero-cap',base,MaximumCopyBytes=0)
 assert sha(cfg.read_bytes())==t.CONFIG
 with contextlib.ExitStack() as stack:
  for priority,path in t.archives(root,cfg):
   original=t.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if original is not None:break
 assert priority==42 and len(original)==1188446 and sha(original)==t.SOURCE;g=original[210:210+139608];assert sha(g)==t.GENERATOR
 v,=t.records(g,12,len(g));sgrp=next(t.records(g,v[2]+12,v[3]));blob=g[sgrp[2]:sgrp[3]];assert sgrp[2]==24 and len(blob)==1977 and sha(blob)=='C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628'
 add('original-shader-group',blob);cases.append(dict(Name='original-bound-group',Hex=blob.hex(),GeneratorHex=g.hex(),Bind=True))
 want=[expected(c) for c in cases];assert all(w['Reject']=='None' for w in want[:4]);orig=want[-1]['Snapshot'];assert len(orig['Families'])==15
 out.joinpath('fixtures.json').write_text(json.dumps(cases));out.joinpath('expected.json').write_text(json.dumps(want));receipt=dict(Cases=len(cases),Accepted=sum(x['Reject']=='None' for x in want),InputRejections=sum(x['Reject']=='Input' for x in want),OptionRejections=sum(x['Reject']=='Options' for x in want),Cancellations=sum(x['Reject']=='Cancel' for x in want),OriginalOccurrences=1,OriginalFamilies=15,OriginalEntries=orig['TotalEntries'],OriginalGroupSha256=sha(blob),Static=evidence)
 out.joinpath('producer-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
def verify(out):
 got=json.loads(out.joinpath('native.json').read_text());want=json.loads(out.joinpath('expected.json').read_text());assert len(got['Cases'])==len(want)
 for a,e in zip(got['Cases'],want):
  assert a['Name']==e['Name'] and a['Reject']==e['Reject'],(e['Name'],a['Reject'],e['Reject'])
  if e['Reject']=='None':assert a['Snapshot']==e['Snapshot'],(e['Name'],'Independent bytes/offsets/raw words/strings/counts/accounting')
 assert got['OwnershipChecks']==21
 receipt=json.loads(out.joinpath('producer-receipt.json').read_text());receipt.update(Passed=True,OwnershipChecks=21,ShaderSemantics=False)
 out.joinpath('comparison-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*(Path(x) for x in sys.argv[2:]))
 elif sys.argv[1]=='verify':verify(Path(sys.argv[2]))
 else:raise ValueError('Operation')
