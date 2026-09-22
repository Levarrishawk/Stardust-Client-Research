#!/usr/bin/env python3
"""Count PTCL/PTQD/PTMH/PTEX versions in effective configured particle assets."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


TARGETS = {"PTCL", "PTQD", "PTMH", "PTEX"}


def child_items(data: bytes, start: int, end: int) -> list[tuple[str, str]]:
    result: list[tuple[str, str]] = []
    pos = start
    while pos + 8 <= end:
        size = struct.unpack_from(">I", data, pos + 4)[0]
        item_end = pos + 8 + size
        if item_end > end or item_end < pos + 8:
            break
        if data[pos:pos + 4] == b"FORM" and size >= 4:
            result.append(("FORM", data[pos + 8:pos + 12].decode("ascii", "replace")))
        else:
            result.append(("CHUNK", data[pos:pos + 4].decode("ascii", "replace")))
        # SWG IFF container sizes already delimit the next item; these assets do
        # not add an external even-byte pad after odd-sized chunks.
        pos = item_end
    return result


def walk(data: bytes, start: int, end: int, counts: Counter[tuple[str, str]]) -> None:
    pos = start
    while pos + 8 <= end:
        kind = data[pos:pos + 4]
        size = struct.unpack_from(">I", data, pos + 4)[0]
        item_end = pos + 8 + size
        if item_end > end or item_end < pos + 8:
            return
        if kind == b"FORM" and size >= 4:
            form_tag = data[pos + 8:pos + 12].decode("ascii", "replace")
            child = pos + 12
            if form_tag in TARGETS:
                direct = child_items(data, child, item_end)
                if form_tag == "PTMH":
                    direct = [item for item in direct if item != ("FORM", "PTCL")]
                elif form_tag == "PTQD":
                    direct = [item for item in direct if item != ("FORM", "PTCL") and item[0] == "FORM"]
                else:
                    direct = [item for item in direct if item[0] == "FORM"]
                if direct:
                    counts[(form_tag, direct[0][1])] += 1
            walk(data, child, item_end, counts)
        pos = item_end


def main(root_value: str, cfg_value: str, output_value: str) -> None:
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))
    winners: set[str] = set()
    counts: Counter[tuple[str, str]] = Counter()
    samples: dict[str, tuple[str, bytes]] = {}
    live = tombstones = errors = 0
    for _, archive_name in sorted(configured, reverse=True):
        archive = root / archive_name
        if not archive.exists():
            continue
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not name.endswith(".prt"):
                continue
            if usize == 0 or not packed:
                tombstones += 1
                continue
            try:
                data = expand(kind, packed, usize)
                for marker in ("PTQD", "PTMH"):
                    if marker.encode("ascii") in data and marker not in samples:
                        samples[marker] = (name, data)
                walk(data, 0, usize, counts)
                live += 1
            except Exception:
                errors += 1
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("form", "version", "occurrences"))
        for (form, version), count in sorted(counts.items()):
            writer.writerow((form, version, count))
    print(f"live={live} tombstones={tombstones} errors={errors}")
    for (form, version), count in sorted(counts.items()):
        print(form, version, count, sep="\t")
    for marker, (name, _) in sorted(samples.items()):
        print("sample", marker, name, sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:4])
