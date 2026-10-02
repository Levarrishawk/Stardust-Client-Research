"""Independently packed synthetic CREO:3 fixtures. No original/captured bytes."""
import json, struct, sys
from pathlib import Path
OID=0xDEADBEEF00000001
def envelope(op, count, fields):
    return struct.pack('<HIQIBIH',5,op,OID,0x4352454F,3,len(fields)+2,count)+fields
def baseline(visibility):
    fields=struct.pack('<fHIHIIHIIIIIIBBBQfIQII',1,0,0,0,0,1,0,0,7,0,0,0,100,visibility,4,9,0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210,0,9)
    return envelope(0x68A75F0C,18,fields)
def delta(height):return envelope(0x12862153,1,struct.pack('<Hf',14,height))
peer={'baseline':baseline(1),'hidden':baseline(0),'shown':baseline(255),'delta':delta(1.5)}
for name,value in [('zero',0),('negativeZero',-0.),('negative',-1),('small',.1),('large',2.1),('minimum',.25),('maximum',2)]:peer[name]=delta(value)
peer['create']=struct.pack('<HIQ7fIB',5,0xFE89DDEA,OID,.2,-0.,.3,1.7,-2,7,4,0xAF1DC1A1,255)
Path(sys.argv[1]).write_text(json.dumps({k:v.hex().upper() for k,v in peer.items()},indent=2)+'\n',encoding='utf-8')
print('PASS: independent synthetic 18-field baseline/height delta/create fixtures')
