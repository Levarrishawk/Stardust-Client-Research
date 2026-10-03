"""Independently mix previously authored field4 bytes with literal scalar wire records."""
import argparse,json,struct
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('source',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
private=Path(__file__).resolve().parents[3]/'local-private'
assert a.output.resolve().is_relative_to(private.resolve()) and a.output.parent.is_dir()
v=json.loads(a.source.read_text());assert v['version']==1 and v['synthetic'] and len(v['Messages'])==10
heights={'wire_zero':1.4,'wire_varied':1.7,'wire_invalid_skin':1.7,'wire_duplicate':1.2,'wire_empty':1.3,'wire_count_zero':1.3,'wire_reset':1.4,'wire_extended_zero':1.4,'wire_unknown_id':2.0,'wire_invalid_inner':2.0}
def scalar(index,value):
 return struct.pack('<H',index)+struct.pack({11:'<B',12:'<B',13:'<Q',14:'<f',15:'<I',16:'<Q'}[index],value)
for key,hexbytes in list(v['Messages'].items()):
 wire=bytes.fromhex(hexbytes);assert len(wire)>=29 and struct.unpack_from('<H',wire)[0]==5 and wire[18]==3 and struct.unpack_from('<H',wire,25)[0]==4
 custom=wire[25:];height=scalar(14,heights[key]);declared=2
 if key=='wire_varied':fields=scalar(14,1.6)+custom+height+scalar(16,64);declared=4
 elif key=='wire_duplicate':fields=scalar(12,5)+custom+height+scalar(13,0xFEDCBA9876543210)+scalar(15,3);declared=5
 elif key=='wire_invalid_skin':fields=custom+scalar(11,8)+height;declared=3
 elif key in ('wire_zero','wire_empty','wire_unknown_id'):fields=height+custom
 else:fields=custom+height
 payload=struct.pack('<H',declared)+fields
 v['Messages'][key]=(wire[:19]+struct.pack('<I',len(payload))+payload).hex().upper()
v['MixedScalars']=True;v['ExpectedHeights']=heights
a.output.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
print('PASS: ten independently mixed field4/scalar packets; both orders and repeated Height')
