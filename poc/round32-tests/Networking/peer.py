"""Independent synthetic CREO6 wire constructor/parser and logical-budget oracle."""
import argparse,json,struct
from pathlib import Path
P=lambda f,*v:struct.pack('<'+f,*v)
OID=0xf122334455667789
MASK=(1<<64)-1
REPO=Path(__file__).resolve().parents[3]
def text(b=b'',extended=False):return dict(Hex=b.hex().upper(),Extended=extended or len(b)>=65535)
def string(t):
 b=bytes.fromhex(t['Hex']);return (P('HI',65535,len(b)) if t['Extended'] else P('H',len(b)))+b
def vector(v,fmt):return P('II',len(v['Values']),v['Counter'])+P(fmt*len(v['Values']),*v['Values'])
def baseline():
 return dict(ObjectId=OID,DeclaredCount=22,Passive=0x76,Defenders=dict(Counter=0,Values=[]),Level=0,
 PerformanceAnimation=text(),MoodString=text(),WeaponId=0,GroupId=0,InviterId=0,InviteCounter=0,Guild=0,TargetId=0,MoodId=0,PerformanceStart=0,PerformanceType=0,
 Ham=dict(Counter=0,Values=[]),MaximumHam=dict(Counter=0,Values=[]),Wearables=dict(Counter=0,Values=[]),AlternateAppearance=text(),Frozen=0)
def baseline_fields(v):
 b=P('I',v['Passive'])+vector(v['Defenders'],'Q')+P('H',v['Level'])+string(v['PerformanceAnimation'])+string(v['MoodString'])
 b+=P('QQQQIQBIi',v['WeaponId'],v['GroupId'],v['InviterId'],v['InviteCounter'],v['Guild'],v['TargetId'],v['MoodId'],v['PerformanceStart'],v['PerformanceType'])
 b+=vector(v['Ham'],'i')+vector(v['MaximumHam'],'i')+P('II',len(v['Wearables']['Values']),v['Wearables']['Counter'])
 for w in v['Wearables']['Values']:b+=string(w['Customization'])+P('IQI',w['Arrangement'],w['ObjectId'],w['TemplateCrc'])
 return b+string(v['AlternateAppearance'])+P('B',v['Frozen'])
def update(index,bits=0,t=None,second=0):return dict(Index=index,Bits=bits,Text=t,SecondId=second)
WIDTH={0:'I',2:'H',5:'Q',6:'Q',8:'I',9:'Q',10:'B',11:'I',12:'I',17:'B'}
def delta_fields(v):
 b=b''
 for u in v['Updates']:
  i=u['Index'];b+=P('H',i)
  b+=string(u['Text']) if i in (3,4,16) else P('QQ',u['Bits'],u['SecondId']) if i==7 else P(WIDTH[i],u['Bits'])
 return b
def envelope(fields,kind,count=22):return P('HIQIBIH',5,0x68a75f0c if kind=='Baseline' else 0x12862153,OID,0x4352454f,6,len(fields)+2,count)+fields

def parse(wire,kind):
 if len(wire)<25:raise ValueError('header')
 n,op,oid,tag,view,size,declared=struct.unpack_from('<HIQIBIH',wire)
 if n!=5 or op!=(0x68a75f0c if kind=='Baseline' else 0x12862153) or tag!=0x4352454f or view!=6 or size!=len(wire)-23:raise ValueError('envelope')
 pos=25;fields=0;elements=0;strings=0;max_elements=0;max_string=0
 def get(fmt):
  nonlocal pos
  length=struct.calcsize('<'+fmt)
  if pos+length>len(wire):raise ValueError('truncated')
  result=struct.unpack_from('<'+fmt,wire,pos);pos+=length;return result[0] if len(result)==1 else list(result)
 def txt():
  nonlocal pos,strings,max_string
  short=get('H');extended=short==65535;length=get('I') if extended else short
  if length>1048576 or pos+length>len(wire):raise ValueError('string')
  value=text(wire[pos:pos+length],extended);pos+=length;strings+=length;max_string=max(max_string,length);return value
 def vec(fmt):
  nonlocal elements,max_elements
  count=get('I');counter=get('I')
  if count>65536:raise ValueError('count')
  elements+=count;max_elements=max(max_elements,count);return dict(Counter=counter,Values=[get(fmt) for _ in range(count)])
 if kind=='Baseline':
  fields=18;v=dict(ObjectId=oid,DeclaredCount=declared,Passive=get('I'),Defenders=vec('Q'),Level=get('H'),PerformanceAnimation=txt(),MoodString=txt(),WeaponId=get('Q'),GroupId=get('Q'),InviterId=get('Q'),InviteCounter=get('Q'),Guild=get('I'),TargetId=get('Q'),MoodId=get('B'),PerformanceStart=get('I'),PerformanceType=get('i'),Ham=vec('i'),MaximumHam=vec('i'))
  count=get('I');counter=get('I')
  if count>65536:raise ValueError('wearable count')
  elements+=count;max_elements=max(max_elements,count);wearables=[]
  for _ in range(count):wearables.append(dict(Customization=txt(),Arrangement=get('I'),ObjectId=get('Q'),TemplateCrc=get('I')))
  v.update(Wearables=dict(Counter=counter,Values=wearables),AlternateAppearance=txt(),Frozen=get('B'))
 else:
  updates=[]
  while pos<len(wire):
   fields+=1;i=get('H')
   if i in (3,4,16):updates.append(update(i,t=txt()))
   elif i==7:updates.append(update(i,get('Q'),second=get('Q')))
   elif i in WIDTH:updates.append(update(i,get(WIDTH[i])))
   else:raise ValueError('unsupported index')
  if not updates:raise ValueError('empty delta')
  v=dict(ObjectId=oid,DeclaredCount=declared,Updates=updates)
 if pos!=len(wire):raise ValueError('tail')
 return v,dict(PayloadBytes=len(wire)-23,Fields=fields,StringBytes=max_string,TotalStringBytes=strings,Elements=max_elements,TotalElements=elements,DecodedBytes=len(wire)-25+10,Work=fields+elements+strings)

def generate():
 cases=[]
 def add(name,v,kind):
  fields=baseline_fields(v) if kind=='Baseline' else delta_fields(v);wire=envelope(fields,kind,v['DeclaredCount']);expected,cost=parse(wire,kind);assert expected==v
  prefixes=[]
  if kind=='Delta':
   cuts=range(len(wire)) if len(wire)<1024 else list(range(80))+list(range(len(wire)-80,len(wire)))
   for n in cuts:
    if n<25:continue
    shorter=bytearray(wire[:n]);shorter[19:23]=P('I',n-23)
    try:prefix,_=parse(bytes(shorter),kind)
    except (ValueError,struct.error):continue
    prefixes.append(dict(Length=n,Expected=prefix))
  cases.append(dict(Name=name,Kind=kind,Hex=wire.hex().upper(),Expected=expected,Cost=cost,ValidPrefixes=prefixes))
 add('empty_server22',baseline(),'Baseline')
 b=baseline();b.update(DeclaredCount=65535,Passive=0xffffffff,Level=65535,PerformanceAnimation=text(b'\0\xff\x80'),MoodString=text(b'mood',True),WeaponId=MASK,GroupId=1<<63,InviterId=0,InviteCounter=MASK,Guild=0xffffffff,TargetId=MASK-1,MoodId=255,PerformanceStart=0x80000000,PerformanceType=-2147483648,Frozen=255,AlternateAppearance=text(b'\xff\0',True))
 b['Defenders']=dict(Counter=0xffffffff,Values=[0,MASK,MASK,1<<63]);b['Ham']=dict(Counter=0x80000000,Values=[-2147483648,-1,0,2147483647]);b['MaximumHam']=dict(Counter=0xffffffff,Values=[-1,-1,2147483647]);w=dict(Customization=text(b'\0\x80\xff',True),Arrangement=0xffffffff,ObjectId=MASK,TemplateCrc=0x80000000);b['Wearables']=dict(Counter=0xfffffffe,Values=[w,w]);add('fullbits_duplicate_order',b,'Baseline')
 b=baseline();b['DeclaredCount']=0;b['PerformanceAnimation']=text(b'',True);b['MoodString']=text(b'x',True);b['AlternateAppearance']=text(b'',True);b['Wearables']=dict(Counter=17,Values=[dict(Customization=text(b'',True),Arrangement=0,ObjectId=0,TemplateCrc=0)]);add('extended_small_empty',b,'Baseline')
 for length in (65534,65535,65536):
  b=baseline();b['PerformanceAnimation']=text(bytes((i%256 for i in range(length))));add(f'string_boundary_{length}',b,'Baseline')
 b=baseline();b['Defenders']=dict(Counter=55,Values=list(range(64)));b['Ham']=dict(Counter=66,Values=list(range(-32,32)));b['MaximumHam']=dict(Counter=77,Values=[9]*64);b['Wearables']=dict(Counter=88,Values=[dict(Customization=text(bytes([i])),Arrangement=i,ObjectId=i,TemplateCrc=i) for i in range(16)]);add('aggregate_vectors_strings',b,'Baseline')
 updates=[update(i,(1<<(8*struct.calcsize('<'+fmt)))-1) for i,fmt in WIDTH.items()]
 updates+=[update(3,t=text(b'\0\xff',True)),update(4,t=text(b'',True)),update(7,MASK,second=1<<63),update(16,t=text(b'appearance'))]
 add('ordered_union_repeated',dict(ObjectId=OID,DeclaredCount=0,Updates=updates+list(reversed(updates))),'Delta')
 for u in updates:add('single_'+str(u['Index']),dict(ObjectId=OID,DeclaredCount=65535,Updates=[u]),'Delta')
 add('extended_delta_boundary',dict(ObjectId=OID,DeclaredCount=22,Updates=[update(16,t=text(b'\xff'*65535)),update(0,0x80000000)]),'Delta')
 malformed=[]
 def bad(name,wire,kind):
  try:parse(wire,kind)
  except (ValueError,struct.error):pass
  else:raise AssertionError('oracle accepted '+name)
  malformed.append(dict(Name=name,Kind=kind,Hex=wire.hex().upper()))
 for i in (1,13,14,15,18,65535):bad(f'unsupported_{i}',envelope(P('HI',0,9)+P('H',i),'Delta'),'Delta')
 bad('empty_delta',envelope(b'','Delta'),'Delta')
 for fields in [P('HH',3,65535),P('HHI',4,65535,0xffffffff),P('HH',16,4)+b'ab',P('H',7)+P('Q',MASK),P('H',2)+b'\xff']:
  bad('truncated_delta_'+str(len(malformed)),envelope(fields,'Delta'),'Delta')
 raw=baseline_fields(baseline());bad('baseline_tail',envelope(raw+b'\0','Baseline'),'Baseline');bad('baseline_missing_frozen',envelope(raw[:-1],'Baseline'),'Baseline');bad('defender_count_overflow',envelope(raw[:4]+P('I',0xffffffff)+raw[8:],'Baseline'),'Baseline')
 for offset in (71,79,87):bad('nested_count_'+str(offset),envelope(raw[:offset]+P('I',0xffffffff)+raw[offset+4:],'Baseline'),'Baseline')
 for offset,data in [(0,b'\x04\0'),(2,P('I',0)),(14,P('I',0x54414e4f)),(18,b'\x03'),(19,P('I',0))]:
  wire=bytearray(envelope(raw,'Baseline'));wire[offset:offset+len(data)]=data;bad('header_'+str(offset),bytes(wire),'Baseline')
 return dict(Version=1,Synthetic=True,Cases=cases,Malformed=malformed)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:
  assert a.generate.resolve().is_relative_to((REPO/'local-private').resolve());a.generate.parent.mkdir(parents=True,exist_ok=True);value=generate();a.generate.write_bytes((json.dumps(value,indent=2)+'\n').encode());print(f"PASS {len(value['Cases'])} independent synthetic CREO6 wires generated")
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();output=json.loads(a.verify.read_text());assert output['Records']==[dict(Name=c['Name'],Kind=c['Kind'],Expected=c['Expected'],Hex=c['Hex']) for c in peer['Cases']];assert output['NegativeChecks']>500 and output['BudgetChecks']>200 and output['OwnershipChecks']>20;print('PASS independent complete CREO6 models, exact bytes and global budget oracle')
