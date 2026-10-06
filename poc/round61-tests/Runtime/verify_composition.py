"""Independent actual-current provenance, Fraction/isqrt target and transition composition oracle."""
import argparse,json,random,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
pairmodel=runpy.run_path(str(ROOT/'poc/round60-tests/Runtime/verify_pair.py'))
planmodel=runpy.run_path(str(ROOT/'poc/round58-tests/Runtime/verify_plan.py'))
def step(**kw):
 s=dict(Flag=1,Direction=[0x3f800000,0,0],Factor=dict(Magnitude=1,Exponent=0,Negative=False),Seconds=0x3f000000,Rate=dict(Magnitude=1,Exponent=0,Negative=False));s.update(kw);return s
def generate(out):
 seed=out.with_suffix('.seed.json');source=pairmodel['generate'](seed);seed.write_text(json.dumps(source,separators=(',',':'))+'\n',encoding='utf-8',newline='\n')
 cases=[];fixed=[[7,0x40000000],[11,0x3f800000],[4,0x3f800000],[5,0x3f800000]]
 def put(**kw):
  c=dict(Name=str(len(cases)),Profile=0,Typed=True,Finalized=True,Precision=64,Updates=fixed,Initial=0,Steps=[step()]);c.update(kw)
  if c['Updates'] is not None:c['Wire']=pairmodel['wire'](c['Updates'])
  cases.append(c);return c
 for p in (24,53,64):
  for profile in range(len(source['Profiles'])):
   for updates in (None,fixed):put(Profile=profile,Updates=updates,Precision=p)
   put(Profile=profile,Updates=None,Precision=p,Typed=False);put(Profile=profile,Updates=None,Precision=p,Finalized=False)
  put(Precision=p,Steps=[],Initial=0x80000000,MaximumSteps=0,MaximumOutputBytes=2336,MaximumCopyBytes=0,MaximumWork=128,EmptyExact=True)
  put(Precision=p,Steps=[step(Factor=dict(Magnitude=1 if i<20 else 3 if i<40 else 0,Exponent=0,Negative=False),Seconds=0x3c23d70a,Rate=dict(Magnitude=1,Exponent=5,Negative=False)) for i in range(60)],SixtyStep=True)
  put(Precision=p,Initial=0x40000000,Steps=[step(Rate=None)],EqualityNoRate=True)
  put(Precision=p,Steps=[step(),step(Rate=None)],LateFailure=True)
  put(Precision=p,Steps=[step(),step(Factor=dict(Magnitude=1,Exponent=512,Negative=False))],LateFailure=True)
  put(Precision=p,Steps=[step(),step(Rate=dict(Magnitude=1,Exponent=512,Negative=False))],LateFailure=True)
  put(Precision=p,Steps=[step(),step(Direction=[0x7f7fffff]*3)],LateFailure=True)
  put(Precision=p,Steps=[step(),step(Direction=[0,0x7fc12345,0])],LateFailure=True)
  put(Precision=p,Updates=[[7,1],[11,1],[4,0x3f800000],[5,0x3f800000]],Steps=[step(Direction=[1,0,0],Factor=dict(Magnitude=1,Exponent=298,Negative=False),Seconds=0x3f800000),step(Direction=[1,0,0],Factor=dict(Magnitude=1,Exponent=298,Negative=False),Rate=None)],RetainedStoreWitness=True)
  put(Precision=p,Updates=[[7,0x80000000],[11,0],[4,0xbf800000],[5,0x3f800000]],Steps=[step(Rate=None)],Initial=0,SignedZero=True)
 rng=random.Random(61)
 for p in (24,53,64):
  for _ in range(100):
   ss=[step(Flag=rng.choice([0,1,255]),Direction=rng.choice([[0,0,0],[0x3f800001,0x3eaaaaab,0x3dcccccd],[0x3f800000,0x40000000,0x40400000]]),Factor=dict(Magnitude=rng.choice([0,1,(1<<64)-1]),Exponent=rng.choice([-64,0,1]),Negative=bool(rng.getrandbits(1))),Seconds=rng.choice([0,0x80000000,0x3f800000,0xbf800000,0x3c23d70a]),Rate=dict(Magnitude=rng.choice([0,1,(1<<53)+1]),Exponent=rng.choice([-53,0,5]),Negative=bool(rng.getrandbits(1)))) for i in range(rng.randint(2,10))]
   put(Precision=p,Steps=ss)
 base=put(Steps=[step(),step(),step()]);pp=pairmodel['evaluate'](base,{},source['Profiles'][0]['RootSha256']);child=child_input(base,pp);cw=planmodel['evaluate'](child)['Reserved'];n=3
 for k,v in [('MaximumSteps',n),('MaximumOutputBytes',2336+864*n),('MaximumCopyBytes',864*n),('MaximumWork',128+64*n+cw)]:
  put(Steps=base['Steps'],**{k:v},Boundary='exact');put(Steps=base['Steps'],**{k:v-1},Boundary='below')
 put(Steps=[step() for _ in range(128)],HardCount='exact');put(Steps=[step() for _ in range(128)],MaximumSteps=127,HardCount='below');put(Steps=[step() for _ in range(129)])
 for k,h in [('MaximumSteps',128),('MaximumOutputBytes',112928),('MaximumCopyBytes',110592),('MaximumWork',9445504)]:
  put(**{k:-1});put(**{k:h+1})
 for kw in [dict(Initial=0x7f800000),dict(Steps=[step(),step(Seconds=0x7f800000)]),dict(Steps=[step(),step(Rate=dict(Magnitude=1,Exponent=513,Negative=False))]),dict(Steps=[step(),step(Factor=dict(Magnitude=1,Exponent=-513,Negative=False))]),dict(MaximumOutputBytes=2335),dict(MaximumWork=127),dict(Cancel=True)]:put(**kw)
 return dict(Baseline=source['Baseline'],Profiles=source['Profiles'],Cases=cases)
def child_input(c,pair):
 return dict(Name=c['Name'],Initial=c['Initial'],Precision=c['Precision'],Steps=[dict(s,Run=pair['Run']['ReturnBits'],Walk=pair['Walk']['ReturnBits']) for s in c['Steps']])
def evaluate(c,profile,pin):
 pair=pairmodel['evaluate']({k:c[k] for k in ('Name','Typed','Finalized','Precision','Updates')},profile,pin)
 if pair['Reject']=='Null':return dict(Name=c['Name'],Reject='NoPair')
 if pair['Reject']!='None':return dict(Name=c['Name'],Reject='PairInput')
 bad=dict(Name=c['Name'],Reject='Input');caps=[c.get(k,d) for k,d in [('MaximumSteps',128),('MaximumOutputBytes',112928),('MaximumCopyBytes',110592),('MaximumWork',9445504)]]
 if any(not 0<=v<=h for v,h in zip(caps,[128,112928,110592,9445504])) or caps[1]<2336 or caps[3]<128:return bad
 if c.get('Cancel'):return dict(Name=c['Name'],Reject='Canceled')
 n=len(c['Steps']);copy=864*n;output=2336+copy
 if n>caps[0] or copy>caps[2] or output>caps[1]:return bad
 ci=child_input(c,pair);child=planmodel['evaluate'](ci)
 if child['Reject']!='None':return bad
 own=128+64*n;work=own+child['Reserved']
 if work>caps[3]:return bad
 return dict(Name=c['Name'],Reject='None',Pair=pair,Plan=child,Output=output,Copy=copy,Reserved=work,OwnWork=own,OwnCopy=224*n,ChildCopy=640*n,ChildReserved=child['Reserved'],ActualChild=child['ActualTarget']+child['ActualTransition'],Reloads=0)
def main():
 parser=argparse.ArgumentParser();sub=parser.add_subparsers(dest='mode',required=True);g=sub.add_parser('generate');g.add_argument('--out',type=Path,required=True);v=sub.add_parser('verify')
 for k in ('cases','native','receipt'):v.add_argument('--'+k,type=Path,required=True)
 a=parser.parse_args();guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if a.mode=='generate':
  out=guard(a.out,ROOT);guard(out.with_suffix('.seed.json'),ROOT);guard(out.with_suffix('.seed.seed.json'),ROOT);fixture=generate(out);out.write_text(json.dumps(fixture,separators=(',',':'))+'\n',encoding='utf-8',newline='\n');print('GENERATED',len(fixture['Cases']));return
 out=guard(a.receipt,ROOT);fixture=json.loads(a.cases.read_text());native=json.loads(a.native.read_text());profiles=pairmodel['expected_profiles'](fixture)
 expected=[evaluate(c,profiles[c['Profile']],fixture['Profiles'][c['Profile']]['RootSha256']) for c in fixture['Cases']]
 assert len(expected)==len(native['Cases'])
 for c,w,g in zip(fixture['Cases'],expected,native['Cases']):
  if w!=g:raise ValueError(f"Mismatch {c['Name']}: expected{w} actual{g}")
 positive=sum(w['Reject']=='None' for w in expected);steps=sum(len(w.get('Plan',{}).get('Rows',[])) for w in expected)
 assert native['PositiveIdentityChecks']==positive and native['OwnedStepChecks']==steps and native['NativeChecks']>100
 assert native['LifecyclePassed'] and native['PriorResultPreserved'] and native['BorrowedAdapterSurvivesDisposal'] and native['Repeated']==100
 boundaries=[(c,w) for c,w in zip(fixture['Cases'],expected) if 'Boundary' in c];assert len(boundaries)==8 and all(w['Reject']==('None' if c['Boundary']=='exact' else 'Input') for c,w in boundaries)
 assert all(w['Reject']=='Input' for c,w in zip(fixture['Cases'],expected) if c.get('LateFailure'))
 assert all(w['Reject']=='None' for c,w in zip(fixture['Cases'],expected) if c.get('EmptyExact') or c.get('SixtyStep') or c.get('RetainedStoreWitness'))
 report=dict(Cases=len(expected),Positive=positive,Rejected=sum(w['Reject']=='Input' for w in expected),PairRejected=sum(w['Reject']=='PairInput' for w in expected),NoPair=sum(w['Reject']=='NoPair' for w in expected),Canceled=sum(w['Reject']=='Canceled' for w in expected),Profiles=len(profiles),AcceptedSteps=steps,IdentityChecks=positive,OwnedStepChecks=steps,ExactCapCases=4,OneBelowCapCases=4,Hard128Count=True,EmptyPlans=3,SixtyStepPlans=3,RetainedStoreWitnesses=3,LateFailures=15,NativeChecks=native['NativeChecks'],Repeated=100,LifecyclePassed=True,PriorResultPreserved=True,BorrowedAdapterSurvivesDisposal=True,ReentryGuardInjectionOnly=True,Precisions=[24,53,64],SyntheticOnly=True,OriginalExecuted=False,OutputSeedUsed=False)
 out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print('ACTUAL_PAIR_FRACTION_ISQRT_PASS '+json.dumps(report))
if __name__=='__main__':main()
