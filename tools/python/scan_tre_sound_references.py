#!/usr/bin/env python3
"""Find embedded .snd path references in non-audio payloads of configured TREs."""
from __future__ import annotations
import csv, re, struct, sys, zlib
from pathlib import Path


PATH_RE = re.compile(rb"[A-Za-z0-9_./\\-]{3,}\.snd", re.I)
SKIP_SUFFIXES = {
    ".snd", ".wav", ".mp3", ".dds", ".tga", ".jpg", ".png", ".bmp",
    ".msh", ".lod", ".lmg", ".skt", ".sat", ".ans", ".trn",
}


def expand(kind, data, expected):
    result = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if result is None or len(result) != expected:
        raise ValueError("unsupported or invalid compressed block")
    return result


def archive_records(path):
    raw = path.read_bytes()
    if raw[:8] != b"EERT5000":
        return
    count, offset, record_kind, record_size, name_kind, name_size, name_usize = struct.unpack_from("<7I", raw, 8)
    record_data = expand(record_kind, raw[offset:offset + record_size], count * 24)
    name_data = expand(name_kind, raw[offset + record_size:offset + record_size + name_size], name_usize)
    for index in range(count):
        _, usize, payload_offset, kind, csize, name_offset = struct.unpack_from("<6I", record_data, index * 24)
        end = name_data.find(b"\0", name_offset)
        name = name_data[name_offset:end].decode("ascii", "replace").lower().replace("\\", "/")
        stored_size = usize if kind == 0 else csize
        yield name, usize, kind, raw[payload_offset:payload_offset + stored_size]


def main(root_value, cfg_value, scalar_csv_value, output_value):
    root, cfg = Path(root_value), Path(cfg_value)
    category13 = None if scalar_csv_value == "-" else {
        row["name"].lower().replace("\\", "/")
        for row in csv.DictReader(Path(scalar_csv_value).open(newline="", encoding="utf-8"))
        if row["+0x44"] == "13"
    }
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match:
            configured.append((int(match.group(1)), match.group(2)))
    winners, findings, errors = set(), [], []
    for priority, archive_name in sorted(configured, reverse=True):
        archive = root / archive_name
        if not archive.exists():
            continue
        for name, usize, kind, packed in archive_records(archive):
            if name in winners:
                continue
            winners.add(name)
            if not packed or Path(name).suffix in SKIP_SUFFIXES or usize > 32 * 1024 * 1024:
                continue
            try:
                payload = expand(kind, packed, usize)
            except Exception as exc:
                errors.append((archive_name, name, str(exc)))
                continue
            references = {
                match.group().decode("ascii", "replace").lower().replace("\\", "/")
                for match in PATH_RE.finditer(payload)
            }
            selected = references if category13 is None else references & category13
            for reference in sorted(selected):
                findings.append((archive_name, priority, name, reference))
    with Path(output_value).open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("archive", "priority", "referrer", "category13_sound"))
        writer.writerows(findings)
    category_label = "all" if category13 is None else str(len(category13))
    print(f"configured={len(configured)} winners={len(winners)} category13={category_label} findings={len(findings)} errors={len(errors)}")
    for row in findings[:100]:
        print("\t".join(map(str, row)))
    for row in errors[:10]:
        print("ERROR\t" + "\t".join(row))


if __name__ == "__main__":
    main(*sys.argv[1:5])
