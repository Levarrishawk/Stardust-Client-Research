"""Independent literal TANO6 wires and structural/cost oracle; no native imports."""
import argparse,json,struct
from pathlib import Path
P=lambda f,*v:struct.pack('<'+f,*v)
OID=0xF122334455667789
TAG=0x54414E4F

def env(fields,baseline,count=65535):
 return P('HIQIBIH',5,0x68A75F0C if baseline else 0x12862153,OID,TAG,6,len(fields)+2,count)+fields

def command(op,index=0,values=()):
 return P('B',op)+(P('H',len(values) if op==3 else index) if op!=4 else b'')+P('Q'*len(values),*values)

def vector(declared,counter,commands):return P('HII',1,declared,counter)+b''.join(commands)

def parse(wire,baseline):
 count,op,oid,tag,view,size,declared=struct.unpack_from('<HIQIBIH',wire)
 assert count==5 and op==(0x68A75F0C if baseline else 0x12862153) and tag==TAG and view==6 and size==len(wire)-23
 pos=25;cost=dict(Bytes=len(wire)-25,Fields=0,Operations=0,Assignments=0,Ids=0)
 def get(fmt):
  nonlocal pos
  n=struct.calcsize('<'+fmt)
  if pos+n>len(wire):raise ValueError('truncated')
  v=struct.unpack_from('<'+fmt,wire,pos);pos+=n;return v[0] if len(v)==1 else list(v)
 if baseline:
  passive,n,counter=get('III');ids=[get('Q') for _ in range(n)];result=dict(Passive=passive,Counter=counter,Ids=ids);cost.update(Fields=2,Ids=n)
 else:
  updates=[]
  while pos<len(wire):
   index=get('H');cost['Fields']+=1
   if index==0:updates.append(dict(Index=0,Passive=get('I')));continue
   if index!=1:raise ValueError('field')
   extent,counter=get('II');units=0;commands=[]
   while units<extent:
    op=get('B');units+=1;cost['Operations']+=1
    if op>4:raise ValueError('operation')
    ix=0;values=[]
    if op in (0,1,2):ix=get('H')
    if op in (1,2):values=[get('Q')]
    if op==3:
     n=get('H');units+=n;cost['Assignments']+=n;values=[get('Q') for _ in range(n)]
    cost['Ids']+=len(values);commands.append(dict(Opcode=op,Index=ix,Values=values))
   updates.append(dict(Index=1,DeclaredOperations=extent,Counter=counter,Commands=commands))
  if not updates:raise ValueError('empty delta')
  result=updates
 if pos!=len(wire):raise ValueError('tail')
 return result,cost

def generate():
 cases=[]
 for i,ids in enumerate([[],[0],[0xffffffffffffffff],[OID,OID,0],[0x8000000000000000,1],list(range(8))]):
  fields=P('III',0x76 if i%2 else 0xffffffff,len(ids),0xffffffff-i)+P('Q'*len(ids),*ids)
  cases.append(dict(Name='baseline_'+str(i),Kind='Baseline',Hex=env(fields,True,0 if i%2 else 65535).hex().upper()))
 v=lambda extent,cmds:vector(extent,0xffffffff,cmds)
 fields=[P('HI',0,0xffffffff),v(0,[]),v(1,[command(0,65535)]),v(1,[command(1,0,[OID])]),v(1,[command(2,65535,[0xffffffffffffffff])]),v(1,[command(4)]),v(1,[command(3)]),v(4,[command(3,values=[OID,0,OID])]),v(1,[command(3,values=[0,0xffffffffffffffff])])+P('HI',0,0x76),P('HI',0,1)+v(5,[command(1,5,[1]),command(2,4,[2]),command(0,3),command(4),command(3)])+P('HI',0,2),v(4,[command(3,values=[1,2]),command(4)])+v(1,[command(3,values=[3])])]
 for i,b in enumerate(fields):cases.append(dict(Name='delta_'+str(i),Kind='Delta',Hex=env(b,False,i%2*65535).hex().upper()))
 for c in cases:c['Expected'],c['Cost']=parse(bytes.fromhex(c['Hex']),c['Kind']=='Baseline')
 malformed=[]
 for name,b in [('unknown_field',P('HI',2,0)),('unknown_command',v(1,[P('B',5)])),('assignment_short',v(1,[P('BH',3,2)+P('Q',1)])),('missing_loop_command',v(2,[command(4)])),('short_header',P('H',1)),('empty',b'')]:
  wire=env(b,False)
  try:parse(wire,False)
  except (ValueError,struct.error):pass
  else:raise AssertionError(name)
  malformed.append(dict(Name=name,Kind='Delta',Hex=wire.hex().upper()))
 return dict(Version=1,Synthetic=True,Cases=cases,Malformed=malformed)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
 if a.generate:
  root=Path(__file__).resolve().parents[3];assert a.generate.resolve().is_relative_to((root/'local-private').resolve())
  a.generate.parent.mkdir(parents=True,exist_ok=True);a.generate.write_bytes((json.dumps(generate(),indent=2)+'\n').encode());print('PASS independent17 TANO6 positive and6 malformed fixtures')
 else:
  peer=json.loads(a.peer.read_text());assert peer==generate();output=json.loads(a.verify.read_text())
  assert output['Records']==[dict(Hex=c['Hex'],Expected=c['Expected']) for c in peer['Cases']]
  assert output['NegativeChecks']>=100 and output['BudgetChecks']>=40
  print('PASS independent TANO6 exact wire/ordered DTO/cost-cap oracle')
