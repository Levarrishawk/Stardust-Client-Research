"""Conditional immediate noise -> retained consumers; original samples are unavailable."""
import importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,relative):
 s=importlib.util.spec_from_file_location(name,ROOT/relative);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
n=load('noise_join_seed','tools/python/verify_terrain_fractal_seed_noise.py')
j=load('noise_join_retained','poc/round47-tests/Integration/retained_fractal_join_peer.py')
def generate(assetroot,config,exe,runroot):
 assert Path.cwd().resolve()==ROOT
 private=Path(runroot).resolve();assert private.is_relative_to(ROOT/'local-private') and private!=ROOT/'local-private'
 private.mkdir(parents=True,exist_ok=True)
 n.gate(Path(exe).read_bytes(),n.EXE);n.setup()
 occurrence,mfrc=n.selected(Path(assetroot),Path(config),private)
 seed=occurrence['Words'][0]
 coords=[(-4096.75,-4096.25),(-4096.25,-4096.75),(-.125,.375),(.125,.375),(.375,.125),(.875,.625),(1.125,2.375),(128.125,-127.375),(255.125,256.375),(1024.125,-1023.375),(12345.25,-12000.75),(0,0),(1,1),(2**-30,2**-29),(-2**-30,2**-29),(.00001,-.000015)]
 cases=[];tables=[]
 for tp in (24,53,64):
  tables.append(dict(Precision=tp,**{k:v for k,v in n.tables(seed,tp)[3].items() if k not in ('FinalSeed','FinalIy')}))
  for qp in (24,53,64):
   for i,(x,z) in enumerate(coords):
    xb,zb=j.word(x),j.word(z);v,_=n.noise(xb,zb,seed,tp,qp)
    for hp in (24,53,64):
     for fs in (j.word(1),0x3f800001):
      c=dict(Name=f't{tp}-q{qp}-xy{i}-h{hp}-f{fs}',X=xb,Z=zb,TablePrecision=tp,NoisePrecision=qp,Sample=j.source(v),Precision=hp,QueryPrecision=53,CombinePrecision=64,ProductPrecision=24,Initial=j.word(-.25),Operation=i%5,Scale=j.word(15),FilterScale=fs,Selector=i%4,Weight=j.word(.75))
      c.update(RootHex=j.ah.encoded(1,c['Operation'],c['Scale']).hex(),FilterHex=j.ff.encoded(scale=fs,selector=c['Selector'],amount=j.word(.75)).hex(),ChildHex=j.ah.encoded(1,1,j.word(.5)).hex())
      cases.append(c)
 return dict(Version=1,SyntheticCoordinates=True,OriginalNoiseSamples=0,Occurrence=occurrence,MfrcHex=mfrc.hex(),Tables=tables,Cases=cases)
def expected(d):
 return [dict(Name=c['Name'],Sample=c['Sample'],Miss=j.oracle(c),Hit=j.oracle(c,True)) for c in d['Cases']]
def summary(d,e):
 return dict(Cases=len(e),UnsignedHighBitSamples=sum(c['Sample']['Magnitude']>0x7fffffffffffffff for c in d['Cases']),AhfrMissHitWitnesses=sum(r['Miss']['First']!=r['Hit']['First'] for r in e),FfraMissHitWitnesses=sum(r['Miss']['Scaled']!=r['Hit']['Scaled'] for r in e),FinalMissHitWitnesses=sum(r['Miss']['Final']!=r['Hit']['Final'] for r in e),OriginalNoiseSamples=0,OriginalTerrainParity=False)
if __name__=='__main__':
 action,assetroot,config,exe,runroot,fixture=sys.argv[1:7]
 d=generate(assetroot,config,exe,runroot);e=expected(d)
 if action=='generate':Path(fixture).write_text(json.dumps(d,indent=2)+'\n');print(json.dumps(summary(d,e)))
 elif action=='verify':
  assert json.loads(Path(fixture).read_text())==d,'Fresh original metadata/synthetic coordinate regeneration differs'
  actual=json.loads(Path(sys.argv[7]).read_text());assert actual['Words']==d['Occurrence']['Words'];assert actual['Tables']==d['Tables'];assert actual['Cases']==e,'Actual Core composition differs from independent arithmetic'
  result=dict(Passed=True,**summary(d,e));assert result['UnsignedHighBitSamples']>0 and result['AhfrMissHitWitnesses']>0
  Path(sys.argv[8]).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
 else:raise ValueError('generate/verify')
