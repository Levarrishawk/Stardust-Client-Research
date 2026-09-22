#!/usr/bin/env python3
"""Print line-oriented context around strings in one text payload inside TRE files."""
import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


parser = argparse.ArgumentParser()
parser.add_argument("root", type=Path)
parser.add_argument("record")
parser.add_argument("needles", nargs="+")
parser.add_argument("--context", type=int, default=4)
args = parser.parse_args()

for archive in sorted(args.root.glob("*.tre")):
    for name, usize, kind, packed in archive_records(archive):
        if name != args.record.lower().replace("\\", "/"):
            continue
        lines = expand(kind, packed, usize).decode("utf-8", "replace").splitlines()
        print(f"ARCHIVE={archive.name} RECORD={name} LINES={len(lines)}")
        selected = set()
        for index, line in enumerate(lines):
            if any(needle.lower() in line.lower() for needle in args.needles):
                selected.update(range(max(0, index - args.context), min(len(lines), index + args.context + 1)))
        previous = -2
        for index in sorted(selected):
            if index != previous + 1:
                print("--")
            print(f"{index + 1}: {lines[index]}")
            previous = index
