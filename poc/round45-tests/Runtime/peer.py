"""Independent deterministic partial-store model; no native outputs used to generate fixtures."""
import json,random,struct,sys
from pathlib import Path
FIELDS=[0,1,4,5,6,7,8,9,10,11,12]
def generate():
 r=random.Random(4504); cases=[]
 for case in range(64):
  steps=[]
  for step in range(2+case%11):
   actor=step%2; updates=[]
   for ordinal in range(1+(case+step)%17):
    f=FIELDS[(case+step+ordinal)%11]; b=r.getrandbits(64 if f==6 else 32)
    if ordinal%5==0:b=[0,0x80000000,0x7fc01234,0xffffffff][(case+step)%4]
    updates.append([f,b])
   declared=[0,1,65535][step%3]
   body=b''.join(struct.pack('<H'+('Q' if f==6 else 'I'),f,b) for f,b in updates)
   wire=(struct.pack('<HIQIBIH',5,0x12862153,actor+1,0x4352454f,4,len(body)+2,declared)+body).hex().upper()
   steps.append(dict(Actor=actor,Declared=declared,Updates=updates,Wire=wire))
  cases.append(dict(Steps=steps))
 return cases

def expected(cases):
 result=[]
 for case in cases:
  states=[{},{}];revisions=[0,0];queue=[];steps=[];published=set()
  for store_rev,step in enumerate(case['Steps'],1):
   actor=step['Actor']; oid=actor+1; known=states[actor];old_p=len(queue);old_a=len(published);first=actor not in published; revisions[actor]+=1;new=[]
   for ordinal,(f,bits) in enumerate(step['Updates']):
    prior=known.get(f);source=dict(Revision=revisions[actor],DeclaredCount=step['Declared'],Ordinal=ordinal,Origin=1)
    event=dict(Index=f,Ordinal=ordinal,OldWasKnown=prior is not None,OldBits=prior[0] if prior else None,NewBits=bits,OldProvenance=prior[1] if prior else None,Provenance=source)
    record=dict(ObjectId=oid,StoreRevision=store_rev,Event=event);new.append(record);queue.append(record);known[f]=(bits,source)
   published.add(actor);k=len(known);n=len(new);work=23+6*n+k+11*first+old_a+old_p
   state=dict(ObjectId=oid,Revision=revisions[actor],KnownMask=sum(1<<f for f in known),DecodedBytes=16+32*k,Fields=[dict(Index=f,Bits=b,Provenance=p) for f,(b,p) in sorted(known.items())])
   steps.append(dict(State=state,Revision=store_rev,WorkUsed=work,StateBytes=sum(16+32*len(states[a]) for a in published),PendingBytes=64*len(queue),NewEvents=new))
  result.append(dict(Steps=steps,Events=queue))
 return result
if __name__=='__main__':
 command,path,*rest=sys.argv[1:];cases=generate()
 if command=='generate':Path(path).write_text(json.dumps(cases,separators=(',',':'))+'\n',encoding='utf-8')
 elif command=='verify':
  assert json.loads(Path(path).read_text())==cases,'fixture regeneration'
  got=json.loads(Path(rest[0]).read_text());want=expected(cases)
  assert got==want,'independent store state/provenance/ordered journal/work mismatch'
  print('peer PASS cases=%s steps=%s events=%s'%(len(cases),sum(len(c['Steps']) for c in cases),sum(len(s['Updates']) for c in cases for s in c['Steps'])))
 else:raise SystemExit('generate|verify')
