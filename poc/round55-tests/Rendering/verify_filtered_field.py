"""Independent Fraction filter-only composition; original-derived fixtures remain private."""
import argparse,importlib.util,json,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
field=load('frozen_field54','poc/round54-tests/Rendering/verify_field.py');sel=load('frozen_select54','poc/round54-tests/Assets/peer.py')
f=field.f;n=f.n;word=f.word;sha=field.sha

def reserve(c):
 n=c['Width']*c['Rows'];l=len(bytes.fromhex(c['RuleHex']));ff=c['FilterBytes'];a=field.account(c);shared=8*l+65536+9512+136+32*n;sw=l+20256+n
 return [a[0]+l+ff+8*n+4*(c['Width']+c['Rows'])+512*n+256,a[1]+shared+1760*n,a[2]+sw+8272*n+32*(c['Width']+c['Rows'])+ff,a[3],a[4]]
def filter_value(c,sample,premature=False):
 snap=c['Filter'];mul=n.arithmetic(c['FilterQuery'])[1];scaled=f.route.b.stored(mul(n.finite_scalar(f.route.b.stored(sample)) if premature else sample,n.finite_scalar(snap['Scale'])))
 q=dict(Version='0002',Lower=snap['Lower'],Upper=snap['Upper'],Amount=snap['Amount'],Height=scaled,Precision=c['FilterQuery'],Limits=sel.ff.fh.QUERY,Action='query',Selector=snap['Selector'],Enabled=1)
 result=sel.ff.fh.evaluate(q);u=sel.ff.old.query_number(q)
 if result['Reject']!='None' or isinstance(u,dict):raise ValueError()
 shaped=sel.ff.l.shape(u,snap['Selector'],c['Combine']);local=word(1)
 if shaped[0]<n.finite_scalar(local)[0]:local=sel.ff.l.store(shaped)
 return result,local,scaled

def expected(c):
 try:
  if c.get('Cancel') or c.get('ForeignFamilySet') or c.get('Policy',1)!=1:raise ValueError()
  se=sel.expected(dict(Name=c['Name'],GeneratorHex=c['GeneratorHex'],Index=c.get('Index',0),Policy=c.get('Policy',1)))
  if se['Reject']!='None':raise ValueError()
  for key in ('Table','Construction','Query','Consumer','FilterQuery','Combine','Product'):
   if c[key] not in (24,53,64):raise ValueError()
   n.arithmetic(c[key])
  ncell=c['Width']*c['Rows'];res=reserve(c)
  for k,default,maximum,v in zip(('MaximumCopyBytes','MaximumRouteBytes','MaximumWork','MaximumFilterQueryBytes','MaximumFilterQueryWork'),(4194304,134217728,67108864,2097152,16777216),(8388608,536870912,134217728,2097152,16777216),res):
   cap=c.get(k,default)
   if not 0<=cap<=maximum or v>cap:raise ValueError()
  if not 0<=c.get('MaximumSamples',256)<=1024:raise ValueError()
  if not 0<=c.get('HeightMaximumSamples',256)<=1024:raise ValueError()
  if not 24<=c.get('MaximumRuleBytes',65536)<=65536 or c['FilterBytes']>c.get('MaximumRuleBytes',65536):raise ValueError()
  # Frozen field model performs strict whole AHFR/family prevalidation without evaluating noise.
  clean=dict(c,Influence=[0]*ncell);clean.pop('MaximumCopyBytes',None);clean.pop('MaximumRouteBytes',None);clean.pop('MaximumWork',None)
  clean['MaximumSamples']=min(c.get('MaximumSamples',256),c.get('HeightMaximumSamples',256))
  clean['MaximumCopyBytes']=c.get('HeightMaximumCopyBytes',1048576);clean['MaximumRouteBytes']=c.get('HeightMaximumRouteBytes',134217728);clean['MaximumWork']=c.get('HeightMaximumWork',16777216)
  clean['MaximumQueryBytes']=c.get('HeightMaximumQueryBytes',2097152);clean['MaximumQueryWork']=c.get('HeightMaximumQueryWork',16777216)
  # Prior display failure belongs to final Field.Build, not the strict preparation prequery gate.
  pre=dict(clean,Prior=[0]*ncell)
  if field.expected(pre)['Reject']!='None':raise ValueError()
  for bits in c['Prior']+c['Incoming']:n.finite_scalar(bits)
  if len(c['Prior'])!=ncell or len(c['Incoming'])!=ncell:raise ValueError()
  x=f.axes(c['OriginX'],c['SpacingX'],c['Width']);z=f.axes(c['OriginZ'],c['SpacingZ'],c['Rows'])
  model=f.route.mode.Model(dict(Hex=f.route.b.frame(c['FilterWords']).hex(),Width=c['Width'],Rows=c['Rows'],TablePrecision=c['Table'],ConstructionPrecision=c['Construction']))
  cells=[];products=[]
  for i,incoming in enumerate(c['Incoming']):
   value=model.miss(dict(XBits=x[i%c['Width']],ZBits=z[i//c['Width']]),c['Query']);result,local,scaled=filter_value(c,value)
   product=f.route.b.stored(n.arithmetic(c['Product'])[1](n.finite_scalar(local),n.finite_scalar(incoming)));products.append(product)
   cells.append(dict(Index=i,Retained=n.fraction_text(value),Projection=f.route.b.stored(value),Scaled=scaled,Amount=result['Amount'],Branch=result['Branch'],Query=result['Query'],Span=result['Span'],Local=local,Product=product))
  h=field.expected(dict(clean,Influence=products))
  if h['Reject']!='None':raise ValueError()
  l=len(bytes.fromhex(c['RuleHex']));shared=8*l+65536+9512+136+32*ncell;sw=l+20256+ncell;oct=c['FilterWords'][5]
  return dict(Name=c['Name'],Reject='None',Prior=c['Prior'],Incoming=c['Incoming'],Products=products,Heights=h['Heights'],Cells=cells,Counts=[1,1,1,ncell,ncell,ncell,1,ncell*oct]+h['Counts'],LogicalCopyBytes=res[0],ReservedRouteBytes=res[1],ReservedWork=res[2],ReservedFilterQueryBytes=res[3],ReservedFilterQueryWork=res[4],AccountedRouteBytes=h['AccountedRouteBytes']+shared+ncell*(480+80*oct),AccountedWork=h['AccountedWork']+sw+ncell*(80+512*oct)+32*(c['Width']+c['Rows'])+c['FilterBytes'])
 except (ValueError,AssertionError,IndexError,ZeroDivisionError,OverflowError):return dict(Name=c['Name'],Reject='Input')

def generate(a):
 n.gate(a.exe.read_bytes(),n.EXE);n.setup();f.route.lead.n.setup();cases=[]
 words=[47,0,0,0,0,3,word(2),word(.5),word(1),word(1),0,0,3]
 def add(w=None,**changes):
  w=list(w or words);rule=sel.ff.encoded(family=0xfffffff9,lo=word(0),hi=word(2),selector=3,amount=word(1),scale=word(1));rulekw=changes.pop('FilterChanges',{})
  if rulekw:rule=sel.ff.encoded(family=0xfffffff9,**dict(dict(lo=word(0),hi=word(2),selector=3,amount=word(1),scale=word(1)),**rulekw))
  g=sel.generator(rule,w);snap=sel.snapshot(rule);group=f.route.group(-7,w)
  c=dict(Name=str(len(cases)),GeneratorHex=g.hex(),GroupHex=group.hex(),Words=w,FilterWords=w,Filter=snap,FilterBytes=len(rule),FamilyId=-7,RuleHex=f.route.rule(-7),Operation=1,Scale=word(15),Width=3,Rows=2,OriginX=word(.125),OriginZ=word(.375),SpacingX=word(.25),SpacingZ=word(.25),Prior=[word(-.25)]*6,Incoming=[0,0x80000000,word(-1),word(.25),word(1),word(2)],Table=53,Construction=53,Query=64,Consumer=64,FilterQuery=64,Combine=64,Product=64,**{})
  c.update(changes);cases.append(c);return c
 for mode in range(6):
  for p in (24,53,64):
   w=list(words);w[12]=mode;add(w,Table=p,Construction=p,Query=p,Consumer=p,FilterQuery=p,Combine=p,Product=p)
   add(w,Table=p,Construction=p,Query=p,Consumer=p,FilterQuery=p,Combine=p,Product=p,Incoming=[word(.75)]*6,Uniform=True,FilterChanges=dict(lo=word(-10),hi=word(10),amount=0))
 for p in (24,53,64):
  for cp in (24,53,64):add(FilterQuery=p,Combine=cp,Product=cp,Consumer=p)
 for kw in (dict(lo=word(2),hi=0),dict(lo=word(1),hi=word(1)),dict(selector=0xffffffff),dict(amount=word(-2)),dict(amount=word(2)),dict(scale=0),dict(selector=1),dict(selector=2)):
  for p in (24,53,64):add(FilterChanges=kw,FilterQuery=p,Combine=p,Product=p)
 add(Incoming=[0]*6);add(Incoming=[0x80000000,word(-1)]*3)
 add(Width=8,Rows=8,Prior=[word(-.25)]*64,Incoming=[word(1)]*64,SpacingX=word(.1234567),SpacingZ=word(.2345678),FilterChanges=dict(scale=word(7.12345)))
 add(OriginX=0,OriginZ=0,SpacingX=word(.0000001),SpacingZ=word(.0000001))
 add(Prior=[0,word(.25),0x80000000,word(-1),word(1),0],Incoming=[0,word(.25),0x80000000,word(-1),word(1),0],Alias=True)
 # Exact pinned selection only; no new corpus inventory.
 g,index,_=sel.original(a.asset_root,a.config);meta=sel.t.metadata(g);gs=next(x['Section'] for x in meta['Groups'] if x['Role']=='FractalMap');gb=g[gs['Offset']:gs['Offset']+gs['EncodedBytes']];ss=meta['Rules'][index]['Section'];rb=g[ss['Offset']:ss['Offset']+ss['EncodedBytes']];snap=sel.snapshot(rb);entries=sel.f.inspect(gb,sel.f.LIMITS)['Entries'];fid=snap['Family'];fw=next(e['Words'] for e in entries if e['Id']==fid)
 c=add();c.update(GeneratorHex=g.hex(),Index=index,GroupHex=gb.hex(),Words=fw,FilterWords=fw,Filter=snap,FilterBytes=len(rb),FamilyId=fid,RuleHex=f.route.rule(fid),Original=True)
 base=add();res=reserve(base);keys=('MaximumCopyBytes','MaximumRouteBytes','MaximumWork','MaximumFilterQueryBytes','MaximumFilterQueryWork');add(**dict(zip(keys,res)))
 for k,v in zip(keys,res):add(**{k:v-1})
 for kw in (dict(Width=1),dict(Width=129),dict(Rows=0),dict(MaximumSamples=5),dict(Prior=[0]*5),dict(Incoming=[0]*7),dict(Prior=[0]*5+[0x7fc12345]),dict(Incoming=[0]*5+[0x7f800000]),dict(SpacingX=0),dict(OriginZ=0x7f800000),dict(FilterQuery=23),dict(Combine=23),dict(Product=23),dict(Cancel=True),dict(ForeignFamilySet=True),dict(Policy=0),dict(HeightMaximumCopyBytes=0),dict(HeightMaximumRouteBytes=0),dict(HeightMaximumWork=0),dict(HeightMaximumQueryBytes=0),dict(HeightMaximumQueryWork=0),dict(MaximumRuleBytes=23),dict(MaximumCopyBytes=8388609)):
  add(**kw)
 for kw in (dict(enabled=0),dict(scale=0x7f800000),dict(lo=0x7fc12345),dict(amount=0xff800000),dict(version='0004')):add(FilterChanges=kw)
 for at,v in ((5,0),(12,6),(1,1),(0,0x7fffffff)):
  w=list(words);w[at]=v;add(w)
 w=list(words);w[5]=2;w[7]=word(-1);add(w,Incoming=[0]*6) # filter normalization must still execute
 w=list(words);w[8]=0x7f7fffff;add(w,OriginX=0,OriginZ=0,Incoming=[0]*6) # late filter query failure
 add(FilterChanges=dict(scale=0x7f7fffff),Incoming=[0]*6)
 add(Prior=[0]*5+[word(2000000)],Incoming=[0]*6)
 add(RuleHex=f.route.rule(-7,scale=0x7f7fffff),Scale=0x7f7fffff,Incoming=[0x7f7fffff]*6,FilterChanges=dict(lo=word(-10),hi=word(10),amount=0))
 add(RuleHex=f.route.rule(-7,enabled=0),Disabled=True,Incoming=[0]*6);add(RuleHex=f.route.rule(99),Missing=True,Incoming=[0]*6)
 add(RuleHex=f.route.rule(-7,4),Operation=4,Incoming=[0]*6)
 add(RuleHex=f.route.rule(-7,scale=0x7fc12345),Scale=0x7fc12345,Incoming=[0]*6)
 # Full-cap shapes and exact/one-below downstream field reservations.
 add(Width=16,Rows=16,Prior=[word(-.25)]*256,Incoming=[word(1)]*256)
 add(Width=128,Rows=8,MaximumSamples=1024,HeightMaximumSamples=1024,Prior=[word(-.25)]*1024,Incoming=[word(1)]*1024,SpacingX=word(.001),SpacingZ=word(.001))
 cap=field.account(base);heightkeys=('HeightMaximumCopyBytes','HeightMaximumRouteBytes','HeightMaximumWork','HeightMaximumQueryBytes','HeightMaximumQueryWork');add(**dict(zip(heightkeys,cap)))
 for k,v in zip(heightkeys,cap):add(**{k:v-1})
 for k,v in (('MaximumRouteBytes',536870913),('MaximumWork',134217729),('MaximumFilterQueryBytes',2097153),('MaximumFilterQueryWork',16777217),('MaximumSamples',1025),('HeightMaximumSamples',1025),('Table',23),('Construction',23),('Query',23),('Consumer',23)):
  add(**{k:v})
 # Distinct valid filter family and failing height family: zero products skip height normalization.
 for failure,zero in (('normalize',True),('normalize',False),('query',False)):
  c=add(Incoming=[0 if zero else word(1)]*6,FilterChanges=dict(lo=word(-10),hi=word(10),amount=0))
  hw=list(words)
  if failure=='normalize':hw[5]=2;hw[7]=word(-1)
  else:hw[8]=0x7f7fffff;c.update(OriginX=0,OriginZ=0)
  multi=f.route.lead.form(b'MGRP',f.route.lead.form(b'0000',f.route.group(-7,words)[24:]+f.route.group(-8,hw)[24:]))
  rb=sel.ff.encoded(family=0xfffffff9,lo=word(-10),hi=word(10),selector=3,amount=0,scale=word(1))
  groups=[sel.t.form(tag,sel.t.form(v)) for tag,v in zip(sel.t.TAGS[:4],sel.t.VERS[:4])]+[multi]
  gen=sel.t.encode(groups=groups,last=sel.t.form(b'LYRS',sel.t.layer(children=rb)))
  c.update(GeneratorHex=gen.hex(),GroupHex=multi.hex(),Words=hw,FilterWords=list(words),FamilyId=-8,RuleHex=f.route.rule(-8))
 return cases
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for k in ('exe','asset-root','config','out'):p.add_argument('--'+k,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args();assert a.out.resolve().is_relative_to(ROOT/'local-private');cases=generate(a);want=[expected(c) for c in cases];a.out.mkdir(parents=True,exist_ok=True)
 witnesses=0
 for c,e in zip(cases,want):
  if e['Reject']!='None':continue
  model=f.route.mode.Model(dict(Hex=f.route.b.frame(c['FilterWords']).hex(),Width=c['Width'],Rows=c['Rows'],TablePrecision=c['Table'],ConstructionPrecision=c['Construction']));x=f.axes(c['OriginX'],c['SpacingX'],c['Width']);z=f.axes(c['OriginZ'],c['SpacingZ'],c['Rows'])
  for i,cell in enumerate(e['Cells']):
   value=model.miss(dict(XBits=x[i%c['Width']],ZBits=z[i//c['Width']]),c['Query']);_,local,scaled=filter_value(c,value,True)
   witnesses+=scaled!=cell['Scaled'] or local!=cell['Local']
 assert witnesses>0,'Retained projection witness required'
 summary=dict(Cases=len(cases),Rejected=sum(e['Reject']!='None' for e in want),Cells=sum(len(e.get('Cells',[])) for e in want),Applied=sum(sum(n.finite_scalar(w)[0]>0 for w in e.get('Products',[])) for e in want),RetainedVsF32Witnesses=witnesses,OriginalSelections=1,OriginalExecuted=False)
 if a.native:
  native=json.loads(a.native.read_text(encoding='utf-8'));assert native['Cases']==want,'Filtered field receipts differ';summary.update(Passed=True,OwnershipChecks=native['OwnershipChecks'],CompositionComparisons=native['CompositionComparisons'],UniformComparisons=native['UniformComparisons']);(a.out/'filtered-receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 else:(a.out/'filtered-fixtures.json').write_text(json.dumps(cases),encoding='utf-8');(a.out/'filtered-expected.json').write_text(json.dumps(want),encoding='utf-8')
 print(json.dumps(summary))
