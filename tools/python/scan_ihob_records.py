from __future__ import annotations

import pathlib, re, struct, sys, zlib

def expand(kind, data, expected):
    out = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if out is None or len(out) != expected:
        raise ValueError
    return out

def chunks(data, start, end):
    p = start
    while p + 8 <= end:
        tag = data[p:p+4]
        size = struct.unpack_from(">I", data, p+4)[0]
        ps, pe = p+8, p+8+size
        if pe > end:
            return
        yield tag, ps, pe
        p = pe

def walk(data, start, end):
    for tag, ps, pe in chunks(data, start, end):
        if tag == b"IHOB":
            yield data[ps:pe]
        if tag == b"FORM" and ps + 4 <= pe:
            yield from walk(data, ps+4, pe)

def cstring(data, p):
    q = data.find(b"\0", p)
    if q < 0:
        raise ValueError
    return data[p:q].decode("ascii", "replace"), q + 1

root, cfg = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
configured = []
for line in cfg.read_text(errors="replace").splitlines():
    m = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if m:
        configured.append((int(m.group(1)), m.group(2)))

seen, records = set(), []
for _, aname in sorted(configured, reverse=True):
    ap = root / aname
    if not ap.exists():
        continue
    raw = ap.read_bytes()
    if raw[:8] != b"EERT5000":
        continue
    count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    recs = expand(rk, raw[off:off+rs], count*24)
    names = expand(nk, raw[off+rs:off+rs+ns], nu)
    for i in range(count):
        _, usize, poff, kind, csize, noff = struct.unpack_from("<6I", recs, i*24)
        nend = names.find(b"\0", noff)
        name = names[noff:nend].decode("ascii", "replace").lower().replace("\\", "/")
        if not name.endswith(".cdf") or name in seen:
            continue
        seen.add(name)
        try:
            data = expand(kind, raw[poff:poff+csize], usize)
        except Exception:
            continue
        for payload in walk(data, 0, len(data)):
            try:
                appearance, p = cstring(payload, 0)
                hardpoint, p = cstring(payload, p)
                values = struct.unpack_from("<3f", payload, p)
                records.append((name, appearance, hardpoint, *values, len(payload)))
            except Exception:
                records.append((name, "PARSE_ERROR", "", 0, 0, 0, len(payload)))

for record in records:
    print("\t".join(str(v) for v in record))
print(f"records={len(records)} files={len(set(r[0] for r in records))}", file=sys.stderr)
