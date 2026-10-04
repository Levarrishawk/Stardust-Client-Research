"""MFRC raw and full-U64 supplied sample oracle; original words remain dynamically private."""
import hashlib,importlib.util,json,random,struct,sys
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
ah=load('raw48_ah','tools/python/verify_terrain_height_fractal.py');a=ah.a
LIMITS=dict(MaximumInputBytes=65536,MaximumInputs=65536,MaximumAggregateInputBytes=5505024,MaximumOwnedCopyBytes=8912896)
def encode(words):return b'FORM'+struct.pack('>I',76)+b'MFRCFORM'+struct.pack('>I',64)+b'0001DATA'+struct.pack('>I',52)+struct.pack('<13I',*words)
def raw_expected(blob,limits):
 if not (0<=limits['MaximumInputBytes']<=1048576 and 0<=limits['MaximumInputs']<=1048576 and 0<=limits['MaximumAggregateInputBytes']<=88080384 and 0<=limits['MaximumOwnedCopyBytes']<=142606336):return dict(Reject='Options')
 if limits['MaximumInputs']<1 or limits['MaximumInputBytes']<len(blob) or limits['MaximumAggregateInputBytes']<len(blob) or limits['MaximumOwnedCopyBytes']<136:return dict(Reject='Input')
 if len(blob)!=84 or blob[:4]!=b'FORM' or blob[8:16]!=b'MFRCFORM' or blob[20:28]!=b'0001DATA' or struct.unpack_from('>I',blob,4)[0]!=76 or struct.unpack_from('>I',blob,16)[0]!=64 or struct.unpack_from('>I',blob,28)[0]!=52:return dict(Reject='Input')
 words=list(struct.unpack_from('<13I',blob,32));signed=lambda w:w if w<1<<31 else w-(1<<32)
 return dict(Reject='None',Snapshot=dict(Words=words,Seed=signed(words[0]),Octaves=signed(words[5]),Curve1=words[1]!=0,Curve2=words[3]!=0,Finite=all(a.finite(words[i]) for i in [2,4,6,7,8,9,10,11]),Sha=hashlib.sha256(blob).hexdigest().upper()))
def wide_expected(c):
 m=c['Magnitude'];e=c['Exponent'];negative=c['Negative'];p=c['Precision']
 if not -512<=e<=512:return dict(Reject='Input')
 value=(Fraction(-m if negative else m)*Fraction(2)**e,negative and m==0);bits=a.bits(value)
 if not a.finite(bits):return dict(Reject='Input')
 if p not in (24,53,64):return dict(Reject='Options')
 add=lambda x,y:a.precision(a.add(x,y),p)
 mul=lambda x,y:a.precision(a.mul(x,y),p)
 h=a.scalar(c['Height']);t=mul(value,a.scalar(c['Scale']));op=c['Operation']
 if op==1:out=add(h,t)
 elif op==2:out=add(h,a.neg(t))
 elif op==3:out=add(h,add(mul(t,h),a.neg(h)))
 else:out=t
 out=a.bits(out)
 if not a.finite(out):return dict(Reject='Input')
 signed=-m if negative else m;signed=signed if -(1<<63)<=signed<(1<<63) else None
 return dict(Reject='None',Magnitude=m,IsNegative=negative,Significand=signed,NegativeZero=negative and m==0,ProjectionBits=bits,Provenance=1,Output=out,Cache=bits,CacheProvenance=2)
def synthetic(original):
 raw=[];wide=[]
 def add(blob,limits=None):
  limits=limits or LIMITS;raw.append(dict(Name='raw-'+str(len(raw)),Hex=blob.hex(),Limits=limits,Expected=raw_expected(blob,limits)))
 baseline=encode([0]*13);add(baseline)
 rng=random.Random(4801)
 for _ in range(128):add(encode([rng.getrandbits(32) for _ in range(13)]))
 for index in range(13):
  for word in [0xffffffff,0x80000000,0x7f800000,0x7fc12345,0x7f800001,1]:
   ws=[0]*13;ws[index]=word;add(encode(ws))
 for length in range(84):add(baseline[:length])
 for offset in [0,8,12,20,24]:
  b=bytearray(baseline);b[offset]^=1;add(b)
 for offset in [4,16,28]:
  for size in [0,1,51,53,0xffffffff]:
   b=bytearray(baseline);struct.pack_into('>I',b,offset,size);add(b)
 for tag in [b'0000',b'0002',b'xxxx']:
  b=bytearray(baseline);b[20:24]=tag;add(b)
 for tail in [b'\0',b'abcd',baseline]:add(baseline+tail)
 for key,value in [('MaximumInputBytes',83),('MaximumInputs',0),('MaximumAggregateInputBytes',83),('MaximumOwnedCopyBytes',135)]:add(baseline,dict(LIMITS,**{key:value}))
 for key,maxval in [('MaximumInputBytes',1048576),('MaximumInputs',1048576),('MaximumAggregateInputBytes',88080384),('MaximumOwnedCopyBytes',142606336)]:
  add(baseline,dict(LIMITS,**{key:-1}));add(baseline,dict(LIMITS,**{key:maxval+1}))
 add(original) # Dynamic SHA-gated selected payload; never embedded in this public source.
 mags=[0,1,(1<<63)-1,1<<63,(1<<63)+1,(1<<64)-2,(1<<64)-1,33554430,33554431]
 for m in mags:
  for negative in [False,True]:
   for e in [-2147483648,-513,-512,-149,-64,-63,0,63,64,103,104,127,128,512,513,2147483647]:
    for p in [24,53,64]:
     for op,scale,h in [(1,0x3f800000,0xbf800000),(2,0x3f800001,0x3f800000),(3,0x3f800000,0x3f000000)]:
      c=dict(Name='wide-'+str(len(wide)),Magnitude=m,Negative=negative,Exponent=e,Precision=p,Operation=op,Scale=scale,Height=h,Hex=ah.encoded(family=1,op=op,scale=scale).hex());c['Expected']=wide_expected(c);wide.append(c)
 return dict(Raw=raw,Wide=wide)
def generate(exe,packages,root,config,output):
 inspector=load('raw48_inspector','tools/python/verify_terrain_fractal_seed_noise.py');data=Path(exe).read_bytes();inspector.gate(data,inspector.EXE)
 static=inspector.static(data,Path(packages));inspector.setup();occ,original=inspector.selected(Path(root),Path(config),Path(output).parent)
 expected=raw_expected(original,LIMITS);assert expected['Snapshot']['Words']==occ['Words']==inspector.parse(original)
 fs=synthetic(original);Path(output).write_text(json.dumps(fs,indent=2)+'\n')
 # Full selected words/seed remain only in this ignored receipt.
 Path(output).with_name('original-inspector.json').write_text(json.dumps(dict(Static=static,Occurrence=occ,FreshRaw=expected),indent=2)+'\n')
 print(json.dumps(dict(RawCases=len(fs['Raw']),WideCases=len(fs['Wide']),OriginalMfrcSha256=occ['MfrcSha256'],StaticAnchors=static['InstructionAnchors'])))
def verify(fixture,native):
 fs=json.loads(Path(fixture).read_text());original=bytes.fromhex(fs['Raw'][-1]['Hex']);assert fs==synthetic(original)
 n=json.loads(Path(native).read_text());rejects={};full=0
 for key in ['Raw','Wide']:
  assert len(fs[key])==len(n[key]);rejects[key]=0
  for c,row in zip(fs[key],n[key]):
   expected=dict(Name=c['Name'],**c['Expected']);assert row==expected,(c['Name'],expected,row)
   rejects[key]+=row['Reject']!='None'
   if key=='Wide' and row['Reject']=='None' and c['Magnitude']>(1<<63)-1:full+=1
 assert n['OwnershipChecks']==10 and n['BudgetChecks']==5 and full>0
 print(json.dumps(dict(Raw=len(fs['Raw']),Wide=len(fs['Wide']),Rejects=rejects,FullU64Accepted=full,Ownership=n['OwnershipChecks'],Budgets=n['BudgetChecks'])))
if __name__=='__main__':
 if sys.argv[1]=='generate':generate(*sys.argv[2:7])
 else:verify(*sys.argv[2:4])
