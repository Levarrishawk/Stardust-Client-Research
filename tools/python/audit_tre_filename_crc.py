"""Read-only raw versus normalized TRE filename CRC audit; prints compact anomalies."""
import argparse
import hashlib
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from tre_crc_vectors import expand, swg_crc


def normalize(name):
    value = name.replace(b"\\", b"/")
    while True:
        value = value.lstrip(b"/")
        if value.startswith(b"./"):
            value = value[2:]
        elif value.startswith(b"../"):
            value = value[3:]
        else:
            break
    while b"//" in value:
        value = value.replace(b"//", b"/")
    return value.lower()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("archive", type=Path)
    args = parser.parse_args()
    raw = args.archive.read_bytes()
    if raw[:8] != b"EERT5000":
        raise ValueError("Expected TREE 0005")
    count, offset, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    records = expand(rk, raw[offset:offset + (rs if rk else count * 24)], count * 24)
    name_start = offset + (rs if rk else count * 24)
    names = expand(nk, raw[name_start:name_start + (ns if nk else nu)], nu)
    raw_failures = normalized_failures = 0
    for index in range(count):
        stored, _, _, _, _, start = struct.unpack_from("<6I", records, index * 24)
        name = names[start:names.index(0, start)]
        raw_crc, normalized_crc = swg_crc(name), swg_crc(normalize(name))
        raw_failures += stored != raw_crc
        normalized_failures += stored != normalized_crc
        if stored != normalized_crc and normalized_failures <= 12:
            print(f"row={index} name={name.decode('ascii')} stored={stored:08X} raw={raw_crc:08X} normalized={normalized_crc:08X}")
    print(f"archive={args.archive.name} sha256={hashlib.sha256(raw).hexdigest().upper()} records={count} raw_failures={raw_failures} normalized_failures={normalized_failures}")


if __name__ == "__main__":
    main()
