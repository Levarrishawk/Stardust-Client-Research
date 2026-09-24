"""List x87 control-word instructions in PE executable sections.

Section-wide scan is a candidate inventory; confirm instruction boundaries and
reachability before inferring process-wide floating-point policy.
"""

import argparse
import bisect
import csv

import pefile
from capstone import CS_ARCH_X86, CS_MODE_32, Cs


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("inventory")
args = parser.parse_args()
functions = []
with open(args.inventory, encoding="utf-8", newline="") as handle:
    for row in csv.DictReader(handle, delimiter="\t"):
        functions.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
functions.sort()
starts = [row[0] for row in functions]
pe = pefile.PE(args.image, fast_load=True)
decoder = Cs(CS_ARCH_X86, CS_MODE_32)
decoder.skipdata = True
for section in pe.sections:
    if not (section.Characteristics & 0x20000000):
        continue
    for insn in decoder.disasm(section.get_data(), pe.OPTIONAL_HEADER.ImageBase + section.VirtualAddress):
        if insn.mnemonic not in ("fldcw", "fnstcw", "fstcw", "fclex", "fnclex"):
            continue
        idx = bisect.bisect_right(starts, insn.address) - 1
        owner = functions[idx] if idx >= 0 and insn.address < functions[idx][1] else None
        print(f"{insn.address:08X}\t{insn.mnemonic}\t{insn.op_str}\t{owner[0]:08X} {owner[2]}" if owner else f"{insn.address:08X}\t{insn.mnemonic}\t{insn.op_str}")
