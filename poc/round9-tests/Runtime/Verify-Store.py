"""Independent complete baseline wire, ordered overlay bits and incarnation metadata audit."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_baselines import fixture
from verify_creature_view3_scalar_deltas import parse,KEY
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));actual=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
assert bytes.fromhex(actual['BaselineHex'])==bytes.fromhex(peer['baseline'])==fixture()
assert actual['DeltaHex']==peer['delta'];e,updates=parse(bytes.fromhex(peer['delta']))
initial=dict(Posture=255,Rank=254,LinkId=0xfe02030405060708,HeightBits=0xbfe00000,Shock=0xf1223344,States=0xf0e1d2c3b4a59687)
state=initial.copy()
for update in updates:state[KEY[update['Index']]]=update['RawBits']
assert [u['Index'] for u in updates]==[16,12,14,11,15,13,11,14]
assert actual['Initial']==actual['Reseeded']==initial and actual['Applied']==actual['AfterLifecycle']==state and state['HeightBits']==0x80000000
inc=actual['SeedIncarnation'];created=actual['CreatedHandle']
assert inc['RegistryId']==created['RegistryId'] and inc['ObjectId']==e['ObjectId'] and inc['SceneGeneration']==created['SceneGeneration']==17
assert inc['CreationRevision']==created['Revision']
for key in ('ContainedHandle','EndedHandle'):
 h=actual[key];assert h['RegistryId']==inc['RegistryId'] and h['ObjectId']==inc['ObjectId'] and h['SceneGeneration']==17 and h['Revision']>inc['CreationRevision']
assert actual['EndedHandle']['Revision']>actual['ContainedHandle']['Revision']
previous=inc['CreationRevision']
for key,generation in [('ReplacementIncarnation',17),('RecreatedIncarnation',17),('NextIncarnation',18)]:
 v=actual[key];assert v['RegistryId']==inc['RegistryId'] and v['ObjectId']==inc['ObjectId'] and v['SceneGeneration']==generation and v['CreationRevision']>previous;previous=v['CreationRevision']
assert [actual[k] for k in ('SeedRevision','AppliedRevision','AfterLifecycleRevision','ReseedRevision')]==[1,2,3,4]
assert actual['BaselineDecodedBytes']==119 and actual['FinalRetainedBytes']==actual['FinalCount']==0
print('PASS: complete unchanged baseline bytes, ordered six-scalar overlay/reseed raw bits and stable/retired incarnation metadata')
