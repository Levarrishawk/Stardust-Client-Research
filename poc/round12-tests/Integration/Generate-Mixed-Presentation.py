"""Add an independently packed wounds assignment to the accepted presentation Height message."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
fields=struct.pack('<HfHII BHiii',14,1.5,17,4,13,3,3,-2,2147483647,-2)
wire=struct.pack('<HIQIBIH',5,0x12862153,0xF122334455667788,0x4352454F,3,len(fields)+2,2)+fields
peer['height']=wire.hex().upper();datagram=frame(struct.pack('>HH',9,7)+wire,True);assert len(datagram)<=496
peer['peer_height']=datagram.hex().upper()
Path(sys.argv[2]).write_text(json.dumps(peer,indent=2)+'\n',encoding='utf-8')
print('PASS: independent mixed Height/wounds framed presentation transaction')
