#!/usr/bin/env python3
"""Inventory raw scalar fields in effective EMTR/0009-0014 DATA chunks."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter, defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


def items(data: bytes, start: int, end: int):
    pos = start
    while pos + 8 <= end:
        kind = data[pos:pos + 4]
        size = struct.unpack_from(">I", data, pos + 4)[0]
        stop = pos + 8 + size
        if stop > end or stop < pos + 8:
            return
        yield pos, kind, size, stop
        # SWG's IFF containers advance directly to the declared end.  Unlike
        # classic EA-IFF, odd-sized chunks in this corpus have no external pad.
        pos = stop


def cstring(payload: bytes, pos: int):
    stop = payload.find(b"\0", pos)
    if stop < 0:
        raise ValueError("unterminated string")
    return payload[pos:stop].decode("ascii", "replace"), stop + 1


def decode_chunk(version: str, payload: bytes):
    pos = 0
    name0, pos = cstring(payload, pos)
    fields: list[tuple[str, str]] = [("string0", name0)]
    # Fixed modern prefix, preserving raw order rather than speculative names.
    widths = [4, 4, 4, 1, 4, 4, 4, 1, 4, 4, 1, 4, 1, 4, 4]
    for index, width in enumerate(widths):
        raw = payload[pos:pos + width]
        if len(raw) != width:
            raise ValueError("short fixed prefix")
        fields.append((f"prefix{index:02d}", raw.hex()))
        pos += width
    name1, pos = cstring(payload, pos)
    fields.append(("string1", name1))
    tail = payload[pos:]
    # Versions 0009-0011 carry one additional legacy dword after the three
    # booleans.  Version 0012 removes that value and changes the following
    # flocking mode from a byte to a dword.  This boundary is loader-confirmed;
    # treating 39 bytes as common to every version silently shifts 0012+ fields.
    for index, value in enumerate(tail[:3]):
        fields.append((f"common_bool{index:02d}", f"{value:02x}"))
    for index in range(8):
        begin = 3 + index * 4
        fields.append((f"common_dword{index:02d}", tail[begin:begin + 4].hex()))
    extension_start = 35
    if version in {"0009", "0010", "0011"}:
        fields.append(("legacy_extra_dword", tail[35:39].hex()))
        extension_start = 39
    fields.append(("extension_raw", tail[extension_start:].hex()))
    return fields, len(payload), len(tail)


def walk(data: bytes, start: int, end: int, path: str, rows, errors):
    for pos, kind, size, stop in items(data, start, end):
        if kind != b"FORM" or size < 4:
            continue
        form = data[pos + 8:pos + 12]
        child_start = pos + 12
        if form == b"EMTR":
            children = list(items(data, child_start, stop))
            if not children or children[0][1] != b"FORM":
                errors.append((path, f"bad EMTR at {pos}"))
                continue
            vpos, _, _, vstop = children[0]
            version = data[vpos + 8:vpos + 12].decode("ascii", "replace")
            if version not in {"0009", "0010", "0011", "0012", "0013", "0014"}:
                continue
            vchildren = list(items(data, vpos + 12, vstop))
            chunks = [(p, s, e) for p, k, s, e in vchildren if k == b"0000"]
            if not chunks:
                errors.append((path, f"missing DATA in EMTR/{version} at {pos}"))
                continue
            p, s, e = chunks[0]
            try:
                fields, payload_len, tail_len = decode_chunk(version, data[p + 8:e])
                for field, raw in fields:
                    rows.append((path, pos, version, payload_len, tail_len, field, raw))
            except Exception as exc:
                errors.append((path, f"EMTR/{version} at {pos}: {exc}"))
        walk(data, child_start, stop, path, rows, errors)


def main(root_value: str, cfg_value: str, output_value: str) -> None:
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))
    winners: set[str] = set()
    rows = []
    errors = []
    for _, archive_name in sorted(configured, reverse=True):
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
                data = expand(kind, packed, usize)
                walk(data, 0, len(data), name, rows, errors)
            except Exception as exc:
                errors.append((name, str(exc)))
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("asset", "emtr_offset", "version", "payload_bytes", "tail_bytes", "field", "raw_le"))
        writer.writerows(rows)
    summary = defaultdict(Counter)
    tail_lengths = Counter()
    for _, _, version, _, tail_len, field, raw in rows:
        summary[(version, field)][raw] += 1
        if field == "string0":
            tail_lengths[(version, tail_len)] += 1
    print(f"field_rows={len(rows)} errors={len(errors)}")
    for (version, tail_len), count in sorted(tail_lengths.items()):
        print("TAIL", version, tail_len, count, sep="\t")
    for key, values in sorted(summary.items()):
        version, field = key
        examples = ";".join(f"{value}:{count}" for value, count in values.most_common(6))
        print(version, field, len(values), examples, sep="\t")
    for error in errors[:20]:
        print("ERROR", *error, sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:4])
