#!/usr/bin/env python3
"""Inventory effective datatables/clientregion/*.iff records in a configured TRE overlay."""
from __future__ import annotations

import re
import struct
import sys
import zlib
from pathlib import Path


def expand(kind, data, expected):
    value = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if value is None or len(value) != expected:
        raise ValueError("invalid TRE block")
    return value


def members(path):
    raw = path.read_bytes()
    if raw[:8] != b"EERT5000":
        return
    count, offset, rk, rs, nk, ns, nus = struct.unpack_from("<7I", raw, 8)
    records = expand(rk, raw[offset:offset + rs], count * 24)
    names = expand(nk, raw[offset + rs:offset + rs + ns], nus)
    for index in range(count):
        _, usize, poff, kind, csize, noff = struct.unpack_from("<6I", records, index * 24)
        end = names.find(b"\0", noff)
        name = names[noff:end].decode("ascii", "replace").lower().replace("\\", "/")
        if name.startswith("datatables/clientregion/") and name.endswith(".iff"):
            yield name, expand(kind, raw[poff:poff + csize], usize)


def parse(raw):
    cols = raw.find(b"COLS")
    rows_at = raw.find(b"ROWS")
    cs = struct.unpack_from(">I", raw, cols + 4)[0]
    cp = raw[cols + 8:cols + 8 + cs]
    n = struct.unpack_from("<I", cp)[0]
    names = [x.decode() for x in cp[4:].split(b"\0")[:n]]
    types_at = cols + 8 + cs
    ts = struct.unpack_from(">I", raw, types_at + 4)[0]
    specs = [x.decode() for x in raw[types_at + 8:types_at + 8 + ts].split(b"\0")[:n]]
    count = struct.unpack_from("<I", raw, rows_at + 8)[0]
    cursor = rows_at + 12
    result = []
    for _ in range(count):
        row = {}
        for name, spec in zip(names, specs):
            if spec.startswith("s"):
                end = raw.index(b"\0", cursor)
                value = raw[cursor:end].decode("ascii", "replace")
                cursor = end + 1
            else:
                value = struct.unpack_from("<f" if spec.startswith("f") else "<i", raw, cursor)[0]
                cursor += 4
            row[name] = value
        result.append(row)
    return names, specs, result


root, cfg = Path(sys.argv[1]), Path(sys.argv[2])
configured = []
for line in cfg.read_text(errors="replace").splitlines():
    match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if match:
        configured.append((int(match.group(1)), match.group(2)))
winners = {}
for priority, archive_name in sorted(configured):
    archive = root / archive_name
    if archive.exists():
        for name, raw in members(archive):
            winners[name] = priority, archive_name, raw
total = 0
for name, (priority, archive_name, raw) in sorted(winners.items()):
    columns, specs, rows = parse(raw)
    total += len(rows)
    print(f"{name}\tpriority={priority}\tarchive={archive_name}\trows={len(rows)}\tcolumns={','.join(columns)}\ttypes={','.join(specs)}")
print(f"TOTAL tables={len(winners)} rows={total}")
