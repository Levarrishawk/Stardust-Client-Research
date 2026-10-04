"""Independent synthetic CREO4 raw snapshot/scalar, work and capacity oracle."""
import argparse,copy,json,random,struct
from pathlib import Path
REPO=Path(__file__).resolve().parents[3];OID=0xf122334455667789;FLOATS=(0,1,4,5,7,8,9,10,11,12);MASK=0xffffffff
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
def text(raw=b'',ext=False):return dict(Hex=raw.hex().upper(),Extended=ext or len(raw)>=65535)
def string(t):
 raw=bytes.fromhex(t['Hex']);return (P('HI',65535,len(raw)) if t['Extended'] else P('H',len(raw)))+raw
def envelope(fields,kind,count=65535):return P('HIQIBIH',5,0x68a75f0c if kind=='Baseline' else 0x12862153,OID,0x4352454f,4,len(fields)+2,count)+fields
def seed():return dict(ObjectId=OID,DeclaredCount=0,FloatBits=[0x80000000,0x7fc12345,0x3f800000,0xff800000,0x00000001,0x7f800000,0xffffffff,0x7fa00001,0x3f000000,0xbf800000],ListenTo=(1<<64)-1,Encumbrances=dict(Counter=MASK,Values=[-2147483648,0,2147483647]),SkillMods=dict(Counter=MASK,Values=[dict(RawCommand=0,Name=text(b'\x00\xff',True),BaseValue=-2147483648,Bonus=2147483647),dict(RawCommand=255,Name=text(b'\x00\xff'),BaseValue=7,Bonus=-9)]),SpaceMissionObjects=dict(Counter=MASK,Values=[dict(OwnerId=1<<63,MissionId=(1<<64)-1),dict(OwnerId=1<<63,MissionId=(1<<64)-1),dict(OwnerId=0,MissionId=0)]))
def baseline_fields(m):
 out=b''
 for f in range(14):
  if f==2:q=m['Encumbrances'];out+=P('II',len(q['Values']),q['Counter'])+P('i'*len(q['Values']),*q['Values'])
  elif f==3:
   q=m['SkillMods'];out+=P('II',len(q['Values']),q['Counter'])
   for e in q['Values']:out+=P('B',e['RawCommand'])+string(e['Name'])+P('ii',e['BaseValue'],e['Bonus'])
  elif f==13:
   q=m['SpaceMissionObjects'];out+=P('II',len(q['Values']),q['Counter'])
   for e in q['Values']:out+=P('QQ',e['OwnerId'],e['MissionId'])
  elif f==6:out+=P('Q',m['ListenTo'])
  else:out+=P('I',m['FloatBits'][FLOATS.index(f)])
 return out
def delta_fields(updates):return b''.join(P('HQ' if u['Index']==6 else 'HI',u['Index'],u['Bits']) for u in updates)
def parse(raw,kind):
 if len(raw)<25:raise ValueError('header')
 n,op,oid,tag,view,size,count=struct.unpack_from('<HIQIBIH',raw)
 if (n,op,oid,tag,view,size)!=(5,0x68a75f0c if kind=='Baseline' else 0x12862153,OID,0x4352454f,4,len(raw)-23):raise ValueError('envelope')
 pos=25;fields=elements=total=strings=maxstr=0
 def get(fmt):
  nonlocal pos
  width=struct.calcsize('<'+fmt)
  if pos+width>len(raw):raise ValueError('truncated')
  value=struct.unpack_from('<'+fmt,raw,pos)[0];pos+=width;return value
 def collection():
  nonlocal elements,total
  n=get('I');c=get('I')
  if n>65536:raise ValueError('collection hard cap')
  elements=max(elements,n);total+=n;return n,c
 def readtext():
  nonlocal pos,strings,maxstr
  size=get('H');extended=size==65535
  if extended:size=get('I')
  if size>1048576 or pos+size>len(raw):raise ValueError('string')
  t=text(raw[pos:pos+size],extended);pos+=size;strings+=size;maxstr=max(maxstr,size);return t
 if kind=='Baseline':
  m=dict(ObjectId=oid,DeclaredCount=count,FloatBits=[])
  for f in range(14):
   fields+=1
   if f==2:n,c=collection();m['Encumbrances']=dict(Counter=c,Values=[get('i') for _ in range(n)])
   elif f==3:n,c=collection();m['SkillMods']=dict(Counter=c,Values=[dict(RawCommand=get('B'),Name=readtext(),BaseValue=get('i'),Bonus=get('i')) for _ in range(n)])
   elif f==13:n,c=collection();m['SpaceMissionObjects']=dict(Counter=c,Values=[dict(OwnerId=get('Q'),MissionId=get('Q')) for _ in range(n)])
   elif f==6:m['ListenTo']=get('Q')
   else:m['FloatBits'].append(get('I'))
  if pos!=len(raw):raise ValueError('tail')
 else:
  updates=[]
  while pos<len(raw):
   f=get('H');fields+=1
   if f not in FLOATS and f!=6:raise ValueError('unsupported scalar')
   updates.append(dict(Index=f,Bits=get('Q' if f==6 else 'I')))
  if not updates:raise ValueError('empty')
  m=dict(ObjectId=oid,DeclaredCount=count,Updates=updates)
 return m,dict(Payload=len(raw)-23,Fields=fields,Elements=elements,TotalElements=total,MaxString=maxstr,Strings=strings,Decoded=len(raw)-25+10,Work=fields+total+strings)
def modelcost(m):
 ns=[len(m[k]['Values']) for k in ('Encumbrances','SkillMods','SpaceMissionObjects')];lengths=[len(bytes.fromhex(e['Name']['Hex'])) for e in m['SkillMods']['Values']]
 return dict(Elements=max(ns),TotalElements=sum(ns),MaxString=max(lengths,default=0),Strings=sum(lengths),StateBytes=len(baseline_fields(m)),SeedWork=14+sum(ns)+sum(lengths))
def apply(m,updates):
 current=copy.deepcopy(m);events=[];cost=modelcost(m)
 for n,u in enumerate(updates):
  f,bits=u['Index'],u['Bits']
  if f==6:old=current['ListenTo'];current['ListenTo']=bits
  else:i=FLOATS.index(f);old=current['FloatBits'][i];current['FloatBits'][i]=bits
  events.append(dict(Index=f,Ordinal=n,OldBits=old,NewBits=bits))
 cost.update(Updates=len(updates),Events=len(updates),OutputBytes=cost['StateBytes']+24*len(updates),Work=cost['SeedWork']+len(updates)*(1+cost['SeedWork']))
 return dict(Model=current,Revision=2,DecodedBytes=cost['StateBytes']),events,cost
U=lambda f,v:dict(Index=f,Bits=v)
def generate():
 cases=[];inputs=[];patterns=[0,0x80000000,1,0x007fffff,0x00800000,0x3f800000,0x7f7fffff,0x7f800000,0xff800000,0x7fc12345,0x7fa00001,MASK]
 for n,p in enumerate(patterns):
  m=seed();m['DeclaredCount']=(0,14,65535)[n%3];m['FloatBits']=[p]*10;updates=[U(f,patterns[(n+i+1)%len(patterns)]) for i,f in enumerate(FLOATS)]+[U(6,(1<<64)-1 if n%2 else 1<<63)];inputs.append((f'all_scalars_bits_{p:08X}',m,updates))
 for f in (*FLOATS,6):inputs.append((f'repeat_equal_{f}',seed(),[U(f,seed()['ListenTo'] if f==6 else seed()['FloatBits'][FLOATS.index(f)]),U(f,0),U(f,(1<<64)-1 if f==6 else MASK)]))
 for length in (0,1,8,9,10,31):
  m=seed();m['Encumbrances']['Values']=[-(i+1) for i in range(length)];m['SkillMods']['Values']=[];m['SpaceMissionObjects']['Values']=[];inputs.append((f'generic_encumbrances_{length}',m,[U(0,0x7fa00001),U(6,0)]))
 m=seed();m['SkillMods']['Values']=[dict(RawCommand=o,Name=text(b'',bool(i%2)),BaseValue=i,Bonus=-i) for i,o in enumerate((0,1,2,3,128,255))];inputs.append(('all_raw_snapshot_commands_empty_duplicate_keys',m,[U(5,0),U(5,0)]))
 m=seed();m['SkillMods']['Values']=[dict(RawCommand=255,Name=text(b'\x00\xff'*32768,True),BaseValue=-1,Bonus=1)];inputs.append(('long_extended_key',m,[U(6,MASK),U(6,1<<63),U(0,0x80000000)]))
 m=seed();m['SkillMods']['Values']=[dict(RawCommand=1,Name=text(b'a'*17,True),BaseValue=3,Bonus=4),dict(RawCommand=2,Name=text(b'b'*19),BaseValue=5,Bonus=6)];inputs.append(('aggregate_strings',m,[U(11,0),U(12,MASK)]))
 rng=random.Random(38)
 for ix in range(8):
  m=seed();m['FloatBits']=[rng.getrandbits(32) for _ in FLOATS];m['ListenTo']=rng.getrandbits(64);updates=[U(f:=rng.choice((*FLOATS,6)),rng.getrandbits(64 if f==6 else 32)) for _ in range(24)];inputs.append((f'ordered_random_{ix}',m,updates))
 for name,m,updates in inputs:
  raw=envelope(baseline_fields(m),'Baseline',m['DeclaredCount']);decoded,bc=parse(raw,'Baseline');assert decoded==m;delta=envelope(delta_fields(updates),'Delta',65535);plan,dc=parse(delta,'Delta');expected,events,sc=apply(m,updates);initial=dict(Model=m,Revision=1,DecodedBytes=sc['StateBytes']);prefixes=[]
  for offset in range(25,len(delta)):
   partial=bytearray(delta[:offset]);struct.pack_into('<I',partial,19,offset-23)
   try:parse(partial,'Delta')
   except ValueError:pass
   else:prefixes.append(offset)
  cases.append(dict(Name=name,Baseline=raw.hex().upper(),Hex=delta.hex().upper(),BaselineCost=bc,CodecCost=dc,StateCost=sc,Plan=plan,Initial=initial,Expected=expected,Events=events,Work=sc['Work'],Reseed=dict(initial,Revision=3),ValidPrefixes=prefixes))
 malformed=[]
 for f in (2,3,13,14,65535):malformed.append(dict(Kind='Delta',Hex=envelope(P('H',f)+b'\x00'*64,'Delta').hex().upper()))
 malformed.append(dict(Kind='Delta',Hex=envelope(b'','Delta').hex().upper()))
 body=baseline_fields(seed());bad=bytearray(body);struct.pack_into('<I',bad,8,MASK);malformed.append(dict(Kind='Baseline',Hex=envelope(bad,'Baseline').hex().upper()));malformed.append(dict(Kind='Baseline',Hex=envelope(body+b'\x00','Baseline').hex().upper()))
 for kind in ('Baseline','Delta'):
  raw=envelope(body if kind=='Baseline' else delta_fields([U(6,(1<<64)-1)]),kind)
  for off,value in ((0,P('H',4)),(2,P('I',0)),(14,P('I',0x54414e4f)),(18,P('B',6)),(19,P('I',0))):
   bad=bytearray(raw);bad[off:off+len(value)]=value;malformed.append(dict(Kind=kind,Hex=bad.hex().upper()))
 for bad in malformed:
  try:parse(bytes.fromhex(bad['Hex']),bad['Kind'])
  except ValueError:pass
  else:raise AssertionError('malformed independently accepted')
 return dict(Synthetic=True,Cases=cases,Malformed=malformed)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:
  assert a.generate.resolve().is_relative_to((REPO/'local-private').resolve());data=generate();a.generate.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8');print('PASS '+str(len(data['Cases']))+' independent CREO4 fixtures')
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();out=json.loads(a.verify.read_text());assert out['Records']==[{k:c[k] for k in ('Name','Plan','Initial','Expected','Events','Work','Reseed')} for c in peer['Cases']];assert out['Negatives']>1000 and out['Budgets']>500;print('PASS independent CREO4 bytes/raw models/events/work/caps')
