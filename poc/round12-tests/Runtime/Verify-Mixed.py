"""Independent496-byte peer and deterministic bounded coalescing/state model."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_creature_view3_baselines import fixture
from verify_creature_view3_mixed_deltas import parse,apply
from verify_creature_wounds_legacy_policy import emulate
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
names=['start','create','baseline','delta','end','contain']
assert actual['DeliveredApplications']==[peer[n] for n in names]
for seq,n in enumerate(names):assert unframe(bytes.fromhex(peer['peer_'+n]))==struct.pack('>HH',9,seq)+bytes.fromhex(peer[n])
raw=bytearray(fixture());raw[-16:-12]=struct.pack('<I',0);assert bytes.fromhex(peer['baseline'])==raw
scene(bytes.fromhex(peer['start']),True)
for name,kind in [('create','create'),('end','end'),('contain','containment')]:decode(kind,bytes.fromhex(peer[name]))
state=apply(parse(bytes.fromhex(peer['delta']),True),True)
for name,revision in [('First',2),('Grown',3),('Noop',4)]:
 if name!='First':
  for index,value in parse(bytes.fromhex(peer['grow' if name=='Grown' else 'noop']),True):
   if index!=17:
    from verify_creature_view3_mixed_deltas import WIDTH
    state[WIDTH[index][1]]=value
   else:
    n,c,ops=value;r=emulate(state['WoundsCounter'],state['Wounds'],[(n,c,ops)],16);state['WoundsCounter']=r['Counter'];state['Wounds']=r['Values']
 assert actual[name]==dict(state,Revision=revision),(name,actual[name],state)
assert actual['FirstMutation']==5 and actual['GrownMutation']==6 and actual['NoopMutation']==7
assert actual['ResourceLoads']==1 and actual['FinalBytes']==0
print('PASS: independent496-byte framing/order and ordered mixed scalar/wounds state, assign cursor, zero-fill, no-op counter and single-mutation model')
