"""Independent byte parser, requested-slot eligibility, and binary32 planar oracle."""
import hashlib, json, math, runpy, struct, sys
from pathlib import Path
REPO = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO / 'tools/python'))
from verify_creature_template_movement import read_chain, SLOTS
old = runpy.run_path(str(REPO / 'poc/round46-tests/Networking/peer.py'))
template = runpy.run_path(str(REPO / 'poc/round45-tests/Networking/research_checks.py'))['template']
NAMES = ['Speed0', 'Speed1', 'SlopeAngle', 'SlopePercent', 'WaterPercent']

def generate(path):
    temporary = path.with_suffix('.r46.json'); old['generate'](temporary)
    cases = [c for c in json.loads(temporary.read_text()) if not c['Reject']]; temporary.unlink()
    literal = lambda value: b'\x01 ' + struct.pack('<f', value)
    for name, speed in [('signed-zero', -0.0), ('negative', -3.0), ('maximum', float.fromhex('0x1.fffffep127'))]:
        raw = template(dict(speed=struct.pack('<i', 2) + literal(speed) + literal(2), slopeModAngle=literal(-45), slopeModPercent=literal(.25), waterModPercent=literal(.5)))
        cases.append(dict(Name=name, RootPath='object/root.iff', RootSha256=hashlib.sha256(raw).hexdigest().upper(), Resources=[dict(Path='object/root.iff', Kind='Present', Hex=raw.hex())]))
    # A nearer unsupported speed prevents fallback to a farther literal.
    base = template(dict(speed=struct.pack('<i', 2) + literal(9) + literal(8)))
    raw = template(dict(speed=struct.pack('<i', 2) + b'\x03 ' + struct.pack('<2f', 1, 4) + literal(2)), 'object/base.iff')
    cases.append(dict(Name='nearer-unresolved-speed', RootPath='object/root.iff', RootSha256=hashlib.sha256(raw).hexdigest().upper(), Resources=[dict(Path='object/root.iff', Kind='Present', Hex=raw.hex()), dict(Path='object/base.iff', Kind='Present', Hex=base.hex())]))
    path.write_text(json.dumps(cases, indent=2) + '\n'); print('GENERATED', len(cases))

def f32(bits): return struct.unpack('<f', struct.pack('<I', bits))[0]
def multiply(a, b):
    try: return struct.unpack('<I', struct.pack('<f', a * b))[0]
    except OverflowError: return 0xff800000 if (a < 0) != (b < 0) else 0x7f800000

def verify(fixtures, native, oracle, receipt):
    cases = json.loads(fixtures.read_text()); actual = json.loads(native.read_text()); checks = 0
    def snapshot(got, chain):
        nonlocal checks
        expected = old['expected'](chain)
        for name, slot, row in zip(NAMES, SLOTS, got['Inputs'], strict=True):
            e = expected[slot]; assert row['Slot'] == name and all(row[k] == e[k] for k in ['Kind', 'Bits', 'Field'])
            assert [v['SourcePath'] for v in row['Evidence']] == e['Provenance']; checks += 1
            for evidence, visit in zip(row['Evidence'], chain):
                parameter = visit['Selected'][slot]
                assert evidence['Hex'] == (parameter['RawHex'].upper() if parameter else None); checks += 1
        inputs = {v['Slot']: v for v in got['Inputs']}
        for selected in got['Selections']:
            values = [inputs[s] for s in selected['Requested']]
            eligible = all(v['Kind'] == 'Literal' for v in values)
            assert selected['Eligible'] == eligible
            assert selected['NonNegative'] == (eligible and all(f32(v['Bits']) >= 0 for v in values))
            assert selected['Bounded'] == (eligible and all(abs(f32(v['Bits'])) <= 2 for v in values)); checks += 3
        evidence = [e for v in got['Inputs'] for e in v['Evidence']]
        raw = sum(len(bytes.fromhex(e['Hex'])) for e in evidence if e['Hex'])
        assert got['SnapshotBytes'] == len(got['Sources']) * 64 + 320 + len(evidence) * 64 + raw
        assert got['Work'] == 5 + len(got['Sources']) + len(evidence) + raw; checks += 2
        return inputs
    for case, row in zip(cases, actual['Synthetic']['Records'], strict=True):
        assert case['Name'] == row['Name']; resources = {r['Path']: bytes.fromhex(r['Hex']) for r in case['Resources']}
        chain = read_chain(lambda p: (77, 'synthetic', resources[p]) if p in resources else None, case['RootPath'])
        inputs = snapshot(row['Snapshot'], chain)
        for velocity in row['Velocities']:
            value = inputs[velocity['Slot']]; x, y = f32(velocity['XBits']), f32(velocity['YBits'])
            ok = value['Kind'] == 'Literal' and f32(value['Bits']) >= 0 and abs(x*x+y*y-1) <= 1e-6
            xb = yb = None
            if ok:
                xb, yb = multiply(x, f32(value['Bits'])), multiply(y, f32(value['Bits']))
                ok = math.isfinite(f32(xb)) and math.isfinite(f32(yb))
                if not ok: xb = yb = None
            assert (velocity['Eligible'], velocity['OutputXBits'], velocity['OutputYBits']) == (ok, xb, yb), case['Name']; checks += 1
    original = json.loads(oracle.read_text()); snapshot(actual['Original'], original['Chain'])
    assert actual['Mounts'] == 48 and len(actual['Original']['Sources']) == 5
    for visit in original['Chain']:
        source = actual['Original']['Sources'][visit['Path']]
        assert (source['Priority'], source['Bytes'], source['Sha256']) == (visit['Priority'], visit['Bytes'], visit['Sha256']); checks += 1
    result = dict(Cases=len(cases), IndependentChecks=checks, OriginalRawAndProvenanceMatch=True, **{k: actual['Synthetic'][k] for k in ['Ownership', 'Boundaries', 'Rejections']})
    receipt.write_text(json.dumps(result, indent=2)+'\n'); print('PASS', result)

if __name__ == '__main__':
    mode, *paths = sys.argv[1:]; paths = list(map(Path, paths))
    if mode == 'generate': generate(*paths)
    elif mode == 'verify': verify(*paths)
    else: raise ValueError(mode)
