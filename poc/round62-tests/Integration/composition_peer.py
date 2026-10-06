"""Independent61 input, retained-set and composed-plan checks over sealed60 host references."""
import importlib.util,json,struct,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('composition62_reference61',ROOT/'poc/round61-tests/Integration/composition_peer.py');old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
records=old.records
qspec=importlib.util.spec_from_file_location('round62_surface_reference',ROOT/'poc/round62-tests/Integration/surface_query_peer.py');qpeer=importlib.util.module_from_spec(qspec);qspec.loader.exec_module(qpeer)

def check_query_set(s):
 expected=dict(ChunkCount=4,PatchCount=16,TotalTriangles=128,ChildBuilds=16,NormalBuilds=0,GeometryBuilds=0,SetAdmissions=0,AggregateCopyBytes=4224,AggregateArrayBytes=11904,AggregateWork=18816,ChildLogicalOwnedBytes=5248,ActualSource=True,OriginalCollisionParity=False)
 for key,value in expected.items():assert s[key]==value,(key,s,expected)
 if 'ActualChildren' in s:assert s['ActualChildren']


def shader_reference(d):
 g=bytes.fromhex(d['GeneratorHex']);assert hashlib.sha256(g).hexdigest().upper()=='B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32'
 def extent(p,end):
  assert p+8<=end;z=p+8+struct.unpack_from('>I',g,p+4)[0];assert z<=end;return z
 begin=24;end=extent(begin,len(g));assert g[begin:begin+4]==b'FORM' and g[begin+8:begin+12]==b'SGRP'
 v=begin+12;assert extent(v,end)==end and g[v+8:v+12]==b'0006';p=v+12;families=entries=strings=0
 while p<end:
  z=extent(p,end);assert g[p:p+4]==b'SFAM';q=p+12
  for _ in range(2):
   t=g.index(0,q,z);strings+=t-q+1;q=t+1
  assert q+15<=z;n=struct.unpack_from('<I',g,q+11)[0];q+=15
  for _ in range(n):
   t=g.index(0,q,z);strings+=t-q+1;q=t+5
  assert q==z;families+=1;entries+=n;p=z
 return dict(EncodedBytes=end-begin,SourceSha256=hashlib.sha256(g[begin:end]).hexdigest().upper(),Families=families,TotalEntries=entries,TotalStringBytes=strings,HasShaderSemantics=False,ActualSource=True)

def check_set(s):
 for key,value in dict(Count=4,TotalCells=400,TotalPatches=16,SelectedSamples=144,SharedEdgePairs=4,SharedCornerPairs=2,SharedSampleComparisons=22,NormalBuilds=0,GeometryBuilds=0,ActualReferences=True,EdgePolicy='RequireBitIdenticalActiveSharedEdges',NormalPolicy='RequireBitIdenticalSharedNormals',BorrowedCopyReservation=40704,BorrowedArrayReservation=71360,BorrowedWorkReservation=144768).items():assert s[key]==value,(key,s)
 assert s['OwnCopyBytes']==1632 and s['OwnArrayBytes']==2368 and s['OwnWork']==8256

def terrain_host(d,t):
 result=old.terrain_host(d,t);rows=records(t,'modern_set_surface_query_samples=');assert len(rows)==1;assert [(r['X'],r['Z']) for r in rows[0]]==qpeer.fixed_locations();query=qpeer.compare(qpeer.fixture_geometry(d),rows[0]);ready=records(t,'modern_set_surface_query_ready=');assert len(ready)==1;check_query_set(ready[0]);result.update(SurfaceQueries=query['Queries'],SurfaceHits=query['Hits'],SurfaceMisses=query['Misses']);shader=records(t,'terrain_shader_families_ready=');assert len(shader)==1
 expected=shader_reference(d)
 for key,value in expected.items():assert shader[0][key]==value,(key,shader,expected)
 sets=records(t,'modern_patch_set_ready=');assert len(sets)==1;check_set(sets[0])
 result.update(ShaderFamilies=expected['Families'],ShaderEntries=expected['TotalEntries'],StrictSharedPoints=22,RetainedPatchSet=True);return result

def current_host(d,wire,t):
 result=old.current_host(d,wire,t);ready=records(t,'current_plan_playback_ready=');assert ready==[dict(Steps=60,StepMicroseconds=10000,OutputBytes=512,BorrowedDriver=True,BorrowedComposition=True,ActualPlan=True,OriginalEffectiveMovementParity=False)];play=records(t,'current_plan_playback_step=');trace=records(t,'nominal_speed_state=');assert len(play)==len(trace)==60;
 for i,(p,r) in enumerate(zip(play,trace)):
  assert p==dict(Index=i,StoredBits=r['Transition']['StoredBits'],SubmitTimestampMicroseconds=r['TimestampMicroseconds']-10000,AdvanceTimestampMicroseconds=r['TimestampMicroseconds'],CompletedSteps=i+1,StateReads=i+1,SubmitAttempts=i+1,AdvanceAttempts=i+1,State='Complete' if i==59 else 'Ready',ActualSource=True,ActualStep=True,Current=True,OriginalEffectiveMovementParity=False),(p,r)
 result.update(ActualPlayback=True,PlaybackSteps=60);rows=records(t,'current_plan_composition_ready=');assert len(rows)==1;s=rows[0];plan=records(t,'nominal_speed_plan_ready=')[0];n=len(d['Steps'])
 expected=dict(OutputBytes=2336+864*n,ReservedCopyBytes=864*n,OwnWork=128+64*n,OwnCopyBytes=224*n,ChildCopyBytes=640*n,ChildReservedWork=plan['ReservedWork'],ActualChildWork=plan['ActualWork'],ReaderReloads=0,TargetEvaluations=n,TransitionEvaluations=n,ActualReferences=True,Current=True,OriginalEffectiveMovementParity=False)
 expected['ReservedWork']=expected['OwnWork']+expected['ChildReservedWork'];assert s==expected,(s,expected)
 result.update(ActualComposedPlan=True,ReaderReloads=0);return result

def late_plan(t):
 assert not records(t,'current_plan_playback_ready=') and not records(t,'current_plan_playback_step=')
 assert len(records(t,'paired_current_speed_ready='))==1 and not records(t,'current_plan_composition_ready=') and not records(t,'nominal_speed_plan_ready=')
 for key in ('nominal_speed_state=','nominal_target_state=','current_nominal_source_state=','nominal_speed_ready='):assert not records(t,key)
 assert records(t,'nominal_speed_preflight_failure=')==[dict(CompletedSteps=0,DriverConstructed=False,MotionPublication=False)]
 assert records(t,'current_nominal_source_cleanup=')==[dict(Closed=True)]
 cleanup=records(t,'pump_failure_cleanup=');assert len(cleanup)==1 and cleanup[0]['calls']==cleanup[0]['closed']==3 and cleanup[0]['actors']==0 and cleanup[0]['drained']
 return dict(Passed=True,PairPublished=True,ComposedPlanPublished=False,NoNewMotion=True,Actors=0,ArchivesDrained=True)

def seam_negative(t):
 assert not records(t,'modern_set_surface_query_ready=') and not records(t,'modern_set_surface_query_samples=')
 assert len(records(t,'owned_sparse_evaluation_ready='))==len(records(t,'modern_height_patch_ready='))==4
 assert not records(t,'modern_patch_set_ready=') and not records(t,'sparse_terrain_light_ready=') and not records(t,'supplied_terrain_patch=')
 assert records(t,'sparse_first_curve_height_preflight_failure=')==[dict(CompletedChunks=4,AllocatedTerrain=0,DisposedTerrain=0,OriginalNumericParity=False)]
 cleanup=records(t,'pump_failure_cleanup=');assert len(cleanup)==1 and cleanup[0]['calls']==cleanup[0]['closed']==0 and cleanup[0]['actors']==0 and cleanup[0]['drained']
 return dict(Passed=True,CompletedNativeChunks=4,StrictSetPublished=False,TerrainAllocations=0,LightAllocated=False,ArchivesDrained=True)

if __name__=='__main__':
 mode,fixture,source,out=sys.argv[1:5];d=json.loads(Path(fixture).read_text());t=Path(source).read_text(encoding='utf-8-sig');assert Path.cwd().resolve()==ROOT
 if mode=='native':
  g=json.loads(t);result=old.old.terrain_native(d,g,*sys.argv[5:8]);assert g['MotionChecks']==dict(Passed=True,Checks=13,ResourceLoads=0,ActualAggregateGrounding=True,AdmissionRejectedBeforePublication=2,OriginalCollisionParity=False);check_query_set(g['QuerySet']);assert g['QueryGeometry']==qpeer.fixture_geometry(d);assert [(r['X'],r['Z']) for r in g['SurfaceQueries']]==qpeer.fixed_locations();result.update(qpeer.compare(qpeer.fixture_geometry(d),g['SurfaceQueries']));check_set(g['PatchSet']);assert g['Shader']==shader_reference(d);result.update(StrictSharedPoints=22,ShaderFamilies=15,ShaderEntries=35)
 elif mode=='terrain-host':result=terrain_host(d,t)
 elif mode=='current-host':result=current_host(d,json.loads(Path(sys.argv[5]).read_text()),t)
 elif mode=='terrain-negative':result=old.old.terrain.verify_negative(d,t);assert not records(t,'modern_patch_set_ready=') and not records(t,'modern_set_surface_query_ready=')
 elif mode=='current-negative':result=old.old.current.verify_negative(d,t);assert not records(t,'current_plan_composition_ready=') and not records(t,'current_plan_playback_ready=')
 elif mode=='late-plan':result=late_plan(t)
 elif mode=='seam-negative':result=seam_negative(t)
 else:raise ValueError(mode)
 old.old.current.old.old.fresh(out).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
