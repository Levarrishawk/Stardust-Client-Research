#!/usr/bin/env python3
"""Print an IFF tree from an already-extracted file."""
from __future__ import annotations

import struct
import sys
from pathlib import Path


def walk(blob: bytes, start: int, end: int, depth: int = 0) -> None:
    position = start
    while position + 8 <= end:
        tag = blob[position:position + 4]
        size = struct.unpack_from(">I", blob, position + 4)[0]
        stop = position + 8 + size
        if stop > end:
            raise ValueError(f"bad {tag!r} at {position:#x}: {stop:#x}>{end:#x}")
        label = tag.decode("ascii", "replace")
        if tag == b"FORM":
            kind = blob[position + 8:position + 12].decode("ascii", "replace")
            print("  " * depth + f"{position:08x} FORM {kind} size={size}")
            walk(blob, position + 12, stop, depth + 1)
        else:
            payload = blob[position + 8:stop]
            suffix = ""
            if tag in (b"DATA", b"WMAP", b"SMAP"):
                suffix = " hex=" + payload[:64].hex(" ")
            print("  " * depth + f"{position:08x} {label} size={size}{suffix}")
        position = stop
    if position != end:
        raise ValueError(f"trailing {end-position} bytes at {position:#x}")


if __name__ == "__main__":
    data = Path(sys.argv[1]).read_bytes()
    walk(data, 0, len(data))
