"""Current identity/copy oracle built on frozen raw generator/family grammar; dated fixtures remain untouched."""
import json,sys
from pathlib import Path
import importlib.util
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
frozen=load('identity52_bridge','poc/round51-tests/Assets/peer.py');family=frozen.family;tgen=frozen.tgen

def current(blob,p,cancel):
 # Enforce current policy ranges before relaxing only the historical reservation in the independent grammar oracle.
 relaxed=dict(p,MaximumEncodedCopyBytes=frozen.HARD['MaximumEncodedCopyBytes'],MaximumWork=frozen.HARD['MaximumWork'])
 e=frozen.expectation(blob,relaxed,cancel)
 if e['Stage']=='Bridge' and any(not 0<=p[k]<=v for k,v in frozen.HARD.items()):return dict(Stage='Bridge',Reject='Options',Snapshot=None)
 if e['Reject']!='None':return e
 s=dict(e['Snapshot']);s['ReservedEncodedCopyBytes']=s['EncodedCopyBytes'];s['ReservedWork']=s['Work']
 if s['EncodedCopyBytes']>p['MaximumEncodedCopyBytes'] or s['Work']>p['MaximumWork']:return dict(Stage='Bridge',Reject='Input',Snapshot=None)
 return dict(Stage='Bridge',Reject='None',Snapshot=s)

def bridge_cases(original):
 return [dict(c,Expected=current(bytes.fromhex(c['Hex']),c['Limits'],c['Canceled'])) for c in frozen.cases(original)]

def identity_cases(original):
 base=bytes.fromhex(frozen.cases(original)[0]['Hex']);out=[]
 def add(b,p=None,gp=None,cancel=False,guard=False):
  p=p or frozen.DEFAULT;gp=gp or tgen.DEFAULT
  try:
   if gp['MaximumInputBytes']<24 or gp['MaximumInputBytes']>67108864 or gp['MaximumNodes']<1 or gp['MaximumGroupRecords']<0:raise OverflowError()
   d=tgen.dimensions(b);e0=frozen.expectation(b,frozen.DEFAULT,False)
   if e0['Stage']=='Generator':raise ValueError()
   if len(b)>gp['MaximumInputBytes'] or d[0]>gp['MaximumNodes']:raise ValueError()
   meta=tgen.metadata(b)
   if meta['GroupRecordCount']>gp['MaximumGroupRecords']:raise ValueError()
   e=current(b,p,cancel);identity=dict(EncodedBytes=len(b),SourceSha256=family.sha(b));bridge=e['Snapshot']
   if bridge is not None:bridge={k:v for k,v in bridge.items() if k!='Entries'}
   expected=dict(Stage='Bridge',Reject=e['Reject'],Identity=identity,Bridge=bridge)
  except OverflowError:expected=dict(Stage='Generator',Reject='Options',Identity=None,Bridge=None)
  except (ValueError,AssertionError,IndexError):expected=dict(Stage='Generator',Reject='Input',Identity=None,Bridge=None)
  out.append(dict(Name='identity-'+str(len(out)),Hex=b.hex(),Limits=p,GeneratorLimits=gp,Canceled=cancel,AllocationGuard=guard,Expected=expected))
 for b in [base,original]:
  e=current(b,frozen.DEFAULT,False)['Snapshot'];exact=dict(frozen.DEFAULT,MaximumSectionBytes=e['GroupBytes'],MaximumEncodedCopyBytes=e['EncodedCopyBytes'],MaximumSnapshotBytes=e['SnapshotBytes'],MaximumWork=e['Work'])
  add(b);add(b,exact,dict(tgen.DEFAULT,MaximumInputBytes=len(b)))
  for key in ['MaximumEncodedCopyBytes','MaximumWork','MaximumSnapshotBytes','MaximumSectionBytes']:
   add(b,dict(exact,**{key:exact[key]-1}),guard=b is original)
  add(b,gp=dict(tgen.DEFAULT,MaximumInputBytes=len(b)-1));add(b,gp=dict(tgen.DEFAULT,MaximumNodes=1));add(b,gp=dict(tgen.DEFAULT,MaximumGroupRecords=0));add(b,cancel=True)
 for gp in [dict(tgen.DEFAULT,MaximumInputBytes=23),dict(tgen.DEFAULT,MaximumInputBytes=67108865),dict(tgen.DEFAULT,MaximumNodes=0),dict(tgen.DEFAULT,MaximumGroupRecords=-1)]:add(base,gp=gp)
 for b in [base[:-1],base+b'\0',bytes(24),original[:-1]]:add(b)
 changed=bytearray(base);changed[-1]^=1;add(bytes(changed))
 add(frozen.encoded(family.group([])))
 return out

def generate(exe,packages,root,config,out):
 out=Path(out);frozen.generate(exe,packages,root,config,out/'historical-bridge.json',out/'family-fixtures.json')
 original=bytes.fromhex(json.loads((out/'historical-bridge.json').read_text(encoding='utf-8'))['Cases'][1]['Hex'])
 (out/'bridge-fixtures.json').write_text(json.dumps(dict(Cases=bridge_cases(original))),encoding='utf-8')
 (out/'identity-fixtures.json').write_text(json.dumps(dict(Cases=identity_cases(original))),encoding='utf-8')
 print(json.dumps(dict(IdentityCases=len(identity_cases(original)),BridgeCases=len(bridge_cases(original)))))

def verify(out):
 out=Path(out);bs=json.loads((out/'bridge-fixtures.json').read_text(encoding='utf-8'));original=bytes.fromhex(bs['Cases'][1]['Hex']);assert bs['Cases']==bridge_cases(original)
 bn=json.loads((out/'bridge-native.json').read_text(encoding='utf-8'));want=[dict(Name=c['Name'],**c['Expected']) for c in bs['Cases']];assert bn['Cases']==want
 fs=json.loads((out/'identity-fixtures.json').read_text(encoding='utf-8'));assert fs['Cases']==identity_cases(original)
 n=json.loads((out/'identity-native.json').read_text(encoding='utf-8'));expected=[dict(Name=c['Name'],**c['Expected']) for c in fs['Cases']];assert n['Cases']==expected
 accepted_descriptors=sum(c['Identity'] is not None for c in expected);assert n['OwnershipChecks']==n['CachedGetterChecks']==accepted_descriptors
 assert n['AtomicFailureChecks']==sum(c['Reject']!='None' for c in expected);assert n['PreCopyAllocationGuards']==sum(c['AllocationGuard'] for c in fs['Cases'])
 family.verify(out/'family-fixtures.json',out/'family-native.json')
 print(json.dumps(dict(Passed=True,IdentityCases=len(expected),IdentityPositive=sum(c['Reject']=='None' for c in expected),Ownership=n['OwnershipChecks'],CachedGetterChecks=n['CachedGetterChecks'],PreCopyAllocationGuards=n['PreCopyAllocationGuards'],AtomicFailures=n['AtomicFailureChecks'],CurrentBridgeCases=len(want),CurrentBridgePositive=sum(c['Reject']=='None' for c in want),Original={k:v for k,v in want[1]['Snapshot'].items() if k!='Entries'})))
if __name__=='__main__':
 mode,*args=sys.argv[1:]
 if mode=='generate':generate(*args)
 elif mode=='verify':verify(*args)
 else:raise ValueError(mode)
