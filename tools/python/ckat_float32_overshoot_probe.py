"""Read-only CKAT quaternion norm audit with executable-matched F32 stores.

This models the F32 table/component stores shown in Stardust.exe x87 code.
It does not emulate every 80-bit x87 intermediate or establish runtime event
handling. Use only as a targeted numerical sign test on the installed corpus.
"""

import collections
import math
import pathlib
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/python"))
from pst_reference_audit import configured_archives
from scan_tre_sound_references import archive_records, expand


def f32(value):
    return struct.unpack("<f", struct.pack("<f", value))[0]


def children(blob, start, end):
    pos = start
    while pos + 8 <= end:
        tag = blob[pos:pos + 4]
        size = struct.unpack_from(">I", blob, pos + 4)[0]
        stop = pos + 8 + size
        if stop > end:
            raise ValueError("chunk overrun")
        yield tag, pos + 8, stop
        pos = stop
    if pos != end:
        raise ValueError("trailing chunk bytes")


def build_table(image):
    # Original immutable range descriptors and 1/1023, 1/511 constants.
    # These bytes were traced to SharedMath install 0x00AC0E20.
    pe = struct.unpack_from("<I", image, 0x3C)[0]
    nsec = struct.unpack_from("<H", image, pe + 6)[0]
    opt = pe + 24
    image_base = struct.unpack_from("<I", image, opt + 28)[0]
    sec = []
    for i in range(nsec):
        at = opt + struct.unpack_from("<H", image, pe + 20)[0] + i * 40
        _, rva, size, file_off = struct.unpack_from("<4I", image, at + 8)
        sec.append((rva, size, file_off))

    def real(va):
        rva = va - image_base
        for start, size, file_off in sec:
            if start <= rva < start + size:
                return struct.unpack_from("<f", image, file_off + rva - start)[0]
        raise ValueError(hex(va))

    one_over_1023 = real(0x015EAB50)
    one_over_511 = real(0x015EAB48)
    table = [(0.0, 0.0, 0.0)] * 256
    for level, start in enumerate((254, 252, 248, 240, 224, 192, 128)):
        width = real(0x018AF648 + 28 * level)
        scale11 = f32(width * one_over_1023)
        scale10 = f32(width * one_over_511)
        for j in range(1 << level):
            base = f32((j + 1) * width - 1.0)
            table[start | j] = (base, scale11, scale10)
    return table


def norm(word, selectors, table):
    fields = ((word >> 21) & 0x7FF, (word >> 10) & 0x7FF, word & 0x3FF)
    values = []
    for i, (field, selector) in enumerate(zip(fields, selectors)):
        base, scale11, scale10 = table[selector]
        signbit = 0x200 if i == 2 else 0x400
        magnitude = field & (signbit - 1)
        scale = scale10 if i == 2 else scale11
        value = base - magnitude * scale if field & signbit else base + magnitude * scale
        values.append(f32(value))
    return math.fsum(value * value for value in values)


def main():
    asset_root = pathlib.Path(sys.argv[1])
    table = build_table((asset_root / "Stardust.exe").read_bytes())
    seen = set()
    counts = collections.Counter()
    clip_names = set()
    examples = []
    maxima = []
    for _, archive in configured_archives(asset_root, asset_root / "swgemu_live.cfg"):
        for path, usize, kind, packed in archive_records(archive):
            if path in seen:
                continue
            seen.add(path)
            if not path.endswith(".ans") or not usize:
                continue
            data = expand(kind, packed, usize)
            if data[8:12] != b"CKAT" or data[20:24] != b"0001":
                continue
            counts["clips"] += 1
            for tag, a, b in children(data, 24, len(data)):
                if tag == b"FORM" and data[a:a + 4] == b"AROT":
                    for subtag, sa, sb in children(data, a + 4, b):
                        if subtag != b"QCHN":
                            continue
                        keys = struct.unpack_from("<H", data, sa)[0]
                        selectors = data[sa + 2:sa + 5]
                        for key in range(keys):
                            word = struct.unpack_from("<I", data, sa + 7 + 6 * key)[0]
                            value = norm(word, selectors, table)
                            counts["AROT"] += 1
                            if value > 1.00001:
                                counts["overshoot_AROT"] += 1
                                clip_names.add(path)
                                if len(examples) < 8:
                                    examples.append((path, "AROT", key, word, selectors.hex(), value))
                                maxima.append(value)
                elif tag == b"SROT":
                    for offset in range(a, b, 7):
                        selectors = data[offset:offset + 3]
                        word = struct.unpack_from("<I", data, offset + 3)[0]
                        value = norm(word, selectors, table)
                        counts["SROT"] += 1
                        if value > 1.00001:
                            counts["overshoot_SROT"] += 1
                            clip_names.add(path)
                            if len(examples) < 8:
                                examples.append((path, "SROT", (offset - a) // 7, word, selectors.hex(), value))
                            maxima.append(value)
    print("COUNTS", dict(counts))
    print("OVERSHOOT_CLIPS", len(clip_names))
    print("MIN_OVER_THRESHOLD", min(maxima) if maxima else None)
    print("MAX", max(maxima) if maxima else None)
    print("EXAMPLES", examples)


if __name__ == "__main__":
    main()
