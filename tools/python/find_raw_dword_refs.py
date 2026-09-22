#!/usr/bin/env python3
"""Find byte offsets containing selected little-endian dword values."""
import argparse
import struct

import pefile


p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("targets", nargs="+", type=lambda value: int(value, 0))
a = p.parse_args()
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    data = section.get_data()
    for target in a.targets:
        needle = struct.pack("<I", target & 0xFFFFFFFF)
        offset = 0
        while True:
            offset = data.find(needle, offset)
            if offset < 0:
                break
            print(f"0x{base + section.VirtualAddress + offset:08X}\t{section.Name.rstrip(bytes([0])).decode(errors='replace')}\t0x{target:08X}")
            offset += 1
