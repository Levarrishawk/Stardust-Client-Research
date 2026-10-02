"""Independent synthetic lifecycle and ordered CREO3 scalar fixtures; no captured bytes."""
import json,struct,sys
from pathlib import Path
OID=0xF122334455667788
fields=b''.join(struct.pack('<H'+fmt,index,value) for index,fmt,value in [(16,'Q',0xFEDCBA9876543210),(12,'B',254),(14,'f',-1.75),(11,'B',255),(15,'I',0xF1020304),(13,'Q',0xFE02030405060708),(11,'B',7)])+struct.pack('<HI',14,0x80000000)
def delta(oid,count=65535):return struct.pack('<HIQIBIH',5,0x12862153,oid,0x4352454F,3,len(fields)+2,count)+fields
create=struct.pack('<HIQ7fIB',5,0xFE89DDEA,OID,0,0,0,1,123.5,-45.25,2,2937962913,255)
peer={'create':create,'delta':delta(OID),'wrongObject':delta(OID+1),'contain':struct.pack('<HIQQI',4,0x56CBDE9E,OID,0,0xFFFFFFFE),'end':struct.pack('<HIQ',2,0x2C436037,OID),'destroy':struct.pack('<HIQB',3,0x4D45D504,OID,255)}
Path(sys.argv[1]).write_text(json.dumps({key:value.hex().upper() for key,value in peer.items()},indent=2)+'\n',encoding='utf-8')
print('PASS: independent lifecycle/six-scalar synthetic fixtures')
