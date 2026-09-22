#!/usr/bin/env python3
"""Inventory effective DDS records in the configured Stardust TRE overlay."""
from __future__ import annotations

import csv
import math
import re
import struct
import sys
import zlib
from collections import Counter
from pathlib import Path


def expand(kind, data, expected):
    out = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if out is None or len(out) != expected:
        raise ValueError("invalid compressed block")
    return out


def records(path):
    raw = path.read_bytes()
    count, offset, record_kind, record_size, name_kind, name_size, name_unpacked = struct.unpack_from("<7I", raw, 8)
    table = expand(record_kind, raw[offset:offset + record_size], count * 24)
    names = expand(name_kind, raw[offset + record_size:offset + record_size + name_size], name_unpacked)
    result = {}
    for index in range(count):
        _, unpacked, position, kind, packed, name_offset = struct.unpack_from("<6I", table, index * 24)
        end = names.find(b"\0", name_offset)
        name = names[name_offset:end].decode("utf-8", "replace").lower().replace("\\", "/")
        if not name.endswith(".dds"):
            continue
        stored_size = unpacked if kind == 0 else packed
        result[name] = None if unpacked == 0 else (raw, position, stored_size, unpacked, kind)
    return result


def parse(blob):
    if len(blob) < 128 or blob[:4] != b"DDS " or struct.unpack_from("<I", blob, 4)[0] != 124:
        raise ValueError("invalid DDS header")
    flags, height, width, pitch, depth, mips = struct.unpack_from("<6I", blob, 8)
    pf_size, pf_flags, fourcc, rgb_bits, rmask, gmask, bmask, amask = struct.unpack_from("<8I", blob, 76)
    caps, caps2 = struct.unpack_from("<2I", blob, 108)
    if pf_size != 32:
        raise ValueError("invalid pixel format")
    if pf_flags & 4:
        fmt = fourcc.to_bytes(4, "little").decode("ascii", "replace")
    else:
        fmt = f"RGB{rgb_bits}:{rmask:08x}/{gmask:08x}/{bmask:08x}/{amask:08x}:f{pf_flags:x}"
    return width, height, max(mips, 1), bool(caps2 & 0x200), fmt, len(blob)


def main(root, config, output):
    priorities = []
    for line in Path(config).read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            priorities.append((int(match.group(1)), match.group(2)))
    winners = {}
    root = Path(root)
    for priority, archive in sorted(priorities, reverse=True):
        for name, record in records(root / archive).items():
            winners.setdefault(name, (priority, archive, record))

    formats, dimensions, mips, cubes = Counter(), Counter(), Counter(), Counter()
    npot = incomplete_chain = 0
    live = tombstones = invalid = 0
    with open(output, "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("name", "priority", "archive", "width", "height", "mips", "cube", "format", "bytes"))
        for name, (priority, archive, record) in sorted(winners.items()):
            if record is None:
                tombstones += 1
                continue
            raw, position, packed, unpacked, kind = record
            try:
                values = parse(expand(kind, raw[position:position + packed], unpacked))
            except Exception as error:
                invalid += 1
                writer.writerow((name, priority, archive, "", "", "", "", "INVALID:" + str(error), unpacked))
                continue
            width, height, mip_count, cube, fmt, size = values
            live += 1
            formats[fmt] += 1
            dimensions[(width, height)] += 1
            mips[mip_count] += 1
            cubes[cube] += 1
            if width & (width - 1) or height & (height - 1):
                npot += 1
            if mip_count != int(math.log2(max(width, height))) + 1:
                incomplete_chain += 1
            writer.writerow((name, priority, archive, width, height, mip_count, int(cube), fmt, size))
    print("winners", len(winners), "live", live, "tombstones", tombstones, "invalid", invalid)
    print("formats", formats)
    print("cube", cubes)
    print("npot", npot, "non_full_mip_chain", incomplete_chain)
    print("mips", mips.most_common())
    print("dimensions", dimensions.most_common(30))


if __name__ == "__main__":
    main(*sys.argv[1:4])
