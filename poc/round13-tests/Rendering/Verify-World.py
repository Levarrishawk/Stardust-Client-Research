"""Independent application/state/coalescing/ownership-export and actual actor-region oracle."""
import json,math,struct,sys,hashlib
from pathlib import Path
from PIL import Image,ImageChops
fixture_path,native_path,gpu_path,headless_path,cancel_path,first_path,updated_path,recreated_path=map(Path,sys.argv[1:])
f={k:bytes.fromhex(v) for k,v in json.loads(fixture_path.read_text(encoding='utf-8-sig')).items()}
A=0xF122334455667788;B=0xE122334455667799
texts=[p.read_text(encoding='utf-8-sig') for p in [native_path,gpu_path,headless_path]]
rows=lambda text,prefix:[json.loads(line[len(prefix):]) for line in text.splitlines() if line.startswith(prefix)]
assert len(f)==18
for key,oid,x in [('a',A,-1.5),('b',B,1.5)]:
 c=struct.unpack('<HIQ7fIB',f[key+'_create']);assert c==(5,0xFE89DDEA,oid,0.,0.,0.,1.,x,0.,0.,2937962913,255)
 assert struct.unpack('<HIQ',f[key+'_end'])==(2,0x2C436037,oid)
 assert struct.unpack('<HIQB',f[key+'_destroy'])==(3,0x4D45D504,oid,255)
 for suffix in ['baseline','hidden','shown','height']:
  wire=f[key+'_'+suffix];header=struct.unpack_from('<HIQIBIH',wire)
  assert header[0]==5 and header[2:5]==(oid,0x4352454F,3) and header[5]==len(wire)-23
  if suffix=='height':assert header[1]==0x12862153 and header[6]==2 and struct.unpack_from('<HfHII',wire,25)==(14,struct.unpack('<f',struct.pack('<f',1.4 if key=='a' else 1.2))[0],17,0,0)
  else:assert header[1]==0x68A75F0C and header[6]==18
assert f['a_bad']==f['a_height'][:-1]
assert struct.unpack_from('<I',f['a_invalid_height'],27)[0]==0x80000000
# Registry revisions are global creation/end revisions; destroy without children retains counter.
actors={};pending={};mutation=registry=replication=0;expected=[]
def apply(key):
 global mutation,registry,replication
 wire=f[key];opcode=struct.unpack_from('<I',wire,2)[0];oid=struct.unpack_from('<Q',wire,6)[0];mutation+=1
 if opcode==0xFE89DDEA:
  registry+=1;actors[oid]=dict(incarnation=registry,handle=registry,revision=None,heightBits=None,visibility=None,wounds=[]);kind=1
 elif opcode==0x2C436037:registry+=1;actors[oid]['handle']=registry;kind=1
 elif opcode==0x4D45D504:del actors[oid];kind=1
 elif opcode==0x68A75F0C:
  body=struct.unpack_from('<fHIHIIHIIIIIIBBBQfIQII',wire,25);replication+=1;actors[oid].update(revision=replication,heightBits=struct.unpack('<I',struct.pack('<f',body[17]))[0],visibility=body[13],wounds=[]);kind=2
 elif opcode==0x12862153:
  replication+=1;actors[oid].update(revision=replication,heightBits=struct.unpack_from('<I',wire,27)[0]);kind=2
 else:raise AssertionError('Unexpected opcode')
 pending[oid]=pending.get(oid,0)|kind
def drain(stage):
 expected.append(dict(stage=stage,Generation=1,MutationRevision=mutation,entries=[dict(ObjectId=oid,kind=kind,current=actors.get(oid,{}).copy() if oid in actors else None) for oid,kind in pending.items()]));pending.clear()
for k in ['a','b']:
 for suffix in ['create','baseline','end']:apply(k+'_'+suffix)
drain('initial')
for k in ['a_height','b_height']:apply(k)
drain('both_height')
apply('b_hidden');drain('b_hidden');apply('b_shown');drain('b_shown');apply('a_invalid_height');apply('a_height');drain('height_retry');apply('a_destroy');drain('destroy')
for k in ['a_create','a_baseline','a_end','a_height']:apply(k)
drain('recreate')
for text in texts:assert rows(text,'two_batch=')==expected
assert 'negatives=50 truncations=41' in texts[0]
for text in texts[1:]:
 for marker in ['PASS: two lifecycle coalesced=true','stale_ack_two_pending=true','unchanged_b_ids=true','wrong_thread=3','invalid_height_pending=true','PASS: two capacity rejected_before_allocation=true limit=1 global_failure_retired=2','PASS: two teardown roots_released=true archives_closed=true drain_complete=true outstanding=0']:assert marker in text
 loads=rows(text,'two_loads=')[0];assert sorted((r['serial'],r['id'],r['reads'],r['bytes']) for r in loads)==[(1,A,39,4686371),(2,B,39,4686371),(3,A,39,4686371)]
 assert len(set(r['sourceHash'] for r in loads))==1
 states=rows(text,'two_state=');assert [s['stage'] for s in states]==['initial','updated','recreated']
 for s,n in zip(states,rows(texts[0],'two_native_state=')):
  assert (s['stage'],s['calls'],s['closed'])==(n['stage'],n['calls'],n['closed'])
  assert [{k:a[k] for k in ['id','incarnation','revision','heightBits','visible']} for a in s['actors']]==[{k:a[k] for k in ['id','incarnation','revision','heightBits','visible']} for a in n['actors']]
  for actor,x in zip(s['actors'],[-1.5,1.5]):
   height=struct.unpack('<f',struct.pack('<I',actor['heightBits']))[0];pose=actor['transform'];assert pose['origin']==[x,0,0]
   for observed,wanted in zip(pose['columns'],[[height,0,0],[0,height,0],[0,0,height]]):assert all(math.isclose(a,b,abs_tol=1e-6) for a,b in zip(observed,wanted))
 for index in [0,1]:assert states[0]['actors'][index]['ids']==states[1]['actors'][index]['ids']
 assert states[2]['actors'][1]['ids']==states[0]['actors'][1]['ids'] and states[2]['actors'][0]['ids']!=states[0]['actors'][0]['ids']
 assert [s['allocations'] for s in states]==[2,2,3] and [s['disposals'] for s in states]==[0,0,1]
assert 'PASS: two canceled jobs=2 publications=0 archives_closed=true drain_complete=true' in cancel_path.read_text(encoding='utf-8-sig')
images=[Image.open(p).convert('RGB') for p in [first_path,updated_path,recreated_path]];assert all(im.size==(1280,720) for im in images)
# Disjoint halves exclude the fixed label and leave generous whole-actor margins.
regions=[(0,100,640,720),(640,100,1280,720)]
changed=[];boxes=[]
for region in regions:
 crops=[im.crop(region) for im in images];background=crops[0].getpixel((0,0));bboxes=[]
 for crop in crops:
  box=ImageChops.difference(crop,Image.new('RGB',crop.size,background)).getbbox();assert box is not None and box[0]>10 and box[1]>10 and box[2]<630 and box[3]<610;bboxes.append(box)
 diff=ImageChops.difference(crops[0],crops[1]);rgb=diff.tobytes();changed.append(sum(any(rgb[i:i+3]) for i in range(0,len(rgb),3)));boxes.append(bboxes)
 assert crops[1].tobytes()==crops[2].tobytes(),'Recreated source/pose must match, retaining B pixels'
assert changed[0]>1000 and changed[1]==0
left=boxes[0];right=boxes[1];assert (left[1][2]-left[1][0])>(left[0][2]-left[0][0]) and (left[1][3]-left[1][1])>(left[0][3]-left[0][1]);assert right[0]==right[1]==right[2]
print('PASS: independent two-ID real layouts/coalescing/global revision/incarnation model; native50 negatives/41 truncations; GPU/headless state and IDs; cancellation and global retirement')
print('PASS: independent actor pixels changed='+str(changed)+' boxes='+str(boxes)+' updated/recreated identical; B region unchanged; fixed camera')
