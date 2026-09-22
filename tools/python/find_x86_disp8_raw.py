#!/usr/bin/env python3
"""Find independently decoded x86 instructions with a selected signed disp8."""
import argparse
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_MEM, X86_REG_EBP, X86_REG_ESP


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("displacement", type=lambda value: int(value, 0))
parser.add_argument("--start", type=lambda value: int(value, 0), default=0)
parser.add_argument("--end", type=lambda value: int(value, 0), default=0xFFFFFFFF)
args = parser.parse_args()

pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
decoder = Cs(CS_ARCH_X86, CS_MODE_32)
decoder.detail = True
seen = set()
for section in pe.sections:
    if not section.Characteristics & 0x20000000:
        continue
    data = section.get_data()
    section_va = base + section.VirtualAddress
    for offset, byte in enumerate(data):
        address = section_va + offset
        if not args.start <= address < args.end or byte not in (0x8B, 0x8D, 0x89, 0x80, 0x83, 0xC6, 0xC7, 0xFF):
            continue
        decoded = next(decoder.disasm(data[offset:offset + 15], address), None)
        if decoded is None:
            continue
        for operand in decoded.operands:
            if operand.type != X86_OP_MEM or operand.mem.disp != args.displacement:
                continue
            if operand.mem.base in (X86_REG_EBP, X86_REG_ESP) or decoded.address in seen:
                continue
            seen.add(decoded.address)
            print(f"0x{decoded.address:08X}\t{decoded.mnemonic}\t{decoded.op_str}")
