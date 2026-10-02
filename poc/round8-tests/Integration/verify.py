"""Combined scalar-store export audit using independent baseline/delta fixtures."""
import json
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'tools/python'))
from verify_creature_view3_baselines import fixture
from verify_creature_view3_scalar_deltas import parse, KEY

baseline, delta, output = [Path(p) for p in sys.argv[1:]]
actual = json.loads(output.read_text(encoding='utf-8-sig'))
assert baseline.read_bytes() == fixture(actual['DeclaredCount'])
envelope, updates = parse(delta.read_bytes())
assert actual['ObjectId'] == envelope['ObjectId'] == 0xF122334455667788
initial = dict(Posture=255, Rank=254, LinkId=0xFE02030405060708,
               HeightBits=0xBFE00000, Shock=0xF1223344, States=0xF0E1D2C3B4A59687)
applied = initial.copy()
for update in updates:
    applied[KEY[update['Index']]] = update['RawBits']
assert actual['Initial'] == actual['Reseeded'] == initial
assert actual['Applied'] == applied
assert actual['AtomicMalformedRejected'] and actual['OldHandleRetired'] and actual['ExplicitReseedRequired']
print('PASS: independent full-baseline seed and ordered raw-bit delta/store integration export')
