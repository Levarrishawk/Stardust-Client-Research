#!/usr/bin/env python3
"""Audit 23-byte on-disk CLEF CLGT leaves across effective CEF winners."""
from __future__ import annotations

import json
import math
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def leaves(data: bytes):
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
            elif tag == b"CLGT":
                if size != 23:
                    raise ValueError(f"CLGT size {size}")
                rgb = tuple(data[p + 8:p + 11])
                fields = struct.unpack_from("<5f", data, p + 11)
                found.append((rgb, fields))
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
    seen = set()
    values = []
    errors = []
    for _, archive in sorted(archives, reverse=True):
        for name, usize, kind, packed in archive_records(archive):
            if name in seen:
                continue
            seen.add(name)
            if not name.endswith(".cef") or not usize:
                continue
            try:
                values.extend((name, *record) for record in leaves(expand(kind, packed, usize)))
            except Exception as exc:
                errors.append((name, str(exc)))
    times = [entry[2][0] for entry in values]
    positions = [tuple(entry[2][1:4]) for entry in values]
    ranges = [entry[2][4] for entry in values]
    result = {
        "records": len(values), "files": len(set(entry[0] for entry in values)),
        "rgb_common": Counter(entry[1] for entry in values).most_common(8),
        "lifetime_min": min(times), "lifetime_max": max(times),
        "position_zero_count": sum(p == (0.0, 0.0, 0.0) for p in positions),
        "position_common": Counter(positions).most_common(8),
        "large_field1_samples": [(name, fields) for name, _, fields in values if fields[1] > 100][:30],
        "range_min": min(ranges), "range_max": max(ranges),
        "nonfinite_scalars": sum(not math.isfinite(v) for _, _, fields in values for v in fields),
        "errors": errors,
    }
    output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main(*(Path(value) for value in sys.argv[1:4]))
