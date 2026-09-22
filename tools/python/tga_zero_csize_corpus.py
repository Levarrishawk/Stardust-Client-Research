#!/usr/bin/env python3
"""Inspect effective kind-0/csize-0 TGA metadata without converting assets."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import expand


def records(path):
    raw = path.read_bytes()
    count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    table = expand(rk, raw[off:off + rs], count * 24)
    names = expand(nk, raw[off + rs:off + rs + ns], nu)
    for index in range(count):
        _, usize, pos, kind, csize, noff = struct.unpack_from("<6I", table, index * 24)
        end = names.find(b"\0", noff)
        name = names[noff:end].decode("ascii", "replace").lower().replace("\\", "/")
        if name.endswith(".tga"):
            yield name, usize, kind, csize, raw[pos:pos + (usize if kind == 0 else csize)]


def header(data):
    if len(data) < 18:
        raise ValueError("short TGA header")
    id_length, color_map, image_type = data[:3]
    cmap_first, cmap_count, cmap_bits, x_origin, y_origin, width, height, bits, descriptor = struct.unpack_from("<HHBHHHHBB", data, 3)
    if not width or not height:
        raise ValueError("zero dimensions")
    return (image_type, width, height, bits, descriptor, color_map, cmap_count, cmap_bits, id_length)


def main(root, cfg, output):
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    rows = []
    for priority, archive in sorted(configured, reverse=True):
        for name, usize, kind, csize, data in records(archive):
            if name in seen:
                continue
            seen.add(name)
            if kind != 0 or csize != 0 or usize == 0:
                continue
            try:
                values = header(data)
                status = "header_ok"
            except Exception as error:
                values = ("",) * 9
                status = str(error)
            rows.append((name, archive.name, priority, usize, *values, status))
    with output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(("name", "archive", "priority", "bytes", "image_type", "width", "height", "bits", "descriptor", "color_map", "cmap_count", "cmap_bits", "id_length", "status"))
        writer.writerows(rows)
    print("effective_tga", len(seen), "zero_csize_live", len(rows))
    print("status", Counter(row[-1] for row in rows))
    print("types", Counter(row[4] for row in rows))
    print("dimensions", Counter((row[5], row[6], row[7]) for row in rows))
    print("prefixes", Counter(row[0].split("/")[0] for row in rows))
    for row in rows:
        print("TGA", row)


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
