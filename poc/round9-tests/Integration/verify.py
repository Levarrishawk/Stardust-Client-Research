import json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_baselines import fixture
from verify_creature_view3_scalar_deltas import parse,KEY
v=json.loads(Path(sys.argv[3]).read_text(encoding='utf-8-sig'))
b=Path(sys.argv[1]).read_bytes();d=Path(sys.argv[2]).read_bytes()
assert b==fixture()==bytes.fromhex(v['BaselineHex'])
e,updates=parse(d)
expected=dict(Posture=255,Rank=254,LinkId=0xfe02030405060708,HeightBits=0xbfe00000,Shock=0xf1223344,States=0xf0e1d2c3b4a59687)
assert v['Initial']==expected
for u in updates:expected[KEY[u['Index']]]=u['RawBits']
assert v['Applied']==expected and v['ObjectId']==e['ObjectId']
tokens=[v[k] for k in ('First','Replacement','Recreated')]
assert len({x['RegistryId'] for x in tokens})==1
assert all(x['SceneGeneration']==1 and x['ObjectId']==v['ObjectId'] for x in tokens)
assert tokens[0]['CreationRevision']<tokens[1]['CreationRevision']<tokens[2]['CreationRevision']
assert all(v[k] is True for k in ('ExactRevisionExpired','StableContainmentAndFinalize','DetachedChildRetained','NoInheritedSeed','ResetReleased'))
print('PASS: independently audited baseline/scalar bits and lifecycle incarnation receipts')
