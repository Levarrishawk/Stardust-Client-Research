import json,struct,sys
from pathlib import Path
native,gpu,headless,cancel=map(lambda p:Path(p).read_text(encoding='utf-8-sig'),sys.argv[1:])
def rows(text,prefix):
    values=[json.loads(line[len(prefix):]) for line in text.splitlines() if line.startswith(prefix)]
    assert len(values)==37 and len({v['Path'] for v in values})==37
    assert sum(v['Bytes'] for v in values)==4683848
    return sorted(values,key=lambda v:v['Path'])
assert rows(native,'source=')==rows(gpu,'visual_source=')==rows(headless,'visual_source=')
expected=struct.pack('<HIQ7fIB',5,0xFE89DDEA,0xDEADBEEF00000001,.2,-0.,.3,1.7,-2,7,4,0xAF1DC1A1,255).hex().upper()
assert len(bytes.fromhex(expected))==47 and bytes.fromhex(expected)[18:22]==b'\x00\x00\x00\x80'
for text,prefix in [(native,'create='),(gpu,'visual_create='),(headless,'visual_create=')]:
    assert next(line[len(prefix):] for line in text.splitlines() if line.startswith(prefix))==expected
for text in [gpu,headless]:
    for marker in ['lifecycle6','staged_cleanup=true','wrong_thread_rejected=true','post_attach_cleanup=true','PASS: visual teardown']:
        assert marker in text
assert 'negatives=19' in native
assert 'source_transform_unapplied=true' in gpu and 'mip_readback=true' in gpu
origin=json.loads(next(line.split('=',1)[1] for line in gpu.splitlines() if line.startswith('visual_engine_origin=')))
assert origin=={'X':1.4,'Y':.2,'Z':-.75}
assert 'PASS: visual canceled' in cancel
print('PASS: 37 original source identities agree; independent synthetic 47-byte oracle and explicit placement agree; lifecycle/GPU/cancellation markers present.')
