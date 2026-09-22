#!/usr/bin/env python3
"""Count nested EMGP/EMTR versions in effective configured particle assets."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


def walk(data: bytes, start: int, end: int, counts: Counter[tuple[str, str]]) -> None:
    pos = start
    while pos + 8 <= end:
        kind = data[pos:pos + 4]
        size = struct.unpack_from(">I", data, pos + 4)[0]
        item_end = pos + 8 + size
        if item_end > end or item_end < pos + 8:
            return
        if kind == b"FORM" and size >= 4:
            form_tag = data[pos + 8:pos + 12].decode("ascii", "replace")
            child = pos + 12
            if form_tag in {"EMGP", "EMTR"} and child + 12 <= item_end and data[child:child + 4] == b"FORM":
                version = data[child + 8:child + 12].decode("ascii", "replace")
                counts[(form_tag, version)] += 1
            walk(data, child, item_end, counts)
        pos = item_end + (size & 1)


def main(root_value: str, cfg_value: str, output_value: str) -> None:
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))
    winners: set[str] = set()
    counts: Counter[tuple[str, str]] = Counter()
    live = tombstones = errors = 0
    for _, archive_name in sorted(configured, reverse=True):
        archive = root / archive_name
        if not archive.exists():
            continue
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not name.endswith(".prt"):
                continue
            if usize == 0 or not packed:
                tombstones += 1
                continue
            try:
                data = expand(kind, packed, usize)
                walk(data, 0, len(data), counts)
                live += 1
            except Exception:
                errors += 1
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("form", "version", "occurrences"))
        for (form, version), count in sorted(counts.items()):
            writer.writerow((form, version, count))
    print(f"live={live} tombstones={tombstones} errors={errors}")
    for (form, version), count in sorted(counts.items()):
        print(form, version, count, sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:4])
