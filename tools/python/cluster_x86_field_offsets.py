"""Group x86 memory operands with selected displacements by containing function.

This is a candidate filter, not a type/ownership proof. Section-wide linear
disassembly can encounter false instruction boundaries; inspect any hit in a
real function's control flow before assigning semantics.
"""

import argparse
import bisect
import csv
from collections import defaultdict

import pefile
from capstone import CS_ARCH_X86, CS_MODE_32, Cs
from capstone.x86 import X86_OP_MEM


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("inventory")
parser.add_argument("offsets", nargs="+", type=lambda value: int(value, 0))
parser.add_argument("--minimum", type=int, default=2)
args = parser.parse_args()
wanted = set(args.offsets)

functions = []
with open(args.inventory, encoding="utf-8", newline="") as handle:
    for row in csv.DictReader(handle, delimiter="\t"):
        functions.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
functions.sort()
starts = [entry for entry, _, _ in functions]
hits = defaultdict(list)
decoder = Cs(CS_ARCH_X86, CS_MODE_32)
decoder.detail = True
decoder.skipdata = True
pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    if not (section.Characteristics & 0x20000000):
        continue
    for insn in decoder.disasm(section.get_data(), base + section.VirtualAddress):
        if insn.id == 0:
            continue
        matched = {op.mem.disp for op in insn.operands if op.type == X86_OP_MEM and op.mem.disp in wanted}
        if not matched:
            continue
        idx = bisect.bisect_right(starts, insn.address) - 1
        if idx < 0 or insn.address >= functions[idx][1]:
            continue
        for offset in matched:
            hits[idx].append((offset, insn.address, insn.mnemonic, insn.op_str))

for idx in sorted(hits):
    rows = hits[idx]
    distinct = {offset for offset, *_ in rows}
    if len(distinct) < args.minimum:
        continue
    entry, _, name = functions[idx]
    print(f"{entry:08X}\t{name}\toffsets={','.join(hex(x) for x in sorted(distinct))}\thits={len(rows)}")
    for offset, va, mnemonic, operands in rows:
        print(f"  {va:08X}\t{offset:#x}\t{mnemonic} {operands}")
