#!/usr/bin/env python3
"""Count live TRE records whose kind-0 compressed-size field is zero."""
from __future__ import annotations

import re
import struct
import sys
from collections import Counter, defaultdict
from pathlib import Path

from scan_tre_sound_references import expand


def archive_records(path):
    raw = path.read_bytes()
    if raw[:8] != b"EERT5000":
        raise ValueError(f"not EERT5000: {path}")
    count, offset, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    records = expand(rk, raw[offset:offset + rs], count * 24)
    names = expand(nk, raw[offset + rs:offset + rs + ns], nu)
    for index in range(count):
        _, usize, _, kind, csize, noff = struct.unpack_from("<6I", records, index * 24)
        end = names.find(b"\0", noff)
        name = names[noff:end].decode("ascii", "replace").lower().replace("\\", "/")
        yield name, usize, kind, csize


def main(root, cfg):
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    counts = Counter()
    examples = defaultdict(list)
    for _, archive in sorted(configured, reverse=True):
        for name, usize, kind, csize in archive_records(archive):
            if name in seen:
                continue
            seen.add(name)
            if kind == 0 and csize == 0 and usize > 0:
                suffix = Path(name).suffix or "<none>"
                counts[suffix] += 1
                if len(examples[suffix]) < 3:
                    examples[suffix].append((archive.name, name, usize))
    print("effective_winners", len(seen), "affected", sum(counts.values()))
    for suffix, count in counts.most_common():
        print(suffix, count, examples[suffix])


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]))
