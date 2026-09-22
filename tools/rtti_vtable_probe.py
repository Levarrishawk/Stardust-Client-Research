#!/usr/bin/env python3
"""Resolve x86 MSVC vtable addresses to raw RTTI type names in a PE32 image."""

from __future__ import annotations

import argparse
import struct
from pathlib import Path

import pefile


def read_u32(pe: pefile.PE, va: int) -> int:
    off = pe.get_offset_from_rva(va - pe.OPTIONAL_HEADER.ImageBase)
    return struct.unpack_from("<I", pe.__data__, off)[0]


def read_cstr(pe: pefile.PE, va: int) -> str:
    off = pe.get_offset_from_rva(va - pe.OPTIONAL_HEADER.ImageBase)
    end = pe.__data__.find(b"\0", off)
    return pe.__data__[off:end].decode("ascii", "replace")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image", type=Path)
    parser.add_argument("addresses", nargs="+")
    args = parser.parse_args()
    pe = pefile.PE(str(args.image), fast_load=True)
    for raw in args.addresses:
        vtable = int(raw, 0)
        try:
            locator = read_u32(pe, vtable - 4)
            type_descriptor = read_u32(pe, locator + 12)
            name = read_cstr(pe, type_descriptor + 8)
            print(f"0x{vtable:08X}\t0x{type_descriptor:08X}\t{name}")
        except Exception as exc:
            print(f"0x{vtable:08X}\tERROR\t{exc}")


if __name__ == "__main__":
    main()
