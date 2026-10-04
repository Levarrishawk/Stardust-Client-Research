"""Independent synthetic CREO6 defender U64 wire/state/events/work/peak oracle."""
import argparse,copy,json,runpy,struct
from pathlib import Path
REPO=Path(__file__).resolve().parents[3];b=runpy.run_path(str(REPO/'poc/round32-tests/Networking/peer.py'));P=b['P'];MASK=0xffffffff;U64=(1<<64)-1

def command(op,index=0,values=()):return P('B',op)+(P('H',len(values) if op==3 else index) if op!=4 else b'')+P('Q'*len(values),*values)
def wire(updates,count=65535):return b['envelope'](b''.join(P('HII',1,n,c)+b''.join(command(*v) for v in cs) for n,c,cs in updates),'Delta',count)
def parse(raw):
 if len(raw)<25:raise ValueError('header')
 n,op,oid,tag,view,size,count=struct.unpack_from('<HIQIBIH',raw)
 if (n,op,oid,tag,view,size)!=(5,0x12862153,b['OID'],0x4352454f,6,len(raw)-23):raise ValueError('header')
 pos=25;updates=[];commands=values=0
 def get(fmt):
  nonlocal pos
  length=struct.calcsize('<'+fmt)
  if pos+length>len(raw):raise ValueError('truncated')
  r=struct.unpack_from('<'+fmt,raw,pos);pos+=length;return r[0]
 while pos<len(raw):
  if get('H')!=1:raise ValueError('field')
  extent=get('I');counter=get('I');cursor=0;cs=[]
  while cursor<extent:
   op=get('B');commands+=1;cursor+=1
   if op>4:raise ValueError('opcode')
   ix=get('H') if op<=2 else 0;v=[]
   if op in (1,2):v=[get('Q')]
   if op==3:
    size=get('H');cursor+=size;v=[get('Q') for _ in range(size)]
   values+=len(v);cs.append(dict(Opcode=op,Index=ix,Values=v))
  updates.append(dict(Field=1,Extent=extent,Counter=counter,Commands=cs))
 if not updates:raise ValueError('empty')
 return dict(ObjectId=oid,DeclaredCount=count,Updates=updates),dict(Payload=len(raw)-23,Fields=len(updates),Commands=commands,Values=values,Decoded=len(raw)-25,Work=len(updates)+commands+values)
def initial(seed):return dict(ObjectId=seed['ObjectId'],Revision=1,Defenders=seed['Defenders'],DecodedBytes=8+8*len(seed['Defenders']['Values']))
def apply(old,updates,cost):
 v=list(old['Defenders']['Values']);counter=old['Defenders']['Counter'];events=[];work=len(v)+cost['Work'];peak=len(v);peakoutput=8+8*len(v)
 def cap(count,extra):
  nonlocal peak,peakoutput
  peak=max(peak,count);peakoutput=max(peakoutput,8+8*count+32*(len(events)+extra))
 def event(op,kind,index=None,old=None,new=None):events.append(dict(Field=1,Opcode=op,Kind=kind,Index=index,OldId=old,NewId=new))
 for extent,incoming,cs in updates:
  skip=min((counter-incoming+extent)&MASK,extent);cursor=0
  for op,ix,values in cs:
   ignore=cursor<skip;cursor+=1+(len(values) if op==3 else 0)
   if ignore:continue
   if op==0:
    if ix>=len(v):continue
    cap(len(v),2);work+=len(v)-ix;oldid=v.pop(ix);counter=(counter+1)&MASK;event(op,'Removed',ix,oldid);event(op,'Changed')
   elif op==1:
    size=max(len(v),ix)+1;cap(size,2);work+=size-len(v)+max(0,len(v)-ix);v.extend([0]*max(0,ix-len(v)));v.insert(ix,values[0]);counter=(counter+1)&MASK;event(op,'Added',ix,None,values[0]);event(op,'Changed')
   elif op==2:
    if ix<len(v) and v[ix]==values[0]:continue
    size=max(len(v),ix+1);cap(size,2);work+=size-len(v)+1;v.extend([0]*max(0,ix+1-len(v)));oldid=v[ix];v[ix]=values[0];counter=(counter+1)&MASK;event(op,'Set',ix,oldid,values[0]);event(op,'Changed')
   elif op==3:
    cap(len(values),1);work+=len(v)+len(values);v=list(values);counter=(counter+1+len(v))&MASK;event(op,'Changed')
   elif op==4:
    if not v:continue
    cap(len(v),len(v)+1);work+=len(v)
    for i,oldid in enumerate(v):event(op,'Removed',i,oldid)
    v=[];counter=(counter+1)&MASK;event(op,'Changed')
 cap(len(v),0);work+=len(v)+len(events)
 return dict(ObjectId=old['ObjectId'],Revision=2,Defenders=dict(Counter=counter,Values=v),DecodedBytes=8+8*len(v)),events,dict(Elements=peak,StateBytes=8+8*peak,OutputBytes=peakoutput,Events=len(events),Work=work)
def generate():
 inputs=[('add',0,[7],[(1,1,[(1,1,[U64])])]),('duplicate',1,[7],[(1,1,[(1,1,[U64])])]),('future',0,[7],[(1,2,[(1,1,[U64])])]),('wrap',MASK,[],[(1,0,[(1,0,[U64])])]),('equal',0,[U64],[(1,1,[(2,0,[U64])])]),('set_zero_beyond',0,[U64],[(1,1,[(2,3,[0])])]),('zero_grow_add',0,[U64],[(1,1,[(1,3,[1<<63])])]),('set_high',0,[7],[(1,1,[(2,0,[1<<63])])]),('different_high_word',0,[1],[(1,1,[(2,0,[(1<<32)+1])])]),('different_low_word',0,[1<<32],[(1,1,[(2,0,[(1<<32)+1])])]),('remove',0,[U64,0,U64],[(1,1,[(0,1,[])])]),('invalid_remove',0,[7],[(1,1,[(0,65535,[])])]),('clear',0,[U64,0,U64],[(1,1,[(4,0,[])])]),('clear_empty',0,[],[(1,1,[(4,0,[])])]),('assign_empty',0,[],[(1,1,[(3,0,[])])]),('assign_equal',0,[U64,0,U64],[(4,4,[(3,0,[U64,0,U64])])]),('assign_overshoot',0,[7],[(1,1,[(3,0,[U64,0,U64])])]),('cross_skip',1,[7],[(4,4,[(3,0,[U64,0]),(4,0,[])])]),('assign_wrap',MASK-1,[7],[(3,1,[(3,0,[U64,0])])]),('zero_extent',7,[7],[(0,999,[])]),('growth_clear',0,[],[(2,2,[(1,15,[U64]),(4,0,[])])]),('repeated',0,[],[(1,1,[(1,0,[7])]),(1,2,[(2,0,[U64])]),(1,3,[(1,2,[0])])]),('equal_counter_drift',0,[7],[(2,2,[(2,0,[7]),(1,1,[U64])]),(1,3,[(1,0,[1<<63])])]),('all_commands',0,[7],[(7,7,[(1,0,[U64]),(2,1,[1<<63]),(0,0,[]),(3,0,[0,U64]),(4,0,[])])])]
 inputs += [('maximum_index',0,[],[(1,1,[(2,65535,[U64])])]),('clear_many',0,[U64,0]*64,[(1,1,[(4,0,[])])])]
 cases=[]
 for name,counter,ids,updates in inputs:
  seed=b['baseline']();seed['Defenders']=dict(Counter=counter,Values=ids);seed['Ham']=dict(Counter=MASK,Values=[-1]);seed['Wearables']=dict(Counter=MASK,Values=[dict(Customization=b['text'](b'\x00\xff',True),Arrangement=MASK,ObjectId=U64,TemplateCrc=MASK)]);raw=wire(updates,0 if name=='zero_extent' else 65535);plan,cost=parse(raw);old=initial(seed);new,events,peak=apply(old,updates,cost);valid=[]
  for offset in range(25,len(raw)):
   partial=bytearray(raw[:offset]);struct.pack_into('<I',partial,19,offset-23)
   try:parse(partial)
   except ValueError:pass
   else:valid.append(offset)
  cases.append(dict(Name=name,Baseline=b['envelope'](b['baseline_fields'](seed),'Baseline').hex().upper(),Hex=raw.hex().upper(),Plan=plan,Initial=old,Expected=new,Events=events,Work=peak['Work'],CodecCost=cost,StateCost=peak,ValidPrefixes=valid,Reseed=dict(old,Revision=3)))
 malformed=[b['envelope'](body,'Delta').hex().upper() for body in (b'',P('H',0),P('H',15),P('HIIB',1,1,1,5),P('HIIBH',1,1,1,3,65535),P('HIIBH',1,1,1,1,0)+b'\x00'*7,P('HII',1,MASK,0),P('HIIB',1,0,0,4),P('H',65535))]
 sample=wire([(1,1,[(1,0,[U64])])])
 for offset,value in [(0,P('H',4)),(2,P('I',0)),(14,P('I',0x54414e4f)),(18,P('B',3)),(19,P('I',0))]:
  bad=bytearray(sample);bad[offset:offset+len(value)]=value;malformed.append(bad.hex().upper())
 for raw in malformed:
  try:parse(bytes.fromhex(raw))
  except ValueError:pass
  else:raise AssertionError('malformed accepted')
 return dict(Synthetic=True,Cases=cases,Malformed=malformed)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:assert a.generate.resolve().is_relative_to((REPO/'local-private').resolve());a.generate.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('PASS '+str(len(generate()['Cases']))+' independent defender cases')
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();out=json.loads(a.verify.read_text());assert out['Records']==[{k:c[k] for k in ('Name','Plan','Initial','Expected','Events','Work','Reseed')} for c in peer['Cases']];assert out['Negatives']>1000 and out['Budgets']>300;print('PASS independent raw U64/counter/events/work/peak oracle')
