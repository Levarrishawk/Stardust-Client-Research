"""Independent accepted45 byte/provenance peer; synthetic fixtures and original details stay private."""
import argparse, hashlib, json, runpy, struct, sys
from pathlib import Path
REPO = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO / 'tools/python'))
from verify_creature_template_movement import inspect_document, read_chain, classify, SLOTS
template = runpy.run_path(str(REPO / 'poc/round45-tests/Networking/research_checks.py'))['template']


def generate(path):
    cases = []
    literal = lambda value, op=32: bytes([1, op]) + struct.pack('<f', value)
    authored = dict(speed=struct.pack('<i', 2) + literal(7) + literal(2), slopeModAngle=literal(45),
                    slopeModPercent=literal(.25), waterModPercent=literal(.5))
    absent = dict(speed=struct.pack('<i', 2) + b'\0 ' * 2, slopeModAngle=b'\0 ', slopeModPercent=b'\0 ', waterModPercent=b'\0 ')
    root, base, end = 'object/root.iff', 'object/base.iff', 'object/end.iff'

    def add(name, raw, parents=None, reject=False, limits=None, pin=None):
        resources = [dict(Path=root, Kind='Present', Hex=raw.hex())]
        resources += [dict(Path=p, Kind='Present' if data else 'Masked', Hex=data.hex() if data else None) for p, data in (parents or {}).items()]
        cases.append(dict(Name=name, RootPath=root, RootSha256=pin or hashlib.sha256(raw).hexdigest().upper(), Resources=resources, Reject=reject, Limits=limits))

    inherited = template(absent, base); parents = {base: template(authored, end), end: template({}, family=b'SHOT')}
    add('inherited', inherited, parents)
    add('all-absent', template(absent))
    add('missing-parameters', template({}))
    add('authored-zero', template(dict(authored, slopeModAngle=literal(0))))
    add('near-override', template(dict(authored, slopeModAngle=literal(12)), base), parents)
    for op in [43, 45, 61, 95, 33]:
        add('relative-or-other-' + str(op), template(dict(authored, slopeModAngle=literal(12, op))))
    ranged = b'\x03 ' + struct.pack('<2f', 1, 4)
    weighted = b'\x02 ' + struct.pack('<i', 2) + struct.pack('<i', 30) + literal(1) + struct.pack('<i', 70) + ranged
    for label, value in [('range', ranged), ('weighted', weighted), ('empty-weighted', b'\x02 ' + struct.pack('<i', 0))]:
        add(label, template(dict(authored, slopeModAngle=value)))
    add('speed-extra', template(dict(authored, speed=struct.pack('<i', 3) + literal(1) + literal(2) + weighted)))
    add('speed-short', template(dict(authored, speed=struct.pack('<i', 1) + literal(1))))
    add('speed-empty', template(dict(authored, speed=struct.pack('<i', 0))))
    for label, value in [('truncated', literal(1)[:-1]), ('trailing', literal(1) + b'x'),
                         ('mode4', b'\x04 '), ('mode-negative', b'\xff '), ('nan', b'\x01 ' + bytes.fromhex('0000c07f')),
                         ('negative-list', b'\x02 ' + struct.pack('<i', -1)), ('excess-list', b'\x02 ' + struct.pack('<i', 65))]:
        add(label, template(dict(authored, slopeModAngle=value)), reject=True)
    add('negative-speed', template(dict(authored, speed=struct.pack('<i', -1))), reject=True)
    add('missing-parent', inherited, reject=True)
    add('masked-parent', inherited, {base: None}, reject=True)
    add('cycle', template(absent, root), reject=True)
    add('wrong-pin', template(authored), reject=True, pin='0' * 64)
    add('non-creature', template({}, family=b'SHOT'), reject=True)
    for size in range(len(inherited)):
        add('iff-cut-' + str(size), inherited[:size], reject=True)
    for name, limits in [('depth', dict(MaximumDepth=1)), ('resources', dict(MaximumResources=1)),
                         ('source-bytes', dict(MaximumSourceBytes=len(inherited))), ('template-bytes', dict(MaximumTemplateBytes=1)),
                         ('parameter-bytes', dict(MaximumParameterBytes=2)), ('nesting', dict(MaximumParameterNesting=1)),
                         ('array', dict(MaximumArrayEntries=1)), ('decoded', dict(MaximumDecodedBytes=1)), ('work', dict(MaximumWork=1))]:
        add('budget-' + name, template(dict(authored, slopeModAngle=weighted), base), parents, True, limits)
    path.write_text(json.dumps(cases, indent=2) + '\n', encoding='utf-8')
    print('GENERATED synthetic cases', len(cases))


def expected(chain):
    result = {}
    for slot in SLOTS:
        provenance = []
        for visit in chain:
            provenance.append(visit['Path']); value = visit['Selected'][slot]
            if value is None or value['Mode'] == 0: continue
            supported = value['Mode'] == 1 and value['RelativeByte'] == 32
            bits = int.from_bytes(bytes.fromhex(value['F32LE']), 'little') if supported else None
            result[slot] = dict(Kind='Literal' if supported else 'Unsupported', Bits=bits,
                                Field=dict(SourcePath=visit['Path'], Family='SCOT', Version=visit['Versions']['SCOT']), Provenance=provenance)
            break
        else: result[slot] = dict(Kind='Absent', Bits=None, Field=None, Provenance=provenance)
    return result


def verify(fixtures, native, original, receipt):
    cases = json.loads(fixtures.read_text()); actual = json.loads(native.read_text()); oracle = json.loads(original.read_text())
    rows = {row['Name']: row for row in actual['Synthetic']['Records']}; positive = negatives = checks = 0
    for case in cases:
        row = rows[case['Name']]; assert row['Rejected'] == case['Reject'], case['Name']; checks += 1
        if case['Reject']: negatives += 1; continue
        resources = {r['Path']: bytes.fromhex(r['Hex']) for r in case['Resources']}
        chain = read_chain(lambda p: (77, 'synthetic', resources[p]) if p in resources else None, case['RootPath'])
        profile = row['Profile']; assert profile['Inputs'] == expected(chain), case['Name']; checks += 5
        assert profile['Operational'] == all(v['Kind'] == 'Literal' for v in expected(chain).values()); checks += 1
        for given, wanted in zip(profile['Visits'], chain, strict=True):
            assert given['SourcePath'] == wanted['Path']
            for slot in SLOTS:
                value = wanted['Selected'][slot]; got = given['Parameters'][slot]
                assert (got is None) == (value is None)
                if value: assert got['Hex'].lower() == value['RawHex'] and got['Mode'] == value['Mode'] and got['RelativeByte'] == value['RelativeByte']
                checks += 1
        positive += 1
    profile = actual['Original']; assert profile['Inputs'] == expected(oracle['Chain']); assert profile['Operational']
    assert actual['Mounts'] == 48 and len(profile['Sources']) == 5 and profile['SourceBytes'] == 8290
    for row, visit in zip(oracle['Chain'], profile['Visits'], strict=True):
        source = profile['Sources'][row['Path']]
        assert (source['Priority'], source['Bytes'], source['Sha256']) == (row['Priority'], row['Bytes'], row['Sha256'])
        for slot in SLOTS:
            wanted = row['Selected'][slot]; got = visit['Parameters'][slot]
            assert (got is None) == (wanted is None)
            if wanted: assert got['Hex'].lower() == wanted['RawHex'] and got['Mode'] == wanted['Mode'] and got['RelativeByte'] == wanted['RelativeByte']
            checks += 1
    result = dict(Cases=len(cases), Positive=positive, Negative=negatives, IndependentChecks=checks,
                  Ownership=actual['Synthetic']['Ownership'], Lifecycle=actual['Synthetic']['Lifecycle'], Boundaries=actual['Synthetic']['Boundaries'],
                  OriginalSources=5, OriginalSourceBytes=8290, OriginalRawAndProvenanceMatch=True,
                  OriginalExecuted=False, LiveContact=False, FinalRuntimeValuesClaimed=False)
    receipt.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8'); print('PASS independent movement profile peer', result)


if __name__ == '__main__':
    p = argparse.ArgumentParser(); p.add_argument('mode', choices=['generate', 'verify']); p.add_argument('paths', nargs='+', type=Path); a = p.parse_args()
    if a.mode == 'generate': generate(a.paths[0])
    else: verify(*a.paths)
