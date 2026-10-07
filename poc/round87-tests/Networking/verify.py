"""Verify complete movement prefix and honest final disconnect attempt outcomes."""
import argparse
import json
import runpy
import sys
from pathlib import Path
import produce


def verify(t, result):
    mode = result.get('Mode')
    if mode not in ('success', 'send-error', 'send-timeout'):
        raise ValueError('Explicit disconnect Mode required')
    for key in ('DisconnectRequested', 'DisconnectSubmitted', 'DisconnectSendCompleted', 'TypedFailureObserved', 'FailureObserved'):
        if type(result.get(key)) is not bool:
            raise ValueError('Boolean observation required: '+key)
    if result['DisconnectRequested'] is not True:
        raise ValueError('Disconnect request required after movement prefix')
    submitted = result['DisconnectSubmitted']
    if submitted:
        if result.get('DisconnectWire') != t['ExpectedDisconnectWire']:
            raise ValueError('Exact actual disconnect attempt wire mismatch')
    elif result.get('DisconnectWire') not in (None, ''):
        raise ValueError('Unsubmitted disconnect cannot claim an attempted wire')
    if mode == 'success':
        if not submitted or not result['DisconnectSendCompleted'] or result['FailureObserved'] or result['TypedFailureObserved']:
            raise ValueError('Successful final send must be submitted and completed without failure')
    elif result['DisconnectSendCompleted'] or not result['FailureObserved'] or not result['TypedFailureObserved']:
        raise ValueError('Failed/timed-out send must report typed failure without completed send')
    # Failure here is final disconnect I/O, not a failed movement prefix. Preserve every
    # other86 observation; mask only its success failure flag in a detached comparison.
    prior = dict(result, FailureObserved=False)
    previous_producer = sys.modules.get('produce')
    sys.modules['produce'] = produce.load(86, 'produce')
    try:
        prior_verifier = produce.load(86, 'verify')
    finally:
        if previous_producer is None:
            del sys.modules['produce']
        else:
            sys.modules['produce'] = previous_producer
    prior_verifier.verify(dict(t, Variant='normal'), prior)
    return dict(Passed=True, Synthetic=True, LiveInterop=False, Mode=mode, ExactMovementWires=3,
                DisconnectRequested=True, DisconnectSubmitted=submitted, DisconnectSendCompleted=result['DisconnectSendCompleted'], Clean=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--transcript', type=Path, required=True)
    parser.add_argument('--result', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    a = parser.parse_args()
    receipt = verify(json.loads(a.transcript.read_text(encoding='utf-8')), json.loads(a.result.read_text(encoding='utf-8')))
    out = runpy.run_path(str(produce.ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.output, produce.ROOT)
    out.write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    print('PASS independent movement-prefix and disconnect outcome comparison')


if __name__ == '__main__':
    main()
