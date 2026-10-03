"""Independent synthetic revision/byte-charge trace plus controlled native receipt audit; no live or legacy scheduler claim."""
import json,runpy,sys
from pathlib import Path
folder=Path(sys.argv[1]);r=json.loads((folder/'state-receipt.json').read_text())
assert r['Result']=='PASS' and r['Checks']==136 and len(r['Cases'])==len(set(r['Cases']))==28 and r['LoaderCalls']==0
expected={'seed-order-ack-body-reseed-lifecycle','transaction-target-thread','logical-retention-reference-budget','seed-plan-overflow','empty-bypass-preservation','router-routing-order-borrow','outer-malformed','multi-object-global-budgets','router-views-thread-byte-ownership','router-budget-world-binding','store-reentry-malformed-snapshot'}
expected|={'router-'+s for s in ('throw','null','target','count','world','generation','ack-world','clear-source','dispose-source','reentry','dispose-router','source-find')}
expected|={'downstream-'+s for s in ('throw','reentry','store','generation','false')}
assert set(r['Cases'])==expected
# Independent authored events carry symbolic immutable identities, distinct from equality of payload bytes.
values={'seed':bytes((1,2,3,4)),'a':bytes((0,255,2)),'empty':b'','b':bytes((9,)),'c':bytes((8,)),'seed77':bytes((7,7)),'d6':bytes((6,)),'seed5':bytes((5,))}
events=['seed','a','a','empty','b'];revision=0;journal=[]
for identity in events:revision+=1;journal.append((revision,identity))
expected_trace=dict(Name='ordered',Revisions=[u[0] for u in journal],Values=[values[u[1]].hex().upper() for u in journal])
revision+=1;journal.append((revision,'c'))
expected_trace['BeforeAckBytes']=sum(len(values[k]) for k in {'seed',journal[-1][1]}|{k for _,k in journal})
journal.clear();expected_trace['CurrentAfterAck']=values['c'].hex().upper();expected_trace['AfterAckBytes']=len(values['seed'])+len(values['c'])
for identity in ('seed77','d6','seed5'):revision+=1;journal.append((revision,identity))
expected_trace['ReseedValues']=[values[k].hex().upper() for _,k in journal];expected_trace['ReseedBytes']=sum(len(values[k]) for k in {'seed5'}|{k for _,k in journal})
assert revision==9 and r['Receipts']==[expected_trace]
print(json.dumps(dict(passed=True,native_checks=136,cases=28,synthetic_order_revision_byte_oracle=True,loader_calls=0,independent_scheduler_execution=False),indent=2))
# Full accepted consumer/pump/drain receipt audit, unchanged.
sys.argv=['verify.py',str(folder)]
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round20-tests/Runtime/verify.py'),run_name='__main__')
