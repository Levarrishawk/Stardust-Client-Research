#!/usr/bin/env python3
"""Decode the positional MOOD/SPCT catalogs used by the original client."""
from __future__ import annotations

import struct
import sys
from pathlib import Path


def u32be(data: bytes, off: int) -> int:
    return struct.unpack_from(">I", data, off)[0]


def chunks(data: bytes, start: int = 0, end: int | None = None):
    end = len(data) if end is None else end
    off = start
    while off + 8 <= end:
        tag = data[off:off + 4].decode("ascii", "replace")
        size = u32be(data, off + 4)
        payload = off + 8
        stop = payload + size
        if stop > end:
            raise ValueError(f"chunk {tag} at {off:#x} overruns parent")
        yield tag, payload, stop
        # SWG's IFF reader advances by the declared length without RIFF-style
        # even-byte padding; odd-sized VOLT records are adjacent in SPCT.
        off = stop


def cstrings(data: bytes) -> list[str]:
    return [part.decode("ascii", "replace") for part in data.split(b"\0") if part]


def descend(data: bytes, start: int, end: int, path: tuple[str, ...] = ()):
    for tag, payload, stop in chunks(data, start, end):
        if tag == "FORM":
            form = data[payload:payload + 4].decode("ascii", "replace")
            yield from descend(data, payload + 4, stop, path + (form,))
        else:
            yield path, tag, data[payload:stop]


def parse(path: Path) -> None:
    data = path.read_bytes()
    print(f"file={path} bytes={len(data)}")
    for form_path, tag, payload in descend(data, 0, len(data)):
        key = "/".join(form_path + (tag,))
        if tag == "TYPS":
            names = cstrings(payload)
            print(f"{key}: count={len(names)}")
            for idx, name in enumerate(names, 1):
                print(f"  {idx}\t{name}")
        elif "VOLS" in form_path:
            nul = payload.find(b"\0")
            name = payload[:nul].decode("ascii", "replace")
            distance = struct.unpack_from("<H", payload, nul + 1)[0]
            print(f"{key}: name={name or '<default>'} distance={distance}")
        elif "SKCS" in form_path:
            values = cstrings(payload)
            print(f"{key}: " + " -> ".join(values))
        else:
            print(f"{key}: bytes={len(payload)}")


for arg in sys.argv[1:]:
    parse(Path(arg))
