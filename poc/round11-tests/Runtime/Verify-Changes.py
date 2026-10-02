"""Independent496-byte peer and deterministic bounded coalescing/state model."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_creature_view3_baselines import fixture
from verify_creature_view3_scalar_deltas import parse,KEY
from verify_scene_object_lifecycle import decode
from verify_zone_scene_protocol import parse as scene
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));actual=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
def unframe(wire):
 assert 5<=len(wire)<=496
 data=bytearray(wire[:-2]);chain=0xa1b2c3d4;cursor=2
 while cursor+4<=len(data):
  cipher,=struct.unpack_from('<I',data,cursor);struct.pack_into('<I',data,cursor,cipher^chain);chain=cipher;cursor+=4
 while cursor<len(data):data[cursor]^=chain&255;cursor+=1
 assert data[-1]==0 and frame(bytes(data[:-1]),True)==wire
 return bytes(data[:-1])
assert actual['NegotiatedBytes']==496 and bytes.fromhex(actual['SessionRequest'])==struct.pack('>HIII',1,2,0x12345678,496)
assert bytes.fromhex(peer['response'])==struct.pack('>HII3BI',2,0x12345678,0xa1b2c3d4,2,1,4,496)
assert actual['SelectDatagram']==peer['expectedSelect'] and actual['ReadyDatagram']==peer['expectedReady']
selected=unframe(bytes.fromhex(actual['SelectDatagram']));assert selected[:4]==struct.pack('>HH',9,0);scene(selected[4:],False)
assert unframe(bytes.fromhex(actual['ReadyDatagram']))==struct.pack('>HH',9,1)+bytes.fromhex('0100221CFD43')
names=['start','create','baseline','delta','end','contain','serverReady','baselineReset','deltaReset']
assert actual['DeliveredApplications']==[peer[name] for name in names]
for sequence,name in enumerate(names):assert unframe(bytes.fromhex(peer['peer_'+name]))==struct.pack('>HH',9,sequence)+bytes.fromhex(peer[name])
assert bytes.fromhex(peer['baseline'])==fixture() and bytes.fromhex(peer['baselineReset'])==fixture(65535)
scene(bytes.fromhex(peer['start']),True)
for name,kind in [('create','create'),('end','end'),('contain','containment')]:decode(kind,bytes.fromhex(peer[name]))
e,updates=parse(bytes.fromhex(peer['delta']));oid=e['ObjectId']
initial=dict(Posture=255,Rank=254,LinkId=0xfe02030405060708,HeightBits=0xbfe00000,Shock=0xf1223344,States=0xf0e1d2c3b4a59687)
owner_guid=actual['First']['Entries'][0]['Incarnation']['RegistryId']
objects={};pending={};world_revision=0;registry_revision=0
def mark(ids,kind):
 global world_revision
 world_revision+=1
 for key in ids:
  if key in pending:pending[key][0]|=kind
  else:pending[key]=[kind,world_revision]
def create(key):
 global registry_revision
 registry_revision+=1;objects[key]=dict(CreationRevision=registry_revision,Container=0,Phase='PendingBaselines',Scalars=None);mark([key],1)
def baseline(key):objects[key]['Scalars']=initial.copy();mark([key],2)
def delta(key):
 for u in updates:objects[key]['Scalars'][KEY[u['Index']]]=u['RawBits']
 mark([key],2)
def end(key):
 global registry_revision
 registry_revision+=1;objects[key]['Phase']='Finalized';mark([key],1)
def contain(key,parent):
 global registry_revision
 registry_revision+=1;objects[key]['Container']=parent;mark([key],1)
def destroy(key):
 global registry_revision
 ids=[key]
 for child,obj in objects.items():
  if obj['Container']==key:registry_revision+=1;obj['Container']=0;ids.append(child)
 objects.pop(key,None);mark(ids,1)
def check_batch(name):
 b=actual[name];assert b['Generation']==1 and b['MutationRevision']==world_revision
 ids=sorted(pending,key=lambda key:(pending[key][1],key));assert [row['ObjectId'] for row in b['Entries']]==ids
 for row,key in zip(b['Entries'],ids):
  assert row['Kind']==pending[key][0] and row['Present']==(key in objects)
  if key in objects:
   obj=objects[key];inc=row['Incarnation'];assert inc['RegistryId']==owner_guid and inc['ObjectId']==key and inc['CreationRevision']==obj['CreationRevision'] and inc['SceneGeneration']==1
   assert row['Container']==obj['Container'] and row['Phase']==obj['Phase'] and row['Scalars']==obj['Scalars']
  else:assert row['Incarnation'] is None and row['Container'] is None and row['Scalars'] is None
create(oid);baseline(oid);delta(oid);end(oid);contain(oid,0);check_batch('First');pending.clear()
baseline(oid);check_batch('Reset');delta(oid)
for _ in range(1000):baseline(oid);delta(oid)
check_batch('Stream');pending.clear()
create(oid+1);baseline(oid+1);delta(oid);destroy(oid+1);check_batch('Removed')
create(oid+1);baseline(oid+1);check_batch('Recreated');pending.clear()
contain(oid,oid+1);pending.clear();destroy(oid+1);check_batch('Detached')
assert actual['StreamPairs']==1000 and actual['FinalDrainEmpty'] and actual['FinalReplicationBytes']==0 and actual['ResourceLoadsBeforeLifecycle']==1
print('PASS: independent496-byte peer/order and deterministic coalesced batch/order/replacement/detachment/state model')
