"""Independent current tangible state and visibility audit, then accepted full oracle."""
import json,runpy,sys
from pathlib import Path
log,peer=map(Path,sys.argv[1:3]);fixture=json.loads(peer.read_text())
rows=[json.loads(line.split('=',1)[1]) for line in log.read_text(encoding='utf-8-sig').splitlines() if line.startswith('wire_customization_state=')]
assert len(rows)==5
for row,expected in zip(rows,fixture['ExpectedTangible']):
 assert row['ExtendedFields'] and row['Name']==expected['Name']
 assert row['ComplexityBits']==expected['ComplexityBits']
 assert all(row['Tangible'][key]==value for key,value in expected['Tangible'].items()),(row,expected)
 assert row['RootVisible']==expected['RootVisible'],(row,expected)
assert rows[1]['FaceSha256']==rows[2]['FaceSha256'] and not rows[1]['RootVisible'] and not rows[2]['RootVisible']
assert rows[3]['RootVisible'] and rows[3]['FaceSha256']==rows[2]['FaceSha256']
sys.argv=['verify_full.py',str(log),str(peer)]
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round23-tests/Integration/verify_full.py'),run_name='__main__')
print(json.dumps(dict(passed=True,current_common_fields=True,hidden_composition=True,visibility_raw255=True,appearance_retained_on_show=True)))
