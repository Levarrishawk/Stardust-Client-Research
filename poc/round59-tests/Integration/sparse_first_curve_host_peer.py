"""Sparse join reuses the sealed independent58 full-field reference; modern normals get a Decimal oracle."""
import importlib.util,json,math,sys,runpy,hashlib
from fractions import Fraction
from decimal import Decimal,localcontext
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('sparse59_full_reference',ROOT/'poc/round58-tests/Integration/first_curve_layer_host_peer.py');old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
POLICY='owned_layer_sparse_first_curve_modern_approximation'
REFERENCE=ROOT/'local-private/parallel-development/round58'

def reference(d,assetroot,config,exe):
 assert old.curve.sha(Path(exe).read_bytes())==old.curve.EXE
 gen=old.filter.s.original(Path(assetroot),Path(config));assert d['GeneratorHex']==gen.hex() and old.curve.sha(gen)==d['GeneratorSha256']
 prior=json.loads((REFERENCE/'host-fixtures/first-curve-layer-input.json').read_text());native=json.loads((REFERENCE/'first-curve-layer-native.json').read_text())
 assert json.loads((REFERENCE/'first-curve-layer-native-verification.json').read_text())['Passed']
 old.verify_native(prior,native)
 assert d['Version']==9 and d['HeightPolicy']==POLICY and d['NormalPolicy']=='ModernBinary64CentralDifference' and d['RenderPolicy']=='ModernLitVertexColor' and d['SuppliedWarmPriors']
 for i,(c,e) in enumerate(zip(d['Chunks'],d['Expected'])):
  assert c['Request']==prior['Chunks'][i]['Request'] and e['PositionFirst']==prior['Expected'][i]['PositionFirst'] and e['PositionSecond']==prior['Expected'][i]['PositionSecond']
  assert e['BaselineHeightBits']==native['Chunks'][i]['HeightBits']
  assert c['IncomingWeightBits']==[0 if j%2 else 0x3f800000 for j in range(100)]
  assert c['PriorHeightBits']==[native['Chunks'][i]['HeightBits'][j] if j%2 else 0 for j in range(100)]
 return prior,native

def scalar(b):return Fraction(old.host.p.f32(b))
def normal_reference(e,heights):
 x=e['PositionFirst'];z=e['PositionSecond'];w=10;out=[]
 with localcontext() as ctx:
  ctx.prec=100
  def dec(f):return Decimal(f.numerator)/Decimal(f.denominator)
  for row in range(w):
   for col in range(w):
    left=max(0,col-1);right=min(w-1,col+1);prev=max(0,row-1);nxt=min(w-1,row+1)
    gx=(scalar(heights[row*w+right])-scalar(heights[row*w+left]))/(scalar(x[right])-scalar(x[left]))
    gz=(scalar(heights[nxt*w+col])-scalar(heights[prev*w+col]))/(scalar(z[nxt])-scalar(z[prev]))
    length=(dec(gx*gx+gz*gz)+1).sqrt();out.append([-dec(gx)/length,1/length,-dec(gz)/length])
 return out

def check_normals(bits,expected):
 assert len(bits)==len(expected)==100
 for got,want in zip(bits,expected):
  values=[old.host.p.f32(b) for b in got];assert all(math.isfinite(v) for v in values)
  assert all(abs(Decimal.from_float(v)-e)<=Decimal('1e-7') for v,e in zip(values,want))
  assert abs(sum(v*v for v in values)-1)<3e-7 and values[1]>0

def verify_native(d,g,prior,baseline):
 assert g['Policy']==POLICY and not g['OriginalNumericParity'] and not g['OriginalTerrainParity'] and len(g['Chunks'])==4
 for i,(c,e,got) in enumerate(zip(d['Chunks'],d['Expected'],g['Chunks'])):
  ref=prior['Expected'][i];oldnative=baseline['Chunks'][i];assert got['Chunk']==i and got['PriorHeightBits']==c['PriorHeightBits']
  products=[0 if j%2 else oldnative['InfluenceBits'][j] for j in range(100)]
  assert got['InfluenceBits']==products and got['FilterCounts']==oldnative['FilterCounts']
  assert got['HeightCounts']==dict(BorrowedCurveQueries=50,ConsumerCalls=100,DyadicConversions=50,HeightApplications=50,SkippedCells=50,OwnedSamplerConstructions=0,OwnedSamplerQueryBatches=0,OwnedSamplerDisposals=0)
  assert [r['Index'] for r in got['CurveCells']]==list(range(98,-1,-2));assert [r['RequestIndex'] for r in got['CurveCells']]==list(range(50))
  rule=ref['HeightRule']['Words'];heights=c['PriorHeightBits'].copy()
  for cell in got['CurveCells']:
   j=cell['Index'];r=ref['Curve']['Cells'][j];b=oldnative['CurveCells'][j]
   assert cell['BaseF32Bits']==r['Base'] and old.sample_value(cell['KernelSample'])[0]==Fraction(r['Kernel'])
   assert cell['OutputBinary64Bits']==b['OutputBinary64Bits'];old.curve.close(old.curve.f64(cell['OutputBinary64Bits']),r['ExpectedOutput'])
   heights[j]=old.height.j.height({'Precision':64},(old.curve.f64(cell['OutputBinary64Bits']),False),c['PriorHeightBits'][j],products[j],rule[1],rule[2])
  assert got['HeightBits']==heights==oldnative['HeightBits']
  assert got['PositionHeightBits']==heights and got['NormalCount']==100 and got['NormalSourceActual']
  check_normals(got['NormalBits'],normal_reference(e,heights))
 return dict(Passed=True,Cells=400,HeightCurveQueries=200,HeightApplications=200,SkippedCells=200,EquivalentSealedFullField=True,NormalComparisons=400,NormalDecimalPrecision=100,NormalAbsoluteTolerance='1e-7',SuppliedWarmPriors=True,OriginalCacheParity=False,OriginalNumericParity=False,OriginalNormalParity=False)

def complete(d,g):
 for e,c in zip(d['Expected'],g['Chunks']):
  e['HeightBits']=c['HeightBits'];e['NormalBits']=c['NormalBits'];e.update(Filter=None,Curve=None,HeightRule=None)
 d=old.complete_host_fixture(d,g)
 for e in d['Expected']:
  for patch in e['Patches']:patch['NormalBits']=[e['NormalBits'][i] for i in patch['SourceIndices']]
 return d

def verify_host(d,t):
 assert d['HeightPolicy']==POLICY
 chunks=old.host.records(t,'sparse_first_curve_height_chunk=');normals=old.host.records(t,'sparse_first_curve_normals=');assert len(chunks)==len(normals)==4
 for i,(h,n,e) in enumerate(zip(chunks,normals,d['Expected'])):
  assert h['Chunk']==n['Chunk']==i and h['HeightBits']==e['HeightBits']==e['BaselineHeightBits'];assert h['HeightCounts']['BorrowedCurveQueries']==h['ComputedMisses']==50 and h['HeightCounts']['SkippedCells']==50
  assert n['NormalBits']==e['NormalBits'] and n['SourceActual'] and not n['OriginalNormalParity'];check_normals(n['NormalBits'],normal_reference(e,e['HeightBits']))
 patches=old.host.records(t,'supplied_terrain_patch=');assert len(patches)==16
 for i,p in enumerate(patches):
  e=d['Expected'][i//4]['Patches'][i%4];assert p['Positions']==e['Positions'] and p['Indices']==e['Indices'] and p['TriangleCount']==8 and p['NormalBits']==e['NormalBits'] and p['RenderPolicy']=='ModernLitVertexColor'
 # Reuse the frozen surface/grounding checks with only the new readiness marker translated.
 result=old.verify_host(d,t.replace('sparse_first_curve_height_','first_curve_height_'))
 assert old.host.records(t,'sparse_terrain_light_ready=')==[dict(Count=1,Owned=True,OriginalLightingParity=False)]
 assert old.host.records(t,'sparse_terrain_light_cleanup=')==[dict(Disposed=True)]
 result.update(HeightCurveQueries=200,SkippedCells=200,NormalComparisons=400,LitPolicies=16,OwnedLightDisposed=True,OriginalNormalParity=False);return result

def verify_negative(d,t):
 assert d['Chunks'][3]['IncomingWeightBits'][-1]==0x7fc00000
 r=old.verify_negative(d,t.replace('sparse_first_curve_height_','first_curve_height_'))
 assert not old.host.records(t,'sparse_terrain_light_ready=') and not old.host.records(t,'sparse_terrain_light_cleanup=');r.update(LightAllocated=False);return r
if __name__=='__main__':
 mode,assetroot,config,exe,fixture=sys.argv[1:6];d=json.loads(Path(fixture).read_text());assert Path.cwd().resolve()==ROOT
 guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if mode=='native':
  prior,baseline=reference(d,assetroot,config,exe);g=json.loads(Path(sys.argv[6]).read_text());result=verify_native(d,g,prior,baseline);out=sys.argv[7]
  guard(Path(sys.argv[8]),ROOT).write_text(json.dumps(complete(d,g),indent=2)+'\n',encoding='utf-8')
 elif mode=='host':result=verify_host(d,Path(sys.argv[6]).read_text(encoding='utf-8-sig'));out=sys.argv[7]
 elif mode=='negative':result=verify_negative(d,Path(sys.argv[6]).read_text(encoding='utf-8-sig'));out=sys.argv[7]
 else:raise ValueError(mode)
 guard(Path(out),ROOT).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
