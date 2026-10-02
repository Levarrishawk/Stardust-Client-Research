"""Independent raw-bit, cross-product placement, source-set and actor-pixel/extent oracle."""
import json, math, struct, sys
from pathlib import Path
from PIL import Image, ImageChops
peer_path,native_path,gpu_path,headless_path,cancel_path,first_path,second_path=map(Path,sys.argv[1:])
peer=json.loads(peer_path.read_text(encoding='utf-8-sig'))
native,gpu,headless,cancel=[p.read_text(encoding='utf-8-sig') for p in [native_path,gpu_path,headless_path,cancel_path]]
def one(text,prefix):return next(line[len(prefix):] for line in text.splitlines() if line.startswith(prefix))
expected=struct.pack('<HIQ7fIB',5,0xFE89DDEA,0xDEADBEEF00000001,.2,-0.,.3,1.7,-2,7,4,0xAF1DC1A1,255)
assert peer['create']==expected.hex().upper() and len(expected)==47 and expected[18:22]==b'\0\0\0\x80'
assert one(native,'baseline=')==peer['baseline']
# Decode the synthetic height delta directly; do not call a production decoder/oracle.
wire=bytes.fromhex(peer['delta']);assert len(wire)==31
assert struct.unpack('<HIQIBIH',wire[:25])==(5,0x12862153,0xDEADBEEF00000001,0x4352454F,3,8,1)
assert struct.unpack('<Hf',wire[25:])==(14,1.5)
for line in native.splitlines():
    if line.startswith('height_bits='):
        row=json.loads(line[len('height_bits='):]);assert row['bits']==f"{struct.unpack_from('<I',bytes.fromhex(peer[row['key']]),27)[0]:08X}"
for line in native.splitlines():
    if line.startswith('visibility='):
        row=json.loads(line[len('visibility='):]);assert row['raw']=={'baseline':1,'hidden':0,'shown':255}[row['key']]
        assert row['visible']==(row['raw']!=0)
x,y,z,w,px,pz,py=struct.unpack_from('<7f',expected,14);norm=math.hypot(x,y,z,w);q=[x/norm,y/norm,z/norm];w/=norm
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def rotate(v):
    a=cross(q,v);b=cross(q,a);return [v[i]+2*w*a[i]+2*b[i] for i in range(3)]
sign=[1,1,-1];columns=[[component*sign[c]*sign[r] for r,component in enumerate(rotate(v))] for c,v in enumerate([[1,0,0],[0,1,0],[0,0,1]])]
for text in [native,gpu,headless]:
    assert one(text,'state_create=')==expected.hex().upper()
    for prefix,height in [('state_base=',1),('state_height=',1.5)]:
        row=json.loads(one(text,prefix));assert row['origin']==[px,py,-pz]
        for a,b in zip(row['columns'],columns):
            assert all(abs(actual-wanted*height)<3e-7 for actual,wanted in zip(a,b)),(a,b)
def sources(text):return sorted([json.loads(line[7:]) for line in text.splitlines() if line.startswith('source=')],key=lambda r:r['Path'])
assert sources(native)==sources(gpu)==sources(headless)
rows=sources(native);assert len(rows)==len({r['Path'] for r in rows})==37 and sum(r['Bytes'] for r in rows)==4683848
for text in [gpu,headless]:
    for marker in ['invalid_heights=5 bounds=2','visibility=0,255','foreign_store=true','application_recheck=true','staged_cleanup=true','post_attach_cleanup=true','destroy_recreate=true','stale_preflight_no_allocation=true','revision_expiration_keeps_visual=true','stale_snapshot_rejected=true','resource_ids_fixed=true','PASS: state teardown']:
        assert marker in text,marker
assert 'negatives=12' in native and 'GPU_allocations=0' in native
assert 'PASS: state canceled archives_closed=true geometry_publications=0' in cancel
images=[Image.open(p).convert('RGB') for p in [first_path,second_path]]
assert images[0].size==images[1].size==(1280,720)
crops=[im.crop((0,180,im.width,im.height)) for im in images]
diff=ImageChops.difference(*crops);data=diff.tobytes();changed=sum(any(data[i:i+3]) for i in range(0,len(data),3))
assert changed==int(one(gpu,'PASS: state GPU pair actor_changed_pixels=').split()[0]) and changed>100
boxes=[]
for crop in crops:
    background=Image.new('RGB',crop.size,crop.getpixel((0,0)));boxes.append(ImageChops.difference(crop,background).getbbox())
assert all(box is not None for box in boxes)
widths=[box[2]-box[0] for box in boxes];heights=[box[3]-box[1] for box in boxes]
assert widths[1]>widths[0]*1.2 and heights[1]>heights[0]*1.2,(boxes,widths,heights)
assert 'fixed_camera=true UI_excluded_y=180 mip_readback=true' in gpu
print('PASS: independent raw signed-zero/scalar bits; quaternion cross-product/SRS placement; 37-source equality; actor pixels='+str(changed)+' widths='+str(widths)+' heights='+str(heights)+' fixed camera and allocation IDs')
