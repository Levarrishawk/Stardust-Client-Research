"""Independent full-model CREO6 expectations joined with accepted TANO6/animation proof."""
from pathlib import Path
import importlib.util,json,subprocess,sys
assert len(sys.argv)==7
directory=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('peer34',directory/'generate_peer.py');p=importlib.util.module_from_spec(s);s.loader.exec_module(p)
fixture=json.loads(Path(sys.argv[6]).read_text(encoding='utf-8'))
assert fixture==p.fixture,'Synthetic fixture differs from independently generated expectations'
lines=Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines()
rows=[json.loads(line.split('=',1)[1]) for line in lines if line.startswith('creature6_state=')]
expected=[dict(Name='seed',Events=0,Indices=[],**fixture['Seed'])]+fixture['Steps']
assert len(rows)==len(expected)==6
for i,(row,value) in enumerate(zip(rows,expected)):
 assert row['Index']==i and row['Name']==value['Name'] and row['ObjectId']==fixture['ObjectId']
 assert row['ModelHex']==value['ModelHex'] and row['RetainedBytes']==value['StateBytes']
 assert row['StateRevision']==i+1 and row['StoreRevision']==i+1
 assert row['Events']==value['Events'] and row['Indices']==value['Indices']
 assert row['Current'] and row['AcknowledgedOnce'] and row['VisualRetained'] and row['Archives']==3
assert any('PASS: pumped creature_view6=true' in line for line in lines)
subprocess.run([sys.executable,str(directory.parents[1]/'round32-tests/Integration/verify_world.py'),*sys.argv[1:6]],check=True)
print('PASS main CREO6 world:6 exact full models/ordered events/raw bits/vector retention/owned acknowledgment/original visuals retained')
