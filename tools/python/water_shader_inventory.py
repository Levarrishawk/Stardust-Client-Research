#!/usr/bin/env python3
"""Inventory PTAT global-water shader IDs and matching SHT dependencies."""
from __future__ import annotations
import argparse, struct, sys
from collections import Counter, defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand


def chunks(data: bytes, start: int, end: int):
    p = start
    while p + 8 <= end:
        tag = data[p:p+4]
        size = struct.unpack_from(">I", data, p + 4)[0]
        stop = p + 8 + size
        if stop > end:
            return
        yield tag, p + 8, stop
        p = stop


def find_chunks(data: bytes, wanted: bytes):
    found = []
    def walk(start: int, end: int):
        for tag, payload, stop in chunks(data, start, end):
            if tag == wanted:
                found.append(data[payload:stop])
            if tag == b"FORM" and payload + 4 <= stop:
                walk(payload + 4, stop)
    walk(0, len(data))
    return found


def find_forms(data: bytes, wanted: set[bytes]):
    found = []
    def walk(start: int, end: int):
        for tag, payload, stop in chunks(data, start, end):
            if tag == b"FORM" and payload + 4 <= stop:
                kind = data[payload:payload+4]
                if kind in wanted:
                    found.append((kind, data[payload+4:stop]))
                walk(payload + 4, stop)
    walk(0, len(data))
    return found


def cstr(data: bytes, pos: int):
    end = data.index(0, pos)
    return data[pos:end].decode("ascii", "replace"), end + 1


ap = argparse.ArgumentParser()
ap.add_argument("root", type=Path)
args = ap.parse_args()

terrain = []
local_water = []
shaders = defaultdict(list)
for archive in sorted(args.root.glob("*.tre")):
    for name, usize, kind, packed in archive_records(archive):
        if not (name.endswith(".trn") or name.endswith(".sht")):
            continue
        if not packed:
            continue
        try:
            data = expand(kind, packed, usize)
        except Exception:
            continue
        if name.endswith(".trn"):
            data_chunks = find_chunks(data, b"DATA")
            if not data_chunks:
                continue
            payload = data_chunks[0]
            try:
                _, pos = cstr(payload, 0)
                size, chunk = struct.unpack_from("<ff", payload, pos); pos += 8
                tiles, enabled = struct.unpack_from("<II", payload, pos); pos += 8
                height, texscale = struct.unpack_from("<ff", payload, pos); pos += 8
                shader, pos = cstr(payload, pos)
                terrain.append((archive.name, name, enabled, height, texscale, shader))
            except Exception:
                pass
            for boundary_kind, body in find_forms(data, {b"BPOL", b"BREC"}):
                payloads = find_chunks(body, b"DATA")
                if not payloads:
                    continue
                p = payloads[-1]
                try:
                    if boundary_kind == b"BPOL":
                        count = struct.unpack_from("<I", p, 0)[0]
                        pos = 4 + count * 8
                        feather, feather_amount, enabled, height, texscale = struct.unpack_from("<IfIff", p, pos)
                        pos += 20
                    else:
                        pos = 16
                        feather, feather_amount, enabled, reserved, height, texscale = struct.unpack_from("<IfIIff", p, pos)
                        pos += 24
                    shader, _ = cstr(p, pos)
                    if enabled:
                        local_water.append((archive.name, name, boundary_kind.decode(), height, texscale, shader))
                except Exception:
                    pass
        else:
            # Collect effect NAME and every texture-map DATA tag paired with its local NAME.
            if not name.startswith("shader/"):
                continue
            names = [p.split(b"\0", 1)[0].decode("ascii", "replace") for p in find_chunks(data, b"NAME")]
            data_tags = []
            for p in find_chunks(data, b"DATA"):
                if len(p) >= 4:
                    raw = p[:4]
                    if all(32 <= b < 127 for b in raw):
                        data_tags.append(raw[::-1].decode("ascii", "replace"))
            shaders[name].append((archive.name, names, data_tags))

print("PTAT_GLOBAL_WATER")
for row in terrain:
    print("\t".join(map(str, row)))
print("UNIQUE_SHADER_IDS")
print("LOCAL_WATER_BOUNDARIES")
for row in local_water:
    print("\t".join(map(str, row)))
counts = Counter(row[5] for row in terrain if row[2])
counts.update(row[5] for row in local_water)
for shader, count in sorted(counts.items()):
    logical = shader.replace("\\", "/")
    path = logical if logical.startswith("shader/") else f"shader/{logical}"
    if not path.endswith(".sht"):
        path += ".sht"
    print(f"{shader}\t{count}\t{path}")
    for archive, names, tags in shaders.get(path.lower(), []):
        print(f"  {archive}\ttags={','.join(tags)}\tnames={'|'.join(names)}")
