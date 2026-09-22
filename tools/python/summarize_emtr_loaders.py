#!/usr/bin/env python3
"""Summarize persistent member activity in Stardust EMTR version loaders."""
from __future__ import annotations

import argparse
import re

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32


LOADERS = {
    "0009": (0x007BBB70, 0x007BC030),
    "0010": (0x007BC030, 0x007BC5A0),
    "0011": (0x007BC5A0, 0x007BCB20),
    "0012": (0x007BCB20, 0x007BD140),
    "0013": (0x007BD140, 0x007BD780),
    "0014": (0x007BD780, 0x007BDDD0),
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("exe")
    args = parser.parse_args()
    pe = pefile.PE(args.exe, fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    image = pe.get_memory_mapped_image()
    md = Cs(CS_ARCH_X86, CS_MODE_32)

    for version, (start, end) in LOADERS.items():
        data = image[start - base:end - base]
        print(f"[{version}] 0x{start:08X}-0x{end:08X}")
        last_member = None
        for ins in md.disasm(data, start):
            text = f"{ins.mnemonic} {ins.op_str}"
            member = re.search(r"\[edi \+ (0x[0-9a-f]+)\]", ins.op_str)
            if member:
                last_member = member.group(1)
            direct_call = re.fullmatch(r"call (0x[0-9a-f]+)", text)
            if direct_call and direct_call.group(1) in {
                "0x7c0380", "0xabfaa0", "0xa94d00", "0xa94d90",
                "0xa94f00", "0xa95270", "0x7b8ff0", "0x7c5d30", "0x7be8c0",
            }:
                print(f"  {ins.address:08X} {text:<22} member={last_member or '-'}")
            if member and ins.mnemonic in {"mov", "lea"}:
                print(f"  {ins.address:08X} {text}")
        print()


if __name__ == "__main__":
    main()
