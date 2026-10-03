"""Independent fixed-width common fields added to the accepted full fixture."""
import json,struct,sys
from pathlib import Path
source,target=map(Path,sys.argv[1:3]);fixture=json.loads(source.read_text())
assert fixture['FullFields'] and fixture['BaselineComponentCounter']==7
P=lambda f,*v:struct.pack('<'+f,*v)
def common(bits,volume,options,use,damage,maximum,visibility):
 return [P('HI',0,bits),P('HI',3,volume),P('HI',6,options),P('HI',7,use),P('HI',8,damage),P('HI',9,maximum),P('HB',10,visibility)]
zero=common(0x80000000,0xffffffff,1,2,3,100,1)
varied=common(0x3fe00000,0,0xf1234567,4,20,150,0)
restore=[P('HB',10,255),P('HI',8,25)]
last=[P('HI',0,0),P('HB',10,1)]
extras={'wire_zero':zero,'wire_varied':list(reversed(varied)),
 'wire_duplicate':[P('HI',7,8),P('HI',7,9)],'wire_empty':restore,
 'wire_count_zero':[P('HB',10,255)],'wire_extended_zero':last}
for key,fields in extras.items():
 wire=bytes.fromhex(fixture['Messages'][key]);count=struct.unpack_from('<H',wire,23)[0]
 # Place new fields on both sides of the old supported union.
 body=b''.join(fields[:1])+wire[25:]+b''.join(fields[1:])
 payload=P('H',(count+len(fields))&65535)+body
 fixture['Messages'][key]=(wire[:19]+P('I',len(payload))+payload).hex().upper()
fixture['ExtendedFields']=True
def expected(name,bits,volume,options,use,damage,maximum,visibility):
 return dict(Name=name,ComplexityBits=f'{bits:08X}',Tangible=dict(Volume=volume,Options=options,UseCount=use,ConditionDamage=damage,MaximumCondition=maximum,Visibility=visibility),RootVisible=visibility!=0)
fixture['ExpectedTangible']=[
 expected('zero',0x80000000,0xffffffff,1,2,3,100,1),
 expected('ordered_valid_then_invalid',0x3fe00000,0,0xf1234567,4,20,150,0),
 expected('duplicate_retains_prior_valid',0x3fe00000,0,0xf1234567,9,20,150,0),
 expected('empty_forms_retain',0x3fe00000,0,0xf1234567,9,25,150,255),
 expected('extended_zero_restore',0,0,0xf1234567,9,25,150,1)]
assert target.resolve().is_relative_to((Path(__file__).resolve().parents[3]/'local-private').resolve())
target.write_text(json.dumps(fixture,indent=2)+'\n',encoding='utf-8')
print('PASS: independent common fixed fields, hidden appearance and raw visibility255 restoration')
