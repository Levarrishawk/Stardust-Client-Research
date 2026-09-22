#!/usr/bin/env python3
import argparse
import pefile

p = argparse.ArgumentParser()
p.add_argument("image")
p.add_argument("start", type=lambda x: int(x, 0))
p.add_argument("end", type=lambda x: int(x, 0))
a = p.parse_args()
pe = pefile.PE(a.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = bytes(pe.__data__)

for section in pe.sections:
    va0 = base + section.VirtualAddress
    va1 = va0 + section.SizeOfRawData
    lo = max(a.start, va0)
    hi = min(a.end, va1)
    if lo >= hi:
        continue
    raw = section.PointerToRawData + lo - va0
    blob = data[raw:raw + hi - lo]
    i = 0
    while i < len(blob):
        if 0x20 <= blob[i] < 0x7f:
            j = i
            while j < len(blob) and 0x20 <= blob[j] < 0x7f:
                j += 1
            if j - i >= 3:
                print(f"{lo+i:08X}\t{blob[i:j].decode('ascii')}")
            i = j
        else:
            i += 1
