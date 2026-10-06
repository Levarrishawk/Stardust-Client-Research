"""Independent stored-speed displacement plus sealed-triangle grounding host checks."""
import copy,importlib.util,json,math,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def module(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
prior=module('terrain63_host62','poc/round62-tests/Integration/composition_peer.py')
ray=module('terrain63_ray','poc/round63-tests/Integration/ray_peer.py')
current=prior.old.old.current
nominal=current.old.old
surface=ray.surface
records=prior.records

def verify(d,terrain,wire,text):
 assert d['Version']==4 and d['Policy']=='supplied_local_current_reader_and_nominal_plan_modern_terrain_trace'
 assert d['TerrainBindingPolicy']=='OwnedTerrainDriverAndCurrentPlanPlayback'
 pure=copy.deepcopy(d);pure['Version']=3;pure['Policy']=current.PLAN
 assert wire==current.finite_wire()
 r,w=current.sources(pure);expanded=current.expanded(pure)
 targets=[current.old.target(step,pure['Precision']) for step in expanded['Steps']]
 plan=nominal.plan(current.old.expanded(expanded))
 result=prior.terrain_host(terrain,text)
 ready=records(text,'current_terrain_playback_ready=');assert len(ready)==1
 for key,value in dict(ActualSource=True,ActualTerrain=True,OwnedDriver=True,OwnedPlayback=True,Steps=60,OriginalEffectiveMovementParity=False).items():assert ready[0][key]==value,(key,ready)
 inputs=records(text,'current_nominal_source_ready=');assert len(inputs)==1
 current.check_reader(inputs[0]['Run'],r);current.check_reader(inputs[0]['Walk'],w)
 fields={f['Index']:f['Bits'] for f in wire['Steps'][-1]['Fields']}
 assert [f['Index'] for f in inputs[0]['Fields']]==[7,11,4,5]
 for f in inputs[0]['Fields']:assert f['RawBits']==fields[f['Index']] and f['Origin']=='Received' and f['Eligibility']=='FiniteCurrent'
 assert inputs[0]['ActualFieldReferences'] and inputs[0]['SourcePublicationRetained'] and inputs[0]['Current'] and inputs[0]['calls']==inputs[0]['closed']==3
 assert inputs[0]['RootSha256']=='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
 assert records(text,'paired_current_speed_ready=')==[dict(OutputBytes=2080,ReservedCopyBytes=256,ReservedWork=16384,ActualWork=r['Work']+w['Work'],ReaderCalls=2,SuppliedWords=6,Multiplications=4,CallerF32Stores=2,ActualReferences=True,Current=True,OriginalGetterBranchParity=False)]
 header=records(text,'nominal_speed_plan_ready=');assert len(header)==1;current.counters(header[0],pure)
 n=len(pure['Steps']);composition=dict(OutputBytes=2336+864*n,ReservedCopyBytes=864*n,OwnWork=128+64*n,OwnCopyBytes=224*n,ChildCopyBytes=640*n,ChildReservedWork=header[0]['ReservedWork'],ActualChildWork=header[0]['ActualWork'],ReaderReloads=0,TargetEvaluations=n,TransitionEvaluations=n,ActualReferences=True,Current=True,OriginalEffectiveMovementParity=False)
 composition['ReservedWork']=composition['OwnWork']+composition['ChildReservedWork']
 assert records(text,'current_plan_composition_ready=')==[composition]
 assert records(text,'current_plan_playback_ready=')==[dict(Steps=60,StepMicroseconds=10000,OutputBytes=512,BorrowedDriver=True,BorrowedComposition=True,ActualPlan=True,OriginalEffectiveMovementParity=False)]
 raw=records(text,'creature4_partial_state=');assert len(raw)==6
 for got,want in zip(raw,[wire['Seed']]+wire['Steps']):
  for key in ('Fields','Events','KnownMask','KnownCount','StateRevision','StoreRevision'):assert got[key]==want[key]
 start=records(text,'nominal_speed_ready=');assert len(start)==1 and start[0]['Steps']==60 and start[0]['DriverConstructed'] and start[0]['calls']==start[0]['closed']==3
 # The sealed mixed-scene create/reset placement is native X=-1.5, Z=0.
 x,y,z=start[0]['Start'];assert x==-1.5 and z==0
 geometry=surface.fixture_geometry(terrain);initial=surface.expected(geometry,x,z);assert initial is not None
 assert math.isclose(y,surface.f(nominal.word(initial['Height'])),abs_tol=2e-6)
 timestamp=start[0]['TimestampMicroseconds'];zbits=nominal.word(z);seconds=nominal.a.scalar(pure['SecondsBits'])
 rows=records(text,'nominal_speed_state=');plays=records(text,'current_plan_playback_step=');targetrows=records(text,'nominal_target_state=');sources=records(text,'current_nominal_source_state=')
 assert len(rows)==len(plays)==len(targetrows)==len(sources)==60
 chunks=set();patches=set()
 for i,(row,step,p,targetrow,source) in enumerate(zip(rows,plan,plays,targetrows,sources)):
  assert row['Index']==targetrow['Index']==i
  nominal.check_result(row['Transition'],step);current.old.check_target(targetrow['Target'],targets[i])
  displacement=nominal.a.bits(nominal.a.precision(nominal.a.mul(nominal.a.scalar(step['StoredBits']),seconds),24))
  zbits=nominal.a.bits(nominal.a.precision(nominal.a.add(nominal.a.scalar(zbits),nominal.a.scalar(displacement)),24));timestamp+=pure['StepMicroseconds']
  wantz=nominal.value(zbits);hit=surface.expected(geometry,x,wantz);assert hit is not None
  chunks.add(hit['ChunkIndex']);patches.add((hit['ChunkIndex'],hit['PatchIndex']));wanty=surface.f(nominal.word(hit['Height']))
  assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip(row['Native'],[x,wanty,wantz])),(i,row,hit)
  assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip(row['Engine'],[x,wanty,-wantz])),(i,row,hit)
  assert row['TimestampMicroseconds']==timestamp and row['StepsThisUpdate']==1 and row['DroppedMicroseconds']==0 and row['FaultType'] is None
  assert row['PublicationRetained'] and row['VisualRetained'] and row['calls']==row['closed']==3
  speed=nominal.a.bits(nominal.circle.sqrt_round(nominal.a.scalar(nominal.a.bits(nominal.a.precision(nominal.a.mul(nominal.a.scalar(step['StoredBits']),nominal.a.scalar(step['StoredBits'])),24))),24))
  assert math.isclose(row['Speed'],nominal.value(speed),abs_tol=2e-6)
  assert p==dict(Index=i,StoredBits=step['StoredBits'],SubmitTimestampMicroseconds=timestamp-10000,AdvanceTimestampMicroseconds=timestamp,CompletedSteps=i+1,StateReads=i+1,SubmitAttempts=i+1,AdvanceAttempts=i+1,State='Complete' if i==59 else 'Ready',ActualSource=True,ActualStep=True,Current=True,OriginalEffectiveMovementParity=False)
  assert source==dict(Index=i,Current=True,RunBits=r['ReturnBits'],WalkBits=w['ReturnBits'],OriginalGetterBranchParity=False)
 stop=records(text,'nominal_speed_stopped=');assert stop==[dict(Speed=0,Active=False,Keys=0)]
 assert records(text,'nominal_speed_cleanup=')==[dict(Closed=True,Steps=60,DriverDisposed=True)]
 assert records(text,'current_terrain_playback_cleanup=')==[dict(Disposed=True,OwnedDriverDisposed=True,OwnedPlaybackDisposed=True,BorrowedSourcePreserved=True,BorrowedTerrainPreserved=True)]
 assert records(text,'current_nominal_source_cleanup=')==[dict(Closed=True)]
 result.update(CurrentTerrainPlayback=True,PlaybackSteps=60,GroundedPositions=60,VisitedChunks=len(chunks),VisitedPatches=len(patches),OriginalEffectiveMovementParity=False)
 return result

if __name__=='__main__':
 fixture,terrain,wire,log,out=sys.argv[1:6];result=verify(json.loads(Path(fixture).read_text()),json.loads(Path(terrain).read_text()),json.loads(Path(wire).read_text()),Path(log).read_text(encoding='utf-8-sig'))
 p=nominal.fresh(out);p.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
