#!/usr/bin/env python3
"""Inventory constant ObjectController type construction in Stardust-3.

This is deliberately a static lower bound. It reports direct constructor calls
whose priority and type are immediate hexadecimal constants; variable-type
wrappers are listed separately instead of guessed.
"""

from __future__ import annotations

import argparse
import pathlib
import re

from object_controller_catalog import registrations


DIRECT = re.compile(
    r"(?P<kind>StandaloneObjectControllerMessage|ObjectControllerMessage)\s*\("
    r"(?P<prefix>[^\n;{}]{0,300}?),\s*"
    r"(?P<priority>0x[0-9A-Fa-f]+)\s*,\s*"
    r"(?P<type>0x[0-9A-Fa-f]+)\b"
)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("image", type=pathlib.Path)
    ap.add_argument("server_root", type=pathlib.Path)
    args = ap.parse_args()

    source_root = args.server_root / "MMOCoreORB" / "src" / "server"
    rows: list[tuple[int, int, str, int, str]] = []
    for path in source_root.rglob("*"):
        if path.suffix not in {".h", ".cpp"}:
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        starts = [0]
        starts.extend(i + 1 for i, ch in enumerate(text) if ch == "\n")
        for match in DIRECT.finditer(text):
            priority = int(match.group("priority"), 16)
            type_id = int(match.group("type"), 16)
            line = text.count("\n", 0, match.start()) + 1
            rel = path.relative_to(args.server_root).as_posix()
            rows.append((type_id, priority, match.group("kind"), line, rel))

    client_rows = [r for r in registrations(args.image) if r[1] is not None]
    client = {r[1]: r for r in client_rows}
    types = sorted({r[0] for r in rows})
    overlap = [t for t in types if t in client]
    missing = [t for t in types if t not in client]

    print(f"direct_sites={len(rows)} direct_types={len(types)} client_overlap={len(overlap)} client_missing={len(missing)}")
    print("type,priority,kind,file,line,client_registration,client_outbound_factory,client_inbound_factory")
    for type_id, priority, kind, line, rel in sorted(rows):
        reg = client.get(type_id)
        fields = (
            f"0x{reg[0]:08X},0x{reg[2]:08X},0x{reg[3]:08X}" if reg else ",,"
        )
        print(f"0x{type_id:X},0x{priority:X},{kind},{rel},{line},{fields}")
    print("missing_types=" + ",".join(f"0x{x:X}" for x in missing))


if __name__ == "__main__":
    main()
