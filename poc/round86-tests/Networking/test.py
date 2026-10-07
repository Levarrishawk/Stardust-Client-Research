"""Bounded independent transport/codec oracle and comparator tests, no session join."""
import argparse
import copy
import json
import struct
import subprocess
from pathlib import Path
import produce
import verify


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--native-dll', type=Path, required=True)
    parser.add_argument('--dotnet', type=Path, required=True)
    a = parser.parse_args()
    checks = negatives = native_checks = native_negatives = 0
    for variant in (None, 'malformed-correction'):
        folder = a.output/(variant or 'normal')
        t = produce.generate(folder, 0x12345678, 100, 'object/creature/player/shared_human_male.iff', 'terrain/tatooine.trn', variant)
        soe = produce.load(14, 'peer')
        soe.SEED = t['ZoneSeed']
        for i, wire in enumerate(t['ExpectedMovementWires']):
            plain = soe.open_frame(bytes.fromhex(wire))
            assert plain == struct.pack('>HH', 9, 3+i)+bytes.fromhex(t['ExpectedMovementApplications'][i])
            assert soe.open_frame(bytes.fromhex(t['MovementAcks'][i])) == struct.pack('>HH', 0x15, 3+i)
            checks += 2
        for event, wire, app in zip(t['CorrectionEvents'], t['CorrectionFrames'], t['CorrectionApplications']):
            assert event['AfterMovementCount'] == 3
            assert soe.open_frame(bytes.fromhex(wire)) == struct.pack('>HH', 9, event['Sequence'])+bytes.fromhex(app)
            checks += 2
        for i, wire in enumerate(t['WorldFrames'], 1):
            assert soe.open_frame(bytes.fromhex(wire))[:4] == struct.pack('>HH', 9, i)
            checks += 1
        assert t['CorrectionEvents'][0]['Sequence'] == len(t['WorldFrames'])+2
        assert len(t['ExpectedDeferred']) == 12
        checks += 2
        native = subprocess.run([str(a.dotnet), str(a.native_dll), str(folder/'transcript.json'), str(folder/'native.json')], capture_output=True, text=True, timeout=30)
        (folder/'native.log').write_text(native.stdout+native.stderr, encoding='utf-8')
        if native.returncode:
            raise AssertionError(native.stdout+native.stderr)
        n = json.loads((folder/'native.json').read_text())
        native_checks += n['Checks']
        native_negatives += n['Negatives']
        for i, row in enumerate(n['Observed'][:3]):
            assert row['Hex'] == t['ExpectedMovementApplications'][i]
            assert verify.primitives(row['Position']) == verify.primitives(t['ExpectedLocalMotions'][i]['Position'])
            assert row['Rows'] == t['ExpectedLocalMotions'][i]['Rows']
            assert row['Flags'] == t['LocalMotionPolicy']['Flags'] and row['ObjectId'] == t['ObjectId']
            assert row['OuterWord'] == t['LocalMotionPolicy']['TimestampBits'][i] and row['MovementCounter'] == t['LocalMotionPolicy']['MovementCounters'][i]
            checks += 5
        for row in n['Observed'][3:]:
            raw = bytes.fromhex(row['Hex'])
            xyz = struct.unpack_from('<3f', raw, 46)
            assert row['Position'] == [xyz[0], xyz[2], xyz[1]]
            assert row['Rows'] == [1, 0, 0, 0, 1, 0, 0, 0, 1]
            assert row['OuterWord'] == 0
            checks += 3
        # Independently apply unsigned half-range ordering; no native result seeds this oracle.
        last = None
        applied = 0
        for o, raw in zip(t['ExpectedCorrectionObservations'], t['CorrectionApplications']):
            counter = struct.unpack_from('<I', bytes.fromhex(raw), 26)[0]
            accept = last is None or 0 < (counter-last) & 0xffffffff < 0x80000000
            assert accept == o['Applied']
            if accept:
                applied += 1
                last = counter
            checks += 1
        assert applied == 2
        checks += 1
        r = dict(AuthWire=t['ExpectedAuthWire'], SelectWire=t['ExpectedSelectWire'], ReadyWire=t['ExpectedReadyWire'], SelectedModels=t['ExpectedSelectedModels'], DeferredHex=t['ExpectedDeferred'],
                 MovementWires=t['ExpectedMovementWires'], LocalMotions=t['ExpectedLocalMotions'], ActorLocalPositions=t['ExpectedActorLocalPositions'], ReadySubmissions=1, Clean=True,
                 FailureObserved=bool(variant), AppliedCorrections=0 if variant else 2, CounterRejectedCorrections=0 if variant else 2,
                 CorrectionObservations=t['ExpectedCorrectionObservations'], ActorCorrectionPositions=t['ExpectedActorCorrectionPositions'], UnhandledMovementHex=t['ExpectedUnhandledMovement'])
        assert verify.verify(t, r)['Passed']
        checks += 1
        mutations = [('AuthWire', ''), ('ReadyWire', ''), ('SelectedModels', {}), ('DeferredHex', []), ('MovementWires', t['ExpectedMovementWires'][:-1]),
                     ('LocalMotions', []), ('ActorLocalPositions', []), ('ReadySubmissions', 2), ('ReadySubmissions', True), ('Clean', False), ('FailureObserved', not bool(variant)),
                     ('AppliedCorrections', 1), ('CounterRejectedCorrections', 1)]
        if not variant:
            mutations += [('CorrectionObservations', []), ('ActorCorrectionPositions', []), ('UnhandledMovementHex', t['ExpectedUnhandledMovement'][::-1])]
        altered_pose = copy.deepcopy(t['ExpectedLocalMotions'])
        altered_pose[0]['Position'][2] = produce.f32(altered_pose[0]['Position'][2]+.01)
        mutations.append(('LocalMotions', altered_pose))
        wrong_id = copy.deepcopy(t['ExpectedLocalMotions'])
        wrong_id[0]['ObjectId'] += 1
        mutations.append(('LocalMotions', wrong_id))
        for key, value in mutations:
            bad = copy.deepcopy(r)
            bad[key] = value
            try:
                verify.verify(t, bad)
            except ValueError:
                negatives += 1
            else:
                raise AssertionError('Comparator admitted '+key)
    receipt = dict(Passed=True, Synthetic=True, LiveInterop=False, Checks=checks, ComparatorNegatives=negatives,
                   NativeCodecChecks=native_checks, NativeMalformedChecks=native_negatives, SessionJoinExecuted=False)
    (a.output/'receipt.json').write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
