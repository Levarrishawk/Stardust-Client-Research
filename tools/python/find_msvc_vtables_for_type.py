#!/usr/bin/env python3
import argparse, struct
import pefile

p = argparse.ArgumentParser()
p.add_argument("image")
p.add_argument("type_descriptor", type=lambda x: int(x, 0))
a = p.parse_args()
pe = pefile.PE(a.image, fast_load=True)
blob = bytes(pe.__data__)
base = pe.OPTIONAL_HEADER.ImageBase

def off_to_va(off):
    for s in pe.sections:
        start = s.PointerToRawData
        end = start + s.SizeOfRawData
        if start <= off < end:
            return base + s.VirtualAddress + off - start
    return None

needle = struct.pack("<I", a.type_descriptor)
for pos in range(len(blob)):
    if blob.startswith(needle, pos):
        locator = off_to_va(pos - 12)
        if locator is None:
            continue
        loc_needle = struct.pack("<I", locator)
        q = 0
        while True:
            q = blob.find(loc_needle, q)
            if q < 0:
                break
            va = off_to_va(q)
            if va is not None:
                print(f"type=0x{a.type_descriptor:08x}\tlocator=0x{locator:08x}\tlocator_ref=0x{va:08x}\tvtable=0x{va+4:08x}")
            q += 1
