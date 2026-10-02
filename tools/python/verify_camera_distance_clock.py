"""Independent closed-form audit of authored modern camera recovery fixtures."""
import argparse
import json
import math
import struct
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('native_output', type=Path)
    args = parser.parse_args()
    rows = [json.loads(s.split('=', 1)[1]) for s in args.native_output.read_text(encoding='utf-8-sig').splitlines()
            if s.startswith('camera_distance_clock=')]
    assert len(rows) == 256
    distance = None; maximum = 0
    for index, row in enumerate(rows):
        assert row['i'] == index
        if row['reset']:
            distance = None
        safe, = struct.unpack('<f', struct.pack('<f', row['safe']))
        initial = safe if distance is None else min(distance, safe)
        fraction = -math.expm1(-row['seconds']/.2) if row['active'] else 0
        distance = initial+(safe-initial)*fraction
        maximum = max(maximum, abs(distance-row['Distance']))
        assert 0 <= row['Distance'] <= safe
        assert row['Recovering'] == (distance < safe)
    assert maximum < 1e-12
    print(json.dumps(dict(cases=len(rows), maximum_distance_error=maximum, time_constant=.2,
                          independent_closed_form=True, original_camera_policy=False)))


if __name__ == '__main__':
    main()
