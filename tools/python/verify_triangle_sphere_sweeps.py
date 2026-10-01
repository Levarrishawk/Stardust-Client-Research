"""Synthetic continuous sphere contact agreement with independent convex distance minimization."""
import argparse
import collections
import hashlib
import json
import math
from pathlib import Path
import random
import struct
import subprocess


def dot(a, b):
    return sum(x*y for x, y in zip(a, b))


def sub(a, b):
    return [x-y for x, y in zip(a, b)]


def closest(p, triangle):
    # Solve the constrained two-variable least-squares problem by active sets.
    a, b, c = triangle
    e, f, q = sub(b, a), sub(c, a), sub(p, a)
    ee, ef, ff = dot(e, e), dot(e, f), dot(f, f)
    pe, pf = dot(q, e), dot(q, f)
    determinant = ee*ff-ef*ef
    u, v = (ff*pe-ef*pf)/determinant, (ee*pf-ef*pe)/determinant
    candidates = []
    if u >= 0 and v >= 0 and u+v <= 1:
        candidates.append([a[j]+u*e[j]+v*f[j] for j in range(3)])
    for start, end in [(a, b), (b, c), (c, a)]:
        edge = sub(end, start)
        t = max(0, min(1, dot(sub(p, start), edge)/dot(edge, edge)))
        candidates.append([start[j]+t*edge[j] for j in range(3)])
    return min(candidates, key=lambda x: dot(sub(p, x), sub(p, x)))


def earliest(case):
    a, delta, radius = case['start'], sub(case['end'], case['start']), case['radius']
    def distance(t):
        p = [a[j]+t*delta[j] for j in range(3)]
        q = sub(p, closest(p, case['triangle']))
        return dot(q, q)
    if distance(0) <= radius*radius:
        return 0
    # Distance squared to a convex set along a line is convex. Locate its minimum,
    # then independently bisect the first radius crossing. No feature-time quadratics.
    left, right = 0.0, 1.0
    ratio = (math.sqrt(5)-1)/2
    x, y = right-ratio*(right-left), left+ratio*(right-left)
    dx, dy = distance(x), distance(y)
    for _ in range(96):
        if dx <= dy:
            right, y, dy = y, x, dx
            x = right-ratio*(right-left); dx = distance(x)
        else:
            left, x, dx = x, y, dy
            y = left+ratio*(right-left); dy = distance(y)
    minimum = min([left, right, (left+right)/2, 1.0], key=distance)
    if distance(minimum) > radius*radius:
        return None
    low, high = 0.0, minimum
    for _ in range(96):
        middle = (low+high)/2
        if distance(middle) <= radius*radius:
            high = middle
        else:
            low = middle
    return high


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def cases():
    rng = random.Random(2026100110)
    result = []
    for i in range(512):
        angles = [rng.uniform(-math.pi, math.pi) for _ in range(3)]
        translation = [rng.uniform(-10, 10) for _ in range(3)]
        def transform(p):
            x, y, z = p
            for axis, angle in enumerate(angles):
                c, s = math.cos(angle), math.sin(angle)
                if axis == 0:
                    y, z = c*y-s*z, s*y+c*z
                elif axis == 1:
                    x, z = c*x+s*z, -s*x+c*z
                else:
                    x, y = c*x-s*y, s*x+c*y
            return [f32(v+t) for v, t in zip([x, y, z], translation)]
        radius = f32(rng.uniform(.05, .8))
        if i < 128:
            start, end = [.5, .5, radius+1], [.5, .5, -radius-1]
        elif i < 256:
            start, end = [-2, -2, .2], [.5, .5, .2]
        elif i < 384:
            start, end = [.5, .5, radius*.2], [.5, .5, radius+2]
        else:
            start, end = [rng.uniform(-5, 5) for _ in range(3)], [rng.uniform(-5, 5) for _ in range(3)]
        result.append(dict(id=i, triangle=[transform(p) for p in [(0, 0, 0), (2, 0, 0), (0, 2, 0)]], start=transform(start), end=transform(end), radius=radius))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('core_dll', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    fixture = args.output/'synthetic-sweeps.json'
    inputs = cases()
    fixture.write_text(json.dumps(inputs, separators=(',', ':'))+'\n', encoding='utf-8')
    pin = hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    run = subprocess.run(['dotnet', str(args.core_dll.resolve()), '--triangle-sphere-sweeps', str(fixture.resolve())], capture_output=True, text=True, timeout=90)
    (args.output/'native.txt').write_text(run.stdout+run.stderr, encoding='utf-8')
    assert run.returncode == 0, 'Native probe failed; see private native output'
    rows = [json.loads(line.split('=', 1)[1]) for line in run.stdout.splitlines() if line.startswith('triangle_sphere_result=')]
    assert len(rows) == len(inputs)
    counts = collections.Counter(); maximum_error = 0; maximum_normal_error = 0
    for case, row in zip(inputs, rows):
        assert row['id'] == case['id']
        expected = earliest(case)
        if expected is None:
            assert row['status'] == 'Clear' and row['parameter'] == 1 and row['triangle'] is None, (case['id'], expected, row)
            counts['clear'] += 1
            continue
        error = abs(expected-row['parameter']); maximum_error = max(maximum_error, error)
        assert error < 1e-8 and row['triangle'] == 0, (case['id'], expected, row, error)
        assert row['status'] == ('InitialOverlap' if expected == 0 else 'Blocked')
        counts['initial' if expected == 0 else 'contact'] += 1
        center = [case['start'][j]+expected*(case['end'][j]-case['start'][j]) for j in range(3)]
        normal = sub(center, closest(center, case['triangle'])); length = math.sqrt(dot(normal, normal))
        normal_error = max(abs(v/length-w) for v, w in zip(normal, row['normal']))
        maximum_normal_error = max(maximum_normal_error, normal_error)
        assert normal_error < 1e-6
    summary = dict(synthetic=True, cases=len(inputs), seed=2026100110, fixture_sha256=pin, counts=counts, maximum_parameter_error=maximum_error, maximum_normal_error=maximum_normal_error)
    (args.output/'summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(summary))


if __name__ == '__main__':
    main()
