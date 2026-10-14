"""Independent Fraction/Decimal oracle for the target road graph under declared modern rounding.

Original baked vectors may appear in receipts only under ignored private output paths.
No original x87 or shader/RNG/flora parity follows from this arithmetic check.
"""
import argparse
import json
import struct
from decimal import Decimal, localcontext
from fractions import Fraction
from pathlib import Path


def f32(bits):
    return struct.unpack('<f', struct.pack('<I', bits))[0]


def bits(value):
    return struct.unpack('<I', struct.pack('<f', float(value)))[0]


def q(value):
    return Fraction.from_float(float(value))


def wide(value):
    return q(value)


def narrow(value):
    return q(f32(bits(value)))


def add(a, b):
    return wide(a + b)


def sub(a, b):
    return wide(a - b)


def mul(a, b):
    return wide(a * b)


def div(a, b):
    return wide(a / b)


def desired(segment, x, z):
    points = segment['Points']
    a, b = points[0], points[-1]
    axis = 'X' if abs(sub(q(b['X']), q(a['X']))) >= abs(narrow(sub(q(b['Z']), q(a['Z'])))) else 'Z'
    assert segment['UsesFirstAxis'] == (axis == 'X')
    query = x if axis == 'X' else z
    query = max(min(q(a[axis]), q(b[axis])), min(query, max(q(a[axis]), q(b[axis]))))
    ascending = points if a[axis] < b[axis] else list(reversed(points))
    # Independent ordered straddle search instead of target pointer walking.
    index = next(i for i, point in enumerate(ascending) if q(point[axis]) >= query)
    assert index > 0
    low, high = ascending[index - 1], ascending[index]
    t = div(sub(query, q(low[axis])), sub(q(high[axis]), q(low[axis])))
    return narrow(add(q(low['Y']), mul(t, sub(q(high['Y']), q(low['Y'])))))


def evaluate(profile, sample):
    prior = sample['PriorBits']
    if not profile['Enabled']:
        return 'Disabled', prior, None
    amount = q(f32(sample['AmountBits']))
    if amount <= 0:
        return 'NonPositiveInfluence', prior, None
    x, z = q(f32(sample['XBits'])), q(f32(sample['ZBits']))
    points = profile['Points']
    width = q(profile['Width'])
    extent = (narrow(min(q(p['X']) for p in points) - width), narrow(max(q(p['X']) for p in points) + width),
              narrow(min(q(p['Z']) for p in points) - width), narrow(max(q(p['Z']) for p in points) + width))
    if x < extent[0] or x > extent[1] or z < extent[2] or z > extent[3]:
        return 'NoHit', prior, None
    radius = narrow(mul(width, Fraction(1, 2)))
    initial = narrow(mul(radius, radius))
    best = initial
    selected_vertex, selected_segment = None, None
    px = pz = height = None
    for index, point in enumerate(points):
        dx, dz = sub(x, q(point['X'])), sub(z, q(point['Z']))
        candidate = add(mul(dx, dx), mul(dz, dz))
        if candidate < best:
            best = narrow(candidate)
            selected_vertex = index
            px, pz = q(point['X']), q(point['Z'])
            height = q(profile['Segments'][index]['Points'][0]['Y']) if index < len(points) - 1 else q(profile['Segments'][index - 1]['Points'][-1]['Y'])
    for index, (a, b) in enumerate(zip(points, points[1:])):
        dx, dz = sub(q(b['X']), q(a['X'])), sub(q(b['Z']), q(a['Z']))
        sx, sz = narrow(dx), narrow(dz)
        numerator = add(mul(sub(x, q(a['X'])), sx), mul(sub(z, q(a['Z'])), sz))
        denominator = add(mul(dx, dx), mul(dz, dz))
        t = narrow(div(numerator, denominator))
        if not 0 <= t <= 1:
            continue
        candidate_x = narrow(add(q(a['X']), mul(sx, t)))
        candidate_z = narrow(add(q(a['Z']), mul(sz, t)))
        ex, ez = sub(x, candidate_x), sub(z, candidate_z)
        candidate = add(mul(ex, ex), mul(ez, ez))
        if candidate < best:
            best = narrow(candidate)
            selected_segment = index
            px, pz = candidate_x, candidate_z
    if best >= initial:
        return 'NoHit', prior, None
    if selected_segment is not None:
        height = desired(profile['Segments'][selected_segment], px, pz)
        selected_vertex = None
    with localcontext() as context:
        context.prec = 100
        distance = narrow((Decimal(best.numerator) / Decimal(best.denominator)).sqrt())
    core_limit = mul(sub(Fraction(1), q(profile['HeightFeatherDistance'])), radius)
    if distance <= core_limit:
        outcome, result = 'Core', bits(height)
    else:
        outcome = 'Feather'
        result = bits(add(height, mul(sub(q(f32(prior)), height), div(distance, radius))))
    return outcome, result, {'VertexIndex': selected_vertex, 'SegmentIndex': selected_segment,
                              'ProjectedXBits': bits(px), 'ProjectedZBits': bits(pz),
                              'DesiredHeightBits': bits(height), 'DistanceBits': bits(distance)}


def verify(receipt):
    assert receipt['Passed'] and receipt['Checks'] >= 2000
    # A target-compatible stored best distance can round upward, allowing the
    # identical endpoint line candidate to win its later strict comparison.
    endpoint_offset = q(f32(0x3dcccccd))
    endpoint_distance = mul(endpoint_offset, endpoint_offset)
    assert endpoint_distance < narrow(endpoint_distance)
    assert receipt['CollapsedEndpointRouteRejected']
    # System.Text.Json emits shortest float32 decimals. Restore the serialized
    # float32 domain before treating these operands as exact dyadic rationals.
    for profile in receipt['Profiles']:
        for key in ('Width', 'HeightFeatherDistance'):
            profile[key] = f32(bits(profile[key]))
        for point in profile['Points']:
            for key in ('X', 'Z'):
                point[key] = f32(bits(point[key]))
        for segment in profile['Segments']:
            for point in segment['Points']:
                for key in ('X', 'Y', 'Z'):
                    point[key] = f32(bits(point[key]))
    outcomes = set()
    for sample in receipt['Samples']:
        outcome, result, evidence = evaluate(receipt['Profiles'][sample['Profile']], sample)
        assert sample['Outcome'] == outcome and sample['ResultHeightBits'] == result, sample
        for key in ('VertexIndex', 'SegmentIndex', 'ProjectedXBits', 'ProjectedZBits', 'DesiredHeightBits', 'DistanceBits'):
            assert sample[key] == (evidence[key] if evidence else None), (key, sample)
        outcomes.add(outcome)
    assert {'Core', 'Feather', 'NoHit'} <= outcomes
    original = receipt.get('Original')
    if original:
        assert original['TerrainSha256'] == '6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427'
        assert original['RuleOffset'] == 98781 and original['Hits'] > 0 and original['Core'] > 0 and original['Feather'] > 0
        assert original['OverlapHeightAndNormals'] and original['SharedSampleComparisons'] > 0
    return {'passed': True, 'samples': len(receipt['Samples']), 'profiles': len(receipt['Profiles']),
            'original_hit_fixture': original is not None, 'original_numeric_parity': False}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('receipt', type=Path)
    args = parser.parse_args()
    print(json.dumps(verify(json.loads(args.receipt.read_text()))))
