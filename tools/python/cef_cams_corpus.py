#!/usr/bin/env python3
"""Audit all live CEF CAMS float fields; keep per-asset data private."""
from __future__ import annotations

import json
import math
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def chunks(data: bytes):
    found = []
    def walk(start: int, end: int):
        p = start
        while p + 8 <= end:
            tag = data[p:p + 4]
            size = struct.unpack_from(">I", data, p + 4)[0]
            stop = p + 8 + size
            if stop > end or stop < p + 8:
                raise ValueError("invalid IFF boundary")
            if tag == b"FORM":
                if size < 4:
                    raise ValueError("short FORM")
                walk(p + 12, stop)
            elif tag == b"CAMS":
                if size != 16:
                    raise ValueError(f"CAMS size {size}")
                found.append(struct.unpack_from("<4f", data, p + 8))
            p = stop
        if p != end:
            raise ValueError("trailing IFF")
    walk(0, len(data))
    return found


def main(root: Path, cfg: Path, output: Path):
    archives = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            archives.append((int(match.group(1)), root / match.group(2)))
    winners = set()
    values = []
    errors = []
    for _, archive in sorted(archives, reverse=True):
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not name.endswith(".cef") or not usize:
                continue
            try:
                for record in chunks(expand(kind, packed, usize)):
                    values.append((name, record))
            except Exception as exc:
                errors.append((name, str(exc)))
    stats = []
    for index in range(4):
        column = [record[index] for _, record in values]
        stats.append({
            "index": index,
            "min": min(column),
            "max": max(column),
            "zero": sum(value == 0 for value in column),
            "negative": sum(value < 0 for value in column),
            "nonfinite": sum(not math.isfinite(value) for value in column),
            "common": Counter(column).most_common(10),
        })
    result = {"cameras": len(values), "files": len(set(name for name, _ in values)),
              "fields": stats, "errors": errors,
              "zero_radius_assets": [name for name, record in values if record[3] == 0],
              "negative_radius_assets": [name for name, record in values if record[3] < 0]}
    output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps({key: value for key, value in result.items() if key not in ("zero_radius_assets", "negative_radius_assets")}, indent=2))


if __name__ == "__main__":
    main(*(Path(value) for value in sys.argv[1:4]))
