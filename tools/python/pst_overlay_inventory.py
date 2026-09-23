#!/usr/bin/env python3
"""Inventory effective playback scripts in the configured TRE overlay."""
from __future__ import annotations

import argparse
import re
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def walk(blob: bytes, start: int, end: int, forms: Counter, actions: Counter,
         leaf_shapes: Counter, pska_v3_info: Counter, context: tuple[str, ...] = ()) -> None:
    position = start
    while position + 8 <= end:
        tag = blob[position:position + 4]
        size = int.from_bytes(blob[position + 4:position + 8], "big")
        stop = position + 8 + size
        if stop > end or stop < position + 8:
            raise ValueError(f"bad IFF chunk at {position:#x}")
        if tag == b"FORM":
            kind = blob[position + 8:position + 12].decode("ascii", "replace")
            parent = context[-1] if context else ""
            forms[(parent, kind)] += 1
            if parent == "THRD":
                actions[kind] += 1
            walk(blob, position + 12, stop, forms, actions, leaf_shapes, pska_v3_info, context + (kind,))
        elif len(context) >= 2 and context[-2] == "PSKA":
            leaf_shapes[(context[-1], tag.decode("ascii", "replace"), size)] += 1
            if context[-1] == "0003" and tag == b"INFO" and size == 11:
                pska_v3_info[blob[position + 8:stop].hex()] += 1
        position = stop
    if position != end:
        raise ValueError(f"trailing {end - position} IFF bytes")


def main(root: Path, config: Path) -> None:
    configured = []
    for line in config.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    versions = Counter()
    forms = Counter()
    actions = Counter()
    pska_leaf_shapes = Counter()
    pska_v3_info = Counter()
    unusual = []
    invalid = []
    for priority, archive in sorted(configured, reverse=True):
        for name, usize, kind, packed in archive_records(archive):
            if name in seen or not name.endswith(".pst"):
                continue
            seen.add(name)
            if not usize:
                unusual.append((name, archive.name, "masked"))
                continue
            try:
                payload = expand(kind, packed, usize)
                version = payload[20:24].decode("ascii", "replace") if payload[12:16] == b"FORM" else "not_version"
                versions[version] += 1
                file_forms = Counter()
                file_actions = Counter()
                walk(payload, 0, len(payload), file_forms, file_actions, pska_leaf_shapes, pska_v3_info)
                forms.update(file_forms)
                actions.update(file_actions)
                if file_forms[("PSKA", "0003")]:
                    unusual.append((name, archive.name, f"PSKA/0003 x{file_forms[('PSKA', '0003')]}"))
                if file_actions["FIPL"] or file_actions["PPRL"]:
                    unusual.append((name, archive.name, "FIPL/PPRL candidate"))
            except ValueError as exc:
                invalid.append((name, archive.name, str(exc)))
    print("configured archives:", len(configured))
    print("effective .pst entries:", len(seen))
    print("versions:", dict(sorted(versions.items())))
    print("top-level forms:", {name: count for (parent, name), count in forms.items() if not parent})
    print("threads:", forms[("THDS", "THRD")])
    print("action forms:", dict(sorted(actions.items())))
    print("PSKA versions:", {kind: count for (parent, kind), count in sorted(forms.items()) if parent == "PSKA"})
    print("PSKA leaf shapes:", dict(sorted(pska_leaf_shapes.items())))
    print("PSKA/0003 INFO bytes:", dict(sorted(pska_v3_info.items())))
    print("unusual:", unusual)
    print("invalid:", invalid)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("config", type=Path)
    args = parser.parse_args()
    main(args.root, args.config)
