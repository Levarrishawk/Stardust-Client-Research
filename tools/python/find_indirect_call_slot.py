"""Find x86 FF /2 memory-indirect calls using a specific disp32 slot.

Discovery aid only: a matching offset does not identify the receiver type.
"""

import argparse
import bisect
import csv
import struct

import pefile


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("inventory")
parser.add_argument("slot", type=lambda value: int(value, 0))
args = parser.parse_args()

functions = []
with open(args.inventory, encoding="utf-8", newline="") as handle:
    for row in csv.DictReader(handle, delimiter="\t"):
        functions.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
functions.sort()
starts = [row[0] for row in functions]

pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
disp = struct.pack("<I", args.slot)
print("call_va\towner_entry\towner_name\tbytes")
for section in pe.sections:
    if not (section.Characteristics & 0x20000000):
        continue
    data = section.get_data()
    for reg in range(8):
        # FF /2, mod=10, rm=reg. ESP requires SIB and is not covered.
        if reg == 4:
            continue
        needle = bytes((0xFF, 0x90 | reg)) + disp
        offset = data.find(needle)
        while offset >= 0:
            va = base + section.VirtualAddress + offset
            index = bisect.bisect_right(starts, va) - 1
            owner = functions[index] if index >= 0 and va < functions[index][1] else None
            print(f"{va:08X}\t{owner[0]:08X}\t{owner[2]}\t{needle.hex()}") if owner else print(f"{va:08X}\t\t\t{needle.hex()}")
            offset = data.find(needle, offset + 1)
