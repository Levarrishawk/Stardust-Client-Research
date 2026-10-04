"""Literal TANO6 world peer joined to the prior independent animation/name checks."""
from pathlib import Path
import json,subprocess,sys
assert len(sys.argv)==6
log=Path(sys.argv[1]);fixture=json.loads(Path(sys.argv[5]).read_text(encoding='utf-8'))
lines=log.read_text(encoding='utf-8-sig').splitlines()
rows=[json.loads(line.split('=',1)[1]) for line in lines if line.startswith('tangible6_state=')]
expected=[dict(Name='seed',**fixture['Seed'])]+fixture['Steps']
assert len(rows)==len(expected)==6
for index,(row,value,event_count) in enumerate(zip(rows,expected,[0,2,2,1,0,3])):
    assert row['Index']==index and row['Name']==value['Name']
    assert row['ObjectId']==fixture['ObjectId']
    for key in ('Passive','Counter','Ids'):assert row[key]==value[key],(key,row,value)
    assert row['StateRevision']==index+1 and row['StoreRevision']==index+2
    assert row['RetainedBytes']==8+8*len(value['Ids']) and row['Events']==event_count
    assert row['Current'] and row['AcknowledgedOnce'] and row['VisualRetained'] and row['Archives']==3
assert any('PASS: pumped tangible_view6=true' in line for line in lines)
subprocess.run([sys.executable,str(Path(__file__).resolve().parents[2]/'round30-tests/Integration/verify_clocked.py'),*sys.argv[1:5]],check=True)
print('PASS main TANO6 world:6 literal states/8 current events/exact ack/current accounting/original visuals retained')
