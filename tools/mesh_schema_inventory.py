#!/usr/bin/env python3
"""Read-only inventory of MESH/SPS/VTXA version tuples in installed TRE archives."""
from __future__ import annotations
import struct, zlib, sys
from collections import Counter
from pathlib import Path

def expand(kind, data, expected):
    out = zlib.decompress(data) if kind == 2 else data[:expected]
    if len(out) != expected: raise ValueError("bad expanded length")
    return out

def children(data, start, end):
    p = start
    while p + 8 <= end:
        tag = data[p:p+4]
        size = struct.unpack_from(">I", data, p+4)[0]
        stop = p + 8 + size
        if stop > end: break
        yield tag, p+8, stop
        p = stop

def forms(data):
    found = {}
    def walk(start, end):
        for tag, payload, stop in children(data, start, end):
            if tag == b"FORM" and payload + 4 <= stop:
                form = data[payload:payload+4]
                found.setdefault(form, []).append(data[payload+12:payload+16] if payload+16 <= stop and data[payload+4:payload+8] == b"FORM" else b"")
                walk(payload+4, stop)
    walk(0, len(data))
    return found

def main(root):
    counts = Counter(); total = errors = 0
    for archive in sorted(Path(root).glob("*.tre")):
        raw = archive.read_bytes()
        if raw[:8] != b"EERT5000": continue
        count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
        recs = expand(rk, raw[off:off+rs], count*24)
        names = expand(nk, raw[off+rs:off+rs+ns], nu)
        for i in range(count):
            _, usize, pos, kind, csize, noff = struct.unpack_from("<6I", recs, i*24)
            end = names.find(b"\0", noff)
            if not names[noff:end].lower().endswith(b".msh"): continue
            total += 1
            try:
                data = expand(kind, raw[pos:pos+csize], usize)
                f = forms(data)
                key = tuple(v.decode("ascii", "replace") for v in (f.get(b"MESH",[b""])[0], f.get(b"SPS ",[b""])[0], f.get(b"VTXA",[b""])[0]))
                counts[key] += 1
            except Exception:
                errors += 1
    for key, count in counts.most_common(): print(f"MESH={key[0]} SPS={key[1]} VTXA={key[2]} count={count}")
    print(f"records={total} errors={errors} tuples={len(counts)}")

if __name__ == "__main__": main(sys.argv[1])
