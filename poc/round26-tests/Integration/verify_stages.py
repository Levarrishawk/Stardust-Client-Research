"""Independent stage result and exact wire audit; no production imports."""
import json,struct,sys
from pathlib import Path
fixture=json.loads(Path(sys.argv[1]).read_text());result=json.loads(Path(sys.argv[2]).read_text())
assert result['CompleteWorldTransaction'] is False
assert result['BaselineExact'] and result['DeltaWiresExact']
assert result['RejectedLateUnsupported'] and result['RejectedGrowthThenClear'] and result['AtomicSnapshots']
assert result['StaleIncarnationRejected'] and result['Drained']
assert result['Tables']==dict(Requested=3,Loaded=2,Missing=1,SourceBytes=7137,Records=97,ResourceCalls=3)
assert len(result['Rows'])==len(fixture['Expected'])==5
for actual,expected in zip(result['Rows'],fixture['Expected']):
 assert all(actual.get(key)==value for key,value in expected.items()),(actual,expected)
 assert actual['BaselineRetained'] and actual['HistoricBaselineWire']==fixture['Baseline']
raw=bytes.fromhex(fixture['Baseline']);count,opcode,oid,tag,view,size,declared=struct.unpack_from('<HIQIBIH',raw)
assert (count,opcode,oid,tag,view,size,declared)==(5,0x68a75f0c,fixture['ObjectId'],0x54414e4f,3,len(raw)-23,65535)
cursor=25
def read(fmt):
 global cursor
 value=struct.unpack_from('<'+fmt,raw,cursor);cursor+=struct.calcsize('<'+fmt);return value[0] if len(value)==1 else value
def narrow():
 global cursor
 prefix=read('H');n=read('I') if prefix==65535 else prefix
 value=raw[cursor:cursor+n];cursor+=n;return value,prefix==65535
assert read('I')==0x80000000
file,fe=narrow();spacer=read('I');key,ke=narrow();unitcount=read('I');units=[read('H') for _ in range(unitcount)]
assert read('I')==0xffffffff
customization,ce=narrow();components=read('I');counter=read('I');values=[read('i') for _ in range(components)]
assert (values,counter)==([9,9,-3],7)
assert read('IIIIB')==(1,2,3,100,1) and cursor==len(raw)
assert fixture['BaselineDecodedBytes']==47+len(file)+len(key)+2*len(units)+len(customization)+4*components
assert fe and not ke and not ce and spacer==0xa1b2c3d4
for row in fixture['Updates']:
 rawwire=bytes.fromhex(row['Hex']);header=struct.unpack_from('<HIQIBIH',rawwire)
 assert header[:5]==(5,0x12862153,fixture['ObjectId'],0x54414e4f,3) and header[5]==len(rawwire)-23
print(json.dumps(dict(passed=True,complete_tano_wire=True,ordered_store_names_raw_components=True,original_owned_preload=True,
 intermediate_growth_atomic=True,incarnation_cleanup=True,complete_world_transaction=False)))
