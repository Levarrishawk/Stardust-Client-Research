#!/usr/bin/env python3
"""List executable-section x86 instructions using selected memory displacements."""
from __future__ import annotations

import argparse
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32, CS_OP_MEM


p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("--start", type=lambda value: int(value, 0))
p.add_argument("--end", type=lambda value: int(value, 0))
p.add_argument("offset", nargs="+", type=lambda value: int(value, 0))
a = p.parse_args()
wanted = set(a.offset)
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
md.skipdata = True
for section in pe.sections:
    if not (section.Characteristics & 0x20000000):
        continue
    va = base + section.VirtualAddress
    data = section.get_data()
    begin = max(0, (a.start or va) - va)
    finish = min(len(data), (a.end or (va + len(data))) - va)
    if finish <= begin:
        continue
    for ins in md.disasm(data[begin:finish], va + begin):
        if ins.id == 0:
            continue
        matches = []
        for operand in ins.operands:
            if operand.type == CS_OP_MEM and operand.mem.base and operand.mem.disp in wanted:
                matches.append(operand.mem.disp)
        if matches:
            print(f"{ins.address:08X}\t{','.join(hex(v) for v in matches)}\t{ins.mnemonic}\t{ins.op_str}")
