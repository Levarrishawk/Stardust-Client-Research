#!/usr/bin/env python3
"""Read-only inventory of live SHT/EFT IFF forms and versions in installed TRE files."""
from __future__ import annotations
import csv, struct, sys, zlib
from collections import Counter
from pathlib import Path

TARGET_FORMS = {b"SSHT", b"EFCT", b"IMPL", b"PASS"}

def expand(kind, data, expected):
    if kind == 0: out = data[:expected]
    elif kind == 2: out = zlib.decompress(data)
    else: raise ValueError(f"compression {kind}")
    if len(out) != expected: raise ValueError(f"length {len(out)} != {expected}")
    return out

def chunks(data, start, end):
    p = start
    while p + 8 <= end:
        tag, size = data[p:p+4], struct.unpack_from(">I", data, p+4)[0]
        stop = p + 8 + size
        if stop > end: raise ValueError(f"bad {tag!r} at {p:#x}")
        yield tag, p + 8, stop
        p = stop
    if p != end: raise ValueError(f"trailing {end-p}")

def scan_iff(data):
    forms, chunks_seen = [], Counter()
    def walk(start, end, depth=0):
        if depth > 48: raise ValueError("nesting")
        for tag, payload, stop in chunks(data, start, end):
            chunks_seen[tag.decode("ascii", "replace")] += 1
            if tag == b"FORM":
                if payload + 4 > stop: raise ValueError("short FORM")
                kind = data[payload:payload+4]
                version = ""
                inner = payload + 4
                if inner + 12 <= stop and data[inner:inner+4] == b"FORM":
                    version = data[inner+8:inner+12].decode("ascii", "replace")
                if kind in TARGET_FORMS: forms.append((kind.decode("ascii"), version))
                walk(inner, stop, depth+1)
    walk(0, len(data))
    return forms, chunks_seen

def main(root, output):
    rows=[]; form_counts=Counter(); ext_counts=Counter(); errors=Counter(); tags=Counter()
    for archive in sorted(Path(root).glob("*.tre")):
        raw=archive.read_bytes()
        if raw[:8] != b"EERT5000": continue
        count, off, rk, rs, nk, ns, nu=struct.unpack_from("<7I", raw, 8)
        recs=expand(rk, raw[off:off+rs], count*24); names=expand(nk, raw[off+rs:off+rs+ns], nu)
        for i in range(count):
            _, usize, pos, kind, csize, noff=struct.unpack_from("<6I", recs, i*24)
            end=names.find(b"\0", noff); name=names[noff:end].decode("utf-8", "replace")
            ext=Path(name).suffix.lower()
            if ext not in (".sht", ".eft"): continue
            if csize == 0: ext_counts[(ext,"tombstone")]+=1; continue
            try:
                data=expand(kind, raw[pos:pos+csize], usize); forms, seen=scan_iff(data)
                ext_counts[(ext,"live")]+=1; tags.update(seen)
                for f,v in forms: form_counts[(ext,f,v)]+=1
                rows.append(dict(archive=archive.name,name=name,ext=ext,forms=";".join(f"{f}/{v}" for f,v in forms),status="live"))
            except Exception as exc:
                errors[(ext,type(exc).__name__,str(exc))]+=1
                rows.append(dict(archive=archive.name,name=name,ext=ext,forms="",status=f"error:{exc}"))
    with open(output,"w",newline="",encoding="utf-8") as f:
        w=csv.DictWriter(f,fieldnames=["archive","name","ext","forms","status"]); w.writeheader(); w.writerows(rows)
    print("EXTENSIONS")
    for k,n in sorted(ext_counts.items()): print(k,n)
    print("FORMS")
    for k,n in sorted(form_counts.items()): print(k,n)
    print("TOP TAGS")
    for k,n in tags.most_common(50): print(k,n)
    print("ERRORS",sum(errors.values()))
    for k,n in errors.most_common(20): print(k,n)

if __name__ == "__main__": main(sys.argv[1],sys.argv[2])
