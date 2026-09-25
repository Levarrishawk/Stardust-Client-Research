#!/usr/bin/env python3
"""Check historical vtable TSV rows against bytes in the original PE image.

No disassembly or runtime reachability is inferred. A row is a byte-verified
slot only when its first column is a mapped VA whose u32 equals column two.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import struct
from bisect import bisect_right
from collections import Counter, defaultdict
from pathlib import Path


class PE:
    def __init__(self, path: Path):
        self.data = path.read_bytes()
        peoff = struct.unpack_from("<I", self.data, 0x3C)[0]
        if self.data[:2] != b"MZ" or self.data[peoff:peoff + 4] != b"PE\0\0":
            raise ValueError("not a PE image")
        coff = peoff + 4
        sections = struct.unpack_from("<H", self.data, coff + 2)[0]
        optsize = struct.unpack_from("<H", self.data, coff + 16)[0]
        opt = coff + 20
        if struct.unpack_from("<H", self.data, opt)[0] != 0x10B:
            raise ValueError("expected PE32")
        self.base = struct.unpack_from("<I", self.data, opt + 28)[0]
        self.sections = []
        for index in range(sections):
            at = opt + optsize + 40 * index
            name = self.data[at:at + 8].split(b"\0", 1)[0].decode("ascii", errors="replace")
            vsize, rva, rawsize, rawptr = struct.unpack_from("<IIII", self.data, at + 8)
            flags = struct.unpack_from("<I", self.data, at + 36)[0]
            self.sections.append((name, rva, max(vsize, rawsize), rawsize, rawptr, flags))

    def locate(self, va: int, size: int = 1):
        rva = va - self.base
        for name, start, vsize, rawsize, rawptr, flags in self.sections:
            delta = rva - start
            if 0 <= delta and delta + size <= rawsize and delta + size <= vsize:
                offset = rawptr + delta
                if offset + size <= len(self.data):
                    return offset, name, flags
        return None

    def u32(self, va: int) -> int | None:
        location = self.locate(va, 4)
        return struct.unpack_from("<I", self.data, location[0])[0] if location else None

    def cstr(self, va: int, maximum: int = 256) -> str | None:
        location = self.locate(va)
        if not location:
            return None
        chunk = self.data[location[0]:location[0] + maximum]
        raw = chunk.split(b"\0", 1)[0]
        return raw.decode("ascii", errors="replace") if b"\0" in chunk else None


def parse_hex(value: str) -> int | None:
    try:
        return int(value.strip(), 16)
    except ValueError:
        return None


def rtti_name(pe: PE, first_slot: int) -> str | None:
    locator = pe.u32(first_slot - 4)
    if locator is None or pe.u32(locator) != 0:
        return None
    type_descriptor = pe.u32(locator + 12)
    if type_descriptor is None:
        return None
    name = pe.cstr(type_descriptor + 8)
    return name if name and name.startswith(".?AV") else None


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("image", type=Path)
    p.add_argument("inventory", type=Path)
    p.add_argument("registry", type=Path)
    p.add_argument("exports", type=Path)
    args = p.parse_args()
    pe = PE(args.image)
    with args.inventory.open(encoding="utf-8", newline="") as stream:
        functions = {int(row["entry"], 16): row for row in csv.DictReader(stream, delimiter="\t")}
    with args.registry.open(encoding="utf-8-sig", newline="") as stream:
        curated = {int(row["address"], 16) for row in csv.DictReader(stream)}
    intervals = sorted((entry, entry + int(row["size"]), row["name"]) for entry, row in functions.items())
    starts = [item[0] for item in intervals]

    def owner(va: int) -> str:
        index = bisect_right(starts, va) - 1
        if index >= 0 and va < intervals[index][1]:
            return f"{intervals[index][0]:08X}:{intervals[index][2]}"
        return "outside_exported_function"

    print(f"SHA256\t{hashlib.sha256(pe.data).hexdigest().upper()}")
    print(f"IMAGEBASE\t{pe.base:08X}")
    for name, rva, vsize, rawsize, _, flags in pe.sections:
        print(f"SECTION\t{name}\t{pe.base+rva:08X}\t{vsize}\t{'execute' if flags & 0x20000000 else 'nonexecute'}")

    rows = Counter()
    verified: dict[str, list[tuple[int, int]]] = defaultdict(list)
    candidate: dict[str, set[int]] = defaultdict(set)
    for path in sorted(args.exports.glob("*vtable*.tsv")):
        with path.open(encoding="utf-8", errors="replace") as stream:
            for line in stream:
                rows["all"] += 1
                fields = line.rstrip("\r\n").split("\t")
                if len(fields) < 2 or (slot := parse_hex(fields[0])) is None or (target := parse_hex(fields[1])) is None:
                    rows["unparsed"] += 1
                    continue
                location = pe.locate(slot, 4)
                if location is None:
                    rows["slot_unmapped"] += 1
                    continue
                if pe.u32(slot) != target:
                    rows["pointer_mismatch"] += 1
                    continue
                rows["pointer_verified"] += 1
                verified[path.name].append((slot, target))
                if target not in functions:
                    rows["verified_nonentry_target"] += 1
                    continue
                targetloc = pe.locate(target)
                if not targetloc or not targetloc[2] & 0x20000000:
                    rows["entry_nonexec"] += 1
                    continue
                rows["verified_exec_entry"] += 1
                candidate[path.name].add(target)

    print("FILE\trows_verified\tunique_exec_entries\tuncurated_generic_65plus\trtti_run_starts")
    all_targets = set()
    for name in sorted(verified):
        slots = sorted(set(slot for slot, _ in verified[name]))
        # An export may include constants or adjacent tables before the first
        # vtable. Test every verified slot for the preceding MSVC locator.
        types = [(slot, rtti_name(pe, slot)) for slot in slots]
        rooted = [(slot, type_name) for slot, type_name in types if type_name]
        exact = candidate[name]
        all_targets.update(exact)
        large = sum(target not in curated and functions[target]["name"].startswith("FUN_") and int(functions[target]["size"]) > 64 for target in exact)
        print(f"FILE\t{name}\t{len(verified[name])}\t{len(exact)}\t{large}\t{len(rooted)}")
        for slot, type_name in rooted[:8]:
            print(f"RTTI\t{name}\t{slot:08X}\t{type_name}")
    print("SUMMARY")
    for key in sorted(rows):
        print(f"{key}\t{rows[key]}")
    print(f"unique_verified_exec_entries\t{len(all_targets)}")
    print(f"curated_verified_exec_entries\t{len(all_targets & curated)}")
    print(f"uncurated_generic_65plus_verified_exec_entries\t{sum(target not in curated and functions[target]['name'].startswith('FUN_') and int(functions[target]['size']) > 64 for target in all_targets)}")

    # Recover each named table from its RTTI-validated start rather than
    # attributing an entire historical TSV slice to the filename's class.
    roots_by_va: dict[int, set[str]] = defaultdict(set)
    for name, pairs in verified.items():
        for slot, _ in pairs:
            if rtti_name(pe, slot):
                roots_by_va[slot].add(name)
    print("TABLE\troot_va\trtti_type\texec_slots\texact_ghidra_entries\tinterior_targets\toutside_exported_intervals\tcurated\tgeneric_65plus_uncurated\texport_files")
    table_targets = set()
    nonentry_targets = set()
    large_owners: dict[int, set[str]] = defaultdict(set)
    for root in sorted(roots_by_va):
        targets = []
        for index in range(256):
            value = pe.u32(root + 4 * index)
            location = pe.locate(value) if value is not None else None
            if not location or not location[2] & 0x20000000:
                break
            targets.append(value)
        exact = {value for value in targets if value in functions}
        table_targets.update(exact)
        nonentry_targets.update(value for value in targets if value not in functions)
        interior = sum(owner(value) != "outside_exported_function" for value in set(targets) - exact)
        outside = len(set(targets) - exact) - interior
        large = sum(value not in curated and functions[value]["name"].startswith("FUN_") and int(functions[value]["size"]) > 64 for value in exact)
        for value in exact:
            if value not in curated and functions[value]["name"].startswith("FUN_") and int(functions[value]["size"]) > 64:
                large_owners[value].add(f"{root:08X}:{rtti_name(pe, root)}")
        print(f"TABLE\t{root:08X}\t{rtti_name(pe, root)}\t{len(targets)}\t{len(exact)}\t{interior}\t{outside}\t{len(exact & curated)}\t{large}\t{','.join(sorted(roots_by_va[root]))}")
    print(f"unique_rtti_table_exact_entries\t{len(table_targets)}")
    print(f"unique_rtti_table_uncurated_generic_65plus\t{sum(value not in curated and functions[value]['name'].startswith('FUN_') and int(functions[value]['size']) > 64 for value in table_targets)}")
    print(f"unique_rtti_table_nonentry_exec_targets\t{len(nonentry_targets)}")
    print(f"unique_rtti_table_nonentry_interior\t{sum(owner(value) != 'outside_exported_function' for value in nonentry_targets)}")
    print(f"unique_rtti_table_nonentry_outside\t{sum(owner(value) == 'outside_exported_function' for value in nonentry_targets)}")
    byte_categories = Counter()
    for value in nonentry_targets:
        at = pe.locate(value, 16)
        if not at:
            byte_categories["unmapped_16_bytes"] += 1
            continue
        head = pe.data[at[0]:at[0] + 16]
        if head[0] == 0xE9 or head[:2] == b"\xff\x25":
            byte_categories["starts_jump"] += 1
        elif head[0] == 0xC3:
            byte_categories["starts_ret"] += 1
        elif 0xC3 in head or 0xC2 in head:
            byte_categories["contains_ret_opcode_in_first_16_raw_bytes"] += 1
        else:
            byte_categories["other_first_16_bytes"] += 1
    for key in sorted(byte_categories):
        print(f"nonentry_byte_class:{key}\t{byte_categories[key]}")
    print("NONENTRY_EXAMPLES\ttarget\towner")
    for value in sorted(nonentry_targets)[:30]:
        print(f"NONENTRY_EXAMPLES\t{value:08X}\t{owner(value)}")
    print("LARGE_CANDIDATE\taddress\tbytes\ttable_roots")
    for value, owners in sorted(large_owners.items()):
        print(f"LARGE_CANDIDATE\t{value:08X}\t{functions[value]['size']}\t{'|'.join(sorted(owners))}")

    # Raw-byte scans are candidate references only. They can hit immediates,
    # displacements or embedded data; no instruction-boundary claim is made.
    text_section = next((s for s in pe.sections if s[0] == ".text"), None)
    if text_section:
        _, rva, _, rawsize, rawptr, _ = text_section
        code = pe.data[rawptr:rawptr + rawsize]
        code_va = pe.base + rva
        incoming = Counter()
        for at in range(len(code) - 4):
            if code[at] != 0xE8:
                continue
            displacement = struct.unpack_from("<i", code, at + 1)[0]
            target = (code_va + at + 5 + displacement) & 0xFFFFFFFF
            if target in all_targets:
                incoming[target] += 1
        print("CANDIDATE_REFS\tfile\troot_va\traw_text_u32_hits\texample_owner\texact_targets_with_raw_E8")
        for name in sorted(verified):
            slots = sorted(set(slot for slot, _ in verified[name]))
            roots = [slot for slot in slots if rtti_name(pe, slot)]
            for root in roots:
                needle = struct.pack("<I", root)
                hit = code.find(needle)
                count = 0
                while hit >= 0:
                    count += 1
                    first = hit if count == 1 else first
                    hit = code.find(needle, hit + 1)
                example = owner(code_va + first) if count else "none"
                print(f"CANDIDATE_REFS\t{name}\t{root:08X}\t{count}\t{example}\t{sum(incoming[t] > 0 for t in candidate[name])}/{len(candidate[name])}")


if __name__ == "__main__":
    main()
