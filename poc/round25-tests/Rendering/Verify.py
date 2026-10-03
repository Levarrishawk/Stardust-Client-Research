"""Independent complete name bytes, supplied diagnostic caption, transaction/GPU audit."""
import base64,json,struct,sys
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_face_composition import verify
from verify_face_resources import verify_preparation
export,peer,log=map(Path,sys.argv[1:4]);data=json.loads(export.read_text());fixture=json.loads(peer.read_text())
race=data['nameRace'];assert race['inputPolicy']=='CompleteBaselineReseed' and race['resolverPolicy']=='SyntheticCustomThenExactStringId'
def parse(name):
 p=bytes.fromhex(fixture[name]);assert struct.unpack_from('<HIQIBIH',p)==(5,0x68A75F0C,0xF122334455667788,0x4352454F,3,len(p)-23,18)
 assert p[25:29]==struct.pack('<f',1);cursor=29
 def read(fmt):
  nonlocal cursor
  value=struct.unpack_from('<'+fmt,p,cursor)[0];cursor+=struct.calcsize('<'+fmt);return value
 def narrow():
  nonlocal cursor
  n=read('H');n=read('I') if n==65535 else n;result=p[cursor:cursor+n];cursor+=n;return result
 file=narrow();spacer=read('I');key=narrow();count=read('I');units=[read('H') for _ in range(count)]
 original=bytes.fromhex(fixture['human_baseline']);tail=bytearray(original[41:]);tail[30]=0 if name=='name_hidden_baseline' else 1
 assert p[cursor:]==tail
 text=''.join(map(chr,units)) if units else {b'alpha':'FIRST CAPTION',b'beta':'SECOND CAPTION'}[key]
 assert file==b'diagnostic'
 return dict(rawFile=file.hex().upper(),rawKey=key.hex().upper(),NameSpacer=spacer,units=units),text
f,h,c,z,s=[race[k] for k in ('first','held','committed','hidden','shown')]
for state,fixture_name,appearance,visible,resolves in [(f,'name_first_baseline',1,True,2),(h,'name_latest_baseline',1,True,3),(c,'name_latest_baseline',4,True,3),(z,'name_hidden_baseline',4,False,4),(s,'name_latest_baseline',4,True,5)]:
 expected,text=parse(fixture_name);assert all(state['name'][k]==v for k,v in expected.items()) and state['name']['text']==text
 assert state['name']['labelId']==f['name']['labelId']!=0 and state['name']['labelVisible']==visible and state['visible']==visible
 assert state['name']['nameResolves']==resolves and state['sourceRevision']==4 and state['sourceValues']==8
 assert state['pose']==f['pose'] and state['ownership']==f['ownership'] and state['Frame']==3 and state['SelectedClip']=='walk'
 assert state['transform']==dict(origin=[-1.5,.2,-.1],x=[1,0,0],y=[0,1,0],z=[0,0,1]) and state['heightBits']==0x3f800000
 assert state['LastAppliedRevision']==appearance and state['peakActive']==1
 assert state['GeneratedFaceSha256']==(data['zeroHash'] if appearance==1 else data['variedHash'])
 if race['gpu']:assert state['gpuFaceSha256']==state['GeneratedFaceSha256']
assert h['Revision']==f['Revision']+1 and c['Revision']==h['Revision'] and z['Revision']==c['Revision']+1 and s['Revision']==z['Revision']+1
assert f['active']==h['active']==1 and c['active']==z['active']==s['active']==0
raw={k:base64.b64decode(v) for k,v in data['raw'].items()};assert len(raw)==data['reads']==76 and sum(map(len,raw.values()))==data['bytes']==5940390
for profile in data['oracle']:
 for e in profile['evidence']:
  verify_preparation(raw,e['blueprint'],e['prepared']);verify(raw,e['blueprint'],e['prepared'],e['composed']);assert e['composed']['sha256']==profile['sha256']
text=log.read_text(encoding='utf-8-sig');assert 'PASS: customization name race retirement='+race['retirement'] in text
assert 'PASS: name guards ctor_ownership=true cache=true stale_apply=true callback_mutation_reentry_failure=true label_inner_once=true' in text
assert 'PASS: customization teardown roots=1/1 archives=1 drain_complete=true' in text
receipt=dict(complete_name_bytes_agree=True,diagnostic_policy_only=True,no_name_delta_claim=True,latest_label=True,raw_name_cache_pose_clock_source_resources_retained=True,hidden_inherited=True,guards=True,peak_active=1,gpu=race['gpu'],retirement=race['retirement'],original_face_oracle=True)
if len(sys.argv)==5:
 stem=Path(sys.argv[4]);paths=[stem.with_name(stem.stem+suffix+'.png') for suffix in ('-first','-latest-held','-latest-committed','-hidden','-shown')]
 first,held,committed,hidden,shown=[np.asarray(Image.open(p).convert('RGBA')) for p in paths]
 assert all(v.shape==(720,1280,4) for v in (first,held,committed,hidden,shown));background=first[0,0]
 caption=np.any(first[:240]!=held[:240],axis=2);assert caption.sum()>500 and np.array_equal(first[240:],held[240:])
 assert np.array_equal(held[:240],committed[:240]) and np.array_equal(shown,committed) and np.all(hidden==background)
 masks=[np.any(v[240:]!=background,axis=2) for v in (held,committed)];assert np.array_equal(*masks)
 body=np.any(held[240:]!=committed[240:],axis=2);assert body.sum()>5000 and not body[~masks[0]].any()
 receipt.update(caption_changed_pixels=int(caption.sum()),appearance_changed_pixels=int(body.sum()),same_body_during_name_change=True,same_caption_during_appearance=True,hidden_background_only=True,shown_equals_committed=True)
print(json.dumps(receipt))
