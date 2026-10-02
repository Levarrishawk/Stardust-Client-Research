import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[3]
if len(sys.argv)!=5: raise SystemExit('usage: verify_sources.py native-log detail1-log detail2-log detail3-log')
native={i:{} for i in (1,2,3)}
for line in Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines():
 if line.startswith('source='):
  r=json.loads(line[7:]); native[r['detail']][r['row']['Path']]=r['row']
for detail,argument in enumerate(sys.argv[2:],1):
 rows=[json.loads(line[len('skeletal_detail_source='):]) for line in Path(argument).read_text(encoding='utf-8-sig').splitlines() if line.startswith('skeletal_detail_source=')]
 assert len(rows)==len({r['Path'] for r in rows}), 'duplicate source identity'
 assert {r['Path']:r for r in rows}==native[detail], 'native/Godot closure differs'
 print(f'PASS: detail {detail}: {len(rows)} native/Godot source identities match path, priority, bytes, SHA-256')
