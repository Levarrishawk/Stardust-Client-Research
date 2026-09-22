#!/usr/bin/env python3
"""Read-only SWG TREE/0005 index and representative IFF probe."""
from __future__ import annotations
import argparse, re, struct, zlib
from pathlib import Path

def expand(kind, packed, expected):
    data = zlib.decompress(packed) if kind == 2 else packed[:expected]
    if len(data) != expected:
        raise ValueError(f"expanded {len(data)} bytes, expected {expected}")
    return data

def records(path):
    raw = path.read_bytes()
    # C++ multi-character constants are stored little-endian in the TRE header.
    if raw[:8] != b"EERT5000":
        raise ValueError(f"unsupported archive {raw[:8]!r}")
    count, data_offset, rk, rsize, nk, nsize, nusize = struct.unpack_from("<7I", raw, 8)
    pos = data_offset
    record_bytes = expand(rk, raw[pos:pos+rsize], count * 24)
    pos += rsize
    name_bytes = expand(nk, raw[pos:pos+nsize], nusize)
    for i in range(count):
        crc, usize, offset, kind, csize, noff = struct.unpack_from("<6I", record_bytes, i*24)
        end = name_bytes.find(b"\0", noff)
        name = name_bytes[noff:end].decode("ascii", "replace").replace("\\", "/")
        yield name, crc, usize, offset, kind, csize, raw

def extract(record):
    name, _, usize, offset, kind, csize, raw = record
    stored_size = usize if kind == 0 else csize
    return name, expand(kind, raw[offset:offset+stored_size], usize)

def iff_tree(data, limit=160):
    out = []
    def walk(start, end, depth):
        p = start
        while p + 8 <= end and len(out) < limit:
            tag = data[p:p+4].decode("ascii", "replace")
            size = struct.unpack_from(">I", data, p+4)[0]
            payload, stop = p+8, min(p+8+size, end)
            if tag == "FORM" and payload + 4 <= stop:
                form = data[payload:payload+4].decode("ascii", "replace")
                out.append(f"{'  '*depth}FORM {form} size={size}")
                walk(payload+4, stop, depth+1)
            else:
                out.append(f"{'  '*depth}{tag} size={size}")
            # SWG IFF sizes advance directly; odd-sized forms are not padded here.
            p += 8 + size
    walk(0, len(data), 0)
    return out

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("root", type=Path)
    ap.add_argument("--suffix", default=".pob")
    ap.add_argument("--contains", default="")
    ap.add_argument("--hex", action="store_true", help="print a compact payload hex dump")
    ap.add_argument("--version", default="", help="select payloads whose leading IFF header contains this version")
    ap.add_argument("--largest", action="store_true", help="select the largest matching record")
    args = ap.parse_args()
    matches = []
    for archive in sorted(args.root.glob("*.tre")):
        for rec in records(archive):
            low = rec[0].lower()
            if low.endswith(args.suffix.lower()) and args.contains.lower() in low:
                if args.version:
                    try:
                        _, candidate = extract(rec)
                    except Exception:
                        continue
                    if args.version.encode("ascii") not in candidate[8:32]:
                        continue
                matches.append((archive, rec))
    print(f"matches={len(matches)}")
    if not matches:
        return
    archive, rec = max(matches, key=lambda item: item[1][2]) if args.largest else (matches[0] if args.version else matches[-1])
    name, data = extract(rec)
    print(f"archive={archive.name}\nname={name}\nbytes={len(data)}")
    if args.hex:
        for offset in range(0, min(len(data), 1024), 16):
            row = data[offset:offset+16]
            print(f"{offset:04x}  {' '.join(f'{value:02x}' for value in row):47}  {''.join(chr(value) if 32 <= value < 127 else '.' for value in row)}")
    print("\n".join(iff_tree(data)))
    print("REFERENCES")
    strings = sorted(set(m.group().decode("ascii", "replace") for m in re.finditer(rb"[A-Za-z0-9_./\\-]{5,}", data)))
    print("\n".join(s for s in strings if "." in s)[:20000])

if __name__ == "__main__":
    main()
