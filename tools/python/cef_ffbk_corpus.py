#!/usr/bin/env python3
"""Audit CLEF FFBK paths, DWORD iterations, and float radius."""
from __future__ import annotations

import json
import math
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def leaves(data: bytes):
    found = []
    def walk(start: int, end: int):
        p = start
        while p + 8 <= end:
            tag = data[p:p + 4]
            size = struct.unpack_from(">I", data, p + 4)[0]
            stop = p + 8 + size
            if stop > end or stop < p + 8:
                raise ValueError("invalid IFF")
            if tag == b"FORM":
                if size < 4:
                    raise ValueError("short FORM")
                walk(p + 12, stop)
            elif tag == b"FFBK":
                payload = data[p + 8:stop]
                nul = payload.find(b"\0")
                if nul < 0 or len(payload) != nul + 1 + 8:
                    raise ValueError("unexpected FFBK payload")
                path = payload[:nul].decode("ascii", "replace").lower().replace("\\", "/")
                iterations, radius = struct.unpack_from("<If", payload, nul + 1)
                found.append((path, iterations, radius))
            p = stop
        if p != end:
            raise ValueError("trailing IFF")
    walk(0, len(data))
    return found


def main(root: Path, cfg: Path, output: Path):
    archives = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            archives.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    values = []
    errors = []
    for _, archive in sorted(archives, reverse=True):
        for name, usize, kind, packed in archive_records(archive):
            if name in seen:
                continue
            seen.add(name)
            if not name.endswith(".cef") or not usize:
                continue
            try:
                values.extend((name, *entry) for entry in leaves(expand(kind, packed, usize)))
            except Exception as exc:
                errors.append((name, str(exc)))
    radius = [entry[3] for entry in values]
    result = {
        "ffbk_records": len(values),
        "files": len(set(entry[0] for entry in values)),
        "unique_ffe_paths": len(set(entry[1] for entry in values)),
        "iterations": sorted(Counter(entry[2] for entry in values).items()),
        "radius_min": min(radius),
        "radius_max": max(radius),
        "radius_zero": sum(value == 0 for value in radius),
        "radius_negative": sum(value < 0 for value in radius),
        "radius_nonfinite": sum(not math.isfinite(value) for value in radius),
        "errors": errors,
    }
    output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main(*(Path(value) for value in sys.argv[1:4]))
