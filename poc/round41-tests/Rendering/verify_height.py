"""Independent supplied-layer/AHCN composition oracle using frozen Fraction query and operation references."""
import argparse,copy,importlib.util,itertools,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
l=load('layer_reference','poc/round40-tests/Rendering/verify_layer.py');a=l.a
poly=load('polygon_reference','poc/round40-tests/Assets/verify_polygon.py')
# Retain the Fraction query before the frozen reference's inspection/selector section.
source=(ROOT/'poc/round40-tests/Assets/verify_polygon.py').read_text();body=source[source.index('def evaluate('):source.index('\ndef fixtures():')]
namespace=dict(poly.__dict__);exec(body.replace('def evaluate(', 'def query_number(').replace('  projection=store(u)','  return u'),namespace)
query_number=namespace['query_number'];original_query=l.query

def query(q,premature=False):
 if q is None or q['Type']!='Polygon':return original_query(q,premature)
 fixture=dict(CachePrecision=q['CachePrecision'],Precision=q['Precision'],Limits=poly.DEFAULT,Points=q['Points'],Version='0005',Action='query',Enabled=1,Amount=q['Amount'],Coordinates=q['Coordinates'],Selector=0)
 u=query_number(fixture)
 if isinstance(u,dict):raise IndexError('Options') if u['Reject']=='Options' else ValueError('polygon query')
 inspection=l.store(u)
 return (a.scalar(inspection) if premature else u),dict(Source=3,Precision=q['Precision'],RawBits=None,ProjectionBits=inspection)
l.query=query
DEFAULT=[256,256,65536,256,65536,327680];HARD=[65536,4096,1048576,4096,1048576,5242880]
def operation_charge(r):return 3 if r['Operation'] in (1,2) else 1 if r['Operation']==4 else 5

def evaluate(f,premature=False):
 name=f['Name'];limits=f.get('Limits',DEFAULT);maxrules=f.get('CreateMaximumRules',256);p=f['AffectorPrecision'];rules=f['Rules']
 if p not in (24,53,64) or not 0<=maxrules<=4096 or any(not 0<=v<=m for v,m in zip(limits,HARD)):return dict(Name=name,Reject='Options')
 if len(rules)>maxrules or any(r is None for r in rules):return dict(Name=name,Reject='Input')
 charge=sum(operation_charge(r) for r in rules);n=len(f['Layer']['Boundaries'])+len(f['Layer']['Filters'])
 if limits[0]<1 or n>limits[1] or n>limits[2] or len(rules)>limits[3] or len(rules)>limits[4] or charge>limits[5]:return dict(Name=name,Reject='Input')
 layer=copy.deepcopy(f['Layer']);layer['Limits']=limits[:3];influence=l.evaluate(layer,premature)
 if influence['Reject']!='None':return dict(Name=name,Reject=influence['Reject'])
 height=f['Initial'];visits=[]
 if influence['AffectorEligible']:
  for index,r in enumerate(rules):
   reject,result,outcome=a.evaluate(r['Enabled'],r['Operation'],r['Constant'],height,influence['ProductBits'],p)
   if reject!='None':return dict(Name=name,Reject=reject)
   visits.append(dict(Index=index,Result=dict(InputHeightBits=height,InfluenceBits=influence['ProductBits'],ConstantHeightBits=r['Constant'],OperationBits=r['Operation'],EnabledBits=r['Enabled'],ResultHeightBits=result,Outcome=outcome,Precision=p)))
   height=result
 return dict(Name=name,Reject='None',Influence=influence,OperationCharge=charge,FinalHeightBits=height,FinalHeightIsFinite=a.finite(height),Visits=visits)

def rule(op,constant,enabled=1):
 raw=a.m.ahcn(h=a.m.header(enabled=enabled),raw=a.m.words([op,constant]));gen=a.m.encode(last=a.m.form(b'LYRS',a.m.layer(children=raw)))
 return dict(Operation=op,Constant=constant,Enabled=enabled,GeneratorHex=gen.hex())
def layer(b=None,f=None,inc=0x3f800000,p=24,pp=24,bg=1,fg=1,ib=False,iff=False):
 return dict(Name='layer',Boundaries=b or [],Filters=f or [],Incoming=inc,Combine=p,Product=pp,BoundaryGate=bg,FilterGate=fg,InvertBoundary=ib,InvertFilter=iff,CreateMaximumQueries=256)
def polygon(qp=53,cp=53):
 points=[[0,0],[l.fb(3),0],[l.fb(3),l.fb(2)],[0,l.fb(2)]]
 q=dict(Type='Polygon',Points=points,Coordinates=[l.fb(.2),l.fb(.7)],Amount=l.fb(.7),Precision=qp,CachePrecision=cp)
 f=dict(Version='0005',Enabled=1,Points=points,EncodedSelector=0,Amount=q['Amount']);q['RuleHex']=poly.encode(f);return q

def fixtures():
 out=[]
 def emit(layer,rules,initial=0,precision=24,limits=None,maxrules=256):
  f=dict(Name=str(len(out)),Layer=layer,Rules=rules,Initial=initial,AffectorPrecision=precision,CreateMaximumRules=maxrules)
  if limits is not None:f['Limits']=limits
  out.append(f)
 values=[0,0x80000000,1,0x80000001,0x3f000001,0x3f800001,0xbf800000,0x7f7fffff,0x7fc12345];ops=[0,1,2,3,4,5,0xffffffff]
 for i,old in enumerate(l.fixtures()):
  rs=[rule(ops[(i+j)%7],values[(i+2*j)%len(values)],[0,1,0xffffffff][(i+j)%3]) for j in range(3)]
  limits=DEFAULT.copy();limits[:3]=old.get('Limits',DEFAULT[:3]);emit(old,rs,values[i%len(values)],[24,53,64][i%3],limits)
 # All independent phase combinations and mixed carrier provenance in one sample.
 for cp,qp,p,pp,ap in itertools.product((24,53,64),repeat=5):
  for sel in (0,1,2,3,4,0xffffffff):
   pq=polygon(qp,cp);cq=dict(Type='Circle',Geometry=[0,0,l.fb(1)],Coordinates=[l.fb(.8),l.fb(.1)],Amount=l.fb(.7),Precision=qp,CachePrecision=cp);cq['RuleHex']=l.encode(cq)
   rq=dict(Type='Rectangle',Geometry=[0,0,l.fb(3),l.fb(2)],Coordinates=[l.fb(.2),l.fb(.7)],Amount=l.fb(.7),Precision=qp,CachePrecision=cp);rq['RuleHex']=l.encode(rq)
   b=[l.entry(q=pq,sel=sel),l.entry(q=cq,sel=(sel+1)%4),l.entry(q=rq,sel=sel)];fs=[l.entry(0x3f000001),l.entry(q=pq,sel=(sel+2)%4)]
   emit(layer(b,fs,p=p,pp=pp),[rule(1,0x3f800001),rule(3,0x3f000001),rule(2,0x3eaaaaab)],0x3f800001,ap)
 # Gates/products/source order, unused raw nonfinites and operation4 recovery.
 for p,pp,ap,inc,local,op,enabled in itertools.product((24,53,64),(24,53,64),(24,53,64),(0,0x80000000,0xbf800000,1,0x3f800000),(0,0x3f000000,0x40000000),ops,(0,1)):
  emit(layer([l.entry(local)],inc=inc,p=p,pp=pp,ib=(local==0x40000000)),[rule(op,0x7fc12345,enabled),rule(4,0x7fc12345)],0x7fc12345,ap)
 exact=[1,2,2,2,2,8];base=layer([l.entry(0x3f000000)],[l.entry(0x3f000000)])
 emit(base,[rule(1,0x3f800000),rule(3,0x3f800000)],limits=exact)
 for i in range(6):lim=exact.copy();lim[i]-=1;emit(base,[rule(1,0x3f800000),rule(3,0x3f800000)],limits=lim)
 for i,maxv in enumerate(HARD):
  for bad in (-1,maxv+1):lim=DEFAULT.copy();lim[i]=bad;emit(layer(),[],limits=lim)
 for bad in (0,23,25,52,54,63,65):emit(layer(),[],precision=bad)
 for cap in (-1,0,1,2,4097):emit(base,[rule(1,0),rule(4,0)],maxrules=cap)
 emit(base,[None]);emit(layer(),[],0x7fc12345)
 for order in itertools.permutations([rule(1,0x3f800001),rule(2,0x3f7fffff),rule(3,0x3f000001)]):emit(base,list(order),0x3f800001,53)
 emit(base,[rule(1,0x3f800000),rule(1,0x7f7fffff)],0x7f7fffff)
 for ap in (24,53,64):
  recovery=[rule(4,0x7fc12345),rule(1,0x3f800000)]
  emit(layer([l.entry(0x3f000000)]),recovery,0x7fc12345,ap)
  emit(layer([l.entry(0x3f000000)],inc=0xbf800000),recovery,0x7fc12345,ap)
  emit(layer([l.entry(0x40000000)],inc=0xbf800000,ib=True),recovery,0x7fc12345,ap)
 return out

def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--regression-fixtures',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('--native',type=Path);args=p.parse_args()
 assert 'local-private' in args.fixtures.parts and 'local-private' in args.receipt.parts
 static=load('layer_static','poc/round40-tests/Rendering/static_layer.py').verify(args.exe,args.packages)
 ahcn=a.static(args.exe);polygon_static=poly.static(args.exe,args.packages)
 fs=fixtures();expected=[evaluate(f) for f in fs];regression=l.fixtures();witnesses=sum(evaluate(f,True)!=e for f,e in zip(fs,expected));assert witnesses>0
 result=dict(Static=static,AhcnStatic=ahcn,PolygonStatic=polygon_static,Cases=len(fs),Accepted=sum(e['Reject']=='None' for e in expected),Rejected=sum(e['Reject']!='None' for e in expected),PrematureProjectionWitnesses=witnesses,FrozenFactoryRegressionCases=len(regression),SyntheticOnly=True)
 args.fixtures.parent.mkdir(parents=True,exist_ok=True)
 if args.native:
  native=json.loads(args.native.read_text());assert len(native['Height']['Cases'])==len(expected)
  for f,want,got in zip(fs,expected,native['Height']['Cases']):assert want==got,(f,want,got)
  assert native['Height']['OwnershipChecks']>=52;result['OwnershipChecks']=native['Height']['OwnershipChecks']
  for f,got in zip(regression,native['FactoryRegression']['Cases']):assert l.evaluate(f)==got,(f,got)
  assert len(native['FactoryRegression']['Cases'])==len(regression) and native['FactoryRegression']['OwnershipChecks']==53
 else:
  args.fixtures.write_text(json.dumps(fs,separators=(',',':')));args.regression_fixtures.write_text(json.dumps(regression,separators=(',',':')))
 args.receipt.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()


