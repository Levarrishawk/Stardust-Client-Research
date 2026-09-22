#!/usr/bin/env python3
"""Minimal decoder for the original SWG localized-string-table layout."""
from __future__ import annotations

import struct
import sys
from pathlib import Path


def u32(data: bytes, off: int) -> int:
    return struct.unpack_from("<I", data, off)[0]


for arg in sys.argv[1:]:
    path = Path(arg)
    data = path.read_bytes()
    if data[:2] != b"\xcd\xab":
        raise SystemExit(f"{path}: bad STF magic")
    variant = data[4]
    next_identity = u32(data, 5)
    name_count = u32(data, 9)
    value_count = name_count
    off = 13
    values: dict[int, str] = {}
    for record_index in range(value_count):
        if off + 12 > len(data):
            raise ValueError(f"value record {record_index} header beyond EOF at {off:#x}")
        identity, _crc, length = struct.unpack_from("<III", data, off)
        off += 12
        if off + length * 2 > len(data):
            raise ValueError(f"value record {record_index} id={identity} length={length} overruns at {off:#x}")
        values[identity] = data[off:off + length * 2].decode("utf-16-le", "replace")
        off += length * 2
    names: dict[int, str] = {}
    for _ in range(name_count):
        identity, length = struct.unpack_from("<II", data, off)
        off += 8
        names[identity] = data[off:off + length].decode("ascii", "replace")
        off += length
    print(f"file={path} bytes={len(data)} variant={variant} next_identity={next_identity} values={value_count} names={name_count} trailing={len(data)-off}")
    for identity, name in sorted(names.items()):
        value = values.get(identity, "").replace("\n", "\\n")
        print(f"{identity}\t{name}\t{value}")
    unnamed = sorted(set(values) - set(names))
    if unnamed:
        print("unnamed identities=" + ",".join(map(str, unnamed)))
