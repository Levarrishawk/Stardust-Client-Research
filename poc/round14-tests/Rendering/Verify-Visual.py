"""Independent raw placement, lifetime metadata and whole-terminal pixels; original effects unknown."""
import json,math,struct,sys
from pathlib import Path
from PIL import Image,ImageChops
fixture,native,gpu,headless,cancel,first,second=map(Path,sys.argv[1:])
f={k:bytes.fromhex(v) for k,v in json.loads(fixture.read_text(encoding='utf-8-sig')).items()};assert len(f)==6
A=0xF122334455667788;B=0xE122334455667799
for name,oid,x in [('a',A,-2),('b',B,8)]:
 assert struct.unpack('<HIQ7fIB',f[name+'_create'])==(5,0xFE89DDEA,oid,struct.unpack('<f',struct.pack('<f',.2))[0],-0.,struct.unpack('<f',struct.pack('<f',.3))[0],struct.unpack('<f',struct.pack('<f',1.7))[0],x,7,4,2907007746,255)
 assert struct.unpack_from('<I',f[name+'_create'],18)[0]==0x80000000
 assert struct.unpack('<HIQ',f[name+'_end'])==(2,0x2C436037,oid)
 assert struct.unpack('<HIQB',f[name+'_destroy'])==(3,0x4D45D504,oid,255)
x,y,z,w=struct.unpack_from('<4f',f['a_create'],14);norm=math.sqrt(x*x+y*y+z*z+w*w);x,y,z,w=[v/norm for v in [x,y,z,w]]
# Derive rotation from quaternion-vector cross products rather than copying matrix equations.
def rotate(v):
 def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
 q=[x,y,z];t=[2*a for a in cross(q,v)];u=cross(q,t);return [v[i]+w*t[i]+u[i] for i in range(3)]
core_cols=[rotate(v) for v in [[1,0,0],[0,1,0],[0,0,1]]];core_rows=[list(row) for row in zip(*core_cols)]
engine_cols=[[component*(1 if i<2 else -1)*(1 if j<2 else -1) for j,component in enumerate(column)] for i,column in enumerate(core_cols)]
rows=lambda text,p:[json.loads(s[len(p):]) for s in text.splitlines() if s.startswith(p)]
ntext=native.read_text(encoding='utf-8-sig');npose=rows(ntext,'static_native_pose=')[0];validation=rows(ntext,'static_native_validation=')[0]
assert validation['negatives']==68 and validation['truncations']==47 and validation['ownedBuffers']==54 and validation['parsedSourcesUnchanged'] and not validation['gpu']
assert npose['AppliedNormalization'] and math.isclose(npose['InputQuaternionNorm'],norm,abs_tol=1e-12) and npose['origin']==[-2,4,7]
for a,b in zip(npose['rows'],core_rows):assert all(math.isclose(x,y,abs_tol=1e-6) for x,y in zip(a,b))
for path in [gpu,headless]:
 text=path.read_text(encoding='utf-8-sig');states=rows(text,'static_state=');assert [s['stage'] for s in states]==['initial','recreated']
 assert [(s['id'],s['handle'],s['incarnation'],s['detail'],s['meshes'],s['textures']) for s in states]==[(A,2,1,3,4,7),(A,6,5,3,4,7)]
 assert states[0]['ids']!=states[1]['ids']
 for state in states:
  assert state['pose']['origin']==[-2,4,-7]
  for a,b in zip(state['pose']['columns'],engine_cols):assert all(math.isclose(x,y,abs_tol=1e-6) for x,y in zip(a,b))
 v,=rows(text,'static_validation=');assert v['negatives']==22 and v['selections']==16 and v['frame']==0 and v['meshInstances']==4 and v['textures']==7 and v['screenOverrides']==2 and v['strictCpu'] and v['otherIdsFixed'] and v['injectedPredicate'] and v['closed']==1 and v['workerThread']!=v['ownerThread']
 assert 'PASS: static teardown roots_released=true adapters_disposed=true predicates_released=true worker_observed=true archives_closed=true allocations=7 disposals=7' in text
assert 'PASS: static canceled publications=0 archives_closed=true worker_observed=true drain_complete=true' in cancel.read_text(encoding='utf-8-sig')
images=[Image.open(p).convert('RGB') for p in [first,second]];assert all(i.size==(1280,720) for i in images)
crops=[i.crop((0,100,1280,720)) for i in images];background=crops[0].getpixel((0,0));assert crops[0].tobytes()==crops[1].tobytes(),'Recreated immutable frame0 should have identical pixels'
diff=ImageChops.difference(crops[0],Image.new('RGB',crops[0].size,background));box=diff.getbbox();assert box is not None and box[0]>20 and box[1]>20 and box[2]<1260 and box[3]<600
rgb=diff.tobytes();pixels=sum(any(rgb[i:i+3]) for i in range(0,len(rgb),3));assert pixels>5000
# Actual warm screen pixels must appear within the whole rendered terminal, beyond UI.
raw=crops[0].tobytes();warm=sum(raw[i]>raw[i+1]+10 and raw[i+1]>raw[i+2]+10 for i in range(0,len(raw),3));assert warm>100
print('PASS: independent raw47/nonunit quaternion/SRS placement; native68/Godot22 negatives; exact lifecycle revisions/injected guards; frame0 four retained LODs/7 textures; teardown/cancel')
print('PASS: independent whole terminal pixels='+str(pixels)+' bbox='+str(box)+' warm_screen_pixels='+str(warm)+' recreated_crop_identical=true camera_fixed=true')
