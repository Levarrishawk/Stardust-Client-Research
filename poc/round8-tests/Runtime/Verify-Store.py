"""Independent fixture/state-bit audit after actual native lifecycle and explicit reseed."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_scalar_deltas import parse,KEY
from verify_scene_object_lifecycle import decode
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
actual=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
data=bytes.fromhex(peer['delta']);envelope,updates=parse(data)
assert actual['EncodedDelta']==peer['delta'] and actual['ObjectId']==envelope['ObjectId']
assert [u['Index'] for u in updates]==[16,12,14,11,15,13,11,14] and envelope['DeclaredCount']==65535
initial=dict(Posture=1,Rank=2,LinkId=3,HeightBits=struct.unpack('<I',struct.pack('<f',4.5))[0],Shock=5,States=6)
state=initial.copy()
for update in updates:state[KEY[update['Index']]]=update['RawBits']
assert actual['Initial']==initial and actual['Applied']==actual['FinalizedApplied']==state
assert state['HeightBits']==0x80000000 and state['Shock']==0xf1020304
create=decode('create',bytes.fromhex(peer['create']))
assert actual['SourcePlacementX']==create['PositionX']==123.5
for name,kind in [('end','end'),('contain','containment'),('destroy','destroy')]:assert decode(kind,bytes.fromhex(peer[name]))['ObjectId']==actual['ObjectId']
seed=actual['SeedHandle'];final=actual['FinalizedHandle']
assert seed==actual['AppliedHandle'] and seed['ObjectId']==actual['ObjectId'] and seed['SceneGeneration']==7
assert seed['RegistryId']==final['RegistryId'] and final['Revision']>seed['Revision'] and final['SceneGeneration']==7
assert actual['SeedRevision']==1 and actual['AppliedRevision']==2 and actual['FinalizedAppliedRevision']>2
assert actual['FinalCount']==0 and actual['LifecycleRetirements']==['contain','end','replace','destroy','reset','registry-close','store-close']
print('PASS: independent actual lifecycle bytes, exact handle/reseed export, six ordered raw bits and separate placement')
