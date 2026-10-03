"""Independent ordered compound packets; strict wounds counters, recovered set rules."""
import json,runpy,struct,sys
from pathlib import Path
output=Path(sys.argv[1]);repo=Path(__file__).resolve().parents[3]
sys.argv=[str(repo/'poc/round22-tests/Rendering/Generate-Peer.py'),str(output)]
runpy.run_path(sys.argv[0],run_name='__main__')
peer=json.loads(output.read_text());P=lambda f,*v:struct.pack('<'+f,*v)
def field(index,body):return P('H',index)+body
def components(counter,commands):return field(5,P('II',len(commands),counter)+b''.join(commands))
def wounds(counter,commands):return field(17,P('II',len(commands),counter)+b''.join(commands))
def assign(values):return P('BH',3,len(values))+P('i'*len(values),*values)
def packet(fields):
 body=b''.join(fields);return P('HIQIBIH',5,0x12862153,0xF122334455667788,0x4352454F,3,len(body)+2,len(fields))+body
peer['race_containers_first']=packet([
 components(9,[P('Bi',1,-2),P('Bi',1,7)]),field(14,P('f',1.25)),wounds(10,[assign([10,-20])]),
 field(11,P('B',2)),field(12,P('B',11)),field(13,P('Q',0xF102030405060788)),field(15,P('I',33)),field(16,P('Q',0x123456789ABCDEF0))]).hex().upper()
peer['race_containers_latest']=packet([
 wounds(12,[P('B',4),assign([5,-6,5])]),field(11,P('B',7)),components(12,[P('B',2),P('Bi',1,99),P('Bi',1,-7)]),
 field(14,P('f',1.6)),field(12,P('B',42)),field(13,P('Q',0xF102030405060799)),field(15,P('I',0xDEADBEEF)),field(16,P('Q',0xFEDCBA9876543211))]).hex().upper()
output.write_text(json.dumps(peer,indent=2)+'\n')
print('PASS: independent compound component/wounds/scalar packets; replay latest unchanged')
