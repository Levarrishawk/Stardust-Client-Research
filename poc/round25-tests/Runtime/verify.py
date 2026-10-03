"""Independent authored compound-state/order arithmetic oracle; no production imports or full wire codec."""
import json,runpy,sys
from pathlib import Path
folder=Path(sys.argv[1]);r=json.loads((folder/'named-receipt.json').read_text(encoding='utf-8'))
f=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text(encoding='utf-8'))
assert r['Checks']==489 and len(r['Cases'])==len(set(r['Cases']))==93 and r['LoaderCalls']==0
mask=0xFFFFFFFF
for receipt in r['Receipts']:
 name=receipt['Name']
 if name.startswith('names-'):
  events=list(reversed(f['name_events'])) if name.endswith('reverse') else f['name_events'];state={};raw=['01020304']
  for e in events:
   if 'raw' in e:raw.append(e['raw'])
   else:state.update(e)
  assert receipt['File']==state['file'] and receipt['Key']==state['key'] and receipt['NameSpacer']==state['spacer'] and receipt['Units']==state['units']
  charge=len(bytes.fromhex(state['file']))+len(bytes.fromhex(state['key']))+2*len(state['units'])
  assert receipt['Additional']==charge==11 and receipt['Raw']==raw and receipt['Height']==state['height'] and receipt['Visibility']==state['visibility']
  assert receipt['EqualBytesCharge']==2*len(state['units'])+len(bytes.fromhex(receipt['SeedFile']))+len(bytes.fromhex(receipt['SeedKey']))
 elif name.startswith('common-'):
  reverse=name.endswith('reverse');events=list(reversed(f['common_events'])) if reverse else f['common_events'];state={};raw=['01020304'];height=None;wounds=None;components=set(receipt['InitialComponents'])
  for e in events:
   if 'index' in e:state[e['index']]=e.get('value',e.get('bits'))
   elif 'raw' in e:raw.append(e['raw'])
   elif 'height' in e:height=e['height']
   elif 'wounds' in e:wounds=e['wounds']
   elif 'component' in e:components.add(e['component'])
  assert receipt['ComplexityBits']==state[0] and receipt['Volume']==state[3] and receipt['Options']==state[6] and receipt['UseCount']==state[7]
  assert receipt['ConditionDamage']==state[8] and receipt['MaximumCondition']==state[9] and receipt['Visibility']==state[10]
  assert receipt['Raw']==raw and receipt['Height']==height and receipt['Wounds']==wounds and receipt['Components']==sorted(components)
 elif name.startswith('full-'):
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
assert len(r['Receipts'])==13
assert {'large-single-pass-StrictInterval','large-single-pass-LegacyBounded'}<=set(r['Cases'])
assert (folder/'kernels-native.txt').read_text(encoding='utf-8').strip().endswith('R23_KERNEL_REGRESSIONS_PASS')
assert {'bytes-command-wounds-full-atomic','bytes-command-components-full-atomic','multi-actor-decoded-capacity-atomic','wound-counter-full-atomic','wound-growth-full-atomic','component-growth-full-atomic'}<=set(r['Cases'])
assert (folder/'named-native.txt').read_text(encoding='utf-8').strip().endswith('R25_NAMED_STATE_PASS checks=489 cases=93')
print('R25 independent name/charge/common/full/order oracle PASS; native489/93; loader0')
sys.argv=['verify.py',str(folder)]
runpy.run_path(str(Path(__file__).resolve().parents[2]/'round24-tests/Runtime/verify.py'),run_name='__main__')
