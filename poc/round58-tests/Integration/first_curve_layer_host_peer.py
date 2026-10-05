"""One pinned layer under synthetic grids and an explicit modern curve policy."""
import importlib.util,json,math,struct,sys,runpy
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
filter=load('layer58_filter','poc/round57-tests/Assets/peer.py')
curve=load('layer58_curve','poc/round57-tests/Rendering/verify_curve.py')
geometry=load('layer58_geometry','poc/round39-tests/Integration/generate_terrain_peer.py')
host=load('layer58_host','poc/round45-tests/Integration/height_filtered_host_peer.py')
height=load('layer58_height','poc/round53-tests/Integration/original_rule_height_host_peer.py')
POLICY='owned_layer_first_curve_modern_approximation'
def generate(assetroot,config,exe):
 assert curve.sha(Path(exe).read_bytes())==curve.EXE
 mode=curve.load('layer58_modes','poc/round50-tests/Rendering/verify_modes.py');mode.n.setup()
 filter.r.n.setup();filter.r.f.route.lead.n.setup()
 generator=filter.s.original(Path(assetroot),Path(config))
 selector=curve.load('layer58_selector','tools/python/verify_terrain_fractal_curve_finish.py')
 meta,private=selector.selected(Path(assetroot),Path(config))
 raw=mode.b.frame(private['Words']);assert curve.sha(raw)==meta['MfrcSha256']
 d=geometry.generate();d.update(Version=8,HeightPolicy=POLICY,OriginalNumericParity=False,OriginalTerrainParity=False,
  OriginalLayerUsed=True,LayerIndex=3,GeneratorHex=generator.hex(),GeneratorSha256=curve.sha(generator),
  CurvePolicy='ModernBinary64LogPowApproximation',Controls=[53,64,64,64],FilterControls=[53,64,64,64,64,64],
  BoundaryControls=[64,64,64],SelectedCurve=meta,Shape=dict(Chunks=4,CellsPerChunk=100))
 for i,(c,e) in enumerate(zip(d['Chunks'],d['Expected'])):
  c.pop('HeightBits');c.pop('NormalBits');c.pop('Argb');c['PriorHeightBits']=[0]*100;c['IncomingWeightBits']=[0x3f800000]*100
  case=dict(Name=f'chunk-{i}',GeneratorHex=generator.hex(),Index=3,Request=c['Request'],Profiles=[24]*4,Incoming=c['IncomingWeightBits'],
   Cache=64,Query=64,BoundaryCombine=64,Table=53,Construction=64,SamplerQuery=64,FilterQuery=64,Combine=64,Product=64)
  expected,witnesses=filter.expected(case);assert expected['Reject']=='None'
  rows=expected['Snapshot']['Cells']
  requests=[dict(Column=r['Coordinates']['Column'],Row=r['Coordinates']['Row'],XBits=r['Coordinates']['FirstBits'],ZBits=r['Coordinates']['SecondBits']) for r in rows]
  cc=dict(Name=f'curve-{i}',Hex=raw.hex(),Width=10,Rows=10,Requests=requests,Table=53,Construction=64,Query=64)
  ce=curve.expected(cc,mode);assert ce['Reject']=='None',ce
  e.update(Filter=expected['Snapshot'],Curve=ce,HeightRule=filter.s.expected(dict(Name='layer',GeneratorHex=generator.hex(),Index=3,Policy=0))['Snapshot']['Height'])
 return d
def sample_value(s):
 value=Fraction(s['Magnitude'])*Fraction(2)**s['Exponent']
 return (-value if s['IsNegative'] else value,s['Magnitude']==0 and s['IsNegative'])
def verify_native(d,actual):
 assert actual['Policy']==POLICY and not actual['OriginalNumericParity'] and not actual['OriginalTerrainParity']
 assert actual['LayerIndex']==3 and actual['GeneratorSha256']==d['GeneratorSha256'] and len(actual['Chunks'])==4
 checks=0;applied=0
 for i,(got,c,e) in enumerate(zip(actual['Chunks'],d['Chunks'],d['Expected'])):
  assert got['Chunk']==i and got['PriorHeightBits']==c['PriorHeightBits']
  assert got['InfluenceBits']==[r['Influence']['ProductBits'] for r in e['Filter']['Cells']]
  assert got['FilterCounts']==e['Filter']['Counts']
  assert len(got['CurveCells'])==100 and len(got['HeightBits'])==100
  assert got['HeightCounts']==dict(BorrowedCurveQueries=100,ConsumerCalls=100,DyadicConversions=100,HeightApplications=100,SkippedCells=0,OwnedSamplerConstructions=0,OwnedSamplerQueryBatches=0,OwnedSamplerDisposals=0)
  rule=e['HeightRule']['Words']
  # API-specific height field names are reconciled before acceptance.
  for k,(g,ce) in enumerate(zip(got['CurveCells'],e['Curve']['Cells'])):
   assert g['BaseF32Bits']==ce['Base'] and sample_value(g['KernelSample'])[0]==Fraction(ce['Kernel'])
   curve.close(curve.f64(g['OutputBinary64Bits']),ce['ExpectedOutput'])
   val=curve.f64(g['OutputBinary64Bits']);weight=got['InfluenceBits'][k]
   expected=height.j.height({'Precision':64},(val,False),c['PriorHeightBits'][k],weight,rule[1],rule[2]) if height.j.a.scalar(weight)[0]>0 else c['PriorHeightBits'][k]
   assert got['HeightBits'][k]==expected,(i,k,got['HeightBits'][k],expected)
   applied+=height.j.a.scalar(weight)[0]>0;checks+=1
 return dict(Passed=True,Cells=checks,HeightApplications=applied,CurvePolicy='ModernBinary64LogPowApproximation',
  OriginalNumericParity=False,OriginalTerrainParity=False,OneOriginalLayer=True)
def complete_host_fixture(d,actual):
 for got,e in zip(actual['Chunks'],d['Expected']):
  e['HeightBits']=got['HeightBits']
  for patch in e['Patches']:
   for j,index in enumerate(patch['SourceIndices']):patch['Positions'][j][1]=got['HeightBits'][index]
 poses={}
 for name,q in zip(host.NAMES,host.XZ):
  hit=next((hit for e in d['Expected'] for p in e['Patches'] if (hit:=host.surface(p,q)) is not None),None)
  assert hit is not None;poses[name]=0 if name=='corrected_paused' else host.p.word(hit['Height'])
 d['MotionExpectedBits']=poses
 for e in d['Expected']:
  for key in ('Filter','Curve','HeightRule'):e.pop(key)
 assert len(json.dumps(d).encode())<=524288,'Bounded host fixture'
 return d
def verify_host(d,text):
 rows=host.records(text,'first_curve_height_chunk=');assert len(rows)==4
 for i,(got,e) in enumerate(zip(rows,d['Expected'])):assert got['Chunk']==i and got['HeightBits']==e['HeightBits']
 patches=host.records(text,'supplied_terrain_patch=');assert len(patches)==16
 for i,p in enumerate(patches):
  e=d['Expected'][i//4]['Patches'][i%4];assert p['Positions']==e['Positions'] and p['Indices']==e['Indices'] and p['TriangleCount']==8
 states=host.records(text,'local_state=');assert [s['Name'] for s in states]==host.NAMES
 for s,q in zip(states,host.XZ):
  x,y,z=s['Native'];assert math.isclose(x,q[0],abs_tol=2e-6) and math.isclose(z,q[1],abs_tol=2e-6)
  hit=next((hit for e in d['Expected'] for p in e['Patches'] if (hit:=host.surface(p,[x,z])) is not None),None)
  assert hit is not None;want=0 if s['Name']=='corrected_paused' else host.p.f32(host.p.word(hit['Height']))
  assert math.isclose(y,want,abs_tol=2e-6)
  assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip(s['Engine'],[x,y,-z]))
 assert host.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
 ready=host.records(text,'first_curve_height_ready=');assert len(ready)==1 and ready[0]['OriginalNumericParity'] is False
 return dict(Passed=True,Cells=400,Patches=16,Triangles=128,GroundedStates=9,OriginalNumericParity=False,OriginalTerrainParity=False)
def verify_negative(d,text):
 assert d['Chunks'][3]['IncomingWeightBits'][-1]==0x7fc00000
 assert host.records(text,'first_curve_height_preflight_failure=')==[dict(CompletedChunks=3,AllocatedTerrain=0,DisposedTerrain=0,OriginalNumericParity=False)]
 assert len(host.records(text,'first_curve_height_chunk='))==3
 for prefix in ('first_curve_height_ready=','supplied_terrain_ready=','supplied_terrain_patch='):assert not host.records(text,prefix)
 c=host.records(text,'pump_failure_cleanup=');assert len(c)==1 and c[0]['drained'] and c[0]['actors']==0
 return dict(Passed=True,CompletedNativeChunks=3,TerrainAllocations=0,Actors=0,ArchivesDrained=True)
if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6];assert Path.cwd().resolve()==ROOT
 guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if action=='generate':
  d=generate(assetroot,config,exe);guard(Path(fixture),ROOT).write_text(json.dumps(d,indent=2)+'\n');print('Prepared400 original-layer approximation cells')
 elif action=='native':
  d=json.loads(Path(fixture).read_text());assert d==generate(assetroot,config,exe)
  got=json.loads(Path(sys.argv[6]).read_text());r=verify_native(d,got)
  guard(Path(sys.argv[7]),ROOT).write_text(json.dumps(r,indent=2)+'\n')
  guard(Path(sys.argv[8]),ROOT).write_text(json.dumps(complete_host_fixture(d,got),indent=2)+'\n');print(json.dumps(r))
 elif action in ('host','negative'):
  d=json.loads(Path(fixture).read_text());r=(verify_negative if action=='negative' else verify_host)(d,Path(sys.argv[6]).read_text(encoding='utf-8-sig'))
  guard(Path(sys.argv[7]),ROOT).write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r))
 else:raise ValueError(action)

