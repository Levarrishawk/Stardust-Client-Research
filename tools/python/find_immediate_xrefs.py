#!/usr/bin/env python3
"""Find little-endian 32-bit immediate references and enclosing inventory functions."""
from __future__ import annotations

import argparse
import bisect
import csv
import struct

import pefile


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("inventory")
parser.add_argument("addresses", nargs="+", type=lambda value: int(value, 0))
args = parser.parse_args()

functions = []
with open(args.inventory, encoding="utf-8") as stream:
    for row in csv.DictReader(stream, delimiter="\t"):
        functions.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
functions.sort()
starts = [row[0] for row in functions]

pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    section_va = base + section.VirtualAddress
    data = section.get_data()
    for target in args.addresses:
        needle = struct.pack("<I", target)
        offset = data.find(needle)
        while offset >= 0:
            va = section_va + offset
            index = bisect.bisect_right(starts, va) - 1
            owner = ""
            if index >= 0 and va < functions[index][1]:
                owner = f"{functions[index][0]:08X}\t{functions[index][2]}"
            print(f"{target:08X}\t{va:08X}\t{owner}")
            offset = data.find(needle, offset + 1)
