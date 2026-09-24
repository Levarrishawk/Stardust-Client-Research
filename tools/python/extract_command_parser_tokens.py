#!/usr/bin/env python3
"""Inventory literal-token comparisons in an x86 PE function.

This is a discovery aid, not a control-flow proof: inspect each reported branch.
"""
import argparse
import collections
import struct

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

p = argparse.ArgumentParser()
p.add_argument("image")
p.add_argument("start", type=lambda x: int(x, 0))
p.add_argument("end", type=lambda x: int(x, 0))
p.add_argument("compare", type=lambda x: int(x, 0), help="compare helper VA")
a = p.parse_args()
pe = pefile.PE(a.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = pe.get_memory_mapped_image()
code = data[a.start - base:a.end - base]
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.skipdata = True
history = collections.deque(maxlen=8)

def pointer_text(location):
    offset = location - base
    if offset < 0 or offset + 4 > len(data):
        return None
    target = struct.unpack_from("<I", data, offset)[0]
    target_offset = target - base
    if target_offset < 0 or target_offset >= len(data):
        return None
    raw = data[target_offset:target_offset + 96].split(b"\0", 1)[0]
    if not raw or any(byte < 32 or byte > 126 for byte in raw):
        return None
    return target, raw.decode("ascii")

for ins in md.disasm(code, a.start):
    if ins.mnemonic == "call" and ins.op_str == hex(a.compare):
        candidate = None
        for prior in reversed(history):
            if prior.mnemonic == "mov" and prior.op_str.startswith(("edx, dword ptr [0x", "ecx, dword ptr [0x", "eax, dword ptr [0x")):
                address_text = prior.op_str.split("[", 1)[1].split("]", 1)[0]
                candidate = pointer_text(int(address_text, 16))
                if candidate:
                    break
        label = f"{candidate[0]:08X}\t{candidate[1]}" if candidate else "?\t?"
        print(f"{ins.address:08X}\t{label}")
    history.append(ins)
