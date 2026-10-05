"""Independent exact target/transition oracle for an explicit modern motion trace."""
import importlib.util,json,struct,sys
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def module(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
old=module('previous_nominal_trace','poc/round56-tests/Integration/nominal_speed_host_peer.py')
a=old.a
def dyadic(d):
 x=Fraction(d['Magnitude'])*Fraction(2)**d['Exponent']
 return (-x if d['IsNegative'] else x,bool(x==0 and d['IsNegative']))
def target(step,p):
 run=step['RunSpeedBits'];walk=step['WalkSpeedBits'];words=step['DirectionWordsInRecoveredOrder']
 assert len(words)==3 and all(a.finite(b) for b in [run,walk,*words])
 choose=step['RunFlag']!=0 and a.scalar(run)[0]>0
 speed=run if choose else walk
 parts=[a.precision(a.mul(a.scalar(b),a.scalar(b)),p) for b in words]
 total=a.precision(a.add(parts[0],parts[1]),p)
 total=a.precision(a.add(total,parts[2]),p)
 magnitude=old.circle.sqrt_round(total,p)
 intermediate=a.precision(a.mul(magnitude,a.scalar(speed)),p)
 snapshot=a.bits(intermediate)
 result=a.precision(a.mul(intermediate,dyadic(step['Factor'])),p)
 stored=a.bits(result)
 assert a.finite(snapshot) and a.finite(stored)
 return dict(TargetBits=stored,IntermediateBits=snapshot,SelectedSpeedBits=speed,RunSelected=choose,
             Magnitude=magnitude,Intermediate=intermediate,RetainedTarget=result)
def generate():
 d=old.generate();d['Version']=2;d['Policy']='supplied_nominal_target_and_rate_modern_planar_trace'
 d['Steps']=[]
 for i in range(60):
  if i<10:run,walk,flag,vector,factor=2,1,1,[0,0,1],(1,0)
  elif i<20:run,walk,flag,vector,factor=1.2,1,128,[3,4,0],(1,0)
  elif i<30:run,walk,flag,vector,factor=99,3,0,[0,0,1],(1,1)
  else:run,walk,flag,vector,factor=0,0,1,[0,0,1],(1,0)
  d['Steps'].append(dict(RunSpeedBits=old.word(run),WalkSpeedBits=old.word(walk),RunFlag=flag,
    DirectionWordsInRecoveredOrder=[old.word(x) for x in vector],
    Factor=dict(Magnitude=factor[0],Exponent=factor[1],IsNegative=False)))
 return d
def expanded(d):
 out=dict(d);steps=[]
 for s in d['Steps']:steps.append(dict(TargetBits=target(s,d['Precision'])['TargetBits']))
 out['Steps']=steps;return out
def check_retained(got,expected):
 actual=dyadic(got)
 assert actual[0]==expected[0] and (actual[0]!=0 or actual[1]==expected[1]),(got,expected)
def check_target(got,expected):
 # Final API-specific property mapping is reconciled before acceptance.
 for k in ('TargetBits','IntermediateBits','SelectedSpeedBits','RunSelected'):assert got[k]==expected[k],(k,got,expected)
 for k in ('Magnitude','Intermediate','RetainedTarget'):check_retained(got[k],expected[k])
def verify_native(d,actual):
 expectations=[target(s,d['Precision']) for s in d['Steps']]
 assert len(actual['Targets'])==60
 for got,e in zip(actual['Targets'],expectations):check_target(got,e)
 result=old.verify_native(expanded(d),actual);result.update(TargetComparisons=60,TargetInputsSupplied=True)
 return result
def verify_host(d,text):
 result=old.verify_host(expanded(d),text)
 rows=old.records(text,'nominal_target_state=')
 assert len(rows)==60
 for i,(got,s) in enumerate(zip(rows,d['Steps'])):
  assert got['Index']==i;check_target(got['Target'],target(s,d['Precision']))
 result.update(TargetComparisons=60,TargetInputsSupplied=True)
 return result
def verify_negative(d,text):
 assert d['Steps'][-1]['DirectionWordsInRecoveredOrder'][2]==0x7fc00000
 assert old.records(text,'nominal_speed_preflight_failure=')==[dict(CompletedSteps=59,DriverConstructed=False,MotionPublication=False)]
 assert len(old.records(text,'nominal_target_state='))==0
 assert not old.records(text,'nominal_speed_ready=') and not old.records(text,'nominal_speed_state=')
 cleanup=old.records(text,'pump_failure_cleanup=')
 assert len(cleanup)==1 and cleanup[0]['calls']==cleanup[0]['closed']==3 and cleanup[0]['drained'] and cleanup[0]['actors']==0
 return dict(Passed=True,CompletedPureSteps=59,NoNewMotionPublication=True,Actors=0,ArchivesDrained=True)
if __name__=='__main__':
 action,fixture=sys.argv[1:3];assert Path.cwd().resolve()==ROOT
 if action=='generate':
  old.fresh(fixture).write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8')
 else:
  d=json.loads(Path(fixture).read_text());assert action=='negative' or d==generate()
  if action=='native':result=verify_native(d,json.loads(Path(sys.argv[3]).read_text()))
  elif action=='host':result=verify_host(d,Path(sys.argv[3]).read_text(encoding='utf-8-sig'))
  elif action=='negative':result=verify_negative(d,Path(sys.argv[3]).read_text(encoding='utf-8-sig'))
  else:raise ValueError(action)
  old.fresh(sys.argv[4]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))

