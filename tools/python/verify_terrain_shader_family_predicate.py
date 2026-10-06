"""SHA-gated static predicate evidence, not a production resolver or CPU runner.

The zero-global-mode byte comparison is closed. Nonzero mode stops at its
unexpanded synchronization and byte-transform delegates. All original witnesses
remain private; synthetic byte algebra never launches the original executable.
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
BODY_VA, BODY_BYTES = 0x01320590, 208
BODY_SHA = "ad7f167f1af74dd4aa9d8bdd3e65e868dd15586b88bc6f535ba112b61b7bd966"
CALLER_VA, CALLER_BYTES = 0x00B6B360, 71
CALLER_SHA = "8253dabf507f11e0fb35d335c0a3043aa6dbf4fa7630bc221de15b8ef8fd278b"
SEALED = {
    "tools/python/verify_terrain_shader_family_consumer.py": "952d30a74b8218eedc597b07ad3ac641f346a4ffe5fce4e40e49a75f9549cdd3",
    "docs/TERRAIN_SHADER_FAMILY_CONSUMER_CONTRACT.md": "fa26c3a02dac0b17b0d3ddd2574172c9635b4ff4e7a88b59f034c4ba06f3bc72",
}
LEADS = (BODY_VA, 0x013234B3, 0x0131D757, 0x01323514)
CALLS = {0x01320600:0x013234B3, 0x01320624:0x0131D757,
         0x0132062E:0x0131D757, 0x01320651:0x01323514}
BRANCHES = {
    0x013205A6:("jne",0x013205E3), 0x013205AE:("je",0x013205DE),
    0x013205B8:("je",0x013205AC), 0x013205D8:("je",0x013205AC),
    0x013205E1:("jmp",0x0132065B), 0x013205F1:("jg",0x013205F7),
    0x013205F5:("jmp",0x0132060C), 0x01320616:("je",0x0132063F),
    0x01320620:("je",0x01320614), 0x01320638:("je",0x01320614),
    0x01320644:("jne",0x0132064F), 0x0132064D:("jmp",0x01320659),
}
ANCHORS = {
    0x01320596: ("8b750c", "mov", "esi, dword ptr [ebp + 0xc]"),
    0x01320599: ("8b7d08", "mov", "edi, dword ptr [ebp + 8]"),
    0x0132059C: ("8d0508a19a01", "lea", "eax, [0x19aa108]"),
    0x013205A2: ("83780800", "cmp", "dword ptr [eax + 8], 0"),
    0x013205A6: ("753b", "jne", "0x13205e3"),
    0x013205AE: ("742e", "je", "0x13205de"),
    0x013205B0: ("8a06", "mov", "al, byte ptr [esi]"),
    0x013205B3: ("8a27", "mov", "ah, byte ptr [edi]"),
    0x013205B6: ("38c4", "cmp", "ah, al"),
    0x013205B8: ("74f2", "je", "0x13205ac"),
    0x013205BA: ("2c41", "sub", "al, 0x41"),
    0x013205BC: ("3c1a", "cmp", "al, 0x1a"),
    0x013205BE: ("1ac9", "sbb", "cl, cl"),
    0x013205C0: ("80e120", "and", "cl, 0x20"),
    0x013205C3: ("02c1", "add", "al, cl"),
    0x013205C5: ("0441", "add", "al, 0x41"),
    0x013205C7: ("86e0", "xchg", "al, ah"),
    0x013205C9: ("2c41", "sub", "al, 0x41"),
    0x013205CB: ("3c1a", "cmp", "al, 0x1a"),
    0x013205CD: ("1ac9", "sbb", "cl, cl"),
    0x013205CF: ("80e120", "and", "cl, 0x20"),
    0x013205D2: ("02c1", "add", "al, cl"),
    0x013205D4: ("0441", "add", "al, 0x41"),
    0x013205D6: ("38e0", "cmp", "al, ah"),
    0x013205D8: ("74d2", "je", "0x13205ac"),
    0x013205DA: ("1ac0", "sbb", "al, al"),
    0x013205DC: ("1cff", "sbb", "al, 0xff"),
    0x013205DE: ("0fbec0", "movsx", "eax, al"),
    0x013205E3: ("f0ff05c8cf9b01", "lock inc", "dword ptr [0x19bcfc8]"),
    0x013205EA: ("833dc4cf9b0100", "cmp", "dword ptr [0x19bcfc4], 0"),
    0x013205F1: ("7f04", "jg", "0x13205f7"),
    0x013205F7: ("f0ff0dc8cf9b01", "lock dec", "dword ptr [0x19bcfc8]"),
    0x013205FE: ("6a13", "push", "0x13"),
    0x01320600: ("e8ae2e0000", "call", "0x13234b3"),
    0x01320605: ("c7042401000000", "mov", "dword ptr [esp], 1"),
    0x0132060C: ("b8ff000000", "mov", "eax, 0xff"),
    0x01320618: ("8a06", "mov", "al, byte ptr [esi]"),
    0x0132061B: ("8a1f", "mov", "bl, byte ptr [edi]"),
    0x01320622: ("50", "push", "eax"),
    0x01320623: ("53", "push", "ebx"),
    0x01320624: ("e82ed1ffff", "call", "0x131d757"),
    0x01320629: ("8bd8", "mov", "ebx, eax"),
    0x0132062B: ("83c404", "add", "esp, 4"),
    0x0132062E: ("e824d1ffff", "call", "0x131d757"),
    0x01320633: ("83c404", "add", "esp, 4"),
    0x01320636: ("38c3", "cmp", "bl, al"),
    0x0132063A: ("1bc0", "sbb", "eax, eax"),
    0x0132063C: ("83d8ff", "sbb", "eax, -1"),
    0x01320641: ("58", "pop", "eax"),
    0x01320646: ("f0ff0dc8cf9b01", "lock dec", "dword ptr [0x19bcfc8]"),
    0x01320651: ("e8be2e0000", "call", "0x1323514"),
    0x01320659: ("8bc3", "mov", "eax, ebx"),
    0x0132065F: ("c3", "ret", ""),
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if sha(data) != EXE_SHA:
        raise ValueError("Executable SHA mismatch before any VA read")


def sealed_gate(relative, data):
    if relative not in SEALED or sha(data) != SEALED[relative]:
        raise ValueError("Frozen62 sealed input mismatch")


def budget(leads=LEADS, sites=tuple(CALLS), maximum=12):
    if type(maximum) is not int or not 0 <= maximum <= 12:
        raise ValueError("Maximum fresh leads/sites must be integer 0..12")
    if len(set(leads)) != len(leads) or len(set(sites)) != len(sites):
        raise ValueError("Duplicate function lead/site")
    if len(leads) > maximum or len(sites) > maximum:
        raise ValueError("Fresh lead/site budget exceeded")
    if tuple(leads) != LEADS or tuple(sites) != tuple(CALLS):
        raise ValueError("Unassigned function lead/site")


class RawPe:
    """Independent bounded raw-header/section mapper; virtual tail is not bytes."""
    def __init__(self, data):
        self.data = data
        def unpack(fmt, at):
            size = struct.calcsize(fmt)
            if at < 0 or at+size > len(data):
                raise ValueError("Truncated PE structure")
            return struct.unpack_from(fmt, data, at)
        if len(data) < 64 or data[:2] != b"MZ":
            raise ValueError("Missing DOS header")
        header, = unpack("<I",60)
        if data[header:header+4] != b"PE\0\0":
            raise ValueError("Missing PE signature")
        machine, sections = unpack("<HH",header+4)
        optional_size, = unpack("<H",header+20)
        optional = header+24
        magic, = unpack("<H",optional)
        self.base, = unpack("<I",optional+28)
        self.image_bytes, = unpack("<I",optional+56)
        if machine != 0x14C or magic != 0x10B or self.base != 0x400000 or optional_size < 96 or not 1 <= sections <= 96:
            raise ValueError("Unexpected PE32 ABI/section table")
        self.sections = []
        for index in range(sections):
            at = optional+optional_size+40*index
            name, virtual, rva, raw_size, raw_at = unpack("<8s4I",at)
            flags, = unpack("<I",at+36)
            span = max(virtual,raw_size)
            if not span or rva+span > self.image_bytes or raw_at+raw_size > len(data):
                raise ValueError("Invalid/truncated PE section extent")
            if any(rva < old[1]+old[2] and old[1] < rva+span for old in self.sections):
                raise ValueError("Overlapping PE virtual section extents")
            self.sections.append((name.rstrip(b"\0").decode("ascii"),rva,span,raw_at,raw_size,flags))

    def location(self, va, size):
        if type(size) is not int or size <= 0 or va < self.base:
            raise ValueError("Invalid VA witness extent")
        rva = va-self.base
        for name,start,span,raw_at,raw_size,flags in self.sections:
            if start <= rva and rva+size <= start+span:
                delta = rva-start
                backed = delta+size <= raw_size
                return dict(Section=name,FileBacked=backed,FileOffset=raw_at+delta if backed else None,
                            VirtualAddress=va,Bytes=size,Executable=bool(flags & 0x20000000))
        raise ValueError("VA extent is outside one bounded section")

    def read(self, va, size):
        location = self.location(va,size)
        if not location["FileBacked"]:
            raise ValueError("Virtual-only tail has no original file bytes")
        start = location["FileOffset"]
        value = self.data[start:start+size]
        if len(value) != size:
            raise ValueError("Truncated VA witness")
        return value


def check_anchors(read, decoder):
    for va,(encoded,mnemonic,operands) in ANCHORS.items():
        expected = bytes.fromhex(encoded)
        actual = read(va,len(expected))
        if actual != expected:
            raise ValueError(f"Raw anchor mismatch at {va:08X}")
        instructions = list(decoder.disasm(actual,va))
        if len(instructions) != 1 or (instructions[0].mnemonic,instructions[0].op_str) != (mnemonic,operands):
            raise ValueError(f"Decoded operand witness mismatch at {va:08X}")


def check_control(instructions):
    from capstone.x86 import X86_OP_IMM
    addresses = {item.address for item in instructions}
    calls, branches = {}, {}
    for item in instructions:
        if item.mnemonic == "call" or item.mnemonic.startswith("j"):
            if len(item.operands) != 1 or item.operands[0].type != X86_OP_IMM:
                raise ValueError("Unresolved indirect control edge in selected body")
            destination = item.operands[0].imm & 0xFFFFFFFF
            if item.mnemonic == "call":
                calls[item.address] = destination
            else:
                if destination not in addresses:
                    raise ValueError("Branch does not target a verified instruction boundary")
                branches[item.address] = (item.mnemonic,destination)
    if calls != CALLS or branches != BRANCHES:
        raise ValueError("Call/branch witness mismatch")


def check_body(read, decoder):
    raw = read(BODY_VA,BODY_BYTES)
    if len(raw) != BODY_BYTES or sha(raw) != BODY_SHA:
        raise ValueError("Selected complete predicate body mismatch")
    instructions = list(decoder.disasm(raw,BODY_VA))
    if len(instructions) != 85 or sum(x.size for x in instructions) != BODY_BYTES or instructions[-1].mnemonic != "ret":
        raise ValueError("Incomplete selected body or incorrect return boundary")
    check_anchors(read,decoder)
    check_control(instructions)
    if sha(read(CALLER_VA,CALLER_BYTES)) != CALLER_SHA:
        raise ValueError("Accepted62 caller body mismatch")
    return instructions


def ascii_fold(byte):
    return byte+32 if 65 <= byte <= 90 else byte


def fold_operand_witness(byte):
    """Synthetic eight-bit carry/mask algebra, never CPU execution."""
    difference = (byte-65) & 255
    carry_mask = 255 if difference < 26 else 0
    return (difference+(carry_mask & 32)+65) & 255


def compare_bytes(left, right):
    """Finite-byte witness of the closed zero-mode contract; no length policy."""
    if left is None or right is None:
        raise ValueError("Synthetic witness cannot dereference a null pointer")
    index = 0
    while index < min(len(left),len(right)):
        a,b = ascii_fold(left[index]),ascii_fold(right[index])
        if a != b:
            return -1 if a < b else 1
        if a == 0:
            return 0
        index += 1
    raise ValueError("Witness extent exhausted before mismatch or equal NUL")


def synthetic_checks():
    for byte in range(256):
        assert ascii_fold(byte) == fold_operand_witness(byte)
    pairs = 0
    for first in range(256):
        for second in range(256):
            a,b = fold_operand_witness(first),fold_operand_witness(second)
            expected = 0 if a == b else -1 if a < b else 1
            assert compare_bytes(bytes((first,0)),bytes((second,0))) == expected
            pairs += 1
    vectors = [
        (b"\0",b"\0",0),(b"MiXeD\0tail",b"mixed\0other",0),
        (b"prefix\0",b"prefixx\0",-1),(b"prefixx\0",b"prefix\0",1),
        (b"[\0",b"{\0",-1),(b"\\path\0",b"/path\0",1),
        (b"\xc0\0",b"\xe0\0",-1),(b"\xff\0",b"\x80\0",1),
        (b"a",b"b",-1),(b"same\0ignored",b"SAME\0different",0),
        (b"x"*4096+b"A\0",b"x"*4096+b"a\0",0),
    ]
    for left,right,wanted in vectors:
        assert compare_bytes(left,right) == wanted
    extent_rejections = 0
    for left,right in [(None,b"\0"),(b"\0",None),(b"",b""),(b"equal",b"equal"),(b"equal",b"EQUAL")]:
        try:
            compare_bytes(left,right)
        except ValueError:
            extent_rejections += 1
        else:
            raise AssertionError("Malformed finite witness accepted")
    return dict(FoldBytes=256,OrderedBytePairs=pairs,StringVectors=len(vectors),FiniteWitnessRejections=extent_rejections)


def frozen_consumer():
    for relative in SEALED:
        sealed_gate(relative,(REPO/relative).read_bytes())
    spec = importlib.util.spec_from_file_location("predicate_consumer62",REPO/"tools/python/verify_terrain_shader_family_consumer.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def verify(executable, packages, asset_root, config, output, maximum=12):
    budget(maximum=maximum)
    consumer = frozen_consumer()
    destination = consumer.output_path(output)
    data = executable.read_bytes()
    gate(data)
    image = RawPe(data)
    sys.path[:0] = [str(packages),str(REPO/"tools/python")]
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe = pefile.PE(data=data,fast_load=True)
    location = image.location(BODY_VA,BODY_BYTES)
    if not location["Executable"] or pe.get_offset_from_rva(BODY_VA-image.base) != location["FileOffset"]:
        raise ValueError("Independent PE mapping disagreement")
    decoder = Cs(CS_ARCH_X86,CS_MODE_32)
    decoder.detail = True
    instructions = check_body(image.read,decoder)
    globals = [image.location(va,4) for va in (0x019AA110,0x019BCFC4,0x019BCFC8)]
    if any(row["FileBacked"] for row in globals):
        raise ValueError("Unexpected file-backed mode/synchronization globals")
    checks = synthetic_checks()
    originals,entries = consumer.original_witness(asset_root,config)
    original_checks = []
    for item in originals:
        name = bytes.fromhex(item["FirstStringHex"])
        case_variant = bytes(byte-32 if 97 <= byte <= 122 else byte for byte in name)
        assert compare_bytes(name,name) == compare_bytes(name,case_variant) == 0
        original_checks.append(dict(Index=item["Index"],WithNulSha256=sha(name),
                                    SyntheticCaseVariantSha256=sha(case_variant),SelfResult=0,VariantResult=0))
    receipt = dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),Predicate="01320590",
                   BodyBytes=BODY_BYTES,BodySha256=BODY_SHA,Instructions=len(instructions),Anchors=len(ANCHORS),
                   DirectCallSites=len(CALLS),DirectBranchSites=len(BRANCHES),FreshDistinctFunctionLeads=len(LEADS),
                   FreshDirectCalleeSites=len(CALLS),LeadAndSiteCap=maximum,ExpandedBodies=1,TinyForwardsExpanded=0,
                   ModeCondition="DWORD[019AA110]==0",ZeroModeRule="NUL byte strings; ASCII A-Z folding; unsigned order; -1/0/+1",
                   NonzeroModeClosed=False,UnexpandedDelegates=["013234B3","0131D757","01323514"],
                   CallerArgumentOne="byte-string pointer in EDI",CallerArgumentTwo="owned first-string pointer in ESI",
                   NullCheckPresent=False,ExplicitLengthPresent=False,AssetNamesProved=False,ProductionResolver=False,
                   OriginalOccurrences=1,OriginalFamilies=len(originals),OriginalEntries=entries,
                   OriginalZeroModeModelComparisons=2*len(originals),OriginalGroupSha256=consumer.GROUP_SHA.upper(),
                   Synthetic=checks,Globals=globals,OriginalExecution=False,OriginalConsumerExecution=False,
                   RuntimeModeEstablished=False,RawPeMapping=location,Frozen62Inputs=SEALED)
    consumer.output_path(destination)
    destination.mkdir(parents=True)
    (destination/"byte-witnesses.json").write_text(json.dumps(original_checks,indent=2)+"\n",encoding="utf-8")
    (destination/"instruction-witnesses.json").write_text(json.dumps([dict(Va=f"{i.address:08X}",Bytes=bytes(i.bytes).hex(),Mnemonic=i.mnemonic,Operands=i.op_str) for i in instructions],indent=2)+"\n",encoding="utf-8")
    (destination/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ("exe","packages","asset-root","config","output"):
        parser.add_argument("--"+key,type=Path,required=True)
    parser.add_argument("--maximum-leads",type=int,default=12)
    args = parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads),indent=2))
