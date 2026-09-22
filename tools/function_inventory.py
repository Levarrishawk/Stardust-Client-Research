#!/usr/bin/env python3
"""Conservative PE32 function-candidate and research-address inventory.

This is not a function-boundary recovery engine. It reports reproducible lower
bounds from direct rel32 call/jump targets and common MSVC entry patterns, then
compares those addresses with stable VA identifiers recorded in the research
Markdown. Indirect-only, tail-merged, exception-described, and unusual-entry
functions will be undercounted.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_IMM


VA_NAME = re.compile(r"\b[A-Za-z][A-Za-z0-9_]*_VA_([0-9A-Fa-f]{8})\b")
VA_LITERAL = re.compile(r"\b0x([0-9A-Fa-f]{8})\b")


def executable_sections(pe: pefile.PE):
    base = pe.OPTIONAL_HEADER.ImageBase
    for section in pe.sections:
        if section.Characteristics & 0x20000000:
            start = base + section.VirtualAddress
            data = section.get_data()
            yield section.Name.rstrip(b"\0").decode("ascii", "replace"), start, data


def direct_targets(pe: pefile.PE):
    ranges = [(start, start + len(data)) for _, start, data in executable_sections(pe)]
    calls = {pe.OPTIONAL_HEADER.ImageBase + pe.OPTIONAL_HEADER.AddressOfEntryPoint}
    jumps: set[int] = set()
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    md.skipdata = True
    for _, start, data in executable_sections(pe):
        for insn in md.disasm(data, start):
            if insn.mnemonic not in ("call", "jmp") or not insn.operands:
                continue
            if insn.operands[0].type != X86_OP_IMM:
                continue
            target = insn.operands[0].imm
            if any(lo <= target < hi for lo, hi in ranges):
                (calls if insn.mnemonic == "call" else jumps).add(target)
    return calls, jumps


def prologue_candidates(pe: pefile.PE) -> set[int]:
    # Conservative classic MSVC starts. These are candidates, not proof.
    patterns = (b"\x55\x8b\xec", b"\x56\x8b\xf1", b"\x57\x8b\xf9")
    result: set[int] = set()
    for _, start, data in executable_sections(pe):
        for pattern in patterns:
            pos = 0
            while True:
                pos = data.find(pattern, pos)
                if pos < 0:
                    break
                result.add(start + pos)
                pos += 1
    return result


def documented_addresses(root: Path):
    stable: set[int] = set()
    literals: set[int] = set()
    files = 0
    for path in root.rglob("*.md"):
        files += 1
        text = path.read_text(encoding="utf-8", errors="replace")
        stable.update(int(x, 16) for x in VA_NAME.findall(text))
        literals.update(int(x, 16) for x in VA_LITERAL.findall(text))
    return files, stable, literals


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("image", type=Path)
    ap.add_argument("research_root", type=Path)
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()

    pe = pefile.PE(str(args.image), fast_load=True)
    calls, jumps = direct_targets(pe)
    prologues = prologue_candidates(pe)
    candidates = calls | prologues
    files, stable, literals = documented_addresses(args.research_root)
    result = {
        "methodology": "function candidates are the union of internal direct-call targets, PE entry point, and three common x86 MSVC prologues; jump targets are reported separately",
        "markdown_files_scanned": files,
        "direct_call_target_candidates": len(calls),
        "direct_jump_targets": len(jumps),
        "prologue_candidates": len(prologues),
        "union_candidates": len(candidates),
        "stable_named_vas": len(stable),
        "all_literal_vas": len(literals),
        "stable_names_intersect_candidates": len(stable & candidates),
        "literal_vas_intersect_candidates": len(literals & candidates),
        "stable_candidate_ratio_percent": round(100 * len(stable & candidates) / len(candidates), 2),
        "literal_candidate_ratio_percent": round(100 * len(literals & candidates) / len(candidates), 2),
        "limitations": [
            "not authoritative function boundaries",
            "misses indirect-only and unusual entries",
            "prologue matches can occur inside functions or data",
            "a Markdown address mention is not proof of semantic documentation",
        ],
    }
    print(json.dumps(result, indent=2) if args.json else "\n".join(f"{k}: {v}" for k, v in result.items()))


if __name__ == "__main__":
    main()
