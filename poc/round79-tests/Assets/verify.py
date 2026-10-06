"""Bounded static key base copy witness; no original code execution."""
from pathlib import Path
import argparse,csv,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x00AA3FE0;SIZE=107;BODY_SHA='a8e6b47957aa1251797dac093feb9dd5b275241e9e245cfbb44f4639060e3d90'
TINY=0x00AA2E10;TINY_SIZE=23;TINY_SHA='c3f73467a10d2bf2c40c647fc872b97e7054645e45d81be6694e1cec4be381fa'
TYPE_NAME=b'.?AVPersistentCrcString@@\0'
ANCHORS={0x00AA3FFA:('8bf1','mov','esi, ecx'),0x00AA4000:('e86bedffff','call','0xaa2d70'),0x00AA4005:('8b7d08','mov','edi, dword ptr [ebp + 8]'),0x00AA400F:('894608','mov','dword ptr [esi + 8], eax'),0x00AA4012:('89460c','mov','dword ptr [esi + 0xc], eax'),0x00AA4015:('c706b8a55e01','mov','dword ptr [esi], 0x15ea5b8'),0x00AA401B:('e8f0edffff','call','0xaa2e10'),0x00AA4022:('7513','jne','0xaa4037'),0x00AA4024:('8b4704','mov','eax, dword ptr [edi + 4]'),0x00AA4027:('50','push','eax'),0x00AA402A:('8bcf','mov','ecx, edi'),0x00AA402C:('ff5004','call','dword ptr [eax + 4]'),0x00AA402F:('50','push','eax'),0x00AA4030:('8bce','mov','ecx, esi'),0x00AA4032:('e819030000','call','0xaa4350'),0x00AA403B:('8bc6','mov','eax, esi'),0x00AA4048:('c20400','ret','4')}
TINY_ANCHORS={0x00AA2E10:('8b01','mov','eax, dword ptr [ecx]'),0x00AA2E12:('ff5004','call','dword ptr [eax + 4]'),0x00AA2E15:('85c0','test','eax, eax'),0x00AA2E17:('7408','je','0xaa2e21'),0x00AA2E19:('803800','cmp','byte ptr [eax], 0'),0x00AA2E1C:('7403','je','0xaa2e21'),0x00AA2E1E:('33c0','xor','eax, eax'),0x00AA2E21:('b801000000','mov','eax, 1')}
CANDIDATES={'00aa3fe0':107,'00aa2d70':18,'00aa2e10':23,'00aa4350':28,'00aa41c0':13}
def sha(b):return hashlib.sha256(b).hexdigest()
def gate(b):
 if sha(b)!=EXE_SHA:raise ValueError('Executable identity mismatch before VA use')
def body_gate(b,size,digest):
 if len(b)!=size or sha(b)!=digest:raise ValueError('Bounded body mismatch')
def typed_gate(col,words,name):
 if col!=0x0161EB9C or words!=(0,0,0,0x018ADC40,0x0161EB8C) or name!=TYPE_NAME:raise ValueError('Key construction RTTI mismatch')
def slot_gate(slots):
 if slots!=[0x00AA41C0,0x00AA41C0]:raise ValueError('Conditional getter slots mismatch')
def reject(action):
 try:action()
 except ValueError:return 1
 raise AssertionError('Expected guard rejection')
def check_anchors(rows,anchors):
 byva={i.address:i for i in rows}
 for va,want in anchors.items():
  i=byva.get(va)
  if i is None or (i.bytes.hex(),i.mnemonic,i.op_str)!=want:raise ValueError('Role anchor mismatch')
def run(exe,packages,evidence_root,out):
 dest=output_path(out);data=exe.read_bytes();gate(data);image=RawPe(data)
 body=image.read(BODY,SIZE);body_gate(body,SIZE,BODY_SHA);tiny=image.read(TINY,TINY_SIZE);body_gate(tiny,TINY_SIZE,TINY_SHA)
 col,=struct.unpack('<I',image.read(0x015EA5B4,4));words=struct.unpack('<5I',image.read(col,20));name=image.read(0x018ADC40+8,len(TYPE_NAME));typed_gate(col,words,name)
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True)
 for va,n in [(BODY,SIZE),(TINY,TINY_SIZE),(0x015EA5B4,4),(col,20),(0x018ADC40+8,len(TYPE_NAME))]:
  if image.read(va,n)!=pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n):raise ValueError('Independent raw mapping mismatch')
 decoder=Cs(CS_ARCH_X86,CS_MODE_32);ins=list(decoder.disasm(body,BODY));check_anchors(ins,ANCHORS);ti=list(decoder.disasm(tiny,TINY));check_anchors(ti,TINY_ANCHORS)
 if len(ti)!=10 or sum(i.size for i in ti)!=TINY_SIZE:raise ValueError('Tiny predicate coverage mismatch')
 tiny_branches=[i for i in ti if i.mnemonic.startswith('j')];tiny_boundaries={i.address for i in ti}
 if len([i for i in ti if i.mnemonic=='ret' and not i.op_str])!=2:raise ValueError('Tiny predicate return boundary mismatch')
 if len(tiny_branches)!=2 or any(int(i.op_str,16) not in tiny_boundaries for i in tiny_branches) or [(i.address,i.op_str) for i in ti if i.mnemonic=='call']!=[(0x00AA2E12,'dword ptr [eax + 4]')]:raise ValueError('Tiny predicate control flow mismatch')
 destination_stores=[i.op_str for i in ins if i.mnemonic=='mov' and i.op_str.startswith('dword ptr [esi')]
 if destination_stores!=['dword ptr [esi + 8], eax','dword ptr [esi + 0xc], eax','dword ptr [esi], 0x15ea5b8'] or any(i.op_str.startswith('dword ptr [edi') for i in ins if i.mnemonic=='mov'):raise ValueError('Local key store role mismatch')
 calls={i.address:i.op_str for i in ins if i.mnemonic=='call'};branches=[i for i in ins if i.mnemonic.startswith('j')];boundaries={i.address for i in ins}
 if len(ins)!=39 or sum(i.size for i in ins)!=SIZE or calls!={0x00AA4000:'0xaa2d70',0x00AA401B:'0xaa2e10',0x00AA402C:'dword ptr [eax + 4]',0x00AA4032:'0xaa4350'} or len(branches)!=1:raise ValueError('Body/control-flow mismatch')
 if len([i for i in ins if i.mnemonic=='ret' and i.op_str=='4'])!=1:raise ValueError('Return boundary mismatch')
 if any(int(i.op_str,16) not in boundaries for i in branches):raise ValueError('Unaligned/outside branch')
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));metadata=[]
 for va,size in CANDIDATES.items():
  rows=[r for r in inventory if r['entry']==va]
  if len(rows)!=1 or (size is not None and int(rows[0]['size'])!=size):raise ValueError('Candidate inventory mismatch')
  metadata.extend(rows)
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 for label,source,n,digest in [('KeyBaseBody',body,SIZE,BODY_SHA),('TinyPredicateBody',tiny,TINY_SIZE,TINY_SHA)]:
  guards[label]=sum(reject(lambda:body_gate(b,n,digest)) for b in [b'',source[:-1],source+b'\0',bytes([source[0]^1])+source[1:]])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 guards['Anchor']=reject(lambda:check_anchors(ins,{BODY:('00','push','ebp')}))
 guards['TypedMetadata']=sum(reject(lambda:typed_gate(*v)) for v in [(col+1,words,name),(col,(1,*words[1:]),name),(col,words,b'other'),(col,words[:-1]+(words[-1]+1,),name)])
 slots=[struct.unpack('<I',image.read(v,4))[0] for v in [0x015EA58C,0x015EA5BC]]
 slot_gate(slots)
 guards['GetterSlots']=reject(lambda:slot_gate([0,slots[1]]))+reject(lambda:slot_gate([slots[0],0]))
 for va in [0x015EA58C,0x015EA5BC]:
  if image.read(va,4)!=pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,4):raise ValueError('Independent getter-slot mapping mismatch')
 fixtures=[]
 # Isolated returned-buffer predicate, not original getter execution or encoding.
 for returned,expected in [(None,1),(b'\0',1),(b'\0X',1),(b'a',0),(b'\xff',0),(b'\x80',0),(b'\r',0),(b'A\0',0)]:
  result=int(returned is None or returned[0]==0)
  if result!=expected:raise ValueError('Synthetic pointer/first-byte predicate fixture mismatch')
  fixtures.append(dict(ReturnedHex=None if returned is None else returned.hex(),Expected=expected,Result=result))
 witness=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ins],TinyInstructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ti],KeyTypeMetadata=dict(VtableVa='015EA5B8',ColVa=f'{col:08X}',ColWords=words,TypeDescriptorVa='018ADC40',TypeName=TYPE_NAME[:-1].decode()),CandidateMetadata=metadata,GetterSlotAddresses=['015EA58C','015EA5BC'],ConditionalGetterTargets=slots,Locations=[image.location(BODY,SIZE)],SyntheticPredicateFixtures=fixtures)
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=39,ExactAnchors=len(ANCHORS),Calls=4,Branches=1,ExplicitFunctionCandidates=5,ConcreteKeyBaseCopyBodies=1,TinyAccessorsExpanded=1,TinyAccessorVa=f'{TINY:08X}',TinyAccessorBytes=TINY_SIZE,TinyAccessorSha256=TINY_SHA,TinyInstructions=10,TinyAnchors=len(TINY_ANCHORS),TinyCalls=1,TinyBranches=2,OtherBodiesExpanded=0,ConstructedBaseKeyRttiType='PersistentCrcString',DirectDestinationFieldOffsets=['00','08','0C'],DirectFieldWidthBytes=4,DirectSourceWritesObserved=0,LocalSourcePayload10WritesObserved=0,FullSourcePreservationProved=False,CopyRetainOwnershipProved=False,AllInsertionRoutesDefaultConfirmed=False,ConditionalGetterTarget='00AA41C0',GetterBodyBytesUnexpanded=13,DestinationSetterVa='00AA4350',DestinationSetterBytesUnexpanded=28,SyntheticPredicateFixtures=len(fixtures),PointerStabilityLifetimeProved=False,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,ProductionChanged=False,PriorActorClosureRepeated=False)
 dest.mkdir(parents=True);(dest/'static-witnesses.json').write_text(json.dumps(witness,indent=2)+'\n',encoding='utf-8');(dest/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
