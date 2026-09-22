#!/usr/bin/env python3
"""Summarize effective DTII planet-environment tables in a configured TRE overlay."""
from __future__ import annotations

import csv
import re
import struct
import sys
import zlib
from pathlib import Path


def expand(kind: int, data: bytes, expected: int) -> bytes:
    result = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if result is None or len(result) != expected:
        raise ValueError("unsupported or invalid compressed block")
    return result


def records(archive: Path):
    raw = archive.read_bytes()
    if raw[:8] != b"EERT5000":
        return
    count, offset, record_kind, record_size, name_kind, name_size, name_usize = struct.unpack_from("<7I", raw, 8)
    table = expand(record_kind, raw[offset:offset + record_size], count * 24)
    names = expand(name_kind, raw[offset + record_size:offset + record_size + name_size], name_usize)
    for index in range(count):
        _, usize, payload_offset, kind, csize, name_offset = struct.unpack_from("<6I", table, index * 24)
        end = names.find(b"\0", name_offset)
        name = names[name_offset:end].decode("ascii", "replace").lower().replace("\\", "/")
        if not (name.startswith("datatables/environment/") and name.endswith(".iff")):
            continue
        if usize == 0:
            yield name, None
        else:
            stored_size = usize if kind == 0 else csize
            yield name, expand(kind, raw[payload_offset:payload_offset + stored_size], usize)


def chunk(data: bytes, tag: bytes) -> bytes:
    offset = data.find(tag)
    if offset < 0:
        raise ValueError(f"missing {tag.decode()}")
    size = struct.unpack_from(">I", data, offset + 4)[0]
    return data[offset + 8:offset + 8 + size]


def parse_dtii(data: bytes):
    cols = chunk(data, b"COLS")
    count = struct.unpack_from("<I", cols)[0]
    names = [value.decode("ascii", "replace") for value in cols[4:].split(b"\0")[:count]]
    types = [value.decode("ascii") for value in chunk(data, b"TYPE").split(b"\0") if value]
    rows = chunk(data, b"ROWS")
    row_count = struct.unpack_from("<I", rows)[0]
    position = 4
    parsed = []
    for _ in range(row_count):
        values = []
        for value_type in types:
            if value_type == "s":
                end = rows.index(0, position)
                values.append(rows[position:end].decode("ascii", "replace"))
                position = end + 1
            elif value_type in ("I", "i"):
                values.append(struct.unpack_from("<i", rows, position)[0])
                position += 4
            elif value_type == "f":
                values.append(struct.unpack_from("<f", rows, position)[0])
                position += 4
            else:
                raise ValueError(f"unsupported type {value_type!r}")
        parsed.append(values)
    if position != len(rows):
        raise ValueError(f"ROWS trailing bytes: {len(rows) - position}")
    return names, types, parsed


root, config, output = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
configured = []
for line in config.read_text(errors="replace").splitlines():
    match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if match:
        configured.append((int(match.group(1)), match.group(2)))

effective = {}
for priority, archive_name in sorted(configured, reverse=True):
    archive = root / archive_name
    if not archive.exists():
        continue
    for name, payload in records(archive):
        if name.startswith("datatables/environment/") and name.endswith(".iff") and name not in effective:
            effective[name] = (priority, archive_name, payload)

with output.open("w", newline="", encoding="utf-8") as stream:
    writer = csv.writer(stream, delimiter="\t")
    writer.writerow(("member", "priority", "archive", "columns", "rows", "families", "weather_values", "families_missing_weather0", "status"))
    for name in sorted(effective):
        priority, archive_name, payload = effective[name]
        try:
            if payload is None:
                writer.writerow((name, priority, archive_name, "", "", "", "", "", "tombstone"))
                continue
            columns, types, rows = parse_dtii(payload)
            families = {}
            if len(columns) >= 2 and len(types) >= 2 and types[0] == "s" and types[1] in ("I", "i"):
                for row in rows:
                    families.setdefault(row[0], set()).add(row[1])
            weather = sorted({value for values in families.values() for value in values})
            missing = sorted(family for family, values in families.items() if 0 not in values)
            status = "ok" if len(columns) == len(types) else "column/type mismatch"
            writer.writerow((name, priority, archive_name, len(columns), len(rows), len(families), ",".join(map(str, weather)), ",".join(missing), status))
        except Exception as error:
            writer.writerow((name, priority, archive_name, "", "", "", "", "", f"error: {error}"))

print(f"tables={len(effective)} output={output}")
