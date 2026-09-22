#!/usr/bin/env python3
"""Inventory IMPL/PASS schemas and DATA payloads across the configured TRE overlay."""
from __future__ import annotations

import csv
import re
import struct
import sys
import zlib
from collections import Counter, defaultdict
from pathlib import Path


def expand(kind, data, expected):
    out = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if out is None or len(out) != expected:
        raise ValueError("invalid compressed block")
    return out


def archive_records(path):
    raw = path.read_bytes()
    count, offset, record_kind, record_size, name_kind, name_size, name_unpacked = struct.unpack_from("<7I", raw, 8)
    records = expand(record_kind, raw[offset:offset + record_size], count * 24)
    names = expand(name_kind, raw[offset + record_size:offset + record_size + name_size], name_unpacked)
    result = {}
    for index in range(count):
        _, unpacked, position, kind, packed, name_offset = struct.unpack_from("<6I", records, index * 24)
        end = names.find(b"\0", name_offset)
        name = names[name_offset:end].decode("utf-8", "replace").lower().replace("\\", "/")
        if Path(name).suffix not in (".sht", ".eft"):
            continue
        stored_size = unpacked if kind == 0 else packed
        result[name] = None if unpacked == 0 else expand(kind, raw[position:position + stored_size], unpacked)
    return result


def inspect_iff(data):
    rows = []

    def walk(start, end, stack):
        position = start
        child_tags = []
        while position + 8 <= end:
            tag = data[position:position + 4]
            size = struct.unpack_from(">I", data, position + 4)[0]
            stop = position + 8 + size
            if stop > end:
                raise ValueError(f"bad IFF at {position:#x}")
            if tag == b"FORM":
                kind = data[position + 8:position + 12].decode("ascii", "replace")
                child_tags.append("FORM:" + kind)
                walk(position + 12, stop, stack + (kind,))
            else:
                text = tag.decode("ascii", "replace")
                child_tags.append(text)
                if len(stack) >= 1 and stack[-1] in ("STAG", "PFFP") and tag in (b"0000", b"0001"):
                    payload = data[position + 8:stop]
                    rows.append((stack[-1], text, payload.hex(), len(payload)))
                if tag == b"DATA" and len(stack) >= 2 and stack[-2] in ("IMPL", "PASS", "STAG", "TXM "):
                    payload = data[position + 8:stop]
                    rows.append((stack[-2].strip(), stack[-1], payload.hex(), len(payload)))
            position = stop
        if position != end:
            raise ValueError("trailing IFF bytes")
        if len(stack) >= 2 and stack[-2] in ("IMPL", "PASS", "STAG", "TXM "):
            rows.append((stack[-2].strip() + "_SHAPE", stack[-1], ";".join(child_tags), len(child_tags)))

    walk(0, len(data), ())
    return rows


def main(root, config, output):
    root = Path(root)
    priorities = []
    for line in Path(config).read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            priorities.append((int(match.group(1)), match.group(2)))

    winners = {}
    archive_cache = {}
    for priority, archive in sorted(priorities, reverse=True):
        records = archive_records(root / archive)
        archive_cache[archive] = records
        for name, blob in records.items():
            winners.setdefault(name, (priority, archive, blob))

    counts = Counter()
    payloads = defaultdict(Counter)
    with open(output, "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("name", "priority", "archive", "record", "version", "length", "value"))
        for name, (priority, archive, blob) in sorted(winners.items()):
            if blob is None:
                continue
            for record, version, value, length in inspect_iff(blob):
                writer.writerow((name, priority, archive, record, version, length, value))
                counts[(record, version, length)] += 1
                if not record.endswith("_SHAPE"):
                    payloads[(record, version)][value] += 1

    print("live_winners", sum(blob is not None for _, _, blob in winners.values()))
    for key, count in sorted(counts.items()):
        print("COUNT", *key, count)
    for key, values in sorted(payloads.items()):
        print("UNIQUE", *key, len(values))
        for value, count in values.most_common(12):
            print("VALUE", *key, count, value)


if __name__ == "__main__":
    main(*sys.argv[1:4])
