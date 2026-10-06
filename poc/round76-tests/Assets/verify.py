"""Bounded static scalar-storage lookup witness; no original code execution."""
from pathlib import Path
import argparse,csv,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x007D3790;SIZE=262;BODY_SHA='38fbd29cbcccf7cd4d55d2db94a80de7b7ff4bac923a185f601d172d1bdbdbc4'
TINY=0x007D3C40;TINY_SIZE=16;TINY_SHA='ab3ff97c89ef583e501b6ef4b4a9ef626b38883a4dc75de8e0eb1b475b770146'
ANCHORS={
0x007D37AC:('8bf1','mov','esi, ecx'),0x007D37AE:('8b4610','mov','eax, dword ptr [esi + 0x10]'),
0x007D37B4:('753b','jne','0x7d37f1'),0x007D37B6:('6a0c','push','0xc'),
0x007D37EE:('897e10','mov','dword ptr [esi + 0x10], edi'),0x007D37F1:('8b7d08','mov','edi, dword ptr [ebp + 8]'),
0x007D37F8:('e883070000','call','0x7d3f80'),0x007D3803:('3b01','cmp','eax, dword ptr [ecx]'),
0x007D3807:('83c010','add','eax, 0x10'),0x007D380D:('e8fef62c00','call','0xaa2f10'),
0x007D3814:('7518','jne','0x7d382e'),0x007D3819:('8d4220','lea','eax, [edx + 0x20]'),
0x007D382B:('c20400','ret','4'),0x007D3837:('c745e400000000','mov','dword ptr [ebp - 0x1c], 0'),
0x007D384C:('e8ef030000','call','0x7d3c40'),0x007D3857:('51','push','ecx'),0x007D3858:('51','push','ecx'),
0x007D386A:('e851080000','call','0x7d40c0'),0x007D387E:('8b4de8','mov','ecx, dword ptr [ebp - 0x18]'),
0x007D3881:('8d4120','lea','eax, [ecx + 0x20]'),0x007D3893:('c20400','ret','4')}
TINY_ANCHORS={0x007D3C43:('8bc1','mov','eax, ecx'),0x007D3C48:('8b11','mov','edx, dword ptr [ecx]'),0x007D3C4A:('8910','mov','dword ptr [eax], edx'),0x007D3C4D:('c20400','ret','4')}
EXPECTED_CALLS={0x007D37B8:'0xac15c0',0x007D37DE:'0x7d3c70',0x007D37F8:'0x7d3f80',0x007D380D:'0xaa2f10',0x007D3832:'0xaa3270',0x007D384C:'0x7d3c40',0x007D385F:'0x7d3c40',0x007D386A:'0x7d40c0',0x007D3879:'0xaa32b0'}
CANDIDATES={'007d3790':262,'007d3c40':16,'007d3c70':112,'007d3f80':54,'007d40c0':1484,'00aa2f10':126,'00aa3270':28,'00aa32b0':11,'00ac15c0':None}
def sha(b):return hashlib.sha256(b).hexdigest()
def gate(b):
 if sha(b)!=EXE_SHA:raise ValueError('Executable identity mismatch before VA use')
def body_gate(b,size,digest):
 if len(b)!=size or sha(b)!=digest:raise ValueError('Bounded body mismatch')
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
 body=image.read(BODY,SIZE);tiny=image.read(TINY,TINY_SIZE);body_gate(body,SIZE,BODY_SHA);body_gate(tiny,TINY_SIZE,TINY_SHA)
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True)
 for va,n in [(BODY,SIZE),(TINY,TINY_SIZE)]:
  if image.read(va,n)!=pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n):raise ValueError('Independent raw mapping mismatch')
 decoder=Cs(CS_ARCH_X86,CS_MODE_32);ins=list(decoder.disasm(body,BODY));ti=list(decoder.disasm(tiny,TINY));check_anchors(ins,ANCHORS);check_anchors(ti,TINY_ANCHORS)
 calls={i.address:i.op_str for i in ins if i.mnemonic=='call'};branches=[i for i in ins if i.mnemonic.startswith('j')];boundaries={i.address for i in ins}
 if len(ins)!=89 or sum(i.size for i in ins)!=SIZE or len(ti)!=8 or sum(i.size for i in ti)!=TINY_SIZE or calls!=EXPECTED_CALLS or len(branches)!=5:raise ValueError('Body/control-flow mismatch')
 if any(int(i.op_str,16) not in boundaries for i in branches):raise ValueError('Unaligned/outside branch')
 key=image.location(0x01923F7C,4)
 if key['FileBacked']:raise ValueError('Runtime-only key unexpectedly file backed')
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));metadata=[]
 for va,size in CANDIDATES.items():
  rows=[r for r in inventory if r['entry']==va]
  if len(rows)!=1 or (size is not None and int(rows[0]['size'])!=size):raise ValueError('Candidate inventory mismatch')
  metadata.extend(rows)
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 for label,source,n,digest in [('LookupBody',body,SIZE,BODY_SHA),('TinyCopyBody',tiny,TINY_SIZE,TINY_SHA)]:
  guards[label]=sum(reject(lambda:body_gate(b,n,digest)) for b in [b'',source[:-1],source+b'\0',bytes([source[0]^1])+source[1:]])
 guards['KeyVirtualTail']=reject(lambda:image.read(0x01923F7C,4))
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 guards['Anchor']=reject(lambda:check_anchors(ins,{BODY:('00','push','ebp')}))+reject(lambda:check_anchors(ti,{TINY:('00','push','ebp')}))
 witness=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ins],TinyInstructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ti],KeyLocation=key,CandidateMetadata=metadata,Locations=[image.location(BODY,SIZE),image.location(TINY,TINY_SIZE)])
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=89,ExactAnchors=len(ANCHORS),TinyAccessorVa=f'{TINY:08X}',TinyAccessorBytes=TINY_SIZE,TinyAccessorSha256=TINY_SHA,TinyInstructions=8,TinyAnchors=len(TINY_ANCHORS),Calls=9,Branches=5,ExplicitCandidates=10,CandidateBudget=12,ConcreteLookupBodies=1,TinyAccessorsExpanded=1,OtherBodiesExpanded=0,LookupObjectTypeKnown=False,KeyRuntimeMeaningKnown=False,NodePayloadOffset='20',ContainerFieldOffset='10',MissTemporaryPayloadBits='00000000',EffectiveMissFloatDefaultConfirmed=False,PointerStabilityLifetimeProved=False,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,ProductionChanged=False,PriorActorClosureRepeated=False)
 dest.mkdir(parents=True);(dest/'static-witnesses.json').write_text(json.dumps(witness,indent=2)+'\n',encoding='utf-8');(dest/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
