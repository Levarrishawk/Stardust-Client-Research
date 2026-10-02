"""Independent synthetic packet / cross-product placement oracle and source identity comparison."""
import json
import math
import struct
import sys
from pathlib import Path

peer = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
native, gpu, headless, cancel = [Path(p).read_text(encoding="utf-8-sig") for p in sys.argv[2:]]
def line(text, prefix):
    return next(s[len(prefix):] for s in text.splitlines() if s.startswith(prefix))
def sources(text, prefix):
    rows = [json.loads(s[len(prefix):]) for s in text.splitlines() if s.startswith(prefix)]
    assert len(rows) == len({r["Path"] for r in rows}) == 37
    assert sum(r["Bytes"] for r in rows) == 4683848
    return sorted(rows, key=lambda r: r["Path"])
assert sources(native, "source=") == sources(gpu, "world_source=") == sources(headless, "world_source=")
expected = struct.pack("<HIQ7fIB", 5, 0xFE89DDEA, 0xF122334455667788, -0., .25, -.5, 2, 123.5, -45.25, 1e-40, 0xAF1DC1A1, 255)
assert len(expected) == 47 and expected[14:18] == b"\0\0\0\x80"
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
for text, prefix in [(native,"placement="),(gpu,"world_placement="),(headless,"world_placement=")]:
    evidence = json.loads(line(text,prefix))
    assert evidence["raw"] == expected.hex().upper()
    assert evidence["policy"] == "Reject" and evidence["normalized"] and not evidence["zero"]
    assert abs(evidence["norm"]-norm) < 1e-14
    assert [f32(v) for v in evidence["translation"]] == [px,py,pz] and [f32(v) for v in evidence["origin"]] == [px,py,-pz]
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
    for marker in ["queued_restart=true","stale_preflight=true","staged_cleanup=true","post_attach_cleanup=true","capacity2=true","generation_destroy_contain=true","late_success_fault=true","awaited_drain=true","PASS: world teardown"]:
        assert marker in text, marker
assert "reflection_once=true" in gpu and "mip_readback=true" in gpu
assert "PASS: world canceled first_lookup=true generation_reset=true archives_closed=true publications=0" in cancel
assert "negatives=15" in native
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_offline_zone_scene import unframe
from verify_zone_scene_protocol import parse
from verify_scene_object_lifecycle import decode
from verify_replication_envelopes import decode as decode_envelope
wires=[]
for text,prefix in [(native,'wire='),(gpu,'world_wire='),(headless,'world_wire=')]:
    wire=json.loads(line(text,prefix));wires.append(wire)
    assert wire['SessionRequest']=='0001000000021234567800000080'
    assert wire['SelectDatagram']==peer['expectedSelect']
    assert wire['ReadyDatagram']==wire['Retransmission']==peer['expectedReady']
    select=unframe(bytes.fromhex(wire['SelectDatagram']));ready=unframe(bytes.fromhex(wire['ReadyDatagram']))
    assert select[:4]==struct.pack('>HH',9,0);parse(select[4:],False)
    assert ready==struct.pack('>HH',9,1)+bytes.fromhex('0100221CFD43')
    names=['start','create','baseline','delta','end','contain','serverReady']
    assert wire['DeliveredApplications']==[peer[n] for n in names]
    assert bytes.fromhex(peer['create'])==expected
    parse(bytes.fromhex(peer['start']),True)
    for name,kind in [('create','create'),('end','end'),('contain','containment')]:decode(kind,bytes.fromhex(peer[name]))
    for name in ['baseline','delta']:
        row=decode_envelope(name,bytes.fromhex(peer[name]),64)
        assert len(bytes.fromhex(peer[name]))==28 and row['RawTag']==0x4352454F and row['DeclaredCount']==99 and row['OpaqueHex']=='7F00FF'
    assert wire['FinalState']=='Ready' and wire['RetainedPackets']==0
    for value in wire['Controls']:
        packet=unframe(bytes.fromhex(value));assert len(packet)==4 and struct.unpack_from('>H',packet)[0] in (17,21)
assert wires[0]==wires[1]==wires[2]
for text,prefix in [(native,'phase='),(gpu,'world_phase='),(headless,'world_phase=')]:
    phases=[s[len(prefix):] for s in text.splitlines() if s.startswith(prefix)]
    assert len(phases)==3 and [s.split()[0] for s in phases]==['Finalized','Prepared','Ready']
print("PASS: 37 source identities; independent 47-byte raw oracle; normalized placement/reflection and five engine point probes; actual SOE select/ready/retry/application/opaque wire export; distinct phases; world invalidation/cancel/drain markers.")
