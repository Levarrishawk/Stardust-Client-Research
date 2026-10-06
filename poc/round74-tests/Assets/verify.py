"""One bounded static physical-track root-delta consumer witness; never executes original code."""
from pathlib import Path
import argparse,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x0083E630;SIZE=606;BODY_SHA='aa7d495e3acf45451158cb3adfefd871006de0d60b7d1c54003f5968829f953d'
VTABLE=0x015E5DC4;SLOT=VTABLE+0x18;COL=0x01618A50;TYPE=0x01896AF0
TYPE_NAME=b'.?AVCompressedKeyframeAnimation@@\0'
ANCHORS={
0x0083E64D:('8bf1','mov','esi, ecx'),0x0083E655:('8d462c','lea','eax, [esi + 0x2c]'),0x0083E67C:('8d7e20','lea','edi, [esi + 0x20]'),
0x0083E735:('8b4e08','mov','ecx, dword ptr [esi + 8]'),0x0083E73A:('7466','je','0x83e7a2'),0x0083E73C:('8b11','mov','edx, dword ptr [ecx]'),
0x0083E73E:('8d45dc','lea','eax, [ebp - 0x24]'),0x0083E742:('8d45cc','lea','eax, [ebp - 0x34]'),0x0083E746:('ff5218','call','dword ptr [edx + 0x18]'),
0x0083E74C:('d807','fadd','dword ptr [edi]'),0x0083E74E:('8d5e2c','lea','ebx, [esi + 0x2c]'),0x0083E755:('d91f','fstp','dword ptr [edi]'),
0x0083E75B:('8d4dcc','lea','ecx, [ebp - 0x34]'),0x0083E75E:('d84704','fadd','dword ptr [edi + 4]'),0x0083E761:('d95f04','fstp','dword ptr [edi + 4]'),
0x0083E767:('d84708','fadd','dword ptr [edi + 8]'),0x0083E76A:('d95f08','fstp','dword ptr [edi + 8]'),0x0083E76D:('e87ec22700','call','0xaba9f0'),
0x0083E774:('8913','mov','dword ptr [ebx], edx'),0x0083E779:('894b04','mov','dword ptr [ebx + 4], ecx'),0x0083E77F:('895308','mov','dword ptr [ebx + 8], edx'),
0x0083E788:('89430c','mov','dword ptr [ebx + 0xc], eax'),0x0083E79D:('e8ce19ffff','call','0x830170'),0x0083E7B0:('ff4004','inc','dword ptr [eax + 4]'),
0x0083E7D8:('894608','mov','dword ptr [esi + 8], eax'),0x0083E88B:('c20800','ret','8')}

def sha(b):return hashlib.sha256(b).hexdigest()
def gate(b):
 if sha(b)!=EXE_SHA:raise ValueError('Executable identity mismatch before PE/VA use')
def body_gate(b):
 if len(b)!=SIZE or sha(b)!=BODY_SHA:raise ValueError('Consumer body mismatch')
def typed_gate(slot,col_pointer,col_words,type_name):
 if slot!=0x00822D40 or col_pointer!=COL or col_words!=(0,0,0,TYPE,0x01618A40) or type_name!=TYPE_NAME:raise ValueError('Typed slot metadata mismatch')
def reject(action):
 try:action()
 except ValueError:return 1
 raise AssertionError('Expected static guard rejection')
def run(exe,packages,out):
 destination=output_path(out);data=exe.read_bytes();gate(data);image=RawPe(data);body=image.read(BODY,SIZE);body_gate(body)
 slot,=struct.unpack('<I',image.read(SLOT,4));cp,=struct.unpack('<I',image.read(VTABLE-4,4));cw=struct.unpack('<5I',image.read(COL,20));tn=image.read(TYPE+8,len(TYPE_NAME));typed_gate(slot,cp,cw,tn)
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True)
 for va,size in [(BODY,SIZE),(SLOT,4),(VTABLE-4,4),(COL,20),(TYPE+8,len(TYPE_NAME))]:
  if pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,size)!=image.read(va,size):raise ValueError('Independent file mapping mismatch')
 decoder=Cs(CS_ARCH_X86,CS_MODE_32);instructions=list(decoder.disasm(body,BODY));byva={i.address:i for i in instructions}
 calls=[(i.address,i.op_str) for i in instructions if i.mnemonic=='call'];branches=[i for i in instructions if i.mnemonic.startswith('j')]
 if len(instructions)!=204 or sum(i.size for i in instructions)!=SIZE or len(calls)!=19 or len(branches)!=25 or (0x0083E746,'dword ptr [edx + 0x18]') not in calls:raise ValueError('Bounded control-flow mismatch')
 if any(int(i.op_str,16) not in byva for i in branches):raise ValueError('Branch outside bounded body/instruction boundary')
 reset=[image.location(0x01945B38,16),image.location(0x01945978,12)]
 if any(v['FileBacked'] for v in reset):raise ValueError('Reset cells unexpectedly file backed')

 for va,(hx,mn,op) in ANCHORS.items():
  ins=byva[va]
  if (ins.bytes.hex(),ins.mnemonic,ins.op_str)!=(hx,mn,op):raise ValueError('Instruction anchor mismatch')
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 guards['Body']=sum(reject(lambda:body_gate(b)) for b in [b'',body[:-1],body+b'\0',bytes([body[0]^1])+body[1:]])
 guards['TypedMetadata']=sum(reject(lambda:typed_gate(*v)) for v in [(0x00822D40+1,cp,cw,tn),(slot,cp+1,cw,tn),(slot,cp,(1,*cw[1:]),tn),(slot,cp,cw,b'other\0')])
 guards['ResetVirtualTails']=sum(reject(lambda:image.read(va,n)) for va,n in [(0x01945B38,16),(0x01945978,12)])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 destination.mkdir(parents=True)
 witnesses=dict(ResetCellLocations=reset,Calls=calls,Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],Locations=[image.location(va,n) for va,n in [(BODY,SIZE),(SLOT,4),(VTABLE-4,4),(COL,20),(TYPE+8,len(TYPE_NAME))]])
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=204,ExactAnchors=len(ANCHORS),IndirectCallVa='0083E746',VtableVa=f'{VTABLE:08X}',SlotOffset='18',SlotVa=f'{SLOT:08X}',RttiTypeName=TYPE_NAME[:-1].decode(),CompleteObjectLocator=f'{COL:08X}',TypeDescriptor=f'{TYPE:08X}',ConcreteConsumerBodies=1,TinyCalleesExpanded=0,AdditionalBodiesExpanded=0,DiscoveryVtableWords=0,VerifiedTypedSlots=1,ConditionalCkatSlotTarget='00822D40',ActiveDynamicTypeProved=False,CallSites=19,BranchSites=25,DistinctUnexpandedDirectCallTargets=len({op for va,op in calls if op.startswith('0x')}),TrackTranslationOffsets=['20','24','28'],TrackRotationOffsets=['2C','30','34','38'],ResetCellRuntimeValuesKnown=False,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,OriginalCorpusRead=False,ActorBasisEstablished=False,ActorApplicationEstablished=False,PhysicalTrackOwnerActorEstablished=False,ProductionCodeChanged=False)
 (destination/'static-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8');(destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.output)
