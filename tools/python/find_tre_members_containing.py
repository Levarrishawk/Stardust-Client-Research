#!/usr/bin/env python3
"""List effective/all TRE members whose expanded bytes contain requested ASCII strings."""
import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand

parser = argparse.ArgumentParser()
parser.add_argument("root", type=Path)
parser.add_argument("needles", nargs="+")
args = parser.parse_args()
needles = [(n, n.encode("ascii")) for n in args.needles]

for archive in sorted(args.root.glob("*.tre")):
    for name, usize, kind, packed in archive_records(archive):
        try:
            data = expand(kind, packed, usize)
        except ValueError:
            continue
        hits = [name_text for name_text, raw in needles if raw in data]
        if hits:
            print(f"{archive.name}\t{name}\t{','.join(hits)}")
