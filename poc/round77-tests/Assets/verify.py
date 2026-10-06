"""Bounded static scalar-node insertion witness; no original code execution."""
from pathlib import Path
import argparse,csv,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x007D40C0;SIZE=1484;BODY_SHA='8516eb225ac447fe4979315a41bd2f45918c7fb76eb46ddb8c53b3ba5021b984'
TINY=0x007D6310;TINY_SIZE=36;TINY_SHA='613941006f2a3b7e8046ba42c47aa99155ae6855c660cb414da90f2f433796db'
ANCHORS={
0x007D40C8:('8bf1','mov','esi, ecx'),0x007D40D0:('8b7d0c','mov','edi, dword ptr [ebp + 0xc]'),
0x007D40D5:('0f8522040000','jne','0x7d44fd'),0x007D40DE:('85c9','test','ecx, ecx'),
0x007D40F4:('8b4d10','mov','ecx, dword ptr [ebp + 0x10]'),0x007D40FE:('e80dee2c00','call','0xaa2f10'),
0x007D4147:('e874170000','call','0x7d58c0'),0x007D415D:('8908','mov','dword ptr [eax], ecx'),
0x007D41C7:('c6451300','mov','byte ptr [ebp + 0x13], 0'),0x007D41D8:('8908','mov','dword ptr [eax], ecx'),
0x007D420D:('8b5510','mov','edx, dword ptr [ebp + 0x10]'),0x007D4213:('e8f8200000','call','0x7d6310'),
0x007D421A:('891d0c000000','mov','dword ptr [0xc], ebx'),0x007D4230:('8b4510','mov','eax, dword ptr [ebp + 0x10]'),
0x007D4234:('e8d7200000','call','0x7d6310'),0x007D423B:('895f08','mov','dword ptr [edi + 8], ebx'),
0x007D4258:('897b04','mov','dword ptr [ebx + 4], edi'),0x007D425B:('894308','mov','dword ptr [ebx + 8], eax'),
0x007D425E:('89430c','mov','dword ptr [ebx + 0xc], eax'),0x007D4268:('e8c316b200','call','0x12f5930'),
0x007D4274:('894604','mov','dword ptr [esi + 4], eax'),0x007D427C:('8918','mov','dword ptr [eax], ebx'),
0x007D4293:('750e','jne','0x7d42a3'),0x007D4298:('8938','mov','dword ptr [eax], edi'),
0x007D42D3:('e838200000','call','0x7d6310'),0x007D4352:('e8b91f0000','call','0x7d6310'),
0x007D4390:('e87b1f0000','call','0x7d6310'),0x007D43B1:('e85a1f0000','call','0x7d6310'),
0x007D4540:('e83b140000','call','0x7d5980'),0x007D4662:('8938','mov','dword ptr [eax], edi'),
0x007D4677:('e804130000','call','0x7d5980'),0x007D4689:('c20c00','ret','0xc')}
TINY_ANCHORS={0x007D6314:('6a24','push','0x24'),0x007D6316:('e85544b100','call','0x12ea770'),0x007D631D:('8b4508','mov','eax, dword ptr [ebp + 8]'),0x007D6321:('8d4e10','lea','ecx, [esi + 0x10]'),0x007D6325:('e886010000','call','0x7d64b0'),0x007D632D:('8bc6','mov','eax, esi'),0x007D6331:('c20400','ret','4')}
EXPECTED_TARGETS={'0x12f5930','0x12f6420','0x12f6490','0x7d58c0','0x7d5980','0x7d6310','0x7d6340','0x7d6450','0xaa2f10'}
CANDIDATES={'007d40c0':1484,'007d58c0':184,'007d5980':430,'007d6310':36,'007d6340':17,'007d6450':24,'00aa2f10':126,'012f5930':890,'012f6420':105,'012f6490':138,'007d64b0':77,'012ea770':421}
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
 if len(ins)!=625 or sum(i.size for i in ins)!=SIZE or len(ti)!=16 or sum(i.size for i in ti)!=TINY_SIZE or len(calls)!=48 or set(calls.values())!=EXPECTED_TARGETS or len(branches)!=60:raise ValueError('Body/control-flow mismatch')
 if len([i for i in ins if i.mnemonic=='ret' and i.op_str=='0xc'])!=17:raise ValueError('Return boundary mismatch')
 if any(int(i.op_str,16) not in boundaries for i in branches):raise ValueError('Unaligned/outside branch')
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));metadata=[]
 for va,size in CANDIDATES.items():
  rows=[r for r in inventory if r['entry']==va]
  if len(rows)!=1 or (size is not None and int(rows[0]['size'])!=size):raise ValueError('Candidate inventory mismatch')
  metadata.extend(rows)
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 for label,source,n,digest in [('InsertionBody',body,SIZE,BODY_SHA),('TinyValueForwarder',tiny,TINY_SIZE,TINY_SHA)]:
  guards[label]=sum(reject(lambda:body_gate(b,n,digest)) for b in [b'',source[:-1],source+b'\0',bytes([source[0]^1])+source[1:]])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 guards['Anchor']=reject(lambda:check_anchors(ins,{BODY:('00','push','ebp')}))+reject(lambda:check_anchors(ti,{TINY:('00','push','ebp')}))
 witness=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ins],TinyInstructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in ti],CandidateMetadata=metadata,Locations=[image.location(BODY,SIZE),image.location(TINY,TINY_SIZE)])
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=625,ExactAnchors=len(ANCHORS),TinyAccessorVa=f'{TINY:08X}',TinyAccessorBytes=TINY_SIZE,TinyAccessorSha256=TINY_SHA,TinyInstructions=16,TinyAnchors=len(TINY_ANCHORS),Calls=48,Branches=60,ExplicitCandidates=12,CandidateBudget=12,ConcreteInsertionBodies=1,TinyAccessorsExpanded=1,OtherBodiesExpanded=0,LookupObjectTypeKnown=False,KeyRuntimeMeaningKnown=False,NodeAllocationRequestedBytes=36,ValueDestinationOffset='10',DirectPayloadCopyProved=False,UnexpandedValueConstructionVa='007D64B0',UnexpandedValueConstructionBytes=77,EffectiveMissFloatDefaultConfirmed=False,PointerStabilityLifetimeProved=False,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,ProductionChanged=False,PriorActorClosureRepeated=False)
 dest.mkdir(parents=True);(dest/'static-witnesses.json').write_text(json.dumps(witness,indent=2)+'\n',encoding='utf-8');(dest/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
