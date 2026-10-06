"""Bounded producer/verifier positives and negatives, before any native comparison."""
import argparse
import copy
import hashlib
import json
import runpy
import struct
from pathlib import Path
import transport_fixtures as api


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--profile', type=Path, required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    out = runpy.run_path(str(api.ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](args.output_dir, api.ROOT, directory=True)
    require = api.require
    require(args.profile.stat().st_size <= 16384, 'Bounded input')
    p = json.loads(args.profile.read_text(encoding='utf-8-sig'))
    checks = 0; negatives = 0
    def check(value):
        nonlocal checks
        require(value, 'Test failure'); checks += 1
    def reject(call):
        nonlocal negatives
        try: call()
        except (ValueError, TypeError, KeyError, OverflowError): negatives += 1
        else: raise ValueError('Expected rejection')
    original = copy.deepcopy(p)
    profiles = [api.reseed(p, cid, seed) for cid, seed in [(0, 0), (0xffffffff, 0xffffffff), (0x11223344, 0x55667788)]]
    check(p == original)
    for fresh in profiles:
        old = api.encoder(p['SoeSeed']); new = api.encoder(fresh['SoeSeed'])
        check({k:v for k,v in fresh.items() if not k.startswith('Soe')} == {k:v for k,v in p.items() if not k.startswith('Soe')})
        check(api.reseed(fresh, p['SoeConnectionId'], p['SoeSeed']) == p)
        h = api.handshakes(fresh)
        check(bytes.fromhex(h['RequestHex']) == struct.pack('>HIII', 1, 2, fresh['SoeConnectionId'], 496))
        check(bytes.fromhex(h['ResponseHex']) == struct.pack('>HII3BI', 2, fresh['SoeConnectionId'], fresh['SoeSeed'], 2, 1, 4, 496))
        for i in range(2):
            check(new.open_frame(bytes.fromhex(fresh['SoeCorrectionDatagrams'][i])) == old.open_frame(bytes.fromhex(p['SoeCorrectionDatagrams'][i])))
            check(new.open_frame(bytes.fromhex(fresh['SoeCorrectionAcks'][i])) == struct.pack('>HH', 0x15, i))
            reject(lambda i=i: new.open_frame(bytes.fromhex(p['SoeCorrectionAcks'][i])))
        check(new.open_frame(bytes.fromhex(fresh['SoeDisconnectHex'])) == struct.pack('>HIH', 5, fresh['SoeConnectionId'], 6))
    for bad in (-1, 0x100000000, True, 1., '1', None):
        reject(lambda bad=bad: api.reseed(p, bad, 0))
        reject(lambda bad=bad: api.reseed(p, 0, bad))
    mutations = [('Version', 12), ('Version', True), ('Policy', 'other'), ('SoeCrcWidth', 1),
                 ('SoeMaximumDatagram', 497), ('SoeSeed', True), ('OriginalSenderParity', True),
                 ('SoeUnexpectedHex', '00'), ('SoeRequestHex', '00'), ('SoeCorrectionAcks', ['00']),
                 ('SoeCorrectionDatagrams', ['00', '00']), ('MaximumUpdates', 6001), ('InteractiveReplay', 1)]
    for key, value in mutations:
        bad = copy.deepcopy(p); bad[key] = value
        reject(lambda bad=bad: api.reseed(bad, 1, 2))
    bad = copy.deepcopy(p); bad['SoeOuterAckHex'] = bad['SoeCorrectionAcks'][0]
    reject(lambda: api.reseed(bad, 1, 2))
    # Authored stationary nativeXYZ and identity quaternion, not observed output.
    # Extend only the explicit bounded settings for this independent cadence fixture.
    logs = []; expectations = []
    for i, fresh in enumerate(profiles):
        fresh['MaximumUpdates'] = fresh['PlaygroundSettings']['MaximumUpdates'] = 120
        n = 1 if i == 1 else 120
        oracle = api.expect_motion(fresh, [dict(Native=[-1.5, 2, 0], Quaternion=[0,0,0,1], Speed=0) for _ in range(n)])
        check([v['Sequence'] for v in oracle['Frames']] == list(range(len(oracle['Frames']))))
        if i != 1: check([v['AtStep'] for v in oracle['Frames']] == [9, 109])
        runtime = dict(Reason='Faulted' if i==1 else 'BudgetCompleted', MaximumUpdates=120,
                       CompletedUpdates=n, AttemptedUpdates=n+int(i==1), Stopped=i!=1, Faulted=i==1,
                       Mode='Idle', Speed=0, TargetActive=False)
        expectations.append(dict(Transport=oracle, Runtime=runtime))
        rows = [('soe_root_transport_ready', dict(api.handshakes(fresh), Established=True, Synthetic=True, SocketCreated=False, Sent=False, OriginalSenderParity=False))]
        rows += [('offline_root_cadence', r) for r in oracle['Cadence']]
        for frame in oracle['Frames']:
            rows.append(('point_outbound_root', dict(Hex=frame['ApplicationHex'], AtStep=frame['AtStep'], ReadAtStep=frame['AtStep'])))
            rows.append(('soe_root_datagram', dict({k:v for k,v in frame.items() if k!='ApplicationHex'}, ReadAtStep=frame['AtStep'], AckReleased=1, ReliableCopyAccepted=True, PendingMessages=0, RetainedPackets=0, SocketCreated=False, Sent=False, OriginalSenderParity=False)))
        rows += [('offline_root_completion_summary', oracle['Completion']), ('offline_completion_summary', runtime),
                 ('soe_root_transport_cleanup', dict(Closed=True, PendingMessages=0, RetainedPackets=0, SocketCreated=False))]
        logs.append('\n'.join(k+'='+json.dumps(v) for k,v in rows))
    check(api.verify_runs(logs, profiles, expectations)['Passed'])
    runtime_oracles = [e['Runtime'] for e in expectations]
    step_oracles = [[f['AtStep'] for f in e['Transport']['Frames']] for e in expectations]
    boundary = api.verify_transport_only(logs, profiles, runtime_oracles, step_oracles)
    check(boundary['Passed'] and boundary['NativeApplicationBorrowedSerializationInput'] and not boundary['ExpectedMovementGenerated'])
    wrongsteps = copy.deepcopy(step_oracles); wrongsteps[0][0] = 8
    reject(lambda: api.verify_transport_only(logs, profiles, runtime_oracles, wrongsteps))
    reject(lambda: api.verify_transport_only(logs, profiles, runtime_oracles, [[True], [], [9,109]]))
    wronglogs = list(logs); wronglogs[0] = wronglogs[0].replace('"AckReleased": 1', '"AckReleased": 0', 1)
    reject(lambda: api.verify_transport_only(wronglogs, profiles, runtime_oracles, step_oracles))
    reject(lambda: api.verify_runs(logs, [profiles[0]]*3, expectations))
    for before, after in [('"Sequence": 0', '"Sequence": 1'), ('"AckReleased": 1', '"AckReleased": 0'),
                          ('"CleanDrain": true', '"CleanDrain": false'), ('"QueueCount": 1', '"QueueCount": 0'),
                          ('"Samples": 1', '"Samples": 2'), ('"SocketCreated": false', '"SocketCreated": true')]:
        changed = list(logs); changed[0] = changed[0].replace(before, after, 1)
        reject(lambda changed=changed: api.verify_runs(changed, profiles, expectations))
    badlogs = list(logs); badlogs[1] = badlogs[1].replace('"AttemptedUpdates": 2', '"AttemptedUpdates": 1')
    reject(lambda: api.verify_runs(badlogs, profiles, expectations))
    (out/'profiles.json').write_text(json.dumps(profiles, indent=2)+'\n')
    (out/'expectations-before-native.json').write_text(json.dumps(expectations, indent=2)+'\n')
    receipt = dict(Passed=True, Checks=checks, Rejections=negatives, AuthoredLogFixtures=True,
                   NativeExecuted=False, SourceSha256=hashlib.sha256(args.profile.read_bytes()).hexdigest(),
                   Verifier=api.verify_runs(logs, profiles, expectations), TransportBoundary=boundary)
    (out/'receipt.json').write_text(json.dumps(receipt, indent=2)+'\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
