"""Independent complete CREO6 host models/events and accepted resource gates."""
import json,runpy,subprocess,sys
from pathlib import Path
if len(sys.argv)!=7:raise SystemExit('log assets-receipt tangible-peer names-peer tangible6-peer full-creature-peer')
root=Path(__file__).resolve().parents[3]
expected_fixture=runpy.run_path(str(Path(__file__).with_name('generate_peer.py')))['fixture']
fixture=json.loads(Path(sys.argv[6]).read_text(encoding='utf-8'))
assert fixture==expected_fixture,'Synthetic fixture differs from independent generation'
lines=Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines()
rows=[json.loads(line.split('=',1)[1]) for line in lines if line.startswith('creature6_full_state=')]
expected=[dict(Name='seed',Events=[],**fixture['Seed'])]+fixture['Steps']
assert len(rows)==len(expected)==6
for i,(row,value) in enumerate(zip(rows,expected)):
 assert row['Index']==i and row['Name']==value['Name'] and row['ObjectId']==fixture['ObjectId']
 assert row['ModelHex']==value['ModelHex'] and row['RetainedBytes']==value['StateBytes']
 assert row['StateRevision']==i+1 and row['StoreRevision']==i+1 and row['Events']==value['Events']
 assert row['PendingBytesBeforeAck']==value['StateBytes']+48*len(value['Events'])
 assert row['Current'] and row['AcknowledgedOnce'] and row['VisualRetained'] and row['Archives']==3
assert any('PASS: pumped creature_view6_full=true' in line for line in lines)
subprocess.run([sys.executable,str(root/'poc/round32-tests/Integration/verify_world.py'),*sys.argv[1:6]],check=True)
print('PASS complete CREO6 world:6 full models/45 ordered events/all18 fields/counters/exact ack/current accounting/original resources retained')
