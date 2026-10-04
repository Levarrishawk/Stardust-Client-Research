"""One owned original MGRP -> opt-in sampler modes -> retained consumers; explicit controls."""
import contextlib,hashlib,importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
prior=load('family_join49','poc/round49-tests/Integration/sampler_consumer_join_peer.py');n=prior.n;j=prior.j
sha=lambda value:hashlib.sha256(value).hexdigest().upper()
def original_group(assetroot,config):
 family=n.family;family.gate(Path(config).read_bytes(),family.CONFIG);m=family.load('family_join_tgen','poc/round33-tests/Assets/verify_tgen_input.py')
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(Path(assetroot),Path(config)):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
 family.gate(payload,family.SOURCE);assert priority==42 and len(payload)==1188446
 version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);g=payload[section[2]:section[3]];family.gate(g,family.GENERATOR)
 gv,=m.records(g,12,len(g));groups=list(m.records(g,gv[2]+12,gv[3]));mg=groups[4];assert mg[1][:4]==b'MGRP' and mg[2]==6602 and mg[3]-mg[2]==5633
 raw=g[mg[2]:mg[3]];mv,=m.records(raw,12,len(raw));assert mv[1][:4]==b'0000'
 entries=[]
 for record in m.records(raw,mv[2]+12,mv[3]):
  assert record[1][:4]==b'MFAM';parts=list(m.records(raw,record[2]+12,record[3]));assert len(parts)==2 and parts[0][0]==b'DATA' and parts[1][1][:4]==b'MFRC'
  data=parts[0][1];assert len(data)>=5 and data[-1]==0 and 0 not in data[4:-1]
  mf=raw[parts[1][2]:parts[1][3]];words=n.parse(mf)
  entries.append(dict(Id=struct.unpack_from('<i',data)[0],NameSha256=sha(data[4:-1]),NameBytes=len(data)-5,MfrcSha256=sha(mf),Words=words,Offset=record[2],Bytes=record[3]-record[2]))
 first=entries[0];assert first['Id']==1 and first['MfrcSha256']==n.MFRC
 return raw,entries

def form(tag,body):return b'FORM'+struct.pack('>I',len(body)+4)+tag+body
def mode_sample(words,tp,cp,qp,xb,zb):
 a=j.a;store=j.l.store;scalar=a.scalar;one=scalar(0x3f800000);zero=scalar(0);mode=words[12]
 add=lambda x,y,p:a.precision(a.add(x,y),p)
 mul=lambda x,y,p:a.precision(a.mul(x,y),p)
 weight,total=one,zero
 for _ in range(words[5]):total=add(total,weight,cp);weight=mul(weight,scalar(words[7]),cp)
 norm=store(a.precision((one[0]/scalar(store(total))[0],False),cp))
 x=store(add(scalar(store(mul(scalar(xb),scalar(words[8]),qp))),scalar(words[10]),qp))
 z=store(add(scalar(store(mul(scalar(zb),scalar(words[9]),qp))),scalar(words[11]),qp))
 frequency=amplitude=0x3f800000;total=0
 for _ in range(words[5]):
  nx=store(mul(scalar(x),scalar(frequency),qp));nz=store(mul(scalar(z),scalar(frequency),qp));noise,_=n.noise(nx,nz,words[0],tp,qp)
  if mode==2:noise=add(one,(-abs(noise[0]),False),qp)
  elif mode==3:noise=(abs(noise[0]),False)
  elif mode in (4,5):
   noise=zero if noise[0]<0 else one if noise[0]>1 else noise
   if mode==4:noise=add(one,a.neg(noise),qp)
  total=store(add(mul(noise,scalar(amplitude),qp),scalar(total),qp))
  frequency=store(mul(scalar(frequency),scalar(words[6]),qp));amplitude=store(mul(scalar(amplitude),scalar(words[7]),qp))
 finish=mul(scalar(total),scalar(norm),qp)
 if mode in (0,1):finish=mul(add(finish,one,qp),scalar(0x3f000000),qp)
 return norm,prior.normalized(finish)

def generate(assetroot,config,exe):
 n.gate(Path(exe).read_bytes(),n.EXE);n.setup();group,entries=original_group(assetroot,config)
 # The native path consumes an original complete group, then separately labeled synthetic one-entry groups.
 # Names and all original parameter words remain only in ignored fixtures/output.
 family=n.family.load('family_join_records','poc/round33-tests/Assets/verify_tgen_input.py');mv,=family.records(group,12,len(group));first,=list(family.records(group,mv[2]+12,mv[3]))[:1]
 parts=list(family.records(group,first[2]+12,first[3]));data=group[parts[0][2]:parts[0][3]];raw=group[parts[1][2]:parts[1][3]];words=entries[0]['Words']
 assert words[1]==words[3]==words[12]==0 and 1<=words[5]<=16
 cases=[];coords=[(-.125,.375),(.125,.375),(.375,.125),(.875,.625),(1.125,2.375),(128.125,-127.375),(255.125,256.375),(.00001,-.000015)]
 for mode in range(6):
  for variant in ('original-parameters','synthetic-three-octave'):
   w=words.copy();w[12]=mode
   if variant!='original-parameters':w[5]=3;w[7]=0x3f800001
   encoded=raw[:32]+struct.pack('<13I',*w);synthetic=form(b'MGRP',form(b'0000',form(b'MFAM',data+encoded)))
   original=mode==0 and variant=='original-parameters'
   for tp in (24,53,64):
    for cp in (24,53,64):
     for qp in (24,53,64):
      for i,(x,z) in enumerate(coords):
       xb,zb=j.word(x),j.word(z);norm,value=mode_sample(w,tp,cp,qp,xb,zb)
       c=dict(Name=f'm{mode}-{variant}-t{tp}-c{cp}-q{qp}-xy{i}',Mode=mode,OriginalGroup=original,MgrpHex=None if original else synthetic.hex(),X=xb,Z=zb,TablePrecision=tp,ConstructionPrecision=cp,NoisePrecision=qp,Normalization=norm,Sample=value,Precision=qp,QueryPrecision=53,CombinePrecision=64,ProductPrecision=24,Initial=j.word(-.25),Operation=i%5,Scale=j.word(15),FilterScale=0x3f800001,Selector=i%4,Weight=j.word(.75))
       c.update(RootHex=j.ah.encoded(1,c['Operation'],c['Scale']).hex(),FilterHex=j.ff.encoded(scale=c['FilterScale'],selector=c['Selector'],amount=j.word(.75)).hex(),ChildHex=j.ah.encoded(1,1,j.word(.5)).hex());cases.append(c)
 return dict(Version=1,OriginalNoiseSamples=0,OriginalGroupSha256=sha(group),OriginalGroupBytes=len(group),OriginalGroupHex=group.hex(),Entries=entries,Cases=cases)
def expected(d):return [dict(Name=c['Name'],Normalization=c['Normalization'],Sample=c['Sample'],Miss=j.oracle(c),Hit=j.oracle(c,True)) for c in d['Cases']]
def summary(d,e):return dict(Cases=len(e),OriginalGroupCases=sum(c['OriginalGroup'] for c in d['Cases']),SyntheticGroupCases=sum(not c['OriginalGroup'] for c in d['Cases']),OriginalGroupFamilies=len(d['Entries']),OriginalGroupBytes=d['OriginalGroupBytes'],Modes=6,AhfrMissHitWitnesses=sum(r['Miss']['First']!=r['Hit']['First'] for r in e),FfraMissHitWitnesses=sum(r['Miss']['Scaled']!=r['Hit']['Scaled'] for r in e),FinalMissHitWitnesses=sum(r['Miss']['Final']!=r['Hit']['Final'] for r in e),OriginalTerrainParity=False)
if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6];assert Path.cwd().resolve()==ROOT
 for p in [fixture]+(sys.argv[6:8] if action=='verify' else []):assert Path(p).resolve().is_relative_to(ROOT/'local-private')
 d=generate(assetroot,config,exe);e=expected(d)
 if action=='generate':Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print(json.dumps(summary(d,e)))
 elif action=='verify':
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  actual=json.loads(Path(sys.argv[6]).read_text(encoding='utf-8'));assert actual['Entries']==d['Entries'] and actual['Cases']==e
  result=dict(Passed=True,**summary(d,e));assert result['FinalMissHitWitnesses']>0
  Path(sys.argv[7]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('generate/verify')
