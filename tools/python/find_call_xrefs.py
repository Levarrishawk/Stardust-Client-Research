#!/usr/bin/env python3
"""Find direct x86 CALL/JMP references to virtual addresses."""
from __future__ import annotations

import argparse
import bisect
import csv

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_INS_CALL, X86_INS_JMP, X86_OP_IMM


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("inventory")
parser.add_argument("targets", nargs="+", type=lambda value: int(value, 0))
args = parser.parse_args()
targets = set(args.targets)

functions = []
with open(args.inventory, encoding="utf-8") as stream:
    for row in csv.DictReader(stream, delimiter="\t"):
        functions.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
functions.sort()
starts = [row[0] for row in functions]

pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
decoder = Cs(CS_ARCH_X86, CS_MODE_32)
decoder.detail = True
decoder.skipdata = True
for section in pe.sections:
    if not section.IMAGE_SCN_MEM_EXECUTE:
        continue
    section_va = base + section.VirtualAddress
    for instruction in decoder.disasm(section.get_data(), section_va):
        if instruction.id not in (X86_INS_CALL, X86_INS_JMP) or not instruction.operands:
            continue
        operand = instruction.operands[0]
        if operand.type != X86_OP_IMM or (operand.imm & 0xFFFFFFFF) not in targets:
            continue
        target = operand.imm & 0xFFFFFFFF
        index = bisect.bisect_right(starts, instruction.address) - 1
        owner = ""
        if index >= 0 and instruction.address < functions[index][1]:
            owner = f"{functions[index][0]:08X}\t{functions[index][2]}"
        print(f"{target:08X}\t{instruction.address:08X}\t{owner}")
