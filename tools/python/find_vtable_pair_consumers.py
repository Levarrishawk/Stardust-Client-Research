#!/usr/bin/env python3
"""Find functions containing indirect calls through vtable slots 0 and 4."""
from __future__ import annotations

import bisect
import csv
import sys

from capstone import CS_ARCH_X86, CS_MODE_32, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_INVALID
import pefile

image, inventory = sys.argv[1:3]
pe = pefile.PE(image)
base = pe.OPTIONAL_HEADER.ImageBase
text = next(s for s in pe.sections if s.Name.rstrip(b"\0") == b".text")
data = text.get_data()
start = base + text.VirtualAddress
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
md.skipdata = True

with open(inventory, newline="", encoding="utf-8") as handle:
    rows = list(csv.DictReader(handle, delimiter="\t"))
starts = [int(row["entry"], 16) for row in rows]
by_function: dict[int, list[tuple[int, int, int]]] = {}
for ins in md.disasm(data, start):
    if ins.mnemonic != "call" or not ins.operands or ins.operands[0].type != X86_OP_MEM:
        continue
    mem = ins.operands[0].mem
    if mem.base != X86_REG_INVALID and mem.index == X86_REG_INVALID and mem.disp in (0, 4):
        index = bisect.bisect_right(starts, ins.address) - 1
        if index >= 0 and ins.address < int(rows[index]["end"], 16):
            by_function.setdefault(index, []).append((ins.address, mem.base, mem.disp))

for index, here in by_function.items():
    slots = {d for _, _, d in here}
    if slots == {0, 4}:
        row = rows[index]
        details = ",".join(f"{a:08X}:r{r}:s{d}" for a, r, d in here)
        print(f"{row['entry']}\t{row['end']}\t{row['name']}\t{details}")
