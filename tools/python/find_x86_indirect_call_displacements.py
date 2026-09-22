#!/usr/bin/env python3
"""Locate x86 indirect calls whose memory operand uses selected displacements."""
from __future__ import annotations

import argparse
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_MEM


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("displacements", nargs="+", type=lambda value: int(value, 0))
args = parser.parse_args()

pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
wanted = set(args.displacements)
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True

for section in pe.sections:
    if not (section.Characteristics & 0x20000000):
        continue
    va = base + section.VirtualAddress
    for instruction in md.disasm(section.get_data(), va):
        if instruction.mnemonic != "call" or len(instruction.operands) != 1:
            continue
        operand = instruction.operands[0]
        if operand.type == X86_OP_MEM and operand.mem.disp in wanted:
            print(f"{instruction.address:08X}\t{instruction.mnemonic}\t{instruction.op_str}")
