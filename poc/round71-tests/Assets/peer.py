"""Independent QCHN byte fixtures, F32 packed decoding and double interpolation before native."""
from pathlib import Path
import hashlib,json,math,struct,sys
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from verify_textured_mesh_bundle import Index
from pst_reference_audit import configured_archives
from verify_terrain_shader_family_selector import dependency
SHA='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
SELECTED=['appearance/animation/rabbit_std_turn_right.ans','appearance/animation/all_b_cbt_hop_turn_right_90.ans']
PINNED={SELECTED[0]:'87A9704BE53F7FBEA888864FF6A526C4DCFC582B8B4ABBA19E736D2BB8CC1B55',SELECTED[1]:'0139B7B7547466E69BF87928C9C3B59F17F259A005F9023717E11347B926E79F'}
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def chunk(t,b):return t+struct.pack('>I',len(b))+b
def form(t,b):return chunk(b'FORM',t+b)
def track(keys,selectors=(254,254,254)):return struct.pack('<H3B',len(keys),*selectors)+b''.join(struct.pack('<HI',*k) for k in keys)
def clip(suffix=b'',nested=False):
 info=struct.pack('<f6h',30,10,0,int(nested),0,0,0)
 return form(b'CKAT',form(b'0001',chunk(b'INFO',info)+form(b'XFRM',b'')+(form(b'AROT',chunk(b'QCHN',track([(0,0),(10,0)]))) if nested else b'')+suffix))
def children(b,start,end):
 result=[]
 while start<end:
  t=b[start:start+4];n=struct.unpack_from('>I',b,start+4)[0];stop=start+8+n
  if stop>end:raise ValueError('Extent')
  result.append((t,b[start+8:stop]));start=stop
 if start!=end:raise ValueError('Tail')
 return result

def decode(word,s):
 values=[]
 for axis,(field,selector) in enumerate(zip(((word>>21)&2047,(word>>10)&2047,word&1023),s)):
  assert 128<=selector<=254
  level=next(i for i,start in enumerate((254,252,248,240,224,192,128)) if selector>=start)
  ordinal=selector-(254,252,248,240,224,192,128)[level];width=f32(2/((1<<level)+1));base=f32((ordinal+1)*width-1)
  scale=f32(width*f32(1/(511 if axis==2 else 1023)));sign=512 if axis==2 else 1024
  values.append(f32(base+(-1 if field&sign else 1)*(field&(sign-1))*scale))
 rad=1-sum(v*v for v in values);q=values+[f32(math.sqrt(max(0,rad)))];norm=math.sqrt(sum(v*v for v in q))
 return [v/norm for v in q],rad<0

def sample(keys,s,frame):
 if frame<=keys[0][0]:return decode(keys[0][1],s)
 if frame>=keys[-1][0]:return decode(keys[-1][1],s)
 hi=next(i for i,k in enumerate(keys) if k[0]>frame);lo=hi-1
 if frame==keys[lo][0]:return decode(keys[lo][1],s)
 a,ra=decode(keys[lo][1],s);b,rb=decode(keys[hi][1],s);t=f32((frame-keys[lo][0])/(keys[hi][0]-keys[lo][0]));dot=sum(x*y for x,y in zip(a,b))
 if dot<0:b=[-v for v in b];dot=-dot
 if dot>0.999999:q=[(1-t)*x+t*y for x,y in zip(a,b)]
 else:
  angle=math.acos(min(1,dot));q=[(math.sin((1-t)*angle)*x+math.sin(t*angle)*y)/math.sin(angle) for x,y in zip(a,b)]
 norm=math.sqrt(sum(v*v for v in q));return [v/norm for v in q],ra or rb

def record(name,b,original=False,priority=None):
 fields=children(b,24,len(b));raw=next((v for t,v in fields if t==b'QCHN'),None);end=struct.unpack_from('<h',dict(fields)[b'INFO'],4)[0]
 s=tuple(raw[2:5]) if raw else ();keys=list(struct.iter_unpack('<HI',raw[5:])) if raw else []
 grid=[0,keys[0][0],end*.25,end*.5,keys[-1][0],end,end*.5,0] if keys else []
 return dict(Name=name,Bytes=b.hex(),Sha256=sha(b),Original=original,Priority=priority,End=end,Selectors=s,Keys=keys,Samples=[dict(Frame=f,Quaternion=sample(keys,s,f)[0],Repaired=sample(keys,s,f)[1]) for f in grid],HasRotation=bool(keys),HasLoct=any(t==b'LOCT' for t,v in fields))

def generate(exe,assetroot,config,out):
 _,_,_,paths=dependency();destination=paths.output_path(out);destination.mkdir(parents=True)
 assert sha(exe.read_bytes())==SHA and sha(config.read_bytes())==CONFIG
 valid=[]
 for name,keys,selectors in [('identity',[(2,0),(8,0)],(254,254,254)),('turn',[(2,0),(8,1023<<10)],(254,254,254)),('antipodal',[(2,1000<<21),(8,2024<<21)],(254,254,254)),('repair',[(2,4192340735),(8,4192340735)],(252,252,232)),('raw-nan-bits',[(2,0xffffffff),(8,0xffffffff)],(254,254,254))]:
  valid.append(record(name,clip(chunk(b'QCHN',track(keys,selectors)))))
 valid+=[record('absent',clip()),record('nested-only',clip(nested=True))]
 pending=set(SELECTED)
 for priority,archive in configured_archives(assetroot,config):
  if not pending:break
  with archive.open('rb') as stream:
   index=Index(stream)
   for name in SELECTED:
    if name not in pending:continue
    if name in index.entries:
     b=index.lookup(name);assert b is not None
     if PINNED:assert sha(b)==PINNED[name]
     valid.append(record(name,b,True,priority));pending.remove(name)
 assert not pending
 originals={v['Name']:v for v in valid if v['Original']};assert not originals[SELECTED[0]]['HasLoct'] and originals[SELECTED[1]]['HasLoct']
 invalid=[]
 def bad(name,b):invalid.append(dict(Name=name,Bytes=b.hex()))
 good=track([(2,0),(8,1023<<10)])
 for n in range(5):bad('header-'+str(n),clip(chunk(b'QCHN',good[:n])))
 for name,raw in [('truncated',good[:-1]),('extra',good+b'x'),('one',track([(0,0)])),('zero',track([])),('duplicate-time',track([(2,0),(2,0)])),('descending',track([(8,0),(2,0)])),('past-end',track([(0,0),(11,0)])),('unsupported-low',track([(0,0),(10,0)],(127,254,254))),('unsupported-high',track([(0,0),(10,0)],(254,255,254))),('unsupported-z',track([(0,0),(10,0)],(254,254,0))),('huge-count',struct.pack('<H3B',65535,254,254,254))]:bad(name,clip(chunk(b'QCHN',raw)))
 bad('duplicate',clip(chunk(b'QCHN',good)*2));bad('form-qchn',clip(form(b'QCHN',b'')));bad('qchn-before-loct',clip(chunk(b'QCHN',good)+chunk(b'LOCT',b'')))
 b=bytearray(clip(chunk(b'QCHN',good)));b[20:24]=b'0002';bad('version',b)
 bad('outer-truncation',b[:-1]);bad('empty',b'')
 data=dict(Valid=valid,Invalid=invalid,Tolerance=2e-6,IndependentBeforeNative=True,OriginalSources=[{k:x[k] for k in ('Name','Sha256','Priority','HasLoct')} for x in valid if x['Original']])
 (destination/'fixtures.json').write_text(json.dumps(data,indent=2)+'\n');print(json.dumps(data['OriginalSources'],indent=2))

def verify(out):
 f=json.loads((out/'fixtures.json').read_text());n=json.loads((out/'native.json').read_text());errors=[];maximum=0;count=0
 assert len(n['Tracks'])==len(f['Valid'])
 for expected,actual in zip(f['Valid'],n['Tracks']):
  for key in ('Name','Sha256','End','HasRotation'):assert expected[key]==actual[key],(key,expected['Name'])
  assert expected['Keys']==actual['Keys'] and list(expected['Selectors'])==actual['Selectors']
  assert len(expected['Samples'])==len(actual['Samples'])
  for a,b in zip(expected['Samples'],actual['Samples']):
   q=b['Quaternion'];e=a['Quaternion'];err=min(max(abs(x-y) for x,y in zip(q,e)),max(abs(x+y) for x,y in zip(q,e)));maximum=max(maximum,err);count+=1
   assert err<=f['Tolerance'] and a['Repaired']==b['Repaired'],(expected['Name'],a,b,err)
 assert n['RejectedMalformed']==len(f['Invalid']) and n['Passed'] and n['Checks']==242
 receipt=dict(Passed=True,ComparedSamples=count,MaximumQuaternionError=maximum,Tolerance=f['Tolerance'],MalformedFixtures=len(f['Invalid']),NativeChecks=n['Checks'],OversizedSourceNegatives=1,OriginalSources=f['OriginalSources'],OriginalExecution=False,IndependentBeforeNative=True)
 (out/'comparison-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*map(Path,sys.argv[2:]))
 else:verify(Path(sys.argv[2]))
