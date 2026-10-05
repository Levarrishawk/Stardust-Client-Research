"""Separate Decimal curve tolerance and exact Fraction AHFR consuming the actual exported binary64 dyadic."""
import json,sys,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
import importlib.util
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);sys.modules[name]=m;spec.loader.exec_module(m);return m
f=load('consumer_filter57','poc/round57-tests/Assets/peer.py');curve=load('consumer_curve57','poc/round57-tests/Rendering/verify_curve.py');s=f.s;l=f.l;fb=f.fb;mode=f.r.f.route.mode
sparse=load('height_sparse58','poc/round58-tests/Rendering/verify_sparse.py')
DEFAULT=dict(MaximumAxisCount=32,MaximumCells=1024,MaximumRequests=1024,MaximumAggregateCopyBytes=4194304,MaximumOwnedArrayBytes=1048576,MaximumAggregateRouteBytes=16777216,MaximumAggregateWork=67108864,MaximumAbsoluteHeightBits=fb(1000000))
HARD={**DEFAULT,'MaximumOwnedArrayBytes':8388608}
def reservation(n,m):
 r=dict(OwnCopyBytes=84+16*m+4*n,OwnEncodedCopyBytes=84,OwnRequestBytes=16*m,OwnPriorBytes=4*n,OwnArrayBytes=340+16*m+4*n,OwnWork=84+16*n+16*m,BatchCopyBytes=696+176*m,BatchRouteBytes=10088+32*n+1600*m if m else 440,BatchWork=20424+n+8288*m if m else 168,BatchQueryBytes=16*n+1440*m if m else 0,BatchQueryWork=n+8256*m if m else 0,ConsumerCopyBytes=4*n,ConsumerArrayBytes=256+192*n,ConsumerMappingBytes=4*n,ConsumerWork=84+32*n+16*m)
 r.update(AggregateCopyBytes=r['OwnCopyBytes']+r['BatchCopyBytes']+r['ConsumerCopyBytes'],OwnedArrayBytes=r['OwnArrayBytes']+r['BatchRouteBytes']+r['ConsumerArrayBytes'],AggregateRouteBytes=r['OwnArrayBytes']+r['BatchRouteBytes']+r['ConsumerArrayBytes']+r['ConsumerMappingBytes'],AggregateWork=r['OwnWork']+r['BatchWork']+r['ConsumerWork'])
 return r
FILTER_KEYS=('Name','GeneratorHex','Index','Request','Profiles','Incoming','Cache','CircleQuery','Table','Construction','SamplerQuery','FilterQuery','Combine','Product')
def filtered(c):
 e=f.expected({k:c[k] for k in FILTER_KEYS if k in c});return e[0] if isinstance(e,tuple) else e

def prepare(c,full=False):
 fe=filtered(c)
 if fe['Reject']!='None':raise ValueError('Filter preparation')
 sl=s.expected(dict(Name=c['Name'],GeneratorHex=c['GeneratorHex'],Index=c.get('Index',0),Policy=0))['Snapshot'];g=bytes.fromhex(c['GeneratorHex']);meta=s.t.metadata(g);group=next(x['Section'] for x in meta['Groups'] if x['Role']=='FractalMap');entries=s.f.inspect(g[group['Offset']:group['Offset']+group['EncodedBytes']],s.f.LIMITS)['Entries'];hw=next(e for e in entries if e['Id']==sl['Height']['FamilyId']);raw=c.get('CurveHex',hw['RawHex']);axis=fe['Snapshot']['Counts']['Recombinations'];axis=int(axis**.5);width,rows=(axis*2,axis//2) if c.get('WrongShape') else (axis,axis)
 indices=list(range(axis*axis)) if full else c.get('RequestIndices',[i for i,cell in enumerate(fe['Snapshot']['Cells']) if l.a.scalar(cell['Influence']['ProductBits'])[0]>0]);requests=[dict(Column=i%width,Row=i//width,XBits=fe['Snapshot']['Cells'][i]['Coordinates']['FirstBits'],ZBits=fe['Snapshot']['Cells'][i]['Coordinates']['SecondBits']) for i in indices]
 if c.get('WrongCoordinates') and requests:requests[-1]['XBits']=fb(.125)
 ce=sparse.expected(dict(Name=c['Name'],Width=width,Rows=rows,Hex=raw,Requests=requests,Table=c.get('CurveTable',53),Construction=c.get('CurveConstruction',53),Query=c.get('CurveQuery',53),MaximumRequests=1024),mode,curve)
 if full:ce=curve.expected(dict(Name=c['Name'],Width=width,Rows=rows,Hex=raw,Requests=requests,Table=c.get('CurveTable',53),Construction=c.get('CurveConstruction',53),Query=c.get('CurveQuery',53),MaximumSamples=1024),mode)
 if ce['Reject']!='None':raise ValueError('Curve preparation '+ce.get('Reason',''))
 return fe['Snapshot'],sl,ce

def height(prior,product,value,words,p):
 if not s.finite(prior) or not s.finite(product):raise ValueError('Finite height operands')
 if l.a.scalar(product)[0]<=0:return dict(InputHeightBits=prior,InfluenceBits=product,SuppliedFractalBits=0,FamilyKeyBits=words[0],ScaleBits=words[2],OperationBits=words[1],ResultHeightBits=prior,Outcome=2,Precision=p)
 h=l.a.scalar(prior);w=l.a.scalar(product);t=l.mul((value,False),l.a.scalar(words[2]),p);op=words[1]
 if op==0:result=l.add(h,l.mul(l.sub(t,h,p),w,p),p)
 elif op==1:result=l.add(h,l.mul(t,w,p),p)
 elif op==2:result=l.sub(h,l.mul(t,w,p),p)
 elif op==3:result=l.add(h,l.mul(l.sub(l.mul(t,h,p),h,p),w,p),p)
 else:raise ValueError('Strict height op')
 return dict(InputHeightBits=prior,InfluenceBits=product,SuppliedFractalBits=l.store((value,False)),FamilyKeyBits=words[0],ScaleBits=words[2],OperationBits=op,ResultHeightBits=l.store(result),Outcome=0,Precision=p)

def expected(c):
 stage='Preparation';fail=lambda:dict(Name=c['Name'],Reject='Input',Stage=stage)
 try:
  fresult=filtered(c)
  if fresult['Reject']!='None':return fail()
  fs=fresult['Snapshot'];stage='Evaluation';n=len(fs['Cells']);axis=int(n**.5);m=sum(l.a.scalar(x['Influence']['ProductBits'])[0]>0 for x in fs['Cells']);lim={**DEFAULT,**{k:c[k] for k in DEFAULT if k in c}};r=reservation(n,m)
  if c.get('Cancel') or c.get('NullControls') or c.get('MismatchSlice') or c.get('MismatchGrid') or c.get('Policy',0)!=0 or c.get('HeightPrecision',53) not in (24,53,64):return fail()
  if any(c.get(k,53) not in (24,53,64) for k in ('CurveTable','CurveConstruction','CurveQuery')):return fail()
  for k,v in lim.items():
   if k!='MaximumAbsoluteHeightBits' and not 0<=v<=HARD[k]:return fail()
  bound=l.a.scalar(lim['MaximumAbsoluteHeightBits'])[0]
  if not 0<=bound<=1000000:return fail()
  if not 2<=axis<=32 or axis>lim['MaximumAxisCount'] or n>lim['MaximumCells'] or m>lim['MaximumRequests']:return fail()
  if any(r[key]>lim['Maximum'+key] for key in ('AggregateCopyBytes','OwnedArrayBytes','AggregateRouteBytes','AggregateWork')):return fail()
  if len(c['Priors'])!=n or not all(s.finite(x) and abs(l.a.scalar(x)[0])<=bound for x in c['Priors']):return fail()
  sl=s.expected(dict(Name=c['Name'],GeneratorHex=c['GeneratorHex'],Index=c.get('Index',0),Policy=0))['Snapshot'];hs=sl['Height'];words=hs['Words']
  if not hs['IsEnabled'] or words[1]>3 or not s.finite(words[2]):return fail()
  g=bytes.fromhex(c['GeneratorHex']);meta=s.t.metadata(g);group=next(x['Section'] for x in meta['Groups'] if x['Role']=='FractalMap');entries=s.f.inspect(g[group['Offset']:group['Offset']+group['EncodedBytes']],s.f.LIMITS)['Entries'];raw=bytes.fromhex(next(x['RawHex'] for x in entries if x['Id']==hs['FamilyId']));raw_words=struct.unpack_from('<13I',raw,32)
  if m and (raw_words[0]==0 or raw_words[7]==fb(-1) and raw_words[5]%2==0):return fail()
  fs,sl,ce=prepare(c)
  if ce['OriginalSha256']!=hs['FamilyRawSha']:return fail()
  byindex={cc['Index']:cc for cc in ce['Cells']}
  assert set(byindex)=={i for i,x in enumerate(fs['Cells']) if l.a.scalar(x['Influence']['ProductBits'])[0]>0}
  for i,(prior,cell) in enumerate(zip(c['Priors'],fs['Cells'])):
   cc=byindex.get(i);value=curve.Fraction(0) if cc is None else curve.f64(struct.unpack('<Q',struct.pack('<d',float(curve.Decimal(cc['ExpectedOutput']))))[0])
   result=height(prior,cell['Influence']['ProductBits'],value,words,c.get('HeightPrecision',53))
   if abs(l.a.scalar(result['ResultHeightBits'])[0])>bound:return fail()
  return dict(Name=c['Name'],Reject='None',Filter=fs,Slice=sl,Curve=ce,Priors=c['Priors'],Precision=c.get('HeightPrecision',53),MaximumAbsoluteHeightBits=lim['MaximumAbsoluteHeightBits'],Reservation=r)
 except (ValueError,AssertionError,IndexError,ZeroDivisionError,OverflowError,KeyError,StopIteration):return fail()

def generate(exe,packages,root,cfg,out):
 assert s.sha(exe.read_bytes())==s.t.EXE;sys.path.insert(0,str(packages));f.r.n.setup();f.r.f.route.lead.n.setup()
 static=dict(Height=s.ah.static(exe,str(packages)),Adta=s.ad.static(exe,packages));cases=[]
 def gen(op=1,scale=15,inv=0,fi=0,first=1,param=.7,second=0,enabled=1,raw=None,allcells=False):
  hw=list(raw or s.FAMILY);hw[1]=first;hw[2]=fb(param);hw[3]=second
  bc=[fb(1),fb(1),fb(1.8),0xffffffff if allcells else 0,fb(.5)];fw=[0xfffffff9,0xffffffff if allcells else 3,fb(1),0,fb(2),fb(1.12345)]
  return s.generator(s.rule(b'BCIR',b'0002',bc)+s.filter(words=fw)+s.height(words=[0xffffffff,op,fb(scale)],enabled=enabled),adta=s.t.words([1 if allcells else inv,1 if allcells else fi,0])+b'synthetic\0',entries=[(-7,s.FAMILY),(-1,hw)])
 base=gen()
 def add(name,g=None,**kw):
  request=kw.pop('Request',{**f.b.DEFAULT_REQUEST,'ChunkWidthBits':fb(.4938268)});n=(2*(request['Tiles']+request['Margin']))**2;incoming=kw.pop('Incoming',([0,0x80000000,fb(-1),fb(.25),fb(1),fb(2),fb(.75),fb(-.5)]*((n+7)//8))[:n]);priors=kw.pop('Priors',([0,0x80000000,fb(-1),fb(.25),fb(1),fb(2),fb(-.25),fb(.5)]*((n+7)//8))[:n]);cases.append(dict(Name=name,GeneratorHex=(g or base).hex(),Request=request,Profiles=kw.pop('Profiles',[53]*4),Incoming=incoming,Priors=priors,**kw))
 add('mixed-first');add('original-layer3',s.original(root,cfg),Index=3)
 for op in range(4):
  for p in (24,53,64):
   for inv,fi in ((0,0),(1,0),(0,1),(1,1)):add(f'op-profile-inversion-{op}-{p}-{inv}-{fi}',gen(op=op,inv=inv,fi=fi),HeightPrecision=p,Combine=p,FilterQuery=p,Product=p)
 for tp,cp,qp in __import__('itertools').product((24,53,64),repeat=3):add(f'curve-profiles-{tp}-{cp}-{qp}',CurveTable=tp,CurveConstruction=cp,CurveQuery=qp,HeightPrecision=64)
 for modebits in range(6):
  w=list(s.FAMILY);w[12]=modebits;add('curve-mode-'+str(modebits),gen(raw=w))
 add('all-active',gen(allcells=True),Incoming=[fb(1)]*16);add('all-skipped',Incoming=[0,0x80000000,fb(-1),fb(-2)]*4)
 add('signed-zero-priors-skipped',Incoming=[0]*16,Priors=[0,0x80000000]*8)
 add('negative-height-scale',gen(scale=-15));add('zero-height-scale',gen(scale=0))
 w=list(s.FAMILY);w[4]=0x7fc12345;add('unused-second-parameter',gen(raw=w))
 for flag in (0x100,0x80000000):add('nonzero-raw-first-flag-'+str(flag),gen(first=flag))
 add('unknown-operation',gen(op=0xffffffff));add('disabled-height',gen(enabled=0));add('invalid-operation',gen(op=4));add('nonfinite-height-scale',gen(scale=float('inf')))
 for first,param,second in ((0,.7,0),(1,0,0),(1,1,0),(1,.7,1),(1,-.5,0),(1,float('nan'),0)):add('curve-domain-'+str(len(cases)),gen(first=first,param=param,second=second))
 for at,value in ((0,0x7fffffff),(5,0),(12,6),(8,0x7f800000)):
  w=list(s.FAMILY);w[at]=value;add('raw-domain-'+str(at),gen(raw=w))
 for key in ('MismatchSlice','MismatchGrid','Cancel'):add(key.lower() if key!='Cancel' else 'cancel',**{key:True})
 add('invalid-policy',Policy=1);add('invalid-height-precision',HeightPrecision=0);add('nonfinite-prior',Priors=[0]*15+[0x7fc12345]);add('short-prior',Priors=[0]*15)
 add('late-height-limit',gen(allcells=True),Incoming=[fb(1)]*16,Priors=[0]*15+[fb(1000000)])
 add('exact-height-limit-skips',Incoming=[0]*16,Priors=[fb(.25),fb(-.25)]*8,MaximumAbsoluteHeightBits=fb(.25));add('one-less-height-limit',Incoming=[0]*16,Priors=[fb(.25)]*16,MaximumAbsoluteHeightBits=fb(.25)-1)
 add('zero-height-limit-skips',Incoming=[0]*16,Priors=[0,0x80000000]*8,MaximumAbsoluteHeightBits=0)
 positive_count=sum(l.a.scalar(x['Influence']['ProductBits'])[0]>0 for x in filtered(cases[0])['Snapshot']['Cells']);rr=reservation(16,positive_count);exact=dict(MaximumAxisCount=4,MaximumCells=16,MaximumRequests=positive_count,**{'Maximum'+k:rr[k] for k in ('AggregateCopyBytes','OwnedArrayBytes','AggregateRouteBytes','AggregateWork')});add('exact-reservations',**exact)
 for k,v in exact.items():add('one-less-'+k,**{**exact,k:v-1})
 for k,v in HARD.items():
  if k=='MaximumAbsoluteHeightBits':continue
  add('above-hard-'+k,**{k:v+1});add('negative-cap-'+k,**{k:-1})
 for bound in (fb(-1),0x7fc12345,fb(1000001)):add('invalid-height-bound-'+str(bound),MaximumAbsoluteHeightBits=bound)
 add('precopy-zero-cap',MaximumAggregateCopyBytes=0)
 add('tiny-parameter-modern-route',gen(param=struct.unpack('<f',struct.pack('<I',1))[0]))
 raw=list(s.FAMILY);raw[12]=3;add('tiny-parameter-export-boundary',gen(param=struct.unpack('<f',struct.pack('<I',1))[0],raw=raw))
 add('true-modern-underflow-route',gen(param=struct.unpack('<f',struct.pack('<I',1))[0],raw=raw),Request={**f.b.DEFAULT_REQUEST,'ChunkWidthBits':fb(.00004)})
 add('late-height-overflow',gen(scale=struct.unpack('<f',struct.pack('<I',0x7f7fffff))[0],allcells=True),Incoming=[0]*15+[fb(2)],Priors=[0]*16)
 add('tiny-dyadic-large-scale',gen(scale=struct.unpack('<f',struct.pack('<I',0x7f7fffff))[0],param=struct.unpack('<f',struct.pack('<I',1))[0],allcells=True),Request={**f.b.DEFAULT_REQUEST,'ChunkWidthBits':fb(.00004)},Incoming=[0]*15+[fb(1)],Priors=[0]*16,HeightPrecision=64)
 for at,v in ((0,0),(7,fb(-1))):
  w=list(s.FAMILY);w[5]=2;w[at]=v
  add('empty-runtime-unevaluated-'+str(at),gen(raw=w),Incoming=[0]*16,Priors=[0,0x80000000]*8)
  add('known-nonempty-ineligible-'+str(at),gen(raw=w,allcells=True),Incoming=[fb(1)]*16)
 emptyr=reservation(16,0);emptycaps=dict(MaximumAxisCount=4,MaximumCells=16,MaximumRequests=0,**{'Maximum'+k:emptyr[k] for k in ('AggregateCopyBytes','OwnedArrayBytes','AggregateRouteBytes','AggregateWork')})
 add('empty-exact-reservations',Incoming=[0]*16,Priors=[0,0x80000000]*8,**emptycaps)
 for k in ('MaximumAggregateCopyBytes','MaximumOwnedArrayBytes','MaximumAggregateRouteBytes','MaximumAggregateWork'):add('empty-one-less-'+k,Incoming=[0]*16,Priors=[0,0x80000000]*8,**{**emptycaps,k:emptycaps[k]-1})
 add('null-controls',NullControls=True)
 for k in ('CurveTable','CurveConstruction','CurveQuery'):add('invalid-'+k,**{k:0})
 add('nonfinite-skipped-prior',Incoming=[0]*16,Priors=[0]*15+[0x7fc12345])
 w=list(s.FAMILY);w[12]=3;add('late-curve-export-failure',gen(param=struct.unpack('<f',struct.pack('<I',1))[0],raw=w,allcells=True),Incoming=[fb(1)]*16)
 results=[expected(c) for c in cases];assert results[0]['Reject']==results[1]['Reject']=='None';assert next(x for x in results if x['Name']=='late-height-limit')['Stage']=='Evaluation'
 original=results[1];assert original['Slice']['Height']['RuleSha256']=='B85D90EC684322F07798476A55A665DCDE050D5B355D86CB3AD5ACB41883F674';assert original['Curve']['OriginalSha256']=='77FDAC709B0D3E3113FAAD8A82F4746FDF3969434DC8BF2BAF42273AE89D5FE3'
 out.joinpath('fixtures.json').write_text(json.dumps(cases));out.joinpath('expected.json').write_text(json.dumps(results));receipt=dict(Cases=len(cases),Rejected=sum(x['Reject']!='None' for x in results),OriginalLayersInspected=1,OriginalNumericParity=False,Static=static)
 out.joinpath('producer-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))

def verify(out):
 actual=json.loads(out.joinpath('native.json').read_text());want=json.loads(out.joinpath('expected.json').read_text());cases=json.loads(out.joinpath('fixtures.json').read_text());assert len(actual['Cases'])==len(want)
 outputs=applied=skipped=witnesses=equivalent=curve_equivalent=empties=underflows=owned_factories=0
 for got,e,c in zip(actual['Cases'],want,cases):
  assert got['Name']==e['Name'] and got['Reject']==e['Reject'],(e['Name'],got['Reject'],e['Reject'])
  if e['Reject']!='None':assert got['Stage']==e['Stage'],(e['Name'],got['Stage'],e['Stage']);continue
  outer=got['Snapshot'];x=outer['Consumer'];sl=e['Slice'];ce=e['Curve'];fs=e['Filter'];n=len(fs['Cells']);words=sl['Height']['Words'];m=len(ce['Cells']);byindex={q['Index']:q for q in ce['Cells']}
  expectedmeta=dict(GeneratorSha256=sl['GeneratorSha256'],LayerIndex=sl['LayerIndex'],LayerSha256=sl['LayerSha256'],HeightSource=sl['Children'][2],FamilyId=sl['Height']['FamilyId'],FamilyGeneratorOffset=sl['Height']['FamilyGeneratorOffset'],GroupSha256=sl['Height']['GroupSha256'],FamilyRawSha256=sl['Height']['FamilyRawSha'],BatchOriginalSha256=ce['OriginalSha256'],Policy=0,OriginalNumericParity=False,Precision=e['Precision'],Priors=e['Priors'],Counts=dict(BorrowedCurveQueries=m,ConsumerCalls=n,DyadicConversions=m,HeightApplications=m,SkippedCells=n-m,OwnedSamplerConstructions=0,OwnedSamplerQueryBatches=0,OwnedSamplerDisposals=0),CopiedPriorBytes=4*n,CopiedEncodedInputBytes=0,ArrayBytes=256+192*n,MappingBytes=4*n,Work=84+32*n+16*m,MaximumAbsoluteHeightBits=e['MaximumAbsoluteHeightBits'],BorrowedSamplerFactories=int(m>0),BorrowedKernelBatches=int(m>0),BorrowedSamplerDisposals=int(m>0),BorrowedLogCalls=2*int(m>0),BorrowedPowerCalls=ce['Counts'][7])
  assert outer['BorrowedFilterCounts']==fs['Counts']
  assert outer['AccountedAggregateRouteBytes']==e['Reservation']['OwnArrayBytes']+ce['AccountedRouteBytes']+e['Reservation']['ConsumerArrayBytes']+e['Reservation']['ConsumerMappingBytes']
  assert outer['AccountedAggregateWork']==e['Reservation']['OwnWork']+ce['AccountedWork']+e['Reservation']['ConsumerWork']
  assert outer['ActualReferences'] and outer['Reservation']==e['Reservation'] and outer['PriorHeightBits']==e['Priors'] and outer['ConsumerCounts']==expectedmeta['Counts']
  assert outer['Policy']==0 and not outer['OriginalNumericParity'] and outer['HeightPrecision']==e['Precision']
  assert outer['CurveControls']==dict(TablePrecision=c.get('CurveTable',53),ConstructionPrecision=c.get('CurveConstruction',53),QueryPrecision=c.get('CurveQuery',53))
  assert [outer[k] for k in ('OwnedSamplerFactories','OwnedSamplerQueryBatches','OwnedSamplerDisposals')]==[int(m>0)]*3
  assert outer['Requests']==[dict(Column=q['Column'],Row=q['Row'],XBits=q['XBits'],ZBits=q['ZBits']) for q in ce['Cells']]
  for k,v in expectedmeta.items():assert x[k]==v,(e['Name'],k,x[k],v)
  assert len(x['Cells'])==n
  for i,(a,fc,prior) in enumerate(zip(x['Cells'],fs['Cells'],e['Priors'])):
   cc=byindex.get(i);product=fc['Influence']['ProductBits'];u=curve.Fraction(0)
   if cc is not None:
    q=a['Curve'];u=curve.f64(q['OutputBinary64Bits']);ex=curve.f64(q['ExponentBinary64Bits']);curve.close(u,cc['ExpectedOutput']);curve.close(ex,cc['ExpectedExponent']);assert q['BaseF32Bits']==cc['Base']==q['KernelProjection'];assert curve.Fraction(q['Kernel'])==curve.Fraction(cc['Kernel']);assert not q['OriginalNumericParity'] and q['Policy']==0
    if u==0:
     assert q['OutputBinary64Bits']==0
     if q['BaseF32Bits']&0x7fffffff not in (0,fb(1)):assert float(curve.Decimal(cc['ExpectedOutput']))==0;underflows+=1
    mag,power=curve.dyadic(u);assert (q['Magnitude'],q['Exponent'])==(mag,power)
    assert a['Request']==dict(Column=cc['Column'],Row=cc['Row'],XBits=cc['XBits'],ZBits=cc['ZBits']);outputs+=1
   else:assert a['Curve'] is None and a['Request'] is None
   h=height(prior,product,u,words,e['Precision']);active=h['Outcome']==0
   assert a['Identity'] and a['Index']==i and a['PriorHeightBits']==prior and a['Product']==product and a['HeightBits']==h['ResultHeightBits'] and a['Applied']==active and a['Consumer']==h,(e['Name'],i,'Exact height arithmetic')
   if active:
    assert curve.Fraction(a['Supplied']['Fraction'])==u and a['Supplied']['ProjectionBits']==l.store((u,False)) and a['Supplied']['Provenance']==1;applied+=1
    early=height(prior,product,l.a.scalar(l.store((u,False)))[0],words,e['Precision']);witnesses+=early['ResultHeightBits']!=h['ResultHeightBits']
   else:assert a['Supplied'] is None and a['HeightBits']==prior;skipped+=1
  assert got['ExplicitRouteEquivalent']
  if got['ExplicitRouteEquivalent']:equivalent+=n;curve_equivalent+=m
  if m==0:empties+=1
  else:owned_factories+=1
 assert actual['OwnershipChecks']==22 and applied>0 and skipped>0 and outputs==applied
 assert actual['ExplicitRouteEquivalentHeightCells']==equivalent and actual['ExplicitRouteEquivalentCurveOutputs']==curve_equivalent and actual['EmptyReceipts']==empties
 receipt=json.loads(out.joinpath('producer-receipt.json').read_text());receipt.update(Passed=True,OwnershipChecks=22,OwnedSamplerFactories=owned_factories,OwnedSamplerQueryBatches=owned_factories,OwnedSamplerDisposals=owned_factories,ConsumerCalls=outputs+skipped,BorrowedSparseQueries=outputs,ApproximateOutputs=outputs,ExactHeightApplications=applied,SkippedHeightCells=skipped,PrematureF32HeightWitnesses=witnesses,ExplicitRouteEquivalentHeightCells=equivalent,ExplicitRouteEquivalentCurveOutputs=curve_equivalent,EmptyReceipts=empties,ModernTrueUnderflows=underflows,DecimalPrecision=100,RelativeTolerance='1e-12',AbsoluteTolerance='1e-15',HeightOracle='Fraction using independently tolerance-checked actual binary64 export')
 out.joinpath('comparison-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*(Path(x) for x in sys.argv[2:]))
 elif sys.argv[1]=='verify':verify(Path(sys.argv[2]))
 else:raise ValueError('Operation')
