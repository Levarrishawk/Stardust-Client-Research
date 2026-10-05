"""Independent complete-grid boundary -> admitted no-curve FFRA -> local/incoming-product Fraction oracle."""
import json,sys,copy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
import importlib.util
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);sys.modules[name]=m;spec.loader.exec_module(m);return m
b=load('admitted_boundary56','poc/round56-tests/Assets/peer.py');r=load('admitted_filter55','poc/round55-tests/Rendering/verify_filtered_field.py');s=b.s;l=b.l
fb=b.fb
DEFAULT=dict(MaximumAxisCount=32,MaximumCells=1024,MaximumFilterQueries=1024,MaximumCopyBytes=131072,MaximumArrayBytes=1048576,MaximumRouteBytes=8388608,MaximumWork=16777216,MaximumSamplerBytes=1048576,MaximumSamplerWork=1048576,MaximumQueryBytes=2097152,MaximumQueryWork=16777216)
HARD={**DEFAULT,'MaximumCopyBytes':1048576,'MaximumArrayBytes':8388608,'MaximumRouteBytes':33554432,'MaximumWork':33554432}
def reserve(n):return dict(MaximumAxisCount=int(n**.5),MaximumCells=n,MaximumFilterQueries=n,MaximumCopyBytes=68*n,MaximumArrayBytes=512*n,MaximumRouteBytes=512*n+9648+16*n+1456*n,MaximumWork=20256+8257*n+96*n,MaximumSamplerBytes=9648+16*n,MaximumSamplerWork=20256,MaximumQueryBytes=1456*n,MaximumQueryWork=8257*n)
def boundary(c):
 return b.expected(dict(Name=c['Name'],GeneratorHex=c['GeneratorHex'],Index=c.get('Index',0),Request=c['Request'],Profiles=c['Profiles'],Cache=c.get('Cache',53),Query=c.get('CircleQuery',53),Combine=c.get('BoundaryCombine',c.get('Combine',53))))
def context(c):
 sl=s.expected(dict(Name=c['Name'],GeneratorHex=c['GeneratorHex'],Index=c.get('Index',0),Policy=0))['Snapshot'];g=bytes.fromhex(c['GeneratorHex']);meta=s.t.metadata(g);group=next(x['Section'] for x in meta['Groups'] if x['Role']=='FractalMap');entries=s.f.inspect(g[group['Offset']:group['Offset']+group['EncodedBytes']],s.f.LIMITS)['Entries'];fw=next(e['Words'] for e in entries if e['Id']==sl['Filter']['FamilyId']);w=sl['Filter']['Words']
 return sl,fw,dict(Family=w[0],Selector=w[1],Amount=w[2],Lower=w[3],Upper=w[4],Scale=w[5])
def query(c,snap,value,premature=False):
 temp=dict(Filter=snap,FilterQuery=c.get('FilterQuery',53),Combine=c.get('Combine',53));result,_,scaled=r.filter_value(temp,value,premature)
 q=dict(Version='0002',Lower=snap['Lower'],Upper=snap['Upper'],Amount=snap['Amount'],Height=scaled,Precision=c.get('FilterQuery',53),Limits=r.sel.ff.fh.QUERY,Action='query',Selector=snap['Selector'],Enabled=1)
 u=r.sel.ff.old.query_number(q)
 if isinstance(u,dict):raise ValueError('Filter query')
 return result,scaled,u

def expected(c):
 fail=dict(Name=c['Name'],Reject='Input')
 try:
  if c.get('Cancel') or c.get('ActiveCancel') or c.get('MismatchSlice') or c.get('MismatchGrid'):return fail
  controls={k:c.get(k,53) for k in ('Table','Construction','SamplerQuery','FilterQuery','Combine','Product')}
  if any(p not in (24,53,64) for p in controls.values()) or c.get('BoundaryCombine',controls['Combine'])!=controls['Combine']:return fail
  limits={**DEFAULT,**{k:c[k] for k in DEFAULT if k in c}}
  if any(not 0<=v<=HARD[k] for k,v in limits.items()):return fail
  be=boundary(c)
  if be['Reject']!='None':return fail
  bs=be['Snapshot'];n=bs['QueryCount'];axis=bs['AxisCount'];res=reserve(n)
  if axis>32 or any(res[k]>limits[k] for k in res) or len(c['Incoming'])!=n or not all(s.finite(v) for v in c['Incoming']):return fail
  sl,fw,snap=context(c);ff=sl['Filter']
  if not ff['IsEnabled'] or not ff['HasOperationalInputs']:return fail
  admitted=sum(x['Admitted'] for x in bs['Cells']);model=None
  if admitted:model=r.f.route.mode.Model(dict(Hex=r.f.route.b.frame(fw).hex(),Width=axis,Rows=axis,TablePrecision=controls['Table'],ConstructionPrecision=controls['Construction']))
  cells=[];witnesses=0
  for i,(bc,incoming) in enumerate(zip(bs['Cells'],c['Incoming'])):
   cmp=flt=carrier=None;receipt=copy.deepcopy(bc['Probe']);local=bc['BoundaryBits'];visits=[]
   if bc['Admitted']:
    pair=bc['Coordinates'];value=model.miss(dict(XBits=pair['FirstBits'],ZBits=pair['SecondBits']),controls['SamplerQuery']);qr,scaled,u=query(c,snap,value);_,spilled,_=query(c,snap,value,True);witnesses+=scaled!=spilled
    cmp=dict(Origin=0,Retained=r.n.fraction_text(value),Projection=r.f.route.b.stored(value),Provenance=1)
    flt=dict(SuppliedSampleBits=cmp['Projection'],ScaledSampleBits=scaled,RuntimeAmountBits=qr['Amount'],QueryProjectionBits=qr['Query'],FeatherSpanProjectionBits=qr['Span'],Branch=qr['Branch'],Precision=controls['FilterQuery'])
    carrier=dict(Source=5,Precision=controls['FilterQuery'],ProjectionBits=l.store(u));shaped=l.shape(u,snap['Selector'],controls['Combine']);old=local;candidate=l.store(shaped);replaced=shaped[0]<l.a.scalar(old)[0]
    if replaced:local=candidate
    visits=[dict(Index=0,Enabled=True,SelectorBits=snap['Selector'],QueryProjectionBits=l.store(u),CandidateBits=candidate,OldAccumulatorBits=old,SelectedBits=local,Replaced=replaced,EarlyExit=l.a.scalar(local)[0]==0)]
    if sl['FilterInverted']:local=l.store(l.sub(l.one,l.a.scalar(local),controls['Combine']))
   product=l.store(l.mul(l.a.scalar(local),l.a.scalar(incoming),controls['Product']));positive=bc['Admitted'] and l.a.scalar(local)[0]>0
   receipt.update(LocalInfluenceBits=local,ProductBits=product,FilterEarlyExit=bool(visits and visits[0]['EarlyExit']),LocalPositive=positive,AffectorEligible=positive,ProductPositive=l.a.scalar(product)[0]>0,FilterVisits=visits)
   cells.append(dict(Index=i,Coordinates=bc['Coordinates'],BoundaryBits=bc['BoundaryBits'],Admitted=bc['Admitted'],IncomingWeightBits=incoming,Computed=cmp,Filter=flt,Carrier=carrier,Influence=receipt,Identity=True))
  o=fw[5];sb=9648+16*n;arrays=512*n;copies=28*n+40*admitted;accounted=arrays+(sb+16*n+admitted*(160+80*o) if admitted else 0);work=96*n+(20000+16*o+n+admitted*(64+512*o) if admitted else 0)
  out=dict(GeneratorSha256=sl['GeneratorSha256'],LayerIndex=sl['LayerIndex'],LayerSha256=sl['LayerSha256'],FilterSource=sl['Children'][1],FamilyId=ff['FamilyId'],FamilyGeneratorOffset=ff['FamilyGeneratorOffset'],GroupSha256=ff['GroupSha256'],Controls=dict(TablePrecision=controls['Table'],ConstructionPrecision=controls['Construction'],SamplerQueryPrecision=controls['SamplerQuery'],FilterQueryPrecision=controls['FilterQuery'],CombinePrecision=controls['Combine'],ProductPrecision=controls['Product']),Incoming=c['Incoming'],Counts=dict(BoundaryProbesRetained=n,Recombinations=n,SamplerConstructions=int(admitted>0),QueryBatches=int(admitted>0),ComputedCells=admitted,FilterQueries=admitted,FilterCarriers=admitted,SamplerDisposals=int(admitted>0),KernelOctaveQueries=admitted*o),ArrayBytes=arrays,CopiedArrayBytes=copies,CopiedInputBytes=4*n,CopiedEncodedInputBytes=0,ReservedCopyBytes=res['MaximumCopyBytes'],ReservedRouteBytes=res['MaximumRouteBytes'],AccountedRouteBytes=accounted,ReservedWork=res['MaximumWork'],AccountedWork=work,ReservedSamplerBytes=sb,ReservedSamplerWork=20256,ReservedQueryBytes=res['MaximumQueryBytes'],ReservedQueryWork=res['MaximumQueryWork'],Cells=cells)
  return dict(Name=c['Name'],Reject='None',Snapshot=out),witnesses
 except (ValueError,AssertionError,IndexError,ZeroDivisionError,OverflowError,KeyError):return fail

def generate(exe,packages,root,cfg,out):
 assert s.sha(exe.read_bytes())==s.t.EXE;sys.path.insert(0,str(packages));r.n.setup();r.f.route.lead.n.setup()
 static=dict(Circle=s.b.static(exe),Adta=s.ad.static(exe,packages),Filter=s.ff.static(exe,packages));cases=[]
 def gen(csel=0,inv=0,fi=0,fw=None,filterwords=None,circlewords=None,disabled=False,extra=False):
  circle=s.rule(b'BCIR',b'0002',circlewords or [fb(1),fb(1),fb(1.8),csel,fb(.5)]);fwords=filterwords or [0xfffffff9,3,fb(1),fb(0),fb(2),fb(1.12345)]
  words=list(fw or s.FAMILY);parts=circle+s.filter(words=fwords,enabled=0 if disabled else 1)+s.height(words=[0xffffffff,1,fb(15)] if extra else None);entries=[(-7,words)]
  if extra:hw=list(s.FAMILY);hw[1]=1;entries.append((-1,hw))
  return s.generator(parts,adta=s.t.words([inv,fi,0xffffffff])+b'synthetic\0',entries=entries)
 base=gen()
 def add(name,g=None,**kw):
  request=kw.pop('Request',b.DEFAULT_REQUEST.copy());n=(2*(request['Tiles']+request['Margin']))**2;weights=kw.pop('Incoming',[0,0x80000000,fb(-1),fb(.25),fb(1),fb(2),fb(.75),fb(-.5)]*(n//8)+[fb(1)]*(n%8));cases.append(dict(Name=name,GeneratorHex=(g or base).hex(),Request=request,Profiles=kw.pop('Profiles',[53]*4),Incoming=weights,**kw))
 add('mixed-first');add('original-layer3',s.original(root,cfg),Index=3)
 add('all-admitted',gen(csel=0xffffffff,inv=1));add('zero-admitted',gen(csel=0xffffffff));add('unsupported-height-allowed',gen(extra=True))
 for mode in range(6):
  for p in (24,53,64):
   fw=list(s.FAMILY);fw[12]=mode;add(f'mode-{mode}-{p}',gen(fw=fw),Table=p,Construction=p,SamplerQuery=p,FilterQuery=p,Combine=p,Product=p)
 for cp,qp in __import__('itertools').product((24,53,64),repeat=2):add(f'independent-profiles-{cp}-{qp}',FilterQuery=qp,Combine=cp,Product=qp,SamplerQuery=64,Table=24,Construction=53)
 for sel in (0,1,2,3,0xffffffff):
  for inv,fi in ((0,0),(1,0),(0,1),(1,0x80000000)):
   add(f'selectors-inversion-{sel}-{inv}-{fi}',gen(csel=sel,inv=inv,fi=fi,filterwords=[0xfffffff9,sel,fb(.7),0,fb(2),fb(1.12345)]),Combine=64,FilterQuery=64,SamplerQuery=64)
 for w in ([0]*16,[0x80000000,fb(-1)]*8):add('incoming-'+str(len(cases)),Incoming=w)
 for label,fwds in [('reversed',[0xfffffff9,0,fb(1),fb(2),0,fb(1)]),('equal',[0xfffffff9,0,fb(1),fb(1),fb(1),fb(1)]),('negative-amount',[0xfffffff9,2,fb(-1),0,fb(2),fb(1)]),('large-amount',[0xfffffff9,1,fb(2),0,fb(2),fb(1)])]:add(label,gen(filterwords=fwds))
 add('disabled-filter',gen(disabled=True));add('zero-blocked-disabled-filter',gen(csel=0xffffffff,disabled=True))
 add('nonfinite-filter',gen(filterwords=[0xfffffff9,0,0,0x7fc12345,fb(2),fb(1)]));add('nonfinite-incoming',Incoming=[0]*15+[0x7fc12345]);add('short-incoming',Incoming=[0]*15)
 for at,value in ((1,1),(3,1),(5,0),(12,6),(0,0x7fffffff),(8,0x7fc12345)):
  fw=list(s.FAMILY);fw[at]=value;add('unsupported-filter-family-'+str(at),gen(fw=fw))
 fw=list(s.FAMILY);fw[5]=2;fw[7]=fb(-1);add('normalization-failure',gen(fw=fw));add('blocked-normalization-not-constructed',gen(csel=0xffffffff,fw=fw))
 fw=list(s.FAMILY);fw[8]=0x7f7fffff;add('late-filter-overflow',gen(fw=fw))
 for key in ('MismatchSlice','MismatchGrid','Cancel'):add(key.lower() if key!='Cancel' else 'cancel',**{key:True})
 add('changed-first-gate-profile',BoundaryCombine=24,Combine=53)
 for key in ('Table','Construction','SamplerQuery','FilterQuery','Combine','Product'):add('invalid-profile-'+key,**{key:0})
 exact=reserve(16);add('exact-reservations',**exact)
 for k,v in exact.items():add('one-less-'+k,**{**exact,k:v-1})
 for k,v in HARD.items():add('above-hard-'+k,**{k:v+1});add('negative-cap-'+k,**{k:-1})
 add('precopy-large-grid',Request={**b.DEFAULT_REQUEST,'Tiles':16},MaximumArrayBytes=0)
 for p in (24,53,64):add('retained-profile-witness-'+str(p),gen(filterwords=[0xfffffff9,3,fb(1),0,fb(2),fb(7.12345)]),Request={**b.DEFAULT_REQUEST,'ChunkWidthBits':fb(.4938268)},SamplerQuery=p,FilterQuery=p,Combine=p,Product=p,Profiles=[p]*4)
 for tp,cp,qp in __import__('itertools').product((24,53,64),repeat=3):add(f'sampler-controls-{tp}-{cp}-{qp}',gen(filterwords=[0xfffffff9,3,fb(1),0,fb(2),fb(7.12345)]),Request={**b.DEFAULT_REQUEST,'ChunkWidthBits':fb(.4938268)},Table=tp,Construction=cp,SamplerQuery=qp,FilterQuery=64,Combine=64,Product=53)
 add('all-hard-options',**HARD)
 add('active-cancel',Request={**b.DEFAULT_REQUEST,'Tiles':16},ActiveCancel=True)
 results=[];witnesses=0
 for c in cases:
  value=expected(c)
  if isinstance(value,tuple):value,w=value;witnesses+=w
  results.append(value)
 out.joinpath('generation-diagnostic.json').write_text(json.dumps(dict(First=results[0]['Reject'],Original=results[1]['Reject'],Witnesses=witnesses,Rejected=[x['Name'] for x in results if x['Reject']!='None']),indent=2))
 assert results[0]['Reject']==results[1]['Reject']=='None' and witnesses>0
 assert next(x for x in results if x['Name']=='late-filter-overflow')['Reject']=='Input'
 assert next(x for x in results if x['Name']=='zero-admitted')['Snapshot']['Counts']['SamplerConstructions']==0
 assert next(x for x in results if x['Name']=='unsupported-height-allowed')['Reject']=='None'
 assert next(x for x in results if x['Name']=='all-admitted')['Snapshot']['Counts']['ComputedCells']==16
 assert 0<results[0]['Snapshot']['Counts']['ComputedCells']<16
 profiles=[next(x for x in results if x['Name']=='retained-profile-witness-'+str(p))['Snapshot']['Cells'] for p in (24,53,64)];profile_variants=len({json.dumps(x,sort_keys=True) for x in profiles});assert profile_variants>1
 out.joinpath('fixtures.json').write_text(json.dumps(cases));out.joinpath('expected.json').write_text(json.dumps(results));receipt=dict(Cases=len(cases),Rejected=sum(x['Reject']!='None' for x in results),RetainedStoreWitnesses=witnesses,ProfileVariants=profile_variants,OriginalLayersInspected=1,OriginalHeightSamples=0,Static=static)
 out.joinpath('producer-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
def verify(out):
 actual=json.loads(out.joinpath('native.json').read_text());expected=json.loads(out.joinpath('expected.json').read_text());assert len(actual['Cases'])==len(expected)
 for a,e in zip(actual['Cases'],expected):
  if a!=e:out.joinpath('first-mismatch.json').write_text(json.dumps(dict(Actual=a,Expected=e),indent=2));raise AssertionError(e['Name'])
 assert actual['OwnershipChecks']==17
 receipt=json.loads(out.joinpath('producer-receipt.json').read_text());receipt.update(Passed=True,OwnershipChecks=17);receipt['Static']['Layer']=json.loads(out.joinpath('static-layer.json').read_text());out.joinpath('comparison-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*(Path(x) for x in sys.argv[2:]))
 elif sys.argv[1]=='verify':verify(Path(sys.argv[2]))
 else:raise ValueError('Operation')
