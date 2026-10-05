"""Independent60 composition checks extend sealed59 arithmetic/host oracles."""
import copy,importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
terrain=load('owned60_sparse59','poc/round59-tests/Integration/sparse_first_curve_host_peer.py')
current=load('owned60_current59','poc/round59-tests/Integration/current_nominal_host_peer.py')
def sparse_charges(g):
 n=100;m=50;w=10;p=4;r=g['SparseReservation']
 expected=dict(OwnCopyBytes=84+16*m+4*n,OwnEncodedCopyBytes=84,OwnRequestBytes=16*m,OwnPriorBytes=4*n,OwnArrayBytes=340+16*m+4*n,OwnWork=84+16*n+16*m,
 BatchCopyBytes=696+176*m,BatchRouteBytes=10088+32*n+1600*m,BatchWork=20424+n+8288*m,BatchQueryBytes=16*n+1440*m,BatchQueryWork=n+8256*m,
 ConsumerCopyBytes=4*n,ConsumerArrayBytes=256+192*n,ConsumerMappingBytes=4*n,ConsumerWork=84+32*n+16*m)
 expected.update(AggregateCopyBytes=expected['OwnCopyBytes']+expected['BatchCopyBytes']+expected['ConsumerCopyBytes'],OwnedArrayBytes=expected['OwnArrayBytes']+expected['BatchRouteBytes']+expected['ConsumerArrayBytes'],
 AggregateRouteBytes=expected['OwnArrayBytes']+expected['BatchRouteBytes']+expected['ConsumerArrayBytes']+expected['ConsumerMappingBytes'],AggregateWork=expected['OwnWork']+expected['BatchWork']+expected['ConsumerWork'])
 assert r==expected,(r,expected)
 assert g['OwnedSamplerFactories']==g['OwnedSamplerQueryBatches']==g['OwnedSamplerDisposals']==1 and g['SparseReferences'] and g['AssemblyReferences']
 a=g['ModernAssembly'];assert a==dict(NormalBuilds=1,FinalPatchBuilds=1,OwnCopyBytes=36*n,NormalCopyBytes=8*w+4*n,FinalReservedCopyBytes=20*n+1024*p,
 AggregateCopyBytes=60*n+8*w+1024*p,AggregateArrayBytes=768+8*w+88*n+2048*p,AggregateWork=512+8*w+192*n+4100*p,PatchCount=p,SelectedSamples=9*p,FinalBuilderSupportsCancellation=False)
def terrain_native(d,g,assetroot,config,exe):
 prior,baseline=terrain.reference(d,assetroot,config,exe);adjusted=copy.deepcopy(g)
 for actual,ref in zip(g['Chunks'],adjusted['Chunks']):
  assert [c['Index'] for c in actual['CurveCells']]==list(range(0,100,2))
  assert [c['RequestIndex'] for c in actual['CurveCells']]==list(range(50))
  sparse_charges(actual)
  ref['CurveCells'].reverse()
  for j,c in enumerate(ref['CurveCells']):c['RequestIndex']=j
 result=terrain.verify_native(d,adjusted,prior,baseline);result.update(OwnedEvaluations=4,ModernAssemblies=4,RowMajorRequestSets=4,ActualCompositionReferences=True);return result

def terrain_host(d,t):
 result=terrain.verify_host(d,t)
 rows=terrain.old.host.records(t,'owned_sparse_evaluation_ready=');patches=terrain.old.host.records(t,'modern_height_patch_ready=')
 assert len(rows)==len(patches)==4
 for i,(r,p) in enumerate(zip(rows,patches)):
  assert r['Chunk']==p['Chunk']==i
  q={k:v for k,v in r.items() if k!='Chunk'};q.update({k:v for k,v in p.items() if k!='Chunk'});sparse_charges(q)
 result.update(OwnedEvaluations=4,ModernAssemblies=4,ActualCompositionReferences=True);return result

def current_host(d,wire,t):
 result=current.verify_host(d,wire,t);r,w=current.sources(d)
 rows=current.old.old.records(t,'paired_current_speed_ready=')
 assert rows==[dict(OutputBytes=2080,ReservedCopyBytes=256,ReservedWork=16384,ActualWork=r['Work']+w['Work'],ReaderCalls=2,SuppliedWords=6,Multiplications=4,CallerF32Stores=2,ActualReferences=True,Current=True,OriginalGetterBranchParity=False)]
 result.update(ActualPairedSource=True,PairOutputBytes=2080);return result

if __name__=='__main__':
 mode,fixture,source,out=sys.argv[1:5];d=json.loads(Path(fixture).read_text());assert Path.cwd().resolve()==ROOT
 if mode=='native':result=terrain_native(d,json.loads(Path(source).read_text()),*sys.argv[5:8])
 elif mode=='terrain-host':result=terrain_host(d,Path(source).read_text(encoding='utf-8-sig'))
 elif mode=='current-host':result=current_host(d,json.loads(Path(sys.argv[5]).read_text()),Path(source).read_text(encoding='utf-8-sig'))
 elif mode=='terrain-negative':
  t=Path(source).read_text(encoding='utf-8-sig');result=terrain.verify_negative(d,t)
  assert len(terrain.old.host.records(t,'owned_sparse_evaluation_ready='))==len(terrain.old.host.records(t,'modern_height_patch_ready='))==3
 elif mode=='current-negative':
  t=Path(source).read_text(encoding='utf-8-sig');result=current.verify_negative(d,t);assert not current.old.old.records(t,'paired_current_speed_ready=')
 else:raise ValueError(mode)
 current.old.old.fresh(out).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
