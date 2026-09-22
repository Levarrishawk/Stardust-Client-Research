#!/usr/bin/env python3
"""Print rows from the simple DTII/0001 tables used by this analysis."""
from __future__ import annotations

import struct
import sys
from pathlib import Path


def parse(path: Path):
    raw = path.read_bytes()
    cols_at = raw.find(b"COLS")
    rows_at = raw.find(b"ROWS")
    col_size = struct.unpack_from(">I", raw, cols_at + 4)[0]
    col_payload = raw[cols_at + 8:cols_at + 8 + col_size]
    count = struct.unpack_from("<I", col_payload)[0]
    names = [x.decode("ascii", "replace") for x in col_payload[4:].split(b"\0")[:count]]
    type_at = cols_at + 8 + col_size
    type_size = struct.unpack_from(">I", raw, type_at + 4)[0]
    specs = [x.decode("ascii", "replace") for x in raw[type_at + 8:type_at + 8 + type_size].split(b"\0")[:count]]
    row_count = struct.unpack_from("<I", raw, rows_at + 8)[0]
    cursor = rows_at + 12
    rows = []
    for _ in range(row_count):
        row = {}
        for name, spec in zip(names, specs):
            if spec.startswith("s"):
                end = raw.index(b"\0", cursor)
                value = raw[cursor:end].decode("ascii", "replace")
                cursor = end + 1
            elif spec.startswith("f"):
                value = struct.unpack_from("<f", raw, cursor)[0]
                cursor += 4
            else:
                value = struct.unpack_from("<i", raw, cursor)[0]
                cursor += 4
            row[name] = value
        rows.append(row)
    return rows


for arg in sys.argv[1:]:
    path = Path(arg)
    print(path)
    for row in parse(path):
        print("\t".join(f"{key}={value}" for key, value in row.items()))
