#!/usr/bin/env python3
"""Inventory effective character appearance/animation resources in a SWG overlay."""
from __future__ import annotations

import csv, mmap, re, struct, sys, zlib
from collections import Counter
from pathlib import Path


EXTS = (".sat", ".skt", ".mgn", ".lmg", ".lat", ".ans")


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
            if name.endswith(EXTS):
                stored = usize if kind == 0 else csize
                yield name, expand(kind, raw[poff:poff+stored], usize)


def tag(raw, at):
    return raw[at:at+4].decode("ascii", "replace") if len(raw) >= at + 4 else "SHORT"


def child_signature(raw):
    """Return immediate chunk/form tags inside outer FORM/version FORM."""
    if len(raw) < 24 or raw[:4] != b"FORM" or raw[12:16] != b"FORM": return ""
    version_end = 20 + struct.unpack_from(">I", raw, 16)[0]
    at, tags = 24, []
    while at + 8 <= min(len(raw), version_end):
        kind = tag(raw, at)
        size = struct.unpack_from(">I", raw, at + 4)[0]
        if kind == "FORM":
            if at + 12 > len(raw): break
            tags.append("FORM:" + tag(raw, at + 8))
        else:
            tags.append(kind)
        at += 8 + size
    return "/".join(tags)


root, cfg, output = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
configured = []
for line in cfg.read_text(errors="replace").splitlines():
    m = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if m: configured.append((int(m.group(1)), m.group(2)))
winners, rows = set(), []
for priority, archive_name in sorted(configured, reverse=True):
    archive = root / archive_name
    if not archive.exists(): continue
    for name, raw in records(archive):
        if name in winners: continue
        winners.add(name)
        ext = Path(name).suffix
        outer = tag(raw, 8) if raw[:4] == b"FORM" else tag(raw, 0)
        child = tag(raw, 20) if raw[:4] == b"FORM" and raw[12:16] == b"FORM" else tag(raw, 12)
        rows.append((archive_name, priority, name, ext, len(raw), outer, child,
                     child_signature(raw), raw[:32].hex()))
with output.open("w", newline="", encoding="utf-8") as handle:
    writer = csv.writer(handle)
    writer.writerow(("archive","priority","name","extension","bytes","outer_tag","first_child_tag","child_signature","prefix_hex"))
    writer.writerows(rows)
print(f"configured={len(configured)} effective={len(rows)}")
print("extensions", sorted(Counter(r[3] for r in rows).items()))
print("outer", sorted(Counter((r[3], r[5]) for r in rows).items()))
print("first_child", sorted(Counter((r[3], r[5], r[6]) for r in rows).items()))
print("signatures")
for key, count in sorted(Counter((r[3], r[5], r[6], r[7]) for r in rows).items()):
    print(count, *key, sep="\t")
