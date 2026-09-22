#!/usr/bin/env python3
"""Extract the effective configured-overlay copy of one TRE member."""
from __future__ import annotations

import re
import struct
import sys
import zlib
from pathlib import Path


def expand(kind: int, data: bytes, expected: int) -> bytes:
    result = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if result is None or len(result) != expected:
        raise ValueError("unsupported or invalid compressed block")
    return result


def find_member(archive: Path, wanted: str) -> bytes | None:
    raw = archive.read_bytes()
    if raw[:8] != b"EERT5000":
        return None
    count, offset, record_kind, record_size, name_kind, name_size, name_usize = struct.unpack_from("<7I", raw, 8)
    records = expand(record_kind, raw[offset:offset + record_size], count * 24)
    names = expand(name_kind, raw[offset + record_size:offset + record_size + name_size], name_usize)
    for index in range(count):
        _, usize, payload_offset, kind, csize, name_offset = struct.unpack_from("<6I", records, index * 24)
        end = names.find(b"\0", name_offset)
        name = names[name_offset:end].decode("ascii", "replace").lower().replace("\\", "/")
        if name == wanted:
            stored_size = usize if kind == 0 else csize
            return expand(kind, raw[payload_offset:payload_offset + stored_size], usize)
    return None


root, cfg, member, output = Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3].lower().replace("\\", "/"), Path(sys.argv[4])
configured: list[tuple[int, str]] = []
for line in cfg.read_text(errors="replace").splitlines():
    match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if match:
        configured.append((int(match.group(1)), match.group(2)))
for priority, archive_name in sorted(configured, reverse=True):
    archive = root / archive_name
    if not archive.exists():
        continue
    payload = find_member(archive, member)
    if payload is not None:
        output.write_bytes(payload)
        print(f"archive={archive_name} priority={priority} member={member} bytes={len(payload)} output={output}")
        break
else:
    raise SystemExit(f"member not found: {member}")
