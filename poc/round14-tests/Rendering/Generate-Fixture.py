"""Hand-authored two-ID original-layout lifecycle, nonunit raw rotation/placement."""
import json,struct,sys
from pathlib import Path
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
r={}
for name,oid,x in [('a',0xF122334455667788,-2),('b',0xE122334455667799,8)]:
 r[name+'_create']=P('HIQ7fIB',5,0xFE89DDEA,oid,.2,-0.,.3,1.7,x,7,4,2907007746,255)
 r[name+'_end']=P('HIQ',2,0x2C436037,oid)
 r[name+'_destroy']=P('HIQB',3,0x4D45D504,oid,255)
Path(sys.argv[1]).write_text(json.dumps({k:v.hex().upper() for k,v in r.items()},indent=2)+'\n',encoding='utf-8')
print('PASS: authored real-layout two-terminal create/end/destroy, distinct IDs/nonunit quaternion/signed-zero')
