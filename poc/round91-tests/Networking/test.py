"""Negative tests for independent receipt verification; use a successful native receipt."""
import argparse
import copy
import importlib.util
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--fixtures', type=Path, required=True)
    parser.add_argument('--result', type=Path, required=True)
    args = parser.parse_args()
    spec = importlib.util.spec_from_file_location('verify91', Path(__file__).with_name('verify.py'))
    verifier = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(verifier)
    fixture = json.loads(args.fixtures.read_text())
    result = json.loads(args.result.read_text(encoding='utf-8-sig'))
    verifier.verify(fixture, result)
    bad = []
    def add(change):
        altered = copy.deepcopy(result)
        change(altered)
        bad.append(altered)
    add(lambda r: r.update(SocketCreated=True))
    add(lambda r: r.update(DnsResolved=True))
    add(lambda r: r['Rows'].pop())
    add(lambda r: r['Rows'][0]['ZoneWire'].remove(fixture['ExpectedAuthWire']))
    add(lambda r: r['Rows'][0]['ZoneWire'].append(fixture['ExpectedSelectWire']))
    negative = next(n for n, r in enumerate(result['Rows']) if r['Stage'] == 'preflight' and not r['Accepted'])
    add(lambda r: r['Rows'][negative].update(ZoneFactories=1))
    add(lambda r: r['Rows'][negative].update(Preserved=False))
    for altered in bad:
        try:
            verifier.verify(fixture, altered)
        except AssertionError:
            continue
        raise AssertionError('Tampered native receipt was admitted')
    print(json.dumps(dict(Passed=True, NegativeReceiptCases=len(bad))))


if __name__ == '__main__':
    main()
