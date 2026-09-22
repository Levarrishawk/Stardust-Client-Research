#!/usr/bin/env python3
"""List normal and delay-load PE imports containing selected substrings."""
import argparse

import pefile


p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("patterns", nargs="+")
a = p.parse_args()
patterns = [value.lower() for value in a.patterns]
pe = pefile.PE(a.exe)
for attr in ("DIRECTORY_ENTRY_IMPORT", "DIRECTORY_ENTRY_DELAY_IMPORT"):
    for descriptor in getattr(pe, attr, []):
        dll = descriptor.dll.decode(errors="replace")
        for item in descriptor.imports:
            name = item.name.decode(errors="replace") if item.name else f"ordinal:{item.ordinal}"
            if any(pattern in name.lower() for pattern in patterns):
                print(f"{attr}\t{dll}\t0x{item.address:08X}\t{name}")
