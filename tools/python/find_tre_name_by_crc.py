#!/usr/bin/env python3
"""Find TRE member names whose SWG CRC matches requested unsigned values."""
from __future__ import annotations

import argparse
import mmap
import struct
import zlib
from pathlib import Path


def make_table():
    result = []
    for i in range(256):
        c = i << 24
        for _ in range(8):
            c = ((c << 1) ^ 0x04C11DB7) & 0xffffffff if c & 0x80000000 else (c << 1) & 0xffffffff
        result.append(c)
    return result


TABLE = make_table()


def swg_crc(name: str) -> int:
    crc = 0xffffffff
    for value in name.encode("ascii"):
        crc = TABLE[((crc >> 24) ^ value) & 0xff] ^ ((crc << 8) & 0xffffffff)
    return (~crc) & 0xffffffff


def expand(kind, data, expected):
    value = data if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if value is None or len(value) != expected:
        raise ValueError("unsupported TRE table compression")
    return value


parser = argparse.ArgumentParser()
parser.add_argument("root", type=Path)
parser.add_argument("crc", nargs="+", type=lambda x: int(x, 0))
args = parser.parse_args()
targets = set(args.crc)
for archive in sorted(args.root.glob("*.tre")):
    with archive.open("rb") as handle, mmap.mmap(handle.fileno(), 0, access=mmap.ACCESS_READ) as raw:
        if raw[:8] != b"EERT5000": continue
        count, offset, rk, rs, nk, ns, nus = struct.unpack_from("<7I", raw, 8)
        records = expand(rk, raw[offset:offset+rs], count * 24)
        names = expand(nk, raw[offset+rs:offset+rs+ns], nus)
        for index in range(count):
            noff = struct.unpack_from("<I", records, index * 24 + 20)[0]
            end = names.index(0, noff)
            name = names[noff:end].decode("ascii", "replace").lower().replace("\\", "/")
            value = swg_crc(name)
            if value in targets:
                print(f"{value:#010x}\t{archive.name}\t{name}")
