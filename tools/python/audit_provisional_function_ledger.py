#!/usr/bin/env python3
"""Reproduce stable-address/confidence counts for UNKNOWN_FUNCTIONS.md."""
from __future__ import annotations

import argparse
from collections import Counter
from pathlib import Path
import re


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("ledger", type=Path)
    args = parser.parse_args()

    rows: list[tuple[int, str]] = []
    for line in args.ledger.read_text(encoding="utf-8").splitlines():
        if not line.startswith("|") or line.startswith("|---"):
            continue
        first_cell = line.split("|", 2)[1]
        match = re.search(r"0x([0-9A-Fa-f]{6,8})", first_cell)
        if match:
            rows.append((int(match.group(1), 16), line))

    address_counts = Counter(address for address, _ in rows)
    result = {
        "address_rows": len(rows),
        "distinct_primary_addresses": len(address_counts),
        "rows_containing_confirmed": sum("Confirmed" in line for _, line in rows),
        "rows_containing_strong": sum("Strong" in line for _, line in rows),
        "status_merely_unknown": sum(
            bool(re.search(r"\|\s*Unknown(?:\s|\|)", line, re.IGNORECASE))
            for _, line in rows
        ),
        "duplicated_historical_addresses": sum(
            count > 1 for count in address_counts.values()
        ),
    }
    for key, value in result.items():
        print(f"{key}\t{value}")
    print("duplicate_addresses")
    for address, count in sorted(address_counts.items()):
        if count > 1:
            print(f"0x{address:08X}\t{count}")


if __name__ == "__main__":
    main()
