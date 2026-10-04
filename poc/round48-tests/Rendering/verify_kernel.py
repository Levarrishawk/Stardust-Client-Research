import argparse,importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--frozen-fixtures',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--native',type=Path);a=p.parse_args()
if not a.out.resolve().is_relative_to((ROOT/'local-private').resolve()):raise ValueError('Private output required')
s=importlib.util.spec_from_file_location('frozen47_seed',ROOT/'tools/python/verify_terrain_fractal_seed_noise.py');o=importlib.util.module_from_spec(s);sys.modules[s.name]=o;s.loader.exec_module(o);o.setup()
source=json.loads(a.frozen_fixtures.read_text());fs=[]
for f in source:
 if f['Kind'] in ('Tables','Noise'):
  f=dict(f);f['TablePrecision']=f.get('TablePrecision',f['Precision']);fs.append(f)
# Additional independently authored signed seeds, separate controls, subnormal/signed-zero/negative/domain cases.
for seed in (47,12345,0xffffffff,0x80000000,0x7ffffffe,0x7fffffff):
 for tp in (24,53,64):
  fs.append(dict(Kind='Tables',Seed=seed,TablePrecision=tp,Precision=tp))
  for qp in (24,53,64):
   for x,z in ((1,0x80000001),(0x80000000,0),(0xc5800800,0xc5801000),(0x3f000001,0xb58637bd),(0xcf000010,0),(0x4f800000,0),(0x7fc12345,0)):
    fs.append(dict(Kind='Noise',Seed=seed,TablePrecision=tp,Precision=qp,X=x,Z=z))
# Locate one zero-gradient synthetic seed with bounded integer recurrence, then validate with the frozen Fraction RNG.
def has_zero(seed):
 state=seed;slots=[0]*322
 for j in range(329,-1,-1):
  state=state*16807%2147483647
  if j<322:slots[j]=state
 last=slots[0]
 def draw():
  nonlocal state,last
  state=state*16807%2147483647;idx=last//6669205;last,slots[idx]=slots[idx],state;return last
 for _ in range(256):
  draw();x=draw()%512-256;z=draw()%512-256
  if x==0 and z==0:return True
 return False
zero_seed=next(seed for seed in range(1,4097) if has_zero(seed))
for tp in (24,53,64):fs.append(dict(Kind='Tables',Seed=zero_seed,TablePrecision=tp,Precision=tp))
expected=[]
for i,f in enumerate(fs):
 f['Name']=str(i)
 if f['Seed']==0x7fffffff:e=dict(Name=str(i),Reject='Input')
 elif f['Kind']=='Tables':
  try:e=dict(Name=str(i),Reject='None',**{k:v for k,v in o.tables(f['Seed'],f['TablePrecision'])[3].items() if k not in ('FinalSeed','FinalIy')})
  except (ValueError,ZeroDivisionError):e=dict(Name=str(i),Reject='Input')
 else:
  e=o.evaluate(f)
  e={k:v for k,v in e.items() if k in ('Name','Reject','Retained','NegativeZero','Projection')}
 expected.append(e)
a.out.mkdir(parents=True,exist_ok=True)
if a.native:
 native=json.loads(a.native.read_text());assert len(native['Cases'])==len(expected)
 for f,want,got in zip(fs,expected,native['Cases']):
  for key,value in want.items():assert got[key]==value,(f,key,value,got)
 receipt=dict(Passed=True,Cases=len(fs),Rejected=sum(e['Reject']!='None' for e in expected),ZeroGradientSyntheticSeed=zero_seed,NativeNegativeChecks=native['NegativeChecks'],Unsigned64MagnitudeWitnesses=sum(c.get('Magnitude',0)>0x7fffffffffffffff for c in native['Cases']),FrozenOracle='Round47 Fraction/modular-integer graph',OriginalExecution=False)
 (a.out/'kernel-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8');print(json.dumps(receipt))
else:
 (a.out/'kernel-fixtures.json').write_text(json.dumps(fs),encoding='utf-8');(a.out/'kernel-expected.json').write_text(json.dumps(expected),encoding='utf-8');print(json.dumps(dict(Cases=len(fs),ZeroGradientSyntheticSeed=zero_seed)))
