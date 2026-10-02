"""Independent full baseline and interleaved set/vector/scalar world transactions."""
import json, struct, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'tools/python'))
from verify_status_vectors import frame
peer = json.loads(Path(sys.argv[1]).read_text())
ID = 0xF122334455667788
P = lambda fmt, *v: struct.pack('<' + fmt, *v)
def envelope(op, fields, count):
    return P('HIQIBIH', 5, op, ID, 0x4352454F, 3, len(fields)+2, count)+fields
def components(counter, ops):
    return P('HII', 5, len(ops), counter)+b''.join(P('B',kind)+(P('i',key) if kind!=2 else b'') for kind,key in ops)
body = P('fHIHIIH',1,0,0,0,0,1,0)+P('IIiii',3,0,-1,-2147483648,-1)
body += P('IIIIBBBQfIQ',0,0,0,100,1,4,9,0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210)+P('II',0,9)
peer['baseline'] = envelope(0x68A75F0C,body,18).hex().upper()
first = P('Hf',14,1.25)+components(2,[(1,17),(0,-1)])+P('HII BHiii HB',17,4,13,3,3,-2,2147483647,-2,11,8)
second = components(3,[(1,17)])+P('HII BHi Hf',17,1,14,2,0,-2,14,1.5)
third = components(5,[(1,2147483647),(0,17)])+P('HII BHi HB',17,1,14,1,5,-7,12,10)
bad = P('Hf',14,1.75)+components(6,[(1,9)])+P('HII BHi',17,1,15,1,65535,99)
for name,fields,count in [('composite',first,4),('equal',second,3),('growth',third,3),('lateGrowth',bad,3)]:
    peer[name]=envelope(0x12862153,fields,count).hex().upper()
peer['delta']=peer['composite']
for sequence,name in enumerate(('start','create','baseline','delta','end','contain','serverReady')):
    raw=frame(struct.pack('>HH',9,sequence)+bytes.fromhex(peer[name]),True)
    assert len(raw)<=496
    peer['peer_'+name]=raw.hex().upper()
Path(sys.argv[2]).write_text(json.dumps(peer,indent=2)+'\n',encoding='utf-8')
print('PASS: independent raw duplicate components baseline and ordered composite496-byte peer')
