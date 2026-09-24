#!/usr/bin/env python3
"""Map CuiAction registration globals to executable string initializers.

Input decompilations are private Ghidra scratch files. Output is a discovery
inventory, not proof of each action's runtime effect.
"""
from __future__ import annotations

import argparse
import re
import struct
from pathlib import Path

import pefile


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("exe", type=Path)
    parser.add_argument("decomp", type=Path, nargs="+")
    parser.add_argument("--fallback-decomp-dir", type=Path)
    args = parser.parse_args()
    pe = pefile.PE(str(args.exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    image = pe.get_memory_mapped_image()
    targets: set[int] = set()
    for path in args.decomp:
        source = path.read_text(encoding="utf-8", errors="replace")
        targets.update(int(s, 16) for s in re.findall(r"FUN_008cc650\(&DAT_([0-9a-fA-F]{8})", source))

    def cstring(va: int) -> str | None:
        pos = va - base
        if pos < 0 or pos >= len(image):
            return None
        end = image.find(b"\0", pos, min(pos + 128, len(image)))
        if end < 0:
            return None
        raw = image[pos:end]
        if not raw or any(c < 32 or c > 126 for c in raw):
            return None
        return raw.decode("ascii")

    text_sections = [s for s in pe.sections if s.Characteristics & 0x20000000]
    fallbacks: dict[int, tuple[int, str]] = {}
    if args.fallback_decomp_dir:
        for path in args.fallback_decomp_dir.glob("*.c"):
            source = path.read_text(encoding="utf-8", errors="replace")
            global_match = re.search(r"DAT_(019[0-9a-fA-F]{5})", source)
            literal_match = re.search(r"pcVar5 = s_\w+_([0-9a-fA-F]{8});", source)
            entry_match = re.match(r"([0-9a-fA-F]{8})_", path.name)
            if global_match and literal_match and entry_match:
                target = int(global_match.group(1), 16)
                literal = cstring(int(literal_match.group(1), 16))
                if target in targets and literal:
                    fallbacks[target] = (int(entry_match.group(1), 16), literal)

    print("global_va\taction_literal\tinitializer_va\tstatus")
    for target in sorted(targets):
        hits: list[tuple[int, str]] = []
        for section in text_sections:
            data = section.get_data()
            # push <literal>; mov ecx,<global>; call <string constructor>
            needle = b"\xb9" + struct.pack("<I", target) + b"\xe8"
            pos = data.find(needle)
            while pos >= 0:
                if pos >= 5 and data[pos - 5] == 0x68:
                    literal_va = struct.unpack_from("<I", data, pos - 4)[0]
                    literal = cstring(literal_va)
                    if literal is not None:
                        hits.append((base + section.VirtualAddress + pos - 5, literal))
                pos = data.find(needle, pos + 1)
        if hits:
            for va, literal in hits:
                print(f"{target:08X}\t{literal}\t{va:08X}\tmatched")
        elif target in fallbacks:
            va, literal = fallbacks[target]
            print(f"{target:08X}\t{literal}\t{va:08X}\tfallback")
        else:
            print(f"{target:08X}\t\t\tunresolved")


if __name__ == "__main__":
    main()
