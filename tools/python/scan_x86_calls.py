#!/usr/bin/env python3
"""List direct x86 call sites for selected virtual-address targets."""
from __future__ import annotations

import argparse
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32, CS_GRP_CALL, CS_OP_IMM

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("target", nargs="+", type=lambda value: int(value, 0))
a = p.parse_args()
wanted = set(a.target)
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
md.skipdata = True
for section in pe.sections:
    if not (section.Characteristics & 0x20000000):
        continue
    va = base + section.VirtualAddress
    for ins in md.disasm(section.get_data(), va):
        if ins.id == 0:
            continue
        if not ins.group(CS_GRP_CALL) or not ins.operands:
            continue
        operand = ins.operands[0]
        if operand.type == CS_OP_IMM and operand.imm in wanted:
            print(f"{ins.address:08X}\t{operand.imm:08X}\t{ins.mnemonic}\t{ins.op_str}")
