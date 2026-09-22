#!/usr/bin/env python3
"""Find CMP/TEST instructions using selected immediate in a bounded PE VA range."""
import argparse
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM

p=argparse.ArgumentParser(); p.add_argument('exe'); p.add_argument('value',type=lambda x:int(x,0)); p.add_argument('start',type=lambda x:int(x,0)); p.add_argument('end',type=lambda x:int(x,0)); a=p.parse_args()
pe=pefile.PE(a.exe,fast_load=True); md=Cs(CS_ARCH_X86,CS_MODE_32); md.detail=True; md.skipdata=True; base=pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    if not section.Characteristics & 0x20000000: continue
    for ins in md.disasm(section.get_data(),base+section.VirtualAddress):
        if not a.start<=ins.address<a.end or ins.mnemonic not in ('cmp','test'): continue
        if any(op.type==X86_OP_IMM and (op.imm&0xffffffff)==(a.value&0xffffffff) for op in ins.operands): print(f'0x{ins.address:08X}\t{ins.mnemonic}\t{ins.op_str}')
