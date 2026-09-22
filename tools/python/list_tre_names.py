#!/usr/bin/env python3
"""List TRE record names matching a substring and optional suffix."""
import argparse
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records


parser = argparse.ArgumentParser()
parser.add_argument("root", type=Path)
parser.add_argument("contains")
parser.add_argument("--suffix", default="")
args = parser.parse_args()

for archive in sorted(args.root.glob("*.tre")):
    for name, _, _, _ in archive_records(archive):
        if args.contains.lower() in name and name.endswith(args.suffix.lower()):
            print(f"{archive.name}\t{name}")
