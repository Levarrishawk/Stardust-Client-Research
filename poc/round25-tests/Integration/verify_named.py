"""Independent current raw/display name audit, then the accepted extended state oracle."""
import json,runpy,sys
from pathlib import Path
log,peer=map(Path,sys.argv[1:3]);fixture=json.loads(peer.read_text())
rows=[json.loads(line.split('=',1)[1]) for line in log.read_text(encoding='utf-8-sig').splitlines() if line.startswith('wire_customization_state=')]
assert len(rows)==5
for row,expected in zip(rows,fixture['ExpectedNames']):
 assert row['NamedFields']
 assert all(row[key]==value for key,value in expected.items()),(row,expected)
 assert row['NameLabelText']==row['NameDisplay'] and row['NameLabelVisible']==row['RootVisible']
assert any('PASS: pumped named_creature=true current_names=true original_table_resolution=true' in line for line in log.read_text(encoding='utf-8-sig').splitlines())
sys.argv=['verify_extended.py',str(log),str(peer)]
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round24-tests/Integration/verify_extended.py'),run_name='__main__')
print(json.dumps(dict(passed=True,current_names=True,prefix_units_spacer_preserved=True,custom_override=True,original_fallback=True,missing_key_distinct=True,name_label_current=True)))
