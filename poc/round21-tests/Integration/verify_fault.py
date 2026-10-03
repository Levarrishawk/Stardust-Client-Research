"""Check actual joined malformed/unknown packed ingress cleanup, without importing production code."""
import json,sys
from pathlib import Path
for filename,expected in zip(sys.argv[1:],['Truncated packed customization.','Unknown installed customization ID 127 at record 0.']):
 text=Path(filename).read_text(encoding='utf-8-sig');lines=text.splitlines()
 rows=[json.loads(s.split('=',1)[1]) for s in lines if s.startswith('wire_customization_state=')]
 assert [r['Name'] for r in rows]==['zero','ordered_valid_then_invalid','duplicate_retains_prior_valid','empty_forms_retain']
 assert rows[-1]['Applied']==5 and rows[-1]['Pending']==0
 assert expected in text and 'Wire customization batch rejected' in text
 assert 'PASS: pumped wire_customization=' not in text
 cleanup=[json.loads(s.split('=',1)[1]) for s in lines if s.startswith('pump_failure_cleanup=')]
 assert cleanup and all(r['drained'] and r['calls']==r['closed']==3 and r['actors']==0 for r in cleanup)
 print(json.dumps(dict(passed=True,expected=expected,prior_appearance_retained=True,actors_cleared=True,archive_jobs_drained=True)))
assert len(sys.argv)==3
