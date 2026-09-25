#!/usr/bin/env python3
"""Count heuristic direct-call and non-code-pointer references to Ghidra entries.

Raw E8 scans can match bytes inside another instruction or embedded code data.
Aligned non-executable dwords can resemble function pointers accidentally.
Zero matches do NOT prove a function unreachable (indirect/dynamic paths exist).
"""
from __future__ import annotations

import argparse
import csv
import struct
from collections import Counter
from pathlib import Path

import pefile


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("inventory", type=Path)
    parser.add_argument("registry", type=Path)
    args = parser.parse_args()

    with args.inventory.open(encoding="utf-8", newline="") as stream:
        functions = {
            int(row["entry"], 16): row
            for row in csv.DictReader(stream, delimiter="\t")
            if not row["name"].startswith("Unwind@")
        }
    with args.registry.open(encoding="utf-8-sig", newline="") as stream:
        curated = {int(row["address"], 16) for row in csv.DictReader(stream)}

    direct: Counter[int] = Counter()
    pointer: Counter[int] = Counter()
    image = pefile.PE(str(args.image), fast_load=True)
    base = image.OPTIONAL_HEADER.ImageBase
    for section in image.sections:
        data = section.get_data()
        start = base + section.VirtualAddress
        if section.Characteristics & 0x20000000:
            offset = data.find(b"\xe8")
            while 0 <= offset <= len(data) - 5:
                displacement = struct.unpack_from("<i", data, offset + 1)[0]
                target = (start + offset + 5 + displacement) & 0xFFFFFFFF
                if target in functions:
                    direct[target] += 1
                offset = data.find(b"\xe8", offset + 1)
        else:
            for offset in range((-start) % 4, len(data) - 3, 4):
                target = struct.unpack_from("<I", data, offset)[0]
                if target in functions:
                    pointer[target] += 1

    print("stratum\tfunctions\twith_raw_E8_candidate\twith_aligned_noncode_pointer\twith_either")
    totals: Counter[str] = Counter()
    for address, row in functions.items():
        size = int(row["size"])
        if row["thunk"].lower() == "true":
            stratum = "ghidra_thunk"
        elif row["name"].startswith("FUN_") and size > 64 and address not in curated:
            stratum = "generic_65plus_uncurated"
        elif address in curated:
            stratum = "curated_entry"
        else:
            stratum = "other_nonunwind"
        totals[stratum + ":all"] += 1
        if direct[address]:
            totals[stratum + ":direct"] += 1
        if pointer[address]:
            totals[stratum + ":pointer"] += 1
        if direct[address] or pointer[address]:
            totals[stratum + ":either"] += 1
    for stratum in ("ghidra_thunk", "generic_65plus_uncurated", "curated_entry", "other_nonunwind"):
        print("\t".join((stratum, *(str(totals[stratum + ":" + field]) for field in ("all", "direct", "pointer", "either")))))
    print(f"raw_E8_references_to_nonunwind_entries\t{sum(direct.values())}")
    print(f"aligned_noncode_pointers_to_nonunwind_entries\t{sum(pointer.values())}")


if __name__ == "__main__":
    main()
