"""Independent synthetic all18 CREO6 wire/model/events/work/peak oracle.
Accepted independent vector oracles are reused; no native output is an input.
"""
import argparse,copy,json,runpy,struct
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
def load(n):return runpy.run_path(str(REPO/f'poc/round{n}-tests/Networking/peer.py'))
b=load(32);h=load(33);a=load(34);w=load(35);d=load(36);P=b['P'];MASK=0xffffffff;S=b['update'];text=b['text'];measure=a['measure'];NAMES=a['NAMES']
KEY={1:'Defenders',13:'Ham',14:'MaximumHam',15:'Wearables'}
def V(f,e,c,cs):return dict(Index=f,Extent=e,Counter=c,Commands=[dict(Opcode=o,Index=i,Values=list(v)) for o,i,v in cs])
def field(u):
 f=u['Index']
 if f not in KEY:return b['delta_fields'](dict(Updates=[u]))
 out=P('HII',f,u['Extent'],u['Counter'])
 for c in u['Commands']:
  o,i,v=c['Opcode'],c['Index'],c['Values'];out+=P('B',o)+(P('H',len(v) if o==3 else i) if o!=4 else b'')
  if f==15:out+=b''.join(w['elem'](e) for e in v)
  else:out+=P(('Q' if f==1 else 'i')*len(v),*v)
 return out
def parse(raw):
 if len(raw)<25:raise ValueError('header')
 n,op,oid,tag,view,size,count=struct.unpack_from('<HIQIBIH',raw)
 if (n,op,oid,tag,view,size)!=(5,0x12862153,b['OID'],0x4352454f,6,len(raw)-23):raise ValueError('envelope')
 pos=25;updates=[];commands=elements=strings=maxstr=0
 def get(fmt):
  nonlocal pos
  size=struct.calcsize('<'+fmt)
  if pos+size>len(raw):raise ValueError('truncated')
  v=struct.unpack_from('<'+fmt,raw,pos)[0];pos+=size;return v
 def string():
  nonlocal pos,strings,maxstr
  n=get('H');ext=n==65535;size=get('I') if ext else n
  if size>1048576 or pos+size>len(raw):raise ValueError('string')
  v=text(raw[pos:pos+size],ext);pos+=size;strings+=size;maxstr=max(maxstr,size);return v
 while pos<len(raw):
  f=get('H');row=dict(Index=f,Scalar=None,Ham=None,Defender=None,Wearable=None)
  if f in KEY:
   e=get('I');c=get('I');cursor=0;cs=[]
   while cursor<e:
    o=get('B');commands+=1;cursor+=1
    if o>4:raise ValueError('opcode')
    i=get('H') if o<=2 else 0;nv=1 if o in (1,2) else get('H') if o==3 else 0
    if o==3:cursor+=nv
    elements+=nv;v=[]
    for _ in range(nv):
     if f==15:v.append(dict(Customization=string(),Arrangement=get('I'),ObjectId=get('Q'),TemplateCrc=get('I')))
     else:v.append(get('Q' if f==1 else 'i'))
    cs.append(dict(Opcode=o,Index=i,Values=v))
   row['Defender' if f==1 else 'Wearable' if f==15 else 'Ham']=dict(Field=f,Extent=e,Counter=c,Commands=cs)
  else:
   t=None;bits=second=0
   if f in (3,4,16):t=string()
   elif f==7:bits=get('Q');second=get('Q')
   elif f in b['WIDTH']:bits=get(b['WIDTH'][f])
   else:raise ValueError('field')
   row['Scalar']=S(f,bits,t,second)
  updates.append(row)
 if not updates:raise ValueError('empty')
 return dict(ObjectId=oid,DeclaredCount=count,Updates=updates),dict(Payload=len(raw)-23,Fields=len(updates),MaxString=maxstr,Strings=strings,Commands=commands,Elements=elements,Decoded=len(raw)-25+10,Work=len(updates)+commands+elements+strings)
def apply(seed,updates,message):
 m=copy.deepcopy(seed);events=[];peak=measure(m);peak['OutputBytes']=peak['StateBytes'];work=18+peak['TotalElements']+peak['Strings']+message['Work']
 def cap(model,ne):
  cost=measure(model)
  for k,v in cost.items():peak[k]=max(peak[k],v)
  peak['OutputBytes']=max(peak['OutputBytes'],cost['StateBytes']+48*ne)
 def ev(kind,f,ordinal,e=None):
  e=e or {};return dict(Kind=kind,Field=f,UpdateOrdinal=ordinal,Opcode=e.get('Opcode'),Index=e.get('Index'),OldId=e.get('OldId',e.get('OldObjectId')),NewId=e.get('NewId',e.get('NewObjectId')),OldValue=e.get('OldValue'),NewValue=e.get('NewValue'))
 for ordinal,u in enumerate(updates):
  f=u['Index'];before=measure(m)
  if f not in KEY:
   work+=18+before['TotalElements']+before['Strings']
   if f==7:m['InviterId']=u['Bits'];m['InviteCounter']=u['SecondId']
   elif f in (3,4,16):m[NAMES[f]]=u['Text']
   elif f==12:m[NAMES[f]]=u['Bits'] if u['Bits']<0x80000000 else u['Bits']-0x100000000
   else:m[NAMES[f]]=u['Bits']
   cost=measure(m);work+=19+cost['TotalElements']+cost['Strings'];events.append(ev('Assignment',f,ordinal));cap(m,len(events));continue
  key=KEY[f];work+=len(m[key]['Values'])
  if f in (13,14):work+=len(m[KEY[27-f]]['Values'])
  if f==15:work+=sum(len(bytes.fromhex(e['Customization']['Hex'])) for e in m[key]['Values'])
  counter=m[key]['Counter'];skip=min((u['Extent']-u['Counter']+counter)&MASK,u['Extent']);cursor=0
  for c in u['Commands']:
   o,i,v=c['Opcode'],c['Index'],c['Values'];ignore=cursor<skip;e=1+(len(v) if o==3 else 0);cursor+=e
   if ignore:continue
   old=copy.deepcopy(m);counter=m[key]['Counter'];incoming=(counter+e)&MASK;cmd=[(o,i,v)]
   if f==1:
    new,es,cost=d['apply'](d['initial'](m),[(e,incoming,cmd)],dict(Work=2+len(v)));used=cost['Work'];m[key]=new[key];kind=lambda x:'Defender'+x['Kind']
   elif f==15:
    stringwork=sum(len(bytes.fromhex(t['Customization']['Hex'])) for t in v);new,es,cost=w['apply'](w['initial'](m),[(e,incoming,cmd)],dict(Work=2+len(v)+stringwork));used=cost['Work'];m[key]=new[key];kind=lambda x:'WearableMutation'
   else:
    new,es,used,cost=h['apply'](h['initial'](m),[(f,e,incoming,cmd)]);m['Ham']=new['Ham'];m['MaximumHam']=new['MaximumHam'];kind=lambda x:'HamMutation'
   work+=used
   if es:
    if o in (0,4):peak["OutputBytes"]=max(peak["OutputBytes"],measure(old)["StateBytes"]+48*len(events)+(24 if f in (13,14) else 32)*len(es))
    cap(m,len(events)+len(es))
   events.extend(ev(kind(x),f,ordinal,x) for x in es);cap(m,len(events))
 peak.update(Updates=len(updates),Events=len(events),Work=work)
 return dict(Model=m,Revision=2,DecodedBytes=measure(m)['StateBytes']),events,peak
def generate():
 fixtures=[]
 # Independently pinned vector corpus plus scalar interleaving on both sides.
 for n,peer in ((33,h),(35,w),(36,d)):
  for c in peer['generate']()['Cases']:
   seed=b['parse'](bytes.fromhex(c['Baseline']),'Baseline')[0];us=[]
   for u in c['Plan']['Updates']:
    f=u.get('Field',15);us.append(V(f,u['Extent'],u['Counter'],[(x['Opcode'],x['Index'],x['Values']) for x in u['Commands']]))
   fixtures.append((f'inherited{n}_{c["Name"]}',seed,[S(4,t=text(b'interleave',True))]+us+[S(12,0xffffffff)]))
 for c in a['generate']()['Cases'][:13]:
  seed=b['parse'](bytes.fromhex(c['Baseline']),'Baseline')[0];us=[]
  for u in c['Plan']['Updates']:
   if u['Scalar'] is not None:us.append(u['Scalar'])
   else:
    q=u['Ham'];us.append(V(q['Field'],q['Extent'],q['Counter'],[(x['Opcode'],x['Index'],x['Values']) for x in q['Commands']]))
  fixtures.append(('inherited34_'+c['Name'],seed,us))
 seed=b['baseline']();seed['Defenders']=dict(Counter=MASK,Values=[0,(1<<64)-1]);seed['Ham']=dict(Counter=MASK,Values=[-1]);seed['MaximumHam']=dict(Counter=MASK,Values=[2147483647]);seed['Wearables']=dict(Counter=MASK,Values=[w['E']((1<<64)-1,b'\x00\xff',True)])
 allfields=[]
 for f in range(18):
  if f in KEY:allfields.append(V(f,1,0,[(1,3,[w['E'](1<<63,b'\xff',True)] if f==15 else [(1<<64)-1] if f==1 else [-2147483648])]))
  elif f in (3,4,16):allfields.append(S(f,t=text(bytes((0,255,f)),True)))
  elif f==7:allfields.append(S(f,(1<<64)-1,second=(1<<64)-1))
  else:allfields.append(S(f,(1<<(struct.calcsize('<'+b['WIDTH'][f])*8))-1))
 fixtures.append(('all18_wrap',seed,allfields))
 mixed=[V(1,1,0,[(1,7,[1<<63])]),S(3,t=text(b'long'*20,True)),V(15,1,0,[(2,8,[w['E'](7,b'raw\x00\xff',True)])]),V(13,1,0,[(2,8,[-1])]),V(14,1,0,[(3,0,[-1,2])]),V(1,1,1,[(4,0,[])]),V(15,1,1,[(4,0,[])]),V(13,1,1,[(4,0,[])]),S(3,t=text(b'',True)),V(14,1,2,[(4,0,[])])]
 fixtures.append(('cross_partition_growth_shrink',seed,mixed))
 for f in (1,13,14,15):
  for op in (0,4):
   longseed=copy.deepcopy(seed);longseed['Wearables']['Values']=[w['E'](7,b'z'*1024,True),w['E'](9,b'keep',False)]
   longseed['Defenders']['Values']=[7,9,11];longseed['Ham']['Values']=[7,-9,11];longseed['MaximumHam']['Values']=[19,-21,23]
   fixtures.append((f'construction_candidate_{f}_{op}',longseed,[S(2,123),S(16,t=text(b'prior',True)),V(f,1,0,[(op,0,[])])]))
 cases=[]
 for name,seed,us in fixtures:
  raw=b['envelope'](b''.join(field(u) for u in us),'Delta',65535);plan,cost=parse(raw);expected,events,peak=apply(seed,us,cost);initial=dict(Model=seed,Revision=1,DecodedBytes=measure(seed)['StateBytes']);valid=[]
  for offset in range(25,len(raw)):
   partial=bytearray(raw[:offset]);struct.pack_into('<I',partial,19,offset-23)
   try:parse(partial)
   except ValueError:pass
   else:valid.append(offset)
  cases.append(dict(Name=name,Baseline=b['envelope'](b['baseline_fields'](seed),'Baseline').hex().upper(),Hex=raw.hex().upper(),Plan=plan,Initial=initial,Expected=expected,Events=events,Work=peak['Work'],CodecCost=cost,StateCost=peak,ValidPrefixes=valid,Reseed=dict(initial,Revision=3)))
 malformed=[b['envelope'](body,'Delta').hex().upper() for body in (b'',P('H',18),P('HIIB',1,1,1,5),P('HIIB',15,1,1,5),P('HIIBH',15,1,1,3,65535),P('HHI',3,65535,MASK),P('HIIBH',1,1,1,1,0)+b'\0'*7)]
 for value in malformed:
  try:parse(bytes.fromhex(value))
  except ValueError:pass
  else:raise AssertionError('Malformed independently accepted')
 return dict(Synthetic=True,Cases=cases,Malformed=malformed)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);args=p.parse_args()
 if args.generate:
  assert args.generate.resolve().is_relative_to((REPO/'local-private').resolve());data=generate();args.generate.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8');print('PASS '+str(len(data['Cases']))+' independent full fixtures')
 else:
  peer=json.loads(args.peer.read_text());assert peer==generate();out=json.loads(args.verify.read_text());assert out['Records']==[{k:c[k] for k in ('Name','Plan','Initial','Expected','Events','Work','Reseed')} for c in peer['Cases']];assert out['Negatives']>1000 and out['Budgets']>1000;print('PASS independent full ordered raw model/counters/events/work/peaks')
