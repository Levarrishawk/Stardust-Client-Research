"""Independent scalar/HAM full-model/event expectations and accepted resource gates."""
import importlib.util,json,subprocess,sys
from pathlib import Path
assert len(sys.argv)==7
directory=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('peer36',directory/'generate_peer.py')
p=importlib.util.module_from_spec(s);s.loader.exec_module(p)
fixture=json.loads(Path(sys.argv[6]).read_text(encoding='utf-8'))
assert fixture==p.fixture,'Synthetic fixture differs from independent generation'
lines=Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines()
rows=[json.loads(line.split('=',1)[1]) for line in lines if line.startswith('creature6_composite_state=')]
expected=[dict(Name='seed',Events=[],**fixture['Seed'])]+fixture['Steps']
assert len(rows)==len(expected)==6
for i,(row,value) in enumerate(zip(rows,expected)):
 assert row['Index']==i and row['Name']==value['Name'] and row['ObjectId']==fixture['ObjectId']
 assert row['ModelHex']==value['ModelHex'] and row['RetainedBytes']==value['StateBytes']
 assert row['StateRevision']==i+1 and row['StoreRevision']==i+1
 assert row['Events']==value['Events']
 assert row['PendingBytesBeforeAck']==value['StateBytes']+24*len(value['Events'])
 assert row['Current'] and row['AcknowledgedOnce'] and row['VisualRetained'] and row['Archives']==3
assert any('PASS: pumped creature_view6_composite=true' in line for line in lines)
subprocess.run([sys.executable,str(directory.parents[1]/'round32-tests/Integration/verify_world.py'),*sys.argv[1:6]],check=True)
print('PASS main scalar/HAM CREO6 world:6 full models/11 ordered events/counter wrap/no-op/exact ack/current accounting/original resources retained')
