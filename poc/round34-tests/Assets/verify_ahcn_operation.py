"""Independent exact-rational nearest-even AHCN vectors and static/original identity audit."""
import collections,importlib.util,json,struct,sys
from fractions import Fraction
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
def module():
 p=REPO/'poc/round33-tests/Assets/verify_tgen_input.py';s=importlib.util.spec_from_file_location('tgen',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
m=module()
def finite(bits):return bits&0x7f800000!=0x7f800000
def scalar(bits):
 e=(bits>>23)&255;n=bits&0x7fffff
 if e:n|=0x800000;e=e-150
 else:e=-149
 value=Fraction(n)*(Fraction(2)**e);return (-value if bits&0x80000000 else value,bool(bits&0x80000000) and n==0)
def neg(x):return (-x[0],not x[1] if x[0]==0 else False)
def add(a,b):return (a[0]+b[0],a[0]==b[0]==0 and a[1] and b[1])
def mul(a,b):return (a[0]*b[0],(a[0]==0 or b[0]==0) and ((a[0]<0 or a[1])!=(b[0]<0 or b[1])))
def floor(x):
 n=x.numerator;d=x.denominator;e=n.bit_length()-d.bit_length()
 return e-1 if x<Fraction(2)**e else e
def integer_even(x):
 q,r=divmod(x.numerator,x.denominator);return q+(2*r>x.denominator or (2*r==x.denominator and q%2==1))
def precision(x,p):
 if x[0]==0:return x
 sign=-1 if x[0]<0 else 1;v=abs(x[0]);q=Fraction(2)**(floor(v)-(p-1));return (sign*integer_even(v/q)*q,False)
def bits(x):
 if x[0]==0:return 0x80000000 if x[1] else 0
 sign=0x80000000 if x[0]<0 else 0;v=abs(x[0]);e=floor(v);quantum=max(-149,e-23);n=integer_even(v/(Fraction(2)**quantum))
 if n==0:return sign
 actual=quantum+n.bit_length()-1
 if actual>127:return sign|0x7f800000
 if actual< -126:return sign|n
 significand=n>>(n.bit_length()-24) if n.bit_length()>24 else n<<(24-n.bit_length())
 return sign|((actual+127)<<23)|(significand&0x7fffff)
def evaluate(enabled,op,c,b,w,p):
 if p not in (24,53,64):return 'Options',None,None
 if not enabled:return 'None',b,1
 if not finite(w):return 'Input',None,None
 if w&0x80000000 or w&0x7fffffff==0:return 'None',b,2
 if op==4:return 'None',0,3
 if not finite(b) or not finite(c):return 'Input',None,None
 B,W,C=map(scalar,(b,w,c));A=lambda x,y:precision(add(x,y),p);M=lambda x,y:precision(mul(x,y),p)
 if op==1:r=A(M(W,C),B)
 elif op==2:r=A(B,neg(M(W,C)))
 elif op==3:r=A(B,M(A(M(B,C),neg(B)),W))
 else:r=A(M(A(scalar(0x3f800000),neg(W)),B),M(W,C))
 out=bits(r);return ('None',out,0) if finite(out) else ('Input',None,None)
def rule(enabled,op,c):return m.ahcn(h=m.header(enabled=enabled),raw=m.words([op,c]))
def fixtures():
 out=[]
 def add_case(n,enabled,op,c,b,w,p):
  reject,result,why=evaluate(enabled,op,c,b,w,p);out.append(dict(Name=n,RuleHex=rule(enabled,op,c).hex().upper(),EnabledBits=enabled,OperationBits=op,HeightBits=c,InputHeightBits=b,InfluenceBits=w,Precision=p,Reject=reject,ResultHeightBits=result,Outcome=why))
 finite_pairs=[(0,0),(0x80000000,0x80000000),(0x3f800000,0x80000000),(0xbf800000,0x3f800000),(0x3eaaaaab,0x3f000001),(0x4b800001,0x3f800001),(0x7f7fffff,0x7f7fffff),(0xff7fffff,0x7f7fffff),(1,1),(0x80000001,1),(0x00800000,0x3f000000),(0x3f800000,0x7f7fffff)]
 influences=[0,0x80000000,0xbf800000,1,0x3eaaaaab,0x3f000000,0x3f800000,0x3f800001,0x40000000,0x7f7fffff]
 for p in (24,53,64):
  for op in (0,1,2,3,4,5,0xffffffff):
   for b,c in finite_pairs:
    for w in influences:add_case(f'grid-{p}-{op}-{b}-{c}-{w}',0xffffffff,op,c,b,w,p)
 for bad in (0x7f800000,0xff800000,0x7fc12345,0x7f800001):
  for enabled in (0,1):
   for op in (0,1,3,4,0x80000000):
    for w in (0,0x80000000,0xbf800000,0x3f800000,bad):add_case(f'nonfinite-{bad}-{enabled}-{op}-{w}',enabled,op,bad,bad,w,24)
 for p in (0,23,25,52,54,63,65):add_case('bad-precision-'+str(p),0,0,0,0,0,p)
 # Literal counterexamples distinguish intermediates and prevent an endpoint shortcut.
 for p in (24,53,64):
  for b,c,w in [(0x3f800000,0x80000000,0x3f800000),(0x3f800001,0x3f7fffff,0x3f000001),(0x4b800001,0x3f800001,0x3f000000)]:
   for op in (0,1,2,3):add_case(f'rounding-{p}-{op}-{b}-{c}-{w}',1,op,c,b,w,p)
 return out

def original(root,cfg):
 o=m.original(root,cfg);rules=[r['HeightConstant'] for r in o['Metadata']['Rules'] if r['HeightConstant'] is not None];groups=collections.defaultdict(list)
 for r in rules:groups[r['OperationBits']].append(r)
 return dict(Root=o['Root'],Configuration=o['Configuration'],ConfigSha256=o['ConfigSha256'],Path=o['Path'],Priority=o['Priority'],SourceBytes=o['SourceBytes'],SourceSha256=o['SourceSha256'],GeneratorSha256=o['Metadata']['GeneratorSha256'],HeightConstants=len(rules),Operations=[dict(OperationBits=k,Count=len(v),EnabledCount=sum(r['Header']['EnabledBits']!=0 for r in v),FiniteHeightCount=sum(finite(r['HeightBits']) for r in v)) for k,v in sorted(groups.items())])
def static(exe):
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 assert m.sha(exe.read_bytes())==m.EXE;pe=pefile.PE(str(exe),fast_load=True);b=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 ins=list(md.disasm(b[0xb76540-base:0xb76620-base],0xb76540));lookup={i.address:(i.mnemonic,i.op_str) for i in ins}
 expected={0xb76547:('fcomp','dword ptr [0x15dc6a0]'),0xb7654f:('test','ah, 0x41'),0xb76552:('jne','0xb7661c'),0xb76563:('dec','eax'),0xb76564:('cmp','eax, 3'),0xb76572:('ja','0xb765e8'),0xb76583:('fmul','dword ptr [ecx + 0x18]'),0xb7658e:('fadd','dword ptr [eax + edx*4]'),0xb7659b:('fmul','dword ptr [ecx + 0x18]'),0xb765a6:('fsubr','dword ptr [eax + edx*4]'),0xb765ca:('fmul','dword ptr [ecx + 0x18]'),0xb765cd:('fsub','st(1)'),0xb765cf:('fmul','dword ptr [ebp + 0x18]'),0xb765d2:('faddp','st(1)'),0xb765db:('call','0xaa1d80'),0xb765e0:('fld','dword ptr [ebp - 4]'),0xb765f3:('fsub','dword ptr [ebp + 0x18]'),0xb765fe:('fmul','dword ptr [eax + edx*4]'),0xb76604:('fmul','dword ptr [ecx + 0x18]'),0xb76607:('faddp','st(1)'),0xb76618:('fstp','dword ptr [edx + ecx*4]')}
 for address,pair in expected.items():assert lookup[address]==pair
 assert sum(i.mnemonic=='fstp' for i in ins)==1 and not any(i.mnemonic=='fldcw' for i in ins)
 assert struct.unpack_from('<4I',b,0xb76624-base)==(0xb7657b,0xb76593,0xb765ab,0xb765d6)
 assert struct.unpack_from('<I',b,0x15dc6a0-base)[0]==0 and struct.unpack_from('<I',b,0x15dc6f8-base)[0]==0x3f800000
 assert b[0x18b8a1c-base:0x18b8a1c-base+18]==b'invalid operation\0'
 caller=list(md.disasm(b[0xb64160-base:0xb641c2-base],0xb64160));anchor={i.address:(i.mnemonic,i.op_str) for i in caller}
 for address,pair in {0xb64166:('call','0xb62f20'),0xb6416d:('je','0xb641c1'),0xb64175:('fmul','dword ptr [ebp - 0x2c]'),0xb6417c:('fstp','dword ptr [esp]'),0xb6418c:('call','dword ptr [edx + 0x10]')}.items():assert anchor[address]==pair
 install=list(md.disasm(b[0xaa7b00-base:0xaa7bc5-base],0xaa7b00));assert any(i.address==0xaa7b1d and i.op_str=='eax, 0xf0ff' for i in install)
 return dict(ExecutableSha256=m.EXE,Process='00B76540',ProcessAnchors=len(expected),CallerAnchors=5,OneFinalF32Store=True,JumpTargets=['00B7657B','00B76593','00B765AB','00B765D6'],StartupNearest24Candidate=True,ActiveTerrainThreadControlWord='Unproven')

if __name__=='__main__':
 if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('AHCN vectors:',len(fixtures()))
 else:
  root,cfg,log=map(Path,sys.argv[1:]);lines=log.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:');r=json.loads(lines[0].split('=',1)[1]);fs=fixtures();assert len(fs)==len(r['Cases'])
  for f,a in zip(fs,r['Cases']):
   assert a['Name']==f['Name'] and a['Reject']==f['Reject']
   if f['Reject']=='None':assert a['Result']['ResultHeightBits']==f['ResultHeightBits'] and a['Result']['Outcome']==f['Outcome'] and a['Result']['Precision']==f['Precision']
  assert r['BatchChecks']==10 and r['Original']==original(root,cfg)
  # Explicitly prove the fixtures distinguish the supplied arithmetic choices.
  profiles={}
  for f in fs:
   if f['Reject']=='None':profiles.setdefault((f['EnabledBits'],f['OperationBits'],f['HeightBits'],f['InputHeightBits'],f['InfluenceBits']),set()).add(f['ResultHeightBits'])
  differences=sum(len(v)>1 for v in profiles.values());assert differences>0
  print(json.dumps(dict(passed=True,vectors=len(fs),accepted=sum(f['Reject']=='None' for f in fs),rejected=sum(f['Reject']!='None' for f in fs),precision_sensitive_inputs=differences,batch_checks=10,static=static(root/'Stardust.exe'),original=r['Original']),indent=2))
