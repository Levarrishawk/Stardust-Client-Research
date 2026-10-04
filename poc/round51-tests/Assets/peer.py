"""Independent generator-to-family audit; raw original data stays in private fixtures."""
import hashlib,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
family=load('bridge51_family','poc/round50-tests/Assets/peer.py');tgen=load('bridge51_tgen','poc/round33-tests/Assets/verify_tgen_input.py')
DEFAULT=dict(FamilyLimits=family.LIMITS,MaximumSectionBytes=1048576,MaximumEncodedCopyBytes=83886080,MaximumSnapshotBytes=33554432,MaximumWork=117440512)
HARD=dict(MaximumSectionBytes=16777216,MaximumEncodedCopyBytes=83886080,MaximumSnapshotBytes=33554432,MaximumWork=117440512)
def encoded(group,bitmap=None):
 groups=[family.form(t,family.form(v,b'')) for t,v in [(b'SGRP',b'0000'),(b'FGRP',b'0001'),(b'RGRP',b'0000'),(b'EGRP',b'0000')]]+[group]
 if bitmap is not None:groups.append(bitmap)
 return family.form(b'TGEN',family.form(b'0000',b''.join(groups)))
def expectation(blob,p,cancel):
 try:
  tgen.dimensions(blob)
  end=family.record(blob,0,len(blob),b'FORM',b'TGEN')
  if end!=len(blob) or family.record(blob,12,end,b'FORM',b'0000')!=end:raise ValueError()
  groups=list(tgen.records(blob,24,len(blob)))
  if len(groups)<5 or any(g[0]!=b'FORM' for g in groups[:5]) or [g[1][:4] for g in groups[:5]]!=[b'SGRP',b'FGRP',b'RGRP',b'EGRP',b'MGRP']:raise ValueError()
  for g in groups[:5]:
   children=list(tgen.records(blob,g[2]+12,g[3]))
   if len(children)!=1 or children[0][0]!=b'FORM':raise ValueError()
  if len(groups)>5 and groups[5][1][:4]!=b'MGRP' and groups[5][1][:4]!=b'LYRS':raise ValueError()
  if len(groups)>6 and groups[6][1][:4]!=b'LYRS':raise ValueError()
  if len(groups)>7:raise ValueError()
  if len(groups)==7 and groups[5][1][:4]!=b'MGRP':raise ValueError()
  # Native original layers are independently covered by the frozen generator oracle.
  g=groups[4];version,=tgen.records(blob,g[2]+12,g[3]);records=list(tgen.records(blob,version[2]+12,version[3]));b=blob[g[2]:g[3]]
 except (ValueError,AssertionError,struct.error,IndexError):return dict(Stage='Generator',Reject='Input',Snapshot=None)
 if any(not 0<=p[k]<=v for k,v in HARD.items()) or any(not lo<=p['FamilyLimits'][k]<=hi for k,(lo,hi) in family.HARD.items()):return dict(Stage='Bridge',Reject='Options',Snapshot=None)
 if cancel:return dict(Stage='Bridge',Reject='Canceled',Snapshot=None)
 if version[1][:4]!=b'0000':return dict(Stage='Bridge',Reject='Input',Snapshot=None)
 try:
  e=family.inspect(b,p['FamilyLimits']);reserved=67108864+len(b);rw=67108864+len(b)+e['Work']
  if len(b)>p['MaximumSectionBytes'] or reserved>p['MaximumEncodedCopyBytes'] or e['SnapshotBytes']>p['MaximumSnapshotBytes'] or rw>p['MaximumWork']:raise ValueError()
  snapshot=dict(GeneratorSha256=family.sha(blob),GeneratorBytes=len(blob),GroupSha256=e['SourceSha256'],GroupOffset=g[2],GroupBytes=len(b),GroupVersion='0000',EncodedCopyBytes=len(blob)+len(b),ReservedEncodedCopyBytes=reserved,SnapshotBytes=e['SnapshotBytes'],Work=len(blob)+len(b)+e['Work'],ReservedWork=rw,Entries=e['Entries'])
  return dict(Stage='Bridge',Reject='None',Snapshot=snapshot)
 except (ValueError,struct.error,IndexError):return dict(Stage='Bridge',Reject='Input',Snapshot=None)
def cases(original):
 rows=[]
 def add(blob,p=None,cancel=False):
  p=p or DEFAULT;rows.append(dict(Name='bridge-'+str(len(rows)),Hex=blob.hex(),Limits=p,Canceled=cancel,Expected=expectation(blob,p,cancel)))
 b=family.group([family.family(-1,b'\xff\0',[0xffffffff]*13),family.family(27,b'raw\0',[0x7fc12345]*13)]);base=encoded(b)
 add(base);add(original);add(encoded(family.group([])));add(encoded(b,family.form(b'MGRP',family.form(b'0099',b''))))
 groups=list(tgen.records(base,24,len(base)))
 add(family.form(b'TGEN',family.form(b'0000',b''.join(base[x[2]:x[3]] for x in groups[:4]))))
 add(encoded(family.form(b'MGRP',family.form(b'0001',b''))))
 add(encoded(b,b)+b'\0');add(encoded(b,b)+b'')
 add(family.form(b'TGEN',family.form(b'0000',b''.join(base[x[2]:x[3]] for x in groups)+b+b)))
 add(encoded(family.group([family.family(1,b'a\0',[0]*13)]*2)))
 add(encoded(family.group([family.family(1,b'no-terminator',[0]*13)])))
 for index in [0,8,12,20,24,36,44,48,52,56,60,64,68,72,76]:
  z=bytearray(base);z[index]^=1;add(bytes(z))
 for length in [0,1,7,8,12,23,24,len(base)//2,len(base)-1]:add(base[:length])
 # Valid generator framing containing malformed opaque MFAM DATA/extents.
 first=family.family(1,b'n\0',[0]*13)
 for offset in [8,12,20,24,26,34,42,46,50,54]:
  z=bytearray(first);z[offset]^=1;add(encoded(family.group([bytes(z)])))
 e=expectation(base,DEFAULT,False)['Snapshot'];exact=dict(DEFAULT,MaximumSectionBytes=e['GroupBytes'],MaximumEncodedCopyBytes=e['ReservedEncodedCopyBytes'],MaximumSnapshotBytes=e['SnapshotBytes'],MaximumWork=e['ReservedWork']);add(base,exact)
 for k in HARD:add(base,dict(exact,**{k:exact[k]-1}));add(base,dict(DEFAULT,**{k:-1}));add(base,dict(DEFAULT,**{k:HARD[k]+1}))
 for k,(low,high) in family.HARD.items():
  add(base,dict(DEFAULT,FamilyLimits=dict(family.LIMITS,**{k:low-1})));add(base,dict(DEFAULT,FamilyLimits=dict(family.LIMITS,**{k:high+1})))
 for k in family.LIMITS:
  add(base,dict(DEFAULT,FamilyLimits=dict(family.LIMITS,**{k:0})))
 add(base,cancel=True);add(original,cancel=True);return rows
def generate(exe,packages,root,config,output,familyfixture):
 family.generate(exe,packages,root,config,familyfixture)
 m=load('bridge51_lookup','tools/python/verify_terrain_fractal_family_lookup.py')
 import contextlib
 with contextlib.ExitStack() as stack:
  for priority,path in tgen.archives(Path(root),Path(config)):
   payload=tgen.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 m.gate(payload,m.SOURCE);v,=tgen.records(payload,12,len(payload));_,section,_=tgen.records(payload,v[2]+12,v[3]);generator=payload[section[2]:section[3]];m.gate(generator,m.GENERATOR)
 rows=cases(generator);Path(output).write_text(json.dumps(dict(Cases=rows)),encoding='utf-8');print(json.dumps(dict(BridgeCases=len(rows),GeneratorBytes=len(generator))))
def verify(fixture,native,familyfixture,familynative):
 fs=json.loads(Path(fixture).read_text(encoding='utf-8'));assert fs['Cases']==cases(bytes.fromhex(fs['Cases'][1]['Hex']))
 n=json.loads(Path(native).read_text(encoding='utf-8'));want=[dict(Name=c['Name'],**c['Expected']) for c in fs['Cases']]
 for a,b in zip(n['Cases'],want):assert a==b,(a['Name'],a['Stage'],a['Reject'],b['Stage'],b['Reject'])
 assert len(n['Cases'])==len(want)
 assert n['OwnershipChecks']==sum(3*len(c['Snapshot']['Entries'])+1 for c in want if c['Reject']=='None')
 assert n['AtomicFailureChecks']==sum(c['Reject']!='None' for c in want)
 family.verify(familyfixture,familynative)
 print(json.dumps(dict(Passed=True,Cases=len(want),Positive=sum(c['Reject']=='None' for c in want),Negative=sum(c['Reject']!='None' for c in want),Ownership=n['OwnershipChecks'],AtomicFailures=n['AtomicFailureChecks'],Original={k:v for k,v in want[1]['Snapshot'].items() if k!='Entries'})))
if __name__=='__main__':
 mode,*args=sys.argv[1:]
 if mode=='generate':generate(*args)
 elif mode=='verify':verify(*args)
 else:raise ValueError(mode)
