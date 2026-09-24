#!/usr/bin/env python3
"""Audit Ghidra entry categories against the curated address registry.

This is an inventory diagnostic, not a semantic or reachability completion score.
"""
from __future__ import annotations

import argparse
import bisect
import csv
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("inventory", type=Path, help="Ghidra TSV function export")
    parser.add_argument("registry", type=Path, help="Curated address/name CSV")
    args = parser.parse_args()

    with args.inventory.open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle, delimiter="\t"))
    inventory = {int(row["entry"], 16): row for row in rows}
    with args.registry.open(encoding="utf-8-sig", newline="") as handle:
        curated = list(csv.DictReader(handle))
    addresses = [int(row["address"], 16) for row in curated]

    def ordinary(row: dict[str, str]) -> bool:
        return not row["name"].startswith("Unwind@")

    print(f"ghidra_entries\t{len(rows)}")
    print(f"ghidra_unique_addresses\t{len(inventory)}")
    print(f"seh_unwind_entries\t{sum(not ordinary(row) for row in rows)}")
    print(f"non_unwind_entries\t{sum(ordinary(row) for row in rows)}")
    for minimum in (16, 64):
        print(
            f"non_unwind_size_gt_{minimum}\t"
            f"{sum(ordinary(row) and int(row['size']) > minimum for row in rows)}"
        )
    print(f"curated_rows\t{len(addresses)}")
    print(f"curated_unique_addresses\t{len(set(addresses))}")
    print(f"curated_in_ghidra_inventory\t{sum(address in inventory for address in addresses)}")
    print(
        f"curated_not_in_ghidra_inventory\t"
        f"{sum(address not in inventory for address in addresses)}"
    )
    print(
        f"curated_matching_unwind\t"
        f"{sum(address in inventory and not ordinary(inventory[address]) for address in addresses)}"
    )
    intervals = sorted((int(row["entry"], 16), int(row["end"], 16)) for row in rows)
    starts = [start for start, _ in intervals]
    absent = [address for address in addresses if address not in inventory]
    interior = 0
    for address in absent:
        index = bisect.bisect_right(starts, address) - 1
        if index >= 0 and address <= intervals[index][1]:
            interior += 1
    print(f"curated_absent_inside_inventory_interval\t{interior}")
    print(f"curated_absent_outside_inventory_interval\t{len(absent) - interior}")


if __name__ == "__main__":
    main()
