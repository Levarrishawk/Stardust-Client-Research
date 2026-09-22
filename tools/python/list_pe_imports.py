#!/usr/bin/env python3
"""Print PE imports with IAT virtual addresses."""
from __future__ import annotations

import argparse

import pefile


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("--dll")
args = parser.parse_args()

pe = pefile.PE(args.image)
for descriptor in pe.DIRECTORY_ENTRY_IMPORT:
    dll = descriptor.dll.decode("ascii", errors="replace")
    if args.dll and args.dll.lower() not in dll.lower():
        continue
    for entry in descriptor.imports:
        name = entry.name.decode("ascii", errors="replace") if entry.name else f"ordinal:{entry.ordinal}"
        print(f"{entry.address:08X}\t{dll}\t{name}")
