"""Bounded DWORD family selector and explicit sentinel/ABI consequences.
Static original evidence and safe finite models only; no resolver/original CPU run.
"""
from __future__ import annotations
import argparse,hashlib,importlib.util,json,sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[2]
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
PREREQUISITE='tools/python/verify_terrain_shader_family_lookup_consumer.py'
PREREQUISITE_SHA='6ccca478aaa8bd9d763e21340be3db5aa3a604a9655f5a22ad79d54eeb5b08f0'
BODY_VA,BODY_BYTES,BODY_SHA=0x00B69860,59,'7b4417782216005f8e6d7c41d944f732cb8391094db9dbbe030724db9149968a'
BEFORE_VA,BEFORE_HEX=0x00B69850,'c2040090909090909090909090909090'
AFTER_VA,AFTER_HEX=0x00B6989B,'9090909090'
CALLS={0x00B69877:0x00B69440}
BRANCHES={0x00B6986D:('jle',0x00B69888),0x00B6987E:('je',0x00B69892),0x00B69886:('jl',0x00B69875)}
RETURNS=(0x00B6988F,0x00B69898)
LEADS=(BODY_VA,0x00B69440)
SITES=(*CALLS,*BRANCHES,*RETURNS,BEFORE_VA,AFTER_VA)
HELPER_APPROVED=True
HELPER_VA,HELPER_BYTES,HELPER_SHA=0x00B69440,3,'19d200194d49b34530548575f37a1bb4203ee60f18d42f0084e86b7a8c899faa'
HELPER_BEFORE_VA,HELPER_BEFORE_HEX=0x00B69439,'5dc20400909090'
HELPER_AFTER_VA,HELPER_AFTER_HEX=0x00B69443,'90909090909090909090909090'
SITES=(*SITES,0x00B69442,HELPER_BEFORE_VA,HELPER_AFTER_VA)
ANCHORS = {
    0x00B69440: ('8b01', 'mov', 'eax, dword ptr [ecx]'),
    0x00B69442: ('c3', 'ret', ''),

    0x00B69860: ('55', 'push', 'ebp'),
    0x00B69861: ('8bec', 'mov', 'ebp, esp'),
    0x00B69863: ('53', 'push', 'ebx'),
    0x00B69864: ('56', 'push', 'esi'),
    0x00B69865: ('57', 'push', 'edi'),
    0x00B69866: ('8b7908', 'mov', 'edi, dword ptr [ecx + 8]'),
    0x00B69869: ('33d2', 'xor', 'edx, edx'),
    0x00B6986B: ('85ff', 'test', 'edi, edi'),
    0x00B6986D: ('7e19', 'jle', '0xb69888'),
    0x00B6986F: ('8b7104', 'mov', 'esi, dword ptr [ecx + 4]'),
    0x00B69872: ('8b5d08', 'mov', 'ebx, dword ptr [ebp + 8]'),
    0x00B69875: ('8b0e', 'mov', 'ecx, dword ptr [esi]'),
    0x00B69877: ('e8c4fbffff', 'call', '0xb69440'),
    0x00B6987C: ('3bc3', 'cmp', 'eax, ebx'),
    0x00B6987E: ('7412', 'je', '0xb69892'),
    0x00B69880: ('42', 'inc', 'edx'),
    0x00B69881: ('83c604', 'add', 'esi, 4'),
    0x00B69884: ('3bd7', 'cmp', 'edx, edi'),
    0x00B69886: ('7ced', 'jl', '0xb69875'),
    0x00B69888: ('5f', 'pop', 'edi'),
    0x00B69889: ('5e', 'pop', 'esi'),
    0x00B6988A: ('83c8ff', 'or', 'eax, 0xffffffff'),
    0x00B6988D: ('5b', 'pop', 'ebx'),
    0x00B6988E: ('5d', 'pop', 'ebp'),
    0x00B6988F: ('c20400', 'ret', '4'),
    0x00B69892: ('5f', 'pop', 'edi'),
    0x00B69893: ('5e', 'pop', 'esi'),
    0x00B69894: ('8bc2', 'mov', 'eax, edx'),
    0x00B69896: ('5b', 'pop', 'ebx'),
    0x00B69897: ('5d', 'pop', 'ebp'),
    0x00B69898: ('c20400', 'ret', '4'),
}
def sha(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data)!=EXE_SHA:
        raise ValueError('Executable identity mismatch before VA inspection')


def sealed_gate(data):
    if sha(data)!=PREREQUISITE_SHA:
        raise ValueError('Frozen68 prerequisite identity mismatch')


def dependency():
    file=REPO/PREREQUISITE
    sealed_gate(file.read_bytes())
    spec=importlib.util.spec_from_file_location('selector68',file)
    source=importlib.util.module_from_spec(spec);spec.loader.exec_module(source)
    lookup,records,mapping,path=source.dependency()
    return source,records,mapping,path


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
    if read(BEFORE_VA,16).hex()!=BEFORE_HEX or read(AFTER_VA,5).hex()!=AFTER_HEX:
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
            if item.op_str!='4':
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


def check_helper(read,decoder):
    raw=read(HELPER_VA,HELPER_BYTES)
    if len(raw)!=HELPER_BYTES or sha(raw)!=HELPER_SHA:
        raise ValueError('Approved complete tiny getter mismatch')
    items=list(decoder.disasm(raw,HELPER_VA))
    if len(items)!=2 or sum(i.size for i in items)!=3 or items[-1].mnemonic!='ret' or items[-1].op_str:
        raise ValueError('Approved tiny getter return/extent mismatch')
    if read(HELPER_BEFORE_VA,7).hex()!=HELPER_BEFORE_HEX or read(HELPER_AFTER_VA,13).hex()!=HELPER_AFTER_HEX:
        raise ValueError('Approved tiny getter fences mismatch')
    return items


def check_body(read,decoder):
    raw=read(BODY_VA,BODY_BYTES)
    if len(raw)!=BODY_BYTES or sha(raw)!=BODY_SHA:
        raise ValueError('Selected complete caller mismatch')
    items=list(decoder.disasm(raw,BODY_VA))
    if len(items)!=31 or sum(i.size for i in items)!=BODY_BYTES or items[-1].address!=RETURNS[-1]:
        raise ValueError('Selected complete body extent mismatch')
    check_extent(read);check_helper(read,decoder);check_anchors(read,decoder);check_control(items)
    return items


def signed_count(word):
    if type(word) is not int:raise ValueError('Synthetic count must be integer')
    value=word&0xffffffff
    return value-(1<<32) if value&0x80000000 else value


def selector_witness(word_reads,input_word,count=None,*,nonvolatile=None,pending=None):
    """Finite DWORD getter-read model; no original reads or malformed accesses.

    Saved nonvolatile and pending identity are asserted only by final approved
    getter/selector evidence. Finite safe extents are modern witness policy.
    """
    captured=signed_count(len(word_reads) if count is None else count)
    reads=[]
    if captured>0:
        if type(input_word) is not int:raise ValueError('Synthetic positive-count input must be integer')
        key=input_word&0xffffffff
        for index in range(captured):
            if index>=len(word_reads):raise ValueError('Synthetic family read extent exhausted')
            value=word_reads[index]
            if type(value) is not int:raise ValueError('Synthetic getter word must be integer')
            reads.append(value&0xffffffff)
            if (value&0xffffffff)==key:
                return dict(Result=index,GetterReads=reads,CapturedCount=captured,InputRead=True,
                            StackCleanupBytes=4,Nonvolatile=nonvolatile,Pending=pending)
    return dict(Result=0xffffffff,GetterReads=reads,CapturedCount=captured,InputRead=captured>0,
                StackCleanupBytes=4,Nonvolatile=nonvolatile,Pending=pending)


def caller_array_address(array_base,index_word):
    """x86 address arithmetic only; never dereferences an invalid sentinel."""
    return (array_base+(index_word&0xffffffff)*4)&0xffffffff


def synthetic_checks():
    checks=0
    registers=object();pending=(object(),object())
    for values,key,wanted in (([0,1,0xffffffff],0,0),([0,1,0xffffffff],1,1),([0,1,0xffffffff],-1,2),
                              ([7,7,8],7,0),([1,2],3,0xffffffff),([0x80000000,0x7fc12345],0x80000000,0),
                              ([0x80000000,0x7fc12345],0x7fc12345,1)):
        result=selector_witness(values,key,nonvolatile=registers,pending=pending)
        assert result['Result']==wanted and result['Nonvolatile'] is registers and result['Pending'] is pending
        assert result['StackCleanupBytes']==4
        checks+=2
    for count in (0,-1,0x80000000,0xffffffff):
        result=selector_witness([],None,count)
        assert result['Result']==0xffffffff and not result['GetterReads'] and not result['InputRead']
        checks+=1
    for base,index,wanted in ((0x1000,0,0x1000),(0x1000,2,0x1008),(0x1000,0xffffffff,0xffc),(0,0xffffffff,0xfffffffc)):
        assert caller_array_address(base,index)==wanted
        checks+=1
    return checks


def verify(executable,packages,asset_root,config,output,maximum_leads=12,maximum_sites=12):
    budget(maximum_leads=maximum_leads,maximum_sites=maximum_sites)
    source,records,mapping,path=dependency();destination=path.output_path(output)
    data=executable.read_bytes();gate(data);image=mapping.RawPe(data)
    sys.path[:0]=[str(packages),str(REPO/'tools/python')]
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=data,fast_load=True);decoder=Cs(CS_ARCH_X86,CS_MODE_32);decoder.detail=True
    location=image.location(BODY_VA,BODY_BYTES)
    if not location['Executable'] or pe.get_offset_from_rva(BODY_VA-image.base)!=location['FileOffset']:
        raise ValueError('Independent raw PE/pefile mapping mismatch')
    helper_location=image.location(HELPER_VA,HELPER_BYTES)
    if not helper_location['Executable'] or pe.get_offset_from_rva(HELPER_VA-image.base)!=helper_location['FileOffset']:
        raise ValueError('Independent tiny getter PE mapping mismatch')
    instructions=check_body(image.read,decoder);helper=check_helper(image.read,decoder)
    original=records.original_records(asset_root,config)
    words=[f['FamilyKeyBits'] for f in original['Families']];pending=(object(),object());registers=object()
    witnesses=[]
    for index,word in enumerate(words):
        result=selector_witness(words,word,nonvolatile=registers,pending=pending)
        expected=words.index(word)
        assert result['Result']==expected and result['Pending'] is pending and result['Nonvolatile'] is registers
        witnesses.append(dict(InputFamilyIndex=index,InputWordBits=word,SelectedIndex=result['Result'],FirstDuplicateIndex=expected,
                              GetterReads=len(result['GetterReads']),SuppliedStableFields=True,StackCleanupBytes=4,PendingIdentityPreserved=True))
    receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),BodyVa=f'{BODY_VA:08X}',BodyBytes=BODY_BYTES,BodySha256=BODY_SHA,
                 Instructions=len(instructions),HelperInstructions=len(helper),TotalVerifiedInstructions=len(instructions)+len(helper),HelperBytes=HELPER_BYTES,HelperSha256=HELPER_SHA,Anchors=len(ANCHORS),ExtentWitnesses=4,DirectCallSites=len(CALLS),Branches=3,Returns=2,
                 FunctionHandlerLeads=len(LEADS),RawSites=len(SITES),ApprovedLeadCap=12,ApprovedRawSiteCap=12,ExpandedBodies=1,TinyHelpersExpanded=int(HELPER_APPROVED),
                 SoleGetter='00B69440',FamilyFieldOffset=0,GetterEdxUnchanged=True,GetterNonvolatileUnchanged=True,InputWidthBits=32,Equality='full DWORD equality',CountOffset=8,CountSigned=True,ArrayOffset=4,ArrayStride=4,
                 CountAndBaseCapturedOnce=True,FirstEqualReturnsIndex=True,FailureResultBits='FFFFFFFF',StackCleanupBytes=4,
                 SelectorNonvolatileSaveRestoreObserved=True,PendingSlotsNeverWrittenBySelector=True,
                 GetterFieldAndEdxPreservationEstablished=HELPER_APPROVED,NormalReadablePathAbiClosureEstablished=HELPER_APPROVED,
                 WrapperSentinelIndexChecked=False,SentinelAddressEffect='array_base minus4 modulo32',
                 FunctionBoundaryConfidence='strongly inferred complete frame/control/returns/padding extent; no external symbol authority',
                 SyntheticChecks=synthetic_checks(),OriginalOccurrences=1,OriginalFamilies=15,OriginalEntries=35,OriginalSelectorModelWitnesses=len(witnesses),
                 OriginalDistinctFamilyWords=len(set(words)),OriginalGroupSha256=original['SourceSha256'],RawPeMapping=location,HelperRawPeMapping=helper_location,FrozenPrerequisiteSha256=PREREQUISITE_SHA,
                 OriginalExecution=False,PriorBodySuiteReanalysis=False,ResourceIdentityProved=False,ProductionResolver=False)
    path.output_path(destination);destination.mkdir(parents=True)
    (destination/'selector-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8')
    (destination/'instruction-witnesses.json').write_text(json.dumps([dict(Va=f'{i.address:08X}',Bytes=bytes(i.bytes).hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],indent=2)+'\n',encoding='utf-8')
    (destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','asset-root','config','output'):parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--maximum-leads',type=int,default=12);parser.add_argument('--maximum-sites',type=int,default=12)
    args=parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads,args.maximum_sites),indent=2))
