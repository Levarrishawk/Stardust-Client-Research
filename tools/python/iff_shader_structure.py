#!/usr/bin/env python3
"""Extract one shader asset and print its IFF structure plus short DATA hex."""
from __future__ import annotations
import struct, sys, zlib
from pathlib import Path

def expand(kind, data, expected):
    out = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if out is None or len(out) != expected:
        raise ValueError((kind, len(out) if out else None, expected))
    return out

def fetch(archive, name):
    raw = Path(archive).read_bytes()
    count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    recs = expand(rk, raw[off:off+rs], count*24)
    names = expand(nk, raw[off+rs:off+rs+ns], nu)
    want = name.lower().replace("\\", "/")
    for i in range(count):
        _, usize, pos, kind, csize, noff = struct.unpack_from("<6I", recs, i*24)
        end = names.find(b"\0", noff)
        got = names[noff:end].decode(errors="replace").lower().replace("\\", "/")
        if got == want:
            if not usize:
                raise ValueError("tombstone")
            stored_size = usize if kind == 0 else csize
            return expand(kind, raw[pos:pos+stored_size], usize)
    raise KeyError(name)

def tree(data):
    def walk(start, end, depth):
        p = start
        while p + 8 <= end:
            tag = data[p:p+4]
            size = struct.unpack_from(">I", data, p+4)[0]
            stop = p + 8 + size
            if stop > end:
                raise ValueError(f"bad {tag!r} at {p:#x}")
            text = tag.decode("ascii", "replace")
            if tag == b"FORM":
                kind = data[p+8:p+12].decode("ascii", "replace")
                print("  "*depth + f"FORM {kind} size={size}")
                walk(p+12, stop, depth+1)
            else:
                payload = data[p+8:stop]
                extra = " hex=" + payload[:96].hex(" ") if tag in (b"DATA", b"INFO", b"CAPS", b"SCAP", b"OPTS", b"OPTN") else ""
                print("  "*depth + f"{text} size={size}{extra}")
            p = stop
        if p != end:
            raise ValueError(f"trailing {end-p}")
    walk(0, len(data), 0)

if __name__ == "__main__":
    blob = fetch(sys.argv[1], sys.argv[2])
    Path(sys.argv[3]).write_bytes(blob)
    tree(blob)
