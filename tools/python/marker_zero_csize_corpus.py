#!/usr/bin/env python3
"""Inventory effective live MKR records affected by TRE kind-0 size metadata."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand
from tre_zero_csize_audit import archive_records as metadata_records


def main(root, cfg, output):
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    winners = {}
    candidates = []
    for priority, archive in sorted(configured, reverse=True):
        metadata = {n: (u, k, c) for n, u, k, c in metadata_records(archive) if n.endswith(".mkr")}
        for name, usize, kind, packed in archive_records(archive):
            if name not in winners:
                winners[name] = bool(usize and packed)
            if not name.endswith(".mkr") or name in seen:
                continue
            seen.add(name)
            _, kind, csize = metadata[name]
            if kind == 0 and csize == 0 and usize > 0:
                candidates.append((name, archive.name, priority, expand(kind, packed, usize)))
    rows = []
    for name, archive, priority, payload in candidates:
        status = "ok"
        shader = ""
        suffix = b""
        if len(payload) < 36 or payload[:4] != b"FORM" or payload[8:12] != b"MKAT" or payload[12:16] != b"FORM" or payload[20:24] != b"0000" or payload[24:28] != b"INFO":
            status = "bad_header"
        elif struct.unpack_from(">I", payload, 4)[0] + 8 != len(payload):
            status = "bad_outer_length"
        else:
            size = struct.unpack_from(">I", payload, 28)[0]
            body = payload[32:32 + size]
            match = re.search(rb"shader[/\\][A-Za-z0-9_./\\-]+\.sht\0", body, re.I)
            if match:
                shader = match.group()[:-1].decode("ascii", "replace").lower().replace("\\", "/")
                suffix = body[match.end():]
            else:
                status = "no_shader_path"
        shader_status = "live" if winners.get(shader) else "masked" if shader in winners else "absent"
        rows.append((name, archive, priority, len(payload), shader, shader_status, len(suffix), suffix.hex(), status))
    with output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(("name", "archive", "priority", "bytes", "shader", "shader_status", "suffix_bytes", "suffix_hex", "status"))
        writer.writerows(rows)
    print("effective_mkr", len(seen), "zero_csize_live", len(rows))
    print("status", Counter(row[-1] for row in rows))
    print("shader_status", Counter(row[5] for row in rows))
    print("suffix_bytes", Counter(row[6] for row in rows))
    for row in rows:
        print("MKR", row)


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
