#!/usr/bin/env python3
"""Inventory effective SWG interior-layout files from a configured TRE overlay."""
from __future__ import annotations

import argparse
import csv
import re
import struct
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def configured_archives(root: Path, cfg: Path) -> list[tuple[int, Path]]:
    result: list[tuple[int, Path]] = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            result.append((int(match.group(1)), root / match.group(2)))
    return sorted(result, reverse=True)


def chunk(data: bytes, offset: int, expected: bytes) -> tuple[bytes, int]:
    tag, size = struct.unpack_from(">4sI", data, offset)
    if tag != expected or offset + 8 + size > len(data):
        raise ValueError(f"expected {expected!r} at {offset:#x}, got {tag!r}")
    return data[offset + 8 : offset + 8 + size], offset + 8 + size


def cstring(data: bytes, offset: int) -> tuple[str, int]:
    end = data.find(b"\0", offset)
    if end < 0:
        raise ValueError("unterminated string")
    return data[offset:end].decode("ascii", "replace"), end + 1


def parse_ilf(data: bytes) -> tuple[str, list[tuple[str, str, tuple[float, ...]]]]:
    outer, end = chunk(data, 0, b"FORM")
    if end != len(data) or outer[:4] != b"INLY":
        raise ValueError("not a complete FORM/INLY")
    version_form, version_end = chunk(outer, 4, b"FORM")
    if version_end != len(outer):
        raise ValueError("trailing outer data")
    version = version_form[:4].decode("ascii", "replace")
    nodes: list[tuple[str, str, tuple[float, ...]]] = []
    cursor = 4
    while cursor < len(version_form):
        payload, cursor = chunk(version_form, cursor, b"NODE")
        template_name, pos = cstring(payload, 0)
        cell_name, pos = cstring(payload, pos)
        if len(payload) - pos != 48:
            raise ValueError(f"NODE has {len(payload) - pos} transform bytes")
        # IFF chunk sizes/tags are big-endian; SWG numeric payloads are little-endian.
        transform = struct.unpack_from("<12f", payload, pos)
        nodes.append((template_name, cell_name, transform))
    return version, nodes


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("cfg", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    winners: set[str] = set()
    rows: list[tuple[object, ...]] = []
    versions: Counter[str] = Counter()
    templates: Counter[str] = Counter()
    cells: Counter[str] = Counter()
    archives: Counter[str] = Counter()
    errors: list[tuple[str, str, str]] = []
    tombstones = 0
    for priority, archive in configured_archives(args.root, args.cfg):
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not name.endswith(".ilf"):
                continue
            if not packed:
                tombstones += 1
                continue
            try:
                version, nodes = parse_ilf(expand(kind, packed, usize))
            except Exception as exc:
                errors.append((archive.name, name, str(exc)))
                continue
            archives[archive.name] += 1
            versions[version] += 1
            for index, (template_name, cell_name, transform) in enumerate(nodes):
                templates[template_name] += 1
                cells[cell_name] += 1
                rows.append((archive.name, priority, name, version, index, template_name, cell_name, *transform))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("archive", "priority", "layout", "version", "node", "template", "cell", *[f"m{i}" for i in range(12)]))
        writer.writerows(rows)

    print(f"effective_ilf={sum(versions.values())} tombstones={tombstones} versions={dict(versions)}")
    print(f"nodes={len(rows)} unique_templates={len(templates)} unique_cells={len(cells)} errors={len(errors)}")
    print("archives=" + repr(dict(archives)))
    print("top_templates=" + repr(templates.most_common(10)))
    print("top_cells=" + repr(cells.most_common(10)))
    for error in errors[:20]:
        print("ERROR\t" + "\t".join(error))


if __name__ == "__main__":
    main()
