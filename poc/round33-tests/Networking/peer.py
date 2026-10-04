"""Synthetic two-field HAM wire/state/event/work oracle, independent of native code."""
import argparse,json,struct,runpy
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
base=runpy.run_path(str(REPO/'poc/round32-tests/Networking/peer.py'))
P=lambda f,*v:struct.pack('<'+f,*v)
MASK=0xffffffff

def command(op,index=0,values=()):return P('B',op)+(P('H',len(values) if op==3 else index) if op!=4 else b'')+P('i'*len(values),*values)
def wire(updates,count=65535):return base['envelope'](b''.join(P('HII',field,extent,counter)+b''.join(command(*c) for c in commands) for field,extent,counter,commands in updates),'Delta',count)
def parse(raw):
 pos=25;updates=[];ops=0;values=0
 def get(fmt):
  nonlocal pos
  n=struct.calcsize('<'+fmt)
  if pos+n>len(raw):raise ValueError('truncated')
  v=struct.unpack_from('<'+fmt,raw,pos);pos+=n;return v[0]
 while pos<len(raw):
  f=get('H')
  if f not in (13,14):raise ValueError('field')
  extent=get('I');counter=get('I');cursor=0;commands=[]
  while cursor<extent:
   op=get('B');cursor+=1;ops+=1;index=0;v=[]
   if op>4:raise ValueError('opcode')
   if op<=2:index=get('H')
   if op in (1,2):v=[get('i')]
   if op==3:n=get('H');cursor+=n;v=[get('i') for _ in range(n)]
   values+=len(v);commands.append(dict(Opcode=op,Index=index,Values=v))
  updates.append(dict(Field=f,Extent=extent,Counter=counter,Commands=commands))
 if not updates:raise ValueError('empty')
 return dict(ObjectId=base['OID'],DeclaredCount=struct.unpack_from('<H',raw,23)[0],Updates=updates),dict(Bytes=len(raw)-25,Fields=len(updates),Commands=ops,Values=values)
def initial(b):return dict(ObjectId=b['ObjectId'],Revision=1,Ham=b['Ham'],MaximumHam=b['MaximumHam'])
def apply(old,updates):
 vectors={13:list(old['Ham']['Values']),14:list(old['MaximumHam']['Values'])};counters={13:old['Ham']['Counter'],14:old['MaximumHam']['Counter']};events=[]
 count=lambda:sum(map(len,vectors.values()))
 work=count()+len(updates)+sum(len(cs)+sum(len(v) for _,_,v in cs) for _,_,_,cs in updates)
 peak_elements=max(map(len,vectors.values()));peak_combined=count();peak_state=16+4*count();peak_output=peak_state
 def cap(field,n):
  nonlocal peak_elements,peak_combined,peak_state,peak_output
  other=len(vectors[27-field]);total=n+other;peak_elements=max(peak_elements,n,other);peak_combined=max(peak_combined,total);peak_state=max(peak_state,16+4*total);peak_output=max(peak_output,16+4*total+24*(len(events)+1))
 for field,extent,incoming,commands in updates:
  skip=min((extent-incoming+counters[field])&MASK,extent);cursor=0;v=vectors[field]
  for op,index,values in commands:
   ignore=cursor<skip;cursor+=1+(len(values) if op==3 else 0)
   if ignore:continue
   old_value=new_value=None;event_index=index if op<=2 else None
   if op==0:
    if index>=len(v):continue
    cap(field,len(v));work+=len(v)-index;old_value=v.pop(index);counters[field]=(counters[field]+1)&MASK
   elif op==1:
    n=max(len(v),index)+1;cap(field,n);work+=n-len(v)+max(0,len(v)-index);v.extend([0]*max(0,index-len(v)));v.insert(index,values[0]);new_value=values[0];counters[field]=(counters[field]+1)&MASK
   elif op==2:
    if index<len(v) and v[index]==values[0]:continue
    n=max(len(v),index+1);cap(field,n);work+=n-len(v)+1;v.extend([0]*max(0,index+1-len(v)));old_value=v[index];v[index]=values[0];new_value=values[0];counters[field]=(counters[field]+1)&MASK
   elif op==3:
    cap(field,len(values));work+=len(v)+len(values);v=list(values);vectors[field]=v;counters[field]=(counters[field]+1+len(values))&MASK
   elif op==4:
    if not v:continue
    cap(field,len(v));work+=len(v);v.clear();counters[field]=(counters[field]+1)&MASK
   events.append(dict(Field=field,Opcode=op,Index=event_index,OldValue=old_value,NewValue=new_value))
 work+=count()+len(events)
 new=dict(ObjectId=old['ObjectId'],Revision=old['Revision']+1,Ham=dict(Counter=counters[13],Values=vectors[13]),MaximumHam=dict(Counter=counters[14],Values=vectors[14]))
 return new,events,work,dict(Elements=peak_elements,Combined=peak_combined,StateBytes=peak_state,OutputBytes=peak_output,Events=len(events),Work=work)
def generate():
 cases=[]
 inputs=[('add',1,[7],[(13,1,2,[(1,1,[9])])]),('duplicate',2,[7],[(13,1,2,[(1,1,[9])])]),('future',1,[7],[(13,1,3,[(1,1,[9])])]),('wrap',MASK,[],[(13,1,0,[(1,0,[-2147483648])])]),('equal_then_add',10,[7],[(13,2,12,[(2,0,[7]),(1,1,[-1])])]),('invalid_remove',10,[7],[(13,1,11,[(0,65535,[])])]),('add_growth',0,[7],[(13,1,1,[(1,3,[-1])])]),('set_growth_zero',0,[7],[(13,1,1,[(2,3,[0])])]),('clear_empty',5,[],[(13,1,6,[(4,0,[])])]),('clear_nonempty',5,[7,-1],[(13,1,6,[(4,0,[])])]),('assign_empty',5,[],[(13,1,6,[(3,0,[])])]),('assign_equal',5,[7,-1],[(13,3,8,[(3,0,[7,-1])])]),('assign_overshoot',5,[7],[(13,1,6,[(3,0,[-1,-1])])]),('skip_cross_assign',6,[7],[(13,4,9,[(3,0,[9,9]),(4,0,[])])]),('zero_extent',7,[7],[(13,0,999,[])]),('assign_wrap',MASK-1,[7],[(13,3,1,[(3,0,[-2147483648,2147483647])])]),('both_combined',0,[],[(13,1,1,[(1,3,[7])]),(14,1,1,[(2,3,[-2147483648])]),(13,1,2,[(4,0,[])])]),('repeated_reconcile',0,[],[(13,1,1,[(1,0,[1])]),(13,1,2,[(2,0,[-1])])]),('maximum_only',0,[99],[(14,3,3,[(3,0,[-1,-1])])])]
 for name,c,ids,updates in inputs:
  b=base['baseline']();b['Ham']=dict(Counter=c,Values=ids);b['MaximumHam']=dict(Counter=0,Values=[-1,-1] if name=='both_combined' else []);b['Frozen']=255;b['Defenders']=dict(Counter=MASK,Values=[0,(1<<64)-1]);bwire=base['envelope'](base['baseline_fields'](b),'Baseline',22);dwire=wire(updates);plan,cost=parse(dwire);old=initial(b);new,events,work,state_cost=apply(old,updates)
  cases.append(dict(Name=name,Baseline=bwire.hex().upper(),Hex=dwire.hex().upper(),Plan=plan,Initial=old,Expected=new,Events=events,Work=work,CodecCost=cost,StateCost=state_cost,Reseed=dict(old,Revision=new['Revision']+1)))
 malformed=[]
 for f in (0,1,12,15,17,65535):malformed.append(base['envelope'](P('H',f),'Delta').hex().upper())
 for body in (P('HIIB',13,1,1,5),P('HIIBH',13,1,1,3,65535),P('HIIBH',14,1,1,2,4),b''):malformed.append(base['envelope'](body,'Delta').hex().upper())
 for raw in malformed:
  try:parse(bytes.fromhex(raw))
  except ValueError:pass
  else:raise AssertionError('Malformed synthetic HAM wire accepted by oracle')
 return dict(Synthetic=True,Cases=cases,Malformed=malformed)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:assert a.generate.resolve().is_relative_to((REPO/'local-private').resolve());a.generate.write_bytes((json.dumps(generate(),indent=2)+'\n').encode());print('PASS 19 synthetic two-vector HAM cases generated')
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();output=json.loads(a.verify.read_text());assert output['Records']==[dict(Name=c['Name'],Plan=c['Plan'],Initial=c['Initial'],Expected=c['Expected'],Events=c['Events'],Work=c['Work'],Reseed=c['Reseed']) for c in peer['Cases']];assert output['Negatives']>500 and output['Budgets']>150 and output['Ownership']>30;print('PASS independent HAM wire/state/counter/event/work oracle')
