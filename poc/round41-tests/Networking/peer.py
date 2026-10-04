"""Independent synthetic full-pair set, signed-ID ordering, counter/event/work oracle."""
import argparse,json,random,struct
from pathlib import Path
U=lambda x:x&0xffffffff
S=lambda x:x if x<1<<63 else x-(1<<64)
def pair(a,b):return dict(OwnerId=a,MissionId=b)
def command(op,p=None):return dict(Opcode=op,Pair=p)
def update(counter,commands):return dict(Counter=counter,Commands=commands,Extent=len(commands))
def key(p):return S(p['OwnerId']),S(p['MissionId'])
def lookup(a,p):
 cost=0
 for n,e in enumerate(a):
  cost+=2 if e['OwnerId']==p['OwnerId'] else 1
  if key(e)>=key(p):return n,e==p,cost
 return len(a),False,cost
def wire(updates):
 body=bytearray()
 for u in updates:
  body+=struct.pack('<HII',13,u['Extent'],u['Counter'])
  for c in u['Commands']:
   body+=bytes([c['Opcode']])
   if c['Pair'] is not None:body+=struct.pack('<QQ',c['Pair']['OwnerId'],c['Pair']['MissionId'])
 payload=struct.pack('<H',65535)+body
 return (struct.pack('<HIQIBI',5,0x12862153,0x1122334455667788,0x4352454f,4,len(payload))+payload).hex()
def oracle(raw,counter,updates):
 a=[];seedwork=len(raw)
 for p in raw:
  n,found,cost=lookup(a,p);seedwork+=cost+1
  if not found:seedwork+=len(a)-n;a.insert(n,p)
 seedwork+=len(a);initial=list(a);events=[];work=len(a)+len(updates)+sum(len(u['Commands']) for u in updates)
 peak=len(a);peakbytes=8+16*peak;peakoutput=peakbytes
 for u in updates:
  skip=0 if U(counter+u['Extent'])<=u['Counter'] else min(U(counter-u['Counter']+u['Extent']),u['Extent'])
  for ordinal,c in enumerate(u['Commands']):
   if ordinal<skip:continue
   candidate=len(a);op=c['Opcode'];p=c['Pair']
   if op==2:
    if not a:continue
    work+=len(a);a=[]
   else:
    n,found,cost=lookup(a,p);work+=cost
    if (op==0 and not found) or (op==1 and found):continue
    if op==0:work+=len(a)-n;a.pop(n)
    else:candidate+=1;work+=1+len(a)-n;a.insert(n,p)
   counter=U(counter+1);events.append(c);peak=max(peak,candidate);peakbytes=max(peakbytes,8+16*candidate);peakoutput=max(peakoutput,8+16*candidate+32*len(events))
  counter=max(counter,u['Counter'])
 work+=len(a)+len(events)
 return dict(Initial=initial,Pairs=a,Counter=counter,Events=events,SeedWork=seedwork,WorkUsed=work,DecodedOutputBytes=8+16*len(a)+32*len(events),Peak=peak,PeakBytes=peakbytes,PeakOutput=peakoutput)
def generate():
 cases=[]
 def add(name,seed,counter,us):cases.append(dict(Name=name,Seed=seed,SeedCounter=counter,Wire=wire(us),Expected=oracle(seed,counter,us)))
 p=pair(1,2)
 for op in range(3):add('opcode'+str(op),[p],0,[update(1,[command(op,None if op==2 else p)])])
 add('insert-new',[],0,[update(1,[command(1,p)])]);add('missing-remove',[],0,[update(1,[command(0,p)])]);add('empty-clear',[],0,[update(1,[command(2)])])
 add('same-owner-different-mission',[pair(8,9),pair(8,10)],0,[update(1,[command(0,pair(8,9))])]);add('different-owner-same-mission',[pair(8,9)],0,[update(1,[command(1,pair(10,9))])])
 high=[0,1,0x7fffffff,0x80000000,0xffffffff,1<<32,(1<<63)-1,1<<63,(1<<63)+1,(1<<64)-1]
 add('full-highbits-order',[pair(a,b) for a in high for b in high]+[pair(0,0),pair(1<<63,1<<63)],0,[update(1,[command(0,pair(1<<63,(1<<64)-1))])])
 add('lowword-first',[pair(0xffffffff,0),pair(1<<32,0)],0,[update(1,[command(1,pair(0x80000000,0))])])
 add('duplicate-snapshot',[p,p,pair(1,3),p],7,[update(8,[command(1,p)])]);add('future-gap',[],0,[update(100,[command(1,p),command(2)])])
 add('stale-skip',[p],100,[update(2,[command(0,p),command(2)])]);add('half-skip',[],1,[update(2,[command(1,p),command(1,pair(3,4))])])
 add('skip-clear',[p],1,[update(2,[command(2),command(1,pair(3,4))])]);add('wrap-mutation',[p],0xffffffff,[update(0,[command(0,p)])])
 add('wrap-final',[p],0xffffffff,[update(0xffffffff,[command(0,p)])]);add('zero-extent',[p],0,[update(99,[])])
 add('ordered',[],0,[update(1,[command(1,p)]),update(2,[command(2)]),update(3,[command(1,pair(3,4))])])
 add('grow-clear',[],0,[update(2,[command(1,p),command(2)])]);add('snapshot65536dups',[p]*65536,7,[update(7,[])])
 add('commands65536',[],0,[update(65536,[command(2)]*65536)])
 r=random.Random(41);names=[pair(a,b) for a in [0,1,1<<63,(1<<64)-1] for b in [0,2,1<<63,(1<<64)-1]]
 for k in range(100):
  seed=[r.choice(names) for _ in range(r.randrange(8))];counter=r.randrange(100);cs=[command(op:=r.randrange(3),None if op==2 else r.choice(names)) for _ in range(r.randrange(1,12))]
  add('random'+str(k),seed,counter,[update(U(counter+len(cs)+r.randrange(-len(cs),len(cs)+5)),cs)])
 return cases
def main():
 p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify']);p.add_argument('fixture');p.add_argument('export',nargs='?');a=p.parse_args()
 if a.mode=='generate':cases=generate();Path(a.fixture).write_text(json.dumps(cases)+'\n',encoding='utf-8');print('generated',len(cases),'synthetic cases');return
 cases=json.loads(Path(a.fixture).read_text());out=json.loads(Path(a.export).read_text());assert len(out['Records'])==len(cases)
 for c,x in zip(cases,out['Records']):
  assert x['Name']==c['Name'] and x['Wire']==c['Wire'],c['Name']
  for key in ['Initial','Pairs','Counter','Events','WorkUsed','DecodedOutputBytes']:assert x[key]==c['Expected'][key],(c['Name'],key)
 print('PASS independent mission wire/fullpair/signed-order/counter/event/work',len(cases),{k:v for k,v in out.items() if k!='Records'})
if __name__=='__main__':main()
