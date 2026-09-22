#!/usr/bin/env python3
"""Find older archive definitions shadowed by live PASS 0010 / IMPL 0009 assets."""
from __future__ import annotations

import csv
import re
import sys
from pathlib import Path

from shader_version_payload_inventory import archive_records, inspect_iff


def main(root, config, output):
    root = Path(root)
    priorities = []
    for line in Path(config).read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            priorities.append((int(match.group(1)), match.group(2)))

    by_name = {}
    for priority, archive in sorted(priorities, reverse=True):
        for name, blob in archive_records(root / archive).items():
            by_name.setdefault(name, []).append((priority, archive, blob))

    pair_assets = 0
    with open(output, "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("name", "priority", "archive", "record", "version", "length", "value"))
        for name, definitions in sorted(by_name.items()):
            winner = definitions[0][2]
            if winner is None:
                continue
            winner_rows = inspect_iff(winner)
            if not any((record, version) in (("PASS", "0010"), ("IMPL", "0009")) for record, version, _, _ in winner_rows):
                continue
            older_rows = []
            for priority, archive, blob in definitions[1:]:
                if blob is not None:
                    older_rows.extend((priority, archive, *row) for row in inspect_iff(blob))
            if not any(record in ("PASS", "IMPL") and version in ("0007", "0008", "0009") for _, _, record, version, _, _ in older_rows):
                continue
            pair_assets += 1
            for priority, archive, blob in definitions:
                if blob is None:
                    continue
                for record, version, value, length in inspect_iff(blob):
                    if record in ("PASS", "IMPL"):
                        writer.writerow((name, priority, archive, record, version, length, value))
    print("assets_with_version_history", pair_assets)


if __name__ == "__main__":
    main(*sys.argv[1:4])
