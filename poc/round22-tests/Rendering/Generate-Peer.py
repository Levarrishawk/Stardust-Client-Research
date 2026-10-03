"""Rendering18 world fixture plus independently authored scalar-only race packets."""
import json, runpy, struct, sys
from pathlib import Path
output=Path(sys.argv[1]);repo=Path(__file__).resolve().parents[3]
sys.argv=[str(repo/'poc/round18-tests/Rendering/Generate-Peer.py'),str(output)]
runpy.run_path(sys.argv[0],run_name='__main__')
peer=json.loads(output.read_text(encoding='utf-8'));oid=0xF122334455667788
def scalar(posture,rank,link,height,shock,states):
 fields=b''.join(struct.pack('<H',index)+struct.pack('<'+kind,value) for index,kind,value in
  [(11,'B',posture),(12,'B',rank),(13,'Q',link),(14,'f',height),(15,'I',shock),(16,'Q',states)])
 return struct.pack('<HIQIBIH',5,0x12862153,oid,0x4352454F,3,len(fields)+2,6)+fields
peer['race_scalars_first']=scalar(2,11,0xF102030405060788,1.25,33,0x123456789ABCDEF0).hex().upper()
peer['race_scalars_latest']=scalar(7,42,0xF102030405060799,1.6,0xDEADBEEF,0xFEDCBA9876543211).hex().upper()
output.write_text(json.dumps(peer,indent=2)+'\n',encoding='utf-8')
print('PASS: Rendering18 peer plus two independent six-scalar race packets')
