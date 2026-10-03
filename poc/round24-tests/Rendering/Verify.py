"""Independent baseline bytes, hidden transaction receipts, original face and GPU pixels."""
import base64,json,struct,sys
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_face_composition import verify
from verify_face_resources import verify_preparation
export,peer,log=map(Path,sys.argv[1:4]);data=json.loads(export.read_text());fixture=json.loads(peer.read_text())
race=data['visibilityRace'];assert race['inputPolicy']=='CompleteBaselineReseed' and race['createdHidden']
original=bytes.fromhex(fixture['human_baseline']);fields=struct.unpack('<fHIHIIHIIIIIIBBBQfIQII',original[25:])
assert fields==(1,0,0,0,0,1,0,0,7,0,0,0,100,1,4,9,0xF102030405060708,1,0xF1234567,0xFEDCBA9876543210,0,9)
offset=25+struct.calcsize('<fHIHIIHIIIIII');assert offset==71
for name,value in [('visibility_hidden_baseline',0),('visibility_shown_baseline',255)]:
 packet=bytes.fromhex(fixture[name]);assert struct.unpack_from('<HIQIBIH',packet)==(5,0x68A75F0C,0xF122334455667788,0x4352454F,3,len(packet)-23,18)
 assert packet[:offset]==original[:offset] and packet[offset]==value and packet[offset+1:]==original[offset+1:]
b,h,c,s=[race[k] for k in ('before','held','committed','shown')]
expected=dict(Posture=fields[14],Rank=fields[15],LinkId=fields[16],heightBits=0x3f800000,Shock=fields[18],States=fields[19])
for state,raw,shown,revision,appearance,source in [(b,1,True,2,1,1),(h,0,False,3,1,4),(c,0,False,3,4,4),(s,255,True,4,4,4)]:
 assert all(state[k]==v for k,v in expected.items())
 assert state['visibility']==raw and state['visible']==shown and state['Revision']==revision and state['LastAppliedRevision']==appearance
 assert state['sourceRevision']==source and state['sourceValues']==8 and state['Frame']==3 and state['SelectedClip']=='walk'
 assert state['transform']==dict(origin=[-1.5,.2,-.1],x=[1,0,0],y=[0,1,0],z=[0,0,1])
 assert state['pose']==b['pose'] and state['ownership']==b['ownership'] and state['peakActive']==1
 assert state['GeneratedFaceSha256']==(data['zeroHash'] if appearance==1 else data['variedHash'])
 if race['gpu']:assert state['gpuFaceSha256']==state['GeneratedFaceSha256']
assert h['active']==1 and c['active']==s['active']==0
raw={k:base64.b64decode(v) for k,v in data['raw'].items()};assert len(raw)==data['reads']==76 and sum(map(len,raw.values()))==data['bytes']==5940390
for profile in data['oracle']:
 for e in profile['evidence']:
  verify_preparation(raw,e['blueprint'],e['prepared']);verify(raw,e['blueprint'],e['prepared'],e['composed']);assert e['composed']['sha256']==profile['sha256']
text=log.read_text(encoding='utf-8-sig');assert 'PASS: customization visibility race retirement='+race['retirement'] in text
assert 'hidden_retirement_drain=true baseline_reseed_not_delta=true' in text and 'PASS: customization teardown roots=1/1 archives=1 drain_complete=true' in text
receipt=dict(complete_baseline_bytes_agree=True,no_visibility_delta_claim=True,created_hidden=True,hidden_latest_commit=True,show_latest_without_reload=True,raw255=True,pose_clock_source_resources_retained=True,peak_active=1,retirement=race['retirement'],gpu=race['gpu'],original_face_oracle=True)
if len(sys.argv)==5:
 stem=Path(sys.argv[4]);paths=[stem.with_name(stem.stem+suffix+'.png') for suffix in ('-visible-before','-hidden-held','-hidden-committed','-shown')]
 before,held,committed,shown=[np.asarray(Image.open(p).convert('RGBA')) for p in paths]
 assert all(v.shape==(720,1280,4) for v in (before,held,committed,shown));background=before[0,0]
 assert np.all(held==background) and np.array_equal(held,committed)
 mask=np.any(before!=background,axis=2);assert mask.sum()>10000 and np.array_equal(mask,np.any(shown!=background,axis=2))
 diff=np.any(before!=shown,axis=2);assert diff.sum()>5000 and not diff[~mask].any()
 receipt.update(hidden_background_only=True,hidden_held_committed_equal=True,restored_same_silhouette=True,restored_appearance_pixels=int(diff.sum()),visible_pixels=int(mask.sum()))
print(json.dumps(receipt))
