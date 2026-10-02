"""Independent exported-wire and application boundary audit; offline synthetic peer."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_offline_zone_scene import unframe
from verify_zone_scene_protocol import parse
from verify_scene_object_lifecycle import decode
from verify_replication_envelopes import decode as envelope
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
t=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
assert t['SessionRequest']=='0001000000021234567800000080'
assert t['SelectDatagram']==peer['expectedSelect']
assert t['ReadyDatagram']==t['Retransmission']==peer['expectedReady']
selected=unframe(bytes.fromhex(t['SelectDatagram']));ready=unframe(bytes.fromhex(t['ReadyDatagram']))
assert selected[:4]==struct.pack('>HH',9,0);parse(selected[4:],False)
assert ready==struct.pack('>HH',9,1)+bytes.fromhex('0100221CFD43')
names=['start','create','baseline','delta','end','contain','serverReady','destroy']
assert t['DeliveredApplications']==[peer[name] for name in names]
parse(bytes.fromhex(peer['start']),True)
for name,kind in [('create','create'),('destroy','destroy'),('contain','containment'),('end','end')]:decode(kind,bytes.fromhex(peer[name]))
for name in ('baseline','delta'):
 row=envelope(name,bytes.fromhex(peer[name]),65536)
 assert row['RawTag']==0x4352454F and row['View']==3 and row['DeclaredCount']==99 and row['OpaqueHex']=='7F00FF'
assert t['PreparedObjects']==1 and t['FinalState']=='Ready'
for value in t['Controls']:
 packet=unframe(bytes.fromhex(value));assert len(packet)==4 and struct.unpack_from('>H',packet)[0] in (17,21)
print('PASS: independent select/ready/retry wire, delivered lifecycle order, CREO:3 opaque envelopes and one publication')

