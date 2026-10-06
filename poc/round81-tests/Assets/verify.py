"""One SHA-gated static key setter; never executes original code."""
from pathlib import Path
import argparse,csv,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x00AA4350;SIZE=28;BODY_SHA='bea5da39201c5445ad342e7bdc0dc357adf21f5487d34af8a97f467ecaed3b01'
ANCHORS={0x00AA4353:('8b4508','mov','eax, dword ptr [ebp + 8]'),0x00AA4357:('6a00','push','0'),0x00AA4359:('50','push','eax'),0x00AA435A:('8bf1','mov','esi, ecx'),0x00AA435C:('e8effeffff','call','0xaa4250'),0x00AA4361:('8b4d0c','mov','ecx, dword ptr [ebp + 0xc]'),0x00AA4364:('894e04','mov','dword ptr [esi + 4], ecx'),0x00AA4369:('c20800','ret','8')}
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
 if len(ins)!=13 or sum(i.size for i in ins)!=SIZE:raise ValueError('Setter coverage mismatch')
 if [(i.address,i.op_str) for i in ins if i.mnemonic=='call']!=[(0x00AA435C,'0xaa4250')] or any(i.mnemonic.startswith('j') for i in ins):raise ValueError('Setter control flow mismatch')
 if [i.op_str for i in ins if i.mnemonic=='mov' and i.op_str.startswith('dword ptr [esi')]!=['dword ptr [esi + 4], ecx']:raise ValueError('Direct destination store mismatch')
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));selected=[]
 for va,n in [('00aa4350',28),('00aa4250',193),('00aa2d70',18)]:
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
 fixtures=[]
 # Authored isolated field+4 MOV expectations, not execution of delegate00AA4250.
 for word,expected in [(0,'00000000'),(1,'01000000'),(0xffffffff,'ffffffff'),(0x12345678,'78563412'),(0x80000000,'00000080'),(0x7fffffff,'ffffff7f'),(0x00aa4350,'5043aa00'),(0x01020304,'04030201')]:
  destination=bytearray(b'A'*20);destination[4:8]=word.to_bytes(4,'little')
  if destination[4:8].hex()!=expected or destination[:4]!=b'A'*4 or destination[8:]!=b'A'*12:raise ValueError('Synthetic field-word store mismatch')
  fixtures.append(dict(InputWordHex=f'{word:08X}',ExpectedLittleEndianHex=expected,ResultHex=destination[4:8].hex(),DestinationOffset='04'))
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=13,ExactAnchors=len(ANCHORS),Calls=1,Branches=0,ExpandedBodies=1,ExpandedChildren=0,ForwardedSourceArgumentOffset='08',ForwardedLiteralArgument=0,LocalMetadataArgumentOffset='0C',LocalDestinationOffset='04',LocalStoreWidthBytes=4,StoreAfterDelegateReturn=True,ImmediateDelegateVa='00AA4250',ImmediateDelegateBytesUnexpanded=193,NextInitializerVa='00AA2D70',NextInitializerBytesUnexpanded=18,KeyTypeContextMetadataOnly=True,SetterVirtualSlotProved=False,ReferenceCopyOwnershipProved=False,FullSourcePreservationProved=False,AliasOrLifetimeProved=False,CompleteZeroPreservationProved=False,SyntheticLocalStoreFixtures=8,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,ProductionChanged=False)
 witness=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ins],KeyTypeContextMetadata=metadata,CandidateInventory=selected,BodyLocation=image.location(BODY,SIZE),SyntheticLocalStoreFixtures=fixtures)
 dest.mkdir(parents=True);(dest/'static-witnesses.json').write_text(json.dumps(witness,indent=2)+'\n',encoding='utf-8');(dest/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
