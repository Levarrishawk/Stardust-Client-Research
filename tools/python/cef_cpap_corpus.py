#!/usr/bin/env python3
"""Audit effective CLEF CPAP payloads across the configured TRE overlay."""
from __future__ import annotations

import json
import math
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def cpap_leaves(data: bytes):
    if data[:4] != b"FORM" or data[8:12] != b"CLEF":
        raise ValueError("not CLEF")
    version = data[20:24].decode("ascii")
    results = []

    def walk(start: int, end: int):
        p = start
        while p + 8 <= end:
            tag = data[p:p + 4]
            size = struct.unpack_from(">I", data, p + 4)[0]
            stop = p + 8 + size
            if stop > end or stop < p + 8:
                raise ValueError("invalid IFF bounds")
            if tag == b"FORM":
                if size < 4:
                    raise ValueError("short FORM")
                walk(p + 12, stop)
            elif tag == b"CPAP":
                payload = data[p + 8:stop]
                nul = payload.find(b"\0")
                if nul < 0:
                    raise ValueError("CPAP without path terminator")
                tail = payload[nul + 1:]
                expected = {"0001": 4, "0002": 5, "0003": 21}[version]
                if len(tail) != expected:
                    raise ValueError(f"CPAP tail {len(tail)} != {expected}")
                scalar = struct.unpack_from("<f", tail)[0]
                flag = tail[4] if version != "0001" else None
                ranges = struct.unpack_from("<4f", tail, 5) if version == "0003" else None
                results.append((payload[:nul].decode("ascii", "replace"), scalar, flag, ranges))
            p = stop
        if p != end:
            raise ValueError("trailing IFF bytes")

    walk(0, len(data))
    return version, results


def main(root: Path, cfg: Path, output: Path):
    archives = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            archives.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    rows = []
    errors = []
    for _, archive in sorted(archives, reverse=True):
        for name, usize, kind, packed in archive_records(archive):
            if name in seen:
                continue
            seen.add(name)
            if not name.endswith(".cef") or not usize:
                continue
            try:
                version, leaves = cpap_leaves(expand(kind, packed, usize))
                rows.extend((version, *leaf) for leaf in leaves)
            except Exception as exc:
                errors.append((name, str(exc)))
    scalars = [row[2] for row in rows]
    flags = Counter(row[3] for row in rows)
    floats = [[row[4][i] for row in rows if row[4] is not None] for i in range(4)]
    result = {
        "records": len(rows),
        "versions": dict(sorted(Counter(row[0] for row in rows).items())),
        "distinct_paths": len(set(row[1].lower().replace("\\", "/") for row in rows)),
        "suffixes": dict(sorted(Counter(Path(row[1].lower()).suffix for row in rows).items())),
        "paths_with_trailing_space": sum(row[1].endswith(" ") for row in rows),
        "base_scalar": {"min": min(scalars), "max": max(scalars), "nonfinite": sum(not math.isfinite(x) for x in scalars), "top_values": Counter(scalars).most_common(15)},
        "flags": dict(sorted((str(k), v) for k, v in flags.items())),
        "v3_floats": [{"min": min(v), "max": max(v), "nonfinite": sum(not math.isfinite(x) for x in v), "top_values": Counter(v).most_common(10)} for v in floats],
        "v3_first_pair_reversed": sum(row[4][0] > row[4][1] for row in rows if row[4] is not None),
        "v3_second_pair_reversed": sum(row[4][2] > row[4][3] for row in rows if row[4] is not None),
        "errors": errors,
    }
    output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main(*(Path(value) for value in sys.argv[1:4]))
