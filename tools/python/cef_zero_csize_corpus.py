#!/usr/bin/env python3
"""Classify effective live CEF records with kind-0/csize-0 TRE metadata."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def records(path):
    raw = path.read_bytes()
    if raw[:8] != b"EERT5000":
        raise ValueError(path)
    count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    table = expand(rk, raw[off:off + rs], count * 24)
    names = expand(nk, raw[off + rs:off + rs + ns], nu)
    for index in range(count):
        _, usize, pos, kind, csize, noff = struct.unpack_from("<6I", table, index * 24)
        end = names.find(b"\0", noff)
        name = names[noff:end].decode("ascii", "replace").lower().replace("\\", "/")
        if name.endswith(".cef"):
            size = usize if kind == 0 else csize
            yield name, usize, kind, csize, raw[pos:pos + size]


def iff_shape(payload):
    if len(payload) < 12 or payload[:4] != b"FORM":
        return "INVALID", "", ""
    outer_size = struct.unpack_from(">I", payload, 4)[0]
    if outer_size + 8 != len(payload):
        return "BAD_LENGTH", "", ""
    tag = payload[8:12].decode("ascii", "replace")
    offset = 12
    children = []
    while offset < len(payload):
        if offset + 8 > len(payload):
            return tag, "TRUNCATED_CHILD", ""
        child_tag = payload[offset:offset + 4]
        size = struct.unpack_from(">I", payload, offset + 4)[0]
        stop = offset + 8 + size
        if stop > len(payload):
            return tag, "BAD_CHILD", ""
        if child_tag == b"FORM" and size >= 4:
            children.append("FORM:" + payload[offset + 8:offset + 12].decode("ascii", "replace"))
        else:
            children.append(child_tag.decode("ascii", "replace"))
        offset = stop
    refs = sorted({m.group().decode("ascii", "replace").lower().replace("\\", "/")
                   for m in re.finditer(rb"(?:appearance|sound|clienteffect|forcefeedback)[/\\][A-Za-z0-9_./\\-]+\.(?:apt|swh|prt|snd|cef|sht|wav|ffe)[ ]*", payload, re.I)})
    return tag, ";".join(children), ";".join(refs)


def leaf_shape(payload):
    if len(payload) < 24 or payload[12:16] != b"FORM":
        return "INVALID"
    offset = 24
    children = []
    sizes = []
    while offset < len(payload):
        if offset + 8 > len(payload):
            return "TRUNCATED"
        tag = payload[offset:offset + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", payload, offset + 4)[0]
        if offset + 8 + size > len(payload):
            return "BAD_SIZE"
        children.append(tag)
        sizes.append(f"{tag}:{size}")
        offset += 8 + size
    return ";".join(children), ";".join(sizes)


def main(root, cfg, out):
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    winners = {}
    rows = []
    for priority, archive in sorted(configured, reverse=True):
        for asset_name, usize, kind, payload in archive_records(archive):
            winners.setdefault(asset_name, bool(usize and payload))
        for name, usize, kind, csize, payload in records(archive):
            if name in seen:
                continue
            seen.add(name)
            if not (kind == 0 and csize == 0 and usize > 0):
                continue
            form, children, refs = iff_shape(payload)
            leaf_tags, leaf_sizes = leaf_shape(payload)
            rows.append((name, archive.name, priority, usize, form, children, leaf_tags, leaf_sizes, refs))
    with out.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(("name", "archive", "priority", "bytes", "form", "children", "leaf_tags", "leaf_sizes", "references"))
        writer.writerows(rows)
    print("effective_cef", len(seen), "zero_csize_live", len(rows))
    print("forms", Counter(row[4] for row in rows))
    print("children", Counter(row[5] for row in rows))
    print("leaf_shapes", Counter(row[6] for row in rows))
    print("with_references", sum(bool(row[8]) for row in rows))
    refs = [ref for row in rows for ref in row[8].split(";") if ref]
    print("reference_suffixes", Counter(Path(ref).suffix for ref in refs))
    print("reference_availability", Counter("live" if winners.get(ref) else "masked" if ref in winners else "absent" for ref in refs))
    print("no_refs", [row[0] for row in rows if not row[8]])
    for row in rows[:20]:
        print("SAMPLE", row)


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
