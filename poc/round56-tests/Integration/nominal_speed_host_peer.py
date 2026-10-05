"""Independent Fraction trace for explicit synthetic nominal-speed host inputs."""
import importlib.util,json,math,struct,sys,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('nominal_host_scalar',ROOT/'poc/round34-tests/Assets/verify_ahcn_operation.py');a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
spec=importlib.util.spec_from_file_location('nominal_host_sqrt',ROOT/'poc/round36-tests/Rendering/verify_circle.py');circle=importlib.util.module_from_spec(spec);spec.loader.exec_module(circle)
def word(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def value(bits):return struct.unpack('<f',struct.pack('<I',bits))[0]
def transition(current,target,seconds,p,rate):
 assert a.finite(current) and a.finite(target) and a.finite(seconds)
 c,t,s=map(a.scalar,(current,target,seconds));order=(t[0]>c[0])-(t[0]<c[0])
 if order==0:return dict(StoredBits=current,CandidateBits=current,Retained=c,Branch=0,Clamped=False,RatesUsed=0)
 assert rate is not None
 from fractions import Fraction
 r=(Fraction(rate['Magnitude'])*Fraction(2)**rate['Exponent'],bool(rate['Magnitude']==0 and rate['IsNegative']))
 if rate['IsNegative'] and rate['Magnitude']:r=(-r[0],False)
 if order<0:r=a.neg(r)
 step=a.precision(a.mul(r,s),p);retained=a.precision(a.add(step,c),p);candidate=a.bits(retained)
 assert a.finite(candidate)
 clamp=retained[0]>t[0] if order>0 else retained[0]<t[0]
 return dict(StoredBits=target if clamp else candidate,CandidateBits=candidate,Retained=retained,Branch=1 if order>0 else 2,Clamped=clamp,RatesUsed=1)
def generate():
 return dict(Version=1,Policy='supplied_nominal_rate_modern_planar_trace',InitialSpeedBits=0,Precision=64,StepMicroseconds=10000,SecondsBits=word(.01),Rate=dict(Magnitude=1,Exponent=5,IsNegative=False),OriginalEffectiveMovementParity=False,Steps=[dict(TargetBits=word(2 if i<10 else 6 if i<30 else 0)) for i in range(60)])
def plan(d):
 current=d['InitialSpeedBits'];out=[]
 for i,step in enumerate(d['Steps']):
  expected=transition(current,step['TargetBits'],d['SecondsBits'],d['Precision'],d['Rate']);out.append(expected);current=expected['StoredBits']
 return out
def check_result(got,expected):
 for k in ('StoredBits','CandidateBits','Branch','Clamped','RatesUsed'):assert got[k]==expected[k],(k,got,expected)
 raw=got['RetainedCandidate'];from fractions import Fraction
 actual=Fraction(raw['Magnitude'])*Fraction(2)**raw['Exponent']
 if raw['IsNegative']:actual=-actual
 assert actual==expected['Retained'][0] and (raw['Magnitude']!=0 or raw['IsNegative']==expected['Retained'][1])
def records(text,prefix):return [json.loads(s[len(prefix):]) for s in text.splitlines() if s.startswith(prefix)]
def verify_native(d,actual):
 expected=plan(d);assert len(actual['Steps'])==len(expected)
 for got,e in zip(actual['Steps'],expected):check_result(got,e)
 return dict(Passed=True,Steps=len(expected),SuppliedRate=True,OriginalEffectiveMovementParity=False)
def verify_host(d,text):
 expected=plan(d);rows=records(text,'nominal_speed_state=');assert len(rows)==len(expected)
 begin=records(text,'nominal_speed_ready=');assert len(begin)==1 and begin[0]['Steps']==60 and begin[0]['DriverConstructed'] and begin[0]['calls']==begin[0]['closed']==3
 reset=next(r for r in records(text,'local_state=') if r['Name']=='reset')
 x,y,z=begin[0]['Start'];assert all(math.isclose(b,v,abs_tol=2e-6) for b,v in zip([x,y,z],reset['Native']))
 zbits=word(z);seconds=a.scalar(d['SecondsBits']);timestamp=begin[0]['TimestampMicroseconds']
 for i,(got,e) in enumerate(zip(rows,expected)):
  assert got['Index']==i;check_result(got['Transition'],e)
  displacement=a.bits(a.precision(a.mul(a.scalar(e['StoredBits']),seconds),24))
  zbits=a.bits(a.precision(a.add(a.scalar(zbits),a.scalar(displacement)),24));timestamp+=d['StepMicroseconds']
  assert got['TimestampMicroseconds']==timestamp and got['StepsThisUpdate']==1 and got['DroppedMicroseconds']==0 and got['FaultType'] is None
  assert got['PublicationRetained'] and got['VisualRetained'] and got['calls']==got['closed']==3
  expected_speed=a.bits(circle.sqrt_round(a.scalar(a.bits(a.precision(a.mul(a.scalar(e['StoredBits']),a.scalar(e['StoredBits'])),24))),24))
  assert math.isclose(got['Speed'],value(expected_speed),abs_tol=2e-6)
  assert all(math.isclose(a1,b,abs_tol=2e-6) for a1,b in zip(got['Native'],[x,y,value(zbits)]))
  assert all(math.isclose(a1,b,abs_tol=2e-6) for a1,b in zip(got['Engine'],[x,y,-value(zbits)]))
 stop=records(text,'nominal_speed_stopped=');assert len(stop)==1 and stop[0]['Speed']==0 and not stop[0]['Active'] and stop[0]['Keys']==0
 assert records(text,'nominal_speed_cleanup=')==[dict(Closed=True,Steps=60,DriverDisposed=True)]
 assert 'PASS: pumped nominal_speed=true' in text
 return dict(Passed=True,Steps=60,PositionComparisons=60,RetainedTransitionComparisons=60,OriginalResourceRetained=True,NoUpdateAssetIo=True,OriginalEffectiveMovementParity=False)
def verify_negative(d,text):
 assert d['Steps'][-1]['TargetBits']==0x7fc00000
 assert records(text,'nominal_speed_preflight_failure=')==[dict(CompletedSteps=59,DriverConstructed=False,MotionPublication=False)]
 assert not records(text,'nominal_speed_ready=') and not records(text,'nominal_speed_state=')
 cleanup=records(text,'pump_failure_cleanup=');assert len(cleanup)==1 and cleanup[0]['calls']==cleanup[0]['closed']==3 and cleanup[0]['drained'] and cleanup[0]['actors']==0 and cleanup[0]['DriverFault'] is None
 return dict(Passed=True,CompletedPureSteps=59,NewDriverConstructed=False,NewMotionPublication=False,ArchivesDrained=True,Actors=0)
def fresh(p):return runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](Path(p),ROOT)
if __name__=='__main__':
 action,fixture=sys.argv[1:3];assert Path.cwd().resolve()==ROOT
 if action=='generate':
  d=generate();fresh(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print('Generated60 synthetic nominal speed steps')
 else:
  d=json.loads(Path(fixture).read_text(encoding='utf-8'))
  if action!='negative':assert d==generate()
  if action=='native':result=verify_native(d,json.loads(Path(sys.argv[3]).read_text(encoding='utf-8')))
  elif action=='host':result=verify_host(d,Path(sys.argv[3]).read_text(encoding='utf-8-sig'))
  elif action=='negative':result=verify_negative(d,Path(sys.argv[3]).read_text(encoding='utf-8-sig'))
  else:raise ValueError('Unknown action')
  fresh(sys.argv[4]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
