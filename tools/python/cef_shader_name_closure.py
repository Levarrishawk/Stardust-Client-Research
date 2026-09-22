#!/usr/bin/env python3
"""Follow reached PTEX shaders to IFF NAME resource paths (local evidence only)."""
from __future__ import annotations

import json
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def norm(value: str) -> str:
    return value.replace("\\", "/").lower()


def names(data: bytes):
    found = []
    def walk(start: int, end: int):
        pos = start
        while pos + 8 <= end:
            tag = data[pos:pos + 4]
            size = struct.unpack_from(">I", data, pos + 4)[0]
            stop = pos + 8 + size
            if stop > end or stop < pos + 8:
                raise ValueError("invalid IFF boundary")
            if tag == b"FORM":
                if size < 4:
                    raise ValueError("short FORM")
                walk(pos + 12, stop)
            elif tag == b"NAME":
                value = data[pos + 8:stop].split(b"\0", 1)[0]
                if value:
                    found.append(norm(value.decode("ascii", "replace")))
            pos = stop
        if pos != end:
            raise ValueError("trailing IFF bytes")
    walk(0, len(data))
    return found


def main(root: Path, cfg: Path, closure: Path, output: Path):
    targets = set(json.loads(closure.read_text(encoding="utf-8"))["ptex_shader_names"])
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    effective = {}
    payloads = {}
    for _, archive in sorted(configured, reverse=True):
        for name, usize, kind, packed in archive_records(archive):
            name = norm(name)
            if name in effective:
                continue
            effective[name] = bool(usize and packed)
            if name in targets and usize:
                payloads[name] = expand(kind, packed, usize)
    parsed = {}
    errors = {}
    for shader in sorted(targets):
        try:
            parsed[shader] = names(payloads[shader])
        except Exception as exc:
            errors[shader] = str(exc)
    refs = [(shader, target) for shader, resources in parsed.items() for target in resources if Path(target).suffix]
    missing = [(shader, target) for shader, target in refs if not effective.get(target)]
    result = {
        "target_shaders": len(targets),
        "parsed_shaders": len(parsed),
        "parse_errors": errors,
        "name_resource_occurrences": len(refs),
        "name_resource_extensions": dict(Counter(Path(t).suffix for _, t in refs)),
        "unique_name_resources": len(set(t for _, t in refs)),
        "unique_name_resources_by_extension": dict(Counter(Path(t).suffix for t in set(t for _, t in refs))),
        "missing_name_resources": missing,
    }
    output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main(*(Path(value) for value in sys.argv[1:5]))
