"""Independent 496-byte negotiated offline mixed-state peer; synthetic CREO3/lifecycle."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_creature_view3_baselines import fixture as original_fixture
from verify_creature_view3_mixed_deltas import packet,scalar,wound,golden
def fixture(count=0):
 b=bytearray(original_fixture(count));b[-16:-12]=struct.pack("<I",0);return bytes(b)
from verify_scene_creature_resources import checksum
OID=0xF122334455667788
start=struct.pack('<HIBQ',9,0x3AE6DFAE,165,OID)+struct.pack('<H',3)+b'A/B'+struct.pack('<3f',1.5,-2.25,3.75)+struct.pack('<H',8)+b'template'+struct.pack('<Q',0xFE02030405060708)
create=struct.pack('<HIQ7fIB',5,0xFE89DDEA,OID,-0.0,.25,-.5,2,123.5,-45.25,1e-40,checksum('object/synthetic/fixture.iff'),255)
fields=b''.join(struct.pack('<H'+fmt,index,value) for index,fmt,value in [(16,'Q',0xFEDCBA9876543210),(12,'B',254),(14,'f',-1.75),(11,'B',255),(15,'I',0xF1020304),(13,'Q',0xFE02030405060708),(11,'B',7)])+struct.pack('<HI',14,0x80000000)
delta=struct.pack('<HIQIBIH',5,0x12862153,OID,0x4352454F,3,len(fields)+2,65535)+fields
apps={'start':start,'create':create,'baseline':fixture(),'delta':delta,'end':struct.pack('<HIQ',2,0x2C436037,OID),'contain':struct.pack('<HIQQI',4,0x56CBDE9E,OID,0,0xFFFFFFFE),'serverReady':bytes.fromhex('0100221CFD43'),'baselineReset':fixture(65535),'deltaReset':delta,'destroy':struct.pack('<HIQB',3,0x4D45D504,OID,255)}
apps['delta']=packet(golden(True),65535)
apps['deltaReset']=apps['delta']
apps.update(strict=packet(golden(False)),grow=packet(scalar(11,8)+wound(1,7,[(2,6,[9])])+scalar(12,7)),noop=packet(wound(2,9,[(0,65535,[]),(2,6,[9])])),clear=packet(wound(1,8,[(4,0,[])])),late=packet(scalar(11,8)+wound(1,1,[(2,0,[7])])+wound(1,2,[(1,65535,[9])])),truncated=packet(scalar(11,8)+struct.pack('<H',13)+b'\x01'))
result={k:v.hex().upper() for k,v in apps.items()}
result['response']=struct.pack('>HII3BI',2,0x12345678,0xA1B2C3D4,2,1,4,496).hex().upper()
for sequence,(name,value) in enumerate(apps.items()):
 wire=frame(struct.pack('>HH',9,sequence)+value,True);assert len(wire)<=496;result['peer_'+name]=wire.hex().upper()
for name,sequence in [('ackSelect',0),('ackReady',1)]:result[name]=frame(struct.pack('>HH',21,sequence),True).hex().upper()
result['expectedSelect']=frame(struct.pack('>HH',9,0)+struct.pack('<HIQ',2,0xB5098D76,OID),True).hex().upper();result['expectedReady']=frame(struct.pack('>HH',9,1)+apps['serverReady'],True).hex().upper()
Path(sys.argv[1]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS: independent full baseline/mixed delta/lifecycle peer, declared496-byte negotiation and every datagram within bound')
