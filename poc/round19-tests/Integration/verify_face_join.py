"""Compare continuous face publication to independently verified native resource receipt."""
import json,sys,hashlib
from pathlib import Path
def records(path,key):
 return [json.loads(s.split('=',1)[1]) for s in Path(path).read_text(encoding='utf-8-sig').splitlines() if s.startswith(key+'=')]
lead,=records(sys.argv[1],'face_resource')
native,=records(sys.argv[2],'face_resource')
route,=records(sys.argv[2],'face_route')
assert lead['ExternalReads']==native['ExternalReads']==len(lead['Sources'])
assert lead['SourceBytes']==native['SourceBytes']==sum(s['Bytes'] for s in lead['Sources'])
assert lead['Sources']==native['Sources'] and lead['Bindings']==native['Bindings']
assert lead['faceProfileSha256']==native['profileSHA256']==hashlib.sha256(Path(sys.argv[3]).read_bytes()).hexdigest().upper()
outputs=[dict(RendererPath=p['Renderer'],Width=p['Width'],Height=p['Height'],Bytes=p['Bytes'],Mip0Sha256=p['Sha256']) for p in lead['Outputs']]
assert outputs==native['Outputs'] and len(outputs)==1
assert outputs[0]['Width']==outputs[0]['Height']==256 and outputs[0]['Bytes']==256*256*4
images={}
for row in route['MaterialTextures']+route['FaceTextures']:
 if row['path'] in images:assert row['mips']==images[row['path']]
 else:images[row['path']]=row['mips']
a=lead['Accounting'];generated=sum(p['Bytes'] for p in outputs)
assert a['Sources']==lead['ExternalReads'] and a['SourceBytes']==lead['SourceBytes']
assert a['DecodedTextures']==len(images) and a['Mips']==sum(len(m) for m in images.values())
assert a['DecodedTextureBytes']==sum(m['Bytes'] for rows in images.values() for m in rows)
assert a['GeneratedFaces']==1 and a['GeneratedFaceBytes']==generated
assert a['DecodedTextureBytes']+generated==route['DecodedBytes']
assert a['PendingWeight']==lead['SourceBytes']+route['DecodedBytes']
text=Path(sys.argv[1]).read_text(encoding='utf-8-sig')
assert 'PASS: pumped generated_face=true' in text and 'PASS: pumped local_input=true' in text
print(json.dumps(dict(passed=True,sources=lead['ExternalReads'],source_bytes=lead['SourceBytes'],decoded_inputs=a['DecodedTextureBytes'],generated_bytes=generated,pending_weight=a['PendingWeight'],exact_source_and_main_binding=True)))

