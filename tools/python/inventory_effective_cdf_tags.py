from __future__ import annotations

import collections
import csv
import pathlib
import re
import struct
import sys
import zlib


def expand(kind: int, data: bytes, expected: int) -> bytes:
    result = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if result is None or len(result) != expected:
        raise ValueError
    return result


def archive_records(path: pathlib.Path):
    raw = path.read_bytes()
    if raw[:8] != b"EERT5000":
        return
    count, offset, record_kind, record_size, name_kind, name_size, name_usize = struct.unpack_from("<7I", raw, 8)
    records = expand(record_kind, raw[offset : offset + record_size], count * 24)
    names = expand(name_kind, raw[offset + record_size : offset + record_size + name_size], name_usize)
    for index in range(count):
        _, usize, payload_offset, kind, csize, name_offset = struct.unpack_from("<6I", records, index * 24)
        end = names.find(b"\0", name_offset)
        name = names[name_offset:end].decode("ascii", "replace").lower().replace("\\", "/")
        if name.endswith(".cdf"):
            try:
                payload = expand(kind, raw[payload_offset : payload_offset + csize], usize)
            except (ValueError, zlib.error):
                continue
            yield name, payload


def chunks(data: bytes, start: int, end: int):
    offset = start
    while offset + 8 <= end:
        tag = data[offset : offset + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", data, offset + 4)[0]
        payload_start, payload_end = offset + 8, offset + 8 + size
        if payload_end > end:
            break
        yield tag, payload_start, payload_end
        offset = payload_end


def describe(data: bytes):
    outer = list(chunks(data, 0, len(data)))
    if len(outer) != 1 or outer[0][0] != "FORM":
        return "?", []
    _, start, end = outer[0]
    if data[start : start + 4] != b"CLDF":
        return "?", []
    versions = list(chunks(data, start + 4, end))
    if len(versions) != 1 or versions[0][0] != "FORM":
        return "?", []
    _, start, end = versions[0]
    version = data[start : start + 4].decode("ascii", "replace")
    found = []

    def walk(child_start: int, child_end: int):
        for tag, payload_start, payload_end in chunks(data, child_start, child_end):
            if tag == "FORM" and payload_start + 4 <= payload_end:
                found.append("FORM/" + data[payload_start : payload_start + 4].decode("ascii", "replace"))
                walk(payload_start + 4, payload_end)
            else:
                found.append(tag)

    walk(start + 4, end)
    return version, found


root, cfg, output = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2]), pathlib.Path(sys.argv[3])
configured = []
for line in cfg.read_text(errors="replace").splitlines():
    match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
    if match:
        configured.append((int(match.group(1)), match.group(2)))

seen = set()
rows = []
for priority, archive_name in sorted(configured, reverse=True):
    archive = root / archive_name
    if not archive.exists():
        continue
    for name, data in archive_records(archive):
        if name in seen:
            continue
        seen.add(name)
        version, tags = describe(data)
        counts = collections.Counter(tags)
        rows.append((name, archive_name, priority, len(data), version, len(tags), " ".join(f"{key}:{counts[key]}" for key in sorted(counts))))

with output.open("w", newline="", encoding="utf-8") as handle:
    writer = csv.writer(handle)
    writer.writerow(("name", "archive", "priority", "bytes", "version", "child_count", "tag_counts"))
    writer.writerows(sorted(rows))

tag_files = collections.Counter()
tag_instances = collections.Counter()
versions = collections.Counter()
for _, _, _, _, version, _, tag_text in rows:
    versions[version] += 1
    for item in tag_text.split():
        tag, count = item.split(":")
        tag_files[tag] += 1
        tag_instances[tag] += int(count)
print(f"effective_cdfs={len(rows)} versions={dict(versions)} output={output}")
for tag in sorted(tag_files, key=lambda value: (-tag_files[value], value)):
    print(f"{tag}\tfiles={tag_files[tag]}\tinstances={tag_instances[tag]}")
