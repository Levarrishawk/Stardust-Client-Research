"""One complete second-string caller and unresolved structured-output forwards.

Static identity/PE/operand/control evidence only. No production serializer,
resolver, original process execution or unproved resource-name interpretation.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

REPO=Path(__file__).resolve().parents[2]
EXE_SHA='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
PREREQUISITE='tools/python/verify_terrain_shader_family_second_string.py'
PREREQUISITE_SHA='755047cdf6e860f4acdf13954a07cb4b993b4ce5f1e4cb56c885b6335734cc38'
BODY_VA,BODY_BYTES=0x00B6AD70,464
BODY_SHA='5466348f6df3dc7feb7b240b0c5adb698469854692655b7403c267038697ef5b'
GETTER_VA,GETTER_SHA=0x00B69370,'01d3abacb68aac3537badfe563d0e21d0244fb61db1d805bcef8f455407fad96'
SEH_SITE,SEH_TARGET=0x00B6AD75,0x014D2508
FALLBACK=0x01908090
# Superseding lead amendment:18 function/handler leads and29 raw sites,
# including unexpanded SEH target/reference; no target-body expansion.
APPROVED_LEADS,APPROVED_SITES=18,29
CALLS = {
    0x00B6AD9E: 0x00A947A0,
    0x00B6ADAC: 0x00A947A0,
    0x00B6ADB3: 0x00B69B10,
    0x00B6ADE2: 0x00A94810,
    0x00B6ADE9: 0x00B69440,
    0x00B6ADF9: 0x00A94870,
    0x00B6AE00: 0x00B69300,
    0x00B6AE08: 0x00A949D0,
    0x00B6AE0F: 0x00B69370,
    0x00B6AE18: 0x00B69370,
    0x00B6AE27: 0x00A949D0,
    0x00B6AE2E: 0x00B693F0,
    0x00B6AE3A: 0x00A94870,
    0x00B6AE47: 0x00A94870,
    0x00B6AE54: 0x00A94870,
    0x00B6AE5B: 0x00B69420,
    0x00B6AE6B: 0x00A94870,
    0x00B6AE72: 0x00B6B410,
    0x00B6AE82: 0x00A94870,
    0x00B6AE8E: 0x00B69B20,
    0x00B6AE9E: 0x00A94870,
    0x00B6AEB9: 0x00B69B40,
    0x00B6AECB: 0x00A949D0,
    0x00B6AED8: 0x00A94870,
    0x00B6AEE7: 0x00B69170,
    0x00B6AEFD: 0x00A94D90,
    0x00B6AF1B: 0x00A94CC0,
    0x00B6AF29: 0x00A94CC0,
}
BRANCHES = {
    0x00B6ADC4: ("jle", 0x00B6AF12),
    0x00B6ADCB: ("jmp", 0x00B6ADD0),
    0x00B6AE16: ("je", 0x00B6AE1F),
    0x00B6AE1D: ("jmp", 0x00B6AE24),
    0x00B6AEAA: ("jle", 0x00B6AEF4),
    0x00B6AEF2: ("jl", 0x00B6AEB0),
    0x00B6AF0B: ("jl", 0x00B6ADCD),
}
LEADS = (BODY_VA,*sorted(set(CALLS.values())),SEH_TARGET)
SITES = (*CALLS,SEH_SITE)
ANCHORS = {
    0x00B6AD8C: ("8b7508", "mov", "esi, dword ptr [ebp + 8]"),
    0x00B6AD90: ("8bf9", "mov", "edi, ecx"),
    0x00B6AD92: ("6a01", "push", "1"),
    0x00B6AD94: ("6850524753", "push", "0x53475250"),
    0x00B6AD99: ("8bce", "mov", "ecx, esi"),
    0x00B6AD9E: ("e8fd99f2ff", "call", "0xa947a0"),
    0x00B6ADA3: ("6a01", "push", "1"),
    0x00B6ADA5: ("6836303030", "push", "0x30303036"),
    0x00B6ADAC: ("e8ef99f2ff", "call", "0xa947a0"),
    0x00B6ADB3: ("e858edffff", "call", "0xb69b10"),
    0x00B6ADC4: ("0f8e48010000", "jle", "0xb6af12"),
    0x00B6ADCB: ("eb03", "jmp", "0xb6add0"),
    0x00B6ADD0: ("8b4704", "mov", "eax, dword ptr [edi + 4]"),
    0x00B6ADD3: ("8b4dec", "mov", "ecx, dword ptr [ebp - 0x14]"),
    0x00B6ADD6: ("8b3c88", "mov", "edi, dword ptr [eax + ecx*4]"),
    0x00B6ADD9: ("6a01", "push", "1"),
    0x00B6ADDB: ("684d414653", "push", "0x5346414d"),
    0x00B6ADE0: ("8bce", "mov", "ecx, esi"),
    0x00B6ADE2: ("e8299af2ff", "call", "0xa94810"),
    0x00B6ADE9: ("e852e6ffff", "call", "0xb69440"),
    0x00B6ADF9: ("e8729af2ff", "call", "0xa94870"),
    0x00B6ADFE: ("8bcf", "mov", "ecx, edi"),
    0x00B6AE00: ("e8fbe4ffff", "call", "0xb69300"),
    0x00B6AE05: ("50", "push", "eax"),
    0x00B6AE06: ("8bce", "mov", "ecx, esi"),
    0x00B6AE08: ("e8c39bf2ff", "call", "0xa949d0"),
    0x00B6AE0D: ("8bcf", "mov", "ecx, edi"),
    0x00B6AE0F: ("e85ce5ffff", "call", "0xb69370"),
    0x00B6AE14: ("85c0", "test", "eax, eax"),
    0x00B6AE16: ("7407", "je", "0xb6ae1f"),
    0x00B6AE18: ("e853e5ffff", "call", "0xb69370"),
    0x00B6AE1D: ("eb05", "jmp", "0xb6ae24"),
    0x00B6AE1F: ("b890809001", "mov", "eax, 0x1908090"),
    0x00B6AE24: ("50", "push", "eax"),
    0x00B6AE25: ("8bce", "mov", "ecx, esi"),
    0x00B6AE27: ("e8a49bf2ff", "call", "0xa949d0"),
    0x00B6AE2E: ("e8bde5ffff", "call", "0xb693f0"),
    0x00B6AE33: ("8bd8", "mov", "ebx, eax"),
    0x00B6AE35: ("6a01", "push", "1"),
    0x00B6AE37: ("53", "push", "ebx"),
    0x00B6AE3A: ("e8319af2ff", "call", "0xa94870"),
    0x00B6AE3F: ("8d4301", "lea", "eax, [ebx + 1]"),
    0x00B6AE42: ("6a01", "push", "1"),
    0x00B6AE44: ("50", "push", "eax"),
    0x00B6AE47: ("e8249af2ff", "call", "0xa94870"),
    0x00B6AE4C: ("6a01", "push", "1"),
    0x00B6AE4E: ("83c302", "add", "ebx, 2"),
    0x00B6AE51: ("53", "push", "ebx"),
    0x00B6AE54: ("e8179af2ff", "call", "0xa94870"),
    0x00B6AE5B: ("e8c0e5ffff", "call", "0xb69420"),
    0x00B6AE60: ("d95de8", "fstp", "dword ptr [ebp - 0x18]"),
    0x00B6AE6B: ("e8009af2ff", "call", "0xa94870"),
    0x00B6AE72: ("e899050000", "call", "0xb6b410"),
    0x00B6AE77: ("d95de4", "fstp", "dword ptr [ebp - 0x1c]"),
    0x00B6AE82: ("e8e999f2ff", "call", "0xa94870"),
    0x00B6AE8E: ("e88decffff", "call", "0xb69b20"),
    0x00B6AE93: ("6a04", "push", "4"),
    0x00B6AE9B: ("894508", "mov", "dword ptr [ebp + 8], eax"),
    0x00B6AE9E: ("e8cd99f2ff", "call", "0xa94870"),
    0x00B6AEAA: ("7e48", "jle", "0xb6aef4"),
    0x00B6AEB9: ("e882ecffff", "call", "0xb69b40"),
    0x00B6AEC1: ("51", "push", "ecx"),
    0x00B6AEC2: ("8bce", "mov", "ecx, esi"),
    0x00B6AECB: ("e8009bf2ff", "call", "0xa949d0"),
    0x00B6AED0: ("6a04", "push", "4"),
    0x00B6AED8: ("e89399f2ff", "call", "0xa94870"),
    0x00B6AEE7: ("e884e2ffff", "call", "0xb69170"),
    0x00B6AEF2: ("7cbc", "jl", "0xb6aeb0"),
    0x00B6AEFD: ("e88e9ef2ff", "call", "0xa94d90"),
    0x00B6AF0B: ("0f8cbcfeffff", "jl", "0xb6adcd"),
    0x00B6AF1B: ("e8a09df2ff", "call", "0xa94cc0"),
    0x00B6AF29: ("e8929df2ff", "call", "0xa94cc0"),
    0x00B6AF3D: ("c20400", "ret", "4"),
    0x00B6AD75: ("6808254d01", "push", "0x14d2508"),
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data)!=EXE_SHA:
        raise ValueError('Executable identity mismatch before VA inspection')


def sealed_gate(data):
    if sha(data)!=PREREQUISITE_SHA:
        raise ValueError('Frozen64 prerequisite changed')


def dependency():
    source=REPO/PREREQUISITE
    sealed_gate(source.read_bytes())
    spec=importlib.util.spec_from_file_location('second_caller64',source)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    mapping,path=module.dependencies()
    return module,mapping,path


def budget(leads=LEADS,sites=SITES,maximum_leads=None,maximum_sites=None):
    maximum_leads=APPROVED_LEADS if maximum_leads is None else maximum_leads
    maximum_sites=APPROVED_SITES if maximum_sites is None else maximum_sites
    if type(maximum_leads) is not int or not 0<=maximum_leads<=APPROVED_LEADS:
        raise ValueError('Function/handler lead ceiling invalid')
    if type(maximum_sites) is not int or not 0<=maximum_sites<=APPROVED_SITES:
        raise ValueError('Raw site ceiling invalid')
    if len(set(leads))!=len(leads) or len(set(sites))!=len(sites):
        raise ValueError('Duplicate function/handler lead or raw site')
    if len(leads)>maximum_leads or len(sites)>maximum_sites:
        raise ValueError('Complete selected inventory exceeds approved budget')
    if tuple(leads)!=LEADS or tuple(sites)!=SITES:
        raise ValueError('Unassigned function/handler/site')


def check_anchors(read,decoder):
    for va,(encoded,mnemonic,operands) in ANCHORS.items():
        raw=read(va,len(bytes.fromhex(encoded)))
        if raw.hex()!=encoded:
            raise ValueError(f'Raw witness mismatch at {va:08X}')
        decoded=list(decoder.disasm(raw,va))
        if len(decoded)!=1 or (decoded[0].mnemonic,decoded[0].op_str)!=(mnemonic,operands):
            raise ValueError(f'Operand witness mismatch at {va:08X}')


def check_control(instructions):
    from capstone.x86 import X86_OP_IMM
    addresses={i.address for i in instructions}
    calls,branches={},{}
    for item in instructions:
        if item.mnemonic=='call' or item.mnemonic.startswith('j'):
            if len(item.operands)!=1 or item.operands[0].type!=X86_OP_IMM:
                raise ValueError('Unassigned indirect control edge')
            target=item.operands[0].imm & 0xFFFFFFFF
            if item.mnemonic=='call':
                calls[item.address]=target
            else:
                if target not in addresses:
                    raise ValueError('Branch misses a selected instruction boundary')
                branches[item.address]=(item.mnemonic,target)
    if calls!=CALLS or branches!=BRANCHES:
        raise ValueError('Complete call/branch inventory mismatch')


def check_body(read,decoder):
    raw=read(BODY_VA,BODY_BYTES)
    if len(raw)!=BODY_BYTES or sha(raw)!=BODY_SHA:
        raise ValueError('Selected complete caller body mismatch')
    instructions=list(decoder.disasm(raw,BODY_VA))
    if len(instructions)!=156 or sum(i.size for i in instructions)!=BODY_BYTES or instructions[-1].mnemonic!='ret':
        raise ValueError('Selected body/return extent mismatch')
    check_anchors(read,decoder)
    check_control(instructions)
    if sha(read(GETTER_VA,4))!=GETTER_SHA:
        raise ValueError('Accepted64 getter prerequisite bytes differ')
    return instructions


def forward_witness(first_read,second_read,fallback,destination):
    """Synthetic supplied read results; no original memory or write execution."""
    if first_read is None:
        return dict(GetterCalls=1,Argument=fallback,Receiver=destination,Fallback=True)
    return dict(GetterCalls=2,Argument=second_read,Receiver=destination,Fallback=False)


def synthetic_checks():
    destination=object()
    fallback=object()
    a,b=bytearray(b'A/\\\xff\0tail'),bytearray(b'different\0')
    vectors=[(None,b,1,fallback,True),(a,a,2,a,False),(a,b,2,b,False),(a,None,2,None,False),
             (bytearray(b'\0'),a,2,a,False)]
    for first,second,calls,argument,chosen in vectors:
        result=forward_witness(first,second,fallback,destination)
        assert result['GetterCalls']==calls and result['Argument'] is argument
        assert result['Receiver'] is destination and result['Fallback']==chosen
    return len(vectors)*2


def verify(executable,packages,asset_root,config,output,maximum_leads=None,maximum_sites=None):
    budget(maximum_leads=maximum_leads,maximum_sites=maximum_sites)
    source,mapping,path=dependency()
    destination=path.output_path(output)
    data=executable.read_bytes()
    gate(data)
    image=mapping.RawPe(data)
    sys.path[:0]=[str(packages),str(REPO/'tools/python')]
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=data,fast_load=True)
    location=image.location(BODY_VA,BODY_BYTES)
    if not location['Executable'] or pe.get_offset_from_rva(BODY_VA-image.base)!=location['FileOffset']:
        raise ValueError('Independent PE mapping mismatch')
    decoder=Cs(CS_ARCH_X86,CS_MODE_32)
    decoder.detail=True
    instructions=check_body(image.read,decoder)
    fallback_location=image.location(FALLBACK,1)
    seh_location=image.location(SEH_TARGET,1)
    if fallback_location['FileBacked'] or not seh_location['Executable']:
        raise ValueError('Fallback/SEH location classification mismatch')
    rows,entries=source.original_second_strings(asset_root,config)
    witnesses=[]
    for row in rows:
        raw=bytearray(bytes.fromhex(row['SecondHex']))
        receiver=object()
        fallback=object()
        chosen=forward_witness(raw,raw,fallback,receiver)
        assert chosen['Argument'] is raw and chosen['Receiver'] is receiver
        witnesses.append(dict(Index=row['Index'],SecondOffset=row['SecondOffset'],WithNulSha256=sha(raw),
                              SuppliedReads='same non-null object',GetterCalls=2,ForwardedBytesSha256=sha(chosen['Argument'])))
    checks=synthetic_checks()
    receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),Caller='00B6AD70',BodyBytes=BODY_BYTES,BodySha256=BODY_SHA,
                 Instructions=len(instructions),Anchors=len(ANCHORS),DirectCallSites=len(CALLS),DistinctDirectCallees=len(set(CALLS.values())),
                 DirectFunctionLeads=len(LEADS)-1,FunctionAndHandlerLeads=len(LEADS),RawDirectCallSites=len(CALLS),SehReferences=1,
                 TotalDistinctCodeAddressesIncludingSeh=len(LEADS),TotalCallAndCompilerReferenceSites=len(SITES),
                 ApprovedLeadCap=APPROVED_LEADS,ApprovedRawSiteCap=APPROVED_SITES,Branches=len(BRANCHES),
                 ExpandedBodies=1,TinyHelpersExpanded=0,Getter='00B69370',StringForward='00A949D0',StringForwardSite='00B6AE27',
                 FirstBodyForward='00A947A0',NullFallbackAddress='01908090',NullCheckedRead=1,NonNullBranchReads=2,
                 StringTransformProved=False,ResourceOpenFetchProved=False,Classification='structured-output/serialization strongly inferred; delegates unexpanded',
                 ByteOutputEncodingProved=False,RuntimeReachabilityProved=False,FallbackRuntimeContentsEstablished=False,
                 OriginalOccurrences=1,OriginalFamilies=len(rows),OriginalEntries=entries,OriginalStringForwardModelWitnesses=len(witnesses),
                 OriginalGroupSha256='C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628',
                 SyntheticChecks=checks,FallbackLocation=fallback_location,SehLocation=seh_location,RawPeMapping=location,
                 OriginalExecution=False,ProductionResolver=False,FrozenPrerequisiteSha256=PREREQUISITE_SHA)
    path.output_path(destination)
    destination.mkdir(parents=True)
    (destination/'string-forward-witnesses.json').write_text(json.dumps(witnesses,indent=2)+'\n',encoding='utf-8')
    (destination/'instruction-witnesses.json').write_text(json.dumps([dict(Va=f'{i.address:08X}',Bytes=bytes(i.bytes).hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],indent=2)+'\n',encoding='utf-8')
    (destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','asset-root','config','output'):
        parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--maximum-leads',type=int)
    parser.add_argument('--maximum-sites',type=int)
    args=parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads,args.maximum_sites),indent=2))
