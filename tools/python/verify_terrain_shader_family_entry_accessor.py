"""Static family-entry record accessor and one group-level forwarding wrapper.
No original execution, resolver, resource semantics or original bounds policy.
"""
from __future__ import annotations
import argparse,contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[2]
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
SEALED={
 'tools/python/verify_terrain_shader_family_second_string_caller.py':'3a9fbd01e99cd3b61cee3d773745af1b4759d6957398fb805b96b12d19d44060',
 'poc/round61-tests/Assets/peer.py':'a6bea13cac97d997f86791939b13a9717a4a98c451a6324b4b819ecfdd429998',
 'poc/round33-tests/Assets/verify_tgen_input.py':'9b41b515019963bab0713003760bd13708d8a3810c2917bf337b4808ddc5f649',
}
APPROVED_LEADS=12
APPROVED_SITES=12
BODIES = {
    'Initializer': (0x00B69160, 13, '1e09d34a9de7a48bd061703ef73f341df8596595468b282c5f19c36b6bf0b98d', 6),
    'Accessor': (0x00B692A0, 84, '9bc611f07cae269fe4656d82cdbc556288f3076896cff4eb3c2635c731936608', 30),
    'Caller': (0x00B69B40, 43, '58ad4d3736af830699184b9de4f8f991412c4f9c7f42b073f5a2eec9c6a5ddd4', 18),
}
ANCHORS = {
    0x00B69160: ('8bc1', 'mov', 'eax, ecx'),
    0x00B69162: ('33c9', 'xor', 'ecx, ecx'),
    0x00B69164: ('8908', 'mov', 'dword ptr [eax], ecx'),
    0x00B69166: ('894804', 'mov', 'dword ptr [eax + 4], ecx'),
    0x00B69169: ('894808', 'mov', 'dword ptr [eax + 8], ecx'),
    0x00B6916C: ('c3', 'ret', ''),

    0x00B692A0: ('55', 'push', 'ebp'),
    0x00B692A1: ('8bec', 'mov', 'ebp, esp'),
    0x00B692A3: ('83ec10', 'sub', 'esp, 0x10'),
    0x00B692A6: ('8bd1', 'mov', 'edx, ecx'),
    0x00B692A8: ('56', 'push', 'esi'),
    0x00B692A9: ('8d4df0', 'lea', 'ecx, [ebp - 0x10]'),
    0x00B692AC: ('c745fc00000000', 'mov', 'dword ptr [ebp - 4], 0'),
    0x00B692B3: ('e8a8feffff', 'call', '0xb69160'),
    0x00B692B8: ('8b0a', 'mov', 'ecx, dword ptr [edx]'),
    0x00B692BA: ('8b5220', 'mov', 'edx, dword ptr [edx + 0x20]'),
    0x00B692BD: ('8b450c', 'mov', 'eax, dword ptr [ebp + 0xc]'),
    0x00B692C0: ('8d0440', 'lea', 'eax, [eax + eax*2]'),
    0x00B692C3: ('894df0', 'mov', 'dword ptr [ebp - 0x10], ecx'),
    0x00B692C6: ('8d0482', 'lea', 'eax, [edx + eax*4]'),
    0x00B692C9: ('8b5004', 'mov', 'edx, dword ptr [eax + 4]'),
    0x00B692CC: ('8955f4', 'mov', 'dword ptr [ebp - 0xc], edx'),
    0x00B692CF: ('8b4008', 'mov', 'eax, dword ptr [eax + 8]'),
    0x00B692D2: ('8945f8', 'mov', 'dword ptr [ebp - 8], eax'),
    0x00B692D5: ('8b4508', 'mov', 'eax, dword ptr [ebp + 8]'),
    0x00B692D8: ('8bf0', 'mov', 'esi, eax'),
    0x00B692DA: ('890e', 'mov', 'dword ptr [esi], ecx'),
    0x00B692DC: ('8b4df8', 'mov', 'ecx, dword ptr [ebp - 8]'),
    0x00B692DF: ('895604', 'mov', 'dword ptr [esi + 4], edx'),
    0x00B692E2: ('894e08', 'mov', 'dword ptr [esi + 8], ecx'),
    0x00B692E5: ('8d4df0', 'lea', 'ecx, [ebp - 0x10]'),
    0x00B692E8: ('e883feffff', 'call', '0xb69170'),
    0x00B692ED: ('5e', 'pop', 'esi'),
    0x00B692EE: ('8be5', 'mov', 'esp, ebp'),
    0x00B692F0: ('5d', 'pop', 'ebp'),
    0x00B692F1: ('c20800', 'ret', '8'),
    0x00B69B40: ('55', 'push', 'ebp'),
    0x00B69B41: ('8bec', 'mov', 'ebp, esp'),
    0x00B69B43: ('51', 'push', 'ecx'),
    0x00B69B44: ('8b4510', 'mov', 'eax, dword ptr [ebp + 0x10]'),
    0x00B69B47: ('8b4904', 'mov', 'ecx, dword ptr [ecx + 4]'),
    0x00B69B4A: ('8b550c', 'mov', 'edx, dword ptr [ebp + 0xc]'),
    0x00B69B4D: ('8b0c91', 'mov', 'ecx, dword ptr [ecx + edx*4]'),
    0x00B69B50: ('56', 'push', 'esi'),
    0x00B69B51: ('8b7508', 'mov', 'esi, dword ptr [ebp + 8]'),
    0x00B69B54: ('50', 'push', 'eax'),
    0x00B69B55: ('56', 'push', 'esi'),
    0x00B69B56: ('c745fc00000000', 'mov', 'dword ptr [ebp - 4], 0'),
    0x00B69B5D: ('e83ef7ffff', 'call', '0xb692a0'),
    0x00B69B62: ('8bc6', 'mov', 'eax, esi'),
    0x00B69B64: ('5e', 'pop', 'esi'),
    0x00B69B65: ('8be5', 'mov', 'esp, ebp'),
    0x00B69B67: ('5d', 'pop', 'ebp'),
    0x00B69B68: ('c20c00', 'ret', '0xc'),
}
CALLS={0x00B692B3:0x00B69160,0x00B692E8:0x00B69170,0x00B69B5D:0x00B692A0}
RAW_CANDIDATES=(0x00B69779,0x00B69B5D)
LEADS=(0x00B692A0,0x00B69B40,0x00B69160,0x00B69170)
SITES=(*CALLS,0x00B69779)
HELPER_APPROVED=True


def sha(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data)!=EXE_SHA:
        raise ValueError('Executable SHA mismatch before VA inspection')


def sealed_gate(name,data):
    if name not in SEALED or sha(data)!=SEALED[name]:
        raise ValueError('Frozen prerequisite source identity mismatch')


def load(name):
    source=REPO/name
    sealed_gate(name,source.read_bytes())
    spec=importlib.util.spec_from_file_location('entry66_'+Path(name).stem,source)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def dependency():
    old=load('tools/python/verify_terrain_shader_family_second_string_caller.py')
    source,mapping,path=old.dependency()
    # Mapping/path machinery only; no setter/predicate/output body verification.
    return mapping,path


def budget(leads=LEADS,sites=SITES,maximum_leads=12,maximum_sites=12):
    for values,maximum,expected in ((leads,maximum_leads,LEADS),(sites,maximum_sites,SITES)):
        if type(maximum) is not int or not 0<=maximum<=12:
            raise ValueError('Approved ceiling must be integer 0..12')
        if len(set(values))!=len(values) or len(values)>maximum or tuple(values)!=expected:
            raise ValueError('Duplicate, unassigned or exceeded selected inventory')


def check_anchors(read,decoder):
    for va,(encoded,mnemonic,operands) in ANCHORS.items():
        raw=read(va,len(bytes.fromhex(encoded)))
        if raw.hex()!=encoded:
            raise ValueError(f'Raw witness mismatch {va:08X}')
        items=list(decoder.disasm(raw,va))
        if len(items)!=1 or (items[0].mnemonic,items[0].op_str)!=(mnemonic,operands):
            raise ValueError(f'Operand witness mismatch {va:08X}')


def check_control(instructions):
    from capstone.x86 import X86_OP_IMM
    calls={}
    for item in instructions:
        if item.mnemonic.startswith('j'):
            raise ValueError('Unexpected selected branch')
        if item.mnemonic=='call':
            if len(item.operands)!=1 or item.operands[0].type!=X86_OP_IMM:
                raise ValueError('Unexpected indirect selected call')
            calls[item.address]=item.operands[0].imm&0xffffffff
    if calls!=CALLS:
        raise ValueError('Complete selected call inventory mismatch')


def check_bodies(read,decoder):
    instructions=[]
    for name,(va,size,wanted,count) in BODIES.items():
        raw=read(va,size)
        if len(raw)!=size or sha(raw)!=wanted:
            raise ValueError('Complete selected body mismatch '+name)
        items=list(decoder.disasm(raw,va))
        if len(items)!=count or sum(i.size for i in items)!=size or items[-1].mnemonic!='ret':
            raise ValueError('Complete selected body/return extent mismatch')
        if any(i.mnemonic=='ret' for i in items[:-1]):
            raise ValueError('Premature selected return')
        instructions.extend(items)
    check_anchors(read,decoder)
    check_control(instructions)
    return instructions


def raw_candidates(image):
    """Raw E8 scan; no instruction-boundary/caller classification for other site."""
    sites=[]
    for name,rva,span,raw_at,raw_size,flags in image.sections:
        if not flags&0x20000000:
            continue
        raw=image.data[raw_at:raw_at+raw_size]
        for offset in range(len(raw)-4):
            if raw[offset]!=0xe8:
                continue
            va=image.base+rva+offset
            target=(va+5+int.from_bytes(raw[offset+1:offset+5],'little',signed=True))&0xffffffff
            if target==0x00B692A0:
                sites.append(va)
    if tuple(sites)!=RAW_CANDIDATES:
        raise ValueError('Raw direct-call candidate inventory mismatch')
    return sites


def initializer_witness(local_destination,family_receiver):
    """Synthetic helper effects: ECX zeroed; EDX family preserved; EAX local."""
    return dict(Eax=local_destination,Ecx=0,Edx=family_receiver,LocalWords=(0,0,0))


def record_witness(family_word,entry_pointer,entry_word,destination):
    """Supplied in-bounds structural record and wrapper return; no byte copy/retain."""
    return dict(Destination=destination,Return=destination,Words=(family_word,entry_pointer,entry_word))


def index_address(base,index,stride):
    """x86 arithmetic only; no bounds check or memory access is modeled."""
    return (base+(index&0xffffffff)*stride)&0xffffffff


def synthetic_checks():
    count=0
    for family in (None,object()):
        local=object()
        result=initializer_witness(local,family)
        assert result['Eax'] is local and result['Ecx']==0
        assert result['Edx'] is family and result['LocalWords']==(0,0,0)
        count+=2
    for key in (0,1,0x80000000,0xffffffff):
        for pointer in (None,bytearray(b'\0'),bytearray(b'MiX/\\\xff\0tail')):
            destination=object()
            result=record_witness(key,pointer,key^0xffffffff,destination)
            assert result['Return'] is destination and result['Destination'] is destination
            assert result['Words'][0]==key and result['Words'][1] is pointer and result['Words'][2]==key^0xffffffff
            count+=2
    for base,index,stride,wanted in ((0x1000,0,12,0x1000),(0x1000,1,12,0x100c),(0x1000,-1,12,0xff4),(0xfffffff8,1,12,4),(0x1000,0x40000000,4,0x1000)):
        assert index_address(base,index,stride)==wanted
        count+=1
    pointer=bytearray(b'owned\0')
    destination=object()
    result=record_witness(1,pointer,2,destination)
    pointer[0]=ord('X')
    assert result['Words'][1][0]==ord('X')
    count+=1
    return count


def original_records(asset_root,config):
    t=load('poc/round33-tests/Assets/verify_tgen_input.py')
    peer=load('poc/round61-tests/Assets/peer.py')
    if t.sha(config.read_bytes())!=t.CONFIG:
        raise ValueError('Pinned configuration SHA mismatch')
    original=None
    with contextlib.ExitStack() as stack:
        for priority,path in t.archives(asset_root,config):
            original=t.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
            if original is not None:
                break
    if original is None or priority!=42 or len(original)!=1188446 or t.sha(original)!=t.SOURCE:
        raise ValueError('Pinned original terrain identity mismatch')
    generator=original[210:210+139608]
    if t.sha(generator)!=t.GENERATOR:
        raise ValueError('Pinned generator SHA mismatch')
    blob=generator[24:24+1977]
    if sha(blob)!='c0f9043140db936e3f3f2dd3eec795f1bee5424e6e904081c667cfc9e99a0628':
        raise ValueError('Pinned SGRP SHA mismatch')
    result=peer.inspect(blob)
    if len(result['Families'])!=15 or result['TotalEntries']!=35:
        raise ValueError('Pinned original record count mismatch')
    return result


def verify(executable,packages,asset_root,config,output,maximum_leads=12,maximum_sites=12):
    budget(maximum_leads=maximum_leads,maximum_sites=maximum_sites)
    mapping,path=dependency()
    destination=path.output_path(output)
    data=executable.read_bytes()
    gate(data)
    image=mapping.RawPe(data)
    sys.path[:0]=[str(packages),str(REPO/'tools/python')]
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=data,fast_load=True)
    decoder=Cs(CS_ARCH_X86,CS_MODE_32)
    decoder.detail=True
    locations={}
    for name,(va,size,_,_) in BODIES.items():
        location=image.location(va,size)
        if not location['Executable'] or pe.get_offset_from_rva(va-image.base)!=location['FileOffset']:
            raise ValueError('Independent raw/pefile mapping mismatch')
        locations[name]=location
    instructions=check_bodies(image.read,decoder)
    candidates=raw_candidates(image)
    source=original_records(asset_root,config)
    witnesses=[]
    for family in source['Families']:
        for entry in family['Entries']:
            pointer=bytearray(bytes.fromhex(entry['StringHex'])+b'\0')
            dest=object()
            value=record_witness(family['FamilyKeyBits'],pointer,entry['RawWordBits'],dest)
            assert value['Return'] is dest and value['Words'][1] is pointer
            witnesses.append(dict(FamilyIndex=family['Index'],EntryIndex=entry['Index'],StringOffset=entry['Offset'],
                FamilyWordBits=value['Words'][0],EntryWordBits=value['Words'][2],StringWithNulSha256=sha(value['Words'][1]),
                BorrowedPointerIdentityPreserved=True,DestinationIdentityPreserved=True))
    receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),Bodies=BODIES,Instructions=len(instructions),Anchors=len(ANCHORS),
                 DirectCalls=len(CALLS),Branches=0,FunctionHandlerLeads=len(LEADS),RawSites=len(SITES),ApprovedLeadCap=12,ApprovedRawSiteCap=12,
                 ExpandedBodies=2,TinyHelpersExpanded=int(HELPER_APPROVED),RawCallCandidates=[f'{x:08X}' for x in candidates],
                 ProvenSelectedCaller='00B69B40',UnexpandedOtherCallCandidate='00B69779',FirstUnexpandedDelegate='00B69170' if HELPER_APPROVED else '00B69160',
                 RecordStride=12,FamilyWordOffset=0,EntryArrayOffset=32,EntryPointerOffset=4,EntryWordOffset=8,
                 DestinationOffsets=[0,4,8],WrapperDestinationReturned=True,AccessorEaxAfterCleanupEstablished=False,OriginalOccurrences=1,OriginalFamilies=15,OriginalEntries=35,
                 OriginalRecordModelWitnesses=len(witnesses),OriginalGroupSha256=source['SourceSha256'],SyntheticChecks=synthetic_checks(),
                 RawPeMappings=locations,OriginalExecution=False,ResourceOpenFetchProved=False,StringConversionProved=False,
                 InitializerEdxPreserved=True,InitializerEcxZeroed=True,InitializerEaxReturnsLocal=True,SetterPredicateSerializationReanalysis=False,ProductionResolver=False,HelperAmendmentApproved=HELPER_APPROVED)
    path.output_path(destination)
    destination.mkdir(parents=True)
    (destination/'record-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8')
    (destination/'instruction-witnesses.json').write_text(json.dumps([dict(Va=f'{i.address:08X}',Bytes=bytes(i.bytes).hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],indent=2)+'\n',encoding='utf-8')
    (destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','asset-root','config','output'):
        parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--maximum-leads',type=int,default=12)
    parser.add_argument('--maximum-sites',type=int,default=12)
    args=parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads,args.maximum_sites),indent=2))
