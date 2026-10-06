"""One SHA-gated static key getter; never executes original code."""
from pathlib import Path
import argparse,csv,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x00AA41C0;SIZE=13;BODY_SHA='d16c026950a9c3e8eded66bc5f342573d7e6c3311ca277299abc93678b15a04a'
ANCHORS={BODY:('8b410c','mov','eax, dword ptr [ecx + 0xc]'),0x00AA41C3:('85c0','test','eax, eax'),0x00AA41C5:('7505','jne','0xaa41cc'),0x00AA41C7:('b890809001','mov','eax, 0x1908090'),0x00AA41CC:('c3','ret','')}
TYPES=[(0x015EA588,0x0161EB24,0x018ADC64,0x0161EB14,b'.?AVCrcLowerString@@\0'),(0x015EA5B8,0x0161EB9C,0x018ADC40,0x0161EB8C,b'.?AVPersistentCrcString@@\0')]
def sha(b):return hashlib.sha256(b).hexdigest()
def gate(b):
 if sha(b)!=EXE_SHA:raise ValueError('Executable identity mismatch before VA use')
def body_gate(b):
 if len(b)!=SIZE or sha(b)!=BODY_SHA:raise ValueError('Getter body mismatch')
def typed_gate(target,col,words,name,expected):
 _,ec,et,eh,en=expected
 if target!=BODY or col!=ec or words!=(0,0,0,et,eh) or name!=en:raise ValueError('Conditional typed slot mismatch')
def anchor_gate(rows):
 byva={i.address:i for i in rows}
 for va,want in ANCHORS.items():
  i=byva.get(va)
  if i is None or (i.bytes.hex(),i.mnemonic,i.op_str)!=want:raise ValueError('Instruction anchor mismatch')
def reject(action):
 try:action()
 except ValueError:return 1
 raise AssertionError('Expected admission rejection')
def run(exe,packages,evidence_root,out):
 dest=output_path(out);data=exe.read_bytes();gate(data);image=RawPe(data);body=image.read(BODY,SIZE);body_gate(body)
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True)
 ranges=[(BODY,SIZE)];metadata=[]
 for expected in TYPES:
  vt,ec,et,eh,en=expected;target,=struct.unpack('<I',image.read(vt+4,4));col,=struct.unpack('<I',image.read(vt-4,4));words=struct.unpack('<5I',image.read(ec,20));name=image.read(et+8,len(en));typed_gate(target,col,words,name,expected)
  ranges.extend([(vt+4,4),(vt-4,4),(ec,20),(et+8,len(en))]);metadata.append(dict(Vtable=f'{vt:08X}',SlotOffset='04',Target=f'{target:08X}',Col=f'{col:08X}',ColWords=words,TypeDescriptor=f'{et:08X}',TypeName=name[:-1].decode()))
 for va,n in ranges:
  if image.read(va,n)!=pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n):raise ValueError('Independent mapping mismatch')
 ins=list(Cs(CS_ARCH_X86,CS_MODE_32).disasm(body,BODY));anchor_gate(ins)
 if len(ins)!=5 or sum(i.size for i in ins)!=SIZE or any(i.mnemonic=='call' for i in ins):raise ValueError('Getter body structure mismatch')
 if [(i.address,i.op_str) for i in ins if i.mnemonic.startswith('j')]!=[(0x00AA41C5,'0xaa41cc')]:raise ValueError('Getter branch mismatch')
 fallback=image.location(0x01908090,1)
 if fallback['FileBacked']:raise ValueError('Runtime fallback unexpectedly file backed')
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));rows=[r for r in inventory if r['entry']=='00aa41c0']
 if len(rows)!=1 or rows[0]['size']!='13':raise ValueError('Getter inventory mismatch')
 cached=evidence_root/'generated-evidence/human-appearance-2026-10-01/animation-producer-decomp/00aa41c0_FUN_00aa41c0.c'
 cached_hash=sha(cached.read_bytes())
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 guards['Body']=sum(reject(lambda:body_gate(b)) for b in [b'',body[:-1],body+b'\0',bytes([body[0]^1])+body[1:]])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 guards['FallbackVirtualTail']=reject(lambda:image.read(0x01908090,1))
 e=TYPES[0];vt,col,td,chd,name=e;words=(0,0,0,td,chd)
 guards['TypedMetadata']=sum(reject(lambda:typed_gate(*v,e)) for v in [(BODY+1,col,words,name),(BODY,col+1,words,name),(BODY,col,(1,*words[1:]),name),(BODY,col,words,b'other')])
 guards['SecondTypedSlot']=reject(lambda:typed_gate(BODY+1,TYPES[1][1],(0,0,0,TYPES[1][2],TYPES[1][3]),TYPES[1][4],TYPES[1]))
 # Every instruction must remain present and retain its exact role.
 guards['Anchor']=reject(lambda:anchor_gate(ins[:-1]))
 fixtures=[]
 for word,expected in [(0,0x01908090),(1,1),(0x1000,0x1000),(0x01908090,0x01908090),(0x80000000,0x80000000),(0xffffffff,0xffffffff),(0x00aa41c0,0x00aa41c0),(0x12345678,0x12345678)]:
  encoded=word.to_bytes(4,'little');read_word=int.from_bytes(encoded,'little');result=read_word if read_word else 0x01908090
  if result!=expected:raise ValueError('Synthetic getter word-choice mismatch')
  fixtures.append(dict(FieldWordHex=f'{word:08X}',ExpectedHex=f'{expected:08X}',ResultHex=f'{result:08X}'))
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=5,ExactAnchors=5,Calls=0,Branches=1,ExpandedBodies=1,OptionalChildren=0,SourceFieldOffset='0C',FieldReadWidthBytes=4,NullFieldFallbackAddress='01908090',FallbackRuntimeByteKnown=False,LocalMemoryWrites=0,LocalScalar10Accesses=0,TypedVtableAssociations=2,ActualSourceDynamicTypeProved=False,AliasOrLifetimeProved=False,OtherDelegatedSideEffectsProved=False,CompleteZeroPreservationProved=False,SyntheticWordChoiceFixtures=8,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,ProductionChanged=False)
 witness=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ins],TypedMetadata=metadata,FallbackLocation=fallback,GetterInventory=rows[0],CachedGetterName=cached.name,CachedGetterSha256=cached_hash,BodyLocation=image.location(BODY,SIZE),SyntheticWordChoiceFixtures=fixtures)
 dest.mkdir(parents=True);(dest/'static-witnesses.json').write_text(json.dumps(witness,indent=2)+'\n',encoding='utf-8');(dest/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
