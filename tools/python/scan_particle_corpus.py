#!/usr/bin/env python3
"""Inventory effective .prt winners and their top-level IFF envelopes."""
from __future__ import annotations

import csv
import re
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


def tag(data: bytes, offset: int) -> str:
    return data[offset:offset + 4].decode("ascii", "replace") if len(data) >= offset + 4 else ""


def main(root_value: str, cfg_value: str, output_value: str) -> None:
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured: list[tuple[int, str]] = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))

    winners: set[str] = set()
    rows: list[tuple[object, ...]] = []
    errors: list[tuple[str, str, str]] = []
    for priority, archive_name in sorted(configured, reverse=True):
        archive = root / archive_name
        if not archive.exists():
            continue
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not name.endswith(".prt"):
                continue
            try:
                data = expand(kind, packed, usize)
                outer = tag(data, 8) if tag(data, 0) == "FORM" else ""
                version = tag(data, 20) if outer == "PEFT" and tag(data, 12) == "FORM" else ""
                first_kind = tag(data, 24) if version else ""
                first_tag = tag(data, 32) if first_kind == "FORM" else first_kind
                rows.append((archive_name, priority, name, usize, outer, version, first_tag))
            except Exception as exc:
                errors.append((archive_name, name, str(exc)))

    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("archive", "priority", "name", "bytes", "outer", "version", "first_child"))
        writer.writerows(rows)
    summary: dict[tuple[str, str, str], int] = {}
    for row in rows:
        key = (str(row[4]), str(row[5]), str(row[6]))
        summary[key] = summary.get(key, 0) + 1
    print(f"configured={len(configured)} effective_names={len(winners)} particles={len(rows)} errors={len(errors)}")
    for key, count in sorted(summary.items(), key=lambda item: (-item[1], item[0])):
        print(count, *key, sep="\t")
    for error in errors[:20]:
        print("ERROR", *error, sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:4])
