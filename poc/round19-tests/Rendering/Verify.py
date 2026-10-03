import sys,json,base64
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_face_composition import verify,verify_bindings
export,default,generated=map(Path,sys.argv[1:4])
data=json.loads(export.read_text());raw={k:base64.b64decode(v) for k,v in data['raw'].items()}
assert (data['defaultReads'],data['generatedReads'],data['defaultBytes'],data['generatedBytes'])==(39,76,4686371,5940390)
assert len(raw)==76 and sum(map(len,raw.values()))==5940390
results=[verify(raw,e['blueprint'],e['prepared'],e['composed']) for e in data['evidence']]
assert len(results)==1 and results[0]['layers']==5
binding=dict(data['binding']);binding['sha256']=data['evidence'][0]['composed']['sha256']
verify_bindings(raw,[binding['MeshPath']],[e['composed'] for e in data['evidence']],[binding])
a,b=[np.asarray(Image.open(p).convert('RGBA')) for p in (default,generated)]
assert a.shape==b.shape==(720,1280,4)
diff=np.any(a!=b,axis=2);ys,xs=np.where(diff);assert len(xs)>100
assert np.all((xs>=610)&(xs<665)&(ys>=135)&(ys<220)),(xs.min(),ys.min(),xs.max(),ys.max())
assert np.array_equal(a[225:700,550:725],b[225:700,550:725])
assert np.array_equal(a[135:148,610:665],b[135:148,610:665])
print(json.dumps({'cpu':results,'raw_head_destination':True,'generated_sha256':binding['sha256'],'changed_pixels':len(xs),'difference_bounds':[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)],'body_unchanged':True}))
