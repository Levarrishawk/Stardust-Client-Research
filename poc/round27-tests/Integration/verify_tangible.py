"""Independent current TANO rows through the actual mixed world/presentation pump."""
import json,runpy,sys
from pathlib import Path
if len(sys.argv)!=4:raise SystemExit('host-log tangible-fixture-json named-creature-fixture-json')
log=Path(sys.argv[1]);data=json.loads(Path(sys.argv[2]).read_text());text=log.read_text(encoding='utf-8-sig')
rows=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('tangible_world_state=')]
preloads=[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith('object_name_preload=')]
assert preloads==[dict(preloadCalls=2,SourceBytes=7137,TotalRecords=97,TotalValueUnits=1724,TotalKeyBytes=1723,Missing=0,FrameIo=False)]
assert len(rows)==len(data['Expected'])==5
for actual,expected in zip(rows,data['Expected']):
 for key,value in expected.items():
  if key=='Origin' and value=='PrimaryTable':value='FallbackTable'
  assert actual[key]==value,(expected['Name'],key,actual[key],value)
 assert actual['LabelText']==actual['DisplayText']
 assert actual['LabelVisible']==actual['RootVisible']==(actual['Visibility']!=0)
 assert actual['PublicationRetained'] and actual['TextureReadbackRetained'] and actual['PoseRetained']
 assert actual['calls']==actual['closed']==3
assert len({row['Generation'] for row in rows})==1
assert all(b['Revision']>a['Revision'] for a,b in zip(rows,rows[1:]))
assert 'PASS: pumped named_creature=true' in text and 'PASS: pumped world continuous=true' in text
repo=Path(__file__).resolve().parents[3]
sys.argv=[str(repo/'poc/round25-tests/Integration/verify_named.py'),str(log),sys.argv[3]]
runpy.run_path(sys.argv[0],run_name='__main__')
print(json.dumps(dict(passed=True,typed_tano_world=True,ordered_rows=5,original_static_publication_retained=True,raw_visibility=True,original_name_resolution=True,mixed_creature_regressions=True)))
