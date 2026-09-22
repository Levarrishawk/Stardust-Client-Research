import argparse, bisect, csv
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("inventory")
p.add_argument("targets", nargs="+", type=lambda s: int(s, 0))
a = p.parse_args()
funcs=[]
with open(a.inventory, encoding="utf-8") as f:
    for r in csv.DictReader(f, delimiter="\t"):
        funcs.append((int(r["entry"],16), int(r["end"],16), r["name"]))
starts=[x[0] for x in funcs]; targets=set(a.targets)
pe=pefile.PE(a.exe, fast_load=True); base=pe.OPTIONAL_HEADER.ImageBase
md=Cs(CS_ARCH_X86, CS_MODE_32); md.detail=True; md.skipdata=True
for sec in pe.sections:
    if not sec.Characteristics & 0x20000000: continue
    for ins in md.disasm(sec.get_data(), base+sec.VirtualAddress):
        if ins.mnemonic != "call" or not ins.operands or ins.operands[0].type != X86_OP_IMM: continue
        target=ins.operands[0].imm & 0xffffffff
        if target not in targets: continue
        i=bisect.bisect_right(starts, ins.address)-1
        owner=""
        if i>=0 and ins.address <= funcs[i][1]: owner=f"{funcs[i][0]:08X}:{funcs[i][2]}"
        print(f"0x{ins.address:08X}\t{owner}\t0x{target:08X}")
