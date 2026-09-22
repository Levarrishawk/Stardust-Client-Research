#!/usr/bin/env python3
"""Summarize effective SWG DTI command tables and their overlay collisions."""
from __future__ import annotations

import struct
import sys
from pathlib import Path


def parse(path: Path):
    raw = path.read_bytes()
    cols_at, rows_at = raw.find(b"COLS"), raw.find(b"ROWS")
    col_size = struct.unpack_from(">I", raw, cols_at + 4)[0]
    type_at = cols_at + 8 + col_size
    col_payload = raw[cols_at + 8:cols_at + 8 + col_size]
    count = struct.unpack_from("<I", col_payload)[0]
    names = [x.decode() for x in col_payload[4:].split(b"\0")[:count]]
    type_size = struct.unpack_from(">I", raw, type_at + 4)[0]
    specs = [x.decode() for x in raw[type_at + 8:type_at + 8 + type_size].split(b"\0")[:count]]
    rows = struct.unpack_from("<I", raw, rows_at + 8)[0]
    cursor = rows_at + 12
    result = []
    for _ in range(rows):
        row = {}
        for name, spec in zip(names, specs):
            kind = spec[:1]
            if kind == "s":
                end = raw.index(b"\0", cursor)
                value = raw[cursor:end].decode("ascii", "replace")
                cursor = end + 1
            elif kind == "f":
                value = struct.unpack_from("<f", raw, cursor)[0]
                cursor += 4
            else:
                value = struct.unpack_from("<i", raw, cursor)[0]
                cursor += 4
            row[name] = value
        result.append(row)
    return result


tables = [(Path(arg).stem.replace("_effective", ""), parse(Path(arg))) for arg in sys.argv[1:]]
for label, rows in tables:
    disabled = sum(bool(row.get("disabled", 0)) for row in rows)
    cpp = sum(bool(row.get("cppHook", "").strip()) for row in rows)
    script = sum(bool(row.get("scriptHook", "").strip()) for row in rows)
    combat = sum(bool(row.get("addToCombatQueue", 0)) for row in rows)
    print(f"{label}: rows={len(rows)} disabled={disabled} cppHook={cpp} scriptHook={script} combatQueue={combat}")

for mode, wanted in (("ground", (0, 1, 2, 3)), ("space", (0, 1, 4, 5))):
    merged = {}
    collisions = 0
    for index in wanted:
        label, rows = tables[index]
        for row in rows:
            key = row["commandName"].lower()
            collisions += key in merged
            merged[key] = (label, row)
    disabled = sum(bool(item[1].get("disabled", 0)) for item in merged.values())
    print(f"overlay={mode}: sourceRows={sum(len(tables[i][1]) for i in wanted)} unique={len(merged)} replacements={collisions} enabled={len(merged)-disabled} disabled={disabled}")
