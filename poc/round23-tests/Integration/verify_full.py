"""Independent full container/state receipt audit plus accepted mixed/motion boundaries."""
import json,runpy,sys
from pathlib import Path
log=Path(sys.argv[1]);peer=Path(sys.argv[2]);lines=log.read_text(encoding='utf-8-sig').splitlines();fixture=json.loads(peer.read_text())
rows=[json.loads(line.split('=',1)[1]) for line in lines if line.startswith('wire_customization_state=')]
assert len(rows)==5 and all(r['Full'] and r['AuthoredContext'] and r['SourceBindings']==8 for r in rows)
for row,expected in zip(rows,fixture['ExpectedContainers']):
 assert all(row[k]==value for k,value in expected.items()),(row,expected)
context=[json.loads(line.split('=',1)[1]) for line in lines if line.startswith('asset_customization_context=')]
assert context==[dict(DirectoryBytes=458340,CidmBytes=3697,Descriptors=36,Selected=8,PaletteBindings=5,AuthoredDefaultsZero=True,LivePlayerState=False)],context
assert any('PASS: pumped full_creature=true containers_retained=true authored_context=true' in line for line in lines)
sys.argv=['verify_mixed.py',str(log)];runpy.run_path(str(Path(__file__).resolve().parents[2]/'round22-tests/Integration/verify_mixed.py'),run_name='__main__')
print(json.dumps(dict(passed=True,component_and_wounds_counters=True,replays_retained=True,selected_original_context=True,live_player_seed=False)))
