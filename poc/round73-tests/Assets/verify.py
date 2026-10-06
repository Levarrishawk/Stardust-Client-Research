"""One bounded static CKAT root-delta consumer witness; never executes original code."""
from pathlib import Path
import argparse,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x00822D40;SIZE=76;BODY_SHA='f6e68d4923cf2c118124d764335b32bb235d580c3123a08542dd4768338e9093'
VTABLE=0x015E5DC4;SLOT=VTABLE+0x18;COL=0x01618A50;TYPE=0x01896AF0
TYPE_NAME=b'.?AVCompressedKeyframeAnimation@@\0'
ANCHORS={0x00822D4D:('d94610','fld','dword ptr [esi + 0x10]'),0x00822D50:('8b4e08','mov','ecx, dword ptr [esi + 8]'),0x00822D55:('8d5648','lea','edx, [esi + 0x48]'),0x00822D59:('8d4644','lea','eax, [esi + 0x44]'),0x00822D64:('d94614','fld','dword ptr [esi + 0x14]'),0x00822D6A:('ff516c','call','dword ptr [ecx + 0x6c]'),0x00822D6D:('8b4e4c','mov','ecx, dword ptr [esi + 0x4c]'),0x00822D70:('d901','fld','dword ptr [ecx]'),0x00822D76:('d91f','fstp','dword ptr [edi]'),0x00822D7D:('d95f04','fstp','dword ptr [edi + 4]'),0x00822D83:('d95f08','fstp','dword ptr [edi + 8]'),0x00822D89:('c20800','ret','8')}
def sha(b):return hashlib.sha256(b).hexdigest()
def gate(b):
 if sha(b)!=EXE_SHA:raise ValueError('Executable identity mismatch before PE/VA use')
def body_gate(b):
 if len(b)!=SIZE or sha(b)!=BODY_SHA:raise ValueError('Consumer body mismatch')
def typed_gate(slot,col_pointer,col_words,type_name):
 if slot!=BODY or col_pointer!=COL or col_words!=(0,0,0,TYPE,0x01618A40) or type_name!=TYPE_NAME:raise ValueError('Typed slot metadata mismatch')
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
 if len(instructions)!=34 or sum(i.size for i in instructions)!=SIZE or [(i.address,i.op_str) for i in instructions if i.mnemonic=='call']!=[(0x00822D6A,'dword ptr [ecx + 0x6c]')] or any(i.mnemonic.startswith('j') for i in instructions):raise ValueError('Bounded control-flow witness mismatch')
 for va,(hx,mn,op) in ANCHORS.items():
  ins=byva[va]
  if (ins.bytes.hex(),ins.mnemonic,ins.op_str)!=(hx,mn,op):raise ValueError('Instruction anchor mismatch')
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 guards['Body']=sum(reject(lambda:body_gate(b)) for b in [b'',body[:-1],body+b'\0',bytes([body[0]^1])+body[1:]])
 guards['TypedMetadata']=sum(reject(lambda:typed_gate(*v)) for v in [(BODY+1,cp,cw,tn),(slot,cp+1,cw,tn),(slot,cp,(1,*cw[1:]),tn),(slot,cp,cw,b'other\0')])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 destination.mkdir(parents=True)
 witnesses=dict(Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],Locations=[image.location(va,n) for va,n in [(BODY,SIZE),(SLOT,4),(VTABLE-4,4),(COL,20),(TYPE+8,len(TYPE_NAME))]])
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=34,ExactAnchors=len(ANCHORS),IndirectCallVa='00822D6A',VtableVa=f'{VTABLE:08X}',SlotOffset='18',SlotVa=f'{SLOT:08X}',RttiTypeName=TYPE_NAME[:-1].decode(),CompleteObjectLocator=f'{COL:08X}',TypeDescriptor=f'{TYPE:08X}',ConcreteConsumerBodies=1,TinyCalleesExpanded=0,AdditionalBodiesExpanded=0,DiscoveryVtableWords=16,VerifiedTypedSlots=1,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,OriginalCorpusRead=False,ActorBasisEstablished=False,ActorApplicationEstablished=False,ScalePointerOwnershipEstablished=False,ProductionCodeChanged=False)
 (destination/'static-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8');(destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.output)
