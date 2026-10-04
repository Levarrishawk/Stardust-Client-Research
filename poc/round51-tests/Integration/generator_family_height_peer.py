"""Original owned TGEN family publication to actual routed AHFR under explicit profiles."""
import contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('generator_family_r50',ROOT/'poc/round50-tests/Integration/family_sampler_join_peer.py');r=importlib.util.module_from_spec(s);s.loader.exec_module(r)
def generator(assetroot,config):
 family=r.n.family;m=family.load('generator_family_tgen','poc/round33-tests/Assets/verify_tgen_input.py')
 family.gate(Path(config).read_bytes(),family.CONFIG)
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(Path(assetroot),Path(config)):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 family.gate(payload,family.SOURCE);assert priority==42
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);g=payload[section[2]:section[3]];family.gate(g,family.GENERATOR)
 return g

def generate(assetroot,config,exe):
 d=r.generate(assetroot,config,exe);g=generator(assetroot,config);group=bytes.fromhex(d['OriginalGroupHex']);assert g[6602:12235]==group
 frames={};cases=[]
 for c in d['Cases']:
  if c['Operation']==4:continue
  if not c['OriginalGroup'] and not c['Name'].endswith('-xy0'):continue
  raw=group if c['OriginalGroup'] else bytes.fromhex(c['MgrpHex']);key=r.sha(raw)
  if key not in frames:
   t=g if c['OriginalGroup'] else g[:6602]+raw+g[12235:]
   if not c['OriginalGroup']:
    t=t[:4]+struct.pack('>I',len(t)-8)+t[8:16]+struct.pack('>I',len(t)-20)+t[20:]
   frames[key]=dict(Name=key,Original=c['OriginalGroup'],GeneratorHex=t.hex(),GeneratorSha256=r.sha(t),GroupSha256=key,GroupBytes=len(raw),GroupOffset=6602,Entries=43 if c['OriginalGroup'] else 1)
  cases.append(dict(Name=c['Name'],Frame=key,Original=c['OriginalGroup'],RuleHex=c['RootHex'],Initial=c['Initial'],Influence=0x3f800000,X=c['X'],Z=c['Z'],TablePrecision=c['TablePrecision'],ConstructionPrecision=c['ConstructionPrecision'],QueryPrecision=c['NoisePrecision'],ConsumerPrecision=c['Precision'],Expected=dict(Name=c['Name'],Frame=key,FamilyId=1,Sample=c['Sample'],Height=r.j.oracle(c)['First'])))
 assert len(cases)==486 and len(frames)==12
 return dict(Version=1,Frames=list(frames.values()),Cases=cases,OriginalTerrainParity=False)

def summary(d):return dict(Cases=len(d['Cases']),Frames=len(d['Frames']),OriginalGroupCases=sum(c['Original'] for c in d['Cases']),SyntheticGroupCases=sum(not c['Original'] for c in d['Cases']),OriginalFamilies=43,Modes=6,OriginalTerrainParity=False)
if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6];assert Path.cwd().resolve()==ROOT
 for p in [fixture]+(sys.argv[6:8] if action=='verify' else []):assert Path(p).resolve().is_relative_to(ROOT/'local-private')
 d=generate(assetroot,config,exe)
 if action=='generate':Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print(json.dumps(summary(d)))
 elif action=='verify':
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  actual=json.loads(Path(sys.argv[6]).read_text(encoding='utf-8'));assert actual['Cases']==[c['Expected'] for c in d['Cases']]
  assert actual['Frames']==[{k:f[k] for k in ('Name','GeneratorSha256','GroupSha256','GroupBytes','GroupOffset','Entries')} for f in d['Frames']]
  result=dict(Passed=True,**summary(d));Path(sys.argv[7]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('generate/verify')
