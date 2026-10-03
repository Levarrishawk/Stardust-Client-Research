"""Reuse accepted pixel/pose oracle, then require generic-consumer observations."""
import runpy,sys
from pathlib import Path
logs=[Path(p).read_text(encoding='utf-8-sig',errors='replace') for p in sys.argv[1:4]]
for text in logs[:2]:
    assert 'PASS: generic consumer dynamic_ids=true actors=2 idempotent=true applied_stale_ack_retried=true stale_resource_rejected=true failed_count=0' in text
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round14-tests/Integration/verify_preview.py'),run_name='__main__')
print('PASS: generic acknowledged creature/static consumer; inherited pixel/pose/cleanup/cancellation oracle')
