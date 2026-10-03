"""Independent authored union-order oracle and native receipt audit; imports no production code."""
import json,runpy,sys
from pathlib import Path
folder=Path(sys.argv[1]);r=json.loads((folder/'scalars-receipt.json').read_text(encoding='utf-8'))
f=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text(encoding='utf-8'))
assert r['Checks']==196 and len(r['Cases'])==len(set(r['Cases']))==40 and r['LoaderCalls']==0
expected=[]
for reverse in (False,True):
 events=list(reversed(f['forward'])) if reverse else f['forward'];raw=[f['seed']];state={}
 for event in events:
  if 'raw' in event:raw.append(event['raw'])
  else:state.update(event)
 expected.append(dict(Name='reverse' if reverse else 'forward',Height=state['height'],Raw=raw,RawRevisions=list(range(1,len(raw)+1)),States=f"{state['states']:016X}"))
assert r['Receipts']==expected
required={'raw-count-atomic','raw-string-atomic','raw-bytes-atomic','raw-overflow-atomic','scalar-limit-atomic','replication-overflow-atomic','world-overflow-atomic','source-binding-atomic','source-closed-atomic','world-journal-capacity-atomic','unseeded-closed-source-atomic','source-actor-capacity-atomic','stale-target-thread-atomic','reseed-destroy-recreate-generation'}
assert required<=set(r['Cases'])
assert (folder/'scalars-native.txt').read_text(encoding='utf-8').strip().endswith('R22_SCALAR_STATE_PASS checks=196 cases=40')
print('R22 independent ordered raw/scalar oracle PASS; native196/40; loader0')
sys.argv=['verify.py',str(folder)]
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round21-tests/Runtime/verify.py'),run_name='__main__')
