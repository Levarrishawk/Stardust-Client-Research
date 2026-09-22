#!/usr/bin/env python3
"""Decode loader-confirmed EMTR flocking/cage fields from scalar inventory."""
from __future__ import annotations

import csv
import struct
import sys
from collections import Counter, defaultdict
from pathlib import Path


def f32(raw: bytes, offset: int) -> float:
    return struct.unpack_from("<f", raw, offset)[0]


def u32(raw: bytes, offset: int) -> int:
    return struct.unpack_from("<I", raw, offset)[0]


def decode(version: str, raw: bytes):
    if version == "0009":
        return []
    if version in {"0010", "0011"}:
        names = [
            "flocking_enabled", "separation_distance", "alignment_gain",
            "cohesion_gain", "separation_gain", "cage_width",
            "cage_y_min", "cage_y_max",
        ]
        values = [(names[0], int(raw[0]))]
        values.extend((name, f32(raw, 1 + i * 4)) for i, name in enumerate(names[1:]))
        values.append(("cage_height_derived", values[-1][1] - values[-2][1]))
        if version == "0011":
            values.append(("environment_color_influence", f32(raw, 29)))
        return values
    names = [
        "flocking_mode", "separation_distance", "alignment_gain",
        "cohesion_gain", "separation_gain", "cage_width", "cage_height",
        "cage_shape", "cage_edge_behavior", "unknown_20c", "unknown_210",
        "environment_color_influence",
    ]
    values = []
    for i, name in enumerate(names):
        value = u32(raw, i * 4) if name in {"flocking_mode", "cage_shape", "cage_edge_behavior"} else f32(raw, i * 4)
        values.append((name, value))
    values.extend([
        ("terrain_height_conform", int(raw[48])),
        ("terrain_normal_align", int(raw[49])),
        ("terrain_height_offset", f32(raw, 50)),
        ("direction_random_angle", f32(raw, 54)),
        ("direction_random_rate", f32(raw, 58)),
    ])
    if version in {"0013", "0014"}:
        values.append(("generation_backlog_policy", int(raw[62])))
    if version == "0014":
        values.append(("pool_reserve_priority", int(raw[63])))
    return values


def main(input_value: str, output_value: str) -> None:
    rows = []
    distributions = defaultdict(Counter)
    with Path(input_value).open(newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            if row["field"] != "extension_raw":
                continue
            version = row["version"]
            raw = bytes.fromhex(row["raw_le"])
            for field, value in decode(version, raw):
                rows.append((row["asset"], row["emtr_offset"], version, field, value))
                distributions[(version, field)][value] += 1
    with Path(output_value).open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(("asset", "emtr_offset", "version", "field", "value"))
        writer.writerows(rows)
    for (version, field), counts in sorted(distributions.items()):
        examples = "; ".join(f"{value}:{count}" for value, count in counts.most_common(8))
        print(version, field, len(counts), examples, sep="\t")


if __name__ == "__main__":
    main(*sys.argv[1:3])
