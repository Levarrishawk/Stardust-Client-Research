#!/usr/bin/env python3
"""Triage linear-sweep x86 instructions using a displacement; absence is not evidence."""

from __future__ import annotations

import argparse

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_MEM


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image")
    parser.add_argument("displacement", type=lambda value: int(value, 0))
    args = parser.parse_args()
    pe = pefile.PE(args.image, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    md.skipdata = True
    base = pe.OPTIONAL_HEADER.ImageBase
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        start = base + section.VirtualAddress
        for instruction in md.disasm(section.get_data(), start):
            if instruction.id == 0:
                continue
            if any(op.type == X86_OP_MEM and op.mem.disp == args.displacement for op in instruction.operands):
                print(f"0x{instruction.address:08X}\t{instruction.mnemonic}\t{instruction.op_str}")


if __name__ == "__main__":
    main()
