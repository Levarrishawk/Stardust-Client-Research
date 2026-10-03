"""Independent deterministic local-clock integration oracle; no production imports."""
import json, math, sys
from pathlib import Path

text=Path(sys.argv[1]).read_text(encoding='utf-8-sig')
rows=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('local_state=')]
names=['walk','run','paused','corrected_paused','resumed','idle','unfocused','focus_resumed','reset']
assert [r['Name'] for r in rows]==names
positions=[[-.5,0,1+3*.016667*2],[-.5,0,1+3*.016667*8],[-.5,0,1+3*.016667*8],[-1.5,0,-1],[-1.5,0,-1+3*.016667*6],[-1.5,0,-1+3*.016667*6],[-1.5,0,-1+3*.016667*6],[-1.5,0,-1+3*.016667*6]]
speeds=[2,6,6,1,6,0,0,0,0]
clips=['walk','run','run','walk','run','idle','idle','idle','idle']
times=[50001,100002,350002,850002,900003,950004,1200004,1250005,1300006]
for i,row in enumerate(rows):
    assert row['TimestampMicroseconds']==times[i]
    assert row['Speed']==speeds[i] and row['Clip']==clips[i]
    assert row['DroppedMicroseconds']==0 and row['calls']==row['closed']==3 and row['Height']==1.4
    assert all(math.isfinite(v) for v in row['Native']+row['Engine'])
    assert abs(row['Frame'])<1e6
    assert all(abs(a-b)<2e-5 for a,b in zip(row['Engine'],[row['Native'][0],row['Native'][1],-row['Native'][2]]))
    if i<8:assert all(abs(a-b)<2e-5 for a,b in zip(row['Native'],positions[i]))
    assert row['StepsThisUpdate']==(0 if i in (2,3,6) else 3)
assert rows[2]['SuspendedMicroseconds']==250000 and rows[3]['SuspendedMicroseconds']==750000
assert rows[6]['SuspendedMicroseconds']==1000000 and rows[8]['SuspendedMicroseconds']==0
assert 'PASS: pumped local_input=true' in text and 'drained=true' in text
print('PASS: independent nine-stage local clock, correction, suspension, clips, retained resources and drain')
