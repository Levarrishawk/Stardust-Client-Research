"""Independent local-current supplied reader, target/plan and original-resource host checks."""
import importlib.util,json,sys,runpy,copy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
old=load('current_target_reference','poc/round57-tests/Integration/nominal_target_host_peer.py')
partial=load('current_wire_reference','poc/round46-tests/Integration/creature4_partial_host_peer.py')
a=old.a
PLAN='supplied_local_current_reader_and_nominal_plan_modern_planar_trace'
def finite_wire():
 d=partial.generate();last=d['Steps'][-1];raw=bytearray.fromhex(last['Hex']);assert raw[-4:]==struct.pack('<I',0xffc00001);raw[-4:]=struct.pack('<I',0x3f800007);last['Hex']=raw.hex().upper()
 for f in last['Fields']:
  if f['Index']==7:f['Bits']=0x3f800007
 for e in last['Events']:
  if e['Index']==7:e['NewBits']=0x3f800007
 return d

def reader(base,first,second,p):
 assert all(a.finite(b) for b in (base,first,second))
 modifiers=a.precision(a.mul(a.scalar(second),a.scalar(first)),p);speed=a.precision(a.mul(modifiers,a.scalar(base)),p);bits=a.bits(speed);assert a.finite(bits)
 def inspect(b):
  e=(b>>23)&255;m=(b&0x7fffff)|(0 if e==0 else 0x800000);return m.bit_length(),(-149 if e==0 else e-150)
 pairs=list(map(inspect,(base,first,second)));work=64+8*(sum(w+abs(e) for w,e in pairs)+p+8)
 return dict(BaseBits=base,FirstModifierSnapshotBits=first,SecondModifierBits=second,ReturnBits=bits,RetainedModifierProduct=modifiers,RetainedSpeed=speed,Work=work)

def sources(d):
 assert d['Version']==3 and d['Policy']==PLAN and d['CurrentInputPolicy']=='SuppliedLocalOnly' and d['Synthetic'] and not d['OriginalGetterBranchParity']
 fs={f['Index']:f['Bits'] for f in finite_wire()['Steps'][-1]['Fields']};words=[fs[i] for i in (7,11,4,5)];assert d['CurrentSourceWords']==words
 return reader(words[0],words[2],words[3],d['Precision']),reader(words[1],words[2],words[3],d['Precision'])

def expanded(d):
 r,w=sources(d);out=copy.deepcopy(d)
 for s in out['Steps']:s.update(RunSpeedBits=r['ReturnBits'],WalkSpeedBits=w['ReturnBits'])
 return out

def check_reader(g,e):
 for k in ('BaseBits','FirstModifierSnapshotBits','SecondModifierBits','ReturnBits'):assert g[k]==e[k],(k,g,e)
 for k in ('RetainedModifierProduct','RetainedSpeed'):old.check_retained(g[k],e[k])
 assert g['ReservedWork']==e['Work'] and g['ReservedCopyBytes']==128 and g['SuppliedWords']==3 and g['Multiplications']==2 and g['CallerF32Stores']==1 and g['Policy']==1

def counters(g,d):
 q=expanded(d);r=d['Rate'];rate=dict(Magnitude=r['Magnitude'],Exponent=r['Exponent'],Negative=r['IsNegative'])
 steps=[dict(Run=s['RunSpeedBits'],Walk=s['WalkSpeedBits'],Flag=s['RunFlag'],Direction=s['DirectionWordsInRecoveredOrder'],Factor=dict(Magnitude=s['Factor']['Magnitude'],Exponent=s['Factor']['Exponent'],Negative=s['Factor']['IsNegative']),Seconds=d['SecondsBits'],Rate=rate) for s in q['Steps']]
 e=runpy.run_path(str(ROOT/'poc/round58-tests/Runtime/verify_plan.py'))['evaluate'](dict(Name='current-host',Initial=d['InitialSpeedBits'],Precision=d['Precision'],Steps=steps));assert e['Reject']=='None'
 for key,value in zip(('StepCount','TargetEvaluations','TransitionEvaluations','RatesUsed','Equalities','Clamps','TargetF32Stores','TransitionF32Stores'),e['Counts']):assert g[key]==value,(key,g,e)
 for key,value in [('ReservedCopyBytes',e['Copy']),('ReservedWork',e['Reserved']),('ActualTargetWork',e['ActualTarget']),('ActualTransitionWork',e['ActualTransition'])]:assert g[key]==value,(key,g,e)
 assert g['ActualWork']==g['ActualTargetWork']+g['ActualTransitionWork']<=g['ReservedWork'] and g['ActualReferences'] and not g['OriginalEffectiveMovementParity']

def verify_native(d,g):
 r,w=sources(d);check_reader(g['RunReader'],r);check_reader(g['WalkReader'],w);counters(g['Plan'],d)
 result=old.verify_native(expanded(d),g);result.update(CurrentReaderCalls=2,ReaderMultiplications=4,ActualReaderWordsUsed=True,OriginalGetterBranchParity=False);return result

def verify_host(d,wire,t):
 assert wire==finite_wire();r,w=sources(d);result=old.verify_host(expanded(d),t)
 rows=old.old.records(t,'current_nominal_source_ready=');assert len(rows)==1;g=rows[0];check_reader(g['Run'],r);check_reader(g['Walk'],w)
 fs={f['Index']:f['Bits'] for f in wire['Steps'][-1]['Fields']}
 assert [f['Index'] for f in g['Fields']]==[7,11,4,5]
 for f in g['Fields']:assert f['RawBits']==fs[f['Index']] and f['Origin']=='Received' and f['Eligibility']=='FiniteCurrent'
 assert g['Current'] and g['ActualFieldReferences'] and g['ReaderCalls']==2 and g['Multiplications']==4 and g['CallerF32Stores']==2 and g['SourcePublicationRetained'] and g['calls']==g['closed']==3 and g['RootSha256']=='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
 rows=old.old.records(t,'nominal_speed_plan_ready=');assert len(rows)==1;counters(rows[0],d)
 states=old.old.records(t,'current_nominal_source_state=');assert len(states)==60
 for i,s in enumerate(states):assert s==dict(Index=i,Current=True,RunBits=r['ReturnBits'],WalkBits=w['ReturnBits'],OriginalGetterBranchParity=False)
 assert old.old.records(t,'current_nominal_source_cleanup=')==[dict(Closed=True)]
 # Actual partial codec/store/journal values are checked by the frozen host and independently reconciled here.
 raw=old.old.records(t,'creature4_partial_state=');assert len(raw)==6
 for got,want in zip(raw,[wire['Seed']]+wire['Steps']):
  for key in ('Fields','Events','KnownMask','KnownCount','StateRevision','StoreRevision'):assert got[key]==want[key]
 result.update(CurrentFields=4,CurrentReaderCalls=2,ReaderMultiplications=4,SourceCurrentFrames=60,WireFiniteVariant=True,OriginalAssetsAltered=False,OriginalGetterBranchParity=False);return result

def verify_negative(d,t):
 assert old.old.records(t,'current_nominal_source_rejected=')==[dict(PublishedReaderResults=0,DriverConstructed=False,MotionPublication=False)]
 assert old.old.records(t,'nominal_speed_preflight_failure=')==[dict(CompletedSteps=0,DriverConstructed=False,MotionPublication=False)]
 for p in ('current_nominal_source_ready=','current_nominal_source_state=','nominal_speed_plan_ready=','nominal_target_state=','nominal_speed_state='):assert not old.old.records(t,p)
 c=old.old.records(t,'pump_failure_cleanup=');assert len(c)==1 and c[0]['calls']==c[0]['closed']==3 and c[0]['drained'] and c[0]['actors']==0
 assert old.old.records(t,'current_nominal_source_cleanup=')==[dict(Closed=True)]
 return dict(Passed=True,PublishedReaderResults=0,PublishedPlanSteps=0,NoNewMotion=True,Actors=0,ArchivesDrained=True,OriginalGetterBranchParity=False)
if __name__=='__main__':
 mode=sys.argv[1];d=json.loads(Path(sys.argv[2]).read_text());assert Path.cwd().resolve()==ROOT
 if mode=='native':result=verify_native(d,json.loads(Path(sys.argv[3]).read_text()));out=sys.argv[4]
 elif mode=='host':result=verify_host(d,json.loads(Path(sys.argv[3]).read_text()),Path(sys.argv[4]).read_text(encoding='utf-8-sig'));out=sys.argv[5]
 elif mode=='negative':result=verify_negative(d,Path(sys.argv[3]).read_text(encoding='utf-8-sig'));out=sys.argv[4]
 else:raise ValueError(mode)
 old.old.fresh(out).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
