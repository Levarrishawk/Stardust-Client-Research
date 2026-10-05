"""Frozen complete-height oracle plus explicit one-table/unique-cell accounting. No injected samples or timings."""
import argparse,importlib.util,json,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('single_patch_frozen52',ROOT/'poc/round52-tests/Rendering/verify_patch.py');f=importlib.util.module_from_spec(s);sys.modules[s.name]=f;s.loader.exec_module(f)
def account(c):
 length=len(bytes.fromhex(c['RuleHex']));n=c['Width']*c['Rows'];old=f.account(c,length,n)
 copy=old[0]+16*n;sharedbytes=8*length+65536+9512+136+32*n;sharedwork=length+20000+256+n
 return copy,sharedbytes+n*1696,length+32*(c['Width']+c['Rows'])+sharedwork+n*(8288+32),sharedbytes,sharedwork

def normalise(c):
 c=dict(c);old=f.account(c,len(bytes.fromhex(c['RuleHex'])),c['Width']*c['Rows']);new=account(c)
 for key,index,maximum in (('MaximumCopyBytes',0,4194304),('MaximumRouteBytes',1,268435456),('MaximumWork',2,67108864)):
  if key in c and 0<=c[key]<=maximum:c[key]+=new[index]-old[index]
 return c

def expected(c):
 # Transform only declared logical cap thresholds back to the frozen independent path;
 # actual numeric axes/kernel/consumer expectations remain entirely frozen.
 candidate=dict(c);new=account(c);old=f.account(c,len(bytes.fromhex(c['RuleHex'])),c['Width']*c['Rows'])
 for key,index,default,maximum in (('MaximumCopyBytes',0,1048576,4194304),('MaximumRouteBytes',1,134217728,268435456),('MaximumWork',2,16777216,67108864)):
  value=c.get(key,default)
  if not 0<=value<=maximum or value<new[index]:return dict(Name=c['Name'],Reject='Input')
  candidate.pop(key,None)
 # The frozen default caps can be lower than its repeated-build requirement for a
 # valid single-sampler request, so provide only adequate independently bounded defaults.
 candidate.update(MaximumCopyBytes=4194304,MaximumRouteBytes=268435456,MaximumWork=67108864)
 want=f.expected(candidate)
 if want['Reject']!='None':return want
 n=c['Width']*c['Rows'];want.update(LogicalCopyBytes=new[0],AggregateRouteBytes=new[1],AccountedWork=new[2],SharedPreparationBytes=new[3],SharedPreparationWork=new[4],SamplingPolicy='SingleFreshSamplerUniqueCells',Counts=[1,1,1,1,n,n,1,n*f.route.n.parse(bytes.fromhex(f.route.b.frame(c['Words']).hex()))[5]])
 for i,sample in enumerate(want['Samples']):sample.update(CacheWidth=c['Width'],CacheRows=c['Rows'],Column=i%c['Width'],Row=i//c['Width'],AccountingPolicy='SharedPreparationCell',AccountedBytes=1696,AccountedWork=8288)
 return want

def generate(a):
 frozen=f.generate(a);single=[normalise(c) for c in frozen];base=dict(single[81]);extra=[]
 # Nearby points would meet the frozen hit epsilon if a single cache cell were reused.
 for p in (24,53,64):extra.append(dict(base,Width=3,Rows=3,OriginX=f.word(0),OriginZ=f.word(0),SpacingX=f.word(.0000001),SpacingZ=f.word(.0000001),Table=p,Construction=p,Query=p,Consumer=p))
 # An eligible first cell followed by coordinate-floor failure inside the batch query.
 w=[47,0,0,0,0,1,f.word(1),f.word(1),0x7f7fffff,f.word(1),0,0,3]
 extra.append(dict(base,GroupHex=f.route.group(-1,w).hex(),Words=w,FamilyId=-1,RuleHex=f.route.rule(-1),Operation=1,Width=2,Rows=2,OriginX=0,OriginZ=0,SpacingX=f.word(.125),SpacingZ=f.word(.125),LateQuery=True))
 # Norm construction rejection; strict flags/grammar remain exactly frozen.
 w=list(base['Words']);w[5]=2;w[7]=f.word(-1);extra.append(dict(base,GroupHex=f.route.group(base['FamilyId'],w).hex(),Words=w))
 for c in extra:c['Name']=str(len(single));single.append(c)
 return frozen,single
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for key in ('exe','asset-root','config','out'):p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args();assert a.out.resolve().is_relative_to(ROOT/'local-private')
 frozen,cases=generate(a);want=[expected(c) for c in cases];old=[f.expected(c) for c in frozen];a.out.mkdir(parents=True,exist_ok=True)
 accepted=[(c,e) for c,e in zip(cases,want) if e['Reject']=='None'];oldaccepted=[e for e in old if e['Reject']=='None'];n=sum(len(e['Heights']) for e in oldaccepted)
 assert len(frozen)==151 and n==2211 and all(e['Reject']==v['Reject'] for e,v in zip(old,want[:151]))
 for oldvalue,newvalue in zip(old,want[:151]):
  if oldvalue['Reject']=='None':assert all(oldvalue[k]==newvalue[k] for k in ('X','Z','Heights','Bounds','SourceSha256','RuleSha256')) and all(all(x[k]==y[k] for k in ('Retained','Projection','Height','X','Z','Origin','FamilyId')) for x,y in zip(oldvalue['Samples'],newvalue['Samples']))
 byte_saved=sum(f.account(c,len(bytes.fromhex(c['RuleHex'])),len(e['Heights']))[0]+f.account(c,len(bytes.fromhex(c['RuleHex'])),len(e['Heights']))[1]-e['LogicalCopyBytes']-e['AggregateRouteBytes'] for c,e in accepted)
 work_saved=sum(f.account(c,len(bytes.fromhex(c['RuleHex'])),len(e['Heights']))[2]-e['AccountedWork'] for c,e in accepted)
 summary=dict(Cases=len(cases),Rejected=sum(e['Reject']!='None' for e in want),Samples=sum(len(e.get('Heights',[])) for e in want),FrozenPatches=151,FrozenHeights=n,FrozenAccepted=len(oldaccepted),SamplerConstructions=len(accepted),DefaultSamplerConstructions=sum(len(e['Heights']) for _,e in accepted),SamplerQueryBatches=len(accepted),ComputedCells=sum(len(e['Heights']) for _,e in accepted),SavedLogicalBytes=byte_saved,SavedLogicalWork=work_saved,OriginalExecuted=False)
 assert byte_saved>0 and work_saved>0
 if a.native:
  native=json.loads(a.native.read_text(encoding='utf-8'));assert native['Default']['Cases']==old,'Frozen Build differs';assert native['Cases']==want,'Opt-in unique-cell patch differs';summary.update(Passed=True,OwnershipChecks=native['OwnershipChecks'],FrozenOwnershipChecks=native['Default']['OwnershipChecks']);(a.out/'single-receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 else:
  (a.out/'default-fixtures.json').write_text(json.dumps(frozen),encoding='utf-8');(a.out/'single-fixtures.json').write_text(json.dumps(cases),encoding='utf-8');(a.out/'single-expected.json').write_text(json.dumps(want),encoding='utf-8')
 print(json.dumps(summary))
