import argparse, bisect, csv
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("inventory")
p.add_argument("targets", nargs="+", type=lambda s: int(s, 0))
a = p.parse_args()
funcs = []
with open(a.inventory, encoding="utf-8") as f:
    for row in csv.DictReader(f, delimiter="\t"):
        funcs.append((int(row["entry"], 16), int(row["end"], 16), row["name"]))
starts = [row[0] for row in funcs]
targets = {target & 0xffffffff for target in a.targets}
pe = pefile.PE(a.exe, fast_load=True)
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
md.skipdata = True
base = pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    if not section.Characteristics & 0x20000000:
        continue
    for ins in md.disasm(section.get_data(), base + section.VirtualAddress):
        if ins.id == 0:
            continue
        matches = [op.imm & 0xffffffff for op in ins.operands if op.type == X86_OP_IMM and (op.imm & 0xffffffff) in targets]
        if not matches:
            continue
        index = bisect.bisect_right(starts, ins.address) - 1
        owner = ""
        if index >= 0 and ins.address <= funcs[index][1]:
            owner = f"{funcs[index][0]:08X}:{funcs[index][2]}"
        print(f"0x{ins.address:08X}\t{owner}\t{','.join(hex(v) for v in matches)}\t{ins.mnemonic}\t{ins.op_str}")
