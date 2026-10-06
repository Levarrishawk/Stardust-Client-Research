"""Bounded static scalar value construction witness; no original code execution."""
from pathlib import Path
import argparse,csv,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x007D64B0;SIZE=77;BODY_SHA='22fca2f6fbbcc8a85f62bbf64f124faf70447f611da1de73e447e619ccde5c72'
TINY=0x00AA3270;TINY_SIZE=28;TINY_SHA='1a0d6939d5ff39e488d135b0836628da341a7dba238dd7f98d171b767b6d5199'
TYPE_NAME=b'.?AVCrcLowerString@@\0'
ANCHORS={
0x007D64CA:('8b7508','mov','esi, dword ptr [ebp + 8]'),0x007D64CD:('8975f0','mov','dword ptr [ebp - 0x10], esi'),
0x007D64D0:('85f6','test','esi, esi'),0x007D64D9:('7413','je','0x7d64ee'),
0x007D64DC:('8b7d0c','mov','edi, dword ptr [ebp + 0xc]'),0x007D64DF:('57','push','edi'),
0x007D64E0:('8bce','mov','ecx, esi'),0x007D64E2:('e889cd2c00','call','0xaa3270'),
0x007D64E7:('8b4710','mov','eax, dword ptr [edi + 0x10]'),0x007D64EA:('894610','mov','dword ptr [esi + 0x10], eax'),
0x007D64ED:('5f','pop','edi'),0x007D64F2:('64890d00000000','mov','dword ptr fs:[0], ecx'),0x007D64FC:('c3','ret','')}
TINY_ANCHORS={0x00AA3273:('8b4508','mov','eax, dword ptr [ebp + 8]'),0x00AA327A:('e8610d0000','call','0xaa3fe0'),0x00AA327F:('c70688a55e01','mov','dword ptr [esi], 0x15ea588'),0x00AA3285:('8bc6','mov','eax, esi'),0x00AA3289:('c20400','ret','4')}
CANDIDATES={'007d64b0':77,'00aa3270':28,'00aa3fe0':107}
def sha(b):return hashlib.sha256(b).hexdigest()
def gate(b):
 if sha(b)!=EXE_SHA:raise ValueError('Executable identity mismatch before VA use')
def body_gate(b,size,digest):
 if len(b)!=size or sha(b)!=digest:raise ValueError('Bounded body mismatch')
def typed_gate(col,words,name):
 if col!=0x0161EB24 or words!=(0,0,0,0x018ADC64,0x0161EB14) or name!=TYPE_NAME:raise ValueError('Key construction RTTI mismatch')
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
 col,=struct.unpack('<I',image.read(0x015EA584,4));words=struct.unpack('<5I',image.read(col,20));name=image.read(0x018ADC64+8,len(TYPE_NAME));typed_gate(col,words,name)
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True)
 for va,n in [(BODY,SIZE),(TINY,TINY_SIZE),(0x015EA584,4),(col,20),(0x018ADC64+8,len(TYPE_NAME))]:
  if image.read(va,n)!=pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n):raise ValueError('Independent raw mapping mismatch')
 decoder=Cs(CS_ARCH_X86,CS_MODE_32);ins=list(decoder.disasm(body,BODY));check_anchors(ins,ANCHORS);ti=list(decoder.disasm(tiny,TINY));check_anchors(ti,TINY_ANCHORS)
 if len(ti)!=12 or sum(i.size for i in ti)!=TINY_SIZE:raise ValueError('Tiny key wrapper coverage mismatch')
 calls={i.address:i.op_str for i in ins if i.mnemonic=='call'};branches=[i for i in ins if i.mnemonic.startswith('j')];boundaries={i.address for i in ins}
 if len(ins)!=28 or sum(i.size for i in ins)!=SIZE or calls!={0x007D64E2:'0xaa3270'} or len(branches)!=1:raise ValueError('Body/control-flow mismatch')
 if len([i for i in ins if i.mnemonic=='ret' and not i.op_str])!=1:raise ValueError('Return boundary mismatch')
 if any(int(i.op_str,16) not in boundaries for i in branches):raise ValueError('Unaligned/outside branch')
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));metadata=[]
 for va,size in CANDIDATES.items():
  rows=[r for r in inventory if r['entry']==va]
  if len(rows)!=1 or (size is not None and int(rows[0]['size'])!=size):raise ValueError('Candidate inventory mismatch')
  metadata.extend(rows)
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 for label,source,n,digest in [('ValueBody',body,SIZE,BODY_SHA),('TinyKeyBody',tiny,TINY_SIZE,TINY_SHA)]:
  guards[label]=sum(reject(lambda:body_gate(b,n,digest)) for b in [b'',source[:-1],source+b'\0',bytes([source[0]^1])+source[1:]])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 guards['Anchor']=reject(lambda:check_anchors(ins,{BODY:('00','push','ebp')}))
 guards['TypedMetadata']=sum(reject(lambda:typed_gate(*v)) for v in [(col+1,words,name),(col,(1,*words[1:]),name),(col,words,b'other'),(col,words[:-1]+(words[-1]+1,),name)])
 fixtures=[]
 # Synthetic isolated four-byte MOV effect, not the key-constructor or original CPU.
 for raw in ['00000000','00000080','0000803f','000080bf','0000807f','4523c17f','01000000','ffffffff']:
  source=b'K'*16+bytes.fromhex(raw)+b'S'*4;node=bytearray(b'N'*36);node[0x20:0x24]=source[0x10:0x14]
  if node[0x20:0x24]!=bytes.fromhex(raw) or node[:0x20]!=b'N'*32:raise ValueError('Synthetic bit-preserving address fixture mismatch')
  fixtures.append(dict(SourcePayloadHex=raw,DestinationNodeOffset='20',CopiedBytes=4,ResultHex=node[0x20:0x24].hex()))
 witness=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ins],TinyInstructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ti],KeyTypeMetadata=dict(VtableVa='015EA588',ColVa=f'{col:08X}',ColWords=words,TypeDescriptorVa='018ADC64',TypeName=TYPE_NAME[:-1].decode()),CandidateMetadata=metadata,Locations=[image.location(BODY,SIZE)],SyntheticCopyFixtures=fixtures)
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=28,ExactAnchors=len(ANCHORS),Calls=1,Branches=1,ExplicitCandidates=3,ConcreteValueConstructionBodies=1,TinyAccessorsExpanded=1,TinyAccessorVa=f'{TINY:08X}',TinyAccessorBytes=TINY_SIZE,TinyAccessorSha256=TINY_SHA,TinyInstructions=12,TinyAnchors=len(TINY_ANCHORS),OtherBodiesExpanded=0,SourceArgumentOffset='0C',DestinationArgumentOffset='08',SourcePayloadOffset='10',DestinationPayloadOffset='10',CopyWidthBytes=4,ComposedNodePayloadOffset='20',ScalarWordCopyConfirmed=True,ZeroBitsPreservedAtCopy=True,SourceZeroPreservedAcrossKeyDelegateProved=False,AllInsertionRoutesDefaultConfirmed=False,KeyConstructorVa='00AA3270',ConstructedKeyRttiType='CrcLowerString',KeyBaseCopyVa='00AA3FE0',KeyBaseCopyBytesUnexpanded=107,SyntheticCopyFixtures=len(fixtures),PointerStabilityLifetimeProved=False,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,ProductionChanged=False,PriorActorClosureRepeated=False)
 dest.mkdir(parents=True);(dest/'static-witnesses.json').write_text(json.dumps(witness,indent=2)+'\n',encoding='utf-8');(dest/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
