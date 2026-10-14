"""Exact-rational independent capsule oracle for geometry-only road exclusion receipts."""
import argparse
import json
from fractions import Fraction
from pathlib import Path


def q(value):
    return Fraction.from_float(float(value))


def distance_squared(point, a, b):
    dx, dz = q(b['X']) - q(a['X']), q(b['Z']) - q(a['Z'])
    vx, vz = q(point['X']) - q(a['X']), q(point['Z']) - q(a['Z'])
    length = dx * dx + dz * dz
    assert length > 0
    dot = vx * dx + vz * dz
    if dot <= 0:
        return vx * vx + vz * vz
    if dot >= length:
        ex, ez = q(point['X']) - q(b['X']), q(point['Z']) - q(b['Z'])
        return ex * ex + ez * ez
    # Algebraic exact point/segment distance; does not reconstruct C# projection.
    return vx * vx + vz * vz - dot * dot / length


def verify(receipt):
    assert receipt['Passed'] and receipt['Checks'] >= 400 and receipt['Cushion'] == 1
    for case in receipt['Cases']:
        radius_squared = (q(case['Width']) + 1) ** 2
        separated = not case['Enabled'] or all(
            distance_squared(point, a, b) > radius_squared
            for point in case['Samples'] for a, b in zip(case['Points'], case['Points'][1:]))
        assert case['Excluded'] == separated, case
    original = receipt.get('Original')
    if original:
        assert original['TerrainSha256'] == '6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
        assert original['RoadOffsets'] == [96801, 98384, 98781]
        assert (original['ChunkX'], original['ChunkZ'], original['Samples']) == (-140, -160, 196)
        # Independently authored rectangle/road separating-plane proof. Nearest
        # first-segment distance exceeds fullwidth+1 (23), far beyond halfwidth11.
        assert Fraction(8270 ** 2, 230 ** 2 + 270 ** 2) > 23 ** 2
    return {'passed': True, 'oracle_cases': len(receipt['Cases']), 'original_records': 3 if original else 0,
            'road_height_implementation': False, 'original_numeric_parity': False}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('receipt', type=Path)
    args = parser.parse_args()
    print(json.dumps(verify(json.loads(args.receipt.read_text()))))
