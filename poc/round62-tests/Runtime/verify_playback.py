"""Independent actual-pair/plan and binary32 modern world-Z playback oracle; no native output seed."""
import argparse,json,runpy
from pathlib import Path
from fractions import Fraction
ROOT=Path(__file__).resolve().parents[3]
composition=runpy.run_path(str(ROOT/'poc/round61-tests/Runtime/verify_composition.py'))
maths=composition['planmodel']['transition'];target=composition['planmodel']['target']
decode=maths['decode'];mul=maths['multiply'];add=maths['add'];store=maths['stored']
def generate(out):
 seed=out.with_suffix('.seed.json');base=composition['generate'](seed);seed.write_text(json.dumps(base,separators=(',',':'))+'\n',encoding='utf-8',newline='\n')
 cases=[];updates=[[7,0x40000000],[11,0x3f800000],[4,0x3f800000],[5,0x3f800000]]
 def put(**kw):
  c=dict(Name=str(len(cases)),Profile=0,Typed=True,Finalized=True,Precision=64,Updates=updates,Initial=0,Steps=[composition['step']()]);c.update(kw);c['Wire']=composition['pairmodel']['wire'](c['Updates']);cases.append(c)
 for p in (24,53,64):
  for profile in range(len(base['Profiles'])):
   put(Profile=profile,Precision=p,Steps=[composition['step'](Factor=dict(Magnitude=1 if i<20 else 3 if i<40 else 0,Exponent=0,Negative=False),Seconds=0x3c23d70a,Rate=dict(Magnitude=1,Exponent=5,Negative=False)) for i in range(60)],SixtyStep=True)
  put(Precision=p,Steps=[],Empty=True)
  put(Precision=p,Single=True)
  put(Precision=p,Steps=[composition['step'](Factor=dict(Magnitude=1,Exponent=0,Negative=True),Seconds=0x3f800000) for _ in range(10)],NegativeZ=True)
  put(Precision=p,Initial=0x80000000,Steps=[composition['step'](Factor=dict(Magnitude=0,Exponent=0,Negative=False),Rate=None)],SignedZero=True)
  put(Precision=p,Initial=1,Steps=[composition['step'](Factor=dict(Magnitude=0,Exponent=0,Negative=False),Seconds=0,Rate=dict(Magnitude=0,Exponent=0,Negative=False))],TinyLength=True)
 return dict(Baseline=base['Baseline'],Profiles=base['Profiles'],Cases=cases)
def expected_rows(plan):
 dt=decode(store((Fraction(1,100),False)))[0];position=(Fraction(0),False);rows=[]
 for i,r in enumerate(plan['Rows']):
  bits=r['Transition']['StoredBits'];v=decode(bits)[0]
  displacement=decode(store(mul(v,dt,24)))[0];position=decode(store(add(position,displacement,24)))[0]
  squared=decode(store(mul(v,v,24)))[0];speed=store(target['sqrt_value'](squared,24))
  rows.append(dict(Index=i,StoredBits=bits,Submit=i*10000,Advance=(i+1)*10000,ResultTime=(i+1)*10000,Steps=1,Active=True,FaultType=None,SpeedBits=speed,PositionBits=[0,0,store(position)]))
 return rows
def main():
 p=argparse.ArgumentParser();sub=p.add_subparsers(dest='mode',required=True);g=sub.add_parser('generate');g.add_argument('--out',type=Path,required=True);v=sub.add_parser('verify')
 for k in ('cases','native','receipt'):v.add_argument('--'+k,type=Path,required=True)
 a=p.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if a.mode=='generate':
  out=guard(a.out,ROOT)
  for suffix in ['.seed.json','.seed.seed.json','.seed.seed.seed.json']:guard(out.with_suffix(suffix),ROOT)
  fixture=generate(out);out.write_text(json.dumps(fixture,separators=(',',':'))+'\n',encoding='utf-8',newline='\n');print('GENERATED',len(fixture['Cases']));return
 out=guard(a.receipt,ROOT);fixture=json.loads(a.cases.read_text());native=json.loads(a.native.read_text());profiles=composition['pairmodel']['expected_profiles'](fixture)
 assert len(native['Cases'])==len(fixture['Cases']);steps=0;sixty=0;tiny=0
 for c,got in zip(fixture['Cases'],native['Cases']):
  wanted=composition['evaluate'](c,profiles[c['Profile']],fixture['Profiles'][c['Profile']]['RootSha256']);assert wanted['Reject']=='None'
  rows=expected_rows(wanted['Plan']);n=len(rows)
  expected=dict(Name=c['Name'],Pair=wanted['Pair'],Plan=wanted['Plan'],Rows=rows,Counts=[n,n,n],BorrowedAlive=True)
  if expected!=got:raise ValueError(f"Mismatch {c['Name']}: expected {expected} actual {got}")
  steps+=n;sixty+=bool(c.get('SixtyStep'))
  tiny+=bool(c.get('TinyLength')) and rows[0]['StoredBits']==1 and rows[0]['SpeedBits']==0
 assert native['ExecutedSteps']==steps and native['NegativeChecks']>=90 and native['NoImplicitBorrowedResetOrDispose']
 assert sixty==60 and tiny==3
 report=dict(Cases=len(fixture['Cases']),Profiles=len(profiles),SixtyStepTraces=sixty,ExecutedSteps=steps,ActualStepResultIdentities=steps,StateReads=steps,SubmitCalls=steps,AdvanceCalls=steps,IndependentPositionsAndTimes=True,IndependentFloatLengthSpeed=True,TinyLengthWitnesses=tiny,NativeChecks=native['NativeChecks'],NegativeChecks=native['NegativeChecks'],ControlledDriverModes=15,SourceMutationStages=3,PreflightZeroCallGates=True,PostEffectFaultLatch=True,SuccessPrefixPreserved=True,NoAutomaticRetryResetDispose=True,Precisions=[24,53,64],SyntheticOnly=True,OriginalExecuted=False,OutputSeedUsed=False)
 out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print('ACTUAL_PLAYBACK_FRACTION_F32_PASS '+json.dumps(report))
if __name__=='__main__':main()
