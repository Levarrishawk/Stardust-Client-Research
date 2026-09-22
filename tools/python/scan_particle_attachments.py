#!/usr/bin/env python3
"""Inventory PATT attachment records in effective configured .prt assets."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


def items(data: bytes, start: int, end: int):
    pos = start
    while pos + 8 <= end:
        tag = data[pos:pos + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", data, pos + 4)[0]
        stop = pos + 8 + size
        if stop > end or stop < pos + 8:
            return
        if tag == "FORM" and size >= 4:
            yield pos, "FORM", data[pos + 8:pos + 12].decode("ascii", "replace"), pos + 12, stop
        else:
            yield pos, "CHUNK", tag, pos + 8, stop
        pos = stop


def decode_patt(data: bytes, start: int, end: int):
    direct = list(items(data, start, end))
    if not direct or direct[0][1] != "FORM":
        return None
    version = direct[0][2]
    children = list(items(data, direct[0][3], direct[0][4]))
    if not children or children[0][1] != "CHUNK" or children[0][2] != "0000":
        return (version, "", None, None, None, None, "missing DATA")
    payload = data[children[0][3]:children[0][4]]
    nul = payload.find(b"\0")
    if nul < 0:
        return (version, "", None, None, None, None, "unterminated path")
    path = payload[:nul].decode("ascii", "replace")
    tail = payload[nul + 1:]
    try:
        if version == "0001" and len(tail) >= 13:
            minimum, maximum = struct.unpack_from("<ff", tail, 0)
            kill = int(tail[8] != 0)
            trigger = struct.unpack_from("<I", tail, 9)[0]
            return (version, path, minimum, maximum, kill, trigger, "")
        if version == "0000" and len(tail) >= 29:
            minimum, maximum = struct.unpack_from("<ff", tail, 0)
            kill = int(tail[28] != 0)
            return (version, path, minimum, maximum, kill, 2, "legacy trigger defaults to 2")
    except struct.error as exc:
        return (version, path, None, None, None, None, str(exc))
    return (version, path, None, None, None, None, f"unexpected tail length {len(tail)}")


def walk(data: bytes, start: int, end: int, found: list[tuple]):
    for _, kind, tag, child, stop in items(data, start, end):
        if kind == "FORM":
            if tag == "PATT":
                decoded = decode_patt(data, child, stop)
                if decoded:
                    found.append(decoded)
            walk(data, child, stop, found)


def main(root_value: str, cfg_value: str, output_value: str) -> None:
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))
    winners: set[str] = set()
    rows = []
    errors = 0
    for priority, archive_name in sorted(configured, reverse=True):
        archive = root / archive_name
        if not archive.exists():
            continue
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not name.endswith(".prt") or usize == 0 or not packed:
                continue
            try:
                found: list[tuple] = []
                walk(expand(kind, packed, usize), 0, usize, found)
                for decoded in found:
                    rows.append((archive_name, priority, name, *decoded))
            except Exception:
                errors += 1
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("archive", "priority", "particle", "version", "path", "minimum_age", "maximum_age", "kill_with_parent", "trigger", "note"))
        writer.writerows(rows)
    versions = Counter(row[3] for row in rows)
    triggers = Counter(row[8] for row in rows)
    flags = Counter(row[7] for row in rows)
    extensions = Counter(Path(row[4]).suffix.lower() for row in rows)
    print(f"attachments={len(rows)} scan_errors={errors} unique_paths={len(set(row[4] for row in rows))}")
    print("versions", dict(sorted(versions.items())))
    print("triggers", dict(sorted(triggers.items(), key=lambda x: str(x[0]))))
    print("kill_with_parent", dict(sorted(flags.items(), key=lambda x: str(x[0]))))
    print("extensions", dict(sorted(extensions.items())))
    ranges = Counter((row[5], row[6]) for row in rows)
    for pair, count in ranges.most_common(12):
        print("range", pair[0], pair[1], count, sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:4])
