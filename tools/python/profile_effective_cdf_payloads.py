from __future__ import annotations

import collections
import pathlib
import re
import struct
import sys
import zlib


TAGS = {"CEFT", "CSND", "CSSI", "WCSI", "MESH", "ASND", "CHLD", "CHL2", "APPR", "PSOR", "HOBJ", "HARD", "LOBJ", "EVNT"}


def expand(kind, data, expected):
    result = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if result is None or len(result) != expected:
        raise ValueError
    return result


def members(path):
    raw = path.read_bytes()
    if raw[:8] != b"EERT5000":
        return
    count, offset, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    records = expand(rk, raw[offset : offset + rs], count * 24)
    names = expand(nk, raw[offset + rs : offset + rs + ns], nu)
    for i in range(count):
        _, usize, poff, kind, csize, noff = struct.unpack_from("<6I", records, i * 24)
        nend = names.find(b"\0", noff)
        name = names[noff:nend].decode("ascii", "replace").lower().replace("\\", "/")
        if not name.endswith(".cdf"):
            continue
        try:
            yield name, expand(kind, raw[poff : poff + csize], usize)
        except (ValueError, zlib.error):
            pass


def chunks(data, start, end):
    offset = start
    while offset + 8 <= end:
        tag = data[offset : offset + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", data, offset + 4)[0]
        ps, pe = offset + 8, offset + 8 + size
        if pe > end:
            return
        yield tag, ps, pe
        offset = pe


def printable_fields(payload):
    values = []
    for value in payload.split(b"\0"):
        if len(value) >= 2 and all(32 <= byte < 127 for byte in value):
            values.append(value.decode("ascii"))
    return values


def walk(data, start, end, path, stats, samples):
    for tag, ps, pe in chunks(data, start, end):
        if tag == "FORM" and ps + 4 <= pe:
            form = data[ps : ps + 4].decode("ascii", "replace")
            walk(data, ps + 4, pe, path + "/" + form, stats, samples)
        elif tag in TAGS:
            payload = data[ps:pe]
            fields = printable_fields(payload)
            key = (tag, len(payload), len(fields), tuple(field.rsplit("/", 1)[-1][-48:] for field in fields[:3]))
            stats[key] += 1
            if len(samples[tag]) < 8:
                samples[tag].append((path, len(payload), fields[:4], payload[:24].hex()))


root, cfg = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
configured = []
for line in cfg.read_text(errors="replace").splitlines():
    m = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if m:
        configured.append((int(m.group(1)), m.group(2)))
seen = set()
stats = collections.Counter()
samples = collections.defaultdict(list)
for _, archive_name in sorted(configured, reverse=True):
    archive = root / archive_name
    if not archive.exists():
        continue
    for name, data in members(archive):
        if name in seen:
            continue
        seen.add(name)
        top = list(chunks(data, 0, len(data)))
        if len(top) == 1 and top[0][0] == "FORM":
            _, ps, pe = top[0]
            if data[ps : ps + 4] == b"CLDF":
                walk(data, ps + 4, pe, name, stats, samples)

for tag in sorted(TAGS):
    print(f"[{tag}]")
    for (kind, size, string_count, tails), count in stats.most_common():
        if kind == tag:
            print(f"count={count}\tsize={size}\tstrings={string_count}\tsamples={tails}")
            if sum(1 for key in stats if key[0] == tag and stats[key] >= count) >= 12:
                break
    for sample in samples[tag][:3]:
        print(f"example={sample}")
