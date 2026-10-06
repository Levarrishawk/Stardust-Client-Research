"""One complete entry-string lookup caller, with explicit unresolved delegates.
Static instruction/extent evidence and supplied-result models; no resolver/CPU run.
"""
from __future__ import annotations
import argparse,hashlib,importlib.util,json,sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[2]
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
PREREQUISITE='tools/python/verify_terrain_shader_family_entry_accessor.py'
PREREQUISITE_SHA='1b64ffb62a74408e737aa1d57bde7c9700e654eb8531d7422429c945f021db9d'
BODY_VA,BODY_BYTES,BODY_SHA=0x00B69700,137,'6ec6af1d3814942cfe2354b786e82edd86c49768aa3c93678ebbe5624b353458'
BEFORE_VA,BEFORE_HEX=0x00B696F0,'5dc20400909090909090909090909090'
AFTER_VA,AFTER_HEX=0x00B69789,'90909090909090'
CALLS={0x00B6972C:0x01320590,0x00B69746:0x00B69160,0x00B69764:0x00B69170,0x00B69779:0x00B692A0}
BRANCHES={0x00B69717:('jle',0x00B69743),0x00B69736:('je',0x00B69772),0x00B69741:('jl',0x00B69720)}
RETURNS=(0x00B6976F,0x00B69786)
INCOMING=(0x00B6B5A8,)
# Discovery-only prologue/adjacent-handler leads retained, never semantic contracts.
DISCOVERY_LEADS=(0x00B69630,0x00B69670,0x00B69790,0x00B697B0,0x014D23E8)
DISCOVERY_SITES=(0x00B697B5,)
DISCOVERY_ANCHORS={0x00B69630:'558bec',0x00B69670:'558bec',0x00B69790:'8bc1',0x00B697B0:'558bec',0x00B697B5:'68e8234d01'}
LEADS=(BODY_VA,*sorted(set(CALLS.values())),*DISCOVERY_LEADS)
SITES=(*CALLS,*BRANCHES,*INCOMING,BEFORE_VA,AFTER_VA,*DISCOVERY_SITES)
ANCHORS = {
    0x00B69700: ('55', 'push', 'ebp'),
    0x00B69701: ('8bec', 'mov', 'ebp, esp'),
    0x00B69703: ('83ec10', 'sub', 'esp, 0x10'),
    0x00B69706: ('53', 'push', 'ebx'),
    0x00B69707: ('56', 'push', 'esi'),
    0x00B69708: ('8bf1', 'mov', 'esi, ecx'),
    0x00B6970A: ('8b4624', 'mov', 'eax, dword ptr [esi + 0x24]'),
    0x00B6970D: ('33db', 'xor', 'ebx, ebx'),
    0x00B6970F: ('57', 'push', 'edi'),
    0x00B69710: ('33ff', 'xor', 'edi, edi'),
    0x00B69712: ('3bc3', 'cmp', 'eax, ebx'),
    0x00B69714: ('895dfc', 'mov', 'dword ptr [ebp - 4], ebx'),
    0x00B69717: ('7e2a', 'jle', '0xb69743'),
    0x00B69719: ('8da42400000000', 'lea', 'esp, [esp]'),
    0x00B69720: ('8b450c', 'mov', 'eax, dword ptr [ebp + 0xc]'),
    0x00B69723: ('8b4e20', 'mov', 'ecx, dword ptr [esi + 0x20]'),
    0x00B69726: ('8b541904', 'mov', 'edx, dword ptr [ecx + ebx + 4]'),
    0x00B6972A: ('50', 'push', 'eax'),
    0x00B6972B: ('52', 'push', 'edx'),
    0x00B6972C: ('e85f6e7b00', 'call', '0x1320590'),
    0x00B69731: ('83c408', 'add', 'esp, 8'),
    0x00B69734: ('85c0', 'test', 'eax, eax'),
    0x00B69736: ('743a', 'je', '0xb69772'),
    0x00B69738: ('8b4624', 'mov', 'eax, dword ptr [esi + 0x24]'),
    0x00B6973B: ('47', 'inc', 'edi'),
    0x00B6973C: ('83c30c', 'add', 'ebx, 0xc'),
    0x00B6973F: ('3bf8', 'cmp', 'edi, eax'),
    0x00B69741: ('7cdd', 'jl', '0xb69720'),
    0x00B69743: ('8d4df0', 'lea', 'ecx, [ebp - 0x10]'),
    0x00B69746: ('e815faffff', 'call', '0xb69160'),
    0x00B6974B: ('8b4508', 'mov', 'eax, dword ptr [ebp + 8]'),
    0x00B6974E: ('8b55f0', 'mov', 'edx, dword ptr [ebp - 0x10]'),
    0x00B69751: ('8bc8', 'mov', 'ecx, eax'),
    0x00B69753: ('8911', 'mov', 'dword ptr [ecx], edx'),
    0x00B69755: ('8b55f4', 'mov', 'edx, dword ptr [ebp - 0xc]'),
    0x00B69758: ('895104', 'mov', 'dword ptr [ecx + 4], edx'),
    0x00B6975B: ('8b55f8', 'mov', 'edx, dword ptr [ebp - 8]'),
    0x00B6975E: ('895108', 'mov', 'dword ptr [ecx + 8], edx'),
    0x00B69761: ('8d4df0', 'lea', 'ecx, [ebp - 0x10]'),
    0x00B69764: ('e807faffff', 'call', '0xb69170'),
    0x00B69769: ('5f', 'pop', 'edi'),
    0x00B6976A: ('5e', 'pop', 'esi'),
    0x00B6976B: ('5b', 'pop', 'ebx'),
    0x00B6976C: ('8be5', 'mov', 'esp, ebp'),
    0x00B6976E: ('5d', 'pop', 'ebp'),
    0x00B6976F: ('c20800', 'ret', '8'),
    0x00B69772: ('57', 'push', 'edi'),
    0x00B69773: ('8b7d08', 'mov', 'edi, dword ptr [ebp + 8]'),
    0x00B69776: ('57', 'push', 'edi'),
    0x00B69777: ('8bce', 'mov', 'ecx, esi'),
    0x00B69779: ('e822fbffff', 'call', '0xb692a0'),
    0x00B6977E: ('8bc7', 'mov', 'eax, edi'),
    0x00B69780: ('5f', 'pop', 'edi'),
    0x00B69781: ('5e', 'pop', 'esi'),
    0x00B69782: ('5b', 'pop', 'ebx'),
    0x00B69783: ('8be5', 'mov', 'esp, ebp'),
    0x00B69785: ('5d', 'pop', 'ebp'),
    0x00B69786: ('c20800', 'ret', '8'),
}
def sha(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data)!=EXE_SHA:
        raise ValueError('Executable identity mismatch before VA inspection')


def sealed_gate(data):
    if sha(data)!=PREREQUISITE_SHA:
        raise ValueError('Frozen66 prerequisite identity mismatch')


def dependency():
    file=REPO/PREREQUISITE
    sealed_gate(file.read_bytes())
    spec=importlib.util.spec_from_file_location('entry_consumer66',file)
    source=importlib.util.module_from_spec(spec);spec.loader.exec_module(source)
    mapping,path=source.dependency()
    return source,mapping,path


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
    if read(BEFORE_VA,16).hex()!=BEFORE_HEX or read(AFTER_VA,7).hex()!=AFTER_HEX:
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
            if item.op_str!='8':
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
    # branch result. First ret must not truncate the explicit alternate branch.
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
    if len(items)!=58 or sum(i.size for i in items)!=BODY_BYTES or items[-1].address!=RETURNS[-1]:
        raise ValueError('Selected complete body extent mismatch')
    check_extent(read);check_anchors(read,decoder);check_control(items)
    return items


def check_discovery(image):
    # Short lead/reference anchors only; no adjacent body/helper contract.
    for va,encoded in DISCOVERY_ANCHORS.items():
        if image.read(va,len(bytes.fromhex(encoded))).hex()!=encoded:
            raise ValueError('Discovery-only lead/reference anchor mismatch')
    handler=image.location(0x014D23E8,1)
    if not handler['Executable']:
        raise ValueError('Discovery-only handler metadata mismatch')
    return handler


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


def signed_count(word):
    if type(word) is not int:raise ValueError("Synthetic count must be an integer")
    value=word&0xffffffff
    return value-(1<<32) if value&0x80000000 else value


def lookup_witness(family_word,entries,search,results,destination,initial_count=None,reloaded_counts=None,selected_record=None):
    """Finite supplied-result model; does not run comparator/accessor/cleanup.

    entries contains (pointer,raw_word) records. Optional selected_record supplies
    post-comparison accessor reads. Safe witness extents are modern model policy.
    """
    count=signed_count(len(entries) if initial_count is None else initial_count)
    index=0;calls=[];reloads=[]
    while index<count:
        if index>=len(entries) or index>=len(results):
            raise ValueError('Synthetic record/result extent exhausted')
        pointer=entries[index][0]
        calls.append((pointer,search))
        if (results[index]&0xffffffff)==0:
            triple=(family_word,entries[index][0],entries[index][1]) if selected_record is None else tuple(selected_record)
            if len(triple)!=3:raise ValueError('Synthetic selected triple extent invalid')
            return dict(Destination=destination,Words=triple,SelectedIndex=index,Comparisons=calls,CountReloads=reloads,
                        StoreOffsets=(0,4,8),ExplicitDestinationReturn=True,Return=destination,Path='match')
        if reloaded_counts is not None and index>=len(reloaded_counts):
            raise ValueError("Synthetic count reload extent exhausted")
        count=signed_count(count if reloaded_counts is None else reloaded_counts[index])
        reloads.append(count);index+=1
    return dict(Destination=destination,Words=(0,None,0),SelectedIndex=None,Comparisons=calls,CountReloads=reloads,
                StoreOffsets=(0,4,8),ExplicitDestinationReturn=False,Return=None,Path='no-match')


def synthetic_checks():
    dest=object();search=object();a=bytearray(b'A\0');b=bytearray(b'B\0')
    entries=[(a,0x7fc12345),(b,0xffffffff)]
    checks=0
    for results,index in (([0,0],0),([9,0],1),([1,0x100000000],1)):
        got=lookup_witness(0x80000000,entries,search,results,dest)
        assert got['SelectedIndex']==index and got['Words'][1] is entries[index][0] and got['Words'][2]==entries[index][1]
        assert got['Return'] is dest and all(first is entries[j][0] and second is search for j,(first,second) in enumerate(got['Comparisons']))
        checks+=2
    for count in (0,-1,0x80000000,0xffffffff):
        got=lookup_witness(7,entries,search,[],dest,initial_count=count)
        assert not got['Comparisons'] and got['Words']==(0,None,0) and not got['ExplicitDestinationReturn']
        checks+=1
    got=lookup_witness(7,entries,search,[1,2],dest)
    assert got['Words']==(0,None,0) and len(got['Comparisons'])==2 and got['CountReloads']==[2,2]
    checks+=1
    got=lookup_witness(7,entries,search,[1],dest,reloaded_counts=[0])
    assert len(got['Comparisons'])==1 and got['SelectedIndex'] is None
    checks+=1
    got=lookup_witness(7,entries,search,[1,0],dest,initial_count=1,reloaded_counts=[2])
    assert got['SelectedIndex']==1
    checks+=1
    replacement=bytearray(b'changed\0')
    got=lookup_witness(7,entries,search,[0],dest,selected_record=(8,replacement,9))
    assert got['Comparisons'][0][0] is a and got['Words'][1] is replacement and got['Words'][0]==8 and got['Words'][2]==9
    replacement[0]=ord('X');assert got['Words'][1][0]==ord('X')
    checks+=2
    return checks


def verify(executable,packages,asset_root,config,output,maximum_leads=12,maximum_sites=12):
    budget(maximum_leads=maximum_leads,maximum_sites=maximum_sites)
    source,mapping,path=dependency();destination=path.output_path(output)
    data=executable.read_bytes();gate(data);image=mapping.RawPe(data)
    sys.path[:0]=[str(packages),str(REPO/'tools/python')]
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=data,fast_load=True);decoder=Cs(CS_ARCH_X86,CS_MODE_32);decoder.detail=True
    location=image.location(BODY_VA,BODY_BYTES)
    if not location['Executable'] or pe.get_offset_from_rva(BODY_VA-image.base)!=location['FileOffset']:
        raise ValueError('Independent raw PE/pefile mapping mismatch')
    instructions=check_body(image.read,decoder);incoming=incoming_candidates(image);handler=check_discovery(image)
    original=source.original_records(asset_root,config)
    witnesses=[]
    for family in original['Families']:
        entries=[(bytearray(bytes.fromhex(entry['StringHex'])+b'\0'),entry['RawWordBits']) for entry in family['Entries']]
        for index,entry in enumerate(family['Entries']):
            destination_record=object();search=object()
            chosen=lookup_witness(family['FamilyKeyBits'],entries,search,[1]*index+[0],destination_record)
            assert chosen['Words'][1] is entries[index][0] and chosen['Return'] is destination_record
            witnesses.append(dict(FamilyIndex=family['Index'],EntryIndex=index,FamilyWordBits=chosen['Words'][0],EntryWordBits=chosen['Words'][2],
                 StringWithNulSha256=sha(chosen['Words'][1]),StringOffset=entry['Offset'],ComparisonCount=len(chosen['Comparisons']),
                 SuppliedPredicateResults='nonzero prefix then zero',PointerIdentityPreserved=True))
    receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY_VA:08X}',BodyBytes=BODY_BYTES,BodySha256=BODY_SHA,
                 Instructions=len(instructions),Anchors=len(ANCHORS),ExtentWitnesses=2,DirectCallSites=len(CALLS),Branches=len(BRANCHES),Returns=2,
                 SelectedFunctionLeads=5,DiscoveryOnlyLeads=len(DISCOVERY_LEADS),FunctionHandlerLeads=len(LEADS),RawSites=len(SITES),
                 DiscoveryRawAnchors=len(DISCOVERY_ANCHORS),DiscoveryHandlerMetadata=handler,ApprovedLeadCap=12,ApprovedRawSiteCap=12,ExpandedBodies=1,TinyHelpersExpanded=0,
                 InheritedAccessor='00B692A0',FirstUnresolvedComparison='01320590',FirstUnresolvedCallerCleanup='00B69170',
                 MatchEntrySelectedByZeroEax=True,FirstZeroWins=True,CountSigned=True,CountReloadAfterNonzero=True,
                 MatchDestinationExplicitlyReturned=True,NoMatchDestinationZeroTriple=True,NoMatchEaxAfterCleanupEstablished=False,
                 AllSelectedInstructionsReachableFromPrologue=True,CallCandidateInstructionBoundaryProved=True,
                 FunctionBoundaryConfidence='strongly inferred complete frame/control/return/padding extent; no external symbol authority',
                 RawIncomingCandidates=[f'{x:08X}' for x in incoming],IncomingCallerBodyProved=False,
                 SyntheticChecks=synthetic_checks(),OriginalOccurrences=1,OriginalFamilies=15,OriginalEntries=35,OriginalLookupModelWitnesses=len(witnesses),
                 OriginalGroupSha256=original['SourceSha256'],RawPeMapping=location,FrozenPrerequisiteSha256=PREREQUISITE_SHA,
                 PredicateBodyReanalysis=False,AccessorBodyReanalysis=False,OriginalPredicateModeKnown=False,OriginalExecution=False,
                 ProductionResolver=False,StringConversionProved=False,ResourceOpenFetchProved=False)
    path.output_path(destination);destination.mkdir(parents=True)
    (destination/'lookup-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8')
    (destination/'instruction-witnesses.json').write_text(json.dumps([dict(Va=f'{i.address:08X}',Bytes=bytes(i.bytes).hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],indent=2)+'\n',encoding='utf-8')
    (destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','asset-root','config','output'):parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--maximum-leads',type=int,default=12);parser.add_argument('--maximum-sites',type=int,default=12)
    args=parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads,args.maximum_sites),indent=2))
