"""Independent mixed CREO3 transaction peer based on accepted authored lifecycle fixture."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
peer=json.loads(Path(sys.argv[1]).read_text())
ID=0xF122334455667788
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
def packet(fields,count=3):return P('HIQIBIH',5,0x12862153,ID,0x4352454F,3,len(fields)+2,count)+fields
# Original fixture baseline is an empty wounds vector at counter9.
# Assignment advances four loop/counter units; equal replacement later is a no-op.
first=packet(P('HfHII BHiii HB',14,1.25,17,4,13,3,3,-2,2147483647,-2,11,8))
second=packet(P('HII BHi Hf',17,1,14,2,0,-2,14,1.5),2)
third=packet(P('HII BHi HB',17,1,14,1,5,-7,12,10),2)
bad=packet(P('HfHII BHi',14,1.75,17,1,15,1,65535,99),2)
peer['mixed']=first.hex().upper();peer['equal']=second.hex().upper();peer['growth']=third.hex().upper();peer['lateGrowth']=bad.hex().upper()
# Staging uses first transaction instead of the accepted scalar fixture.
peer['delta']=peer['mixed'];peer['peer_delta']=frame(struct.pack('>HH',9,3)+first,True).hex().upper()
for name in ('peer_delta',):assert len(bytes.fromhex(peer[name]))<=496
Path(sys.argv[2]).write_text(json.dumps(peer,indent=2)+'\n',encoding='utf-8')
print('PASS: independently packed ordered scalar/legacy-wounds transactions and496-byte staged peer')
