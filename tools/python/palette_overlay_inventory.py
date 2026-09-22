#!/usr/bin/env python3
"""Inventory effective .pal resources selected by a SWG searchTree overlay."""
from __future__ import annotations

import csv
import mmap
import re
import struct
import sys
import zlib
from collections import Counter
from pathlib import Path


def expand(kind, data, expected):
    value = bytes(data[:expected]) if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if value is None or len(value) != expected:
        raise ValueError(f"compression kind={kind} size={len(value) if value else None}/{expected}")
    return value


def records(path):
    with path.open("rb") as handle, mmap.mmap(handle.fileno(), 0, access=mmap.ACCESS_READ) as raw:
        if raw[:8] != b"EERT5000": return
        count, offset, rk, rs, nk, ns, nus = struct.unpack_from("<7I", raw, 8)
        recs = expand(rk, raw[offset:offset+rs], count * 24)
        names = expand(nk, raw[offset+rs:offset+rs+ns], nus)
        for i in range(count):
            _, usize, poff, kind, csize, noff = struct.unpack_from("<6I", recs, i * 24)
            end = names.index(0, noff)
            name = names[noff:end].decode("ascii", "replace").lower().replace("\\", "/")
            if name.endswith(".pal"):
                stored_size = usize if kind == 0 else csize
                yield name, expand(kind, raw[poff:poff+stored_size], usize)


def parse_pal(raw):
    errors = []
    if len(raw) < 24: return None, ["short"]
    magic, riff_size, kind, chunk, data_size, marker = struct.unpack_from("<4sI4s4sII", raw)
    if magic != b"RIFF": errors.append(f"magic={magic!r}")
    if kind != b"PAL ": errors.append(f"kind={kind!r}")
    if chunk != b"data": errors.append(f"chunk={chunk!r}")
    if data_size < 4 or (data_size - 4) % 4: errors.append(f"data_size={data_size}")
    if 20 + data_size != len(raw): errors.append(f"data_extent={20+data_size}/{len(raw)}")
    if riff_size + 8 != len(raw): errors.append(f"riff_extent={riff_size+8}/{len(raw)}")
    version = marker & 0xffff
    header_count = marker >> 16
    colors = [tuple(raw[i:i+4]) for i in range(24, min(len(raw), 20+data_size), 4)]
    if version != 0x0300: errors.append(f"version={version:#x}")
    if header_count != len(colors): errors.append(f"header_count={header_count}/{len(colors)}")
    trailing = raw[min(len(raw), 20+data_size):]
    return (riff_size, data_size, marker, version, header_count, colors, trailing), errors


root, cfg, output = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
configured = []
for line in cfg.read_text(errors="replace").splitlines():
    m = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if m: configured.append((int(m.group(1)), m.group(2)))
winners, rows, failures = set(), [], []
for priority, archive_name in sorted(configured, reverse=True):
    archive = root / archive_name
    if not archive.exists(): continue
    for name, raw in records(archive):
        if name in winners: continue
        winners.add(name)
        parsed, errors = parse_pal(raw)
        if parsed is None:
            failures.append((archive_name, name, ";".join(errors))); continue
        riff_size, data_size, marker, version, header_count, colors, trailing = parsed
        alphas = Counter(x[3] for x in colors)
        rows.append((archive_name, priority, name, len(raw), riff_size, data_size, marker, version, header_count,
                     len(colors), len(set(colors)), len(set(x[:3] for x in colors)),
                     ";".join(f"{k}:{v}" for k,v in sorted(alphas.items())), len(trailing),
                     sum(bool(x) for x in trailing), ";".join(errors)))
with output.open("w", newline="", encoding="utf-8") as handle:
    w = csv.writer(handle)
    w.writerow(("archive","priority","name","bytes","riff_size","data_size","marker","version","header_count",
                "colors","unique_rgba","unique_rgb","alpha_histogram","trailing_bytes","trailing_nonzero","errors"))
    w.writerows(rows)
print(f"configured={len(configured)} effective_palettes={len(rows)} failures={len(failures)}")
print("color_counts", sorted(Counter(x[9] for x in rows).items()))
print("versions", sorted(Counter(x[7] for x in rows).items()))
print("error_rows", sum(bool(x[-1]) for x in rows))
for failure in failures[:20]: print("FAIL", *failure, sep="\t")
