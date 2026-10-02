import json,re,sys
from pathlib import Path
if len(sys.argv)<3: raise SystemExit('usage: verify_sources.py native-log retained-Godot-log [additional-retained-log ...]')
def rows(path,prefix):
 values=[json.loads(l[len(prefix):]) for l in Path(path).read_text(encoding='utf-8-sig').splitlines() if l.startswith(prefix)]
 assert len(values)==len({r['Path'] for r in values}), 'duplicate source identity'
 return {r['Path']:r for r in values}
native=rows(sys.argv[1],'source=');assert len(native)==27
for path in sys.argv[2:]:
 actual=rows(path,'retained_source=');assert actual==native,'native/Godot path/priority/bytes/hash differs'
 log=Path(path).read_text(encoding='utf-8-sig')
 for detail,vertices,triangles in [(2,542,756),(3,163,190)]:
  assert f'retained detail={detail} surfaces=2 vertices={vertices} triangles={triangles} skeleton_detail=0' in log
 assert 'same_frame=true hidden_catchup=true idle_cycles=2' in log
 assert 'geometry_material_shader_texture_retained=true' in log
 assert 'retained teardown updates_disabled=true roots_released=true resources_disposed=true loader_disposed=true' in log
 print(f'PASS: {Path(path).name}: 27 union source identities, selected geometry, shared phase and teardown match')
