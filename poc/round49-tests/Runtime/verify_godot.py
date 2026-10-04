"""Independent source and stop observations from the actual Godot receipt."""
import json,struct,sys
from pathlib import Path
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
r=json.loads(Path(sys.argv[1]).read_text());assert len(r['Cases'])==16;checks=0
for c in r['Cases']:
 if c['Kind']=='Source':
  speed={1:2,2:6,3:3,4:8}[c['Source']];assert c['Speed']==speed and list(map(f,c['Forward']))==[0,0,f(speed*f(.01))];checks+=2
  x,z=map(f,c['DiagonalDirection']);assert abs(x-2**-.5)<1e-7 and abs(z-2**-.5)<1e-7;assert c['Stopped']==0 and c['FocusNoCatchup'];checks+=3
 elif c['Kind']=='Unavailable':
  assert c['Before']==c['Unavailable']==c['Resume'];assert abs(f(c['After'])-f(c['Before'])-.04)<1e-7;checks+=2
 elif c['Kind']=='MissingAuthored':assert c['Speed']==0;checks+=1
 else:assert c['Closed'] and c['ClockCalls']==1;checks+=2
print('R49_GODOT_INDEPENDENT_PASS cases=16 comparisons=%s nativechecks=%s'%(checks,r['Checks']))
