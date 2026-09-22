#!/usr/bin/env python3
"""List x86 instructions referencing an absolute data-address interval."""
from __future__ import annotations

import argparse
import bisect
import csv

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM, X86_OP_MEM


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("image")
    parser.add_argument("inventory")
    parser.add_argument("start", type=lambda value: int(value, 0))
    parser.add_argument("end", type=lambda value: int(value, 0))
    args = parser.parse_args()

    functions = []
    with open(args.inventory, encoding="utf-8") as handle:
        for row in csv.DictReader(handle, delimiter="\t"):
            functions.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
    starts = [row[0] for row in functions]

    pe = pefile.PE(args.image, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    md.skipdata = True
    base = pe.OPTIONAL_HEADER.ImageBase
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        for instruction in md.disasm(section.get_data(), base + section.VirtualAddress):
            if instruction.id == 0:
                continue
            values = []
            for operand in instruction.operands:
                if operand.type == X86_OP_MEM:
                    value = operand.mem.disp & 0xFFFFFFFF
                    if args.start <= value < args.end:
                        values.append(value)
                elif operand.type == X86_OP_IMM:
                    value = operand.imm & 0xFFFFFFFF
                    if args.start <= value < args.end:
                        values.append(value)
            if not values:
                continue
            index = bisect.bisect_right(starts, instruction.address) - 1
            owner = ""
            if index >= 0 and instruction.address <= functions[index][1]:
                owner = f"{functions[index][0]:08x}:{functions[index][2]}"
            print(f"0x{instruction.address:08X}\t{owner}\t{','.join(hex(v) for v in values)}\t{instruction.mnemonic}\t{instruction.op_str}")


if __name__ == "__main__":
    main()
