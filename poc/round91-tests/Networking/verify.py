"""Compare native admission outcomes and protected handoff against independent fixtures."""
import argparse
import json
import runpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def verify(fixture, result):
    assert result['Passed'] and result['Synthetic'] and not result['SocketCreated'] and not result['DnsResolved']
    assert len(result['Rows']) == len(fixture['Cases']) == 29
    assert result['Checks'] >= 100
    good = 0
    for case, row in zip(fixture['Cases'], result['Rows']):
        assert row['Name'] == case['Name'] and row['Accepted'] == case['ExpectedAccepted']
        if row['Sources'] is not None:
            assert row['Sources'] == fixture['ExpectedSources']
        if row['Accepted']:
            good += 1
            assert row['ZoneFactories'] == row['LoginFactories'] == 1
            assert row['LoginDisposals'] == row['ZoneDisposals'] == 1
            assert row['PreflightObserved'] and row['FailureType'] is None
            # There may be zone control acknowledgements after application frames.
            wire = row['ZoneWire']
            assert wire[0] == fixture['ExpectedZoneRequest']
            assert wire.count(fixture['ExpectedAuthWire']) == wire.count(fixture['ExpectedSelectWire']) == 1
        else:
            assert row['ZoneFactories'] == row['ZoneDisposals'] == 0 and row['ZoneWire'] == []
            assert row['FailureType'] is not None
            if row['Stage'] == 'preflight':
                assert row['Preserved'] and row['LoginFactories'] == row['LoginDisposals'] == 1
            elif row['Stage'] in ('mapping', 'startup'):
                assert row['LoginFactories'] == row['LoginDisposals'] == 0
            else:
                assert row['Stage'] == 'login' and row['LoginFactories'] == row['LoginDisposals'] == 1
    assert good == 4
    return dict(Passed=True, Cases=29, Accepted=good, Rejected=25, ExactProtectedHandoffs=good,
                RejectedZoneFactories=0, Synthetic=True, SocketCreated=False, DnsResolved=False)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--fixtures', type=Path, required=True)
    parser.add_argument('--result', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    result = verify(json.loads(args.fixtures.read_text()), json.loads(args.result.read_text(encoding='utf-8-sig').strip()))
    out = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](args.out, ROOT)
    out.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
