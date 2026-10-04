"""Independent accepted30 wire/state oracle with publication revision and cumulative journal checks."""
import json,runpy,sys
from pathlib import Path
fixture=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());peer=runpy.run_path(str(Path(__file__).parents[2]/'round30-tests/Networking/peer.py'));expected=peer['generate']();expected.pop('Malformed',None);assert fixture==expected
native=json.loads(Path(sys.argv[1]).read_text());assert native['Checks']==496 and native['Sequences']==21 and len(native['Cases'])==27
records={r['Name']:r for r in native['Records']};transitions=0;events=0
for case in fixture['Cases']:
 record=records[case['Name']];assert record['Initial']==case['Initial'];queue=[]
 assert len(record['Steps'])==len(case['Steps'])
 for i,(step,actual) in enumerate(zip(case['Steps'],record['Steps']),2):
  # Recompute from independent wire reader/state machine, not native exported expected values.
  previous=case['Initial'] if i==2 else case['Steps'][i-3]['Expected'];state,descriptions,work,cost=peer['apply'](previous,bytes.fromhex(step['Hex']))
  assert state==actual['State']==step['Expected'] and descriptions==step['Events'];queue += [dict(StoreRevision=i,**e) for e in descriptions]
  assert actual['StoreRevision']==i and actual['Queue']==queue
  transitions+=1;events+=len(descriptions)
assert {'state','queue','events','output','work','store-revision','world-journal','world-revision','global-state','history-move-recreate-generation','foreign-owner-close','default-opaque','missing-seed','cancel-drain','wrong-family','pending-target','router-malformed','router-budget','router-owner'} | {'callback-'+f for f in ['null','id','count','world-mutation','ack','router-reentry','dispose','generation']} == set(native['Cases'])
print(json.dumps(dict(passed=True,native_checks=496,sequences=21,independent_transitions=transitions,ordered_events=events,world_cases=27,live_or_original_execution=False),indent=2))
