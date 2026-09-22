#!/usr/bin/env python3
"""Inspect ENVM/0000 chunks and decode repeated CELS records."""
from __future__ import annotations

import argparse
import struct
from pathlib import Path


def cstring(data: bytes, offset: int) -> tuple[str, int]:
    end = data.index(0, offset)
    return data[offset:end].decode("ascii", errors="replace"), end + 1


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("path", type=Path)
    args = parser.parse_args()
    data = args.path.read_bytes()
    if data[:4] != b"FORM" or data[8:12] != b"ENVM" or data[12:16] != b"FORM":
        raise SystemExit("not FORM/ENVM/FORM")
    pos = 24  # outer FORM, ENVM type, nested FORM, 0000 type
    index = 0
    while pos + 8 <= len(data):
        tag = data[pos:pos + 4].decode("ascii", errors="replace")
        size = struct.unpack_from(">I", data, pos + 4)[0]
        payload = data[pos + 8:pos + 8 + size]
        if len(payload) != size:
            raise SystemExit(f"truncated {tag} at {pos:#x}")
        print(f"{pos:#06x} {tag!r} size={size}", end="")
        if tag == "CELS":
            off = 0
            primary, off = cstring(payload, off)
            primary_size = struct.unpack_from("<f", payload, off)[0]; off += 4
            secondary, off = cstring(payload, off)
            secondary_size = struct.unpack_from("<f", payload, off)[0]; off += 4
            values = struct.unpack_from("<4f", payload, off); off += 16
            print(
                f" index={index} primary={primary!r} primarySize={primary_size:g}"
                f" secondary={secondary!r} secondarySize={secondary_size:g}"
                f" angles/animation={tuple(round(value, 6) for value in values)}"
                f" trailing={payload[off:].hex()}"
            )
            index += 1
        else:
            print(f" payload={payload.hex()}")
        # This client IFF stream packs following chunks immediately after the
        # declared payload; odd-sized chunks are not externally padded.
        pos += 8 + size


if __name__ == "__main__":
    main()
