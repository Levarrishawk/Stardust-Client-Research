"""Exact per-cell Fraction oracle, explicit skips, conservative all-positive preflight. No expected-noise injection."""
import argparse,importlib.util,json,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('field_frozen52',ROOT/'poc/round52-tests/Rendering/verify_patch.py');f=importlib.util.module_from_spec(s);sys.modules[s.name]=f;s.loader.exec_module(f)
n=f.n;word=f.word;sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def account(c):
 count=c['Width']*c['Rows'];l=len(bytes.fromhex(c['RuleHex']));copies=l+4*(c['Width']+c['Rows'])+100*count+256
 decode=8*l+65536;shared=decode+9512+136+32*count;sw=l+20000+256+count
 return copies,shared+count*1696,l+32*(c['Width']+c['Rows'])+24*count+sw+count*(8288+16),16*count+count*1440,count+count*8256

def generate(a):
 n.gate(a.exe.read_bytes(),n.EXE);n.setup();f.route.lead.n.setup();raw,entries=f.route.lead.original_group(a.asset_root,a.config);w=entries[0]['Words']
 template=dict(GroupHex=raw.hex(),Words=w,FamilyId=1,RuleHex=f.route.rule(1),Operation=1,Scale=word(15),Width=3,Rows=2,OriginX=word(-.5),OriginZ=word(.125),SpacingX=word(.25),SpacingZ=word(.375),Prior=[word(-.25)]*6,Influence=[word(.75)]*6,Uniform=True)
 cases=[]
 for tp in (24,53,64):
  for cp in (24,53,64):
   for qp in (24,53,64):
    for hp in (24,53,64):cases.append(dict(template,Table=tp,Construction=cp,Query=qp,Consumer=hp))
 authored=[47,0,0x7fc12345,0,0x7f800000,3,word(1.5),word(.35),word(.5),word(.25),word(-.25),word(.5),0]
 for mode in range(6):
  sw=list(authored);sw[12]=mode
  for p in (24,53,64):cases.append(dict(template,GroupHex=f.route.group(-1,sw).hex(),Words=sw,FamilyId=-1,RuleHex=f.route.rule(-1,mode%4),Operation=mode%4,Width=3,Rows=3,Prior=[word(-.25),word(.125),0x80000000,0,word(.75),word(1),word(-.5),word(.25),word(-1)],Influence=[0,0x80000000,word(-1),word(.25),word(.75),word(2),0xff7fffff,word(.0000001),0],Table=p,Construction=53,Query=p,Consumer=p,Uniform=False))
 first=dict(cases[0]);zero=dict(first,Width=2,Rows=2,Prior=[0,0x80000000,word(1),word(-1)],Influence=[0,0x80000000,word(-1),0xff7fffff],Uniform=False)
 cases.append(zero)
 for p in (24,53,64):
  sw=list(authored);sw[5]=2;sw[7]=word(-1);cases.append(dict(zero,GroupHex=f.route.group(1,sw).hex(),Words=sw,Table=p,Construction=p,Query=p,Consumer=p,NoNormalization=True))
 sw=list(authored);sw[0]=0;cases.append(dict(zero,GroupHex=f.route.group(1,sw).hex(),Words=sw,NoNormalization=True))
 sw=list(authored);sw[8]=0x7f7fffff;cases.append(dict(zero,GroupHex=f.route.group(1,sw).hex(),Words=sw,NoQuery=True))
 for p in (24,53,64):cases.append(dict(cases[81],OriginX=0,OriginZ=0,SpacingX=word(.0000001),SpacingZ=word(.0000001),Table=p,Construction=p,Query=p,Consumer=p))
 alias=dict(zero,Prior=[0,word(.25),0x80000000,word(-1)],Influence=[0,word(.25),0x80000000,word(-1)],Alias=True);cases.append(alias)
 for changes in (dict(Width=16,Rows=16,Prior=[word(-.25)]*256,Influence=[word(.75)]*256),dict(Width=128,Rows=8,Prior=[word(-.25)]*1024,Influence=[word(.75)]*1024,MaximumSamples=1024)):
  cases.append(dict(first,**changes))
 base=dict(zero);requirements=account(base);keys=('MaximumCopyBytes','MaximumRouteBytes','MaximumWork','MaximumQueryBytes','MaximumQueryWork');cap=dict(base,**dict(zip(keys,requirements)));cases.append(cap)
 for key in keys:cases.append(dict(cap,**{key:cap[key]-1}))
 for changes in (dict(Width=1),dict(Width=129),dict(Rows=0),dict(MaximumSamples=1025),dict(MaximumSamples=5),dict(Prior=first['Prior'][:-1]),dict(Influence=first['Influence']+[0]),dict(Prior=first['Prior'][:-1]+[0x7f800000]),dict(Influence=first['Influence'][:-1]+[0x7fc12345]),dict(MaximumRuleBytes=0),dict(MaximumRuleBytes=65537),dict(MaximumCopyBytes=4194305),dict(MaximumRouteBytes=268435457),dict(MaximumWork=67108865),dict(MaximumQueryBytes=2097153),dict(MaximumQueryWork=16777217),dict(OriginX=0x7f800000),dict(SpacingX=0),dict(SpacingZ=word(-1)),dict(OriginX=word(1),SpacingX=1),dict(Table=23),dict(Construction=65),dict(Query=0),dict(Consumer=23),dict(Cancel=True)):
  cases.append(dict(first,**changes))
 # Whole-field grammar remains strict even when no cell asks for noise.
 for changes in (dict(RuleHex=f.route.rule(987654),Missing=True),dict(RuleHex=f.route.rule(1,enabled=0),Disabled=True),dict(RuleHex=f.route.rule(1,4),Operation=4),dict(RuleHex=f.route.rule(1,scale=0x7fc12345),Scale=0x7fc12345)):
  cases.append(dict(zero,**changes))
 for index,value in ((12,6),(1,1),(5,0),(0,0x7fffffff),(6,0x7f800000)):
  sw=list(authored);sw[index]=value;cases.append(dict(zero,GroupHex=f.route.group(1,sw).hex(),Words=sw))
 malformed=bytearray.fromhex(zero['RuleHex']);malformed[0]^=1;cases.append(dict(zero,RuleHex=malformed.hex(),Malformed=True))
 opaque=bytearray.fromhex(zero['RuleHex']);opaque[20:24]=b'9999';cases.append(dict(zero,RuleHex=opaque.hex(),Opaque=True))
 # Late query, actual affector overflow and late skipped display failure.
 sw=[47,0,0,0,0,1,word(1),word(1),0x7f7fffff,word(1),0,0,3]
 cases.append(dict(zero,GroupHex=f.route.group(1,sw).hex(),Words=list(sw),OriginX=0,OriginZ=0,SpacingX=word(.125),SpacingZ=word(.125),Influence=[word(1)]*4,LateQuery=True))
 sw[8]=word(1);cases.append(dict(zero,GroupHex=f.route.group(1,sw).hex(),Words=sw,RuleHex=f.route.rule(1,1,0x7f7fffff),Scale=0x7f7fffff,OriginX=0,OriginZ=0,SpacingX=word(.125),SpacingZ=word(.125),Prior=[0]*4,Influence=[0x7f7fffff]*4,LateConsumer=True))
 cases.append(dict(zero,Prior=zero['Prior'][:-1]+[word(2000000)],LateDisplay=True))
 for i,c in enumerate(cases):c['Name']=str(i)
 return cases

def expected(c):
 try:
  w,r=c['Width'],c['Rows'];count=w*r;l=len(bytes.fromhex(c['RuleHex']))
  for key,default,maximum in (('MaximumSamples',256,1024),('MaximumRuleBytes',65536,65536),('MaximumCopyBytes',1048576,4194304),('MaximumRouteBytes',134217728,268435456),('MaximumWork',16777216,67108864),('MaximumQueryBytes',2097152,2097152),('MaximumQueryWork',16777216,16777216)):
   if not 0<=c.get(key,default)<=maximum:raise ValueError()
  if not(2<=w<=128 and 2<=r<=128 and count<=c.get('MaximumSamples',256) and len(c['Prior'])==len(c['Influence'])==count and 24<=l<=c.get('MaximumRuleBytes',65536)):raise ValueError()
  for key in ('Table','Construction','Query','Consumer'):n.arithmetic(c[key])
  for key in ('OriginX','OriginZ','SpacingX','SpacingZ'):n.finite_scalar(c[key])
  if n.finite_scalar(c['SpacingX'])[0]<=0 or n.finite_scalar(c['SpacingZ'])[0]<=0:raise ValueError()
  reserves=account(c)
  for i,(key,default) in enumerate((('MaximumCopyBytes',1048576),('MaximumRouteBytes',134217728),('MaximumWork',16777216),('MaximumQueryBytes',2097152),('MaximumQueryWork',16777216))):
   if reserves[i]>c.get(key,default):raise ValueError()
  if c.get('Cancel'):raise ValueError()
  for bits in c['Prior']+c['Influence']:n.finite_scalar(bits)
  x=f.axes(c['OriginX'],c['SpacingX'],w);z=f.axes(c['OriginZ'],c['SpacingZ'],r)
  if any(c.get(k) for k in ('Missing','Disabled','Malformed','Opaque')) or c['Operation']>3:raise ValueError()
  n.finite_scalar(c['Scale']);words=c['Words']
  if words[12]>5 or words[1] or words[3] or not 1<=words[5]<=16 or words[0]==0x7fffffff:raise ValueError()
  for i in (6,7,8,9,10,11):n.finite_scalar(words[i])
  positives=sum(n.finite_scalar(bits)[0]>0 for bits in c['Influence']);cells=[];heights=[]
  for i,(prior,weight) in enumerate(zip(c['Prior'],c['Influence'])):
   applied=None;bits=prior
   if n.finite_scalar(weight)[0]>0:
    rc=dict(c,Name=str(i),Width=1,Rows=1,Column=0,Row=0,X=x[i%w],Z=z[i//w],Height=prior,Weight=weight)
    for key in ('MaximumRuleBytes','MaximumWork'):rc.pop(key,None)
    result=f.route.expected(rc)
    if result['Reject']!='None':raise ValueError()
    bits=result['Result'];applied=dict(Origin=result['Origin'],Retained=result['Retained'],Projection=result['Projection'],Height=bits,FamilyId=result['FamilyId'],CacheWidth=w,CacheRows=r,Column=i%w,Row=i//w,AccountingPolicy='SharedPreparationCell',AccountedBytes=1696,AccountedWork=8288)
   if abs(n.finite_scalar(bits)[0])>1000000:raise ValueError()
   heights.append(bits);cells.append(dict(Index=i,Column=i%w,Row=i//w,X=x[i%w],Z=z[i//w],Prior=prior,Influence=weight,Height=bits,Status='Applied' if applied else 'SkippedNonPositiveInfluence',Applied=applied))
  decoded=8*l+65536;shared=decoded+(9512+136+32*count if positives else 0);sw=l+(20000+256+count if positives else 0)
  work=l+32*(w+r)+24*count+sw+positives*(8288+16)
  low=min(heights,key=lambda h:n.finite_scalar(h)[0]);high=max(heights,key=lambda h:n.finite_scalar(h)[0]);has=int(positives>0)
  return dict(Name=c['Name'],Reject='None',SourceSha256=sha(bytes.fromhex(c['GroupHex'])),RuleSha256=sha(bytes.fromhex(c['RuleHex'])),X=x,Z=z,Prior=c['Prior'],Influence=c['Influence'],Heights=heights,Cells=cells,Bounds=[x[0],x[-1],z[0],z[-1],low,high],Counts=[1,1,has,has,positives,positives,has,positives*words[5]],LogicalCopyBytes=reserves[0],ReservedRouteBytes=reserves[1],ReservedWork=reserves[2],ReservedQueryBytes=reserves[3],ReservedQueryWork=reserves[4],SharedPreparationBytes=shared,SharedPreparationWork=sw,AccountedRouteBytes=shared+positives*1696,AccountedWork=work)
 except (ValueError,ZeroDivisionError,OverflowError,IndexError):return dict(Name=c['Name'],Reject='Input')
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for key in ('exe','asset-root','config','out'):p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args();assert a.out.resolve().is_relative_to(ROOT/'local-private');cases=generate(a);want=[expected(c) for c in cases];a.out.mkdir(parents=True,exist_ok=True)
 summary=dict(Cases=len(cases),Rejected=sum(e['Reject']!='None' for e in want),Cells=sum(len(e.get('Cells',[])) for e in want),Applied=sum(e['Counts'][4] for e in want if e['Reject']=='None'),Skipped=sum(sum(x['Status']!='Applied' for x in e['Cells']) for e in want if e['Reject']=='None'),NoiseFree=sum(e['Counts'][2]==0 for e in want if e['Reject']=='None'),OriginalExecuted=False)
 if a.native:
  native=json.loads(a.native.read_text(encoding='utf-8'));assert native['Cases']==want,'Per-cell field differs';summary.update(Passed=True,OwnershipChecks=native['OwnershipChecks'],UniformComparisons=native['UniformComparisons']);(a.out/'field-receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 else:(a.out/'field-fixtures.json').write_text(json.dumps(cases),encoding='utf-8');(a.out/'field-expected.json').write_text(json.dumps(want),encoding='utf-8')
 print(json.dumps(summary))
