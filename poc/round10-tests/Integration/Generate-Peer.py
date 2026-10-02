"""Independent synthetic typed-world peer; original create CRC metadata only, no captured bytes."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_creature_view3_baselines import fixture
from verify_creature_view3_scalar_deltas import wire,FIELDS
OID=0xF122334455667788
start=struct.pack('<HIBQ',9,0x3AE6DFAE,165,OID)+struct.pack('<H',3)+b'A/B'+struct.pack('<3f',1.5,-2.25,3.75)+struct.pack('<H',8)+b'template'+struct.pack('<Q',0xFE02030405060708)
apps={'start':start,'create':struct.pack('<HIQ7fIB',5,0xFE89DDEA,OID,-0.,.25,-.5,2,123.5,-45.25,1e-40,2937962913,255),'baseline':fixture(),'delta':wire(FIELDS,65535),'end':struct.pack('<HIQ',2,0x2C436037,OID),'contain':struct.pack('<HIQQI',4,0x56CBDE9E,OID,0,0xFFFFFFFE),'serverReady':bytes.fromhex('0100221CFD43')}
result={k:v.hex().upper() for k,v in apps.items()}
result['response']=struct.pack('>HII3BI',2,0x12345678,0xA1B2C3D4,2,1,4,496).hex().upper()
for i,(name,app) in enumerate(apps.items()):
 datagram=frame(struct.pack('>HH',9,i)+app,True);assert len(datagram)<=496;result['peer_'+name]=datagram.hex().upper()
Path(sys.argv[1]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS: independent full baseline/scalar peer frames, declared496-byte negotiation')
