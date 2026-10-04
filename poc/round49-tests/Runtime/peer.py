"""Synthetic stop schedules and independent binary32 native planar oracle."""
import json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def generate(path):
 old=json.loads((ROOT/'poc/round48-tests/Runtime/ordered-fixtures.json').read_text());cases=[]
 for source in range(1,5):
  for direction in [(1,0),(0,-1),(.6,.8)]:
   for stop in [0,1,9999,10000,10001,20000,49999,50000]:
    for partition in [1000,9000,27000]: cases.append(dict(Source=source,Direction=list(direction),Stop=stop,Partition=partition))
 Path(path).write_text(json.dumps(dict(Synthetic=True,Profile=old['Profiles'][0],Missing=next(p for p in old['Profiles'] if p['Name']=='absent'),Baseline=old['Baseline'],Cases=cases),separators=(',',':'))+'\n',encoding='utf-8',newline='\n');print('R49_GENERATED',len(cases))
def verify(path,native):
 fixture=json.loads(Path(path).read_text());native=json.loads(Path(native).read_text());assert len(native['Cases'])==len(fixture['Cases']);comparisons=0
 for c,r in zip(fixture['Cases'],native['Cases']):
  speed={1:2,2:6,3:3,4:8}[c['Source']];dx,dz=map(f,c['Direction']);vx,vz=f(dx*speed),f(dz*speed);x=z=f(0)
  for t in range(0,50000,10000):
   if t<c['Stop']:x=f(x+f(vx*f(.01)));z=f(z+f(vz*f(.01)))
  assert list(map(f,r['Position']))==[x,0,z],(c,r,x,z);assert r['Source']==c['Source'];assert r['StopAccepted'];assert f(r['Speed'])==(f((vx*vx+vz*vz)**.5) if c['Stop']>40000 else 0);comparisons+=4
 print('R49_INDEPENDENT_PASS cases=%s comparisons=%s nativechecks=%s'%(len(native['Cases']),comparisons,native['Checks']))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(sys.argv[2])
 else:verify(sys.argv[2],sys.argv[3])
