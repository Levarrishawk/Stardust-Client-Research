from pathlib import Path
import importlib.util,json,struct,copy,sys
r=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('creo_peer',r/'poc/round32-tests/Networking/peer.py');p=importlib.util.module_from_spec(s);s.loader.exec_module(p)
# Same synthetic ID as round16-tests/Integration/Generate-Peer.py; no asset identity.
oid=0xDABC001122334455;p.OID=oid
model=p.baseline();model['ObjectId']=oid
model.update(Passive=0x80000000,Level=65535,WeaponId=0xffffffffffffffff,GroupId=0x8000000000000000,InviterId=0xf123456789abcdef,InviteCounter=0xffffffffffffffff,Guild=0xffffffff,TargetId=0,MoodId=255,PerformanceStart=0xffffffff,PerformanceType=-2147483648,Frozen=255)
model['Defenders']=dict(Counter=0xfffffffe,Values=[0,0xffffffffffffffff]);model['Ham']=dict(Counter=0xffffffff,Values=[-2147483648,0,2147483647]);model['MaximumHam']=dict(Counter=0x80000000,Values=[2147483647,-1,0]);model['Wearables']=dict(Counter=0xf1234567,Values=[dict(Customization=p.text(b'\x00\xff\x80',True),Arrangement=0xffffffff,ObjectId=0xffffffffffffffff,TemplateCrc=0x80000000)])
model['PerformanceAnimation']=p.text(b'\xff\x00\x80',True);model['MoodString']=p.text(b'raw mood');model['AlternateAppearance']=p.text(b'\x00\xff')
def wire(m):return p.envelope(p.baseline_fields(m),'Baseline',m['DeclaredCount']).hex().upper()
def state(m):return dict(ModelHex=wire(m),StateBytes=len(p.baseline_fields(m))+10)
seed=state(model);baseline=wire(model)
steps=[]
updates=[('raw-target',[p.update(9,0xffffffffffffffff)]),('ordered-pair-and-target',[p.update(7,0,second=0x8000000000000000),p.update(9,0),p.update(9,0xf122334455667789)]),('raw-strings',[p.update(3,t=p.text(b'\x00\x80\xff',True)),p.update(4,t=p.text(b'',True)),p.update(16,t=p.text(b'\xff\x00'))]),('signed-and-flags',[p.update(12,0xffffffff),p.update(17,0),p.update(2,0),p.update(0,0xffffffff)]),('equal-target',[p.update(9,0xf122334455667789)])]
keys={0:'Passive',2:'Level',3:'PerformanceAnimation',4:'MoodString',5:'WeaponId',6:'GroupId',8:'Guild',9:'TargetId',10:'MoodId',11:'PerformanceStart',12:'PerformanceType',16:'AlternateAppearance',17:'Frozen'}
for name,us in updates:
 delta=dict(ObjectId=oid,DeclaredCount=65535,Updates=us)
 hexwire=p.envelope(p.delta_fields(delta),'Delta',65535).hex().upper()
 for u in us:
  i=u['Index']
  if i==7:model['InviterId']=u['Bits'];model['InviteCounter']=u['SecondId']
  elif i in (3,4,16):model[keys[i]]=copy.deepcopy(u['Text'])
  elif i==12:model[keys[i]]=u['Bits']-(1<<32) if u['Bits']>=1<<31 else u['Bits']
  else:model[keys[i]]=u['Bits']
 steps.append(dict(Name=name,Hex=hexwire,Events=len(us),Indices=[u['Index'] for u in us],**state(model)))
fixture=dict(Version=1,Synthetic=True,ObjectId=oid,Baseline=baseline,Seed=seed,Steps=steps)

if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(fixture,indent=2)+'\n',encoding='utf-8')
 print('Prepared five ordered CREO6 supported updates; full raw model/vector retention expectations')
