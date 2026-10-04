"""Independent synthetic raw-byte map/counter/summary/work oracle; no native imports."""
import argparse,json,random,struct
from pathlib import Path
U=lambda x:x&0xffffffff
def entry(key,a=0,b=0,extended=False,raw=255):return dict(Key=key.hex(),Extended=extended or len(key)>=65535,BaseValue=a,Bonus=b,RawCommand=raw)
def command(op,key,a=0,b=0,extended=False):return dict(Opcode=op,**entry(key,a,b,extended))
def update(counter,commands):return dict(Counter=counter,Extent=len(commands),Commands=commands)
def wire(updates):
 body=bytearray()
 for u in updates:
  body+=struct.pack('<HII',3,u['Extent'],u['Counter'])
  for c in u['Commands']:
   key=bytes.fromhex(c['Key']);body+=bytes([c['Opcode']]);body+=struct.pack('<HI',65535,len(key)) if c['Extended'] else struct.pack('<H',len(key));body+=key+struct.pack('<ii',c['BaseValue'],c['Bonus'])
 payload=struct.pack('<H',65535)+body
 return (struct.pack('<HIQIBI',5,0x12862153,0x1122334455667788,0x4352454f,4,len(payload))+payload).hex()
def exported(a):return [dict(Key=k.hex(),Extended=len(k)>=65535,BaseValue=v[0],Bonus=v[1]) for k,v in a]
def lookup(a,key):
 cost=0
 for index,(old,_) in enumerate(a):
  cost+=1+min(len(old),len(key))
  if old>=key:return index,old==key,cost
 return len(a),False,cost

def oracle(raw,counter,updates):
 a=[];seedwork=len(raw)+sum(len(bytes.fromhex(e['Key'])) for e in raw)
 for e in raw:
  key=bytes.fromhex(e['Key']);index,found,search=lookup(a,key);seedwork+=search+1
  if found:a[index]=(a[index][0],(e['BaseValue'],e['Bonus']))
  else:seedwork+=len(a)-index;a.insert(index,(key,(e['BaseValue'],e['Bonus'])))
 seedwork+=len(a)+sum(len(k) for k,_ in a)
 initial=exported(a);keys=sum(len(k) for k,_ in a);events=[];eventkeys=0
 work=len(a)+keys+len(updates)+sum(len(u['Commands'])+sum(len(bytes.fromhex(c['Key'])) for c in u['Commands']) for u in updates)
 peak_count=len(a);peak_keys=keys;peak_bytes=8+16*len(a)+keys;peak_output=peak_bytes;peak_key=max([len(k) for k,_ in a]+[0])
 for u in updates:
  skip=0 if U(counter+u['Extent'])<=u['Counter'] else min(U(counter-u['Counter']+u['Extent']),u['Extent'])
  for ordinal,c in enumerate(u['Commands']):
   if ordinal<skip:continue
   key=bytes.fromhex(c['Key']);peak_key=max(peak_key,len(key));index,found,search=lookup(a,key);work+=search
   old=a[index][1] if found else None;new=(c['BaseValue'],c['Bonus'])
   if c['Opcode']==1 and not found:continue
   if c['Opcode']!=1 and old==new:continue
   candidate_count=len(a)+(0 if found or c['Opcode']==1 else 1);candidate_keys=keys+(0 if found else len(key))
   if c['Opcode']==1:work+=len(a)-index;a.pop(index);keys-=len(key);new=None
   else:
    work+=1+(0 if found else len(a)-index)
    if found:a[index]=(key,new)
    else:a.insert(index,(key,new));keys+=len(key)
   counter=U(counter+1);eventkeys+=len(key)
   events.append(dict(Opcode=c['Opcode'],Key=key.hex(),Extended=len(key)>=65535,OldBase=old[0] if old else None,OldBonus=old[1] if old else None,NewBase=new[0] if new else None,NewBonus=new[1] if new else None))
   peak_count=max(peak_count,candidate_count);peak_keys=max(peak_keys,candidate_keys);peak_bytes=max(peak_bytes,8+16*candidate_count+candidate_keys);peak_output=max(peak_output,8+16*candidate_count+candidate_keys+32*len(events)+eventkeys)
  counter=max(counter,u['Counter'])
 work+=len(a)+keys+len(events)+eventkeys
 return dict(Initial=initial,Entries=exported(a),Counter=counter,Events=events,WorkUsed=work,SeedWork=seedwork,DecodedOutputBytes=8+16*len(a)+keys+32*len(events)+eventkeys,PeakCount=peak_count,PeakKeys=peak_keys,PeakBytes=peak_bytes,PeakOutput=peak_output,PeakKey=peak_key)
def generate():
 cases=[]
 def add(name,seed,counter,us):cases.append(dict(Name=name,Seed=seed,SeedCounter=counter,Wire=wire(us),Expected=oracle(seed,counter,us)))
 for op in range(3):add('opcode'+str(op),[entry(b'x',1,2)],0,[update(1,[command(op,b'x',3,-4)])])
 add('remove-wire-pair-ignored',[entry(b'x',1,2)],0,[update(1,[command(1,b'x',-2147483648,2147483647)])])
 add('missing-remove',[],0,[update(1,[command(1,b'x')])]);add('equal-pair',[entry(b'x',1,2)],0,[update(1,[command(0,b'x',1,2)])])
 add('opcode2-inserts',[],0,[update(1,[command(2,b'x',1,2)])])
 add('raw-byte-order',[entry(b'\xff',1),entry(b'\0x',2),entry(b'',3),entry(b'\x80',4),entry(b'a\0z',5),entry(b'a',6),entry(b'a\0',7)],0,[update(1,[command(0,b'\x7f',8)])])
 add('snapshot-last-wins',[entry(b'x',1,2,raw=1),entry(b'x',3,4,True,raw=254),entry(b'a',5,6)],9,[update(10,[command(2,b'x',7,8)])])
 add('prefix-not-identity',[entry(b'x',1,2,True)],0,[update(1,[command(0,b'x',1,2,False)])])
 add('future-gap',[],0,[update(100,[command(0,b'a',1),command(2,b'b',2)])]);add('stale-skip',[entry(b'a',1)],100,[update(2,[command(0,b'a',2),command(1,b'a')])])
 add('half-skip',[],1,[update(2,[command(0,b'a',1),command(0,b'b',2)])]);add('wrap-mutation',[entry(b'a',1)],0xffffffff,[update(0,[command(2,b'a',2)])])
 add('wrap-final-counter',[entry(b'a',1)],0xffffffff,[update(0xffffffff,[command(2,b'a',2)])])
 add('wrap-sum-forward-gap',[],0xfffffff0,[update(32,[command(0,bytes([k]),k) for k in range(32)])])
 add('zero-extent',[entry(b'a',1)],0,[update(99,[])]);add('counter-only-equal',[entry(b'a',1,2)],0,[update(9,[command(2,b'a',1,2)])])
 add('ordered-repeats',[],0,[update(1,[command(2,b'a',1)]),update(2,[command(1,b'a',999,999)]),update(3,[command(0,b'\0\xff',-1,-2,True)])])
 add('grow-remove-peak',[],0,[update(2,[command(0,b'longer-key',1),command(1,b'longer-key')])])
 for length in [65534,65535]:add('key'+str(length),[],0,[update(1,[command(0,b'k'*length,2147483647,-2147483648)])])
 r=random.Random(40);names=[b'',b'a',b'a\0',b'a\0x',b'\x80',b'\xff',b'\0',b'x']
 for k in range(100):
  seed=[entry(r.choice(names),r.randrange(-20,21),r.randrange(-20,21),r.choice([False,True]),r.randrange(256)) for _ in range(r.randrange(8))];counter=r.randrange(100)
  cs=[command(r.randrange(3),r.choice(names),r.randrange(-20,21),r.randrange(-20,21),r.choice([False,True])) for _ in range(r.randrange(1,12))]
  incoming=U(counter+len(cs)+r.randrange(-len(cs),len(cs)+5));add('random'+str(k),seed,counter,[update(incoming,cs)])
 return cases
def main():
 p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify']);p.add_argument('fixture');p.add_argument('export',nargs='?');a=p.parse_args()
 if a.mode=='generate':cases=generate();Path(a.fixture).write_text(json.dumps(cases)+'\n',encoding='utf-8');print('generated',len(cases),'synthetic cases');return
 cases=json.loads(Path(a.fixture).read_text());out=json.loads(Path(a.export).read_text());assert len(out['Records'])==len(cases)
 for c,x in zip(cases,out['Records']):
  assert x['Name']==c['Name'] and x['Wire']==c['Wire'],c['Name']
  for key in ['Initial','Entries','Counter','Events','WorkUsed','DecodedOutputBytes']:assert x[key]==c['Expected'][key],(c['Name'],key)
 print('PASS independent skill wire/rawkey/map/counter/event/work',len(cases),{k:v for k,v in out.items() if k!='Records'})
if __name__=='__main__':main()
