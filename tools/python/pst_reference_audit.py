#!/usr/bin/env python3
"""Find literal references to effective playback scripts in configured TRE payloads."""
from __future__ import annotations

import argparse
import re
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def configured_archives(root: Path, config: Path) -> list[tuple[int, Path]]:
    result = []
    for line in config.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            result.append((int(match.group(1)), root / match.group(2)))
    return sorted(result, reverse=True)


def main(root: Path, config: Path) -> None:
    archives = configured_archives(root, config)
    winners = {}
    for _, archive in archives:
        for name, usize, _, _ in archive_records(archive):
            winners.setdefault(name, usize != 0)
    names = sorted(name for name, live in winners.items() if live and name.endswith(".pst"))
    needles = [(name, name.encode("ascii"), Path(name).stem.encode("ascii")) for name in names]
    hits = []
    errors = []
    seen = set()
    for _, archive in archives:
        for source, usize, kind, packed in archive_records(archive):
            if source in seen:
                continue
            seen.add(source)
            if not usize or source.endswith(".pst"):
                continue
            try:
                payload = expand(kind, packed, usize).lower()
            except ValueError as exc:
                errors.append((archive.name, source, str(exc)))
                continue
            for target, full, stem in needles:
                if full in payload:
                    hits.append((source, target, "full"))
                elif stem in payload:
                    hits.append((source, target, "basename"))
    print("configured archives:", len(archives), "effective PST:", len(names), "searched payloads:", len(seen))
    for source, target, kind in hits:
        print(f"{kind}\t{source}\t{target}")
    full_targets = {target for _, target, kind in hits if kind == "full"}
    basename_targets = {target for _, target, kind in hits if kind == "basename"}
    print("full-path target count:", len(full_targets))
    print("basename-only target count:", len(basename_targets - full_targets))
    print("no literal target:", sorted(set(names) - full_targets - basename_targets))
    print("hits:", len(hits), "errors:", len(errors))
    for row in errors[:10]:
        print("ERROR", *row, sep="\t")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("config", type=Path)
    args = parser.parse_args()
    main(args.root, args.config)
