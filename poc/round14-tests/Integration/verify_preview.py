"""Mixed-world log and fixed-camera PNG oracle; labels excluded from pixel comparisons."""
import json, struct, sys
from pathlib import Path
from PIL import Image
def states(path):
    text=Path(path).read_text(encoding='utf-8-sig',errors='replace')
    assert 'PASS: mixed lifecycle typed_creature=true opaque_terminal=true stale_ack_retained=true static_only_reload=true human_ids_unchanged=true family_failure_retired=2 calls=3' in text
    assert 'PASS: mixed teardown roots_released=true archives_closed=true drain_complete=true outstanding=0' in text
    result=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('mixed_state=')]
    assert [r['stage'] for r in result]==['initial','updated','recreated']
    a,b,c=result
    assert [r['calls'] for r in result]==[2,2,3] and [r['closed'] for r in result]==[2,2,3]
    assert a['humanIncarnation']==b['humanIncarnation']==c['humanIncarnation']
    assert a['terminalIncarnation']==b['terminalIncarnation']<c['terminalIncarnation']
    assert a['humanIds']==b['humanIds']==c['humanIds']
    assert a['terminalIds']==b['terminalIds']!=c['terminalIds']
    assert [r['opaque'] for r in result]==[1,2,0]
    assert a['heightBits']==0x3f800000 and b['heightBits']==c['heightBits']==struct.unpack('<I',struct.pack('<f',1.4))[0]
    assert a['terminalPose']==b['terminalPose']==c['terminalPose']
    assert b['humanPose']==c['humanPose'] and a['humanPose']!=b['humanPose']
    return result
gpu,headless,cancel,first,updated,recreated=sys.argv[1:]
g=states(gpu);h=states(headless)
for x,y in zip(g,h):
    for k in ['stage','calls','closed','heightBits','humanIncarnation','terminalIncarnation','opaque','humanPose','terminalPose']:
        assert x[k]==y[k],(k,x[k],y[k])
canceltext=Path(cancel).read_text(encoding='utf-8-sig',errors='replace')
assert 'PASS: mixed canceled jobs=2 publications=0 archives_closed=true drain_complete=true' in canceltext
images=[Image.open(p).convert('RGBA') for p in [first,updated,recreated]]
assert images[0].size==images[1].size==images[2].size
w,height=images[0].size
assert w>=640 and height>=480
human_region=(w//2,180,w,height);terminal_region=(0,180,w//2,height)
before=images[0].crop(human_region).tobytes();after=images[1].crop(human_region).tobytes()
changed=sum(before[i:i+4]!=after[i:i+4] for i in range(0,len(before),4))
assert changed>=1000
assert images[0].crop(terminal_region).tobytes()==images[1].crop(terminal_region).tobytes()==images[2].crop(terminal_region).tobytes()
assert images[1].crop(human_region).tobytes()==images[2].crop(human_region).tobytes()
print(json.dumps({'pass':True,'changed_human_pixels':changed,'unchanged_terminal_pixels':True,'updated_recreated_actor_pixels_identical':True,'typed_opaque_state_and_incarnations':True,'gpu_headless_agree':True,'cancel_drained':True}))
