"""Independent packet/native transport checks and fabricated-result comparator regression."""
import argparse
import copy
import json
import struct
import subprocess
import sys
from pathlib import Path
import produce
import verify


def prefix(t):
    return dict(AuthWire=t['ExpectedAuthWire'], SelectWire=t['ExpectedSelectWire'], ReadyWire=t['ExpectedReadyWire'], SelectedModels=t['ExpectedSelectedModels'], DeferredHex=t['ExpectedDeferred'],
                MovementWires=t['ExpectedMovementWires'], LocalMotions=t['ExpectedLocalMotions'], ActorLocalPositions=t['ExpectedActorLocalPositions'],
                CorrectionObservations=t['ExpectedCorrectionObservations'], ActorCorrectionPositions=t['ExpectedActorCorrectionPositions'],
                AppliedCorrections=2, CounterRejectedCorrections=2, UnhandledMovementHex=t['ExpectedUnhandledMovement'], ReadySubmissions=1, Clean=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--native-dll', type=Path, required=True)
    parser.add_argument('--dotnet', type=Path, required=True)
    a = parser.parse_args()
    t = produce.generate(a.output, 0x12345678, 100, 'object/creature/player/shared_human_male.iff', 'terrain/tatooine.trn')
    prior = produce.load(86, 'produce').generate(a.output/'prior86', 0x12345678, 100, t['ObjectPath'], t['TerrainPath'])
    assert all(t[key] == value for key, value in prior.items())
    checks = 1
    soe = produce.load(14, 'peer')
    soe.SEED = t['ZoneSeed']
    assert bytes.fromhex(t['ExpectedDisconnectPlain']) == b'\0\5'+struct.pack('>I', 0x87654321)+b'\0\6'
    assert soe.open_frame(bytes.fromhex(t['ExpectedDisconnectWire'])).hex().upper() == t['ExpectedDisconnectPlain']
    assert t['DisconnectReliableSequence'] is None and t['DisconnectReplyRequired'] is False
    checks += 3
    native = subprocess.run([str(a.dotnet), str(a.native_dll), str(a.output/'transcript.json'), str(a.output/'native.json')], capture_output=True, text=True, timeout=30)
    (a.output/'native.log').write_text(native.stdout+native.stderr, encoding='utf-8')
    if native.returncode:
        raise AssertionError(native.stdout+native.stderr)
    observed = json.loads((a.output/'native.json').read_text())
    assert observed['Passed'] and observed['SessionSendExecuted'] is False
    checks += 1
    expected = [('Disconnect', 'PeerDisconnected'), ('WrongId', 'InvalidDataException'), ('BadCrc', 'RejectedFraming'),
                ('ShortDecoded', 'InvalidDataException'), ('ExtraDecoded', 'InvalidDataException'), ('AlternateReason', 'PeerDisconnected')]
    for row, (name, outcome) in zip(observed['Records'], expected):
        closed = outcome == 'PeerDisconnected'
        assert row == dict(Name=name, Outcome=outcome, State='Closed' if closed else 'Established', Pending=0 if closed else 1, Retained=0 if closed else 1, ControlReplies=0)
        checks += 1
    assert len(observed['Records']) == len(expected)
    checks += 1
    negatives = positives = 0
    for mode, submitted in [('success', True), ('send-error', True), ('send-timeout', True), ('send-error', False), ('send-timeout', False)]:
        r = prefix(t)
        r.update(Mode=mode, DisconnectRequested=True, DisconnectSubmitted=submitted, DisconnectSendCompleted=mode=='success',
                 DisconnectWire=t['ExpectedDisconnectWire'] if submitted else None, TypedFailureObserved=mode!='success', FailureObserved=mode!='success')
        assert verify.verify(t, r)['Passed']
        positives += 1
        mutations = [('Mode', 'unknown'), ('DisconnectRequested', False), ('DisconnectSubmitted', 1), ('DisconnectSendCompleted', mode!='success'),
                     ('TypedFailureObserved', mode=='success'), ('FailureObserved', mode=='success'), ('Clean', False), ('ReadySubmissions', 2),
                     ('MovementWires', t['ExpectedMovementWires']+[t['ExpectedMovementWires'][-1]]), ('CorrectionObservations', []),
                     ('ActorLocalPositions', []), ('DeferredHex', []), ('SelectedModels', {}), ('DisconnectWire', '' if submitted else t['ExpectedDisconnectWire'])]
        for key, value in mutations:
            bad = copy.deepcopy(r)
            bad[key] = value
            try:
                verify.verify(t, bad)
            except ValueError:
                negatives += 1
            else:
                raise AssertionError('Comparator admitted '+key)
        result_path = a.output/f'fabricated-{mode}-{submitted}.json'
        result_path.write_text(json.dumps(r), encoding='utf-8')
        command = [sys.executable, str(Path(__file__).with_name('verify.py')), '--transcript', str(a.output/'transcript.json'), '--result', str(result_path), '--output', str(a.output/f'cli-{mode}-{submitted}.json')]
        cli = subprocess.run(command, capture_output=True, text=True, timeout=30)
        if cli.returncode:
            raise AssertionError(cli.stdout+cli.stderr)
        checks += 1
    receipt = dict(Passed=True, Synthetic=True, Checks=checks, ComparatorPositives=positives, ComparatorNegatives=negatives,
                   NativeTransportChecks=observed['Checks'], FabricatedComparisonsOnly=True, JoinedSendExecuted=False, LiveInterop=False)
    (a.output/'receipt.json').write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
