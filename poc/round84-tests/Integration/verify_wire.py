"""Compare captured synthetic protected outputs to independent pre-native vectors."""
import argparse
import json
from pathlib import Path


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--transcript', type=Path, required=True)
    p.add_argument('--captured', type=Path, required=True)
    a = p.parse_args()
    transcript = json.loads(a.transcript.read_text())
    captured = json.loads(a.captured.read_text())
    if set(captured) != {'AuthWire', 'SelectWire', 'ReadyWire'}:
        raise ValueError('Exact three protected exchanges required')
    for name in ('Auth', 'Select', 'Ready'):
        if captured[name + 'Wire'] != transcript['Expected' + name + 'Wire']:
            raise ValueError('Independent protected ' + name + ' mismatch')
    if transcript.get('AuthoredTypedCreo3') is not True or transcript.get('CompleteServerBaselineSet') is not False:
        raise ValueError('Expected bounded typed baseline policy')
    print(json.dumps(dict(Passed=True, ExactAuthSelectReady=3, AuthoredTypedCreo3=True,
                         CompleteServerBaselineSet=False, LiveCompatibility=False)))


if __name__ == '__main__':
    main()
