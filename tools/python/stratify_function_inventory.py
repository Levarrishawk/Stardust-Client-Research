#!/usr/bin/env python3
"""Stratify a Ghidra function export without treating it as semantic coverage.

This is a discovery/denominator aid. Size, name and Ghidra's thunk flag do not
prove runtime reachability, compiler origin, or whether a routine needs its own
replacement contract.
"""
from __future__ import annotations

import argparse
import csv
from collections import Counter
from pathlib import Path


def size_bin(size: int) -> str:
    if size <= 4:
        return "1-4"
    if size <= 16:
        return "5-16"
    if size <= 64:
        return "17-64"
    return "65+"


def name_bin(name: str) -> str:
    if name.startswith("Unwind@"):
        return "unwind_prefix"
    if name.startswith("FUN_"):
        return "generic_FUN"
    return "non_generic_name"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("inventory", type=Path)
    parser.add_argument("registry", type=Path)
    args = parser.parse_args()

    with args.registry.open(encoding="utf-8-sig", newline="") as stream:
        registry = {int(row["address"], 16) for row in csv.DictReader(stream)}

    bins: Counter[tuple[str, str, str, str]] = Counter()
    totals: Counter[str] = Counter()
    seen: set[int] = set()
    with args.inventory.open(encoding="utf-8", newline="") as stream:
        for row in csv.DictReader(stream, delimiter="\t"):
            entry = int(row["entry"], 16)
            if entry in seen:
                raise ValueError(f"duplicate Ghidra entry {entry:#x}")
            seen.add(entry)
            category = name_bin(row["name"])
            size = size_bin(int(row["size"]))
            thunk = row["thunk"].strip().lower()
            if thunk not in ("true", "false"):
                raise ValueError(f"unexpected thunk value: {thunk!r}")
            curated = "curated" if entry in registry else "not_curated"
            bins[(category, thunk, size, curated)] += 1
            totals[category] += 1

    print("name_category\tghidra_thunk\tsize_bytes\tcurated_entry\tcount")
    for key, count in sorted(bins.items()):
        print("\t".join((*key, str(count))))
    print(f"TOTAL\t{len(seen)}")
    print(f"NON_UNWIND\t{len(seen) - totals['unwind_prefix']}")
    print(f"CURATED_ENTRY_INTERSECTION\t{len(seen & registry)}")
    print(f"CURATED_OUTSIDE_ENTRY_SET\t{len(registry - seen)}")


if __name__ == "__main__":
    main()
