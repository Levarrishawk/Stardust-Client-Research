#!/usr/bin/env python3
"""Find direct near-call references by scanning raw E8 rel32 encodings.

Every candidate is decoded independently, avoiding loss of synchronization when
linear disassembly crosses embedded data or an unrecognized instruction.
"""
import argparse
import bisect
import csv
import struct

import pefile


p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("inventory")
p.add_argument("targets", nargs="+", type=lambda value: int(value, 0))
a = p.parse_args()

functions = []
with open(a.inventory, encoding="utf-8") as handle:
    for row in csv.DictReader(handle, delimiter="\t"):
        functions.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
starts = [row[0] for row in functions]
targets = set(a.targets)

pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    if not section.Characteristics & 0x20000000:
        continue
    data = section.get_data()
    section_va = base + section.VirtualAddress
    for offset in range(len(data) - 4):
        if data[offset] != 0xE8:
            continue
        address = section_va + offset
        displacement = struct.unpack_from("<i", data, offset + 1)[0]
        target = (address + 5 + displacement) & 0xFFFFFFFF
        if target not in targets:
            continue
        index = bisect.bisect_right(starts, address) - 1
        owner = ""
        if index >= 0 and address <= functions[index][1]:
            owner = f"{functions[index][0]:08X}:{functions[index][2]}"
        print(f"0x{address:08X}\t{owner}\t0x{target:08X}")
