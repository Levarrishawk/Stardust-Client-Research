"""One bounded static CKAT scalar-pointer initializer witness; never executes original code."""
from pathlib import Path
import argparse,hashlib,json,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_terrain_shader_family_predicate import RawPe
from verify_terrain_shader_family_consumer import output_path
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
BODY=0x00822FC0;SIZE=793;BODY_SHA='decc1b4d66c5d4142fafe6341ef5e25780e228ecdc0527a3570895e459fabdcb'
VTABLE=0x015E5DC4;SLOT=VTABLE+0x18;COL=0x01618A50;TYPE=0x01896AF0
TYPE_NAME=b'.?AVCompressedKeyframeAnimation@@\0'
ANCHORS={0x00822FE1:('8bf1','mov','esi, ecx'),0x008230F5:('8b4d0c','mov','ecx, dword ptr [ebp + 0xc]'),
0x008230FA:('687c3f9201','push','0x1923f7c'),0x00823103:('894644','mov','dword ptr [esi + 0x44], eax'),0x00823106:('894648','mov','dword ptr [esi + 0x48], eax'),
0x00823109:('e88206fbff','call','0x7d3790'),0x0082310E:('8bce','mov','ecx, esi'),0x00823110:('89464c','mov','dword ptr [esi + 0x4c], eax'),
0x00823113:('c706c45d5e01','mov','dword ptr [esi], 0x15e5dc4'),0x008232C8:('8bc6','mov','eax, esi'),0x008232D6:('c20c00','ret','0xc')}

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
def run(exe,packages,evidence_root,out):
 destination=output_path(out);data=exe.read_bytes();gate(data);image=RawPe(data);body=image.read(BODY,SIZE);body_gate(body)
 slot,=struct.unpack('<I',image.read(SLOT,4));cp,=struct.unpack('<I',image.read(VTABLE-4,4));cw=struct.unpack('<5I',image.read(COL,20));tn=image.read(TYPE+8,len(TYPE_NAME));typed_gate(slot,cp,cw,tn)
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True)
 for va,size in [(BODY,SIZE),(SLOT,4),(VTABLE-4,4),(COL,20),(TYPE+8,len(TYPE_NAME))]:
  if pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,size)!=image.read(va,size):raise ValueError('Independent file mapping mismatch')
 decoder=Cs(CS_ARCH_X86,CS_MODE_32);instructions=list(decoder.disasm(body,BODY));byva={i.address:i for i in instructions}
 calls=[(i.address,i.op_str) for i in instructions if i.mnemonic=='call'];branches=[i for i in instructions if i.mnemonic.startswith('j')]
 if len(instructions)!=273 or sum(i.size for i in instructions)!=SIZE or len(calls)!=30 or len(branches)!=25 or (0x00823109,'0x7d3790') not in calls:raise ValueError('Bounded control-flow mismatch')
 if any(int(i.op_str,16) not in byva for i in branches):raise ValueError('Branch outside bounded body/instruction boundary')
 reset=[image.location(0x01923F7C,4)]
 if any(v['FileBacked'] for v in reset):raise ValueError('Key cell unexpectedly file backed')

 for va,(hx,mn,op) in ANCHORS.items():
  ins=byva[va]
  if (ins.bytes.hex(),ins.mnemonic,ins.op_str)!=(hx,mn,op):raise ValueError('Instruction anchor mismatch')
 guards={}
 guards['Identity']=sum(reject(lambda:gate(b)) for b in [b'',data[:-1],data+b'\0',bytes([data[0]^1])+data[1:]])
 guards['Body']=sum(reject(lambda:body_gate(b)) for b in [b'',body[:-1],body+b'\0',bytes([body[0]^1])+body[1:]])
 guards['TypedMetadata']=sum(reject(lambda:typed_gate(*v)) for v in [(0x00822D40+1,cp,cw,tn),(slot,cp+1,cw,tn),(slot,cp,(1,*cw[1:]),tn),(slot,cp,cw,b'other\0')])
 guards['KeyVirtualTail']=sum(reject(lambda:image.read(va,n)) for va,n in [(0x01923F7C,4)])
 guards['Mapping']=sum(reject(lambda:image.read(va,n)) for va,n in [(BODY,0),(BODY,-1),(0x400000,4),(0x019AA110,4)])
 import csv
 cached=evidence_root/'generated-evidence';edges=list(csv.DictReader((cached/'a03_locomotion_xrefs.tsv').open(encoding='utf-8'),delimiter='\t'))
 expected={('0083ec70','0082f9a7','0082f990'),('007d6a90','007d7830','007d7810'),('007d7810','007ca232','007ca220'),('007ca220','00639182','00639100'),('00639100','006389fd','00638810')}
 got={(v['target'],v['from'],v['from_function_entry']) for v in edges};assert expected<=got
 indirect=list(csv.DictReader((cached/'a03_indirect_call_disp_0c.tsv').open(encoding='utf-8'),delimiter='\t'));assert any(v['call']=='007d6aa4' and v['owner_entry']=='007d6a90' for v in indirect)
 inventory=list(csv.DictReader((evidence_root/'ghidra/function_inventory.tsv').open(encoding='utf-8'),delimiter='\t'));helper=[v for v in inventory if v['entry']=='007d3790'];assert len(helper)==1 and helper[0]['size']=='262'
 application=evidence_root/'ghidra/decompiled/a03-application';files=sorted(application.glob('*.c'));assert len(files)==9
 audit=dict(CachedGetterEdges=5,CachedIndirectSlotEvidence=1,ApplicationFilesPresent=9,ApplicationFileHashes=[dict(Name=f.name,Sha256=sha(f.read_bytes())) for f in files],LookupAccessorInventory=helper[0],MetadataHashes=[dict(Name=f.name,Sha256=sha(f.read_bytes())) for f in [cached/'a03_locomotion_xrefs.tsv',cached/'a03_indirect_call_disp_0c.tsv',cached/'a03_controller_vtable.tsv']])
 destination.mkdir(parents=True)
 witnesses=dict(ResetCellLocations=reset,Calls=calls,Instructions=[dict(Va=f'{i.address:08X}',Hex=i.bytes.hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],Locations=[image.location(va,n) for va,n in [(BODY,SIZE),(SLOT,4),(VTABLE-4,4),(COL,20),(TYPE+8,len(TYPE_NAME))]])
 receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY:08X}',BodyBytes=SIZE,BodySha256=BODY_SHA,Instructions=273,ExactAnchors=len(ANCHORS),ProducerCallVa='00823109',PointerStoreVa='00823110',LookupArgumentVa='01923F7C',LookupObjectArgumentOffset='0C',VtableVa=f'{VTABLE:08X}',SlotOffset='18',SlotVa=f'{SLOT:08X}',RttiTypeName=TYPE_NAME[:-1].decode(),CompleteObjectLocator=f'{COL:08X}',TypeDescriptor=f'{TYPE:08X}',ConcreteProducerBodies=1,TinyCalleesExpanded=0,AdditionalBodiesExpanded=0,DiscoveryVtableWords=0,VerifiedTypedSlots=1,ConditionalCkatSlotTarget='00822D40',ActiveDynamicTypeProved=False,CallSites=30,BranchSites=25,DistinctUnexpandedDirectCallTargets=len({op for va,op in calls if op.startswith('0x')}),ScalarKeyRuntimeContentsKnown=False,ScalarStorageOwnerProved=False,ConcreteInitializerProved=True,CachedActorApplicationPriorClosure=True,ActorApplicationBodiesReplayed=False,CachedGetterEdges=5,CachedApplicationFileCount=9,LookupAccessorBytesUnexpanded=262,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),OriginalExecution=False,OriginalCorpusRead=False,ActorBasisEstablishedInFreshBody=False,ActorApplicationEstablishedInFreshBody=False,PriorActorBasisConfidence='Confirmed by code (September25 inherited)',PriorActorApplicationScope='Eligible CreatureController object-local to parent application',FreshActorApplicationProof=False,ProductionCodeChanged=False)
 (destination/'cached-evidence-audit.json').write_text(json.dumps(audit,indent=2)+'\n',encoding='utf-8');(destination/'static-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8');(destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['exe','packages','evidence-root','output']:p.add_argument('--'+key,type=Path,required=True)
 a=p.parse_args();run(a.exe,a.packages,a.evidence_root,a.output)
