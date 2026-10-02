"""Independent496-byte peer and deterministic bounded coalescing/state model."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_creature_view3_baselines import fixture
from verify_creature_view3_mixed_deltas import parse as mixed_parse,apply as mixed_apply
from verify_creature_view3_components_deltas import parse as component_parse,apply as component_apply,cases,field,message
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
names=['start','create','baseline','components','delta','end','contain']
assert actual['DeliveredApplications']==[peer[n] for n in names]
for seq,n in enumerate(names):assert unframe(bytes.fromhex(peer['peer_'+n]))==struct.pack('>HH',9,seq)+bytes.fromhex(peer[n])
seed=bytearray(fixture());seed[-16:-12]=struct.pack('<I',0);seed[73:77]=struct.pack('<I',0);seed[85:89]=struct.pack('<i',-1)
assert bytes.fromhex(peer['baseline'])==seed
scene(bytes.fromhex(peer['start']),True)
for name,kind in [('create','create'),('end','end'),('contain','containment')]:decode(kind,bytes.fromhex(peer[name]))
components=component_apply(0,[-1,-2147483648,-1],component_parse(bytes.fromhex(peer['components'])),8)
state=mixed_apply(mixed_parse(bytes.fromhex(peer['delta']),True),True);state.pop('ObjectId')
def expected(revision):return dict(state,Revision=revision,ComponentCounter=components['Counter'],Components=components['Keys'])
assert actual['First']==expected(3)
state['Posture']=9;assert actual['Scalar']==expected(4)
components=component_apply(components['Counter'],components['Keys'],component_parse(bytes.fromhex(peer['componentNoop'])),8)
assert actual['Noop']==expected(5)
components=component_apply(components['Counter'],components['Keys'],component_parse(bytes.fromhex(peer['future'])),8)
assert actual['Future']==expected(6)
for _ in range(1000):
 components=component_apply(0,[-1,-2147483648,-1],component_parse(bytes.fromhex(peer['components'])),8)
 state=mixed_apply(mixed_parse(bytes.fromhex(peer['delta']),True),True);state.pop('ObjectId')
assert actual['Stream']==expected(3006)
from verify_creature_view3_mixed_deltas import SCALARS
state=dict(SCALARS,WoundsCounter=0,Wounds=[-2,2147483647,-2]);components=dict(Counter=0,Keys=[-2147483648,-1])
assert actual['Reseed']==expected(3007)
assert actual['FirstMutation']==6 and actual['NoopMutation']==8 and actual['StreamMutation']==3009
assert actual['ResourceLoads']==1 and actual['FinalBytes']==0
specs=json.loads(peer['componentCases']);expected_cases=[]
for spec,(name,counter,raw,fields,cap,keycap) in zip(specs,cases()):
 wire=message(fields,65535 if name=='count65535' else 0);assert spec['Wire']==wire.hex().upper() and spec['Name']==name
 b=bytearray(seed);b[69:89]=struct.pack('<II',len(raw),counter)+struct.pack('<'+'i'*len(raw),*raw);struct.pack_into('<I',b,19,len(b)-23)
 assert bytes.fromhex(spec['Baseline'])==b and spec['Elements']==cap and spec['KeyBudget']==keycap
 decoded=False;accepted=False;result=dict(Counter=counter,Keys=sorted(set(raw)))
 try:
  updates=component_parse(wire,keycap);decoded=True;result=component_apply(counter,raw,updates,cap);accepted=True
 except ValueError:pass
 expected_cases.append(dict(Name=name,Decoded=decoded,Accepted=accepted,**result,Revision=2 if accepted else 1))
assert len(specs)==len(cases())==69 and actual['Cases']==expected_cases
print('PASS: independent496-byte framing/order,69 recovered set arithmetic/store cases and sequential component/mixed/scalar state/revision/observer model')
