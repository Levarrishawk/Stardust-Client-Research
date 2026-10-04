"""Independent synthetic complete CREO6 host peer, using accepted Python models."""
import copy,json,runpy,struct,sys
from pathlib import Path
r=Path(__file__).resolve().parents[3]
base=runpy.run_path(str(r/'poc/round34-tests/Integration/generate_peer.py'))
oracle=runpy.run_path(str(r/'poc/round37-tests/Networking/peer.py'))
p,oid=base['p'],base['oid'];model=p.parse(bytes.fromhex(base['baseline']),'Baseline')[0]
S,V=oracle['S'],oracle['V'];E=oracle['w']['E'];measure=oracle['measure']
def envelope(fields,opcode,count):return struct.pack('<HIQIBIH',5,opcode,oid,0x4352454f,6,len(fields)+2,count)+fields
def snapshot(m):return dict(ModelHex=envelope(p.baseline_fields(m),0x68a75f0c,m['DeclaredCount']).hex().upper(),StateBytes=measure(m)['StateBytes'])
def vector(field,commands,extent=None,incoming=None):
 key=oracle['KEY'][field];extent=extent if extent is not None else sum(1+(len(v) if op==3 else 0) for op,ix,v in commands)
 incoming=incoming if incoming is not None else (model[key]['Counter']+extent)&0xffffffff
 return V(field,extent,incoming,commands)
def all_fields():
 us=[]
 for f in range(18):
  if f==1:us.append(vector(f,[(1,1,[0xffffffffffffffff])]))
  elif f==13:us.append(vector(f,[(1,1,[-1])]))
  elif f==14:us.append(vector(f,[(2,1,[-2147483648])]))
  elif f==15:us.append(vector(f,[(1,1,[E(0x8000000000000001,b'\x00\xff',True)])]))
  elif f in (3,4,16):us.append(S(f,t=p.text(bytes((0,255,f)),True)))
  elif f==7:us.append(S(f,0xffffffffffffffff,second=0x8000000000000000))
  else:us.append(S(f,0 if f==17 else (1<<(struct.calcsize('<'+p.WIDTH[f])*8))-1))
 return us
def growth():return [vector(1,[(1,7,[0x8000000000000000])]),S(3,t=p.text(b'long'*20,True)),vector(15,[(2,8,[E(7,b'raw\x00\xff',True)])]),vector(13,[(2,8,[-1])]),vector(14,[(1,2,[9])]),S(9,0)]
def shrink():return [S(16,t=p.text(b'prior',True)),vector(1,[(4,0,[])]),vector(15,[(0,8,[])]),vector(13,[(4,0,[])]),vector(14,[(0,1,[])]),S(3,t=p.text(b'',True))]
def assign():return [vector(1,[(3,0,[0,0xffffffffffffffff])],1),vector(13,[(3,0,[-1,2147483647])],1),vector(15,[(3,0,[E(7,b'a',False),E(0x8000000000000000,b'\xff',True)])],1),S(17,0)]
def equal_skip():return [vector(15,[(2,0,[E(7,b'ignored\x00\xff',True)])]),vector(1,[(2,1,[0xffffffffffffffff])]),vector(13,[(2,0,[-1])]),S(9,0),vector(1,[(0,0,[])],incoming=model['Defenders']['Counter'])]
seed=snapshot(model);steps=[]
for name,make in [('all18_wrap',all_fields),('cross_partition_growth',growth),('prior_events_then_shrink',shrink),('assignment_overshoot',assign),('equal_identity_and_skipped',equal_skip)]:
 updates=make();fields=b''.join(oracle['field'](u) for u in updates)
 # parse() pins the synthetic oracle recipient; use its packet for grammar/work only.
 _,cost=oracle['parse'](oracle['b']['envelope'](fields,'Delta',65535))
 result,events,peak=oracle['apply'](model,updates,cost);model=result['Model']
 steps.append(dict(Name=name,Hex=envelope(fields,0x12862153,65535).hex().upper(),Events=events,Peak=peak,**snapshot(model)))
fixture=dict(Version=1,Synthetic=True,ObjectId=oid,Baseline=seed['ModelHex'],Seed=seed,Steps=steps)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(fixture,indent=2)+'\n',encoding='utf-8')
 print(json.dumps({'Plans':len(steps),'Events':sum(len(s['Events']) for s in steps),'FieldsInFirstPlan':18,'MaxMessageBytes':max(len(bytes.fromhex(s['Hex'])) for s in steps),'Synthetic':True}))
