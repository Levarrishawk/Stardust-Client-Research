#!/usr/bin/env python3
"""Inventory literal named-message checks in Stardust.exe's GroundScene listener.

Read-only helper.  This does not claim handler semantics; it records stable branch
sites and the literal names supplied to the client's hash/gate helpers.
"""

import argparse
import struct
import sys

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM


def c_string(pe, va, limit=160):
    try:
        off = pe.get_offset_from_rva(va - pe.OPTIONAL_HEADER.ImageBase)
    except Exception:
        return None
    raw = pe.__data__[off:off + limit]
    end = raw.find(b"\0")
    if end < 4:
        return None
    raw = raw[:end]
    if any(b < 0x20 or b > 0x7e for b in raw):
        return None
    return raw.decode("ascii")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("image")
    ap.add_argument("--start", default="0x0051BD10")
    ap.add_argument("--end", default="0x0051E5A0")
    ap.add_argument("--all-text", action="store_true", help="scan every executable code section")
    args = ap.parse_args()

    pe = pefile.PE(args.image, fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    md.skipdata = True
    if args.all_text:
        ins = []
        for section in pe.sections:
            if not section.Characteristics & 0x20000000:
                continue
            start = base + section.VirtualAddress
            code = section.get_data()[:section.Misc_VirtualSize]
            ins.extend(md.disasm(code, start))
    else:
        start, end = int(args.start, 0), int(args.end, 0)
        off = pe.get_offset_from_rva(start - base)
        code = pe.__data__[off:off + end - start]
        ins = list(md.disasm(code, start))

    rows = []
    for i, op in enumerate(ins):
        if op.mnemonic != "push" or len(op.operands) != 1 or op.operands[0].type != X86_OP_IMM:
            continue
        target = op.operands[0].imm & 0xffffffff
        name = c_string(pe, target)
        if not name:
            continue
        call = next((x for x in ins[i + 1:i + 5] if x.mnemonic == "call"), None)
        if not call or len(call.operands) != 1 or call.operands[0].type != X86_OP_IMM:
            continue
        callee = call.operands[0].imm & 0xffffffff
        if callee not in (0x00AC4D10, 0x0051E670):
            continue
        rows.append((op.address, name, callee))

    print("BRANCH_VA  HELPER      MESSAGE_NAME")
    for va, name, helper in rows:
        print(f"{va:08X}   {helper:08X}   {name}")
    print(f"\ncount={len(rows)}")


if __name__ == "__main__":
    sys.exit(main())
