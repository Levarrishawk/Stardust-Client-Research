"""Independent supplied names, static raw geometry/DDS, GPU readback and pixel oracle."""
import base64,contextlib,hashlib,io,json,struct,sys
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_static_mesh_slice import inspect as mesh
from verify_dds_pixels import inspect as pixels
export,peer,log=map(Path,sys.argv[1:4]);data=json.loads(export.read_text());fixture=json.loads(peer.read_text())
assert data['inputPolicy']=='SuppliedProjectionOpaqueTano'
def names(key):
 p=bytes.fromhex(fixture[key]);assert struct.unpack_from('<HIQIBIH',p)==(5,0x68A75F0C,0xF122334455667788,0x4352454F,3,len(p)-23,18)
 cursor=29
 def read(fmt):
  nonlocal cursor
  v=struct.unpack_from('<'+fmt,p,cursor)[0];cursor+=struct.calcsize('<'+fmt);return v
 def narrow():
  nonlocal cursor
  n=read('H');n=read('I') if n==65535 else n;v=p[cursor:cursor+n];cursor+=n;return v
 file=narrow();spacer=read('I');key=narrow();units=[read('H') for _ in range(read('I'))]
 assert file==b'diagnostic' and p[cursor:]==bytes.fromhex(fixture['human_baseline'])[41:]
 return dict(rawFile=file.hex().upper(),rawKey=key.hex().upper(),NameSpacer=spacer,units=units),''.join(map(chr,units)) if units else 'FIRST TERMINAL'
f,l,h,s=data['states']
for state,fixture_key,revision,visibility,resolves,ticks,applies in [(f,'name_first_baseline',1,255,1,0,0),(l,'name_latest_baseline',2,255,2,0,1),(h,'name_latest_baseline',3,0,2,2,1),(s,'name_latest_baseline',4,255,2,3,1)]:
 raw,text=names(fixture_key);assert all(state[k]==v for k,v in raw.items()) and state['text']==text
 assert (state['revision'],state['visibility'],state['resolves'],state['Ticks'],state['Applies'])==(revision,visibility,resolves,ticks,applies)
 assert state['rootVisible']==state['labelVisible']==bool(visibility)
 assert state['labelId']==f['labelId']!=0 and state['pose']==f['pose'] and state['identities']==f['identities']
raw={k:base64.b64decode(v) for k,v in data['raw'].items()};e=data['evidence']
assert len(raw)==e['ExternalReads']==27 and sum(map(len,raw.values()))==e['SourceBytes']==1401794 and set(e['counts'])==set(raw) and set(e['counts'].values())=={1}
for source in e['sources']:
 b=raw[source['Path']];assert len(b)==source['Bytes'] and hashlib.sha256(b).hexdigest().upper()==source['Sha256']
assert len(e['sources'])==27 and len(e['meshes'])==4 and len(e['textures'])==7 and len(e['materials'])==5
for item in e['meshes']:
 parsed=mesh(raw[item['path']]);assert (parsed['vertices'],parsed['triangles'],parsed['geometry_sha256'])==(item['vertices'],item['triangles'],item['hash'])
for item in e['textures']:
 with contextlib.redirect_stdout(io.StringIO()):expected=pixels(raw[item['path']])
 assert expected==item['mips']
 if data['gpu'] and item['path'] in data['readback']:assert expected==data['readback'][item['path']]
if data['gpu']:assert set(data['readback'])=={binding['AssetPath'] for material in e['materials'] for binding in material['textures'] if binding['Tag']==0x4D41494E}
assert e['frame']==0 and e['selectedPath']=='texture/anim_mission_terminal_01.dds'
text=log.read_text(encoding='utf-8-sig');assert 'PASS: static guards tests=18' in text and 'retirement='+data['retirement']+' root=1/1 archive=1' in text and 'PASS: supplied static state complete' in text
assert 'PASS: static additional guards stale_apply=true late_owner=true' in text
receipt=dict(raw_names=True,raw255=True,exact_name_identity_cache=True,unchanged_pose_resources=True,sources=27,bytes=1401794,independent_meshes=4,independent_dds=7,gpu_readback=data['gpu'],readback_textures=len(data['readback']),retirement=data['retirement'])
if len(sys.argv)==5:
 stem=Path(sys.argv[4]);first,latest,hidden,shown=[np.asarray(Image.open(stem.with_name(stem.stem+suffix+'.png')).convert('RGBA')) for suffix in ('-first','-latest','-hidden','-shown')]
 assert all(v.shape==(720,1280,4) for v in (first,latest,hidden,shown));background=first[0,0]
 changed=np.any(first[:180]!=latest[:180],axis=2);assert changed.sum()>500 and np.array_equal(first[180:],latest[180:])
 body=np.any(first[180:]!=background,axis=2);assert body.sum()>5000 and np.all(hidden==background) and np.array_equal(latest,shown)
 receipt.update(caption_changed_pixels=int(changed.sum()),unchanged_body_pixels=int(body.sum()),hidden_background_only=True,shown_equals_latest=True)
print(json.dumps(receipt))
