"""Extend the independent complete TANO stage fixture with an exact no-op delta."""
import json,struct,sys
from pathlib import Path
if len(sys.argv)!=4:raise SystemExit('round26-stages-json main-mixed-peer-json private-output-json')
data=json.loads(Path(sys.argv[1]).read_text());mixed=json.loads(Path(sys.argv[2]).read_text())
oid=struct.unpack_from('<Q',bytes.fromhex(mixed['terminal_create']),6)[0]
assert data['Version']==1 and data['Synthetic']
original=data['ObjectId']
def retarget(value):
 wire=bytearray.fromhex(value);assert struct.unpack_from('<Q',wire,6)[0]==original
 struct.pack_into('<Q',wire,6,oid);return wire.hex().upper()
for key in ('Baseline','LateUnsupported','IntermediateOverBudget'):data[key]=retarget(data[key])
for row in data['Updates']:row['Hex']=retarget(row['Hex'])
data['ObjectId']=oid;data['Create']=mixed['terminal_create'];data['RetargetedSyntheticObjectId']=True
fields=struct.pack('<HB',10,1)
data['Noop']=(struct.pack('<HIQIBIH',5,0x12862153,oid,0x54414e4f,3,len(fields)+2,65535)+fields).hex().upper()
for value in [data['Baseline'],data['Noop'],*[row['Hex'] for row in data['Updates']]]:
 wire=bytes.fromhex(value);assert struct.unpack_from('<I',wire,19)[0]==len(wire)-23
output=Path(sys.argv[3]);assert 'local-private' in output.resolve().parts
output.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
print('PASS: synthetic TANO envelopes explicitly retargeted to main terminal create; exact field bytes retained; no-op raw visibility1')
