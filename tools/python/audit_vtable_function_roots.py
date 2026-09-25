#!/usr/bin/env python3
"""Audit known vtable-export candidate roots against the function inventory.

This is a *sample*, not a comprehensive vtable discovery or proof of runtime
object instantiation. The TSV files have heterogeneous rows (some are type
descriptors, some contain addresses from other modules). Only an exact
second-column address matching a Ghidra entry is counted. A match is still a
candidate and must be reviewed before promoting it to a confirmed vtable slot.
"""
from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from pathlib import Path


def address(value: str) -> int | None:
    value = value.strip()
    if not value:
        return None
    try:
        return int(value, 16)
    except ValueError:
        return None


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("inventory", type=Path)
    parser.add_argument("registry", type=Path)
    parser.add_argument("vtable_directory", type=Path)
    args = parser.parse_args()

    with args.inventory.open(encoding="utf-8", newline="") as stream:
        entries = {int(row["entry"], 16): row for row in csv.DictReader(stream, delimiter="\t")}
    with args.registry.open(encoding="utf-8-sig", newline="") as stream:
        curated = {int(row["address"], 16) for row in csv.DictReader(stream)}

    roots: dict[int, set[str]] = defaultdict(set)
    rows = Counter()
    for path in sorted(args.vtable_directory.glob("*vtable*.tsv")):
        with path.open(encoding="utf-8", errors="replace") as stream:
            for line in stream:
                fields = line.rstrip("\r\n").split("\t")
                rows["all"] += 1
                if len(fields) < 2 or (target := address(fields[1])) is None:
                    rows["unparsed"] += 1
                    continue
                if target not in entries:
                    rows["no_exact_entry"] += 1
                    continue
                roots[target].add(path.name)
                rows["matched"] += 1

    bins = Counter()
    for target, sources in roots.items():
        row = entries[target]
        if row["name"].startswith("Unwind@"):
            group = "unwind"
        elif row["thunk"].lower() == "true":
            group = "ghidra_thunk"
        elif target in curated:
            group = "curated"
        elif row["name"].startswith("FUN_") and int(row["size"]) > 64:
            group = "generic_65plus_uncurated"
        else:
            group = "other_nonunwind"
        bins[group] += 1
        if len(sources) > 1:
            bins["in_multiple_exports"] += 1

    print("source_file\trows\tunique_exact_targets\tcurated_targets\tgeneric_65plus_uncurated")
    for path in sorted(args.vtable_directory.glob("*vtable*.tsv")):
        found = {target for target, sources in roots.items() if path.name in sources}
        generic = sum(entries[target]["name"].startswith("FUN_") and int(entries[target]["size"]) > 64 and target not in curated for target in found)
        with path.open(encoding="utf-8", errors="replace") as stream:
            count = sum(1 for _ in stream)
        print(f"{path.name}\t{count}\t{len(found)}\t{len(found & curated)}\t{generic}")
    print("SUMMARY")
    for key in sorted(rows):
        print(f"{key}\t{rows[key]}")
    print(f"unique_exact_roots\t{len(roots)}")
    for key in sorted(bins):
        print(f"{key}\t{bins[key]}")


if __name__ == "__main__":
    main()
