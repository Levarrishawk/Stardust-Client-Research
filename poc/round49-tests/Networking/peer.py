"""Independent raw authored/received presence, provenance and context oracle."""
import json, runpy, struct, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/python'))
from verify_creature_template_movement import read_chain
FIELDS = [0, 1, 4, 5, 7, 8, 9, 10, 11, 12]
AUTHORED = {7: 'speed[0]', 11: 'speed[1]', 8: 'slopeModAngle', 9: 'slopeModPercent', 12: 'waterModPercent'}
BITS = [0, 0x80000000, 1, 0xBF800000, 0x7F7FFFFF, 0x7F800000, 0xFF800000, 0x7F800001, 0xFFC01234, 0x3F800000]

def generate(path):
    temporary = path.with_suffix('.profiles.json')
    runpy.run_path(str(ROOT / 'poc/round47-tests/Networking/peer.py'))['generate'](temporary)
    profiles = json.loads(temporary.read_text()); temporary.unlink()
    baseline = bytearray.fromhex(json.loads((ROOT / 'poc/round39-tests/Runtime/ordered-fixtures.json').read_text())['Peer']['baseline'])
    struct.pack_into('<Q', baseline, 6, 42)
    patterns = [None, [(6, 123)], [(7, 0x7F800001), (7, 0)], [(11, 0x80000000)]]
    patterns += [[(field, BITS[(offset+i) % len(BITS)]) for i, field in enumerate(FIELDS)] for offset in range(len(BITS))]
    cases = []
    for pi in range(len(profiles)):
        for typed in [False, True]:
            for final in [False, True]:
                for updates in patterns:
                    case = dict(Profile=pi, Typed=typed, Finalized=final, Updates=updates)
                    if updates is not None:
                        body = struct.pack('<H', 65535) + b''.join(struct.pack('<H' + ('Q' if i == 6 else 'I'), i, bits) for i, bits in updates)
                        case['Wire'] = (struct.pack('<HIQIBI', 5, 0x12862153, 42, 0x4352454F, 4, len(body)) + body).hex().upper()
                    cases.append(case)
    path.write_text(json.dumps(dict(Baseline=baseline.hex().upper(), Profiles=profiles, Cases=cases), separators=(',', ':'))+'\n')
    print('GENERATED', len(cases))

def verify(fixtures, native, receipt):
    fixture = json.loads(fixtures.read_text()); actual = json.loads(native.read_text())
    expected = runpy.run_path(str(ROOT / 'poc/round46-tests/Networking/peer.py'))['expected']; profiles = []
    for p in fixture['Profiles']:
        resources = {r['Path']: bytes.fromhex(r['Hex']) for r in p['Resources']}
        chain = read_chain(lambda q: (77, 'synthetic', resources[q]) if q in resources else None, p['RootPath'])
        profiles.append(expected(chain))
    checks = 0
    for case, row in zip(fixture['Cases'], actual['Cases'], strict=True):
        assert row['HasCurrentTypedState'] == case['Typed'] and row['Phase'] == int(case['Finalized'])
        assert row['IsOperationalContext'] == (case['Typed'] and case['Finalized'])
        assert row['HasReceived'] == (case['Updates'] is not None) and row['OutputBytes'] == 1632; checks += 4
        received = {index: (bits, ordinal) for ordinal, (index, bits) in enumerate(case['Updates'] or [])}
        for index, field in zip(FIELDS, row['Fields'], strict=True):
            assert field['Index'] == index
            authored = profiles[case['Profile']].get(AUTHORED.get(index))
            assert field['AuthoredKind'] == (authored['Kind'] if authored else None)
            assert field['AuthoredBits'] == (authored['Bits'] if authored else None)
            assert field['AuthoredPaths'] == (authored['Provenance'] if authored else None); checks += 4
            stores = {0: 0x42F14C, 1: 0x42F122, 4: 0x42F093, 5: 0x42F071, 10: 0x42F176}
            assert field['ConstructorLiteralStoreVA'] == stores.get(index)
            assert field['ConstructorLiteralBits'] == (0x3F800000 if index in stores else None); checks += 2
            if index in received:
                bits, ordinal = received[index]; origin = 'Received'
                assert field['ReceivedBits'] == bits
                assert field['ReceivedProvenance'] == dict(Revision=1, DeclaredCount=65535, Ordinal=ordinal, Origin=1)
            else:
                assert field['ReceivedBits'] is None and field['ReceivedProvenance'] is None
                if authored is None: bits, origin = 0x3F800000, 'ConstructorLiteralOne'
                elif authored['Kind'] == 'Literal': bits, origin = authored['Bits'], 'ConstructorAuthoredRaw'
                else: bits, origin = None, 'Unknown'
            eligibility = 'Unknown' if bits is None else 'NonFinite' if bits & 0x7F800000 == 0x7F800000 else 'PendingPhase' if not case['Finalized'] else 'MissingTypedState' if not case['Typed'] else 'FiniteCurrent'
            assert (field['RawBits'], field['Origin'], field['Eligibility']) == (bits, origin, eligibility); checks += 3
    result = dict(Cases=len(fixture['Cases']), Profiles=len(profiles), IndependentChecks=checks, NativeChecks=actual['Checks'],
                  RawPresenceProvenanceOverride=True, OriginalExecuted=False, RuntimeArbitrationClaimed=False)
    receipt.write_text(json.dumps(result, indent=2)+'\n'); print('PASS', result)

if __name__ == '__main__':
    mode, *paths = sys.argv[1:]; paths = list(map(Path, paths))
    if mode == 'generate': generate(*paths)
    else: verify(*paths)
