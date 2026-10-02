"""Independent synthetic complete baseline/lifecycle/delta producer; no captured/original bytes."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_baselines import fixture
OID=0xF122334455667788
fields=b''.join(struct.pack('<H'+fmt,index,value) for index,fmt,value in [(16,'Q',0xFEDCBA9876543210),(12,'B',254),(14,'f',-1.75),(11,'B',255),(15,'I',0xF1020304),(13,'Q',0xFE02030405060708),(11,'B',7)])+struct.pack('<HI',14,0x80000000)
def delta(oid):return struct.pack('<HIQIBIH',5,0x12862153,oid,0x4352454F,3,len(fields)+2,65535)+fields
baseline=fixture();big=bytearray(baseline);big[38:38]=b'\x01\x02\x03\x04';struct.pack_into('<I',big,31,7);struct.pack_into('<I',big,19,len(big)-23)
wrong=bytearray(baseline);struct.pack_into('<Q',wrong,6,OID+1)
peer={'baseline':baseline,'bigBaseline':big,'wrongBaseline':wrong,'delta':delta(OID),'wrongDelta':delta(OID+1),'create':struct.pack('<HIQ7fIB',5,0xFE89DDEA,OID,0,0,0,1,123.5,-45.25,2,2937962913,255),'contain':struct.pack('<HIQQI',4,0x56CBDE9E,OID,0,0xFFFFFFFE),'end':struct.pack('<HIQ',2,0x2C436037,OID),'destroy':struct.pack('<HIQB',3,0x4D45D504,OID,255)}
Path(sys.argv[1]).write_text(json.dumps({k:bytes(v).hex().upper() for k,v in peer.items()},indent=2)+'\n',encoding='utf-8')
print('PASS: complete baseline/six-scalar/lifecycle synthetic fixtures')
