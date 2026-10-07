"""Independent comparisons of captured movement wire and actual motion/actor observations."""
import argparse
import json
import runpy
from pathlib import Path
import produce


def primitives(value):
    """JSON serializers print different decimal lengths; compare exact binary32 values."""
    if type(value) is float:
        return produce.f32(value)
    if isinstance(value, list):
        return [primitives(item) for item in value]
    if isinstance(value, dict):
        return {key: primitives(item) for key, item in value.items()}
    return value


def verify(t, result):
    prior = dict(t, Variant='normal')
    produce.load(85, 'verify').verify(prior, result)
    for key, expected in [('MovementWires', t['ExpectedMovementWires']), ('LocalMotions', t['ExpectedLocalMotions']),
                          ('ActorLocalPositions', t['ExpectedActorLocalPositions'])]:
        if primitives(result.get(key)) != primitives(expected):
            raise ValueError('Independent '+key+' mismatch')
    if result.get('Clean') is not True or type(result.get('ReadySubmissions')) is not int or result['ReadySubmissions'] != 1:
        raise ValueError('Clean drain and exactly one ready required')
    if t['Variant'] == 'malformed-correction':
        if result.get('FailureObserved') is not True or result.get('AppliedCorrections') != 0 or result.get('CounterRejectedCorrections') != 0:
            raise ValueError('Malformed first correction must fail before publishing corrections')
    else:
        for key, expected in [('CorrectionObservations', t['ExpectedCorrectionObservations']), ('ActorCorrectionPositions', t['ExpectedActorCorrectionPositions']),
                              ('AppliedCorrections', t['ExpectedAppliedCorrections']), ('CounterRejectedCorrections', t['ExpectedCounterRejectedCorrections']),
                              ('UnhandledMovementHex', t['ExpectedUnhandledMovement'])]:
            if primitives(result.get(key)) != primitives(expected):
                raise ValueError('Independent '+key+' mismatch')
        if result.get('FailureObserved') is not False:
            raise ValueError('Normal peer must finish without failure')
    return dict(Passed=True, Synthetic=True, LiveInterop=False, Variant=t['Variant'], ExactMovementWires=3,
                AppliedCorrections=0 if t['Variant']=='malformed-correction' else 2, ReadySubmissions=1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--transcript', type=Path, required=True)
    parser.add_argument('--result', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    a = parser.parse_args()
    receipt = verify(json.loads(a.transcript.read_text(encoding='utf-8')), json.loads(a.result.read_text(encoding='utf-8')))
    out = runpy.run_path(str(produce.ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.output, produce.ROOT)
    out.write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    print('PASS independent movement comparison')


if __name__ == '__main__':
    main()
