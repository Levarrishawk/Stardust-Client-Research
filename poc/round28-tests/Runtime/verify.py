"""Independent bounded repeated-double reference; no production admission-check reuse or x87 claim."""
import json,math,struct,sys
from pathlib import Path
f32=lambda bits:struct.unpack('<f',struct.pack('<I',bits))[0]
bits=lambda value:struct.unpack('<I',struct.pack('<f',value))[0]
fixtures=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());native=json.loads(Path(sys.argv[1]).read_text());U64=(1<<64)-1
assert native['Checks']==870 and native['Cases']==len(fixtures)==36
assert native['NumericPolicy']=='binary64-intermediate-float32-persistent' and native['InputUnits']=='abstract-matching-units' and native['OriginalX87Parity'] is False
out={r['Name']:r for r in native['Results']};assert len(out)==len(fixtures)
def metadata(c):return dict(FrameCount=c['FrameCount'],IntervalBits=c['IntervalBits'],MaximumCatchUpSteps=c['Work'],NumericPolicy=native['NumericPolicy'],InputUnits=native['InputUnits'],OriginalX87Parity=False)
def initial(c):return metadata(c)|dict(Index=0,ResidualBits=0,Revision=c['RevisionSeed'],CumulativeAdvances=c['CumulativeSeed'],LastAdvanceSteps=0)
def reference(c,state,op):
 delta=f32(op.get('DeltaBits',0));interval=f32(c['IntervalBits'])
 if op['Kind']=='advance' and (not math.isfinite(delta) or delta<0):return 'argument',state
 if state['Revision']==U64:return 'overflow',state
 if op['Kind']=='reset':return 'ok',metadata(c)|dict(Index=0,ResidualBits=0,Revision=state['Revision']+1,CumulativeAdvances=state['CumulativeAdvances'],LastAdvanceSteps=0)
 remaining=f32(state['ResidualBits'])+delta;steps=0
 # Deliberately no production estimate: at most work+1 loop conditions establish exact bounded outcome.
 while remaining>=interval:
  if steps==c['Work']:return 'budget',state
  remaining=remaining-interval;steps+=1
 if state['CumulativeAdvances']>U64-steps:return 'overflow',state
 return 'ok',metadata(c)|dict(Index=(state['Index']+steps)%c['FrameCount'],ResidualBits=bits(remaining),Revision=state['Revision']+1,CumulativeAdvances=state['CumulativeAdvances']+steps,LastAdvanceSteps=steps)
operations=0;failures={};full_budget=0
for c in fixtures:
 result=out[c['Name']];interval=f32(c['IntervalBits'])
 invalid=c['FrameCount']<1 or not math.isfinite(interval) or interval<=0 or not 1<=c['Work']<=1048576
 assert result['Constructor']==('argument' if invalid else 'ok')
 if invalid:assert not result['Rows'];continue
 state=initial(c);assert result['Initial']==state and len(result['Rows'])==len(c['Operations'])
 for op,row in zip(c['Operations'],result['Rows']):
  outcome,candidate=reference(c,state,op);assert row['Kind']==op['Kind'] and row['DeltaBits']==op.get('DeltaBits',0)
  assert row['Outcome']==outcome and row['State']==candidate,(c['Name'],op,row,candidate)
  operations+=1
  if outcome!='ok':failures[outcome]=failures.get(outcome,0)+1
  if outcome=='ok' and candidate['LastAdvanceSteps']==c['Work']:full_budget+=1
  state=candidate
 assert result['Final']==state
rounded=out['rounded-up-residual']['Rows'];assert rounded[1]['State']['ResidualBits']==0x3dcccccd and rounded[1]['State']['LastAdvanceSteps']==1 and rounded[2]['State']['LastAdvanceSteps']==1 and rounded[2]['State']['ResidualBits']==0
assert out['boundary-8-'+str(0x3dcccccd-1)]['Rows'][0]['State']['LastAdvanceSteps']==0
assert out['boundary-8-'+str(0x3dcccccd)]['Rows'][0]['State']['LastAdvanceSteps']==1
assert out['max-work-exact']['Rows'][0]['State']['LastAdvanceSteps']==1048576 and out['max-work-over']['Rows'][0]['Outcome']=='budget'
assert out['budget-exact']['Rows'][0]['Outcome']=='ok' and out['budget-one-less']['Rows'][0]['Outcome']=='budget'
a=out['partition-ten']['Final'];b=out['partition-single']['Final'];assert a['ResidualBits']!=b['ResidualBits']
print(json.dumps(dict(passed=True,native_checks=native['Checks'],cases=len(fixtures),independent_operations=operations,failed_operations=failures,full_budget_acceptances=full_budget,rounded_interval_then_zero_advance=True,partition_residual_bits=[a['ResidualBits'],b['ResidualBits']],numeric_policy=native['NumericPolicy'],abstract_units=True,original_x87_parity=False),indent=2))
