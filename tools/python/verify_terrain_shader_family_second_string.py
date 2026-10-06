"""Bounded static second-SFAM-string copy/getter/consumer evidence.

No resolver, original execution or inferred filename semantics. The selected
consumer stops at an unresolved index selector and returns an unchanged pointer.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import sys

REPO = Path(__file__).resolve().parents[2]
EXE_SHA = "e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150"
SEALED = {
    "tools/python/verify_terrain_shader_family_predicate.py":"8d5a1c0589565eaabb88bcdd0f3b1cc7ff54bc7ce041604cdc3d2d5841d97c5d",
    "tools/python/verify_terrain_shader_family_consumer.py":"952d30a74b8218eedc597b07ad3ac641f346a4ffe5fce4e40e49a75f9549cdd3",
}
BODIES = {
    "Setter":(0x00B69390,90,"4723759e4264a51dd5b4e1cc1422a49cd40984490c8810823613b32e6f62637d",41),
    "Getter":(0x00B69370,4,"01d3abacb68aac3537badfe563d0e21d0244fb61db1d805bcef8f455407fad96",2),
    "Consumer":(0x00B6B480,31,"544beabfd4ccb4dc944c9d35c048a6f504ac1bd83491475990a38bc9ea10ad44",13),
}
CALLS = {0x00B6939F:0x00AC1660,0x00B693C6:0x00AC15E0,
         0x00B6B48A:0x00B69860,0x00B6B495:0x00B69370}
BRANCHES = {0x00B6939C:("je",0x00B693AE),0x00B693B3:("je",0x00B693E4)}
LEADS = (0x00B69390,0x00B69370,0x00B6B480,0x00AC1660,0x00AC15E0,0x00B69860)
SITES = (*CALLS,0x00B6AE0F,0x00B6AE18)
ANCHORS = {
    0x00B69394: ("8bd9", "mov", "ebx, ecx"),
    0x00B69396: ("8b4314", "mov", "eax, dword ptr [ebx + 0x14]"),
    0x00B69399: ("85c0", "test", "eax, eax"),
    0x00B6939C: ("7410", "je", "0xb693ae"),
    0x00B6939F: ("e8bc82f5ff", "call", "0xac1660"),
    0x00B693A7: ("c7431400000000", "mov", "dword ptr [ebx + 0x14], 0"),
    0x00B693AE: ("8b7508", "mov", "esi, dword ptr [ebp + 8]"),
    0x00B693B1: ("85f6", "test", "esi, esi"),
    0x00B693B3: ("742f", "je", "0xb693e4"),
    0x00B693BD: ("f2ae", "repne scasb", "al, byte ptr es:[edi]"),
    0x00B693C1: ("49", "dec", "ecx"),
    0x00B693C4: ("47", "inc", "edi"),
    0x00B693C5: ("57", "push", "edi"),
    0x00B693C6: ("e81582f5ff", "call", "0xac15e0"),
    0x00B693D4: ("f3a5", "rep movsd", "dword ptr es:[edi], dword ptr [esi]"),
    0x00B693DE: ("f3a4", "rep movsb", "byte ptr es:[edi], byte ptr [esi]"),
    0x00B693E0: ("894314", "mov", "dword ptr [ebx + 0x14], eax"),
    0x00B693E7: ("c20400", "ret", "4"),
    0x00B69370: ("8b4114", "mov", "eax, dword ptr [ecx + 0x14]"),
    0x00B69373: ("c3", "ret", ""),
    0x00B6B480: ("55", "push", "ebp"),
    0x00B6B481: ("8bec", "mov", "ebp, esp"),
    0x00B6B483: ("8b4508", "mov", "eax, dword ptr [ebp + 8]"),
    0x00B6B486: ("56", "push", "esi"),
    0x00B6B487: ("50", "push", "eax"),
    0x00B6B488: ("8bf1", "mov", "esi, ecx"),
    0x00B6B48A: ("e8d1e3ffff", "call", "0xb69860"),
    0x00B6B48F: ("8b4e04", "mov", "ecx, dword ptr [esi + 4]"),
    0x00B6B492: ("8b0c81", "mov", "ecx, dword ptr [ecx + eax*4]"),
    0x00B6B495: ("e8d6deffff", "call", "0xb69370"),
    0x00B6B49A: ("5e", "pop", "esi"),
    0x00B6B49B: ("5d", "pop", "ebp"),
    0x00B6B49C: ("c20400", "ret", "4"),
    0x00B6ABAF: ("68e8030000", "push", "0x3e8"),
    0x00B6ABB4: ("8d8dd0fbffff", "lea", "ecx, [ebp - 0x430]"),
    0x00B6ABBA: ("51", "push", "ecx"),
    0x00B6ABBB: ("8bcb", "mov", "ecx, ebx"),
    0x00B6ABBD: ("e8eea5f2ff", "call", "0xa951b0"),
    0x00B6ABC2: ("8d95d0fbffff", "lea", "edx, [ebp - 0x430]"),
    0x00B6ABC8: ("52", "push", "edx"),
    0x00B6ABC9: ("8bce", "mov", "ecx, esi"),
    0x00B6ABCB: ("e8c0e7ffff", "call", "0xb69390"),
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data) != EXE_SHA:
        raise ValueError("Original executable identity mismatch before VA inspection")


def sealed_gate(name,data):
    if name not in SEALED or sha(data) != SEALED[name]:
        raise ValueError("Frozen prerequisite source identity mismatch")


def dependencies():
    modules = []
    for name in SEALED:
        sealed_gate(name,(REPO/name).read_bytes())
        spec = importlib.util.spec_from_file_location("second_string_"+Path(name).stem,REPO/name)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        modules.append(module)
    # Reuse mapping/path machinery only; no first-string or predicate analysis.
    return modules[0],modules[1]


def budget(leads=LEADS,sites=SITES,maximum=12):
    if type(maximum) is not int or not 0 <= maximum <= 12:
        raise ValueError("Budget must be integer 0..12")
    if len(set(leads)) != len(leads) or len(set(sites)) != len(sites) or len(leads)>maximum or len(sites)>maximum:
        raise ValueError("Duplicate or exceeded function/site budget")
    if tuple(leads)!=LEADS or tuple(sites)!=SITES:
        raise ValueError("Unassigned function/site")


def check_anchors(read,decoder):
    for va,(encoded,mnemonic,operands) in ANCHORS.items():
        raw = read(va,len(bytes.fromhex(encoded)))
        if raw.hex()!=encoded:
            raise ValueError(f"Raw witness mismatch at {va:08X}")
        decoded = list(decoder.disasm(raw,va))
        if len(decoded)!=1 or (decoded[0].mnemonic,decoded[0].op_str)!=(mnemonic,operands):
            raise ValueError(f"Register/operand witness mismatch at {va:08X}")


def check_control(instructions):
    from capstone.x86 import X86_OP_IMM
    calls,branches = {},{}
    addresses = {i.address for i in instructions}
    for item in instructions:
        if item.mnemonic=='call' or item.mnemonic.startswith('j'):
            if len(item.operands)!=1 or item.operands[0].type!=X86_OP_IMM:
                raise ValueError("Unassigned indirect edge")
            target = item.operands[0].imm & 0xFFFFFFFF
            if item.mnemonic=='call':
                calls[item.address]=target
            else:
                if target not in addresses:
                    raise ValueError("Branch target is not a selected instruction boundary")
                branches[item.address]=(item.mnemonic,target)
    if calls!=CALLS or branches!=BRANCHES:
        raise ValueError("Selected call/branch set mismatch")


def check_bodies(read,decoder):
    all_instructions = []
    for name,(va,size,expected,count) in BODIES.items():
        raw = read(va,size)
        if len(raw)!=size or sha(raw)!=expected:
            raise ValueError("Bounded complete body mismatch: "+name)
        decoded = list(decoder.disasm(raw,va))
        if len(decoded)!=count or sum(i.size for i in decoded)!=size or decoded[-1].mnemonic!='ret':
            raise ValueError("Body instruction/return extent mismatch")
        all_instructions.extend(decoded)
    check_anchors(read,decoder)
    check_control(all_instructions)
    return all_instructions


def direct_candidates(image,target):
    """Raw rel32 discovery; does not prove alignment, reachability or callers."""
    hits = []
    for _,rva,_,raw_at,raw_size,flags in image.sections:
        if not flags & 0x20000000:
            continue
        raw = image.data[raw_at:raw_at+raw_size]
        for opcode in (b'\xe8',b'\xe9'):
            at = raw.find(opcode)
            while at>=0:
                if at+5<=len(raw) and image.base+rva+at+5+struct.unpack_from('<i',raw,at+1)[0]==target:
                    hits.append(image.base+rva+at)
                at=raw.find(opcode,at+1)
    return sorted(hits)


def copy_witness(source):
    """Finite synthetic NUL-copy witness, not an original allocation/CPU run."""
    if source is None:
        return None
    stop = source.index(0)
    return bytearray(source[:stop+1])


def pointer_witness(families,argument,selector,index):
    """The unresolved selector result is supplied, not reconstructed."""
    selector_call = (families,argument)
    if type(index) is not int or not 0<=index<len(families):
        raise ValueError("Synthetic pointer witness index is outside supplied extent")
    return families[index],dict(Selector=selector,SelectorArgumentIdentity=True,
                                 SelectedIndex=index,InputCall=selector_call)


def synthetic_checks():
    count=0
    for raw,wanted in [(bytearray(b'\0'),b'\0'),(bytearray(b'A/\\\xff\x80\0tail'),b'A/\\\xff\x80\0'),
                       (bytearray(b'x'*999+b'\0'),b'x'*999+b'\0')]:
        owned=copy_witness(raw)
        assert bytes(owned)==wanted and owned is not raw
        raw[0]^=1
        assert bytes(owned)==wanted
        count+=2
    assert copy_witness(None) is None
    count+=1
    families=[bytearray(b'one\0'),None,bytearray(b'ONE\0')]
    opaque=object()
    for index in range(len(families)):
        value,call=pointer_witness(families,opaque,'00B69860',index)
        assert value is families[index] and call['InputCall'][1] is opaque
        count+=2
    return count


def original_second_strings(asset_root,config):
    spec=importlib.util.spec_from_file_location('second_tgen33',REPO/'poc/round33-tests/Assets/verify_tgen_input.py')
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    import contextlib
    if module.sha(config.read_bytes())!=module.CONFIG:
        raise ValueError("Pinned configuration SHA mismatch")
    original=None
    with contextlib.ExitStack() as stack:
        for priority,path in module.archives(asset_root,config):
            original=module.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
            if original is not None:
                break
    if original is None or priority!=42 or len(original)!=1188446 or module.sha(original)!=module.SOURCE:
        raise ValueError("Pinned original terrain identity mismatch")
    generator=original[210:210+139608]
    if module.sha(generator)!=module.GENERATOR:
        raise ValueError("Pinned generator identity mismatch")
    blob=generator[24:24+1977]
    if sha(blob)!='c0f9043140db936e3f3f2dd3eec795f1bee5424e6e904081c667cfc9e99a0628':
        raise ValueError("Pinned SGRP identity mismatch")
    root,=module.records(blob,0,len(blob))
    version,=module.records(blob,12,len(blob))
    if root[0]!=b'FORM' or root[1][:4]!=b'SGRP' or version[0]!=b'FORM' or version[1][:4]!=b'0006':
        raise ValueError("Pinned group/version mismatch")
    rows=[]
    entries=0
    for index,(tag,payload,start,end) in enumerate(module.records(blob,24,len(blob))):
        if tag!=b'SFAM':
            raise ValueError("Pinned family tag mismatch")
        first_end=payload.index(0,4)
        second_end=payload.index(0,first_end+1)
        second=payload[first_end+1:second_end+1]
        child_count,=struct.unpack_from('<I',payload,second_end+12)
        if child_count>1024:
            raise ValueError("Pinned entry count witness bound")
        cursor=second_end+16
        for _ in range(child_count):
            cursor=payload.index(0,cursor)+5
        if cursor!=len(payload):
            raise ValueError("Pinned family byte consumption mismatch")
        owned=copy_witness(bytearray(second))
        assert bytes(owned)==second
        rows.append(dict(Index=index,FamilyOffset=start,SecondOffset=start+8+first_end+1,
                         SecondBytes=len(second)-1,SourceWithNulSha256=sha(second),OwnedWithNulSha256=sha(owned),
                         SecondHex=second.hex(),StoredFieldOffset=20))
        entries+=child_count
    if len(rows)!=15 or entries!=35:
        raise ValueError("Pinned group counts mismatch")
    return rows,entries


def verify(executable,packages,asset_root,config,output,maximum=12):
    budget(maximum=maximum)
    prerequisite,consumer=dependencies()
    destination=consumer.output_path(output)
    data=executable.read_bytes()
    gate(data)
    image=prerequisite.RawPe(data)
    sys.path[:0]=[str(packages),str(REPO/'tools/python')]
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=data,fast_load=True)
    for va,size,_,_ in BODIES.values():
        location=image.location(va,size)
        if not location['Executable'] or location['FileOffset']!=pe.get_offset_from_rva(va-image.base):
            raise ValueError("Independent raw PE/code mapping disagreement")
    decoder=Cs(CS_ARCH_X86,CS_MODE_32)
    decoder.detail=True
    instructions=check_bodies(image.read,decoder)
    getter_candidates=direct_candidates(image,0x00B69370)
    consumer_candidates=direct_candidates(image,0x00B6B480)
    if getter_candidates!=[0x00B6AE0F,0x00B6AE18,0x00B6B495] or consumer_candidates:
        raise ValueError("Pinned raw reference candidates differ")
    rows,entries=original_second_strings(asset_root,config)
    checks=synthetic_checks()
    receipt=dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),Instructions=len(instructions),Anchors=len(ANCHORS),
                 FullSetterBodies=1,SmallHelpersExpanded=1,SelectedConsumerBodies=1,
                 FreshDistinctFunctionLeads=len(LEADS),UniqueFollowedAndDiscoveredRawSites=len(SITES),BudgetCap=maximum,
                 DirectCalls=len(CALLS),DirectBranches=len(BRANCHES),FieldOffset=20,
                 Setter='00B69390',Getter='00B69370',Consumer='00B6B480',UnresolvedIndexSelector='00B69860',
                 GetterRawCandidates=[f'{va:08X}' for va in getter_candidates],ConsumerRawCallerCandidates=0,
                 ConsumerRuntimeReachabilityProved=False,ResourceOpenFetchProved=False,StringTransformationObserved=False,
                 OriginalOccurrences=1,OriginalFamilies=len(rows),OriginalEntries=entries,OriginalSecondStringByteWitnesses=len(rows),
                 OriginalGroupSha256='C0F9043140DB936E3F3F2DD3EEC795F1BEE5424E6E904081C667CFC9E99A0628',
                 SyntheticChecks=checks,AssetNameSemanticsProved=False,ProductionResolver=False,OriginalExecution=False,
                 FrozenPrerequisiteSources=SEALED,Bodies={name:dict(Va=f'{va:08X}',Bytes=size,Sha256=h,Instructions=n) for name,(va,size,h,n) in BODIES.items()})
    consumer.output_path(destination)
    destination.mkdir(parents=True)
    (destination/'second-byte-witnesses.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
    (destination/'instruction-witnesses.json').write_text(json.dumps([dict(Va=f'{i.address:08X}',Bytes=bytes(i.bytes).hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],indent=2)+'\n',encoding='utf-8')
    (destination/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    return receipt


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('exe','packages','asset-root','config','output'):
        parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--maximum-leads',type=int,default=12)
    args=parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads),indent=2))
