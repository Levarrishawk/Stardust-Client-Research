"""Independent Hamilton quaternion action and root-motion fixture oracle before native."""
from pathlib import Path
import importlib.util,json,math,struct,sys
REPO=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('rotation71',REPO/'poc/round71-tests/Assets/peer.py');old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
f32=old.f32

def mul(a,b):
 x,y,z,w=a;p,q,r,v=b
 return [w*p+x*v+y*r-z*q,w*q-x*r+y*v+z*p,w*r+x*q-y*p+z*v,w*v-x*p-y*q-z*r]
def conj(q):return [-q[0],-q[1],-q[2],q[3]]
def norm(q):
 n=math.sqrt(sum(x*x for x in q));return [x/n for x in q]
def loct(keys,speed=2):return struct.pack('<fh',speed,len(keys))+b''.join(struct.pack('<H3f',frame,*p) for frame,p in keys)
def position(keys,frame):
 if frame<=keys[0][0]:return keys[0][1]
 if frame>=keys[-1][0]:return keys[-1][1]
 hi=next(i for i,k in enumerate(keys) if k[0]>frame);a,va=keys[hi-1];b,vb=keys[hi];t=f32((frame-a)/(b-a))
 return [f32(x+f32(f32(y-x)*t)) for x,y in zip(va,vb)]
def transform(q,d):
 v=mul(mul(conj(q),[*d,0]),q)[:3];return list(reversed(v))
def delta(qkeys,s,pkeys,a,b):
 q0,r0=old.sample(qkeys,s,a) if qkeys else ([0,0,0,1],False);q1,r1=old.sample(qkeys,s,b) if qkeys else ([0,0,0,1],False)
 relative=norm(mul(q1,conj(q0)));d=[f32(y-x) for x,y in zip(position(pkeys,a),position(pkeys,b))] if pkeys else [0,0,0]
 if qkeys and pkeys:d=transform(q1,d)
 return relative,d,r0 or r1

def record(name,b):
 fields=old.children(b,24,len(b));raw=dict(fields).get(b'QCHN');qkeys=list(struct.iter_unpack('<HI',raw[5:])) if raw else [];s=list(raw[2:5]) if raw else []
 raw=dict(fields).get(b'LOCT');pkeys=[(f,list(p)) for f,*p in struct.iter_unpack('<H3f',raw[6:])] if raw else [];speed=struct.unpack_from('<f',raw)[0] if raw else 0
 end=struct.unpack_from('<h',dict(fields)[b'INFO'],4)[0];pairs=[(0,0),(0,end),(end,0),(end*.25,end*.75),(end*.75,end*.25),(end*.5,end*.5),(2,8),(8,2)]
 values=[dict(From=a,To=c,Rotation=delta(qkeys,s,pkeys,a,c)[0],Translation=delta(qkeys,s,pkeys,a,c)[1],Repaired=delta(qkeys,s,pkeys,a,c)[2]) for a,c in pairs]
 return dict(Name=name,Bytes=b.hex(),Sha256=old.sha(b),End=end,Mode=('Combined' if pkeys else 'Rotation') if qkeys else ('Translation' if pkeys else 'None'),Positions=pkeys,Speed=speed,Samples=values)

def generate(exe,assetroot,config,out):
 old.generate(exe,assetroot,config,out/'selected71');base=json.loads((out/'selected71/fixtures.json').read_text());valid=[]
 p=loct([(2,(1,2,3)),(8,(5,9,14))]);q=old.track([(2,512<<21),(8,1023<<10)])
 for name,suffix in [('none',b''),('translation',old.chunk(b'LOCT',p)),('rotation',old.chunk(b'QCHN',q)),('combined-order',old.chunk(b'LOCT',p)+old.chunk(b'QCHN',q))]:valid.append(record(name,old.clip(suffix)))
 for item in base['Valid']:
  if item['Original'] or item['Name'] in ('antipodal','repair'):valid.append(record(item['Name'],bytes.fromhex(item['Bytes'])))
 # The asymmetric combined fixture must actually discriminate wrong matrix source and unreversed output.
 q0,_=old.sample([(2,512<<21),(8,1023<<10)],(254,254,254),2);q1,_=old.sample([(2,512<<21),(8,1023<<10)],(254,254,254),8);d=[4,7,11]
 good=transform(q1,d);assert all(max(abs(x-y) for x,y in zip(good,bad))>1 for bad in [transform(q0,d),transform(norm(mul(q1,conj(q0))),d),list(reversed(good))])
 invalid=[]
 def bad(name,b):invalid.append(dict(Name=name,Bytes=b.hex()))
 for n in range(6):bad('loct-header-'+str(n),old.clip(old.chunk(b'LOCT',p[:n])))
 for name,raw in [('truncated',p[:-1]),('extra',p+b'x'),('one',loct([(0,(0,0,0))])),('zero',loct([])),('negative-count',struct.pack('<fh',1,-1)),('duplicate-time',loct([(2,(0,0,0)),(2,(1,2,3))])),('descending',loct([(8,(0,0,0)),(2,(1,2,3))])),('past-end',loct([(0,(0,0,0)),(11,(1,2,3))])),('speed-nan',loct([(0,(0,0,0)),(10,(1,2,3))],float('nan'))),('speed-negative',loct([(0,(0,0,0)),(10,(1,2,3))],-1)),('speed-limit',loct([(0,(0,0,0)),(10,(1,2,3))],1e6+1)),('position-nan',loct([(0,(0,0,0)),(10,(float('nan'),2,3))])),('position-inf',loct([(0,(0,0,0)),(10,(1,float('inf'),3))])),('position-limit',loct([(0,(0,0,0)),(10,(1,2,1e6+1))]))]:bad(name,old.clip(old.chunk(b'LOCT',raw)))
 bad('duplicate-loct',old.clip(old.chunk(b'LOCT',p)*2));bad('form-loct',old.clip(old.form(b'LOCT',b'')))
 invalid+=base['Invalid']
 data=dict(Valid=valid,Invalid=invalid,Tolerance=3e-6,OriginalSources=base['OriginalSources'],MatrixDiscriminators=3,IndependentBeforeNative=True)
 (out/'fixtures.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8');print('Independent root motion fixtures ready')

def verify(out):
 f=json.loads((out/'fixtures.json').read_text());n=json.loads((out/'native.json').read_text(),parse_int=lambda value:float(value) if value=='-0' else int(value));maximumQ=maximumP=0;count=0
 assert n['Passed'] and n['RejectedMalformed']==len(f['Invalid']) and len(n['Tracks'])==len(f['Valid'])
 for a,b in zip(f['Valid'],n['Tracks']):
  for key in ('Name','Sha256','Mode','End'):assert a[key]==b[key],(key,a['Name'])
  assert struct.pack('<f',a['Speed'])==struct.pack('<f',b['Speed']) and len(a['Positions'])==len(b['Positions'])
  for (af,ap),(bf,bp) in zip(a['Positions'],b['Positions']):assert af==bf and struct.pack('<3f',*ap)==struct.pack('<3f',*bp)
  assert len(a['Samples'])==len(b['Samples'])
  for x,y in zip(a['Samples'],b['Samples']):
   assert x['From']==y['From'] and x['To']==y['To'] and x['Repaired']==y['Repaired'];count+=1
   eq=min(max(abs(c-d) for c,d in zip(x['Rotation'],y['Rotation'])),max(abs(c+d) for c,d in zip(x['Rotation'],y['Rotation'])));ep=max(abs(c-d) for c,d in zip(x['Translation'],y['Translation']));maximumQ=max(maximumQ,eq);maximumP=max(maximumP,ep)
   assert eq<=f['Tolerance'] and ep<=f['Tolerance']*max(1,max(abs(v) for v in x['Translation'])),(a['Name'],x,y,eq,ep)
 receipt=dict(Passed=True,ComparedDeltas=count,MaximumQuaternionError=maximumQ,MaximumTranslationError=maximumP,Tolerance=f['Tolerance'],MalformedFixtures=len(f['Invalid']),NativeChecks=n['Checks'],MatrixDiscriminators=3,OriginalSources=f['OriginalSources'],IndependentBeforeNative=True,OriginalExecution=False)
 (out/'comparison-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt,indent=2))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*map(Path,sys.argv[2:]))
 else:verify(Path(sys.argv[2]))
