#!/usr/bin/env python3
"""Inventory effective ASH ACTN chunk payloads and trailing boolean bytes."""
from __future__ import annotations

import csv
import re
import struct
import sys
import zlib
from collections import Counter
from pathlib import Path


def expand(kind, data, expected):
    out = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if out is None or len(out) != expected:
        raise ValueError("invalid compressed TRE block")
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
        if not name.endswith(".ash"):
            continue
        result[name] = None if packed == 0 else expand(kind, raw[position:position + packed], unpacked)
    return result


def action_chunks(data):
    rows = []
    def walk(start, end, stack):
        position = start
        while position + 8 <= end:
            tag = data[position:position + 4]
            size = struct.unpack_from(">I", data, position + 4)[0]
            stop = position + 8 + size
            if stop > end:
                raise ValueError(f"bad IFF at {position:#x}")
            if tag == b"FORM":
                kind = data[position + 8:position + 12].decode("ascii", "replace")
                walk(position + 12, stop, stack + (kind,))
            elif tag == b"ACTN":
                rows.append(("/".join(stack), data[position + 8:stop]))
            position = stop
        if position != end:
            raise ValueError("trailing IFF bytes")
    walk(0, len(data), ())
    return rows


def summarize_payload(payload):
    first_end = payload.find(b"\0")
    if first_end < 0:
        return "", "", "", ""
    first = payload[:first_end].decode("utf-8", "replace")
    rest = payload[first_end + 1:]
    second_end = rest.find(b"\0")
    if second_end >= 0:
        second = rest[:second_end].decode("utf-8", "replace")
        trailer = rest[second_end + 1:]
        return first, second, trailer.hex(), len(trailer)
    return first, "", rest.hex(), len(rest)


def main(root, config, output, mode="effective"):
    root = Path(root)
    priorities = []
    for line in Path(config).read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            priorities.append((int(match.group(1)), match.group(2)))
    winners = {}
    if mode == "all":
        for priority, archive in sorted(priorities, reverse=True):
            for name, blob in archive_records(root / archive).items():
                winners[f"{archive}:{name}"] = (priority, archive, blob, name)
    else:
        for priority, archive in sorted(priorities, reverse=True):
            for name, blob in archive_records(root / archive).items():
                winners.setdefault(name, (priority, archive, blob, name))
    counts = Counter()
    with open(output, "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("name", "priority", "archive", "stack", "action_name", "second_string", "trailer_length", "trailer_hex", "payload_hex"))
        for _, (priority, archive, blob, name) in sorted(winners.items()):
            if blob is None:
                continue
            for stack, payload in action_chunks(blob):
                first, second, trailer, trailer_length = summarize_payload(payload)
                writer.writerow((name, priority, archive, stack, first, second, trailer_length, trailer, payload.hex()))
                counts[(stack, trailer_length, trailer)] += 1
    print("ash_records", sum(blob is not None for _, _, blob, _ in winners.values()), "mode", mode)
    print("actn_chunks", sum(counts.values()))
    for key, count in counts.most_common(80):
        print("COUNT", count, *key)


if __name__ == "__main__":
    main(*sys.argv[1:])
