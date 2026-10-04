"""Synthetic ordered full CREO6 oracle; no native implementation imports."""
import argparse,copy,json,runpy,struct
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
b=runpy.run_path(str(REPO/'poc/round32-tests/Networking/peer.py'))
h=runpy.run_path(str(REPO/'poc/round33-tests/Networking/peer.py'))
P=b['P'];text=b['text'];S=b['update'];MASK=0xffffffff
NAMES={0:'Passive',2:'Level',3:'PerformanceAnimation',4:'MoodString',5:'WeaponId',6:'GroupId',8:'Guild',9:'TargetId',10:'MoodId',11:'PerformanceStart',12:'PerformanceType',16:'AlternateAppearance',17:'Frozen'}
def H(f,e,c,cmds):return dict(Index=f,Extent=e,Counter=c,Commands=[dict(Opcode=o,Index=i,Values=v) for o,i,v in cmds])
def field(u):
 if u['Index'] not in (13,14):return b['delta_fields'](dict(Updates=[u]))
 return P('HII',u['Index'],u['Extent'],u['Counter'])+b''.join(h['command'](c['Opcode'],c['Index'],c['Values']) for c in u['Commands'])
def parse(raw):
 if len(raw)<25:raise ValueError('header')
 n,op,oid,tag,view,size,count=struct.unpack_from('<HIQIBIH',raw)
 if (n,op,oid,tag,view,size)!=(5,0x12862153,b['OID'],0x4352454f,6,len(raw)-23):raise ValueError('envelope')
 pos=25;updates=[];commands=values=strings=maxstr=0
 def get(fmt):
  nonlocal pos
  length=struct.calcsize('<'+fmt)
  if pos+length>len(raw):raise ValueError('truncated')
  r=struct.unpack_from('<'+fmt,raw,pos);pos+=length;return r[0]
 while pos<len(raw):
  f=get('H')
  if f in (13,14):
   e=get('I');c=get('I');cursor=0;cs=[]
   while cursor<e:
    o=get('B');commands+=1;cursor+=1
    if o>4:raise ValueError('opcode')
    i=get('H') if o<=2 else 0;v=[]
    if o in (1,2):v=[get('i')]
    if o==3:
     nv=get('H');cursor+=nv;v=[get('i') for _ in range(nv)]
    values+=len(v);cs.append(dict(Opcode=o,Index=i,Values=v))
   updates.append(dict(Index=f,Scalar=None,Ham=dict(Field=f,Extent=e,Counter=c,Commands=cs)))
  else:
   t=None;bits=second=0
   if f in (3,4,16):
    short=get('H');extended=short==65535;length=get('I') if extended else short
    if length>1048576 or pos+length>len(raw):raise ValueError('string')
    t=text(raw[pos:pos+length],extended);pos+=length;strings+=length;maxstr=max(maxstr,length)
   elif f==7:bits=get('Q');second=get('Q')
   elif f in b['WIDTH']:bits=get(b['WIDTH'][f])
   else:raise ValueError('field')
   updates.append(dict(Index=f,Scalar=S(f,bits,t,second),Ham=None))
 if not updates:raise ValueError('empty')
 return dict(ObjectId=oid,DeclaredCount=count,Updates=updates),dict(Payload=len(raw)-23,Fields=len(updates),MaxString=maxstr,Strings=strings,Commands=commands,Values=values,Decoded=len(raw)-25+10,Work=len(updates)+commands+values+strings)
def measure(m):
 vs=[m[k]['Values'] for k in ('Defenders','Ham','MaximumHam','Wearables')];ts=[m[k] for k in ('PerformanceAnimation','MoodString','AlternateAppearance')]+[v['Customization'] for v in vs[3]]
 lengths=[len(bytes.fromhex(t['Hex'])) for t in ts];size=102+sum(len(v)*w for v,w in zip(vs,(8,4,4,16)))+sum(n+(6 if t['Extended'] else 2) for t,n in zip(ts,lengths))
 return dict(Elements=max(map(len,vs)),TotalElements=sum(map(len,vs)),MaxString=max(lengths),Strings=sum(lengths),StateBytes=size)
def apply(seed,updates):
 m=copy.deepcopy(seed);events=[];peak=measure(m);peak['OutputBytes']=peak['StateBytes'];work=(len(m['Ham']['Values'])+len(m['MaximumHam']['Values'])) if any(u['Index'] in (13,14) for u in updates) else 0
 for ordinal,u in enumerate(updates):
  i=u['Index'];before=measure(m)
  if i in (13,14):
   old=h['initial'](m);cs=[(c['Opcode'],c['Index'],c['Values']) for c in u['Commands']];new,ev,w,cost=h['apply'](old,[(i,u['Extent'],u['Counter'],cs)])
   other=before['StateBytes']-(16+4*(len(m['Ham']['Values'])+len(m['MaximumHam']['Values'])));othercount=len(m['Defenders']['Values'])+len(m['Wearables']['Values'])
   peak['Elements']=max(peak['Elements'],cost['Elements']);peak['TotalElements']=max(peak['TotalElements'],othercount+cost['Combined']);peak['StateBytes']=max(peak['StateBytes'],other+cost['StateBytes']);peak['OutputBytes']=max(peak['OutputBytes'],other+24*len(events)+cost['OutputBytes']);work+=w
   m['Ham']=new['Ham'];m['MaximumHam']=new['MaximumHam'];events.extend(dict(Kind='HamMutation',Field=e['Field'],UpdateOrdinal=ordinal,Opcode=e['Opcode'],ElementIndex=e['Index'],OldValue=e['OldValue'],NewValue=e['NewValue']) for e in ev)
  else:
   if i==7:m['InviterId']=u['Bits'];m['InviteCounter']=u['SecondId']
   elif i in (3,4,16):m[NAMES[i]]=u['Text']
   elif i==12:m[NAMES[i]]=u['Bits'] if u['Bits']<0x80000000 else u['Bits']-0x100000000
   else:m[NAMES[i]]=u['Bits']
   after=measure(m);work+=19+after['TotalElements']+after['Strings'];events.append(dict(Kind='Assignment',Field=i,UpdateOrdinal=ordinal,Opcode=None,ElementIndex=None,OldValue=None,NewValue=None))
  after=measure(m)
  for k,v in after.items():peak[k]=max(peak[k],v)
  peak['OutputBytes']=max(peak['OutputBytes'],after['StateBytes']+24*len(events))
 peak.update(Events=len(events),Work=work,Updates=len(updates))
 return dict(Model=m,Revision=2,DecodedBytes=measure(m)['StateBytes']),events,peak
def generate():
 cases=[]
 allscalars=[S(i,(1<<(struct.calcsize('<'+b['WIDTH'][i])*8))-1) for i in b['WIDTH']]+[S(7,(1<<64)-1,second=(1<<64)-1)]+[S(i,t=text(bytes([0,255,i]),True)) for i in (3,4,16)]
 fixtures=[('scalar_ham',[S(2,65535),H(13,1,1,[(1,3,[-1])]),S(3,t=text(b'new',True)),H(14,1,1,[(2,2,[-2147483648])])]),('ham_scalar',[H(14,1,1,[(1,2,[2147483647])]),S(12,0x80000000),H(13,1,1,[(3,0,[-1,-1])]),S(7,(1<<64)-1,second=(1<<64)-1)]),('all_supported',allscalars+[H(13,0,999,[]),H(14,0,999,[])]),('growth_shrink',[H(13,1,1,[(1,15,[9])]),S(3,t=text(b'x'*99,True)),H(13,1,2,[(4,0,[])]),S(3,t=text(b'',True))]),('repeated_strings',[S(3,t=text(b'long'*30)),S(4,t=text(b'a'*33,True)),S(3,t=text(b'',True)),S(16,t=text(b'\xff\x00'))]),('equal_noops',[S(0,0x76),H(13,1,1,[(2,0,[7])]),H(14,1,1,[(0,65535,[])])]),('skip_cross',[H(13,4,3,[(3,0,[9,9]),(4,0,[])]),S(17,255)]),('overshoot',[H(13,1,1,[(3,0,[-2147483648,2147483647])]),S(11,0xffffffff)]),('zero_extent',[H(13,0,123,[])]),('both_clear',[H(13,1,1,[(4,0,[])]),H(14,1,1,[(4,0,[])]),S(10,255)]),('interleaved_reconcile',[H(13,1,1,[(1,0,[1])]),S(16,t=text(b'a')),H(13,1,2,[(2,0,[-1])]),H(14,3,3,[(3,0,[2,3])])]),('wrap',[H(13,1,0,[(1,0,[-2147483648])]),S(9,(1<<64)-1)]),('assign_wrap',[S(2,1),H(13,3,1,[(3,0,[-1,-1])])])]
 overrides={}
 for hc in h['generate']()['Cases']:
  name='inherited_'+hc['Name'];seedrow=b['parse'](bytes.fromhex(hc['Baseline']),'Baseline')[0];overrides[name]=seedrow
  hs=[H(u['Field'],u['Extent'],u['Counter'],[(c['Opcode'],c['Index'],c['Values']) for c in u['Commands']]) for u in hc['Plan']['Updates']]
  fixtures.append((name,[S(4,t=text(b'interleave',True))]+hs+[S(12,0xffffffff)]))
 fixtures.append(('long_overwrite',[S(3,t=text(b'x'*65535,True)),H(13,0,999,[]),S(3,t=text(b'',True))]))
 fixtures.append(('within_growth_clear',[S(2,3),H(13,2,2,[(1,15,[9]),(4,0,[])]),S(2,4)]))
 for name,updates in fixtures:
  seed=b['baseline']();seed['Ham']=dict(Counter=MASK if name=='wrap' else MASK-1 if name=='assign_wrap' else 0,Values=[7]);seed['MaximumHam']=dict(Counter=0,Values=[]);seed['Defenders']=dict(Counter=MASK,Values=[0,(1<<64)-1]);seed['Wearables']=dict(Counter=0x12345678,Values=[dict(Customization=text(b'\x00\xff',True),Arrangement=MASK,ObjectId=(1<<64)-1,TemplateCrc=MASK)]);seed['PerformanceAnimation']=text(b'old',True);seed['Frozen']=255
  if name in overrides:seed=overrides[name]
  raw=b['envelope'](b''.join(field(u) for u in updates),'Delta',65535);plan,cost=parse(raw);expected,events,peak=apply(seed,updates);valid=[]
  for offset in range(25,len(raw)):
   partial=bytearray(raw[:offset]);struct.pack_into('<I',partial,19,offset-23)
   try:parse(partial)
   except ValueError:pass
   else:valid.append(offset)
  initial=dict(Model=seed,Revision=1,DecodedBytes=measure(seed)['StateBytes']);cases.append(dict(ValidPrefixes=valid,Name=name,Baseline=b['envelope'](b['baseline_fields'](seed),'Baseline').hex().upper(),Hex=raw.hex().upper(),Plan=plan,Initial=initial,Expected=expected,Events=events,Work=peak['Work'],CodecCost=cost,StateCost=peak,Reseed=dict(initial,Revision=3)))
 malformed=[b['envelope'](body,'Delta').hex().upper() for body in (b'',P('H',1),P('H',15),field(S(2,7))+P('H',1),field(S(2,7))+P('HIIB',13,1,1,5),P('HIIBH',13,1,1,3,65535),P('HHI',3,65535,0xffffffff),P('H',65535))]
 for raw in malformed:
  try:parse(bytes.fromhex(raw))
  except ValueError:pass
  else:raise AssertionError('malformed accepted')
 return dict(Synthetic=True,Cases=cases,Malformed=malformed)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:
  assert a.generate.resolve().is_relative_to((REPO/'local-private').resolve());a.generate.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('PASS '+str(len(generate()['Cases']))+' ordered composite fixtures')
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();out=json.loads(a.verify.read_text());assert out['Records']==[{k:c[k] for k in ('Name','Plan','Initial','Expected','Events','Work','Reseed')} for c in peer['Cases']];assert out['Negatives']>500 and out['Budgets']>200;print('PASS independent composite full model/events/work/peak caps')
