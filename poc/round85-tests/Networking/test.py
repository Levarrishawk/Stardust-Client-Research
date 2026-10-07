"""Bounded offline producer/native codec/verifier regression."""
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
    # Producer itself applies the fresh-private guard to every generated directory.
    checks = negatives = 0
    for variant in (None, 'missing4', 'missing6', 'truncated6'):
        folder = a.output/(variant or 'normal')
        t = produce.generate(folder, 0x12345678, 100, 'object/creature/player/shared_human_male.iff', 'terrain/tatooine.trn', variant)
        soe = produce.load(14, 'peer')
        soe.SEED = t['ZoneSeed']
        for i, (frame, name) in enumerate(zip(t['WorldFrames'], t['IngressOrder']), 1):
            plain = soe.open_frame(bytes.fromhex(frame))
            assert plain[:4] == struct.pack('>HH', 9, i)
            assert plain[4:].hex().upper() == t['Applications'][name]
            checks += 2
        assert soe.open_frame(bytes.fromhex(t['ServerReady']))[:4] == struct.pack('>HH', 9, len(t['WorldFrames'])+1)
        assert len(t['ExpectedDeferred']) == 12
        assert t['ExpectedReadyWire'] == produce.load(84, 'transcript').generate(folder/'prior-comparison', 0x12345678, 100, t['ObjectPath'], t['TerrainPath'])['ExpectedReadyWire']
        checks += 3
        native = subprocess.run([str(a.dotnet), str(a.native_dll), str(folder/'transcript.json'), str(folder/'native-models.json')], capture_output=True, text=True, timeout=30)
        (folder/'native.log').write_text(native.stdout+native.stderr, encoding='utf-8')
        if native.returncode:
            raise AssertionError(native.stdout+native.stderr)
        checks += 1
        if variant != 'truncated6':
            assert json.loads((folder/'native-models.json').read_text()) == t['ExpectedSelectedModels']
            checks += 1
        # Synthetic result tests exercise comparator rejection; they are not session observations.
        r = dict(AuthWire=t['ExpectedAuthWire'], SelectWire=t['ExpectedSelectWire'])
        if variant:
            r.update(FailureObserved=True, ReadySubmissions=0, PlayerPublished=False, ReadyWire=None)
        else:
            r.update(ReadyWire=t['ExpectedReadyWire'], SelectedModels=t['ExpectedSelectedModels'], DeferredHex=t['ExpectedDeferred'])
        assert verify.verify(t, r)['Passed']
        checks += 1
        mutations = [('AuthWire', ''), ('SelectWire', '')]
        mutations += [('ReadySubmissions', 1), ('PlayerPublished', True), ('FailureObserved', False), ('ReadyWire', t['ExpectedReadyWire'])] if variant else [('ReadyWire', ''), ('SelectedModels', {}), ('DeferredHex', t['ExpectedDeferred'][::-1]), ('DeferredHex', t['ExpectedDeferred'][:-1])]
        for key, value in mutations:
            bad = copy.deepcopy(r)
            bad[key] = value
            try:
                verify.verify(t, bad)
            except ValueError:
                negatives += 1
            else:
                raise AssertionError('Comparator admitted '+key)
    receipt = dict(Passed=True, Checks=checks, ComparatorNegatives=negatives, NativeTruncationRejection=1,
                   AuthoredServerShaped=True, LiveInterop=False, SessionJoinExecuted=False)
    (a.output/'receipt.json').write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
