"""Independent common-model/state/charge oracle; not TANO wire evidence or production execution."""
import json,runpy,sys
from pathlib import Path
folder=Path(sys.argv[1]);r=json.loads((folder/'tangible-receipt.json').read_text(encoding='utf-8'));f=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text(encoding='utf-8'))
assert r['Checks']==93 and len(r['Cases'])==len(set(r['Cases']))==28 and r['ModelProjectionOnly'] and r['LoaderCalls']==0
baseline=f['baseline_fixed']+f['seed_file_bytes']+f['seed_key_bytes']+2*f['seed_units']+f['seed_raw_bytes']+4*len(f['seed_components']);seed=baseline+16+4*len(set(f['seed_components']));mask=0xFFFFFFFF
for receipt in r['Receipts']:
 name=receipt['Name']
 if name in ('forward','reverse'):
  events=list(reversed(f['events'])) if name=='reverse' else f['events'];state={};keys=set(f['seed_components']);counter=0
  for e in events:
   if 'add' in e:
    skip=0 if ((counter+e['count'])&mask)<=e['counter'] else min((counter-e['counter']+e['count'])&mask,e['count'])
    for _ in range(skip,e['count']):
     if e['add'] not in keys:keys.add(e['add']);counter=(counter+1)&mask
    counter=max(counter,e['counter'])
   else:state.update(e)
  extra=len(bytes.fromhex(state['file']))+len(bytes.fromhex(state['key']))+2*len(state['units'])+len(bytes.fromhex(state['raw']))
  assert receipt['BaselineBytes']==baseline and receipt['SeedBytes']==seed and receipt['Keys']==sorted(keys) and receipt['Counter']==counter
  assert receipt['Raw']==state['raw'] and receipt['File']==state['file'] and receipt['Key']==state['key'] and receipt['Units']==state['units'] and receipt['Visibility']==state['visibility'] and receipt['Options']==state['options']
  assert receipt['CurrentBytes']==baseline+16+4*len(keys)+extra and receipt['AliasBytes']==seed+8 and receipt['EqualBytes']==seed+8+f['seed_raw_bytes']
 elif name.startswith('wrap-'):
  counter=receipt['InitialCounter'];keys=set(receipt['Initial']);remote=receipt['Remote'];skip=0 if ((counter+1)&mask)<=remote else min((counter-remote+1)&mask,1)
  if not skip and 123 not in keys:keys.add(123);counter=(counter+1)&mask
  counter=max(counter,remote);assert receipt['Keys']==sorted(keys) and receipt['Counter']==counter
 else:
  assert name=='common' and receipt['ComplexityBits']==2147483648 and receipt['Volume']==4294967295 and receipt['Options']==123 and receipt['UseCount']==4294967295 and receipt['ConditionDamage']==2147483648 and receipt['MaximumCondition']==17 and receipt['Visibility']==255 and receipt['Retained']==seed
assert len(r['Receipts'])==6 and {'atomic-component-then-clear','atomic-name-then-empty','atomic-raw-then-empty','global-stale-nonpruning-recovery','seed-stale-nonpruning-registry-close','large-single-pass'}<=set(r['Cases'])
assert (folder/'tangible-native.txt').read_text(encoding='utf-8').strip().endswith('R26_TANGIBLE_STATE_PASS checks=93 cases=28')
print('R26 independent projected tangible order/charge/counter oracle PASS; native93/28; no TANO wire claim')
sys.argv=['verify.py',str(folder)]
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round25-tests/Runtime/verify.py'),run_name='__main__')
