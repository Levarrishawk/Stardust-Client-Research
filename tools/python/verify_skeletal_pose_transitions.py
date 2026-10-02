"""Independent double quaternion/translation oracle for native original-pose blend fixtures."""
import argparse
import json
from pathlib import Path
import numpy as np


def quaternion(row_matrix):
    # System.Numerics stores row-vector transforms; convert to the column-vector convention.
    m = row_matrix[:3, :3].T
    trace = np.trace(m)
    if trace > 0:
        s = np.sqrt(trace + 1) * 2
        q = np.array([(m[2, 1]-m[1, 2])/s, (m[0, 2]-m[2, 0])/s,
                      (m[1, 0]-m[0, 1])/s, s/4])
    else:
        i = int(np.argmax(np.diag(m)))
        j, k = (i+1) % 3, (i+2) % 3
        s = np.sqrt(1 + m[i, i] - m[j, j] - m[k, k]) * 2
        q = np.zeros(4)
        q[i] = s/4
        q[j] = (m[i, j]+m[j, i])/s
        q[k] = (m[i, k]+m[k, i])/s
        q[3] = (m[k, j]-m[j, k])/s
    return q / np.linalg.norm(q)


def blend(first, second, weight):
    if weight == 0:
        return first
    if weight == 1:
        return second
    a, b = quaternion(first), quaternion(second)
    dot = np.dot(a, b)
    if dot < 0:
        dot, b = -dot, -b
    dot = min(1, max(-1, dot))
    if dot > .999999:
        q = (1-weight)*a + weight*b
    else:
        angle = np.arccos(dot)
        q = (np.sin((1-weight)*angle)*a + np.sin(weight*angle)*b)/np.sin(angle)
    x, y, z, w = q / np.linalg.norm(q)
    result = np.eye(4)
    result[:3, :3] = np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
                              [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                              [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]).T
    result[3, :3] = first[3, :3]*(1-weight) + second[3, :3]*weight
    return result


def verify(rows):
    assert len(rows) == 3*53*5
    keys, endpoints = set(), {}
    maximum = 0
    for row in rows:
        pair, joint, weight = row['pair'], row['joint'], row['weight']
        assert pair in (0, 1, 2) and 0 <= joint < 53 and weight in (0, .25, .5, .75, 1)
        key = (pair, joint, weight)
        assert key not in keys
        keys.add(key)
        first, second, actual = [np.array(row[name], dtype=np.float64).reshape(4, 4) for name in ('first', 'second', 'output')]
        assert all(np.isfinite(m).all() for m in (first, second, actual))
        endpoint = (tuple(first.flat), tuple(second.flat))
        previous = endpoints.setdefault((pair, joint), endpoint)
        assert previous == endpoint
        expected = blend(first, second, weight)
        maximum = max(maximum, float(np.max(np.abs(actual-expected))))
        assert np.max(np.abs(actual[:3, :3] @ actual[:3, :3].T - np.eye(3))) < 3e-5
        assert abs(np.linalg.det(actual[:3, :3])-1) < 3e-5
        if weight in (0, 1):
            assert np.array_equal(actual, first if weight == 0 else second)
    assert maximum < 2e-5, maximum
    return {'original_pose_matrices': len(rows), 'pairs': 3, 'joints': 53, 'weights': 5,
            'maximum_component_error': maximum, 'tolerance': 2e-5,
            'scope': 'blend math; source local poses are supplied native fixtures'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('native_output', type=Path)
    args = parser.parse_args()
    assert args.native_output.stat().st_size <= 4*1048576
    rows = [json.loads(line.split('=', 1)[1]) for line in args.native_output.read_text(encoding='utf-8-sig').splitlines()
            if line.startswith('skeletal_pose_blend=')]
    print(json.dumps(verify(rows)))


if __name__ == '__main__':
    main()
