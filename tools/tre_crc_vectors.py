#!/usr/bin/env python3
"""Verify SWG non-reflected CRC-32 filename checksums against TRE indexes."""
from __future__ import annotations
import argparse, struct, zlib
from pathlib import Path

POLY = 0x04C11DB7

def make_table():
    table = []
    for value in range(256):
        crc = value << 24
        for _ in range(8):
            crc = ((crc << 1) ^ POLY) & 0xffffffff if crc & 0x80000000 else (crc << 1) & 0xffffffff
        table.append(crc)
    return table

TABLE = make_table()

def swg_crc(data: bytes) -> int:
    crc = 0xffffffff
    for value in data:
        crc = ((crc << 8) & 0xffffffff) ^ TABLE[((crc >> 24) ^ value) & 0xff]
    return (~crc) & 0xffffffff

def expand(kind: int, data: bytes, expected: int) -> bytes:
    result = zlib.decompress(data) if kind == 2 else data[:expected]
    if len(result) != expected:
        raise ValueError((len(result), expected))
    return result

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("archive", type=Path)
    parser.add_argument("--limit", type=int, default=12)
    args = parser.parse_args()
    raw = args.archive.read_bytes()
    count, offset, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    records = expand(rk, raw[offset:offset+rs], count * 24)
    names = expand(nk, raw[offset+rs:offset+rs+ns], nu)
    failures = 0
    for index in range(count):
        stored, _, _, _, _, name_offset = struct.unpack_from("<6I", records, index * 24)
        end = names.index(0, name_offset)
        name = names[name_offset:end]
        calculated = swg_crc(name)
        if calculated != stored:
            failures += 1
        if index < args.limit:
            print(f"{name.decode('ascii')}\tstored={stored:08X}\tcalculated={calculated:08X}\tmatch={calculated == stored}")
    print(f"records={count} failures={failures}")

if __name__ == "__main__":
    main()
