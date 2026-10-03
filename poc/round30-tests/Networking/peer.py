"""Independent bounded TANO6 wire/state/event/work fixtures, no native imports."""
import argparse,importlib.util,json,struct
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('wire28',REPO/'poc/round28-tests/Networking/peer.py');wire=importlib.util.module_from_spec(spec);spec.loader.exec_module(wire)
P=wire.P;MASK=0xffffffff

def baseline(counter,ids,passive=0x76):return wire.env(P('III',passive,len(ids),counter)+P('Q'*len(ids),*ids),True)

def delta(extent,incoming,commands,passive=None):
 fields=wire.vector(extent,incoming,[wire.command(op,index,values) for op,index,values in commands])
 if passive is not None:fields=P('HI',0,passive)+fields+P('HI',0,passive^0xffffffff)
 return wire.env(fields,False,0 if incoming%2 else 65535)

def initial(b):
 v,cost=wire.parse(b,True);return dict(ObjectId=wire.OID,Passive=v['Passive'],Counter=v['Counter'],Ids=v['Ids'],Revision=1,DecodedBytes=8+8*len(v['Ids']))

def apply(old,b):
 updates,cost=wire.parse(b,False);ids=list(old['Ids']);counter=old['Counter'];passive=old['Passive'];events=[]
 work=len(ids)+cost['Fields']+cost['Operations']+cost['Ids'];peak=len(ids);peakoutput=8+8*peak
 def event(kind,index=None,old=None,new=None):events.append(dict(Kind=kind,Index=index,OldId=old,NewId=new))
 def capacity(count,extra):
  nonlocal peak,peakoutput
  peak=max(peak,count);peakoutput=max(peakoutput,8+8*count+24*(len(events)+extra))
 for u in updates:
  if u['Index']==0:passive=u['Passive'];continue
  skip=min((counter-u['Counter']+u['DeclaredOperations'])&MASK,u['DeclaredOperations']);cursor=0
  for c in u['Commands']:
   op,index,values=c['Opcode'],c['Index'],c['Values'];ignored=cursor<skip;cursor+=1+(len(values) if op==3 else 0)
   if ignored:continue
   if op==0:
    if index>=len(ids):continue
    capacity(len(ids),2);work+=len(ids)-index;oldid=ids.pop(index);counter=(counter+1)&MASK;event('Removed',index,oldid);event('Changed')
   elif op==1:
    n=max(len(ids),index)+1;capacity(n,2);work+=n-len(ids)+max(0,len(ids)-index)
    ids.extend([0]*max(0,index-len(ids)));ids.insert(index,values[0]);counter=(counter+1)&MASK;event('Added',index,None,values[0]);event('Changed')
   elif op==2:
    if index<len(ids) and ids[index]==values[0]:continue
    n=max(len(ids),index+1);capacity(n,2);work+=n-len(ids)+1;ids.extend([0]*max(0,index+1-len(ids)));oldid=ids[index];ids[index]=values[0];counter=(counter+1)&MASK;event('Set',index,oldid,values[0]);event('Changed')
   elif op==3:
    capacity(len(values),1);work+=len(ids)+len(values);ids=list(values);counter=(counter+1+len(values))&MASK;event('Changed')
   elif ids:
    capacity(len(ids),len(ids)+1);work+=len(ids)
    for i,oldid in enumerate(ids):event('Removed',i,oldid)
    ids=[];counter=(counter+1)&MASK;event('Changed')
 capacity(len(ids),0);work+=len(ids)+len(events)
 return dict(ObjectId=wire.OID,Passive=passive,Counter=counter,Ids=ids,Revision=old['Revision']+1,DecodedBytes=8+8*len(ids)),events,work,dict(cost,PeakElements=peak,PeakStateBytes=8+8*peak,PeakOutputBytes=peakoutput,Events=len(events),Work=work)

def generate():
 cases=[]
 inputs=[
 ('contiguous_add',1,[7],1,2,[(1,1,[9])]),('duplicate_skip',2,[7],1,2,[(1,1,[9])]),('future_gap',1,[7],1,3,[(1,1,[9])]),('wrap',MASK,[],1,0,[(1,0,[9])]),
 ('equal_then_add',10,[7],2,12,[(2,0,[7]),(1,1,[9])]),('invalid_remove',10,[7],1,11,[(0,65535,[])]),('add_growth',0,[7],1,1,[(1,3,[9])]),('set_growth_zero',0,[7],1,1,[(2,3,[0])]),
 ('clear_empty',5,[],1,6,[(4,0,[])]),('clear_nonempty',5,[7,9],1,6,[(4,0,[])]),('assign_empty',5,[],1,6,[(3,0,[])]),('assign_equal',5,[7,9],3,8,[(3,0,[7,9])]),
 ('assign_overshoot',5,[7],1,6,[(3,0,[9,9])]),('skip_cross_assignment',6,[7],4,9,[(3,0,[9,9]),(4,0,[])]),('overlap',2,[7],2,3,[(1,1,[8]),(1,1,[9])]),('zero_extent',7,[7],0,999,[]),
 ('fullbits_duplicates',MASK-2,[0,MASK+1,0xffffffffffffffff,0],3,MASK,[(2,1,[0xffffffffffffffff]),(0,2,[]),(1,0,[0xffffffffffffffff])]),
 ('assign_wrap',MASK-1,[7],3,1,[(3,0,[0xffffffffffffffff,0])])]
 for name,c,ids,n,u,commands in inputs:
  b=baseline(c,ids);d=delta(n,u,commands,0x80000000 if name=='fullbits_duplicates' else None);old=initial(b);new,events,work,cost=apply(old,d)
  cases.append(dict(Name=name,Baseline=b.hex().upper(),Initial=old,Steps=[dict(Hex=d.hex().upper(),Expected=new,Events=events,Work=work,Cost=cost)]))
 # Repeated field1 stages force recomputation against changed local counter.
 b=baseline(1,[wire.OID,wire.OID,0]);old=initial(b);steps=[]
 for d in [delta(2,3,[(2,0,[wire.OID]),(1,3,[0xffffffffffffffff])]),delta(1,4,[(4,0,[])]),wire.env(wire.vector(1,old['Counter']+1,[wire.command(3,values=[1,2])])+wire.vector(1,7,[wire.command(4)]),False)]:
  new,events,work,cost=apply(old,d);steps.append(dict(Hex=d.hex().upper(),Expected=new,Events=events,Work=work,Cost=cost));old=new
 cases.append(dict(Name='continuous_counter_drift',Baseline=b.hex().upper(),Initial=initial(b),Steps=steps))
 b=baseline(0,[]);d=wire.env(wire.vector(1,1,[wire.command(1,3,[7])])+wire.vector(1,2,[wire.command(4)]),False)
 new,events,work,cost=apply(initial(b),d);cases.append(dict(Name='grow_then_clear',Baseline=b.hex().upper(),Initial=initial(b),Steps=[dict(Hex=d.hex().upper(),Expected=new,Events=events,Work=work,Cost=cost)]))
 b=baseline(0,[]);d=wire.env(wire.vector(1,1,[wire.command(1,0,[1])])+wire.vector(1,2,[wire.command(2,0,[2])]),False)
 new,events,work,cost=apply(initial(b),d);cases.append(dict(Name='repeated_vector_fields',Baseline=b.hex().upper(),Initial=initial(b),Steps=[dict(Hex=d.hex().upper(),Expected=new,Events=events,Work=work,Cost=cost)]))
 for c in cases:c['Reseed']=dict(c['Initial'],Revision=c['Steps'][-1]['Expected']['Revision']+1)
 return dict(Version=1,Synthetic=True,Cases=cases,Malformed=wire.generate()['Malformed'])

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:
  assert a.generate.resolve().is_relative_to((REPO/'local-private').resolve());a.generate.parent.mkdir(parents=True,exist_ok=True);peer=generate();a.generate.write_bytes((json.dumps(peer,indent=2)+'\n').encode());print(f"PASS independent{len(peer['Cases'])} state sequences,{sum(len(c['Steps']) for c in peer['Cases'])} transitions generated")
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();output=json.loads(a.verify.read_text())
  assert output['Records']==[dict(Name=c['Name'],Initial=c['Initial'],Steps=[dict(Expected=s['Expected'],Events=s['Events'],Work=s['Work']) for s in c['Steps']],Reseed=c['Reseed']) for c in peer['Cases']]
  assert output['NegativeChecks']>=100 and output['BudgetChecks']>=80 and output['OwnershipChecks']>=40
  print('PASS independent ordered U64 state/events/work and exact wire oracle')
