"""Synthetic wearable bytes/state/events/work/peak oracle, independent of native code."""
import argparse,copy,json,runpy,struct
from pathlib import Path
REPO=Path(__file__).resolve().parents[3];b=runpy.run_path(str(REPO/'poc/round32-tests/Networking/peer.py'));P=b['P'];text=b['text'];MASK=0xffffffff

def E(id=0,s=b'',ext=False,arr=MASK,crc=0):return dict(Customization=text(s,ext),Arrangement=arr,ObjectId=id,TemplateCrc=crc)
DEFAULT=E()
def elem(e):return b['string'](e['Customization'])+P('IQI',e['Arrangement'],e['ObjectId'],e['TemplateCrc'])
def command(op,index=0,values=()):return P('B',op)+(P('H',len(values) if op==3 else index) if op!=4 else b'')+b''.join(elem(e) for e in values)
def wire(updates):return b['envelope'](b''.join(P('HII',15,extent,counter)+b''.join(command(*c) for c in cs) for extent,counter,cs in updates),'Delta',65535)
def parse(raw):
 if len(raw)<25:raise ValueError('header')
 n,op,oid,tag,view,size,count=struct.unpack_from('<HIQIBIH',raw)
 if (n,op,oid,tag,view,size)!=(5,0x12862153,b['OID'],0x4352454f,6,len(raw)-23):raise ValueError('header')
 pos=25;updates=[];commands=elements=strings=maxstr=0
 def get(fmt):
  nonlocal pos
  length=struct.calcsize('<'+fmt)
  if pos+length>len(raw):raise ValueError('truncated')
  r=struct.unpack_from('<'+fmt,raw,pos);pos+=length;return r[0]
 def entry():
  nonlocal pos,elements,strings,maxstr
  short=get('H');extended=short==65535;length=get('I') if extended else short
  if length>1048576 or pos+length>len(raw):raise ValueError('string')
  t=text(raw[pos:pos+length],extended);pos+=length;elements+=1;strings+=length;maxstr=max(maxstr,length)
  return dict(Customization=t,Arrangement=get('I'),ObjectId=get('Q'),TemplateCrc=get('I'))
 while pos<len(raw):
  if get('H')!=15:raise ValueError('field')
  extent=get('I');counter=get('I');cursor=0;cs=[]
  while cursor<extent:
   op=get('B');commands+=1;cursor+=1
   if op>4:raise ValueError('opcode')
   ix=get('H') if op<=2 else 0;v=[]
   if op in (1,2):v=[entry()]
   if op==3:
    countv=get('H');cursor+=countv;v=[entry() for _ in range(countv)]
   cs.append(dict(Opcode=op,Index=ix,Values=v))
  updates.append(dict(Field=15,Extent=extent,Counter=counter,Commands=cs))
 if not updates:raise ValueError('empty')
 return dict(ObjectId=oid,DeclaredCount=count,Updates=updates),dict(Payload=len(raw)-23,Fields=len(updates),Commands=commands,Elements=elements,MaxString=maxstr,Strings=strings,Decoded=len(raw)-25,Work=len(updates)+commands+elements+strings)
def metrics(v):
 lengths=[len(bytes.fromhex(e['Customization']['Hex'])) for e in v]
 return dict(Elements=len(v),MaxString=max(lengths,default=0),Strings=sum(lengths),StateBytes=8+sum(len(elem(e)) for e in v))
def initial(seed):return dict(ObjectId=seed['ObjectId'],Revision=1,Wearables=seed['Wearables'],DecodedBytes=metrics(seed['Wearables']['Values'])['StateBytes'])
def apply(old,updates,cost):
 v=copy.deepcopy(old['Wearables']['Values']);counter=old['Wearables']['Counter'];events=[];peak=metrics(v);peak['OutputBytes']=peak['StateBytes'];work=len(v)+peak['Strings']+cost['Work']
 def cap(candidate):
  m=metrics(candidate)
  for k,x in m.items():peak[k]=max(peak[k],x)
  peak['OutputBytes']=max(peak['OutputBytes'],m['StateBytes']+32*(len(events)+1))
 for extent,incoming,cs in updates:
  skip=min((extent-incoming+counter)&MASK,extent);cursor=0
  for op,ix,values in cs:
   ignore=cursor<skip;cursor+=1+(len(values) if op==3 else 0)
   if ignore:continue
   prior=nextid=None;eventix=ix if op<=2 else None
   if op==0:
    if ix>=len(v):continue
    cap(v);work+=len(v)-ix;prior=v.pop(ix)['ObjectId'];counter=(counter+1)&MASK
   elif op==1:
    nv=copy.deepcopy(v);nv.extend(copy.deepcopy(DEFAULT) for _ in range(max(0,ix-len(v))));nv.insert(ix,copy.deepcopy(values[0]));cap(nv);work+=len(nv)-len(v)+max(0,len(v)-ix);v=nv;nextid=values[0]['ObjectId'];counter=(counter+1)&MASK
   elif op==2:
    if ix<len(v) and v[ix]['ObjectId']==values[0]['ObjectId']:continue
    nv=copy.deepcopy(v);nv.extend(copy.deepcopy(DEFAULT) for _ in range(max(0,ix+1-len(v))));prior=nv[ix]['ObjectId'];nv[ix]=copy.deepcopy(values[0]);cap(nv);work+=len(nv)-len(v)+1;v=nv;nextid=values[0]['ObjectId'];counter=(counter+1)&MASK
   elif op==3:
    cap(values);work+=len(v)+len(values);v=copy.deepcopy(values);counter=(counter+len(v)+1)&MASK
   elif op==4:
    if not v:continue
    cap(v);work+=len(v);v=[];counter=(counter+1)&MASK
   events.append(dict(Field=15,Opcode=op,Index=eventix,OldObjectId=prior,NewObjectId=nextid))
  m=metrics(v)
  for k,x in m.items():peak[k]=max(peak[k],x)
  peak['OutputBytes']=max(peak['OutputBytes'],m['StateBytes']+32*len(events))
 final=metrics(v);work+=len(v)+final['Strings']+len(events);peak.update(Events=len(events),Work=work)
 return dict(ObjectId=old['ObjectId'],Revision=2,Wearables=dict(Counter=counter,Values=v),DecodedBytes=final['StateBytes']),events,peak

def generate():
 A=E((1<<64)-1,b'\x00\xff',True,0x12345678,MASK);B=E(0x8000000000000000,b'new',False,0,0x87654321);C=E(17,b'',True);equal=E(A['ObjectId'],b'changed',False,1,2)
 inputs=[('insert',0,[A],[(1,1,[(1,1,[B])])]),('duplicate',1,[A],[(1,1,[(1,1,[B])])]),('future',0,[A],[(1,2,[(1,1,[B])])]),('wrap',MASK,[],[(1,0,[(1,0,[A])])]),('set_same_id_changed_metadata',0,[A],[(1,1,[(2,0,[equal])])]),('set_new_id',0,[A],[(1,1,[(2,0,[B])])]),('set_beyond_zero_id',0,[A],[(1,1,[(2,3,[DEFAULT])])]),('insert_default_growth',0,[A],[(1,1,[(1,3,[B])])]),('set_default_growth',0,[],[(1,1,[(2,3,[C])])]),('remove',0,[A,B,C],[(1,1,[(0,1,[])])]),('remove_invalid',0,[A],[(1,1,[(0,65535,[])])]),('clear',0,[A,B],[(1,1,[(4,0,[])])]),('clear_empty',0,[],[(1,1,[(4,0,[])])]),('assign_empty',0,[],[(1,1,[(3,0,[])])]),('assign_equal',0,[A],[(2,2,[(3,0,[A])])]),('assign_overshoot',0,[A],[(1,1,[(3,0,[A,B,C])])]),('cross_skip_assign',1,[A],[(4,4,[(3,0,[B,C]),(4,0,[])])]),('assign_wrap',MASK-1,[A],[(3,1,[(3,0,[B,C])])]),('repeated_fields',0,[],[(1,1,[(1,0,[A])]),(1,2,[(2,0,[B])]),(1,3,[(1,2,[C])])]),('growth_shrink',0,[],[(2,2,[(1,15,[A]),(4,0,[])])]),('string_growth_shrink',0,[A],[(1,1,[(2,0,[E(1,b'x'*257,True)])]),(1,2,[(2,0,[E(2,b'',True)])])]),('zero_extent',9,[A],[(0,999,[])]),('both_spellings',0,[],[(3,3,[(3,0,[E(1,b'x',False),E(2,b'x',True)])])])]
 inputs += [('same_id_string_only',0,[A],[(1,1,[(2,0,[E(A['ObjectId'],b'different',False,A['Arrangement'],A['TemplateCrc'])])])]),('same_id_arrangement_only',0,[A],[(1,1,[(2,0,[E(A['ObjectId'],b'\x00\xff',True,1,A['TemplateCrc'])])])]),('same_id_crc_only',0,[A],[(1,1,[(2,0,[E(A['ObjectId'],b'\x00\xff',True,A['Arrangement'],1)])])]),('different_high_id_word',0,[E(1,b'old')],[(1,1,[(2,0,[E((1<<32)+1,b'new')])])]),('different_low_id_word',0,[E(1<<32,b'old')],[(1,1,[(2,0,[E((1<<32)+1,b'new')])])]),('long_extended_overwrite',0,[],[(1,1,[(1,0,[E(1,b'x'*65535,True)])]),(1,2,[(2,0,[E(2,b'',True)])])])]
 cases=[]
 for name,counter,values,updates in inputs:
  seed=b['baseline']();seed['Wearables']=dict(Counter=counter,Values=values);seed['Ham']=dict(Counter=99,Values=[-1]);seed['Defenders']=dict(Counter=MASK,Values=[(1<<64)-1]);raw=wire(updates);plan,cost=parse(raw);old=initial(seed);new,events,peak=apply(old,updates,cost);valid=[]
  for offset in range(25,len(raw)):
   partial=bytearray(raw[:offset]);struct.pack_into('<I',partial,19,offset-23)
   try:parse(partial)
   except ValueError:pass
   else:valid.append(offset)
  cases.append(dict(Name=name,Baseline=b['envelope'](b['baseline_fields'](seed),'Baseline').hex().upper(),Hex=raw.hex().upper(),Plan=plan,Initial=old,Expected=new,Events=events,Work=peak['Work'],CodecCost=cost,StateCost=peak,ValidPrefixes=valid,Reseed=dict(old,Revision=3)))
 malformed=[b['envelope'](body,'Delta').hex().upper() for body in (b'',P('H',13),P('HIIB',15,1,1,5),P('HIIBH',15,1,1,3,65535),P('HIIBHHI',15,1,1,1,0,65535,0xffffffff),P('HII',15,MASK,0),P('HIIB',15,0,0,4),P('H',65535))]
 for raw in malformed:
  try:parse(bytes.fromhex(raw))
  except ValueError:pass
  else:raise AssertionError('malformed accepted')
 return dict(Synthetic=True,Cases=cases,Malformed=malformed)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:assert a.generate.resolve().is_relative_to((REPO/'local-private').resolve());a.generate.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('PASS '+str(len(generate()['Cases']))+' synthetic wearable cases')
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();out=json.loads(a.verify.read_text());assert out['Records']==[{k:c[k] for k in ('Name','Plan','Initial','Expected','Events','Work','Reseed')} for c in peer['Cases']];assert out['Negatives']>1000 and out['Budgets']>300;print('PASS independent wearable full values/counters/events/work/peak oracle')
