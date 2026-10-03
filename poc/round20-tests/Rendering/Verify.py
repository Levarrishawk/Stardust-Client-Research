"""Independent original-byte preparation/composition and fixed-frame GPU color evidence."""
import sys,json,base64
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_face_composition import verify
from verify_face_resources import verify_preparation
export,zero,varied=map(Path,sys.argv[1:4])
data=json.loads(export.read_text(encoding='utf-8-sig'));raw={k:base64.b64decode(v) for k,v in data['raw'].items()}
assert data['reads']==len(raw)==76 and data['bytes']==sum(map(len,raw.values()))==5940390
expected={'zero':'A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808','varied':'60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5'}
results=[]
for profile in data['oracle']:
 assert profile['sha256']==expected[profile['name']]
 assert len(profile['evidence'])==1
 for e in profile['evidence']:
  preparation=verify_preparation(raw,e['blueprint'],e['prepared'])
  composed=verify(raw,e['blueprint'],e['prepared'],e['composed'])
  assert e['composed']['sha256']==profile['sha256']
  results.append(dict(name=profile['name'],preparation=preparation,composition=composed,sha256=profile['sha256']))
assert data['zeroHash']==expected['zero'] and data['variedHash']==expected['varied']
a,b=[np.asarray(Image.open(p).convert('RGBA')) for p in (zero,varied)]
assert a.shape==b.shape==(720,1280,4)
background=a[0,0];ma=np.any(a!=background,axis=2);mb=np.any(b!=background,axis=2)
assert np.array_equal(ma,mb),'Fixed pose silhouette changed'
diff=np.any(a!=b,axis=2);face=int(diff[135:220,610:665].sum());body=int(diff[225:700,550:725].sum())
assert face>1000 and body>10000
assert not diff[~ma].any()
print(json.dumps(dict(independent=results,changed_pixels=int(diff.sum()),face_changed=face,body_changed=body,silhouette_unchanged=True,background_unchanged=True)))
