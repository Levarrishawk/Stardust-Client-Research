"""Original authored raw words from SHA-gated independent chain; synthetic received schedule."""
import json,math,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
fixture=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'));actual=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8'));oracle=json.loads(Path(sys.argv[3]).read_text(encoding='utf-8'));authored=runpy.run_path(str(ROOT/'poc/round47-tests/Integration/movement_input_join_peer.py'))['original'](sys.argv[3]);assert len(actual['Cases'])==6 and actual['Mounts']==48;checks=0
for src in oracle['Chain']:
 n=actual['Sources'][src['Path']];assert (n['Priority'],n['Bytes'],n['Sha256'])==(src['Priority'],src['Bytes'],src['Sha256']);checks+=1
for c in actual['Cases']:
 index=7 if c['Source']==5 else 11;slot='speed[0]' if index==7 else 'speed[1]';bootstrap=authored[slot]['Bits'];known={};dx,dz=map(f,c['Direction'])
 for stage,r in zip(fixture['Stages'],c['Stages'],strict=True):
  known.update(stage['Entries'] or []);raw=known.get(index,bootstrap);origin=3 if index in known else 2;speed=struct.unpack('<f',struct.pack('<I',raw))[0];eligible=math.isfinite(speed) and speed>=0;v=[f(dx*speed),f(dz*speed)] if eligible else None;eligible=eligible and all(math.isfinite(t) and abs(t)<=10 for t in v)
  assert (r['InputBits'],r['Origin'],r['Eligible'])==(raw,origin,eligible);x=z=f(0)
  if eligible:
   assert r['RawBits']==raw
   for _ in range(3):x=f(x+f(v[0]*f(.01)));z=f(z+f(v[1]*f(.01)))
  else:assert r['RawBits'] is None
  assert list(map(f,r['Position']))==[x,0,z];checks+=5
print('R50_ORIGINAL_INDEPENDENT_PASS sequences=6 stages=54 comparisons=%s sourceidentities=5'%(checks))
