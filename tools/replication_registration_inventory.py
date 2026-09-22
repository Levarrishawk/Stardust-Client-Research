#!/usr/bin/env python3
"""Extract ordered ClientObject view registrations from saved Ghidra constructor pseudocode."""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path


ASSIGN = re.compile(r"param_1\[(0x[0-9a-f]+|[0-9]+)\]\s*=\s*&PTR_(?:FUN|LAB)_([0-9a-f]+)", re.I)
POINTER_ALIAS = re.compile(r"\b([A-Za-z][A-Za-z0-9_]*)\s*=\s*param_1\s*\+\s*(0x[0-9a-f]+|[0-9]+)\s*;", re.I)
POINTER_ASSIGN = re.compile(r"\*([A-Za-z][A-Za-z0-9_]*)\s*=\s*&PTR_(?:FUN|LAB)_([0-9a-f]+)\s*;", re.I)
REGISTER = re.compile(r"FUN_005554(20|40|60|80|a0|c0)\(([^;]+)\);", re.I)
DIRECT = re.compile(r"param_1\s*\+\s*(0x[0-9a-f]+|[0-9]+)$", re.I)
DYNAMIC = re.compile(r"param_1\[(0x[0-9a-f]+|[0-9]+)\]$", re.I)
VIEW = {"20": 1, "40": 4, "60": 8, "80": 9, "a0": 3, "c0": 6}


def number(raw: str) -> int:
    return int(raw, 0)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("constructor_dir", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    rows = []
    for path in sorted(args.constructor_dir.glob("*.c")):
        text = path.read_text(encoding="utf-8", errors="replace")
        entry = path.name[:8].upper()
        assignments = {number(index): vtable.upper() for index, vtable in ASSIGN.findall(text)}
        aliases: dict[str, int] = {}
        for line in text.splitlines():
            alias = POINTER_ALIAS.search(line)
            if alias:
                aliases[alias.group(1)] = number(alias.group(2))
                continue
            indirect = POINTER_ASSIGN.search(line)
            if indirect and indirect.group(1) in aliases:
                assignments[aliases[indirect.group(1)]] = indirect.group(2).upper()
        ordinals: dict[int, int] = {}
        for helper, expression in REGISTER.findall(text):
            view = VIEW[helper.lower()]
            ordinal = ordinals.get(view, 0)
            ordinals[view] = ordinal + 1
            expression = expression.strip()
            direct = DIRECT.fullmatch(expression)
            dynamic = DYNAMIC.fullmatch(expression)
            index = number(direct.group(1)) if direct else None
            rows.append({
                "constructor_va": f"0x{entry}",
                "view": view,
                "added_ordinal": ordinal,
                "field_expression": expression,
                "object_offset": f"0x{index * 4:X}" if index is not None else "dynamic",
                "vtable_va": f"0x{assignments[index]}" if index in assignments else "",
                "dynamic_pointer_member": f"0x{number(dynamic.group(1)) * 4:X}" if dynamic else "",
            })

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=rows[0].keys(), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
    print(f"wrote {len(rows)} registration rows to {args.output}")


if __name__ == "__main__":
    main()
