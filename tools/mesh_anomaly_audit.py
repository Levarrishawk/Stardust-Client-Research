#!/usr/bin/env python3
"""Read-only detailed audit of MSH records not matching the known dominant schemas."""
from __future__ import annotations
import csv, struct, sys, zlib
from pathlib import Path

KNOWN = {("0005", "0001", "0003"), ("0004", "0001", "0003"), ("0003", "0000", "0002")}

def expand(kind: int, data: bytes, expected: int) -> bytes:
    if expected == 0:
        return b""
    if kind == 0:
        out = data[:expected]
    elif kind == 2:
        out = zlib.decompress(data)
    else:
        raise ValueError(f"unsupported compression kind {kind}")
    if len(out) != expected:
        raise ValueError(f"expanded length {len(out)} != {expected}")
    return out

def iter_chunks(data: bytes, start: int, end: int):
    p = start
    while p + 8 <= end:
        tag, size = data[p:p+4], struct.unpack_from(">I", data, p+4)[0]
        stop = p + 8 + size
        if stop > end:
            raise ValueError(f"chunk {tag!r} at {p:#x} ends {stop:#x} beyond {end:#x}")
        yield tag, p + 8, stop
        p = stop
    if p != end:
        raise ValueError(f"{end-p} trailing bytes at {p:#x}")

def inventory(data: bytes):
    versions: dict[bytes, list[str]] = {}
    tags: list[str] = []
    def walk(start: int, end: int, depth: int = 0):
        if depth > 32:
            raise ValueError("IFF nesting exceeds 32")
        for tag, payload, stop in iter_chunks(data, start, end):
            tags.append(tag.decode("ascii", "replace"))
            if tag == b"FORM":
                if payload + 4 > stop:
                    raise ValueError("short FORM")
                form = data[payload:payload+4]
                if form in (b"MESH", b"SPS ", b"VTXA"):
                    version = ""
                    nested = payload + 4
                    if nested + 12 <= stop and data[nested:nested+4] == b"FORM":
                        version = data[nested+8:nested+12].decode("ascii", "replace")
                    versions.setdefault(form, []).append(version)
                walk(payload + 4, stop, depth + 1)
    walk(0, len(data))
    first = tuple((versions.get(k) or [""])[0] for k in (b"MESH", b"SPS ", b"VTXA"))
    return first, ",".join(tags[:40])

def main(root: str, output: str):
    rows = []
    for archive in sorted(Path(root).glob("*.tre")):
        raw = archive.read_bytes()
        if raw[:8] != b"EERT5000":
            continue
        count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
        recs = expand(rk, raw[off:off+rs], count*24)
        names = expand(nk, raw[off+rs:off+rs+ns], nu)
        for i in range(count):
            _, usize, pos, kind, csize, noff = struct.unpack_from("<6I", recs, i*24)
            end = names.find(b"\0", noff)
            name = names[noff:end].decode("utf-8", "replace")
            if not name.lower().endswith(".msh"):
                continue
            row = dict(archive=archive.name, record=i, name=name, usize=usize, csize=csize,
                       compression=kind, mesh="", sps="", vtxa="", status="", detail="")
            try:
                # SearchTree tests the uncompressed-size field. For kind 0,
                # payload length is usize even if csize is zero.
                if usize == 0:
                    row["status"] = "tombstone"
                else:
                    stored_size = usize if kind == 0 else csize
                    data = expand(kind, raw[pos:pos+stored_size], usize)
                    schema, tags = inventory(data)
                    row["mesh"], row["sps"], row["vtxa"] = schema
                    row["detail"] = tags
                    row["status"] = "known" if schema in KNOWN else "alternate"
            except Exception as exc:
                row["status"] = "error"
                row["detail"] = f"{type(exc).__name__}: {exc}"
            if row["status"] != "known":
                rows.append(row)
    with open(output, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0]) if rows else ["status"])
        w.writeheader(); w.writerows(rows)
    from collections import Counter
    print(Counter(r["status"] for r in rows))
    print(f"unresolved={len(rows)} output={output}")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
