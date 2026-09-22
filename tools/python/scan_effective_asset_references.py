#!/usr/bin/env python3
"""Resolve configured TRE overlays and find byte-string references in live winners."""
from __future__ import annotations
import argparse, csv, re, struct, zlib
from pathlib import Path

def expand(kind: int, data: bytes, expected: int) -> bytes:
    if kind == 0:
        out = data[:expected]
    elif kind == 2:
        out = zlib.decompress(data)
    else:
        raise ValueError(f"unsupported compression {kind}")
    if len(out) != expected:
        raise ValueError(f"size mismatch {len(out)} != {expected}")
    return out

def records(path: Path):
    raw = path.read_bytes()
    count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    recs = expand(rk, raw[off:off+rs], count * 24)
    names = expand(nk, raw[off+rs:off+rs+ns], nu)
    for i in range(count):
        _, usize, pos, kind, csize, noff = struct.unpack_from("<6I", recs, i * 24)
        end = names.find(b"\0", noff)
        name = names[noff:end].decode("utf-8", "replace").lower().replace("\\", "/")
        data = None if csize == 0 else expand(kind, raw[pos:pos+csize], usize)
        yield name, data

def main(root: Path, cfg: Path, needles_csv: Path, output: Path):
    priorities = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            priorities.append((int(match.group(1)), match.group(2)))
    needles = []
    with needles_csv.open(newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            if row["status"] == "live" and re.search(r"PASS/0010|IMPL/000[89]", row["forms"]):
                needles.append(row["name"].lower())
    winners = {}
    for priority, archive in sorted(priorities, reverse=True):
        path = root / archive
        if not path.exists():
            continue
        for name, data in records(path):
            if name not in winners:
                winners[name] = (priority, archive, data)
    rows = []
    encoded = [(n, n.encode("ascii")) for n in needles]
    for owner, (priority, archive, data) in winners.items():
        if data is None:
            continue
        low = data.lower()
        for target, needle in encoded:
            count = low.count(needle)
            if count:
                rows.append((target, owner, priority, archive, count))
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("target", "referrer", "priority", "archive", "count"))
        writer.writerows(sorted(rows))
    print("targets", len(needles), "effective_winners", len(winners), "reference_rows", len(rows))
    for target in needles:
        refs = [r for r in rows if r[0] == target and r[1] != target]
        print(target, len(refs), sum(r[4] for r in refs))

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("cfg", type=Path)
    parser.add_argument("needles_csv", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    main(args.root, args.cfg, args.needles_csv, args.output)
