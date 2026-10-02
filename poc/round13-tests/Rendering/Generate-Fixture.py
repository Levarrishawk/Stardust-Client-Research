"""Independently authored application layouts; no capture or original executable."""
import json,struct,sys
from pathlib import Path
A=0xF122334455667788;B=0xE122334455667799
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
def envelope(oid,op,count,body):return P('HIQIBIH',5,op,oid,0x4352454F,3,len(body)+2,count)+body
def baseline(oid,visible=1):return envelope(oid,0x68A75F0C,18,P('fHIHIIHIIIIIIBBBQfIQII',1,0,0,0,0,1,0,0,7,0,0,0,100,visible,4,9,0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210,0,9))
def height(oid,value):return envelope(oid,0x12862153,2,P('HfHII',14,value,17,0,0))
r={'start':P('HIBQ',9,0x3AE6DFAE,165,A)+P('H',3)+b'A/B'+P('3f',0,0,0)+P('H',8)+b'template'+P('Q',0),'ready':bytes.fromhex('0100221CFD43')}
for key,oid,x in [('a',A,-1.5),('b',B,1.5)]:
 r[key+'_create']=P('HIQ7fIB',5,0xFE89DDEA,oid,0,0,0,1,x,0,0,2937962913,255)
 r[key+'_baseline']=baseline(oid);r[key+'_hidden']=baseline(oid,0);r[key+'_shown']=baseline(oid,255)
 r[key+'_height']=height(oid,1.4 if key=='a' else 1.2)
 r[key+'_end']=P('HIQ',2,0x2C436037,oid);r[key+'_destroy']=P('HIQB',3,0x4D45D504,oid,255)
r['a_invalid_height']=height(A,-0.0);r['a_bad']=r['a_height'][:-1]
Path(sys.argv[1]).write_text(json.dumps({k:v.hex().upper() for k,v in r.items()},indent=2)+'\n',encoding='utf-8')
print('PASS: independent two-ID lifecycle/full18 baseline/mixed Height+empty wounds fixtures')
