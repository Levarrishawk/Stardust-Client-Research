"""SHA-gated, static-only FSLP startup registration and ordering verifier.

No original process is launched. Full-section linear decoding is a discovery
cross-check, not a proof that every decoded byte is reachable code.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path

EXPECTED = "e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150"
BASE = 0x400000
TABLE_START, TABLE_END, SLOT = 0x184D000, 0x1866B90, 0x18560AC
CLAMP = 0x1947228
ANCHORS = {
    0x131DD1F: ("call", "0x1324772"),
    0x131DD55: ("call", "0x401810"),
    0x1324772: ("mov", "eax, dword ptr [0x18ee4a8]"),
    0x1324777: ("test", "eax, eax"),
    0x1324779: ("je", "0x132477d"),
    0x132477B: ("call", "eax"),
    0x132477D: ("push", "0x1866ba8"),
    0x1324782: ("push", "0x1866b94"),
    0x1324787: ("call", "0x1324896"),
    0x132478C: ("push", "0x1866b90"),
    0x1324791: ("push", "0x184d000"),
    0x1324796: ("call", "0x1324896"),
    0x132479B: ("add", "esp, 0x10"),
    0x132479E: ("ret", ""),
    0x1324896: ("push", "esi"),
    0x1324897: ("mov", "esi, dword ptr [esp + 8]"),
    0x132489B: ("cmp", "esi, dword ptr [esp + 0xc]"),
    0x132489F: ("jae", "0x13248ae"),
    0x13248A1: ("mov", "eax, dword ptr [esi]"),
    0x13248A3: ("test", "eax, eax"),
    0x13248A5: ("je", "0x13248a9"),
    0x13248A7: ("call", "eax"),
    0x13248A9: ("add", "esi, 4"),
    0x13248AC: ("jmp", "0x132489b"),
    0x13248AE: ("pop", "esi"),
    0x13248AF: ("ret", ""),
    0xB77640: ("jmp", "0xb77650"),
    0xB77650: ("fld", "dword ptr [0x15ed6d8]"),
    0xB77656: ("fadd", "st(0), st(0)"),
    0xB77658: ("fstp", "dword ptr [0x194722c]"),
    0xB7765E: ("ret", ""),
    0xB77660: ("jmp", "0xb77670"),
    0xB77670: ("fld", "dword ptr [0x15ed6d8]"),
    0xB77676: ("fmul", "dword ptr [0x15dc5e4]"),
    0xB7767C: ("fstp", "dword ptr [0x1947228]"),
    0xB77682: ("ret", ""),
    0xB77690: ("jmp", "0xb776a0"),
    0xB776A0: ("fld", "dword ptr [0x15dc6f8]"),
    0xB776A6: ("fdiv", "dword ptr [0x194722c]"),
    0xB776AC: ("fstp", "dword ptr [0x1947230]"),
    0xB776B2: ("ret", ""),
    0xB78A20: ("mov", "dword ptr [esi], 0x15ed72c"),
    0xB78A26: ("call", "0xb78f40"),
    0xB78A2E: ("call", "0xb78fb0"),
    0xB78A56: ("call", "0xb78a80"),
    0xB78A86: ("jmp", "0xb63410"),
    0x1324844: ("push", "0x1866bb4"),
    0x1324849: ("push", "0x1866bac"),
    0x132484E: ("call", "0x1324896"),
    0x1324855: ("push", "0x1866bc0"),
    0x132485A: ("push", "0x1866bb8"),
    0x132485F: ("call", "0x1324896"),
}
CLAMP_REFS = {
    0xB7767C: ("fstp", "dword ptr [0x1947228]"),
    0xB78F5C: ("fsubr", "dword ptr [0x1947228]"),
    0xB78F6E: ("fcomp", "dword ptr [0x1947228]"),
    0xB78F7B: ("fld", "dword ptr [0x1947228]"),
    0xB78F84: ("fsubr", "dword ptr [0x1947228]"),
    0xB78F99: ("fsubr", "dword ptr [0x1947228]"),
    0xB78FCC: ("fsubr", "dword ptr [0x1947228]"),
    0xB78FDE: ("fcomp", "dword ptr [0x1947228]"),
    0xB78FEB: ("fld", "dword ptr [0x1947228]"),
    0xB78FF4: ("fsubr", "dword ptr [0x1947228]"),
    0xB79009: ("fsubr", "dword ptr [0x1947228]"),
}


def gate(data: bytes) -> None:
    if hashlib.sha256(data).hexdigest() != EXPECTED:
        raise ValueError("Executable SHA-256 mismatch; no VA interpretation permitted")


def u32(image: bytes, va: int) -> int:
    return struct.unpack_from("<I", image, va - BASE)[0]


def check_image(image: bytes, decoder) -> dict:
    """Called only after the source identity gate, except synthetic negatives."""
    for va, expected in {**ANCHORS, **CLAMP_REFS}.items():
        instruction = next(decoder.disasm(image[va-BASE:va-BASE+15], va, count=1), None)
        assert instruction is not None and (instruction.mnemonic, instruction.op_str) == expected, hex(va)
    assert (TABLE_END-TABLE_START) % 4 == 0 and (SLOT-TABLE_START) % 4 == 0
    words = struct.unpack_from("<26340I", image, TABLE_START-BASE)
    assert sum(bool(w) for w in words) == 26339 and words[0] == 0
    assert words[9258:9261] == (0xB77640, 0xB77660, 0xB77690)
    assert words.count(0xB77660) == 1
    assert u32(image, CLAMP) == 0
    assert u32(image, 0x15ED6D8) == 0x40490FDB
    assert u32(image, 0x15DC5E4) == 0x3F000000
    assert u32(image, 0x18EE4A8) == 0x131DB29
    # Independent integer binary32 model: multiplication by 2^-1 shifts
    # the exponent of this finite normal without discarding significand bits.
    p = u32(image, 0x15ED6D8)
    assert (p & 0x7F800000) > 0x00800000
    exact_half_bits = p - 0x00800000
    assert exact_half_bits == 0x3FC90FDB
    return dict(InstructionAnchors=len(set(ANCHORS)|set(CLAMP_REFS)),
                TableWords=len(words), NonNullTableWords=sum(bool(w) for w in words),
                Slot=f"{SLOT:08X}", ZeroBasedSlotIndex=9259,
                MappedClampBits="00000000", ExactFiniteHalfBits=f"{exact_half_bits:08X}")


def verify(executable: Path, packages: Path) -> dict:
    data = executable.read_bytes()
    gate(data)  # Must precede PE parsing, package imports and every VA read.
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32, CS_OP_IMM, CS_OP_MEM
    pe = pefile.PE(data=data, fast_load=True)
    assert pe.OPTIONAL_HEADER.ImageBase == BASE
    assert BASE+pe.OPTIONAL_HEADER.AddressOfEntryPoint == 0x131DC7A
    assert pe.OPTIONAL_HEADER.DATA_DIRECTORY[9].VirtualAddress == 0
    assert pe.OPTIONAL_HEADER.DATA_DIRECTORY[9].Size == 0
    image = pe.get_memory_mapped_image()
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    decoder.skipdata = True
    result = check_image(image, decoder)
    refs, branches, decoded, skipped = {}, [], 0, 0
    literal_pointers = {0xB77660: [], 0xB77670: []}
    for section in pe.sections:
        section_data = section.get_data()
        section_va = BASE+section.VirtualAddress
        for target, locations in literal_pointers.items():
            needle = struct.pack("<I", target)
            offset = section_data.find(needle)
            while offset >= 0:
                locations.append(section_va+offset)
                offset = section_data.find(needle, offset+1)
        if not section.IMAGE_SCN_MEM_EXECUTE:
            continue
        for instruction in decoder.disasm(section_data, section_va):
            if not instruction.id:
                skipped += 1
                continue  # skip-data records have no operand details
            decoded += 1
            for operand in instruction.operands:
                if operand.type == CS_OP_MEM and (operand.mem.disp & 0xFFFFFFFF) == CLAMP:
                    refs[instruction.address] = (instruction.mnemonic, instruction.op_str)
                if (instruction.mnemonic in ("call", "jmp") and operand.type == CS_OP_IMM
                        and (operand.imm & 0xFFFFFFFF) in (0xB77660, 0xB77670)):
                    branches.append((instruction.address, operand.imm & 0xFFFFFFFF))
    assert refs == CLAMP_REFS, refs
    assert branches == [(0xB77660, 0xB77670)], branches
    assert literal_pointers == {0xB77660: [SLOT], 0xB77670: []}, literal_pointers
    # Wrong-identity negatives never reach the decoder. Synthetic in-memory
    # anchor/table mutations are separately labeled and are never source proof.
    identity_negatives = 0
    for offset in (0, 1, len(data)//2, len(data)-1):
        changed = bytearray(data)
        changed[offset] ^= 1
        try:
            gate(changed)
        except ValueError:
            identity_negatives += 1
        else:
            raise AssertionError("wrong identity accepted")
    for changed in (b"", data[:-1], data+b"\0", bytes(64)):
        try:
            gate(changed)
        except ValueError:
            identity_negatives += 1
        else:
            raise AssertionError("wrong size/identity accepted")
    structural_negatives = 0
    for va in sorted(set(ANCHORS)|set(CLAMP_REFS)|{SLOT, CLAMP, 0x15ED6D8, 0x15DC5E4}):
        changed = bytearray(image)
        changed[va-BASE] ^= 1
        try:
            check_image(changed, decoder)
        except (AssertionError, StopIteration):
            structural_negatives += 1
        else:
            raise AssertionError(f"synthetic mutation accepted at {va:08X}")
    return dict(Passed=True, ExecutableSha256=EXPECTED.upper(), **result,
                LinearDecodedInstructions=decoded, SkipDataRecords=skipped,
                ClampDirectReferences=len(refs), ClampDirectWrites=1,
                HelperDirectBranchSites=len(branches), InitializerThunkPointerOccurrences=1,
                HelperLiteralPointerOccurrences=0, WrongIdentityNegatives=identity_negatives,
                SyntheticStructureNegatives=structural_negatives,
                Ordering="initializer before ordinary application entry on returning startup path",
                UniversalPreConstructorOrderProved=False, OriginalExecution=False,
                ProductionInfluence=False, FsinNumericalParity=False)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    parser.add_argument("packages", type=Path)
    args = parser.parse_args()
    print(json.dumps(verify(args.executable, args.packages), indent=2))
