#!/usr/bin/env python3
"""Classify every effective CEF IFF against the observed CLEF loader versions."""
from __future__ import annotations

import csv
import re
import sys
from collections import Counter
from pathlib import Path

from cef_zero_csize_corpus import iff_shape, leaf_shape, records
from scan_tre_sound_references import archive_records, expand


def main(root, cfg, output):
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    seen = set()
    winners = {}
    rows = []
    for priority, archive in sorted(configured, reverse=True):
        for path, usize, kind, packed in archive_records(archive):
            winners.setdefault(path, bool(usize and packed))
        for name, usize, kind, csize, packed in records(archive):
            if name in seen:
                continue
            seen.add(name)
            if usize == 0:
                rows.append((name, archive.name, priority, 0, "mask", "", "", ""))
                continue
            payload = expand(kind, packed, usize)
            form, versions, refs = iff_shape(payload)
            leaf_tags, leaf_sizes = leaf_shape(payload)
            rows.append((name, archive.name, priority, usize, form, versions, leaf_tags, refs))
    with output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(("name", "archive", "priority", "bytes", "form", "versions", "leaf_tags", "references"))
        writer.writerows(rows)
    print("effective", len(rows))
    print("forms", Counter(row[4] for row in rows))
    print("versions", Counter(row[5] for row in rows))
    print("leaf_tags", Counter(tag for row in rows for tag in row[6].split(";") if tag))
    references = [(row[0], ref) for row in rows for ref in row[7].split(";") if ref]
    print("references", len(references), "suffixes", Counter(Path(ref).suffix for _, ref in references))
    print("availability", Counter("live" if winners.get(ref) else "masked" if ref in winners else "absent" for _, ref in references))
    print("unresolved", [(name, ref) for name, ref in references if not winners.get(ref)])
    print("invalid", [row[0] for row in rows if row[4] not in ("CLEF", "mask") or row[6] in ("INVALID", "BAD_SIZE", "TRUNCATED")])


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
