"""Literal scalar-only wires and provenance/presence oracle; imports no native implementation."""
import json,random,struct,sys
from pathlib import Path
FIELDS=[0,1,4,5,6,7,8,9,10,11,12]
def wire(oid,declared,updates):
 body=b''.join(struct.pack('<H'+('Q' if f==6 else 'I'),f,b) for f,b in updates)
 return (struct.pack('<HIQIBIH',5,0x12862153,oid,0x4352454f,4,len(body)+2,declared)+body).hex().upper()
def parse(raw):
 raw=bytes.fromhex(raw) if isinstance(raw,str) else raw
 if len(raw)<25:raise ValueError('header')
 c,op,oid,tag,view,size,declared=struct.unpack_from('<HIQIBIH',raw)
 if (c,op,tag,view,size)!=(5,0x12862153,0x4352454f,4,len(raw)-23):raise ValueError('envelope')
 pos=25;updates=[];ends=[]
 while pos<len(raw):
  if pos+2>len(raw):raise ValueError('index')
  f=struct.unpack_from('<H',raw,pos)[0];pos+=2
  if f not in FIELDS:raise ValueError('scalar')
  n=8 if f==6 else 4
  if pos+n>len(raw):raise ValueError('word')
  b=struct.unpack_from('<Q' if n==8 else '<I',raw,pos)[0];pos+=n;updates.append((f,b));ends.append(pos)
 if not updates or len(updates)>4096:raise ValueError('count')
 return oid,declared,updates,ends

def row(oid,revision,known):
 fs=[dict(ObjectId=oid,StateRevision=revision,Index=f,Bits=b,Provenance=p) for f,(b,p) in sorted(known.items())]
 return dict(ObjectId=oid,Revision=revision,KnownMask=sum(1<<f for f in known),KnownCount=len(known),DecodedBytes=16+32*len(known),KnownFields=fs)
def apply(oid,revision,known,declared,updates):
 if revision==(1<<64)-1:raise ValueError('revision')
 known=dict(known);revision+=1;events=[]
 for n,(f,b) in enumerate(updates):
  old=known.get(f);p=dict(Revision=revision,DeclaredCount=declared,Ordinal=n,Origin=1)
  events.append(dict(Index=f,Ordinal=n,OldWasKnown=old is not None,OldBits=None if old is None else old[0],NewBits=b,OldProvenance=None if old is None else old[1],Provenance=p));known[f]=(b,p)
 cost=dict(Updates=len(updates),Known=len(known),Events=len(updates),State=16+32*len(known),Output=16+32*len(known)+64*len(updates),Work=22+3*len(updates)+len(known),Payload=2+sum(10 if f==6 else 6 for f,b in updates),MessageDecoded=10+sum(10 if f==6 else 6 for f,b in updates),MessageWork=len(updates))
 return revision,known,events,cost

def generate():
 cases=[]
 def add(name,oid,seed,steps):
  known={};rev=seed;out=[]
  for declared,updates in steps:
   initial=row(oid,rev,known);rev,known,events,cost=apply(oid,rev,known,declared,updates);hex=wire(oid,declared,updates);_,_,_,ends=parse(hex)
   out.append(dict(Hex=hex,Initial=initial,Current=row(oid,rev,known),Events=events,Cost=cost,ValidPrefixes=ends[:-1]))
  cases.append(dict(Name=name,ObjectId=oid,RevisionSeed=seed,Steps=out))
 for f in FIELDS:add('zero'+str(f),0,0,[(0,[(f,0)]),(65535,[(f,0),(f,0)])])
 values=[0,0x80000000,0x7fa00001,0x7fc00000,0xffc00001,1,0xffffffff,0x3f800000]
 add('all-fields-highbits',(1<<64)-1,0,[(65535,[(f,(1<<64)-1 if f==6 else values[n%len(values)]) for n,f in enumerate(FIELDS)]),(0,[(f,0) for f in reversed(FIELDS)])])
 add('same-field-provenance',1<<63,17,[(14,[(0,0),(0,0x80000000),(0,0x7fa00001),(0,0x7fa00001)]),(2,[(6,1<<63),(6,0),(6,(1<<64)-1)])])
 add('revision-last',9,(1<<64)-2,[(1,[(6,0)])])
 add('hard4096',123,0,[(0,[(0,n&0xffffffff) for n in range(4096)])])
 r=random.Random(44)
 for i in range(100):
  steps=[]
  for j in range(5):
   us=[]
   for n in range(r.randrange(1,33)):
    f=r.choice(FIELDS);b=r.getrandbits(64) if f==6 else r.choice(values+[r.getrandbits(32)]);us.append((f,b))
   steps.append((r.choice([0,1,14,65535]),us))
  add('random'+str(i),r.getrandbits(64),r.randrange(8),steps)
 return dict(Synthetic=True,Cases=cases)

if sys.argv[1]=='generate':
 d=generate();Path(sys.argv[2]).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8',newline='\n');print('GENERATED',len(d['Cases']),'cases',sum(len(c['Steps']) for c in d['Cases']))
else:
 d=json.loads(Path(sys.argv[2]).read_text());assert d==generate();r=json.loads(Path(sys.argv[3]).read_text());assert len(r['Records'])==len(d['Cases']);steps=events=0
 for c,a in zip(d['Cases'],r['Records']):
  assert c['Name']==a['Name'];assert len(c['Steps'])==len(a['Steps'])
  for s,b in zip(c['Steps'],a['Steps']):
   oid,declared,us,_=parse(s['Hex']);assert oid==c['ObjectId'];assert b['Hex']==s['Hex'] and b['Initial']==s['Initial'] and b['Current']==s['Current'] and b['Events']==s['Events'],(c['Name'],'model/events')
   assert b['DeclaredCount']==declared and b['WorkUsed']==s['Cost']['Work'] and b['DecodedOutputBytes']==s['Cost']['Output'],(c['Name'],'cost');steps+=1;events+=len(b['Events'])
 print(json.dumps(dict(Passed=True,Cases=len(d['Cases']),Steps=steps,Events=events,Checks=r['Checks'],Negatives=r['Negatives'],Budgets=r['Budgets'],Ownership=r['Ownership']),indent=2))
