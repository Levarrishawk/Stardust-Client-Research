"""Present received nonfinite source rejects without authored fallback; actual host cleanup."""
import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def verify(fixture,log,receipt):
 d=json.loads(Path(fixture).read_text(encoding='utf-8'))
 expected={r['Index']:r['Bits'] for r in d['Steps'][-1]['Fields']}[7]
 assert expected&0x7f800000==0x7f800000 and expected&0x7fffff
 text=Path(log).read_text(encoding='utf-8-sig')
 assert 'Selected original/received speed is eligible without fallback' in text
 assert 'sourced_input_state=' not in text and 'PASS: pumped sourced_input=true' not in text
 rows=[json.loads(s.split('=',1)[1]) for s in text.splitlines() if s.startswith('sourced_current_inputs=')]
 assert len(rows)==1
 view=rows[0];field=next(r for r in view['Fields'] if r['Index']==7)
 assert view['Current'] and view['OutputBytes']==1632 and not view['RawReceivedStoreSeeded'] and not view['MovementApplied']
 assert field['RawBits']==expected and field['Origin']=='Received' and field['Eligibility']=='NonFinite'
 assert field['AuthoredBits'] is not None and field['AuthoredBits']&0x7f800000!=0x7f800000
 cleanup=[json.loads(s.split('=',1)[1]) for s in text.splitlines() if s.startswith('pump_failure_cleanup=')]
 assert len(cleanup)==1 and cleanup[0]['calls']==cleanup[0]['closed']==3 and cleanup[0]['drained'] and cleanup[0]['actors']==0 and cleanup[0]['DriverFault'] is None
 result=dict(Passed=True,SelectedSource='CurrentField7',ObservedPresentNonfiniteReceived=True,FiniteAuthoredNotSubstituted=True,NoSourcedMotionState=True,ArchivesDrained=3,ActorsCleared=True,OriginalEffectiveMovementParity=False)
 out=Path(receipt).resolve();assert out.is_relative_to(ROOT/'local-private')
 out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
if __name__=='__main__':verify(*sys.argv[1:4])
