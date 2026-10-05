"""Independent Fraction supplied local-reader oracle; all fixtures synthetic."""
import argparse,json,random,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
f=runpy.run_path(str(ROOT/'poc/round56-tests/Runtime/verify_transition.py'))
decode=f['decode'];mul=f['multiply'];store=f['stored'];text=f['text']
def state(c,reload=False,regroup=False):
 p=c['Precision'];a=decode(c['Base'])[0];b=decode(c['First'])[0];d=decode(c['Second'])[0]
 modifiers=mul(d,b,p)
 speed=mul(d,mul(b,a,p),p) if regroup else mul(decode(store(modifiers))[0] if reload else modifiers,a,p)
 return modifiers,speed,store(speed)
def work(c):
 ds=[decode(c[k]) for k in ('Base','First','Second')]
 return 64+8*(sum(m.bit_length()+abs(e) for _,m,e in ds)+c['Precision']+8)
def evaluate(c):
 name=c['Name']
 if c.get('Cancel'):return dict(Name=name,Reject='Canceled')
 try:
  p=c['Precision'];cw=c.get('MaximumCopyBytes',128);ww=c.get('MaximumWork',8192)
  if p not in (24,53,64) or c.get('Policy',1)!=1 or not 0<=cw<=128 or not 0<=ww<=8192:raise ValueError()
  w=work(c)
  if cw<128 or ww<w:raise ValueError()
  modifiers,speed,bits=state(c)
  return dict(Name=name,Reject='None',Base=c['Base'],First=c['First'],Second=c['Second'],Precision=p,Modifiers=text(modifiers),Speed=text(speed),ReturnBits=bits,Work=w,Copy=128,Counts=[3,2,1],Policy='SuppliedLocalOnly',Provenance='SuppliedLocalFiniteWords')
 except (ValueError,OverflowError):return dict(Name=name,Reject='Input')
def generate():
 cases=[]
 def put(**kw):
  c=dict(Name=str(len(cases)),Base=0x40000000,First=0x3f800000,Second=0x3f800000,Precision=64);c.update(kw);cases.append(c);return c
 values=[0,0x80000000,1,0x80000001,0x3f800000,0xbf800000,0x7f7fffff,0xff7fffff]
 rng=random.Random(59)
 def finiteword():
  b=rng.getrandbits(32)
  return b^0x00800000 if (b>>23)&255==255 else b
 for p in (24,53,64):
  for a in values:
   for b in values:
    for d in values:put(Precision=p,Base=a,First=b,Second=d)
  put(Precision=p,Base=0x7f7fffff,First=1,Second=0x3f000000,UnderflowWitness=True)
  put(Precision=p,Base=0,First=0x7f7fffff,Second=0x7f7fffff,OverflowRescue=True)
  for _ in range(200):put(Precision=p,Base=finiteword(),First=finiteword(),Second=finiteword())
  for _ in range(200):put(Precision=p,Base=0x3f000000+rng.randrange(0x1000000),First=0x3f000000+rng.randrange(0x1000000),Second=0x3f000000+rng.randrange(0x1000000))
  put(Precision=p,Base=0x7f7fffff,First=0x40000000,Second=0x3f800000,LateOverflow=True)
 base=put();w=work(base)
 for k,v in [('MaximumCopyBytes',128),('MaximumWork',w)]:
  put(**{k:v},Boundary='exact');put(**{k:v-1},Boundary='below')
 for kw in [dict(Base=0x7f800000),dict(First=0xff800000),dict(Second=0x7fc12345),dict(Policy=0),dict(Policy=2),dict(Precision=23),dict(MaximumCopyBytes=-1),dict(MaximumCopyBytes=129),dict(MaximumWork=-1),dict(MaximumWork=8193),dict(MaximumWork=0),dict(Cancel=True)]:put(**kw)
 # Last supplied operand must be finite even when preceding zero would erase it.
 put(Base=0,First=0,Second=0x7fc12345,LastOperandNaN=True)
 return cases
def main():
 parser=argparse.ArgumentParser();sub=parser.add_subparsers(dest='mode',required=True)
 g=sub.add_parser('generate');g.add_argument('--out',type=Path,required=True)
 v=sub.add_parser('verify')
 for k in ('cases','native','receipt'):v.add_argument('--'+k,type=Path,required=True)
 a=parser.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if a.mode=='generate':
  out=guard(a.out,ROOT);cs=generate();out.write_text(json.dumps(cs,indent=2)+'\n',encoding='utf-8',newline='\n');print('GENERATED',len(cs));return
 out=guard(a.receipt,ROOT);cs=json.loads(a.cases.read_text());native=json.loads(a.native.read_text());expected=[evaluate(c) for c in cs]
 assert len(expected)==len(native['Cases'])
 reloadw=groupw=underflow=overflowrescue=negativezero=0
 for c,w,g in zip(cs,expected,native['Cases']):
  if w!=g:raise ValueError(f"Mismatch {c['Name']}: expected{w} actual{g}")
  if w['Reject']=='None':
   try:reloadw+=state(c,reload=True)[2]!=w['ReturnBits']
   except ValueError:reloadw+=1
   groupw+=text(state(c,regroup=True)[1])!=w['Speed']
   underflow+=bool(c.get('UnderflowWitness')) and w['ReturnBits']!=0
   overflowrescue+=bool(c.get('OverflowRescue'))
   negativezero+=w['ReturnBits']==0x80000000
 assert reloadw>=3 and groupw>0 and underflow==3 and overflowrescue==3 and negativezero>0
 boundaries=[(c,w) for c,w in zip(cs,expected) if 'Boundary' in c]
 assert len(boundaries)==4 and all(w['Reject']==('None' if c['Boundary']=='exact' else 'Input') for c,w in boundaries)
 assert all(w['Reject']=='Input' for c,w in zip(cs,expected) if c.get('LateOverflow') or c.get('LastOperandNaN'))
 positive=sum(w['Reject']=='None' for w in expected)
 assert native['OwnershipChecks']==positive and native['Repeated']==100 and native['Threaded']==64 and native['PriorResultPreserved'] and native['NoMutableResultProperties']
 assert native['IndependentReaderMultiplications']==4 and native['IndependentReaderStores']==2
 report=dict(Cases=len(cs),Positive=positive,Rejected=sum(w['Reject']=='Input' for w in expected),Canceled=sum(w['Reject']=='Canceled' for w in expected),RetainedReloadWitnesses=reloadw,GroupingWitnesses=groupw,UnderflowRescues=underflow,IntermediateOverflowRescues=overflowrescue,NegativeZeroReturns=negativezero,ExactCapCases=2,OneBelowCapCases=2,LateOverflowCases=3,LastOperandNaN=1,OwnershipChecks=positive,Repeated=100,Threaded=64,PriorResultPreserved=True,NoMutableResultProperties=True,IndependentReaderMultiplications=4,IndependentReaderStores=2,Precisions=[24,53,64],SyntheticOnly=True,OriginalExecuted=False)
 out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print('FRACTION_PASS '+json.dumps(report))
if __name__=='__main__':main()
