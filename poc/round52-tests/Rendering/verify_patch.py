"""Independent authored rectangular F32 axes -> frozen exact family/noise/AHFR model. No expected sample injection."""
import argparse,importlib.util,json,hashlib,sys
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('patch_route51',ROOT/'poc/round51-tests/Rendering/verify_route.py');route=importlib.util.module_from_spec(s);sys.modules[s.name]=route;s.loader.exec_module(route)
n=route.n;b=route.b;mode=route.mode
sha=lambda v:hashlib.sha256(v).hexdigest().upper()
def word(v):return route.lead.j.word(v)
def axes(origin,spacing,count):
 add,mul,_=n.arithmetic(24);result=[]
 for i in range(count):
  offset=b.stored(mul(n.exact_int(i),n.finite_scalar(spacing)));bits=b.stored(add(n.finite_scalar(origin),n.finite_scalar(offset)));v=n.finite_scalar(bits)[0]
  if abs(v)>1000000 or result and v<=n.finite_scalar(result[-1])[0]:raise ValueError('Axis/display')
  result.append(bits)
 return result

def generate(a):
 n.gate(a.exe.read_bytes(),n.EXE);n.setup();route.lead.n.setup();raw,entries=route.lead.original_group(a.asset_root,a.config);original=entries[0]['Words']
 template=dict(GroupHex=raw.hex(),Words=original,FamilyId=1,RuleHex=route.rule(1),Operation=1,Scale=word(15),Width=3,Rows=2,OriginX=word(-.5),OriginZ=word(.125),SpacingX=word(.25),SpacingZ=word(.375),Height=word(-.25),Weight=word(.75))
 cases=[]
 for tp in (24,53,64):
  for cp in (24,53,64):
   for qp in (24,53,64):
    for hp in (24,53,64):cases.append(dict(template,Table=tp,Construction=cp,Query=qp,Consumer=hp))
 authored=[47,0,0x7fc12345,0,0x7f800000,3,word(1.5),word(.35),word(.5),word(.25),word(-.25),word(.5),0]
 for m in range(6):
  w=list(authored);w[12]=m;id=-1 if m%2 else -7
  for p in (24,53,64):cases.append(dict(template,GroupHex=route.group(id,w).hex(),Words=w,FamilyId=id,RuleHex=route.rule(id,m%4),Operation=m%4,Width=3,Rows=3,OriginX=word(-1.25),OriginZ=word(-.375),SpacingX=word(.5000000596046448),SpacingZ=word(.125),Table=p,Construction=53,Query=p,Consumer=p))
 first=dict(cases[0])
 cases.extend([dict(first,Width=16,Rows=16),dict(first,Width=128,Rows=8,MaximumSamples=1024,MaximumWork=67108864),dict(first,Width=2,Rows=128)])
 for changes in (dict(Width=2,Rows=2,OriginX=0x80000000,OriginZ=0x80000000,SpacingX=1,SpacingZ=1),dict(Width=5,Rows=3,OriginX=word(999999),OriginZ=word(-1000000),SpacingX=word(.25),SpacingZ=word(.5)),dict(Width=2,Rows=2,OriginX=word(-.125),OriginZ=word(-.125),SpacingX=word(.1),SpacingZ=word(.1))):cases.append(dict(first,**changes))
 base=cases[-1];length=len(bytes.fromhex(base['RuleHex']));count=base['Width']*base['Rows'];copies,routes,work=account(base,length,count)
 cases.append(dict(base,MaximumSamples=count,MaximumRuleBytes=length,MaximumCopyBytes=copies,MaximumRouteBytes=routes,MaximumWork=work))
 for changes in (dict(MaximumSamples=count-1),dict(MaximumRuleBytes=length-1),dict(MaximumCopyBytes=copies-1),dict(MaximumRouteBytes=routes-1),dict(MaximumWork=work-1)):
  c=dict(base,MaximumSamples=count,MaximumRuleBytes=length,MaximumCopyBytes=copies,MaximumRouteBytes=routes,MaximumWork=work);c.update(changes);cases.append(c)
 for changes in (dict(Width=1),dict(Rows=0),dict(Width=129),dict(Width=33,Rows=33,MaximumSamples=1024),dict(Width=17,Rows=16),dict(MaximumSamples=1025),dict(MaximumCopyBytes=4194305),dict(MaximumRouteBytes=268435457),dict(MaximumWork=67108865),dict(MaximumRuleBytes=65537),dict(OriginX=0x7f800000),dict(OriginZ=0x7fc12345),dict(SpacingX=0),dict(SpacingZ=0x80000000),dict(SpacingX=word(-1)),dict(SpacingZ=0x7f800000),dict(Weight=0),dict(Weight=word(-1)),dict(Weight=0x7fc12345),dict(Height=0x7f800000),dict(OriginX=word(1),SpacingX=1),dict(OriginZ=word(999999),SpacingZ=word(2)),dict(Table=23),dict(Construction=65),dict(Query=0),dict(Consumer=23),dict(Cancel=True),dict(RuleHex=route.rule(987654),Missing=True),dict(RuleHex=route.rule(1,enabled=0),Disabled=True),dict(RuleHex=route.rule(1,4),Operation=4),dict(RuleHex=route.rule(1,scale=0x7fc12345),Scale=0x7fc12345)):
  c=dict(first);c.update(changes);cases.append(c)
 malformed=bytearray.fromhex(first['RuleHex']);malformed[0]^=1;cases.append(dict(first,RuleHex=malformed.hex(),Malformed=True))
 opaque=bytearray.fromhex(first['RuleHex']);opaque[20:24]=b'9999';cases.append(dict(first,RuleHex=opaque.hex(),Opaque=True))
 for index,value in ((12,6),(1,1),(5,0),(6,0x7f800000),(0,0x7fffffff)):
  w=list(authored);w[index]=value;cases.append(dict(first,GroupHex=route.group(1,w).hex(),Words=w))
 # At the first integer origin abs(noise)=0. Subsequent noninteger cell overflows AHFR;
 # complete-route expectation records the late index, but supplies no sample to native.
 w=[47,0,0,0,0,1,word(1),word(1),word(1),word(1),0,0,3]
 cases.append(dict(first,GroupHex=route.group(1,w).hex(),Words=w,Width=2,Rows=2,OriginX=0,OriginZ=0,SpacingX=word(.125),SpacingZ=word(.125),Height=0,Weight=0x7f7fffff,Scale=0x7f7fffff,RuleHex=route.rule(1,1,0x7f7fffff),LateFailure=True))
 cases.append(dict(first,GroupHex=route.group(1,w).hex(),Words=w,Width=2,Rows=2,OriginX=0,OriginZ=0,SpacingX=word(.125),SpacingZ=word(.125),Height=0,Weight=word(1),Scale=word(10000000),RuleHex=route.rule(1,1,word(10000000)),LateFailure=True))
 for i,c in enumerate(cases):c['Name']=str(i)
 return cases

def account(c,length,count):
 copies=length+4*(c['Width']+c['Rows']+count)+8*count+256
 perbytes=8*length+65536+9512+136+32+160+1280+256;perwork=length+20000+256+1+8192+64+32
 return copies,count*perbytes,length+32*(c['Width']+c['Rows'])+count*(perwork+16)

def expected(c,details=None):
 try:
  w,r=c['Width'],c['Rows'];count=w*r;length=len(bytes.fromhex(c['RuleHex']))
  for key,default,maximum in (('MaximumSamples',256,1024),('MaximumRuleBytes',65536,65536),('MaximumCopyBytes',1048576,4194304),('MaximumRouteBytes',134217728,268435456),('MaximumWork',16777216,67108864)):
   if not 0<=c.get(key,default)<=maximum:raise ValueError('Hard limit')
  if not(2<=w<=128 and 2<=r<=128 and count<=c.get('MaximumSamples',256) and 24<=length<=c.get('MaximumRuleBytes',65536)):raise ValueError('Shape/rule')
  for key in ('Table','Construction','Query','Consumer'):n.arithmetic(c[key])
  for key in ('OriginX','OriginZ','SpacingX','SpacingZ','Height','Weight'):n.finite_scalar(c[key])
  if any(n.finite_scalar(c[k])[0]<=0 for k in ('SpacingX','SpacingZ','Weight')):raise ValueError('Positive input')
  copies,routes,work=account(c,length,count)
  if copies>c.get('MaximumCopyBytes',1048576) or routes>c.get('MaximumRouteBytes',134217728) or work>c.get('MaximumWork',16777216) or c.get('Cancel'):raise ValueError('Budget/cancel')
  x=axes(c['OriginX'],c['SpacingX'],w);z=axes(c['OriginZ'],c['SpacingZ'],r);samples=[];height=[]
  for row in range(r):
   for col in range(w):
    rc=dict(c,Width=1,Rows=1,Column=0,Row=0,X=x[col],Z=z[row]);rc.pop('Name');rc['Name']=str(len(height))
    # Patch limits have different accounting from the frozen single-call route.
    for k in ('MaximumRuleBytes','MaximumWork'):rc.pop(k,None)
    result=route.expected(rc)
    if result['Reject']!='None' or abs(n.finite_scalar(result['Result'])[0])>1000000:
     if details is not None:details.append(dict(Name=c['Name'],Index=len(height)))
     raise ValueError('Late route/display')
    height.append(result['Result']);samples.append(dict(FamilyId=result['FamilyId'],Origin=result['Origin'],Retained=result['Retained'],Projection=result['Projection'],Height=result['Result'],X=x[col],Z=z[row]))
  low=min(height,key=lambda h:n.finite_scalar(h)[0]);high=max(height,key=lambda h:n.finite_scalar(h)[0])
  return dict(Name=c['Name'],Reject='None',Width=w,Rows=r,SourceSha256=sha(bytes.fromhex(c['GroupHex'])),RuleSha256=sha(bytes.fromhex(c['RuleHex'])),X=x,Z=z,Heights=height,Samples=samples,Bounds=[x[0],x[-1],z[0],z[-1],low,high],LogicalCopyBytes=copies,AggregateRouteBytes=routes,AccountedWork=work)
 except (ValueError,ZeroDivisionError,OverflowError,IndexError):return dict(Name=c['Name'],Reject='Input')
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for key in ('exe','asset-root','config','out'):p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args();assert a.out.resolve().is_relative_to(ROOT/'local-private')
 cases=generate(a);late=[];want=[expected(c,late) for c in cases];a.out.mkdir(parents=True,exist_ok=True)
 summary=dict(Cases=len(cases),Rejected=sum(c['Reject']!='None' for c in want),Samples=sum(len(c.get('Heights',[])) for c in want),OriginalFamilyCases=sum(c['GroupHex']==cases[0]['GroupHex'] and w['Reject']=='None' for c,w in zip(cases,want)),SyntheticModeCases=18,LateFailures=[x for x in late if cases[int(x['Name'])].get('LateFailure')],OriginalExecuted=False)
 assert len(summary['LateFailures'])==2 and all(x['Index']>0 for x in summary['LateFailures'])
 if a.native:
  native=json.loads(a.native.read_text(encoding='utf-8'));assert native['Cases']==want,'Native height patch differs';summary.update(Passed=True,OwnershipChecks=native['OwnershipChecks']);(a.out/'patch-receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 else:
  (a.out/'patch-fixtures.json').write_text(json.dumps(cases),encoding='utf-8');(a.out/'patch-expected.json').write_text(json.dumps(want),encoding='utf-8')
 print(json.dumps(summary))
