"""Independent Fraction oracle for an explicit complete-grid first boundary gate."""
import importlib.util,itertools,json,sys,types
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
s=load('admission_slice','poc/round55-tests/Assets/peer.py')
l=load('admission_layer','poc/round40-tests/Rendering/verify_layer.py')
gridpath=ROOT/'poc/round38-tests/Rendering/verify_grid.py'
grid=types.ModuleType('admission_grid');grid.__file__=str(gridpath);exec(gridpath.read_text().split('p=argparse.ArgumentParser();')[0],grid.__dict__)
fb=l.fb
DEFAULT_REQUEST=dict(ChunkWidthBits=fb(4),Tiles=2,Margin=0,First=0,Second=0,Scale=1)
DEFAULT_LIMITS=dict(MaximumAxisCount=256,MaximumCells=65536,MaximumQueries=65536,MaximumArrayBytes=16777216,MaximumCopyBytes=1572864,MaximumWork=4194304)
HARD=dict(MaximumAxisCount=1024,MaximumCells=1048576,MaximumQueries=1048576,MaximumArrayBytes=268435456,MaximumCopyBytes=25165824,MaximumWork=67108864)
def expected(c):
 fail=dict(Name=c['Name'],Reject='Input')
 try:
  selected=s.expected(dict(Name=c['Name'],GeneratorHex=c['GeneratorHex'],Index=c.get('Index',0),Policy=c.get('Policy',0)))
  if selected['Reject']!='None' or c.get('Cancel') or c.get('Policy',0)!=0:return fail
  sl=selected['Snapshot'];w=sl['Circle']['Words'];limits={**DEFAULT_LIMITS,**{k:c[k] for k in DEFAULT_LIMITS if k in c}}
  if any(not 0<=v<=HARD[k] for k,v in limits.items()) or any(c.get(k,53) not in (24,53,64) for k in ('Cache','Query','Combine')):return fail
  if not sl['IsLocallyEnabled'] or not sl['Circle']['Header']['EnabledBits'] or not all(s.finite(w[i]) for i in (0,1,2,4)):return fail
  g=grid.evaluate(dict(Name=c['Name'],Request=c['Request'],Profiles=c['Profiles'],Limits=dict(MaximumCount=1024,MaximumPairs=1048576)))
  if g['Reject']!='None':return fail
  n=g['Parameters']['Count'];cells=n*n
  charges=dict(MaximumAxisCount=n,MaximumCells=cells,MaximumQueries=cells,MaximumArrayBytes=256*cells,MaximumCopyBytes=24*cells,MaximumWork=64*cells)
  if any(v>limits[k] for k,v in charges.items()):return fail
  out=[]
  for pair in g['Layer']['Pairs']:
   q=dict(Type='Circle',Geometry=w[:3],Coordinates=[pair['FirstBits'],pair['SecondBits']],Amount=w[4],CachePrecision=c.get('Cache',53),Precision=c.get('Query',53))
   cq=l.c.evaluate({**q,'Selector':w[3],'Enabled':1,'Version':'0002','Action':'query'})
   if cq['Reject']!='None':return fail
   receipt=l.evaluate(dict(Name=c['Name'],Boundaries=[dict(Enabled=True,Selector=w[3],Query=q)],Filters=[],Incoming=0x3f800000,Combine=c.get('Combine',53),Product=c.get('Combine',53),BoundaryGate=1,FilterGate=0,InvertBoundary=sl['BoundaryInverted'],InvertFilter=False,CreateMaximumQueries=1,Limits=[1,1,1]))
   if receipt['Reject']!='None':return fail
   carrier=receipt.pop('BoundaryInputs')[0];receipt.pop('FilterInputs');receipt.pop('Name');receipt.pop('Reject')
   out.append(dict(Coordinates=pair,BoundaryBits=receipt['LocalInfluenceBits'],Admitted=receipt['BoundaryPositive'],Circle=dict(FirstCoordinateBits=pair['FirstBits'],SecondCoordinateBits=pair['SecondBits'],RuntimeFeatherAmountBits=cq['Amount'],RadiusSquaredBits=cq['RadiusSquared'],DistanceSquaredBits=cq['DistanceSquared'],QueryProjectionBits=cq['Query'],Branch=cq['Branch'],CachePrecision=q['CachePrecision'],Precision=q['Precision']),Carrier=carrier,Probe=receipt,Identity=True))
  result=dict(GeneratorSha256=sl['GeneratorSha256'],LayerIndex=sl['LayerIndex'],LayerSha256=sl['LayerSha256'],CircleSource=sl['Children'][0],Request=g['Request'],Policies=g['Policies'],Parameters=g['Parameters'],Controls=dict(CachePrecision=c.get('Cache',53),QueryPrecision=c.get('Query',53),CombinePrecision=c.get('Combine',53)),AxisCount=n,QueryCount=cells,CarrierCount=cells,ReceiptCount=cells,ArrayBytes=256*cells,CopiedEntryBytes=24*cells,CopiedInputBytes=0,Work=64*cells,Cells=out)
  return dict(Name=c['Name'],Reject='None',Snapshot=result)
 except (ValueError,AssertionError,OverflowError,ZeroDivisionError):return fail

def generate(exe,packages,root,cfg,out):
 assert s.sha(exe.read_bytes())==s.t.EXE
 sys.path.insert(0,str(packages));static=dict(Circle=s.b.static(exe),Adta=s.ad.static(exe,packages),CircleQuery=l.c.static(exe,packages))
 cases=[]
 def circle(w=None,**kw):return s.rule(b'BCIR',b'0002',w or [fb(1),fb(1),fb(1.8),0,fb(.5)],**kw)
 base=s.generator(circle()+s.filter()+s.height())
 def add(name,g=None,**kw):cases.append(dict(Name=name,GeneratorHex=(g or base).hex(),Request=kw.pop('Request',DEFAULT_REQUEST.copy()),Profiles=kw.pop('Profiles',[53]*4),**kw))
 add('synthetic-complete');add('original-layer3-boundary',s.original(root,cfg),Index=3)
 for cp,qp,p in itertools.product((24,53,64),repeat=3):
  for sel in (0,1,2,3):add(f'controls-{cp}-{qp}-{p}-{sel}',s.generator(circle([fb(1),fb(1),fb(1.8),sel,fb(.5)])+s.filter()+s.height()),Cache=cp,Query=qp,Combine=p)
 for sel in (4,0xffffffff):add('unknown-selector-'+str(sel),s.generator(circle([fb(1),fb(1),fb(1.8),sel,fb(.5)])+s.filter()+s.height()))
 for inv in (0,1,0x80000000,0x7fc12345):
  for fi in (0,0xffffffff):add(f'inversion-{inv}-{fi}',s.generator(circle()+s.filter()+s.height(),adta=s.t.words([inv,fi,0xffffffff])+b'synthetic\0'))
 for amount in (-1,0,1,2):add('amount-'+str(amount),s.generator(circle([fb(1),fb(1),fb(-1.8),3,fb(amount)])+s.filter()+s.height()))
 for at in (0,1,2,4):
  w=[fb(1),fb(1),fb(1.8),0,fb(.5)];w[at]=0x7fc12345;add('nonfinite-'+str(at),s.generator(circle(w)+s.filter()+s.height()))
 add('disabled-circle',s.generator(circle(enabled=0)+s.filter()+s.height()));add('disabled-layer',s.generator(circle()+s.filter()+s.height(),h=s.t.header(enabled=0)))
 changed=list(s.FAMILY);changed[12]=6;add('unsupported-height-family-allowed',s.generator(circle()+s.filter()+s.height(),entries=[(-7,changed)]))
 add('operational-policy-reject',Policy=1);add('cancel',Cancel=True)
 exact=dict(MaximumAxisCount=4,MaximumCells=16,MaximumQueries=16,MaximumArrayBytes=4096,MaximumCopyBytes=384,MaximumWork=1024);add('exact-reservations',**exact)
 for k,v in exact.items():add('one-less-'+k,**{**exact,k:v-1})
 for k,v in HARD.items():add('invalid-option-'+k,**{k:v+1});add('negative-option-'+k,**{k:-1})
 for k in ('Cache','Query','Combine'):add('invalid-control-'+k,**{k:0})
 add('late-overflow',Request={**DEFAULT_REQUEST,'ChunkWidthBits':0x7f000000,'Tiles':1})
 add('preallocation-large-grid',Request={**DEFAULT_REQUEST,'Tiles':128},MaximumArrayBytes=0)
 for p in (24,64):add('coordinate-profile-'+str(p),Profiles=[p]*4)
 add('zero-radius',s.generator(circle([0,0,0,0,fb(.5)])+s.filter()+s.height()))
 add('radius-cache-overflow',s.generator(circle([0,0,0x7f7fffff,0,fb(.5)])+s.filter()+s.height()))
 nested=s.t.layer(h=s.t.header(enabled=0),children=s.t.layer(h=s.t.header(enabled=1),children=circle()+s.filter()+s.height()));add('disabled-parent-explicit-child',s.generator(last=s.t.form(b'LYRS',nested)),Index=1)
 e=[expected(c) for c in cases];assert e[0]['Reject']==e[1]['Reject']=='None';assert next(x for x in e if x['Name']=='late-overflow')['Reject']=='Input'
 assert not s.expected({**cases[1],'Policy':0})['Snapshot']['HasSupportedInputs']
 assert next(x for x in e if x['Name']=='unsupported-height-family-allowed')['Reject']=='None'
 assert {cell['Circle']['Branch'] for x in e if x['Reject']=='None' and x['Name']!='original-layer3-boundary' for cell in x['Snapshot']['Cells']}=={0,1,2}
 for inv in (0,1,0x80000000,0x7fc12345):
  pair=[next(x for x in e if x['Name']==f'inversion-{inv}-{fi}')['Snapshot']['Cells'] for fi in (0,0xffffffff)];assert pair[0]==pair[1]
 out.joinpath('fixtures.json').write_text(json.dumps(cases));out.joinpath('expected.json').write_text(json.dumps(e))
 receipt=dict(Cases=len(cases),Rejected=sum(x['Reject']!='None' for x in e),OriginalLayersInspected=1,OriginalLayerIndex=3,OriginalHeightSamples=0,Static=static)
 out.joinpath('producer-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
def verify(out):
 n=json.loads(out.joinpath('native.json').read_text());e=json.loads(out.joinpath('expected.json').read_text())
 for actual,want in zip(n['Cases'],e):
  if actual!=want:
   out.joinpath('first-mismatch.json').write_text(json.dumps(dict(Actual=actual,Expected=want),indent=2));raise AssertionError('Boundary fixture '+want['Name'])
 assert len(n['Cases'])==len(e) and n['OwnershipChecks']==14
 receipt=json.loads(out.joinpath('producer-receipt.json').read_text());receipt.update(Passed=True,OwnershipChecks=14);receipt['Static']['Layer']=json.loads(out.joinpath('static-layer.json').read_text())
 out.joinpath('comparison-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*(Path(x) for x in sys.argv[2:]))
 elif sys.argv[1]=='verify':verify(Path(sys.argv[2]))
 else:raise ValueError('Operation')
