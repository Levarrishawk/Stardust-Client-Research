"""Independent binary32 trajectory for unchanged Node current-mode output."""
import json,struct,sys
from pathlib import Path
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
r=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'));assert len(r['Cases'])==10;checks=0
for c in r['Cases']:
 if c['Kind']=='Current':
  authored,received,restored=(2,3,4) if c['Source']==5 else (6,8,9);a=f(authored*f(.01));b=f(a+f(received*f(.01)));d=f(b+f(restored*f(.01)))
  assert f(c['Bootstrap'])==a and f(c['Late'])==b;assert c['Unavailable']==c['Resume']==c['Late'];assert f(c['Restored'])==d and c['Zero']==c['Restored'];assert c['RawZero']==(0 if c['Source']==5 else 0x80000000);checks+=6
 else:assert c['Closed'];checks+=1
print('R50_GODOT_INDEPENDENT_PASS cases=10 comparisons=%s nativechecks=%s'%(checks,r['Checks']))
