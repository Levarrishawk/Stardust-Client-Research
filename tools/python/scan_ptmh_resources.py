#!/usr/bin/env python3
"""Inventory PTMH/0000 appearance-template references in effective .prt files."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


def items(data: bytes, start: int, end: int):
    pos = start
    while pos + 8 <= end:
        size = struct.unpack_from(">I", data, pos + 4)[0]
        stop = pos + 8 + size
        if stop > end or stop < pos + 8:
            return
        yield pos, data[pos:pos + 4], size, stop
        pos = stop


def walk(data: bytes, start: int, end: int, asset: str, rows, errors):
    for pos, kind, size, stop in items(data, start, end):
        if kind != b"FORM" or size < 4:
            continue
        tag = data[pos + 8:pos + 12]
        child = pos + 12
        if tag == b"PTMH":
            chunks = [(p, e) for p, k, _, e in items(data, child, stop) if k == b"0000"]
            if not chunks:
                errors.append((asset, pos, "missing PTMH/0000"))
            else:
                p, e = chunks[0]
                raw = data[p + 8:e]
                nul = raw.find(b"\0")
                if nul < 0:
                    errors.append((asset, pos, "unterminated PTMH resource"))
                else:
                    rows.append((asset, pos, raw[:nul].decode("ascii", "replace")))
        walk(data, child, stop, asset, rows, errors)


def main(root_value: str, cfg_value: str, output_value: str) -> None:
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))
    winners, rows, errors = set(), [], []
    for _, archive_name in sorted(configured, reverse=True):
        archive = root / archive_name
        if not archive.exists():
            continue
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not name.endswith(".prt") or not usize or not packed:
                continue
            try:
                data = expand(kind, packed, usize)
                walk(data, 0, len(data), name, rows, errors)
            except Exception as exc:
                errors.append((name, 0, str(exc)))
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("particle_asset", "ptmh_offset", "appearance_template"))
        writer.writerows(rows)
    counts = Counter(row[2] for row in rows)
    print(f"records={len(rows)} unique={len(counts)} errors={len(errors)}")
    for value, count in counts.most_common(40):
        print(count, value, sep="\t")
    for error in errors[:20]:
        print("ERROR", *error, sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:4])
