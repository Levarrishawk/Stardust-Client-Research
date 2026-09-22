#!/usr/bin/env python3
"""Build Ghidra's address/name import from stable identifiers in research Markdown."""

from __future__ import annotations

import argparse
import csv
import re
from collections import defaultdict
from pathlib import Path


NAME_VA = re.compile(r"\b([A-Za-z][A-Za-z0-9_]*_VA_([0-9A-Fa-f]{8}))\b")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("research_root", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    found: dict[int, set[str]] = defaultdict(set)
    for path in sorted(args.research_root.glob("*.md")):
        text = path.read_text(encoding="utf-8", errors="replace")
        for name, address in NAME_VA.findall(text):
            found[int(address, 16)].add(name)

    conflicts = {address: names for address, names in found.items() if len(names) > 1}
    if conflicts:
        for address, names in sorted(conflicts.items()):
            print(f"conflict 0x{address:08X}: {', '.join(sorted(names))}")
        raise SystemExit("resolve stable-name conflicts before import")

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream, lineterminator="\n")
        writer.writerow(("address", "name"))
        for address, names in sorted(found.items()):
            writer.writerow((f"0x{address:08X}", next(iter(names))))
    print(f"wrote {len(found)} stable names to {args.output}")


if __name__ == "__main__":
    main()
