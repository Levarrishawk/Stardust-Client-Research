"""Independent synthetic packet / cross-product placement oracle and source identity comparison."""
import json
import math
import struct
import sys
from pathlib import Path

native, gpu, headless, cancel = [Path(p).read_text(encoding="utf-8-sig") for p in sys.argv[1:]]
def line(text, prefix):
    return next(s[len(prefix):] for s in text.splitlines() if s.startswith(prefix))
def sources(text, prefix):
    rows = [json.loads(s[len(prefix):]) for s in text.splitlines() if s.startswith(prefix)]
    assert len(rows) == len({r["Path"] for r in rows}) == 37
    assert sum(r["Bytes"] for r in rows) == 4683848
    return sorted(rows, key=lambda r: r["Path"])
assert sources(native, "source=") == sources(gpu, "async_source=") == sources(headless, "async_source=")
expected = struct.pack("<HIQ7fIB", 5, 0xFE89DDEA, 0xDEADBEEF00000001, .2, -0., .3, 1.7, -2, 7, 4, 0xAF1DC1A1, 255)
assert len(expected) == 47 and expected[18:22] == b"\0\0\0\x80"
qx, qy, qz, qw, px, pz, py = struct.unpack_from("<7f", expected, 14)
norm = math.hypot(qx, qy, qz, qw)
xyz = [v / norm for v in [qx, qy, qz]]
w = qw / norm
def cross(a, b):
    return [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]]
def rotate(v):
    first = cross(xyz, v)
    second = cross(xyz, first)
    return [v[i] + 2*w*first[i] + 2*second[i] for i in range(3)]
columns = [rotate(v) for v in [[1,0,0], [0,1,0], [0,0,1]]]
rows = [[columns[c][r] for c in range(3)] for r in range(3)]
sign = [1, 1, -1]
engine_columns = [[columns[c][r]*sign[c]*sign[r] for r in range(3)] for c in range(3)]
def f32(v):
    return struct.unpack("<f", struct.pack("<f", v))[0]
def close(actual, wanted, tolerance):
    assert len(actual) == len(wanted)
    for a, b in zip(actual, wanted):
        assert abs(f32(a)-b) <= tolerance*max(1,abs(b)), (a,b)
for text, prefix in [(native,"placement="),(gpu,"async_placement="),(headless,"async_placement=")]:
    evidence = json.loads(line(text,prefix))
    assert evidence["raw"] == expected.hex().upper()
    assert evidence["policy"] == "Reject" and evidence["normalized"] and not evidence["zero"]
    assert abs(evidence["norm"]-norm) < 1e-14
    assert evidence["translation"] == [px,py,pz] and evidence["origin"] == [px,py,-pz]
    for a,b in zip(evidence["rows"],rows): close(a,b,2e-7)
    for a,b in zip(evidence["columns"],engine_columns): close(a,b,2e-7)
    assert len(evidence["points"]) == 5
    for point in evidence["points"]:
        v = [f32(x) for x in point["source"]]
        close(point["engineInput"],[v[i]*sign[i] for i in range(3)],1e-7)
        rotated = rotate(v)
        close(point["result"],[(rotated[i]+[px,py,pz][i])*sign[i] for i in range(3)],3e-6)
    # Fixture discriminates transpose, wrong X/Z/Y mapping and missing/one-sided reflection.
    assert any(abs(engine_columns[c][r]-rows[c][r]) > .01 for c in range(3) for r in range(3))
assert line(native,"create=") == expected.hex().upper()
for text in [gpu,headless]:
    for marker in ["same_handle_restart=true","staged_cleanup=true","post_attach_cleanup=true","capacity2=true","lifecycle5=true","canceled_late_success=true","late_fault_observed=true","disposal_drain=true","PASS: async teardown"]:
        assert marker in text, marker
assert "source_transform_applied=true reflection_once=true" in gpu and "mip_readback=true" in gpu
assert "PASS: async canceled first_lookup=true archives_closed=true publications=0" in cancel
assert "negatives=19" in native
print("PASS: 37 source identities; independent 47-byte raw oracle; normalized placement/reflection and five engine point probes; attempt/lifecycle/cancel/drain markers.")
