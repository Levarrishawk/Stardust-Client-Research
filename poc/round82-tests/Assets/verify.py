"""One SHA-gated static key initializer; never executes original code."""
from pathlib import Path
import argparse,csv,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x00AA2D70;SIZE=18;BODY_SHA='f66d61b0c8353ade3bf643cc9b2fee7dee93c67c801cf2407e4c5c554c5267d7'
ANCHORS={0x00AA2D70:('8bc1','mov','eax, ecx'),0x00AA2D72:('8b0db8469401','mov','ecx, dword ptr [0x19446b8]'),0x00AA2D78:('894804','mov','dword ptr [eax + 4], ecx'),0x00AA2D7B:('c70070a55e01','mov','dword ptr [eax], 0x15ea570'),0x00AA2D81:('c3','ret','')}
TYPES=[(0x015EA588,0x0161EB24,0x018ADC64,0x0161EB14,b'.?AVCrcLowerString@@\0'),(0x015EA5B8,0x0161EB9C,0x018ADC40,0x0161EB8C,b'.?AVPersistentCrcString@@\0')]
def sha(b):return hashlib.sha256(b).hexdigest()
def gate(b):
 if sha(b)!=EXE_SHA:raise ValueError('Executable identity mismatch before VA use')
def body_gate(b):
 if len(b)!=SIZE or sha(b)!=BODY_SHA:raise ValueError('Getter body mismatch')
def typed_gate(col,words,name,expected):
 _,ec,et,eh,en=expected
 if col!=ec or words!=(0,0,0,et,eh) or name!=en:raise ValueError('Conditional typed slot mismatch')
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
  vt,ec,et,eh,en=expected;col,=struct.unpack('<I',image.read(vt-4,4));words=struct.unpack('<5I',image.read(ec,20));name=image.read(et+8,len(en));typed_gate(col,words,name,expected)
  ranges.extend([(vt-4,4),(ec,20),(et+8,len(en))]);metadata.append(dict(Vtable=f'{vt:08X}',Col=f'{col:08X}',ColWords=words,TypeDescriptor=f'{et:08X}',TypeName=name[:-1].decode()))
 for va,n in ranges:
  if image.read(va,n)!=pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n):raise ValueError('Independent mapping mismatch')
 ins=list(Cs(CS_ARCH_X86,CS_MODE_32).disasm(body,BODY));anchor_gate(ins)
 if len(ins)!=5 or sum(i.size for i in ins)!=SIZE:raise ValueError('Setter coverage mismatch')
 if any(i.mnemonic=='call' for i in ins) or any(i.mnemonic.startswith('j') for i in ins):raise ValueError('Setter control flow mismatch')
 if [i.op_str for i in ins if i.mnemonic=='mov' and i.op_str.startswith('dword ptr [eax')]!=['dword ptr [eax + 4], ecx','dword ptr [eax], 0x15ea570']:raise ValueError('Direct destination store mismatch')
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));selected=[]
 for va,n in [('00aa2d70',18),('00aa4250',193)]:
  rows=[r for r in inventory if r['entry']==va]
  if len(rows)!=1 or rows[0]['size']!=str(n):raise ValueError('Exact candidate inventory mismatch')
  selected.extend(rows)
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 guards['Body']=sum(reject(lambda:body_gate(b)) for b in [b'',body[:-1],body+b'\0',bytes([body[0]^1])+body[1:]])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 e=TYPES[0];vt,col,td,chd,name=e;words=(0,0,0,td,chd)
 guards['TypedMetadata']=sum(reject(lambda:typed_gate(*v,e)) for v in [(col+1,words,name),(col,(1,*words[1:]),name),(col,words,b'other'),(col,words[:-1]+(words[-1]+1,),name)])
 guards['SecondKeyType']=reject(lambda:typed_gate(TYPES[1][1]+1,(0,0,0,TYPES[1][2],TYPES[1][3]),TYPES[1][4],TYPES[1]))
 guards['Anchor']=reject(lambda:anchor_gate(ins[:-1]))
 seed_location=image.location(0x019446B8,4)
 if seed_location['FileBacked']:raise ValueError('Runtime seed unexpectedly file backed')
 guards['SeedVirtualTail']=reject(lambda:image.read(0x019446B8,4))
 fixtures=[]
 # Authored isolated field+4 MOV expectations, not original initializer execution/runtime global contents.
 for word,expected in [(0,'00000000'),(1,'01000000'),(0xffffffff,'ffffffff'),(0x12345678,'78563412'),(0x80000000,'00000080'),(0x7fffffff,'ffffff7f'),(0x00aa4350,'5043aa00'),(0x01020304,'04030201')]:
  destination=bytearray(b'A'*20);destination[4:8]=word.to_bytes(4,'little');destination[0:4]=bytes.fromhex('70a55e01')
  if destination[4:8].hex()!=expected or destination[:4]!=bytes.fromhex('70a55e01') or destination[8:]!=b'A'*12:raise ValueError('Synthetic field-word store mismatch')
  fixtures.append(dict(InputWordHex=f'{word:08X}',ExpectedLittleEndianHex=expected,ResultHex=destination[4:8].hex(),DestinationOffset='04'))
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=5,ExactAnchors=len(ANCHORS),Calls=0,Branches=0,ExpandedBodies=1,ExpandedChildren=0,Receiver='ECX',ReturnReceiverRegister='EAX',ExplicitStackArgumentsRead=0,GlobalSeedVa='019446B8',GlobalReadWidthBytes=4,GlobalRuntimeValueKnown=False,DestinationSeedOffset='04',DestinationSeedWidthBytes=4,InstalledVtableVa='015EA570',InstalledVtableTypeNamed=False,LocalSourceScalar10Accesses=0,InheritedKeyTypeContextOnly=True,FullSourcePreservationProved=False,AliasOrLifetimeProved=False,CompleteZeroPreservationProved=False,SyntheticLocalEffectFixtures=8,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,ProductionChanged=False)
 witness=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ins],InheritedKeyTypeContextMetadata=metadata,CandidateInventory=selected,GlobalSeedLocation=seed_location,BodyLocation=image.location(BODY,SIZE),SyntheticLocalEffectFixtures=fixtures)
 dest.mkdir(parents=True);(dest/'static-witnesses.json').write_text(json.dumps(witness,indent=2)+'\n',encoding='utf-8');(dest/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
