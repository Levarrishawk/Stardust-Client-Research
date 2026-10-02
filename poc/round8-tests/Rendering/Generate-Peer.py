"""Independently assembled real-layout application/peer fixtures, with no network or captured packets."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_scene_creature_resources import checksum
OID=0xF122334455667788
start=struct.pack('<HIBQ',9,0x3AE6DFAE,165,OID)+struct.pack('<H',3)+b'A/B'+struct.pack('<3f',1.5,-2.25,3.75)+struct.pack('<H',8)+b'template'+struct.pack('<Q',0xFE02030405060708)
def create(crc,oid=OID):return struct.pack('<HIQ7fIB',5,0xFE89DDEA,oid,-0.0,.25,-.5,2,123.5,-45.25,1e-40,crc,255)
def envelope(op):return struct.pack('<HIQIBIH',5,op,OID,0x4352454F,3,5,99)+b'\x7f\x00\xff'
apps={'start':start,'create':create(2937962913),'baseline':envelope(0x68A75F0C),'delta':envelope(0x12862153),'end':struct.pack('<HIQ',2,0x2C436037,OID),'contain':struct.pack('<HIQQI',4,0x56CBDE9E,OID,0,0xFFFFFFFE),'serverReady':bytes.fromhex('0100221CFD43'),'destroy':struct.pack('<HIQB',3,0x4D45D504,OID,255)}
result={k:v.hex().upper() for k,v in apps.items()};result['syntheticCreate']=create(checksum('object/synthetic/fixture.iff')).hex().upper();result['syntheticParent']=create(checksum('object/synthetic/fixture.iff'),OID+1).hex().upper()
result['response']=struct.pack('>HII3BI',2,0x12345678,0xA1B2C3D4,2,1,4,128).hex().upper()
for sequence,(name,value) in enumerate(apps.items()):result['peer_'+name]=frame(struct.pack('>HH',9,sequence)+value,True).hex().upper()
for name,sequence in [('ackSelect',0),('ackReady',1)]:result[name]=frame(struct.pack('>HH',21,sequence),True).hex().upper()
result['expectedSelect']=frame(struct.pack('>HH',9,0)+struct.pack('<HIQ',2,0xB5098D76,OID),True).hex().upper();result['expectedReady']=frame(struct.pack('>HH',9,1)+apps['serverReady'],True).hex().upper()
result['peer_containAgain']=frame(struct.pack('>HH',9,7)+struct.pack('<HIQQI',4,0x56CBDE9E,OID,0,0xFFFFFFFD),True).hex().upper()
Path(sys.argv[1]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print('PASS: independent real-layout application/128-byte SOE peer fixtures generated')
