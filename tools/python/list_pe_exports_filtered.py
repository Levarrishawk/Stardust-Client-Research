#!/usr/bin/env python3
"""List PE exports whose decorated or undecorated names contain substrings."""
import argparse
import pefile

p = argparse.ArgumentParser()
p.add_argument("image")
p.add_argument("patterns", nargs="*")
a = p.parse_args()
patterns = [item.lower() for item in a.patterns]
pe = pefile.PE(a.image)
directory = getattr(pe, "DIRECTORY_ENTRY_EXPORT", None)
if directory:
    for symbol in directory.symbols:
        name = symbol.name.decode(errors="replace") if symbol.name else ""
        if not patterns or any(pattern in name.lower() for pattern in patterns):
            print(f"0x{pe.OPTIONAL_HEADER.ImageBase + symbol.address:08X}\t{symbol.ordinal}\t{name}")
