#!/usr/bin/env python3
"""Cross-reference Stardust packet classes against private client research."""
from __future__ import annotations

import csv
import re
import sys
from collections import Counter
from pathlib import Path


CLASS_RE = re.compile(r"\bclass\s+(\w+)\s*:\s*public\s+([\w:<>]+)")
OPCODE_RE = re.compile(r"\binsertInt\s*\(\s*(0x[0-9a-fA-F]+|\d+)")


def main(repo_value: str, docs_value: str, output_value: str) -> None:
    repo, docs, output = Path(repo_value), Path(docs_value), Path(output_value)
    packet_root = repo / "MMOCoreORB" / "src" / "server" / "zone" / "packets"
    source_files = [p for p in (repo / "MMOCoreORB" / "src").rglob("*") if p.suffix.lower() in {".h", ".cpp", ".idl"}]
    source_identifiers = Counter()
    for source in source_files:
        source_identifiers.update(re.findall(r"\b[A-Za-z_]\w*\b", source.read_text(errors="replace")))
    doc_files = [p for p in docs.glob("*.md") if p.is_file()]
    doc_text = "\n".join(p.read_text(errors="replace") for p in doc_files)
    rows = []
    for path in sorted(packet_root.rglob("*.h")):
        text = path.read_text(errors="replace")
        opcodes = OPCODE_RE.findall(text)
        for match_index, match in enumerate(CLASS_RE.finditer(text)):
            name, base = match.groups()
            # Callback classes describe client->server parsing. Other packet
            # classes are server->client unless later manually classified.
            direction = "C2S" if name.endswith("Callback") or "Callback" in base else "S2C"
            references = source_identifiers[name]
            reachable = references > 1
            name_hit = re.search(rf"\b{re.escape(name)}\b", doc_text, re.I) is not None
            opcode = opcodes[min(match_index, len(opcodes) - 1)] if opcodes else ""
            opcode_hit = bool(opcode and re.search(rf"(?i)(?:0x0*{int(opcode, 0):x}\b|\b{int(opcode, 0):08x}\b)", doc_text))
            status = "documented" if name_hit or opcode_hit else ("reachable_gap" if reachable else "definition_only_gap")
            rows.append((str(path.relative_to(repo)), name, base, direction, opcode, references, int(reachable), int(name_hit), int(opcode_hit), status))
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("header", "class", "base", "direction", "declared_opcode", "source_references", "reachable", "documented_name", "documented_opcode", "status"))
        writer.writerows(rows)
    for status in ("documented", "reachable_gap", "definition_only_gap"):
        subset = [row for row in rows if row[-1] == status]
        print(status, len(subset), sep="\t")
        if status == "reachable_gap":
            for row in sorted(subset, key=lambda value: (-value[5], value[1]))[:80]:
                print("GAP", row[3], row[1], row[4], row[5], row[0], sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:4])
