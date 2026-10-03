"""Hand-authored heterogeneous application fixtures; synthetic opaque TANO payload."""
import json, struct, sys
from pathlib import Path
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
A=0xF122334455667788
B=0xE122334455667799
def envelope(oid,op,tag,count,body):
    return P('HIQIBIH',5,op,oid,tag,3,len(body)+2,count)+body
def baseline():
    return envelope(A,0x68A75F0C,0x4352454F,18,P('fHIHIIHIIIIIIBBBQfIQII',
        1,0,0,0,0,1,0,0,7,0,0,0,100,1,4,9,0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210,0,9))
r={'start':P('HIBQ',9,0x3AE6DFAE,165,A)+P('H',3)+b'A/B'+P('3f',0,0,0)+P('H',8)+b'template'+P('Q',0),
   'ready':bytes.fromhex('0100221CFD43')}
for prefix,oid,x,crc in [('human',A,-1.5,2937962913),('terminal',B,1.5,2907007746)]:
    r[prefix+'_create']=P('HIQ7fIB',5,0xFE89DDEA,oid,0,0,0,1,x,0,0,crc,255)
    r[prefix+'_end']=P('HIQ',2,0x2C436037,oid)
    r[prefix+'_destroy']=P('HIQB',3,0x4D45D504,oid,255)
r['human_baseline']=baseline()
r['human_height']=envelope(A,0x12862153,0x4352454F,1,P('Hf',14,1.4))
r['terminal_baseline']=envelope(B,0x68A75F0C,0x54414E4F,0,b'\xca\xfe\x00\xff')
r['terminal_delta']=envelope(B,0x12862153,0x54414E4F,0,b'\x11\x22\x33\x44')
r['terminal_bad_family']=envelope(B,0x68A75F0C,0x4352454F,18,baseline()[25:])
r['late_bad']=r['human_height'][:-1]
Path(sys.argv[1]).write_text(json.dumps({k:v.hex().upper() for k,v in r.items()},indent=2)+'\n',encoding='utf-8')
print('PASS: independent mixed-world synthetic application fixture; opaque TANO payload has no schema claim')
