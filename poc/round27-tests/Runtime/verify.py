"""Independent TANO byte grammar/order/charge oracle plus accepted store/world regressions."""
import json,runpy,sys
from pathlib import Path
folder=Path(sys.argv[1]);fixture=json.loads((Path(__file__).parent/'ordered-fixtures.json').read_text());native=json.loads((folder/'worldtangible-receipt.json').read_text())
codec=runpy.run_path(str(Path(__file__).parents[2]/'round26-tests/Networking/peer.py'))
Reader=codec['Reader'];parse_base=codec['parse_base'];parse_delta=codec['parse_delta'];mask=0xffffffff
base=parse_base(fixture['baseline']);assert base['ObjectId']==fixture['id'] and base['Decoded']==73 and base['Components']==[-1,-2147483648,-1]
assert native['Checks']==132 and len(native['Cases'])==len(set(native['Cases']))==37 and native['ExactTanoWires'] and native['LoaderCalls']==2
seed=base['Decoded']+16+4*len(set(base['Components']))
for receipt in native['Receipts']:
 keys=set(base['Components']);counter=base['Counter'];values={k:base[k] for k in ('File','Key','Units','Raw','Visibility','Options')}
 for update in parse_delta(fixture[receipt['Name']]):
  r=Reader(bytes.fromhex(update['Hex']));index=r.u('H')
  if index==1:values['File']=r.string();r.u('I');values['Key']=r.string()
  elif index==2:values['Units']=r.units()
  elif index==4:values['Raw']=r.string()
  elif index==6:values['Options']=r.u('I')
  elif index==10:values['Visibility']=r.u('B')
  elif index==5:
   count,remote=r.read('II');skip=0 if ((counter+count)&mask)<=remote else min((counter-remote+count)&mask,count)
   for i in range(count):
    op=r.u('B');key=r.u('i') if op<2 else None
    if i<skip:continue
    if op==1 and key not in keys:keys.add(key);counter=(counter+1)&mask
    elif op==0 and key in keys:keys.remove(key);counter=(counter+1)&mask
    elif op==2:keys.clear();counter=(counter+1)&mask
   counter=max(counter,remote)
  else:raise AssertionError(index)
  r.end()
 additional=sum(len(values[k]['Hex'])//2 for k in ('File','Key','Raw'))+2*len(values['Units'])
 assert receipt['BaselineBytes']==base['Decoded'] and receipt['SeedBytes']==seed and receipt['CurrentBytes']==base['Decoded']+16+4*len(keys)+additional
 assert receipt['Keys']==sorted(keys) and receipt['Counter']==counter and receipt['Revision']==2
 for key in ('File','Key','Raw'):assert receipt[key]==values[key]['Hex']
 for key in ('Units','Visibility','Options'):assert receipt[key]==values[key]
 assert receipt['AliasBytes']==base['Decoded']+16+4*len(keys)+2*len(values['Units']) and receipt['EqualBytes']==receipt['AliasBytes']+len(base['Raw']['Hex'])//2
negative=0
for name in ('baseline','forward','reverse'):
 wire=fixture[name];parser=parse_base if name=='baseline' else parse_delta
 for n in range(len(wire)//2):
  try:parser(wire[:n*2])
  except (AssertionError,IndexError):negative+=1
  else:raise AssertionError(('truncated advertised envelope accepted',name,n))
 try:parser(wire+'00')
 except AssertionError:negative+=1
 else:raise AssertionError('tail accepted')
try:parse_delta(fixture['unknown'])
except AssertionError:negative+=1
else:raise AssertionError('creature-only field accepted')
assert {'held-stale-jobs','journal-capacity','world-revision-overflow','direct-ingress-rollback','decoder-ack','baseline-policy-reentry','rollback-global'}<=set(native['Cases'])
print('R27 independent exact TANO order/charge oracle PASS; native132/37; malformed envelope cases='+str(negative))
runpy.run_path(str(Path(__file__).parents[2]/'round26-tests/Runtime/verify.py'),run_name='__main__')
