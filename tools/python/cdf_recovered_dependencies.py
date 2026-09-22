#!/usr/bin/env python3
"""Inspect resource links in CDFs recovered by the kind-0 TRE size correction."""
from __future__ import annotations

import csv
import re
import struct
import sys
from collections import Counter
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def read_csv(path):
    with path.open(newline="", encoding="utf-8") as stream:
        return list(csv.DictReader(stream))


def leaves(payload):
    if len(payload) < 24 or payload[:4] != b"FORM" or payload[8:12] != b"CLDF" or payload[12:16] != b"FORM":
        return [("INVALID", b"")]
    if struct.unpack_from(">I", payload, 4)[0] + 8 != len(payload):
        return [("BAD_LENGTH", b"")]
    position = 24
    found = []
    while position < len(payload):
        if position + 8 > len(payload):
            return [("BAD_CHILD", b"")]
        tag = payload[position:position + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", payload, position + 4)[0]
        end = position + 8 + size
        if end > len(payload):
            return [("BAD_CHILD", b"")]
        found.append((tag, payload[position + 8:end]))
        position = end
    return found


def main(root, cfg, original, corrected, output):
    old_names = {row["name"] for row in read_csv(original)}
    new_rows = {row["name"]: row for row in read_csv(corrected) if row["name"] not in old_names}
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    winners = {}
    recovered = {}
    for _, archive in sorted(configured, reverse=True):
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners[name] = bool(usize and packed)
            if name in new_rows:
                recovered[name] = expand(kind, packed, usize)
    rows = []
    for name, payload in sorted(recovered.items()):
        for tag, body in leaves(payload):
            references = sorted({match.group().decode("ascii", "replace").lower().replace("\\", "/")
                                 for match in re.finditer(rb"(?:appearance|sound|clienteffect)[/\\][A-Za-z0-9_./\\-]+\.(?:apt|prt|snd|cef|swh)", body, re.I)})
            for reference in references or [""]:
                status = ("live" if winners.get(reference) else "masked" if reference in winners else "absent") if reference else "none"
                rows.append((name, tag, len(body), reference, status))
    with output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(("cdf", "tag", "leaf_bytes", "reference", "reference_status"))
        writer.writerows(rows)
    print("recovered", len(recovered), "expected", len(new_rows))
    print("tags", Counter(row[1] for row in rows))
    print("reference_status", Counter(row[4] for row in rows))
    print("suffixes", Counter(Path(row[3]).suffix for row in rows if row[3]))
    print("unresolved", [row for row in rows if row[4] in ("absent", "masked")])
    print("none", [row[0] for row in rows if row[4] == "none"])


if __name__ == "__main__":
    main(*(Path(value) for value in sys.argv[1:7]))
