"""Independent scalar/receipt, original face composition and fixed-pose race pixel audit."""
import base64,json,struct,sys
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_face_composition import verify
from verify_face_resources import verify_preparation
export,peer,log=map(Path,sys.argv[1:4]);data=json.loads(export.read_text(encoding='utf-8'));fixture=json.loads(peer.read_text(encoding='utf-8'))
race=data['heightRace'];assert race is not None
latest=bytes.fromhex(fixture['race_scalars_latest']);assert struct.unpack_from('<HIQIBIH',latest)==(5,0x12862153,0xF122334455667788,0x4352454F,3,len(latest)-23,6)
cursor=25;expected={}
for index,kind,name in [(11,'B','Posture'),(12,'B','Rank'),(13,'Q','LinkId'),(14,'I','heightBits'),(15,'I','Shock'),(16,'Q','States')]:
 assert struct.unpack_from('<H',latest,cursor)[0]==index;cursor+=2
 expected[name]=struct.unpack_from('<'+kind,latest,cursor)[0];cursor+=struct.calcsize('<'+kind)
assert cursor==len(latest)
h,c,r=[race[k] for k in ('held','committed','reset')]
for state in (h,c):
 assert all(state[k]==v for k,v in expected.items())
 assert state['transform']==dict(origin=[-1.5,.2,-.1],x=[1.6,0,0],y=[0,1.6,0],z=[0,0,1.6])
 assert state['Frame']==4.5 and state['SelectedClip']=='walk' and state['sourceRevision']==4 and state['sourceValues']==8 and state['peakActive']==1
assert h['Revision']==c['Revision']==3 and h['pose']==c['pose'] and h['ownership']==c['ownership']==r['ownership']
assert h['LastAppliedRevision']==1 and c['LastAppliedRevision']==4 and h['active']==1 and c['active']==0
assert r['heightBits']==0x3f800000 and r['Frame']==0 and r['SelectedClip']=='idle' and r['LastAppliedRevision']==22 and r['sourceRevision']==22
assert all(r[k]==v for k,v in expected.items() if k!='heightBits')
assert r['transform']==dict(origin=[-1.5,0,0],x=[1,0,0],y=[0,1,0],z=[0,0,1])
assert h['GeneratedFaceSha256']==data['zeroHash'] and c['GeneratedFaceSha256']==r['GeneratedFaceSha256']==data['variedHash']
if race['gpu']:
 assert h['gpuFaceSha256']==data['zeroHash'] and c['gpuFaceSha256']==r['gpuFaceSha256']==data['variedHash']
raw={k:base64.b64decode(v) for k,v in data['raw'].items()};assert len(raw)==data['reads']==76 and sum(map(len,raw.values()))==data['bytes']==5940390
for profile in data['oracle']:
 for e in profile['evidence']:
  verify_preparation(raw,e['blueprint'],e['prepared']);verify(raw,e['blueprint'],e['prepared'],e['composed']);assert e['composed']['sha256']==profile['sha256']
text=log.read_text(encoding='utf-8-sig');assert 'PASS: customization Height race retirement='+race['retirement'] in text
assert 'PASS: customization teardown roots=1/1 archives=1 drain_complete=true' in text
receipt=dict(six_scalar_bytes_agree=True,height_once=True,pose_clock_source_ownership_retained=True,peak_active=1,gpu=race['gpu'],retirement=race['retirement'],original_face_oracle=True)
if len(sys.argv)==5:
 stem=Path(sys.argv[4]);paths=[stem.with_name(stem.stem+s+'.png') for s in ('-held','-committed','-reset')]
 a,b,z=[np.asarray(Image.open(p).convert('RGBA')) for p in paths];assert a.shape==b.shape==z.shape==(720,1280,4)
 background=a[0,0];masks=[np.any(v!=background,axis=2) for v in (a,b,z)];assert np.array_equal(masks[0],masks[1])
 diff=np.any(a!=b,axis=2);assert diff.sum()>10000 and not diff[~masks[0]].any()
 reset_changed=np.logical_xor(masks[1],masks[2]);assert reset_changed.sum()>10000
 receipt.update(changed_appearance_pixels=int(diff.sum()),same_held_committed_silhouette=True,unchanged_background=True,reset_silhouette_pixels=int(reset_changed.sum()))
print(json.dumps(receipt))
