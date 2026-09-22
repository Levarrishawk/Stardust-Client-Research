#!/usr/bin/env python3
"""List printable C-string immediates referenced by an x86 VA range."""
from __future__ import annotations

import argparse
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM


p = argparse.ArgumentParser()
p.add_argument("image")
p.add_argument("start", type=lambda value: int(value, 0))
p.add_argument("end", type=lambda value: int(value, 0))
p.add_argument("--minimum", type=int, default=4)
a = p.parse_args()

pe = pefile.PE(a.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
code = pe.get_data(a.start - base, a.end - a.start)
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
md.skipdata = True

seen = set()
for instruction in md.disasm(code, a.start):
    for operand in instruction.operands:
        if operand.type != X86_OP_IMM:
            continue
        va = operand.imm & 0xFFFFFFFF
        raw = pe.get_data(va - base, 512) if base <= va < base + pe.OPTIONAL_HEADER.SizeOfImage else b""
        value = raw.split(b"\0", 1)[0]
        if len(value) < a.minimum or any(byte < 32 or byte >= 127 for byte in value):
            continue
        key = (va, value)
        if key in seen:
            continue
        seen.add(key)
        print(f"{instruction.address:08X}\t{va:08X}\t{value.decode('ascii')}")
