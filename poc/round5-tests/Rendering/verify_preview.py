import json,math,sys
from pathlib import Path
if len(sys.argv)<3: raise SystemExit('usage: verify_preview.py native-log Godot-log [more-logs]')
def identities(path,prefix):
 rows=[json.loads(l[len(prefix):]) for l in Path(path).read_text(encoding='utf-8-sig').splitlines() if l.startswith(prefix)]
 assert len(rows)==len({r['Path'] for r in rows})
 return {r['Path']:r for r in rows}
expected=identities(sys.argv[1],'source=');assert len(expected)==58
for path in sys.argv[2:]:
 log=Path(path).read_text(encoding='utf-8-sig');assert identities(path,'retained_source=')==expected,'union identity mismatch'
 for detail,vertices,triangles in [(0,1790,2810),(1,1256,1904),(2,542,756),(3,163,190)]:
  assert f'retained detail={detail} surfaces={5 if detail<2 else 2} vertices={vertices} triangles={triangles} skeleton_detail=0' in log
 current=None;cases=0
 for line in log.splitlines():
  if not line.startswith('projection='):continue
  row=json.loads(line[11:]);s=row['sample'];p=s['BoundingRadius']/s['CenterDepth']*s['ViewportHeight']/math.tan(math.radians(s['VerticalFovDegrees'])/2)
  assert abs(p-row['pixels'])<1e-7
  thresholds=[240,120,60]
  if current is None:current=sum(p<t for t in thresholds)
  else:
   coarser=[d+1 for d,t in enumerate(thresholds) if d>=current and p<t*.9]
   if coarser:current=max(coarser)
   finer=[d for d,t in enumerate(thresholds) if d<current and p>=t*1.1]
   if finer:current=min(finer)
  assert current==row['selected'],(current,row)
  cases+=1
 assert cases==34,cases
 assert 'real camera all4 sweep jitter24 hysteresis=true near_inside_zero_guard=true invalid_atomic=true manual_override=true reset=true world_scale=true retained=true' in log
 assert 'same_frame=true hidden_catchup=true idle_cycles=2' in log
 assert 'retained teardown' in log
 print(f'PASS: {Path(path).name}: 58 source identities; 34 independent camera projections/hysteresis choices; all4 geometry and lifecycle')
