#!/usr/bin/env python3
"""Find canonical `call dword ptr [reg+disp32]` byte patterns in PE sections."""
from __future__ import annotations

import argparse
import struct
import pefile


p = argparse.ArgumentParser()
p.add_argument("image")
p.add_argument("displacements", nargs="+", type=lambda value: int(value, 0))
a = p.parse_args()
pe = pefile.PE(a.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase

for section in pe.sections:
    data = section.get_data()
    for displacement in a.displacements:
        suffix = struct.pack("<I", displacement & 0xFFFFFFFF)
        for register in range(8):
            needle = bytes((0xFF, 0x90 + register)) + suffix
            start = 0
            while True:
                offset = data.find(needle, start)
                if offset < 0:
                    break
                va = base + section.VirtualAddress + offset
                print(f"{va:08X}\treg={register}\tdisp={displacement:#x}\t{section.Name.rstrip(bytes((0,))).decode(errors='replace')}")
                start = offset + 1
