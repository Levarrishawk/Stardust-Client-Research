#!/usr/bin/env python3
"""Read-only PE32 disassembly and direct-reference probe for stable VA research."""
from __future__ import annotations

import argparse
import struct
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_32, Cs
from capstone.x86 import X86_OP_IMM, X86_OP_MEM


def number(value: str) -> int:
    return int(value, 0)


class Image:
    def __init__(self, path: Path):
        self.path = path
        self.raw = path.read_bytes()
        self.pe = pefile.PE(data=self.raw, fast_load=True)
        self.base = self.pe.OPTIONAL_HEADER.ImageBase
        self.md = Cs(CS_ARCH_X86, CS_MODE_32)
        self.md.detail = True
        # Stardust.exe's executable section contains alignment/data islands.
        # Continue scanning after them so xref reports cover later modules.
        self.md.skipdata = True

    def offset(self, va: int) -> int:
        return self.pe.get_offset_from_rva(va - self.base)

    def bytes(self, va: int, size: int) -> bytes:
        offset = self.offset(va)
        return self.raw[offset : offset + size]

    def executable_sections(self):
        for section in self.pe.sections:
            if section.Characteristics & 0x20000000:
                start = self.base + section.VirtualAddress
                data = section.get_data()
                yield section.Name.rstrip(b"\0").decode("ascii", "replace"), start, data


def disassemble(image: Image, va: int, size: int) -> None:
    for instruction in image.md.disasm(image.bytes(va, size), va):
        data = instruction.bytes.hex(" ")
        print(f"{instruction.address:08X}  {data:<24} {instruction.mnemonic:<8} {instruction.op_str}")


def references(image: Image, target: int) -> None:
    packed = struct.pack("<I", target)
    print("RAW_DWORD_REFERENCES")
    for section_name, start, data in image.executable_sections():
        at = data.find(packed)
        while at >= 0:
            print(f"{start + at:08X}  {section_name}")
            at = data.find(packed, at + 1)

    print("DECODED_IMMEDIATE_OR_MEMORY_REFERENCES")
    for section_name, start, data in image.executable_sections():
        for instruction in image.md.disasm(data, start):
            if instruction.id == 0:  # Capstone skip-data pseudo instruction
                continue
            matched = False
            for operand in instruction.operands:
                if operand.type == X86_OP_IMM and operand.imm & 0xFFFFFFFF == target:
                    matched = True
                elif operand.type == X86_OP_MEM and operand.mem.disp & 0xFFFFFFFF == target:
                    matched = True
            if matched:
                print(f"{instruction.address:08X}  {section_name:<8} {instruction.mnemonic:<8} {instruction.op_str}")

    print("RAW_REL32_CALL_OR_JUMP_REFERENCES")
    for section_name, start, data in image.executable_sections():
        for offset in range(0, len(data) - 4):
            if data[offset] not in (0xE8, 0xE9):
                continue
            displacement = struct.unpack_from("<i", data, offset + 1)[0]
            destination = start + offset + 5 + displacement
            if destination == target:
                operation = "call" if data[offset] == 0xE8 else "jump"
                print(f"{start + offset:08X}  {section_name:<8} {operation}")


def references_many(image: Image, targets: list[int]) -> None:
    """Scan executable sections once for several absolute/immediate targets."""
    wanted = {target & 0xFFFFFFFF for target in targets}
    hits = {target: [] for target in wanted}
    for section_name, start, data in image.executable_sections():
        for instruction in image.md.disasm(data, start):
            if instruction.id == 0:
                continue
            found = set()
            for operand in instruction.operands:
                if operand.type == X86_OP_IMM:
                    value = operand.imm & 0xFFFFFFFF
                elif operand.type == X86_OP_MEM:
                    value = operand.mem.disp & 0xFFFFFFFF
                else:
                    continue
                if value in wanted:
                    found.add(value)
            for value in found:
                hits[value].append(
                    f"{instruction.address:08X}  {section_name:<8} "
                    f"{instruction.mnemonic:<8} {instruction.op_str}"
                )
    for target in targets:
        print(f"TARGET {target & 0xFFFFFFFF:08X}")
        for line in hits[target & 0xFFFFFFFF]:
            print(line)


def search_bytes(image: Image, needle: bytes) -> None:
    for section in image.pe.sections:
        name = section.Name.rstrip(b"\0").decode("ascii", "replace")
        start = image.base + section.VirtualAddress
        data = section.get_data()
        at = data.find(needle)
        while at >= 0:
            print(f"{start + at:08X}  {name:<8} file+0x{section.PointerToRawData + at:X}")
            at = data.find(needle, at + 1)


def hexdump(image: Image, va: int, size: int) -> None:
    data = image.bytes(va, size)
    for offset in range(0, len(data), 16):
        chunk = data[offset:offset + 16]
        ascii_text = "".join(chr(b) if 32 <= b < 127 else "." for b in chunk)
        print(f"{va + offset:08X}  {chunk.hex(' '):<47}  {ascii_text}")


def rtti_vtables(image: Image, type_descriptor: int) -> None:
    col_addresses = []
    for section in image.pe.sections:
        start = image.base + section.VirtualAddress
        data = section.get_data()
        for at in range(0, len(data) - 20, 4):
            sig, objoff, cdoff, td, hierarchy = struct.unpack_from("<IIIII", data, at)
            if sig == 0 and objoff < 0x10000 and cdoff < 0x10000 and td == type_descriptor:
                col = start + at
                col_addresses.append(col)
                print(f"COL {col:08X} object_offset=0x{objoff:X} hierarchy={hierarchy:08X}")
    for col in col_addresses:
        packed = struct.pack("<I", col)
        for section in image.pe.sections:
            start = image.base + section.VirtualAddress
            data = section.get_data()
            at = data.find(packed)
            while at >= 0:
                print(f"VTABLE {start + at + 4:08X} locator_slot={start + at:08X}")
                at = data.find(packed, at + 1)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image", type=Path)
    modes = parser.add_subparsers(dest="mode", required=True)
    dis = modes.add_parser("disasm")
    dis.add_argument("va", type=number)
    dis.add_argument("--size", type=number, default=0x200)
    refs = modes.add_parser("xrefs")
    refs.add_argument("target", type=number)
    refs_many = modes.add_parser("xrefs-many")
    refs_many.add_argument("targets", nargs="+", type=number)
    search = modes.add_parser("search")
    search.add_argument("hexbytes", help="hex bytes, with or without spaces")
    ascii_search = modes.add_parser("ascii")
    ascii_search.add_argument("text")
    dump = modes.add_parser("dump")
    dump.add_argument("va", type=number)
    dump.add_argument("--size", type=number, default=0x100)
    rtti = modes.add_parser("rtti")
    rtti.add_argument("type_descriptor", type=number)
    args = parser.parse_args()

    image = Image(args.image)
    if args.mode == "disasm":
        disassemble(image, args.va, args.size)
    elif args.mode == "xrefs":
        references(image, args.target)
    elif args.mode == "xrefs-many":
        references_many(image, args.targets)
    elif args.mode == "search":
        search_bytes(image, bytes.fromhex(args.hexbytes))
    elif args.mode == "ascii":
        search_bytes(image, args.text.encode("ascii"))
    elif args.mode == "dump":
        hexdump(image, args.va, args.size)
    else:
        rtti_vtables(image, args.type_descriptor)


if __name__ == "__main__":
    main()
