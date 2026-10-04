"""Independent full mode0 octave -> retained consumers; controls remain conditional."""
import importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
n=load('sampler_join_noise','tools/python/verify_terrain_fractal_seed_noise.py')
j=load('sampler_join_consumers','poc/round47-tests/Integration/retained_fractal_join_peer.py')
def normalized(v):
 r=j.source(v);m=r['Magnitude'];e=r['Exponent']
 if not m:e=0
 else:
  while m%2==0:m//=2;e+=1
 return dict(Magnitude=m,Exponent=e,Negative=r['Negative'])
def sample(words,tp,cp,qp,xb,zb):
 a=j.a;store=j.l.store
 def add(x,y,p):return a.precision(a.add(x,y),p)
 def mul(x,y,p):return a.precision(a.mul(x,y),p)
 scalar=a.scalar;one=scalar(0x3f800000);zero=scalar(0)
 # 013E6DF8 adds a retained weight; 013E6DFE multiplies that retained weight.
 # Only the final sum and reciprocal have mandatory F32 stores.
 weight,total=one,zero
 for _ in range(words[5]):total=add(total,weight,cp);weight=mul(weight,scalar(words[7]),cp)
 norm=store(a.precision((one[0]/scalar(store(total))[0],False),cp))
 sx=store(mul(scalar(xb),scalar(words[8]),qp));sz=store(mul(scalar(zb),scalar(words[9]),qp))
 x=store(add(scalar(sx),scalar(words[10]),qp));z=store(add(scalar(sz),scalar(words[11]),qp))
 frequency=amplitude=0x3f800000;total=0
 for _ in range(words[5]):
  nx=store(mul(scalar(x),scalar(frequency),qp));nz=store(mul(scalar(z),scalar(frequency),qp))
  noise,_=n.noise(nx,nz,words[0],tp,qp)
  total=store(add(mul(noise,scalar(amplitude),qp),scalar(total),qp))
  frequency=store(mul(scalar(frequency),scalar(words[6]),qp));amplitude=store(mul(scalar(amplitude),scalar(words[7]),qp))
 finish=mul(add(mul(scalar(total),scalar(norm),qp),one,qp),scalar(0x3f000000),qp)
 return norm,normalized(finish)
def generate(assetroot,config,exe,runroot):
 private=Path(runroot).resolve();assert private.is_relative_to(ROOT/'local-private') and private!=ROOT/'local-private'
 n.gate(Path(exe).read_bytes(),n.EXE);n.setup();private.mkdir(parents=True,exist_ok=True)
 occurrence,raw=n.selected(Path(assetroot),Path(config),private);words=occurrence['Words']
 assert words[1]==words[3]==words[12]==0 and 1<=words[5]<=16,'Selected fresh original family must be eligible'
 variants=[('original',raw),('synthetic-three-octave',raw[:32]+struct.pack('<13I',*(words[:5]+[3]+words[6:7]+[0x3f800001]+words[8:]))) ]
 coords=[(-.125,.375),(.125,.375),(.375,.125),(.875,.625),(1.125,2.375),(128.125,-127.375),(255.125,256.375),(.00001,-.000015)]
 cases=[]
 for name,encoded in variants:
  w=list(struct.unpack('<13I',encoded[32:84]))
  for tp in (24,53,64):
   for cp in (24,53,64):
    for qp in (24,53,64):
     for i,(x,z) in enumerate(coords):
      xb,zb=j.word(x),j.word(z);norm,value=sample(w,tp,cp,qp,xb,zb)
      c=dict(Name=f'{name}-t{tp}-c{cp}-q{qp}-xy{i}',Variant=name,MfrcHex=encoded.hex(),X=xb,Z=zb,TablePrecision=tp,ConstructionPrecision=cp,NoisePrecision=qp,Normalization=norm,Sample=value,Precision=qp,QueryPrecision=53,CombinePrecision=64,ProductPrecision=24,Initial=j.word(-.25),Operation=i%5,Scale=j.word(15),FilterScale=0x3f800001,Selector=i%4,Weight=j.word(.75))
      c.update(RootHex=j.ah.encoded(1,c['Operation'],c['Scale']).hex(),FilterHex=j.ff.encoded(scale=c['FilterScale'],selector=c['Selector'],amount=j.word(.75)).hex(),ChildHex=j.ah.encoded(1,1,j.word(.5)).hex());cases.append(c)
 return dict(Version=1,SyntheticCoordinates=True,OriginalNoiseSamples=0,Occurrence=occurrence,MfrcHex=raw.hex(),Cases=cases)
def expected(d):return [dict(Name=c['Name'],Normalization=c['Normalization'],Sample=c['Sample'],Miss=j.oracle(c),Hit=j.oracle(c,True)) for c in d['Cases']]
def summary(d,e):return dict(Cases=len(e),OriginalFamilyCases=sum(c['Variant']=='original' for c in d['Cases']),SyntheticFamilyCases=sum(c['Variant']!='original' for c in d['Cases']),UnsignedHighBitSamples=sum(c['Sample']['Magnitude']>0x7fffffffffffffff for c in d['Cases']),AhfrMissHitWitnesses=sum(r['Miss']['First']!=r['Hit']['First'] for r in e),FfraMissHitWitnesses=sum(r['Miss']['Scaled']!=r['Hit']['Scaled'] for r in e),FinalMissHitWitnesses=sum(r['Miss']['Final']!=r['Hit']['Final'] for r in e),OriginalNoiseSamples=0,OriginalTerrainParity=False)
if __name__=='__main__':
 action,assetroot,config,exe,runroot,fixture=sys.argv[1:7]
 assert Path.cwd().resolve()==ROOT
 for path in [fixture]+(sys.argv[7:9] if action=='verify' else []):
  assert Path(path).resolve().is_relative_to(ROOT/'local-private'),'Ignored private fixture/native/receipt required'
 d=generate(assetroot,config,exe,runroot);e=expected(d)
 if action=='generate':Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print(json.dumps(summary(d,e)))
 elif action=='verify':
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  actual=json.loads(Path(sys.argv[7]).read_text(encoding='utf-8'));assert actual['Words']==d['Occurrence']['Words'];assert actual['Cases']==e,'Actual full sampler composition differs from independent arithmetic'
  result=dict(Passed=True,**summary(d,e));assert result['AhfrMissHitWitnesses']>0 and result['FinalMissHitWitnesses']>0
  Path(sys.argv[8]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('generate/verify')
