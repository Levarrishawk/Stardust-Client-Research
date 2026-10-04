"""Independent bounded MGRP oracle. Original names and words stay in private receipts."""
import contextlib,hashlib,importlib.util,json,random,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
LIMITS=dict(MaximumInputBytes=1048576,MaximumEntries=4096,MaximumNameBytes=4096,MaximumAggregateNameBytes=1048576,MaximumEmbeddedBytes=344064,MaximumSnapshotBytes=2097152,MaximumWork=2097152)
HARD=dict(MaximumInputBytes=(0,16777216),MaximumEntries=(0,65536),MaximumNameBytes=(1,65536),MaximumAggregateNameBytes=(0,16777216),MaximumEmbeddedBytes=(0,5505024),MaximumSnapshotBytes=(0,33554432),MaximumWork=(0,33554432))
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def record(b,at,end,tag,kind=None):
 if at<0 or end>len(b) or end-at<8 or b[at:at+4]!=tag:raise ValueError()
 stop=at+8+struct.unpack_from('>I',b,at+4)[0]
 if stop>end:raise ValueError()
 if kind is not None and (stop-at<12 or b[at+8:at+12]!=kind):raise ValueError()
 return stop
def inspect(b,p):
 end=record(b,0,len(b),b'FORM',b'MGRP')
 if end!=len(b) or record(b,12,end,b'FORM',b'0000')!=end:raise ValueError()
 entries=[];cursor=24;names=0
 while cursor<end:
  stop=record(b,cursor,end,b'FORM',b'MFAM');data=cursor+12;ds=record(b,data,stop,b'DATA')
  if ds-data-8<5:raise ValueError()
  name=b[data+12:ds];key=struct.unpack_from('<i',b,data+8)[0]
  if not name or name[-1]!=0 or 0 in name[:-1] or len(name)>p['MaximumNameBytes']:raise ValueError()
  raw=b[ds:stop]
  if len(raw)!=84 or record(raw,0,84,b'FORM',b'MFRC')!=84 or record(raw,12,84,b'FORM',b'0001')!=84 or record(raw,24,84,b'DATA')!=84 or len(raw[32:])!=52:raise ValueError()
  entries.append(dict(Id=key,Offset=cursor,EncodedBytes=stop-cursor,NameBytes=len(name),NameHex=name.hex().upper(),Words=list(struct.unpack('<13I',raw[32:])),RawHex=raw.hex().upper()));names+=len(name);cursor=stop
 n=len(entries);snapshot=names+136*n;work=len(b)+84*n
 if len(b)>p['MaximumInputBytes'] or n>p['MaximumEntries'] or names>p['MaximumAggregateNameBytes'] or 84*n>p['MaximumEmbeddedBytes'] or snapshot>p['MaximumSnapshotBytes'] or work>p['MaximumWork']:raise ValueError()
 if len({e['Id'] for e in entries})!=n:raise ValueError()
 return dict(SourceSha256=sha(b),InputBytes=len(b),AggregateNameBytes=names,EmbeddedBytes=84*n,SnapshotBytes=snapshot,Work=work,Entries=entries)
def expected(b,p,canceled):
 if any(not low<=p[k]<=high for k,(low,high) in HARD.items()):return dict(Reject='Options',Snapshot=None)
 if canceled:return dict(Reject='Canceled',Snapshot=None)
 try:return dict(Reject='None',Snapshot=inspect(b,p))
 except (ValueError,struct.error,IndexError):return dict(Reject='Input',Snapshot=None)
def chunk(t,b):return t+struct.pack('>I',len(b))+b
def form(t,b):return chunk(b'FORM',t+b)
def raw(words):return form(b'MFRC',form(b'0001',chunk(b'DATA',struct.pack('<13I',*words))))
def family(key,name,words):return form(b'MFAM',chunk(b'DATA',struct.pack('<i',key)+name)+raw(words))
def group(entries):return form(b'MGRP',form(b'0000',b''.join(entries)))
def cases(original):
 out=[]
 def add(b,p=None,canceled=False):
  p=p or LIMITS;out.append(dict(Name='family-'+str(len(out)),Hex=b.hex(),Limits=p,Canceled=canceled,Expected=expected(b,p,canceled)))
 baseline=group([family(k,n,[0xffffffff,0x7f800001,0x7fc12345]+[i]*10) for i,(k,n) in enumerate([(0,b'\0'),(-1,b'\xff\xfe\0'),(-2147483648,b'negative\0'),(2147483647,b'positive\0'),(27,b'odd\0')])])
 add(group([]));add(baseline);add(original)
 rng=random.Random(5001)
 for i in range(24):add(group([family(k,bytes([0x80+i])*i+b'\0',[rng.getrandbits(32) for _ in range(13)]) for k in [i,-i-1]]))
 for length in range(len(baseline)):add(baseline[:length])
 for b in [baseline+b'\0',baseline+baseline,group([family(-1,b'a\0',[0]*13)]*2),group([family(1,b'no-term',[0]*13)]),group([family(1,b'a\0b\0',[0]*13)])]:add(b)
 for offset in [0,8,12,20,24,32,36,45,49,57,65,69,73]:
  b=bytearray(baseline);b[offset]^=1;add(bytes(b))
 for offset in [4,16,28,40,53,65,77]:
  for size in [0,1,0xffffffff]:
   b=bytearray(baseline);struct.pack_into('>I',b,offset,size);add(bytes(b))
 e=inspect(baseline,LIMITS);exact=dict(LIMITS,MaximumInputBytes=e['InputBytes'],MaximumEntries=len(e['Entries']),MaximumNameBytes=max(x['NameBytes'] for x in e['Entries']),MaximumAggregateNameBytes=e['AggregateNameBytes'],MaximumEmbeddedBytes=e['EmbeddedBytes'],MaximumSnapshotBytes=e['SnapshotBytes'],MaximumWork=e['Work']);add(baseline,exact)
 for k in LIMITS:add(baseline,dict(exact,**{k:exact[k]-1}))
 for k,(low,high) in HARD.items():add(baseline,dict(LIMITS,**{k:low-1}));add(baseline,dict(LIMITS,**{k:high+1}))
 add(baseline,canceled=True);add(b'',canceled=True);add(original,dict(LIMITS,MaximumEntries=1));return out
def original(root,config):
 f=load('family50_original','tools/python/verify_terrain_fractal_family_lookup.py');m=load('family50_tgen','poc/round33-tests/Assets/verify_tgen_input.py')
 f.gate(config.read_bytes(),f.CONFIG)
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,config):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 f.gate(payload,f.SOURCE)
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);g=payload[section[2]:section[3]];f.gate(g,f.GENERATOR)
 v,=m.records(g,12,len(g));mg=list(m.records(g,v[2]+12,v[3]))[4]
 assert priority==42 and mg[2]==6602 and mg[3]-mg[2]==5633 and mg[1][:4]==b'MGRP'
 b=g[mg[2]:mg[3]];e=inspect(b,LIMITS);assert e['Entries'][0]['Id']==1
 return b,dict(Priority=priority,GroupOffset=mg[2],GroupBytes=len(b),GroupSha256=sha(b),Entries=len(e['Entries']),NameBytes=e['AggregateNameBytes'],EmbeddedBytes=e['EmbeddedBytes'],SnapshotBytes=e['SnapshotBytes'],Work=e['Work'])
def generate(exe,packages,root,config,output):
 f=load('family50_static','tools/python/verify_terrain_fractal_family_lookup.py');static=f.verify(Path(exe),Path(packages),Path(root),Path(config));b,occ=original(Path(root),Path(config));rows=cases(b)
 Path(output).write_text(json.dumps(dict(Cases=rows)),encoding='utf-8');Path(output).with_name('original-inspector.json').write_text(json.dumps(dict(Static=static,Occurrence=occ)),encoding='utf-8');print(json.dumps(dict(Cases=len(rows),Occurrence=occ)))
def verify(fixture,native):
 fs=json.loads(Path(fixture).read_text(encoding='utf-8'));original_b=bytes.fromhex(fs['Cases'][2]['Hex']);assert fs['Cases']==cases(original_b)
 n=json.loads(Path(native).read_text(encoding='utf-8'));want=[dict(Name=c['Name'],**c['Expected']) for c in fs['Cases']];assert n['Cases']==want,'Independent family oracle mismatch'
 assert n['OwnershipChecks']==sum(3*len(c['Snapshot']['Entries'])+1 for c in want if c['Reject']=='None') and n['AtomicFailureChecks']==sum(c['Reject']!='None' for c in want)
 print(json.dumps(dict(Passed=True,Cases=len(want),Positive=sum(c['Reject']=='None' for c in want),Negative=sum(c['Reject']!='None' for c in want),Ownership=n['OwnershipChecks'],AtomicFailures=n['AtomicFailureChecks'],OriginalEntries=len(want[2]['Snapshot']['Entries']),OriginalSha256=want[2]['Snapshot']['SourceSha256'])))
if __name__=='__main__':
 mode,*args=sys.argv[1:]
 if mode=='generate':generate(*args)
 elif mode=='verify':verify(*args)
 else:raise ValueError(mode)
