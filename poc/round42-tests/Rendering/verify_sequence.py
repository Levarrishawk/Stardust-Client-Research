"""Independent supplied flat sequence oracle: frozen Fraction height model, explicit ordering and complete reservations."""
import argparse,copy,itertools,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'poc/round41-tests/Rendering'))
import verify_height as h
DEFAULT=[256,64,16384,256,65536,256,65536,327680,131328];HARD=[65536,4096,65536,4096,1048576,4096,1048576,5242880,2101248]
def descriptor(f):
 if f is None:raise ValueError('null descriptor')
 layer=f['Layer'];entries=layer['Boundaries']+layer['Filters']
 for entry in entries:h.l.query(entry['Query'])
 if layer['Combine'] not in (24,53,64) or layer['Product'] not in (24,53,64) or not 0<=layer['CreateMaximumQueries']<=4096:raise IndexError('Options')
 if len(entries)>layer['CreateMaximumQueries'] or not h.a.finite(layer['Incoming']) or any(e['Enabled'] and e['Query'] is None for e in entries):raise ValueError('layer input')
 if f['AffectorPrecision'] not in (24,53,64) or not 0<=f['CreateMaximumRules']<=4096:raise IndexError('Options')
 if len(f['Rules'])>f['CreateMaximumRules'] or any(r is None for r in f['Rules']):raise ValueError('rules')
 return len(entries),len(f['Rules']),sum(h.operation_charge(r) for r in f['Rules'])
def evaluate(f,premature=False):
 name=f['Name'];layers=f['Layers'];limits=f.get('Limits',DEFAULT);maxlayers=f.get('CreateMaximumLayers',64)
 try:
  counts=[descriptor(layer) for layer in layers]
  if not 0<=maxlayers<=4096:raise IndexError('Options')
  if len(layers)>maxlayers:raise ValueError('plan cap')
  if any(not 0<=v<=hard for v,hard in zip(limits,HARD)):raise IndexError('Options')
  queries=sum(q for q,r,o in counts);rules=sum(r for q,r,o in counts);operations=sum(o for q,r,o in counts);largest=max((r for q,r,o in counts),default=0)
  charge=dict(Layers=len(layers),Queries=queries,Rules=rules,Operations=operations,RuleReferenceSlots=2*rules+largest)
  if limits[0]<1 or len(layers)>limits[1] or len(layers)>limits[2] or any(q>limits[3] or r>limits[5] for q,r,o in counts) or queries>limits[4] or rules>limits[6] or operations>limits[7] or charge['RuleReferenceSlots']>limits[8]:raise ValueError('complete budget')
  height=f['Initial'];stages=[]
  for index,layer in enumerate(layers):
   child=copy.deepcopy(layer);child['Initial']=height;child['Limits']=[1,limits[3],limits[4],limits[5],limits[6],limits[7]]
   result=h.evaluate(child,premature)
   if result['Reject']!='None':return dict(Name=name,Reject=result['Reject'])
   stages.append(dict(Index=index,Receipt=result));height=result['FinalHeightBits']
  return dict(Name=name,Reject='None',Charge=charge,FinalHeightBits=height,FinalHeightIsFinite=h.a.finite(height),Stages=stages)
 except IndexError:return dict(Name=name,Reject='Options')
 except (ValueError,ZeroDivisionError):return dict(Name=name,Reject='Input')
def fixtures():
 out=[]
 def emit(layers,initial=0,limits=None,maxlayers=64):
  f=dict(Name=str(len(out)),Layers=layers,Initial=initial,CreateMaximumLayers=maxlayers)
  if limits is not None:f['Limits']=limits
  out.append(f)
 base=h.fixtures()
 for i,f in enumerate(base):
  limits=DEFAULT.copy();old=f.get('Limits',h.DEFAULT)
  for oldidx,newidx in ((0,0),(1,3),(2,4),(3,5),(4,6),(5,7)):limits[newidx]=old[oldidx]
  emit([f],f['Initial'],limits)
 def stage(layer,rules,p=24):return dict(Name='stage',Layer=layer,Rules=rules,AffectorPrecision=p,CreateMaximumRules=256)
 # Retained geometric queries across all independent phases, then heterogeneous ordered mutation.
 mixed=[f for f in base if any(e['Query'] is not None and e['Query']['Type']=='Polygon' for e in f['Layer']['Boundaries'])]
 for i,f in enumerate(mixed):
  p=[24,53,64][i%3];second=stage(h.layer([h.l.entry(0x3f000001)],inc=[0,0x80000000,0xbf800000,0x3f800000][i%4],p=p,pp=p),[h.rule(1,0x3f800001),h.rule(3,0x3f000001),h.rule(2,0x3eaaaaab)],p)
  third=stage(h.layer([h.l.entry(0x3f800000)],p=p,pp=p),[h.rule(0,0x3f7fffff),h.rule(1,0x80000000,0)],p)
  emit([f,second,third],0x3f800001)
 for p,pp,ap,inc,q in itertools.product((24,53,64),(24,53,64),(24,53,64),(0,0x80000000,0xbf800000,1,0x3f800000),(0,0x3f000000,0x40000000)):
  first=stage(h.layer([h.l.entry(q)],inc=inc,p=p,pp=pp,ib=q==0x40000000),[h.rule(4,0x7fc12345),h.rule(1,0x3f800000)],ap)
  second=stage(h.layer(p=p,pp=pp),[h.rule(4,0x7fc12345),h.rule(2,0x3f800001),h.rule(3,0x3f000001)],ap)
  emit([first,second],0x7fc12345)
 ordered=[stage(h.layer(),[h.rule(op,c)],53) for op,c in [(1,0x3f800001),(2,0x3f7fffff),(3,0x3f000001),(4,0)]]
 for order in itertools.permutations(ordered):emit(list(order),0x3f800001)
 pair=[stage(h.layer([h.l.entry(0x3f000000)],[h.l.entry(0x3f000000)]),[h.rule(1,0x3f800000),h.rule(3,0x40000000)])]*2
 exact=[1,2,2,2,4,2,4,16,10];emit(pair,0x3f800000,exact)
 for i in range(9):l=exact.copy();l[i]-=1;emit(pair,0x3f800000,l)
 for i,maximum in enumerate(HARD):
  for bad in (-1,maximum+1):l=DEFAULT.copy();l[i]=bad;emit([],limits=l)
 for cap in (-1,0,1,2,4097):emit(pair,maxlayers=cap)
 emit([None]);emit([],0x7fc12345);emit([],0x80000000)
 emit([ordered[0],stage(h.layer(),[h.rule(1,0x7f7fffff)])],0x7f7fffff)
 emit([stage(h.layer(),[h.rule(4,0)]),ordered[0]],0x7fc12345)
 return out

def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('--native',type=Path);args=p.parse_args()
 assert 'local-private' in args.fixtures.parts and 'local-private' in args.receipt.parts
 static=h.load('layer_static','poc/round40-tests/Rendering/static_layer.py').verify(args.exe,args.packages);ahcn=h.a.static(args.exe);polygon=h.poly.static(args.exe,args.packages)
 fs=fixtures();expected=[evaluate(f) for f in fs];witness=sum(evaluate(f,True)!=e for f,e in zip(fs,expected));assert witness>0
 result=dict(Static=static,AhcnStatic=ahcn,PolygonStatic=polygon,Cases=len(fs),Accepted=sum(e['Reject']=='None' for e in expected),Rejected=sum(e['Reject']!='None' for e in expected),PrematureProjectionWitnesses=witness,MultiLayerCases=sum(len(f['Layers'])>1 for f in fs),SyntheticOnly=True)
 args.fixtures.parent.mkdir(parents=True,exist_ok=True)
 if args.native:
  native=json.loads(args.native.read_text());assert len(native['Cases'])==len(expected)
  for f,want,got in zip(fs,expected,native['Cases']):assert want==got,(f,want,got)
  assert native['OwnershipChecks']>=35;result['OwnershipChecks']=native['OwnershipChecks']
 else:args.fixtures.write_text(json.dumps(fs,separators=(',',':')))
 args.receipt.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
