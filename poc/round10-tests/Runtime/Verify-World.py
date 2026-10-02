"""Independent declared496-byte wire/order and typed-world scalar/baseline output audit."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_creature_view3_baselines import fixture
from verify_creature_view3_scalar_deltas import parse,KEY
from verify_zone_scene_protocol import parse as scene
from verify_scene_object_lifecycle import decode
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));actual=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
def unframe(wire):
 assert 5<=len(wire)<=496
 data=bytearray(wire[:-2]);chain=0xa1b2c3d4;cursor=2
 while cursor+4<=len(data):
  cipher,=struct.unpack_from('<I',data,cursor);struct.pack_into('<I',data,cursor,cipher^chain);chain=cipher;cursor+=4
 while cursor<len(data):data[cursor]^=chain&255;cursor+=1
 assert data[-1]==0 and frame(bytes(data[:-1]),True)==wire
 return bytes(data[:-1])
assert actual['NegotiatedBytes']==496
assert bytes.fromhex(actual['SessionRequest'])==struct.pack('>HIII',1,2,0x12345678,496)
assert bytes.fromhex(peer['response'])==struct.pack('>HII3BI',2,0x12345678,0xa1b2c3d4,2,1,4,496)
assert actual['SelectDatagram']==peer['expectedSelect'];selected=unframe(bytes.fromhex(actual['SelectDatagram']));assert selected[:4]==struct.pack('>HH',9,0);scene(selected[4:],False)
assert actual['ReadyDatagram']==actual['Retransmission']==peer['expectedReady']
assert unframe(bytes.fromhex(actual['ReadyDatagram']))==struct.pack('>HH',9,1)+bytes.fromhex('0100221CFD43')
names=['start','create','baseline','delta','end','contain','serverReady','baselineReset','deltaReset','destroy']
assert actual['DeliveredApplications']==[peer[name] for name in names]
for seq,name in enumerate(names):assert unframe(bytes.fromhex(peer['peer_'+name]))==struct.pack('>HH',9,seq)+bytes.fromhex(peer[name])
scene(bytes.fromhex(peer['start']),True)
for name,kind in [('create','create'),('end','end'),('contain','containment'),('destroy','destroy')]:decode(kind,bytes.fromhex(peer[name]))
assert bytes.fromhex(actual['BaselineHex'])==bytes.fromhex(peer['baseline'])==fixture()
assert bytes.fromhex(peer['baselineReset'])==fixture(65535)
e,updates=parse(bytes.fromhex(peer['delta']));initial=dict(Posture=255,Rank=254,LinkId=0xfe02030405060708,HeightBits=0xbfe00000,Shock=0xf1223344,States=0xf0e1d2c3b4a59687)
state=initial.copy()
for update in updates:state[KEY[update['Index']]]=update['RawBits']
assert [u['Index'] for u in updates]==[16,12,14,11,15,13,11,14]
assert actual['First']==actual['AfterResetDelta']==actual['AfterLongStream']==state and actual['BaselineReset']==initial
assert state['HeightBits']==0x80000000 and actual['BaselineDecodedBytes']==119
assert [actual[key] for key in ['FirstRevision','ResetRevision','ResetDeltaRevision','StreamRevision']]==[2,3,4,2004]
assert actual['StreamPairs']==1000 and actual['OpaqueHistoryCount']==0 and actual['ResourceLoadsBeforeLifecycleChange']==1
assert actual['FinalReplicationBytes']==0 and actual['FinalState']=='Failed'
print('PASS: independent496-byte handshake/peer/retry/order, full baseline and six-scalar/reset bits, bounded stream and cleanup outputs')
