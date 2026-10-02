"""Independent synthetic 496-byte SOE peer and complete CREO3 fields; no capture bytes."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
OID=0xF122334455667788
def envelope(op,count,fields):return struct.pack('<HIQIBIH',5,op,OID,0x4352454F,3,len(fields)+2,count)+fields
def baseline(visibility):
    return envelope(0x68A75F0C,18,struct.pack('<fHIHIIHIIIIIIBBBQfIQII',1,0,0,0,0,1,0,0,7,0,0,0,100,visibility,4,9,0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210,0,9))
def height(value):return envelope(0x12862153,1,struct.pack('<Hf',14,value))
start=struct.pack('<HIBQ',9,0x3AE6DFAE,165,OID)+struct.pack('<H',3)+b'A/B'+struct.pack('<3f',1.5,-2.25,3.75)+struct.pack('<H',8)+b'template'+struct.pack('<Q',0xFE02030405060708)
create=struct.pack('<HIQ7fIB',5,0xFE89DDEA,OID,.2,-0.,.3,1.7,-2,7,4,2937962913,255)
delta=envelope(0x12862153,2,struct.pack('<HBHQ',11,7,16,0xFEDCBA9876543210))
apps={'start':start,'create':create,'baseline':baseline(1),'delta':delta,'end':struct.pack('<HIQ',2,0x2C436037,OID),'contain':struct.pack('<HIQQI',4,0x56CBDE9E,OID,0,0xFFFFFFFE),'serverReady':bytes.fromhex('0100221CFD43'),'height':height(1.5),'hidden':baseline(0),'shown':baseline(255)}
result={k:v.hex().upper() for k,v in apps.items()}
result['response']=struct.pack('>HII3BI',2,0x12345678,0xA1B2C3D4,2,1,4,496).hex().upper()
for sequence,(name,value) in enumerate(apps.items()):
    wire=frame(struct.pack('>HH',9,sequence)+value,True);assert len(wire)<=496;result['peer_'+name]=wire.hex().upper()
for name,sequence in [('ackSelect',0),('ackReady',1)]:result[name]=frame(struct.pack('>HH',21,sequence),True).hex().upper()
result['expectedSelect']=frame(struct.pack('>HH',9,0)+struct.pack('<HIQ',2,0xB5098D76,OID),True).hex().upper()
result['expectedReady']=frame(struct.pack('>HH',9,1)+apps['serverReady'],True).hex().upper()
result['destroy']=struct.pack('<HIQB',3,0x4D45D504,OID,255).hex().upper()
for name,value in [('zero',0),('negativeZero',-0.),('negative',-1),('small',.1),('large',2.1),('minimum',.25),('maximum',2)]:result[name]=height(value).hex().upper()
result['bad']=envelope(0x12862153,2,struct.pack('<HfH',14,1.5,65535)).hex().upper()
Path(sys.argv[1]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS: independent full baseline/scalar/lifecycle peer, negotiation496 and all frames within bound')
