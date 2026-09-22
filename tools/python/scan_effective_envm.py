#!/usr/bin/env python3
"""Inventory effective terrain/environment/*.iff ENVM records in a TRE overlay."""
from __future__ import annotations

import csv
import re
import struct
import sys
import zlib
from collections import Counter
from pathlib import Path


def expand(kind: int, data: bytes, expected: int) -> bytes:
    value = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if value is None or len(value) != expected:
        raise ValueError("unsupported/invalid compression")
    return value


def members(archive: Path):
    raw = archive.read_bytes()
    if raw[:8] != b"EERT5000":
        return
    count, offset, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    table = expand(rk, raw[offset:offset + rs], count * 24)
    names = expand(nk, raw[offset + rs:offset + rs + ns], nu)
    for index in range(count):
        _, usize, payload_offset, kind, csize, name_offset = struct.unpack_from("<6I", table, index * 24)
        end = names.find(b"\0", name_offset)
        name = names[name_offset:end].decode("ascii", "replace").lower().replace("\\", "/")
        if name.startswith("terrain/environment/") and name.endswith(".iff"):
            stored_size = usize if kind == 0 else csize
            yield name, None if usize == 0 else expand(kind, raw[payload_offset:payload_offset + stored_size], usize)


def cstring(data: bytes, offset: int) -> tuple[str, int]:
    end = data.index(0, offset)
    return data[offset:end].decode("ascii", "replace"), end + 1


def chunks(data: bytes):
    if data[:4] != b"FORM" or data[8:12] != b"ENVM" or data[12:16] != b"FORM":
        raise ValueError("not ENVM")
    version = data[20:24].decode("ascii", "replace")
    pos = 24
    while pos + 8 <= len(data):
        tag = data[pos:pos + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", data, pos + 4)[0]
        payload = data[pos + 8:pos + 8 + size]
        if len(payload) != size:
            raise ValueError(f"truncated {tag}")
        yield version, tag, payload
        pos += 8 + size


root, config, output = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
chunk_output = Path(sys.argv[4]) if len(sys.argv) > 4 else None
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
    for name, payload in members(archive):
        effective.setdefault(name, (priority, archive_name, payload))

tag_counts = Counter()
member_chunks = []
records = []
with output.open("w", newline="", encoding="utf-8") as stream:
    writer = csv.writer(stream, delimiter="\t")
    writer.writerow(("member", "priority", "archive", "version", "index", "primary", "primary_size", "secondary", "secondary_size", "angle_1_degrees", "angle_2_degrees", "animation_scale", "animation_period", "status"))
    for name in sorted(effective):
        priority, archive_name, payload = effective[name]
        if payload is None:
            writer.writerow((name, priority, archive_name, "", "", "", "", "", "", "", "", "", "", "tombstone"))
            continue
        try:
            index = 0
            version_seen = ""
            for version, tag, data in chunks(payload):
                version_seen = version
                tag_counts[(version, tag)] += 1
                member_chunks.append((name, priority, archive_name, version, tag, len(data), data.hex()))
                if tag != "CELS":
                    continue
                off = 0
                primary, off = cstring(data, off)
                primary_size = struct.unpack_from("<f", data, off)[0]; off += 4
                secondary, off = cstring(data, off)
                secondary_size = struct.unpack_from("<f", data, off)[0]; off += 4
                values = struct.unpack_from("<4f", data, off); off += 16
                if off != len(data):
                    raise ValueError(f"CELS trailing={len(data)-off}")
                writer.writerow((name, priority, archive_name, version, index, primary, primary_size, secondary, secondary_size, *values, "ok"))
                records.append(values)
                index += 1
            if index == 0:
                writer.writerow((name, priority, archive_name, version_seen, "", "", "", "", "", "", "", "", "", "no CELS"))
        except Exception as error:
            writer.writerow((name, priority, archive_name, "", "", "", "", "", "", "", "", "", "", f"error: {error}"))

print(f"effective_files={len(effective)} cels_records={len(records)}")
print("tag_counts=" + ", ".join(f"{version}/{tag}:{count}" for (version, tag), count in sorted(tag_counts.items())))
for field_index, label in enumerate(("angle_1", "angle_2", "animation_scale", "animation_period")):
    values = Counter(round(record[field_index], 6) for record in records)
    print(f"{label}=" + ", ".join(f"{value:g}:{count}" for value, count in values.most_common()))
if chunk_output is not None:
    with chunk_output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream, delimiter="\t")
        writer.writerow(("member", "priority", "archive", "version", "tag", "payload_size", "payload_hex"))
        writer.writerows(member_chunks)
    print(f"chunk_output={chunk_output}")
