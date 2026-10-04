"""Independent synthetic current raw presence and binary32 planar admission oracle."""
import json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def wire(entries):
 body=struct.pack('<H',65535)+b''.join(struct.pack('<H'+('Q' if i==6 else 'I'),i,b) for i,b in entries)
 return (struct.pack('<HIQIBI',5,0x12862153,42,0x4352454f,4,len(body))+body).hex().upper()
def generate(path):
 old=json.loads((ROOT/'poc/round48-tests/Runtime/ordered-fixtures.json').read_text(encoding='utf-8'));stages=[]
 for name,entries in [('bootstrap',None),('listen-first',[(6,123)]),('valid',[(7,bits(3)),(11,bits(8))]),('zero',[(7,0),(11,0x80000000)]),('nonfinite',[(7,0x7FC01234),(11,0x7F800000)]),('negative',[(7,bits(-2)),(11,bits(-1))]),('large',[(7,0x7F7FFFFF),(11,0x7F7FFFFF)]),('subnormal',[(7,1),(11,1)]),('restored',[(7,bits(4)),(11,bits(9))])]:stages.append(dict(Name=name,Entries=entries,Wire=None if entries is None else wire(entries)))
 cases=[dict(Profile=p,Source=s,Direction=d,Partition=partition) for p in range(len(old['Profiles'])) for s in [5,6] for d in [[1,0],[.6,.8],[0,-1]] for partition in [1000,11000,27000]]
 Path(path).write_text(json.dumps(dict(Synthetic=True,Profiles=old['Profiles'],Baseline=old['Baseline'],Stages=stages,Cases=cases),separators=(',',':'))+'\n',encoding='utf-8',newline='\n');print('R50_GENERATED',len(cases),'sequences',len(cases)*len(stages),'stages')
def verify(path,native):
 fixture=json.loads(Path(path).read_text(encoding='utf-8'));native=json.loads(Path(native).read_text(encoding='utf-8'));assert len(native['Cases'])==len(fixture['Cases']);checks=0
 for c,rows in zip(fixture['Cases'],native['Cases'],strict=True):
  p=fixture['Profiles'][c['Profile']];slot=c['Source']-5;bootstrap=p['SpeedWords'][slot] if len(p['SpeedWords'])>slot else None;known={};revision=0;dx,dz=map(f,c['Direction'])
  for stage,r in zip(fixture['Stages'],rows,strict=True):
   if stage['Entries'] is not None:
    revision+=1
    for ordinal,(index,b) in enumerate(stage['Entries']):known[index]=(b,ordinal,revision)
   index=7 if slot==0 else 11;current=known.get(index);raw=current[0] if current else bootstrap;origin=3 if current else (2 if raw is not None else 0)
   speed=None if raw is None else struct.unpack('<f',struct.pack('<I',raw))[0];eligible=speed is not None and math.isfinite(speed) and speed>=0
   velocity=[f(dx*speed),0,f(dz*speed)] if eligible else None;eligible=eligible and all(math.isfinite(x) and abs(x)<=10 for x in velocity)
   assert r['InputBits']==raw and r['Origin']==origin and r['Eligible']==eligible;checks+=3
   if current:assert r['Provenance']==dict(Revision=current[2],DeclaredCount=65535,Ordinal=current[1],Origin=1);checks+=1
   else:assert r['Provenance'] is None;checks+=1
   x=z=f(0)
   if eligible:
    assert r['RawBits']==raw and r['CurrentSource'] and r['Source']==c['Source'];checks+=3
    for _ in range(3):x=f(x+f(velocity[0]*f(.01)));z=f(z+f(velocity[2]*f(.01)))
   else:assert r['RawBits'] is None;checks+=1
   assert list(map(f,r['Position']))==[x,0,z];checks+=1
 print('R50_INDEPENDENT_PASS sequences=%s stages=%s comparisons=%s nativechecks=%s'%(len(native['Cases']),len(native['Cases'])*len(fixture['Stages']),checks,native['Checks']))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(sys.argv[2])
 else:verify(sys.argv[2],sys.argv[3])
