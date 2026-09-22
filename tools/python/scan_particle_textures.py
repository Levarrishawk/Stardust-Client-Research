#!/usr/bin/env python3
"""Inventory PTEX/0000 records in effective configured .prt assets."""
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
            yield "FORM", data[pos + 8:pos + 12].decode("ascii", "replace"), pos + 12, stop
        else:
            yield "CHUNK", tag, pos + 8, stop
        pos = stop


def decode_ptex(data: bytes, start: int, end: int):
    direct = list(items(data, start, end))
    if not direct or direct[0][0] != "CHUNK":
        return None
    version = direct[0][1]
    payload = data[direct[0][2]:direct[0][3]]
    nul = payload.find(b"\0")
    if nul < 0 or len(payload) < nul + 1 + 25:
        return (version, "", *(None,) * 7, "invalid payload")
    name = payload[:nul].decode("ascii", "replace")
    layout, first, last, uv_extent, per_row, fps = struct.unpack_from("<IIIfIf", payload, nul + 1)
    enabled = int(payload[nul + 25] != 0)
    return (version, name, layout, first, last, uv_extent, per_row, fps, enabled, "")


def walk(data: bytes, start: int, end: int, found: list[tuple]):
    for kind, tag, child, stop in items(data, start, end):
        if kind == "FORM":
            if tag == "PTEX":
                decoded = decode_ptex(data, child, stop)
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
        writer.writerow(("archive", "priority", "particle", "version", "resource", "layout", "first_frame", "last_frame", "uv_extent", "frames_per_row", "frames_per_second", "enabled", "note"))
        writer.writerows(rows)
    print(f"records={len(rows)} scan_errors={errors} unique_resources={len(set(row[4] for row in rows))}")
    for label, index in (("version", 3), ("layout", 5), ("frame_range", None), ("per_row", 9), ("fps", 10), ("enabled", 11)):
        values = Counter((row[6], row[7]) if index is None else row[index] for row in rows)
        print(label, values.most_common(20))


if __name__ == "__main__":
    main(*sys.argv[1:4])
