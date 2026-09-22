#!/usr/bin/env python3
"""Dump the common SWG DTII table dialect to TSV."""
from __future__ import annotations
import csv, struct, sys
from pathlib import Path


def dtii_chunks(data):
    if data[:4] != b"FORM" or data[8:12] != b"DTII" or data[12:16] != b"FORM":
        raise ValueError("not DTII")
    inner_end = 20 + struct.unpack_from(">I", data, 16)[0]
    pos, result = 24, {}
    while pos < inner_end:
        tag = data[pos:pos+4]
        size = struct.unpack_from(">I", data, pos+4)[0]
        result[tag] = data[pos+8:pos+8+size]
        pos += 8 + size
    return result


raw, out = Path(sys.argv[1]).read_bytes(), Path(sys.argv[2])
parts = dtii_chunks(raw)
cols = parts[b"COLS"]
count = struct.unpack_from("<I", cols)[0]
names = [x.decode("ascii", "replace") for x in cols[4:].split(b"\0")[:count]]
types = [x.decode("ascii", "replace") for x in parts[b"TYPE"].split(b"\0") if x][:count]
print(f"schema columns={count} names={names} types={types}")
rows = parts[b"ROWS"]
row_count, pos, result = struct.unpack_from("<I", rows)[0], 4, []
for _ in range(row_count):
    row = []
    for desc in types:
        kind = desc[0]
        if kind == "s":
            end = rows.index(0, pos); row.append(rows[pos:end].decode("ascii", "replace")); pos = end + 1
        elif kind == "f": row.append(struct.unpack_from("<f", rows, pos)[0]); pos += 4
        else: row.append(struct.unpack_from("<i", rows, pos)[0]); pos += 4
    result.append(row)
if pos != len(rows): raise ValueError(f"trailing ROWS bytes {len(rows)-pos}")
with out.open("w", newline="", encoding="utf-8") as handle:
    writer = csv.writer(handle, delimiter="\t")
    writer.writerow(names); writer.writerows(result)
print(f"columns={count} rows={row_count} types={types} trailing=0")
