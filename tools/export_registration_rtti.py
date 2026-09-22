#!/usr/bin/env python3
"""Join replication registration vtables with raw x86 MSVC RTTI type names."""

from __future__ import annotations

import argparse
import csv
import struct
from pathlib import Path

import pefile


def u32(pe: pefile.PE, va: int) -> int:
    offset = pe.get_offset_from_rva(va - pe.OPTIONAL_HEADER.ImageBase)
    return struct.unpack_from("<I", pe.__data__, offset)[0]


def cstring(pe: pefile.PE, va: int) -> str:
    offset = pe.get_offset_from_rva(va - pe.OPTIONAL_HEADER.ImageBase)
    end = pe.__data__.find(b"\0", offset)
    return pe.__data__[offset:end].decode("ascii", "replace")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image", type=Path)
    parser.add_argument("registration_inventory", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--overrides", type=Path)
    args = parser.parse_args()

    pe = pefile.PE(str(args.image), fast_load=True)
    with args.registration_inventory.open(newline="", encoding="utf-8") as stream:
        rows = list(csv.DictReader(stream, delimiter="\t"))

    overrides = {}
    if args.overrides:
        with args.overrides.open(newline="", encoding="utf-8") as stream:
            for item in csv.DictReader(stream):
                key = (item["constructor_va"].upper(), item["view"], item["added_ordinal"])
                overrides[key] = item

    cache: dict[int, tuple[str, str]] = {}
    for row in rows:
        key = (row["constructor_va"].upper(), row["view"], row["added_ordinal"])
        if not row["vtable_va"] and key in overrides:
            row["vtable_va"] = overrides[key]["vtable_va"]
            row["vtable_evidence"] = overrides[key]["evidence"]
        else:
            row["vtable_evidence"] = "constructor pseudocode" if row["vtable_va"] else ""
        raw = row["vtable_va"]
        if not raw:
            row["rtti_type_descriptor_va"] = ""
            row["rtti_raw_name"] = ""
            continue
        vtable = int(raw, 0)
        try:
            if vtable not in cache:
                locator = u32(pe, vtable - 4)
                descriptor = u32(pe, locator + 12)
                cache[vtable] = (f"0x{descriptor:08X}", cstring(pe, descriptor + 8))
            row["rtti_type_descriptor_va"], row["rtti_raw_name"] = cache[vtable]
        except Exception as exc:
            row["rtti_type_descriptor_va"] = "ERROR"
            row["rtti_raw_name"] = str(exc)

    fields = list(rows[0].keys())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
    print(f"wrote {len(rows)} registration/RTTI rows ({len(cache)} unique vtables) to {args.output}")


if __name__ == "__main__":
    main()
