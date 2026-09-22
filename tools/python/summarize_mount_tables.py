#!/usr/bin/env python3
"""Summarize the four effective SWG mount compatibility tables."""
import struct
import sys
from collections import Counter
from pathlib import Path


def table(path):
    raw = Path(path).read_bytes()
    ca, ra = raw.find(b"COLS"), raw.find(b"ROWS")
    cs = struct.unpack_from(">I", raw, ca + 4)[0]
    cp = raw[ca + 8:ca + 8 + cs]
    n = struct.unpack_from("<I", cp)[0]
    names = [x.decode() for x in cp[4:].split(b"\0")[:n]]
    ta = ca + 8 + cs
    ts = struct.unpack_from(">I", raw, ta + 4)[0]
    specs = [x.decode() for x in raw[ta + 8:ta + 8 + ts].split(b"\0")[:n]]
    count = struct.unpack_from("<I", raw, ra + 8)[0]
    cursor, rows = ra + 12, []
    for _ in range(count):
        row = {}
        for name, spec in zip(names, specs):
            if spec.startswith("s"):
                end = raw.index(b"\0", cursor)
                value, cursor = raw[cursor:end].decode("ascii", "replace"), end + 1
            elif spec.startswith("f"):
                value, cursor = struct.unpack_from("<f", raw, cursor)[0], cursor + 4
            else:
                value, cursor = struct.unpack_from("<i", raw, cursor)[0], cursor + 4
            row[name] = value
        rows.append(row)
    return rows


logical, saddle, pose, scale = map(table, sys.argv[1:5])
logical_names = {r["logical_saddle_name"] for r in logical}
saddle_names = {r["logical_saddle_name"] for r in saddle}
appearances = {r["saddle_appearance_name"] for r in saddle}
pose_appearances = {r["saddle_appearance_name"] for r in pose}
print(f"logical rows={len(logical)} uniqueSources={len({r['sat_or_skt_name'] for r in logical})} uniqueLogical={len(logical_names)}")
print(f"saddle rows={len(saddle)} uniqueLogical={len(saddle_names)} uniqueAppearances={len(appearances)} capacities={dict(sorted(Counter(r['saddle_capacity'] for r in saddle).items()))}")
print(f"pose rows={len(pose)} uniqueAppearances={len(pose_appearances)} seatRange={min(r['seat_index'] for r in pose)}..{max(r['seat_index'] for r in pose)} uniquePoses={len({r['rider_pose'] for r in pose})}")
print(f"scale rows={len(scale)} uniqueAppearances={len({r['appearance_name'] for r in scale})} capacities={dict(sorted(Counter(r['saddle_capacity'] for r in scale).items()))}")
print(f"logicalWithoutSaddle={sorted(logical_names-saddle_names)}")
print(f"saddleWithoutLogical={sorted(saddle_names-logical_names)}")
print(f"saddleAppearanceWithoutPose={sorted(appearances-pose_appearances)}")
print(f"poseAppearanceWithoutSaddle={sorted(pose_appearances-appearances)}")
for r in saddle[:8]: print("SAMPLE", r)
