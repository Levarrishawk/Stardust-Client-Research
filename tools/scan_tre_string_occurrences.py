#!/usr/bin/env python3
"""Count exact byte-string occurrences across TRE archives without extracting them."""
import argparse
import mmap
from pathlib import Path


parser = argparse.ArgumentParser()
parser.add_argument("root", type=Path)
parser.add_argument("needles", nargs="+")
args = parser.parse_args()

encoded = [(value, value.encode("ascii")) for value in args.needles]
totals = {value: 0 for value, _ in encoded}

for archive in sorted(args.root.glob("*.tre")):
    hits = {}
    with archive.open("rb") as handle:
        with mmap.mmap(handle.fileno(), 0, access=mmap.ACCESS_READ) as data:
            for value, needle in encoded:
                count = 0
                start = 0
                while True:
                    offset = data.find(needle, start)
                    if offset < 0:
                        break
                    count += 1
                    start = offset + 1
                if count:
                    hits[value] = count
                    totals[value] += count
    for value, count in hits.items():
        print(f"{archive.name}\t{count}\t{value}")

print("TOTALS")
for value, _ in encoded:
    print(f"{totals[value]}\t{value}")
