"""One family lookup wrapper; first selector boundary remains unresolved.
Static extent/operand/control and supplied-state model only; no resolver/CPU run.
"""
from __future__ import annotations
import argparse,hashlib,importlib.util,json,sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[2]
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
PREREQUISITE='tools/python/verify_terrain_shader_family_entry_consumer.py'
PREREQUISITE_SHA='777c6f605d08bf8a4c0c9eca8b4969bbc08f1ea2c5413608e9939195606e8f85'
BODY_VA,BODY_BYTES,BODY_SHA=0x00B6B580,55,'f1b4be7cfda63e76daa9ee145dfa6d9cecedaa6b4b4d776f1626fd34cb3d30a1'
BEFORE_VA,BEFORE_HEX=0x00B6B572,'5f5e5b8be55dc204009090909090'
AFTER_VA,AFTER_HEX=0x00B6B5B7,'909090909090909090'
CALLS={0x00B6B59D:0x00B69860,0x00B6B5A8:0x00B69700}
BRANCHES={}
RETURNS=(0x00B6B5B4,)
INCOMING=()
LEADS=(BODY_VA,*sorted(set(CALLS.values())))
SITES=(*CALLS,BEFORE_VA,AFTER_VA,*RETURNS)
ANCHORS = {
    0x00B6B580: ('55', 'push', 'ebp'),
    0x00B6B581: ('8bec', 'mov', 'ebp, esp'),
    0x00B6B583: ('51', 'push', 'ecx'),
    0x00B6B584: ('8b4510', 'mov', 'eax, dword ptr [ebp + 0x10]'),
    0x00B6B587: ('56', 'push', 'esi'),
    0x00B6B588: ('57', 'push', 'edi'),
    0x00B6B589: ('8b7d08', 'mov', 'edi, dword ptr [ebp + 8]'),
    0x00B6B58C: ('50', 'push', 'eax'),
    0x00B6B58D: ('8bf1', 'mov', 'esi, ecx'),
    0x00B6B58F: ('8b4d0c', 'mov', 'ecx, dword ptr [ebp + 0xc]'),
    0x00B6B592: ('57', 'push', 'edi'),
    0x00B6B593: ('51', 'push', 'ecx'),
    0x00B6B594: ('8bce', 'mov', 'ecx, esi'),
    0x00B6B596: ('c745fc00000000', 'mov', 'dword ptr [ebp - 4], 0'),
    0x00B6B59D: ('e8bee2ffff', 'call', '0xb69860'),
    0x00B6B5A2: ('8b5604', 'mov', 'edx, dword ptr [esi + 4]'),
    0x00B6B5A5: ('8b0c82', 'mov', 'ecx, dword ptr [edx + eax*4]'),
    0x00B6B5A8: ('e853e1ffff', 'call', '0xb69700'),
    0x00B6B5AD: ('8bc7', 'mov', 'eax, edi'),
    0x00B6B5AF: ('5f', 'pop', 'edi'),
    0x00B6B5B0: ('5e', 'pop', 'esi'),
    0x00B6B5B1: ('8be5', 'mov', 'esp, ebp'),
    0x00B6B5B3: ('5d', 'pop', 'ebp'),
    0x00B6B5B4: ('c20c00', 'ret', '0xc'),
}
def sha(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data)!=EXE_SHA:
        raise ValueError('Executable identity mismatch before VA inspection')


def sealed_gate(data):
    if sha(data)!=PREREQUISITE_SHA:
        raise ValueError('Frozen67 prerequisite identity mismatch')


def dependency():
    file=REPO/PREREQUISITE
    sealed_gate(file.read_bytes())
    spec=importlib.util.spec_from_file_location('lookup_wrapper67',file)
    source=importlib.util.module_from_spec(spec);spec.loader.exec_module(source)
    inherited,mapping,path=source.dependency()
    return source,inherited,mapping,path


def budget(leads=LEADS,sites=SITES,maximum_leads=12,maximum_sites=12):
    for values,maximum,expected in ((leads,maximum_leads,LEADS),(sites,maximum_sites,SITES)):
        if type(maximum) is not int or not 0<=maximum<=12:
            raise ValueError('Approved ceiling must be integer 0..12')
        if len(set(values))!=len(values) or len(values)>maximum or tuple(values)!=expected:
            raise ValueError('Duplicate, exceeded or unassigned inventory')


def check_anchors(read,decoder):
    for va,(encoded,mnemonic,operands) in ANCHORS.items():
        raw=read(va,len(bytes.fromhex(encoded)))
        if raw.hex()!=encoded:
            raise ValueError(f'Raw anchor mismatch {va:08X}')
        items=list(decoder.disasm(raw,va))
        if len(items)!=1 or (items[0].mnemonic,items[0].op_str)!=(mnemonic,operands):
            raise ValueError(f'Operand anchor mismatch {va:08X}')


def check_extent(read):
    if read(BEFORE_VA,len(bytes.fromhex(BEFORE_HEX))).hex()!=BEFORE_HEX or read(AFTER_VA,len(bytes.fromhex(AFTER_HEX))).hex()!=AFTER_HEX:
        raise ValueError('Predecessor return/padding or successor padding mismatch')
    if read(BODY_VA,3)!=bytes.fromhex('558bec'):
        raise ValueError('Selected frame prologue mismatch')


def check_control(instructions):
    from capstone.x86 import X86_OP_IMM
    calls,branches={},{}
    returns=[]
    addresses={i.address for i in instructions}
    by_address={i.address:i for i in instructions}
    for item in instructions:
        if item.mnemonic=='ret':
            returns.append(item.address)
            if item.op_str!='0xc':
                raise ValueError('Selected return cleanup mismatch')
        if item.mnemonic=='call' or item.mnemonic.startswith('j'):
            if len(item.operands)!=1 or item.operands[0].type!=X86_OP_IMM:
                raise ValueError('Unexpected indirect control edge')
            target=item.operands[0].imm&0xffffffff
            if item.mnemonic=='call':calls[item.address]=target
            else:
                if target not in addresses:
                    raise ValueError('Branch misses selected instruction boundary')
                branches[item.address]=(item.mnemonic,target)
    if calls!=CALLS or branches!=BRANCHES or tuple(returns)!=RETURNS:
        raise ValueError('Complete call/branch/return inventory mismatch')
    # All selected instructions reachable from the prologue under some supplied
    # branch result; this selected wrapper has one straight-line return.
    seen=set();pending=[BODY_VA]
    while pending:
        at=pending.pop()
        if at in seen:continue
        if at not in by_address:raise ValueError('Control flow leaves selected body')
        seen.add(at);item=by_address[at]
        if item.mnemonic=='ret':continue
        if item.mnemonic.startswith('j'):
            pending.append(branches[at][1])
            if item.mnemonic=='jmp':continue
        pending.append(at+item.size)
    if seen!=addresses:
        raise ValueError('Disconnected selected instructions')


def check_body(read,decoder):
    raw=read(BODY_VA,BODY_BYTES)
    if len(raw)!=BODY_BYTES or sha(raw)!=BODY_SHA:
        raise ValueError('Selected complete caller mismatch')
    items=list(decoder.disasm(raw,BODY_VA))
    if len(items)!=24 or sum(i.size for i in items)!=BODY_BYTES or items[-1].address!=RETURNS[-1]:
        raise ValueError('Selected complete body extent mismatch')
    check_extent(read);check_anchors(read,decoder);check_control(items)
    return items


def incoming_candidates(image):
    sites=[]
    for name,rva,span,raw_at,size,flags in image.sections:
        if not flags&0x20000000:continue
        raw=image.data[raw_at:raw_at+size]
        for offset in range(len(raw)-4):
            if raw[offset]!=0xe8:continue
            va=image.base+rva+offset
            if (va+5+int.from_bytes(raw[offset+1:offset+5],'little',signed=True))&0xffffffff==BODY_VA:
                sites.append(va)
    if tuple(sites)!=INCOMING:
        raise ValueError('Incoming raw E8 candidate inventory mismatch')
    return sites


UNCHANGED=object()


def wrapper_witness(group_receiver,families,selector_input,search,destination,selected_index,lookup_words,
                    *,post_selector_families=None,edi_after_selector=UNCHANGED,lookup_eax=None):
    """Supplied ABI-conforming stack state: selector consumes one stack word.

    Finite index/triple guards are modern model policy. EDI and array changes are
    explicitly supplied; no original selector, lookup or memory access is run.
    """
    available=families if post_selector_families is None else post_selector_families
    if type(selected_index) is not int or not 0<=selected_index<=0xffffffff or selected_index>=len(available):
        raise ValueError('Synthetic selected family index outside supplied extent')
    words=tuple(lookup_words)
    if len(words)!=3:raise ValueError('Synthetic lookup triple extent invalid')
    register=destination if edi_after_selector is UNCHANGED else edi_after_selector
    return dict(SelectorReceiver=group_receiver,SelectorStackPrefix=(selector_input,destination,search),
                SelectedIndex=selected_index,SelectedFamily=available[selected_index],LookupDestination=destination,
                LookupSearch=search,Words=words,IgnoredLookupEax=lookup_eax,ReturnedEdi=register,Return=register,
                SelectorOneWordStackCleanupSupplied=True,StorePerformedByWrapper=False,PointerByteTransform=False)


def synthetic_checks():
    group=object();families=[object(),object()];key=object();search=bytearray(b'MiX/\\\xff\0');dest=object()
    pointer=bytearray(b'record\0');checks=0
    for index in (0,1):
        for words in ((0x80000000,pointer,0xffffffff),(0,None,0)):
            for child_eax in (None,object()):
                value=wrapper_witness(group,families,key,search,dest,index,words,lookup_eax=child_eax)
                assert value['SelectorStackPrefix'][0] is key and value['SelectorStackPrefix'][1] is dest and value['SelectorStackPrefix'][2] is search
                assert value['SelectedFamily'] is families[index] and value['LookupSearch'] is search and value['LookupDestination'] is dest
                assert value['Return'] is dest and value['Words'][1] is words[1] and value['IgnoredLookupEax'] is child_eax
                checks+=3
    replacement=object();changed=[replacement];register=object()
    value=wrapper_witness(group,families,key,search,dest,0,(1,pointer,2),post_selector_families=changed,edi_after_selector=register)
    assert value['SelectedFamily'] is replacement and value['Return'] is register and value['LookupDestination'] is dest
    pointer[0]=ord('X');assert value['Words'][1][0]==ord('X')
    checks+=2
    return checks


def verify(executable,packages,asset_root,config,output,maximum_leads=12,maximum_sites=12):
    budget(maximum_leads=maximum_leads,maximum_sites=maximum_sites)
    lookup,source,mapping,path=dependency();destination=path.output_path(output)
    data=executable.read_bytes();gate(data);image=mapping.RawPe(data)
    sys.path[:0]=[str(packages),str(REPO/'tools/python')]
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=data,fast_load=True);decoder=Cs(CS_ARCH_X86,CS_MODE_32);decoder.detail=True
    location=image.location(BODY_VA,BODY_BYTES)
    if not location['Executable'] or pe.get_offset_from_rva(BODY_VA-image.base)!=location['FileOffset']:
        raise ValueError('Independent raw PE/pefile mapping mismatch')
    instructions=check_body(image.read,decoder);incoming=incoming_candidates(image)
    original=source.original_records(asset_root,config)
    families=[object() for _ in original['Families']];group=object();selector_input=object();search=object()
    witnesses=[]
    for family in original['Families']:
        entries=[(bytearray(bytes.fromhex(entry['StringHex'])+b'\0'),entry['RawWordBits']) for entry in family['Entries']]
        for index,entry in enumerate(family['Entries']):
            destination_record=object()
            selected=lookup.lookup_witness(family['FamilyKeyBits'],entries,search,[1]*index+[0],destination_record)
            chosen=wrapper_witness(group,families,selector_input,search,destination_record,family['Index'],selected['Words'],lookup_eax=selected['Return'])
            assert chosen['Words'][1] is entries[index][0] and chosen['Return'] is destination_record
            witnesses.append(dict(FamilyIndex=family['Index'],EntryIndex=index,FamilyWordBits=chosen['Words'][0],EntryWordBits=chosen['Words'][2],
                 StringWithNulSha256=sha(chosen['Words'][1]),StringOffset=entry['Offset'],SuppliedSelectorIndex=family['Index'],
                 SuppliedPredicateResults='nonzero prefix then zero',SuppliedSelectorStackCleanupBytes=4,
                 PointerIdentityPreserved=True,SearchAndDestinationIdentityPreserved=True))
    receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY_VA:08X}',BodyBytes=BODY_BYTES,BodySha256=BODY_SHA,
                 Instructions=len(instructions),Anchors=len(ANCHORS),ExtentWitnesses=2,DirectCallSites=len(CALLS),Branches=0,Returns=1,
                 FunctionHandlerLeads=len(LEADS),RawSites=len(SITES),ApprovedLeadCap=12,ApprovedRawSiteCap=12,ExpandedBodies=1,TinyHelpersExpanded=0,
                 CandidateCallBoundaryProved=True,FirstUnresolvedSelector='00B69860',FirstUnresolvedSelectorSite='00B6B59D',
                 LookupForward='00B69700',LookupForwardSite='00B6B5A8',SearchSource='caller stack argument three',
                 SelectorInputSource='caller stack argument two; semantics unknown',DestinationSource='caller stack argument one',
                 FamilyArrayOffset=4,IndexScale=4,ReturnRegister='EDI',LookupEaxIgnored=True,WrapperRecordBytesDereferenced=False,
                 OriginalSelectorStackCleanupProved=False,NormalIntendedStackCleanupBytes=4,OriginalSelectorNonvolatilePreservationProved=False,
                 FunctionBoundaryConfidence='strongly inferred complete frame/control/return/padding extent; no external symbol authority',
                 RawIncomingCandidates=[f'{x:08X}' for x in incoming],RuntimeReachabilityProved=False,
                 SyntheticChecks=synthetic_checks(),OriginalOccurrences=1,OriginalFamilies=15,OriginalEntries=35,OriginalWrapperModelWitnesses=len(witnesses),
                 OriginalGroupSha256=original['SourceSha256'],RawPeMapping=location,FrozenPrerequisiteSha256=PREREQUISITE_SHA,
                 SelectorBodyExpanded=False,LookupBodyReanalysis=False,AccessorPredicateBodyReanalysis=False,OriginalExecution=False,
                 ProductionResolver=False,StringConversionProved=False,ResourceOpenFetchProved=False)
    path.output_path(destination);destination.mkdir(parents=True)
    (destination/'wrapper-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8')
    (destination/'instruction-witnesses.json').write_text(json.dumps([dict(Va=f'{i.address:08X}',Bytes=bytes(i.bytes).hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],indent=2)+'\n',encoding='utf-8')
    (destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','asset-root','config','output'):parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--maximum-leads',type=int,default=12);parser.add_argument('--maximum-sites',type=int,default=12)
    args=parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads,args.maximum_sites),indent=2))
