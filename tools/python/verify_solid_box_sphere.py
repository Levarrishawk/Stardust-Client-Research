"""Independent continuous sphere/solid-box distance reference over synthetic fixtures."""
import argparse
import collections
import hashlib
import json
import math
from pathlib import Path
import random
import subprocess

from verify_triangle_sphere_sweeps import f32
from verify_portal_floor_boxes import transform32


def earliest(row):
    pose = row['pose']
    def local(p):
        d = [p[j]-pose[3][j] for j in range(3)]
        return [sum(pose[k][j]*d[k] for k in range(3)) for j in range(3)]
    a, b = local(row['start']), local(row['end'])
    delta = [b[j]-a[j] for j in range(3)]
    def distance(t):
        p = [a[j]+delta[j]*t for j in range(3)]
        return sum((v-max(row['minimum'][j], min(row['maximum'][j], v)))**2 for j, v in enumerate(p))
    radius2 = row['radius']**2
    if distance(0) <= radius2:
        return 0
    # Independent convex minimization, then entry bisection. No face cuts or quadratics.
    left, right, ratio = 0.0, 1.0, (math.sqrt(5)-1)/2
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
    if distance(minimum) > radius2:
        return None
    low, high = 0.0, minimum
    for _ in range(96):
        mid = (low+high)/2
        if distance(mid) <= radius2:
            high = mid
        else:
            low = mid
    return high


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('core_dll', type=Path); parser.add_argument('output', type=Path)
    args = parser.parse_args()
    args.output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve())
    args.output.mkdir(parents=True, exist_ok=True)
    rng = random.Random(2026100113); cases = []
    for i in range(512):
        angle = rng.uniform(-math.pi, math.pi); c, s = f32(math.cos(angle)), f32(math.sin(angle))
        pose = [[c, 0, s], [0, 1, 0], [-s, 0, c], [f32(rng.uniform(-20, 20)) for _ in range(3)]]
        low = [f32(rng.uniform(-2, 0)) for _ in range(3)]
        high = [f32(low[j]+rng.uniform(.1, 2)) for j in range(3)]
        radius = f32(rng.uniform(.05, .8)); middle = [(a+b)/2 for a, b in zip(low, high)]
        if i < 128:
            start, end = [low[0]-radius-2, middle[1], middle[2]], [high[0]+radius+2, middle[1], middle[2]]
        elif i < 256:
            start, end = [v-2 for v in low], middle
        elif i < 384:
            start, end = middle, [v+2 for v in high]
        else:
            start, end = [rng.uniform(-5, 5) for _ in range(3)], [rng.uniform(-5, 5) for _ in range(3)]
        cases.append(dict(id=i, minimum=low, maximum=high, pose=pose, start=transform32(start, pose), end=transform32(end, pose), radius=radius))
    fixture = args.output/'fixtures.json'; fixture.write_text(json.dumps(cases, separators=(',', ':')), encoding='utf-8')
    run = subprocess.run(['dotnet', str(args.core_dll), '--solid-box-sphere-sweeps', str(fixture)], capture_output=True, text=True, check=True, timeout=120)
    (args.output/'native.txt').write_text(run.stdout, encoding='utf-8')
    rows = [json.loads(line.split('=', 1)[1]) for line in run.stdout.splitlines() if line.startswith('solid_sphere_result=')]
    assert len(rows) == len(cases)
    counts = collections.Counter(); maximum_error = 0
    for case, row in zip(cases, rows):
        expected = earliest(case); assert row['id'] == case['id']
        if expected is None:
            assert row['status'] == 'Clear', (case, row)
        else:
            assert row['status'] == ('InitialOverlap' if expected == 0 else 'Blocked'), (case, row, expected)
            error = abs(row['parameter']-expected); maximum_error = max(maximum_error, error)
            assert error < 1e-9, (case, row, expected)
        counts[row['status']] += 1
    summary = dict(synthetic=True, seed=2026100113, queries=len(cases), outcomes=counts, maximum_parameter_error=maximum_error,
                   fixtures_sha256=hashlib.sha256(fixture.read_bytes()).hexdigest().upper())
    (args.output/'summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print('PASS independent solid box sphere distance: '+json.dumps(summary, sort_keys=True))


if __name__ == '__main__':
    main()
