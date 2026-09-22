#!/usr/bin/env python3
"""Inventory ordered EMTR WVFM payloads without assigning semantics."""
from __future__ import annotations

import csv
import hashlib
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
        pos = stop


def walk(data: bytes, start: int, end: int, asset: str, rows: list[tuple]):
    for pos, kind, size, stop in items(data, start, end):
        if kind != b"FORM" or size < 4:
            continue
        form = data[pos + 8:pos + 12]
        child = pos + 12
        if form == b"EMTR":
            outer = list(items(data, child, stop))
            if outer and outer[0][1] == b"FORM":
                vpos, _, _, vstop = outer[0]
                version = data[vpos + 8:vpos + 12].decode("ascii", "replace")
                ordinal = 0
                for wpos, wkind, wsize, wstop in items(data, vpos + 12, vstop):
                    if wkind == b"FORM" and data[wpos + 8:wpos + 12] == b"WVFM":
                        raw = data[wpos:wstop]
                        digest = hashlib.sha256(raw).hexdigest().upper()
                        nested = list(items(data, wpos + 12, wstop))
                        nested_version = nested[0][1].decode("ascii", "replace") if nested else ""
                        payload = data[nested[0][0] + 8:nested[0][3]] if nested else b""
                        rows.append((asset, pos, version, ordinal, wsize, nested_version, digest, payload.hex()))
                        ordinal += 1
        walk(data, child, stop, asset, rows)


def main(root_value: str, cfg_value: str, output_value: str) -> None:
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))
    winners: set[str] = set()
    rows: list[tuple] = []
    errors = 0
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
                walk(expand(kind, packed, usize), 0, usize, name, rows)
            except Exception:
                errors += 1
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("asset", "emtr_offset", "emtr_version", "ordinal", "form_size", "wvfm_version", "sha256", "payload_hex"))
        writer.writerows(rows)
    grouped = defaultdict(list)
    for row in rows:
        grouped[row[3]].append(row)
    print(f"waveforms={len(rows)} scan_errors={errors} emitters={len(set((r[0], r[1]) for r in rows))}")
    for ordinal in sorted(grouped):
        values = grouped[ordinal]
        hashes = Counter(row[6] for row in values)
        sizes = Counter(row[4] for row in values)
        shaped = nonzero_values = 0
        version_shapes = Counter()
        for row in values:
            payload = bytes.fromhex(row[7])
            version = row[5]
            header_size = 12 if version == "0001" else 20 if version == "0002" else 0
            count_offset = 8 if version == "0001" else 16 if version == "0002" else -1
            if header_size and len(payload) >= header_size:
                count = struct.unpack_from("<I", payload, count_offset)[0]
                if len(payload) == header_size + count * 16:
                    shaped += 1
                    version_shapes[version] += 1
                    if any(struct.unpack_from("<f", payload, header_size + key * 16 + lane * 4)[0] != 0.0
                           for key in range(count) for lane in (1, 2, 3)):
                        nonzero_values += 1
        print(f"ordinal={ordinal} records={len(values)} unique_payloads={len(hashes)} common_hash_count={hashes.most_common(1)[0][1]} shaped={shaped} shapes_by_version={dict(version_shapes)} nonzero_value_records={nonzero_values} sizes={dict(sizes)}")


if __name__ == "__main__":
    main(*sys.argv[1:4])
