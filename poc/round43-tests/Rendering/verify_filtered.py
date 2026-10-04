"""Independent Fraction oracle for supplied current-height FHGT binding and frozen AHCN composition."""
import argparse,copy,itertools,json,random,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'poc/round42-tests/Rendering'))
import verify_sequence as seq
h=seq.h;l=h.l;a=h.a
fh=h.load('filtered_height_reference','poc/round42-tests/Assets/verify_height_filter.py')
source=(ROOT/'poc/round42-tests/Assets/verify_height_filter.py').read_text();body=source[source.index('def evaluate('):source.index('\ndef fixtures():')]
ns=dict(fh.__dict__);exec(body.replace('def evaluate(', 'def query_number(').replace('  projection=store(u)','  return u'),ns);query_number=ns['query_number']
old_query=l.query
def query(q,premature=False):
 if q is None or q['Type']!='HeightFilter':return old_query(q,premature)
 u=query_number(q['Fixture'])
 if isinstance(u,dict):raise IndexError('Options') if u['Reject']=='Options' else ValueError('FHGT input')
 projection=l.store(u)
 return (a.scalar(projection) if premature else u),dict(Source=4,Precision=q['Precision'],RawBits=None,ProjectionBits=projection)
l.query=query
DEFAULT=dict(Sequence=seq.DEFAULT,Work=196608,Slots=589824)
def filter(lo=0,hi=0x40800000,selector=0,amount=0x3f800000,enabled=1,version='0002'):
 return dict(Lower=lo,Upper=hi,Selector=selector if version=='0002' else 0,Amount=amount if version=='0002' else 0,Enabled=enabled,Version=version,Hex=fh.encoded(version,lo,hi,selector,amount,enabled).hex())
def stage(filters=None,rules=None,qp=53,cp=53,pp=53,ap=53,b=None,inc=0x3f800000,bg=1,fg=1,ib=False,iff=False):
 return dict(Name='stage',Layer=h.layer(b=b,inc=inc,p=cp,pp=pp,bg=bg,fg=fg,ib=ib,iff=iff),Filters=filters or [],Rules=rules or [],QueryPrecision=qp,AffectorPrecision=ap,CreateMaximumRules=256)
def evaluate(f,premature=False,stale=False):
 name=f['Name'];layers=f['Layers'];lim=f.get('Limits',DEFAULT);s=lim['Sequence']
 try:
  if not 0<=f['CreateMaximumLayers']<=4096 or any(not 0<=v<=hard for v,hard in zip(s,seq.HARD)) or not 0<=lim['Work']<=3145728 or not 0<=lim['Slots']<=9437184:raise IndexError('Options')
  if len(layers)>f['CreateMaximumLayers'] or len(layers)>s[1] or len(layers)>s[2] or s[0]<1:raise ValueError('layers')
  q=r=o=w=largest=0
  for st in layers:
   if st is None:raise ValueError('null layer')
   ly=st['Layer'];n=len(ly['Boundaries'])+len(st['Filters']);nr=len(st['Rules'])
   if st['QueryPrecision'] not in (24,53,64):raise IndexError('Options')
   seq.descriptor(st)
   if n>ly['CreateMaximumQueries'] or n>s[3] or nr>s[5]:raise ValueError('per-layer cap')
   q+=n;r+=nr;o+=sum(h.operation_charge(x) for x in st['Rules']);largest=max(largest,nr);w+=2*len(ly['Boundaries'])+3*len(st['Filters'])
   if q>s[4] or r>s[6] or o>s[7] or 2*r+largest>s[8] or w>lim['Work'] or 9*q>lim['Slots']:raise ValueError('aggregate cap')
  height=f['Initial'];stages=[]
  for index,st in enumerate(layers):
   ly=copy.deepcopy(st['Layer']);probe=copy.deepcopy(ly);probe.update(Incoming=0,Filters=[],FilterGate=0,InvertFilter=False,Limits=[1,s[3],s[4]])
   probed=l.evaluate(probe,premature)
   if probed['Reject']!='None':raise ValueError('boundary')
   accumulator=probed['LocalInfluenceBits'];stopped=False;bound=[];bindings=[]
   for i,inp in enumerate(st['Filters']):
    state=1 if not inp['Enabled'] else 2 if not probed['BoundaryPositive'] else 3 if ly['FilterGate']<=0 else 4 if stopped else 0
    fixture=dict(inp,Height=f['Initial'] if stale else height,Precision=st['QueryPrecision'],Limits=fh.QUERY,Action='query')
    qf=dict(Type='HeightFilter',Precision=st['QueryPrecision'],Fixture=fixture);projection=candidate=current=None
    if state==0:
     u,meta=query(qf,premature);projection=meta['ProjectionBits'];current=fixture['Height'];shaped=l.shape(u,inp['Selector'],ly['Combine']);candidate=l.store(shaped)
     if shaped[0]<a.scalar(accumulator)[0]:accumulator=candidate
     bound.append(dict(Enabled=True,Selector=inp['Selector'],Query=qf));stopped=a.scalar(accumulator)[0]==0
    elif not stopped and probed['BoundaryPositive'] and ly['FilterGate']>0:bound.append(dict(Enabled=False,Selector=inp['Selector'],Query=None))
    bindings.append(dict(Index=i,State=state,Height=current,Projection=projection,CandidateBits=candidate))
   ly['Filters']=bound
   child=copy.deepcopy(st);child.update(Layer=ly,Initial=height,Limits=[1,s[3],s[4],s[5],s[6],s[7]])
   result=h.evaluate(child,premature)
   if result['Reject']!='None':raise ValueError('child')
   stages.append(dict(Index=index,Receipt=result,Bindings=bindings));height=result['FinalHeightBits']
  return dict(Name=name,Reject='None',FinalHeightBits=height,FinalHeightIsFinite=a.finite(height),Stages=stages)
 except IndexError:return dict(Name=name,Reject='Options')
 except (ValueError,ZeroDivisionError,TypeError):return dict(Name=name,Reject='Input')
def fixtures():
 out=[]
 def emit(layers,initial=0x3f800000,limits=None,cap=64):
  f=dict(Name=str(len(out)),Layers=layers,Initial=initial,CreateMaximumLayers=cap)
  if limits is not None:f['Limits']=limits
  out.append(f)
 fb=l.fb
 # Crossed query/combine/product/affector profiles and later-stage height dependence.
 for qp,cp,pp,ap,selector in itertools.product((24,53,64),(24,53,64),(24,53,64),(24,53,64),range(5)):
  first=stage([filter(fb(-8),fb(8),1,fb(.5))],[h.rule(1,fb(.9))],qp,cp,pp,ap)
  second=stage([filter(0,fb(4),selector,fb(1))],[h.rule(1,fb(3)),h.rule(3,fb(.8))],qp,cp,pp,ap)
  emit([first,second],fb(.2))
 rng=random.Random(43003)
 for i in range(300):
  qp,cp,pp,ap=[rng.choice((24,53,64)) for j in range(4)];height=fb(rng.uniform(.01,1.2));upper=fb(rng.uniform(2.5,5));amount=fb(rng.uniform(.6,1));sel=rng.randrange(4)
  emit([stage([filter(0,upper,sel,amount)],[h.rule(1,fb(rng.uniform(.2,4)))],qp,cp,pp,ap)],height)
 nan=0x7fc12345
 for qp,cp,pp,ap,version in itertools.product((24,53,64),(24,53,64),(24,53,64),(24,53,64),('0000','0001','0002')):
  emit([stage([filter(0,fb(4),3,fb(.7),version=version)],[h.rule(1,fb(1))],qp,cp,pp,ap)],fb(.2))
 for fg,bg,ib,iff,enabled,height in itertools.product((-1,0,1),(-1,0,1),(False,True),(False,True),(0,1),(0x3f800000,nan)):
  emit([stage([filter(nan,nan,0,nan,enabled)],[h.rule(4,0)],fg=fg,bg=bg,ib=ib,iff=iff,b=[l.entry(0)],inc=0xbf800000)],height)
 for sel in (0,4,0xffffffff):
  emit([stage([filter(0,fb(4),sel),filter(nan,nan,0,nan),filter(nan,nan,0,nan,0)],[h.rule(1,fb(1))])],0)
 # Complete reservations fail before a nonfinite late query; exact cap succeeds.
 pair=[stage([filter()],[h.rule(1,fb(1))],b=[l.entry(fb(1))])]*2
 exact=dict(Sequence=[1,2,2,2,4,1,2,6,5],Work=10,Slots=36);emit(pair,limits=exact)
 for i in range(9):v=copy.deepcopy(exact);v['Sequence'][i]-=1;emit(pair,limits=v)
 for k in ('Work','Slots'):v=copy.deepcopy(exact);v[k]-=1;emit(pair,limits=v)
 for i,hard in enumerate(seq.HARD):
  for n in (-1,hard+1):v=copy.deepcopy(DEFAULT);v['Sequence'][i]=n;emit([],limits=v)
 for k,hard in [('Work',3145728),('Slots',9437184)]:
  for n in (-1,hard+1):v=copy.deepcopy(DEFAULT);v[k]=n;emit([],limits=v)
 for cap in (-1,0,1,4097):emit(pair,cap=cap)
 for p in (0,25,65):st=stage();st['QueryPrecision']=p;emit([st])
 emit([None]);emit([],nan);emit([],0x80000000)
 emit([stage([], [h.rule(4,0)]),stage([filter()],[h.rule(1,fb(1))])],nan)
 emit([stage([filter()],[h.rule(1,0x7f7fffff)]),stage([filter()])],0x7f7fffff)
 return out

def main():
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--regression',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('--native',type=Path);args=p.parse_args()
 for path in (args.fixtures,args.regression,args.receipt):assert 'local-private' in path.parts
 static=h.load('filtered_static_layer','poc/round40-tests/Rendering/static_layer.py').verify(args.exe,args.packages);hs=fh.static(args.exe,args.packages);ahcn=a.static(args.exe)
 fs=fixtures();expected=[evaluate(f) for f in fs];stale=sum(evaluate(f,stale=True)!=e for f,e in zip(fs,expected));premature=sum(evaluate(f,True)!=e for f,e in zip(fs,expected));assert stale>0 and premature>0,(stale,premature)
 stale_final=sum(evaluate(f,stale=True).get('FinalHeightBits')!=e.get('FinalHeightBits') for f,e in zip(fs,expected));projection_final=sum(evaluate(f,True).get('FinalHeightBits')!=e.get('FinalHeightBits') for f,e in zip(fs,expected));assert stale_final>0 and projection_final>0,(stale_final,projection_final)
 regression=l.fixtures();old=[l.evaluate(f) for f in regression]
 result=dict(Static=static,HeightFilterStatic=hs,AhcnStatic=ahcn,Cases=len(fs),Accepted=sum(e['Reject']=='None' for e in expected),Rejected=sum(e['Reject']!='None' for e in expected),StaleInitialHeightWitnesses=stale,PrematureProjectionWitnesses=premature,StaleFinalHeightWitnesses=stale_final,PrematureProjectionFinalHeightWitnesses=projection_final,RegressionCases=len(old),SyntheticOnly=True)
 args.fixtures.parent.mkdir(parents=True,exist_ok=True)
 if args.native:
  native=json.loads(args.native.read_text());assert len(native['Cases'])==len(expected)
  for f,want,got in zip(fs,expected,native['Cases']):assert want==got,(f,want,got)
  assert native['OwnershipChecks']>=35;result['OwnershipChecks']=native['OwnershipChecks'];assert native['Regression']['Cases']==old;assert native['Regression']['OwnershipChecks']>=53;result['RegressionOwnershipChecks']=native['Regression']['OwnershipChecks']
 else:args.fixtures.write_text(json.dumps(fs,separators=(',',':')));args.regression.write_text(json.dumps(regression,separators=(',',':')))
 args.receipt.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
