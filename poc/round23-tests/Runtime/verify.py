"""Independent authored compound-state/order arithmetic oracle; no production imports or full wire codec."""
import json,runpy,sys
from pathlib import Path
folder=Path(sys.argv[1]);r=json.loads((folder/'full-receipt.json').read_text(encoding='utf-8'))
f=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text(encoding='utf-8'))
assert r['Checks']==338 and len(r['Cases'])==len(set(r['Cases']))==67 and r['LoaderCalls']==0
mask=0xFFFFFFFF
for receipt in r['Receipts']:
 name=receipt['Name']
 if name.startswith('full-'):
  legacy='LegacyBounded' in name;reverse=name.endswith('True')
  assert receipt['Wounds']==f['assign'] and receipt['WoundCounter']==(receipt['InitialWoundCounter']+(len(f['assign'])+1 if legacy else 1))&mask
  keys=set(receipt['InitialComponents']);changed=f['add'] not in keys;keys.add(f['add'])
  assert receipt['Components']==sorted(keys) and receipt['ComponentCounter']==max((receipt['InitialComponentCounter']+int(changed))&mask,(receipt['InitialComponentCounter']+1)&mask)
  assert receipt['Raw']==f['raw_reverse' if reverse else 'raw_forward'] and receipt['Height']==f['height']
 elif name.startswith('wrap-'):
  remote=receipt['Remote'];counter=receipt['InitialWoundCounter'];values=receipt['InitialWounds'][:]
  skip=min((1-remote+counter)&mask,1)
  if not skip and (not values or values[0]!=f['wrap_replace']):
   if not values:values=[0]
   values[0]=f['wrap_replace'];counter=(counter+1)&mask
  assert receipt['Wounds']==values and receipt['WoundCounter']==counter
  counter=receipt['InitialComponentCounter'];keys=set(receipt['InitialComponents'])
  skip=0 if ((counter+1)&mask)<=remote else min((counter-remote+1)&mask,1)
  if not skip and f['add'] not in keys:keys.add(f['add']);counter=(counter+1)&mask
  counter=max(counter,remote)
  assert receipt['Components']==sorted(keys) and receipt['ComponentCounter']==counter
 else:
  assert name in ('forward','reverse')
  assert receipt['Height']==(1.25 if name=='reverse' else 2.5)
  assert receipt['Raw']==(['01020304','09','08'] if name=='reverse' else ['01020304','08','09'])
  assert receipt['RawRevisions']==[1,2,3] and receipt['States']=='FEDCBA9876543210'
assert len(r['Receipts'])==9
assert {'large-single-pass-StrictInterval','large-single-pass-LegacyBounded'}<=set(r['Cases'])
assert (folder/'kernels-native.txt').read_text(encoding='utf-8').strip().endswith('R23_KERNEL_REGRESSIONS_PASS')
assert {'bytes-command-wounds-full-atomic','bytes-command-components-full-atomic','multi-actor-decoded-capacity-atomic','wound-counter-full-atomic','wound-growth-full-atomic','component-growth-full-atomic'}<=set(r['Cases'])
assert (folder/'full-native.txt').read_text(encoding='utf-8').strip().endswith('R23_FULL_STATE_PASS checks=338 cases=67')
print('R23 independent compound/order/counter oracle PASS; native338/67; loader0')
sys.argv=['verify.py',str(folder)]
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round22-tests/Runtime/verify.py'),run_name='__main__')
