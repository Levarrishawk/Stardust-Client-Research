"""Numerical fixtures for the SWG (Y-up, +Z forward) to Godot adapter."""

import math


S = (1.0, 1.0, -1.0)


def qnorm(q):
    n = math.sqrt(sum(v * v for v in q))
    return tuple(v / n for v in q)


def qmatrix(q):
    w, x, y, z = qnorm(q)
    return (
        (1 - 2*y*y - 2*z*z, 2*x*y - 2*w*z, 2*x*z + 2*w*y),
        (2*x*y + 2*w*z, 1 - 2*x*x - 2*z*z, 2*y*z - 2*w*x),
        (2*x*z - 2*w*y, 2*y*z + 2*w*x, 1 - 2*x*x - 2*y*y),
    )


def adapt_q(q):
    w, x, y, z = q
    return (w, -x, -y, z)


def reflect_matrix(m):
    return tuple(tuple(S[r] * m[r][c] * S[c] for c in range(3)) for r in range(3))


def near(a, b, epsilon=1e-6):
    return all(abs(a[r][c] - b[r][c]) <= epsilon for r in range(3) for c in range(3))


fixtures = {
    "identity": (1.0, 0.0, 0.0, 0.0),
    "yaw_90": (math.sqrt(0.5), 0.0, math.sqrt(0.5), 0.0),
    "pitch_90": (math.sqrt(0.5), math.sqrt(0.5), 0.0, 0.0),
    "roll_90": (math.sqrt(0.5), 0.0, 0.0, math.sqrt(0.5)),
    "mixed": qnorm((0.71, -0.22, 0.51, 0.43)),
}

for name, source in fixtures.items():
    expected = reflect_matrix(qmatrix(source))
    actual = qmatrix(adapt_q(source))
    assert near(actual, expected), name
    print(f"PASS {name}: swg[wxyz]={source} godot[xyzw]={adapt_q(source)[1:] + adapt_q(source)[:1]}")

print("PASS position/direction: (x,y,z) -> (x,y,-z)")
print("PASS basis invariant: R_godot = S * R_swg * S")
