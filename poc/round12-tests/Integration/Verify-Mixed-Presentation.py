"""Audit mixed mode reaching current presentation state and unchanged scalar/palette GPU pixels."""
import json,struct,sys
from pathlib import Path
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_mixed_deltas import parse
from verify_creature_wounds_legacy_policy import emulate
from verify_status_vectors import frame
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
log=Path(sys.argv[2]).read_text(encoding='utf-8-sig')
rows=[json.loads(line.split('=',1)[1]) for line in log.splitlines() if line.startswith('mixed_state=')]
assert len(rows)==2
expected=[dict(stage='initial',Mode='CreatureView3Mixed',ObjectId=0xF122334455667788,Revision=2,HeightBits=0x3F800000,Counter=9,Values=[])]
local,values=9,[];height=0x3F800000
for index,value in parse(bytes.fromhex(peer['height']),True):
    if index==14:height=value
    elif index==17:
        state=emulate(local,values,[value],64);local,values=state['Counter'],state['Values']
    else:raise AssertionError('Unexpected fixture field')
assert frame(struct.pack('>HH',9,7)+bytes.fromhex(peer['height']),True).hex().upper()==peer['peer_height']
expected.append(dict(stage='height',Mode='CreatureView3Mixed',ObjectId=0xF122334455667788,Revision=3,HeightBits=height,Counter=local,Values=values))
assert rows==expected,(rows,expected)
for marker in ['apply_before_ack=true','stale_ack_pending=true','retry_latest=true','state_revision_separate=true resource_ids_fixed=true','PASS: observer palette off_thread=true','PASS: replicated teardown']:
    assert marker in log,marker
# Same current scalar transform/color and unchanged GPU resources imply identical actor pixels.
for normal,mixed in [(3,4),(5,6),(7,8)]:
    a=Image.open(sys.argv[normal]).convert('RGBA');b=Image.open(sys.argv[mixed]).convert('RGBA')
    assert a.size==b.size==(1280,720)
    assert a.crop((0,180,1280,720)).tobytes()==b.crop((0,180,1280,720)).tobytes()
print('PASS: independently framed mixed transaction reaches current wounds13 and Height1.5; observer/cleanup guards and all three scalar/palette actor images unchanged')
