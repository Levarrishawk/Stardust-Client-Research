"""Independent 496-byte wire, state bits, cross-product placement and actor pixel/extent proof."""
import json,math,struct,sys
from pathlib import Path
from PIL import Image,ImageChops
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
peer_path,native_path,gpu_path,headless_path,cancel_path,first_path,second_path,authored_path=map(Path,sys.argv[1:]);peer=json.loads(peer_path.read_text(encoding='utf-8-sig'))
native,gpu,headless,cancel=[p.read_text(encoding='utf-8-sig') for p in [native_path,gpu_path,headless_path,cancel_path]]
def one(text,prefix):return next(s[len(prefix):] for s in text.splitlines() if s.startswith(prefix))
def rows(text,prefix):return [json.loads(s[len(prefix):]) for s in text.splitlines() if s.startswith(prefix)]
def unframe(wire):
    assert 5<=len(wire)<=496;data=bytearray(wire[:-2]);chain=0xA1B2C3D4;cursor=2
    while cursor+4<=len(data):
        cipher,=struct.unpack_from('<I',data,cursor);struct.pack_into('<I',data,cursor,cipher^chain);chain=cipher;cursor+=4
    while cursor<len(data):data[cursor]^=chain&255;cursor+=1
    assert data[-1]==0 and frame(bytes(data[:-1]),True)==wire;return bytes(data[:-1])
names=['start','create','baseline','delta','end','contain','serverReady','height','hidden','shown']
for seq,name in enumerate(names):assert unframe(bytes.fromhex(peer['peer_'+name]))==struct.pack('>HH',9,seq)+bytes.fromhex(peer[name])
for text in [native,gpu,headless]:
    transcript=json.loads(one(text,'replicated_wire='));assert bytes.fromhex(transcript['SessionRequest'])==struct.pack('>HIII',1,2,0x12345678,496)
    assert transcript['SelectDatagram']==peer['expectedSelect'] and transcript['ReadyDatagram']==transcript['Retransmission']==peer['expectedReady']
    assert transcript['DeliveredApplications']==[peer[name] for name in names] and transcript['State']=='Ready' and transcript['RetainedPackets']==0
    assert one(text,'replicated_baseline=')==peer['baseline']
    for marker in ['replicated_phase=Finalized state=AwaitingServerReady prepared=false','replicated_phase=Prepared state=AwaitingServerReady finalized=true','replicated_phase=Ready finalized=true prepared=true']:assert marker in text
assert bytes.fromhex(peer['response'])==struct.pack('>HII3BI',2,0x12345678,0xA1B2C3D4,2,1,4,496)
expected=struct.pack('<HIQ7fIB',5,0xFE89DDEA,0xF122334455667788,.2,-0.,.3,1.7,-2,7,4,2937962913,255)
assert peer['create']==expected.hex().upper() and len(expected)==47 and expected[18:22]==b'\0\0\0\x80'
wire=bytes.fromhex(peer['height']);assert struct.unpack('<HIQIBIHHf',wire)==(5,0x12862153,0xF122334455667788,0x4352454F,3,8,1,14,1.5)
for row in rows(native,'height_bits='):assert row['bits']==f"{struct.unpack_from('<I',bytes.fromhex(peer[row['key']]),27)[0]:08X}"
x,y,z,w,px,pz,py=struct.unpack_from('<7f',expected,14);norm=math.hypot(x,y,z,w);q=[x/norm,y/norm,z/norm];w/=norm
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def rotate(v):
    a=cross(q,v);b=cross(q,a);return [v[i]+2*w*a[i]+2*b[i] for i in range(3)]
sign=[1,1,-1];columns=[[component*sign[c]*sign[r] for r,component in enumerate(rotate(v))] for c,v in enumerate([[1,0,0],[0,1,0],[0,0,1]])]
for text in [native,gpu,headless]:
    assert one(text,'state_create=')==expected.hex().upper()
    for prefix,height in [('state_base=',1),('state_height=',1.5)]:
        row=json.loads(one(text,prefix));assert row['origin']==[px,py,-pz]
        for a,b in zip(row['columns'],columns):assert all(abs(actual-wanted*height)<3e-7 for actual,wanted in zip(a,b))
    for prefix,key in [('source=','Path'),('factor=','path'),('program=','path'),('texture=','path'),('sample=','shader')]:
        assert sorted(rows(native,prefix),key=lambda r:r[key])==sorted(rows(text,prefix),key=lambda r:r[key])
for text in [gpu,headless]:
    for marker in ['invalid_heights=5 bounds=2','foreign_world=true','application_recheck=true','restart_attempt=true','stale_preflight=true','staged_cleanup=true','post_attach_cleanup=true','destroy_recreate=true','stale_generation=true','capacity2=true','late_success_fault=true','close_drain=true','malformed_generation=true','state_revision_separate=true resource_ids_fixed=true visibility=0,255 original39=true','PASS: replicated teardown']:assert marker in text,marker
    choices={r['name']:r for r in rows(text,'observer_color=')};assert set(choices)=={'initial','authored'}
    gpu_rows=rows(text,'gpu_factor=');assert len({r['path'] for r in gpu_rows})==4
    for row in gpu_rows:
        factor=next(r['factors'] for r in choices[row['choice']]['factors'] if r['path']==row['path']);assert row['mask']==factor['UsesAlphaHueMask']
        for key,argb in [('first',factor['FirstArgb']),('second',factor['SecondArgb'])]:assert all(abs(a-(argb>>shift&255)/255)<1e-7 for a,shift in zip(row[key],[16,8,0]))
    for marker in ['apply_before_ack=true','stale_ack_pending=true','retry_latest=true','foreign_batch=true','stale_batch=true','invalid_height_pending=true','restart_separate=true','tombstone=true','reset_global_retire=true','failure_global_retire=true','close_global_retire=true','no_history=true','palette_closure_rejected=true','wrong_thread=true','PASS: observer palette off_thread=true owned_profile=true source_reads=39 no_extra_reads=true GPU_ids_fixed=true choices=2']:assert marker in text,marker
assert 'negatives=19' in native and 'epoch_independent=true' in native and 'borrowed_ownership=true' in native and 'GPU_allocations=0' in native
assert all('epoch_independent=true' in text for text in [gpu,headless])
assert 'PASS: replicated canceled generation_reset=true archives_closed=true publications=0 drain_complete=true' in cancel
images=[Image.open(p).convert('RGB') for p in [first_path,second_path]];assert images[0].size==images[1].size==(1280,720)
crops=[im.crop((0,180,im.width,im.height)) for im in images];diff=ImageChops.difference(*crops);data=diff.tobytes();changed=sum(any(data[i:i+3]) for i in range(0,len(data),3))
assert changed==int(one(gpu,'PASS: replicated GPU pair actor_changed_pixels=').split()[0]) and changed>100
boxes=[ImageChops.difference(crop,Image.new('RGB',crop.size,crop.getpixel((0,0)))).getbbox() for crop in crops];assert all(boxes)
widths=[box[2]-box[0] for box in boxes];heights=[box[3]-box[1] for box in boxes];assert widths[1]>widths[0]*1.2 and heights[1]>heights[0]*1.2
assert all(0<b[0]<b[2]<1280 and 0<b[1]<b[3]<540 for b in boxes),'Whole actor must fit below UI'
warm=[]
for crop in crops:
    rgb=crop.tobytes();warm.append(sum(rgb[i]>rgb[i+1]+8 and rgb[i+1]>=rgb[i+2] for i in range(0,len(rgb),3)))
assert min(warm)>100,'Authored body hue must be visible'
print('PASS: independent496-byte peer/request/retry/order; state/signed-zero bits; quaternion/SRS placement; sources/factors/mips/GPU parameters; actor pixels='+str(changed)+' widths='+str(widths)+' heights='+str(heights)+' warm_pixels='+str(warm))

# Independent coalescing model: five initial applications coalesce lifecycle/replication;
# acknowledged scalar/baseline updates retain the handle/incarnation and advance state.
expected_batches=[('initial',5,3,2,0x3F800000,1),('height',6,2,3,0x3FC00000,1),('hidden',7,2,4,0x3F800000,0),('shown',8,2,5,0x3F800000,255)]
for text in [native,gpu,headless]:
    batches=rows(text,'observer_batch=');assert len(batches)==4
    for actual,(stage,mutation,kind,revision,height,visibility) in zip(batches,expected_batches):
        assert actual=={'stage':stage,'Generation':1,'MutationRevision':mutation,'entries':[{'ObjectId':0xF122334455667788,'kind':kind,'current':{'incarnation':1,'handle':3,'revision':revision,'heightBits':height,'visibility':visibility}}]}
assert 'PASS: native observer stale_ack_pending=true retry_latest=true no_history=true' in native
third=Image.open(authored_path).convert('RGB');assert third.size==images[1].size
color_crop=third.crop((0,180,third.width,third.height));data=ImageChops.difference(crops[1],color_crop).tobytes();colors_changed=sum(any(data[i:i+3]) for i in range(0,len(data),3));assert colors_changed>100
color_box=ImageChops.difference(color_crop,Image.new('RGB',color_crop.size,color_crop.getpixel((0,0)))).getbbox();assert color_box==boxes[1],'Palette choice must preserve actual actor extent'
print('PASS: independent coalesced/acknowledged latest-state model; palette actor changed_pixels='+str(colors_changed)+' same_extent='+str(color_box)+' fixed camera/GPU IDs')
