"""Compare native observations against independently authored primitive expectations."""
import argparse
import json
import runpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def verify(transcript, result):
    for name in ('Auth', 'Select'):
        if result.get(name+'Wire') != transcript['Expected'+name+'Wire']:
            raise ValueError('Exact protected '+name+' mismatch')
    if transcript['Variant'] != 'normal':
        if result.get('FailureObserved') is not True or result.get('ReadySubmissions') != 0 or result.get('PlayerPublished') is not False or result.get('ReadyWire') not in (None, ''):
            raise ValueError('Negative ingress must fail without ready or player publication')
    else:
        if result.get('ReadyWire') != transcript['ExpectedReadyWire']:
            raise ValueError('Exact protected Ready mismatch')
        if result.get('SelectedModels') != transcript['ExpectedSelectedModels']:
            raise ValueError('Independent selected full4/6 model mismatch')
        if result.get('DeferredHex') != transcript['ExpectedDeferred']:
            raise ValueError('Deferred identity/order/count mismatch')
    return dict(Passed=True, AuthoredServerShaped=True, LiveInterop=False, Variant=transcript['Variant'], DeferredCount=len(transcript['ExpectedDeferred']))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--transcript', type=Path, required=True)
    parser.add_argument('--result', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    a = parser.parse_args()
    receipt = verify(json.loads(a.transcript.read_text(encoding='utf-8')), json.loads(a.result.read_text(encoding='utf-8')))
    out = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.output, ROOT)
    out.write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    print('PASS independent Round85 observation comparison')


if __name__ == '__main__':
    main()
