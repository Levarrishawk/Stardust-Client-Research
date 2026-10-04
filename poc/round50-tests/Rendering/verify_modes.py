"""Independent finite mode composition; expected values never come from Core."""
import argparse,importlib.util,json,struct,sys
from pathlib import Path
from fractions import Fraction
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
b=load('frozen49_mode_base','poc/round49-tests/Rendering/verify_sampler.py');n=b.n
ANCHORS={
0x013E7DA9:('call','0x13e66f0'),0x013E7DAF:('fabs',''),0x013E7DB1:('fsubr','dword ptr [0x15dc6f8]'),0x013E7DB7:('fmul','dword ptr [ebp + 0x10]'),0x013E7DBA:('fadd','dword ptr [ebp - 8]'),0x013E7DBD:('fstp','dword ptr [ebp - 8]'),0x013E7DC6:('fstp','dword ptr [ebp + 0x14]'),0x013E7DCF:('fstp','dword ptr [ebp + 0x10]'),
0x013E7E1D:('call','0x13e66f0'),0x013E7E23:('fabs',''),0x013E7E2B:('fstp','dword ptr [ebp - 8]'),0x013E7E34:('fstp','dword ptr [ebp + 0x14]'),0x013E7E3D:('fstp','dword ptr [ebp + 0x10]'),
0x013E7E8B:('call','0x13e66f0'),0x013E7E90:('fcom','dword ptr [0x15dc6a0]'),0x013E7E98:('test','ah, 5'),0x013E7E9B:('jp','0x13e7ea7'),0x013E7E9F:('fld','dword ptr [0x15dc6a0]'),0x013E7EA7:('fcom','dword ptr [0x15dc6f8]'),0x013E7EAF:('test','ah, 0x41'),0x013E7EB2:('jne','0x13e7ebc'),0x013E7EB6:('fld','dword ptr [0x15dc6f8]'),0x013E7EBD:('fld','dword ptr [0x15dc6f8]'),0x013E7EC3:('fsub','st(1)'),0x013E7ECB:('fstp','dword ptr [ebp - 8]'),0x013E7ED6:('fstp','dword ptr [ebp + 0x14]'),0x013E7EDF:('fstp','dword ptr [ebp + 0x10]'),
0x013E7F2A:('call','0x13e66f0'),0x013E7F2F:('fcom','dword ptr [0x15dc6a0]'),0x013E7F37:('test','ah, 5'),0x013E7F3A:('jp','0x13e7f46'),0x013E7F3E:('fld','dword ptr [0x15dc6a0]'),0x013E7F46:('fcom','dword ptr [0x15dc6f8]'),0x013E7F4E:('test','ah, 0x41'),0x013E7F51:('jne','0x13e7f5b'),0x013E7F55:('fld','dword ptr [0x15dc6f8]'),0x013E7F5C:('fmul','dword ptr [ebp + 0x10]'),0x013E7F62:('fstp','dword ptr [ebp - 8]'),0x013E7F6B:('fstp','dword ptr [ebp + 0x14]'),0x013E7F74:('fstp','dword ptr [ebp + 0x10]'),0x013E7F8B:('fld','dword ptr [ebp - 8]'),0x013E7F8E:('fmul','dword ptr [esi + 0x20]')}
def static(args):
 data=args.exe.read_bytes();n.gate(data,n.EXE);sys.path.insert(0,str(args.packages));import pefile,capstone
 pe=pefile.PE(data=data);md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32)
 for va,want in ANCHORS.items():
  off=pe.get_offset_from_rva(va-pe.OPTIONAL_HEADER.ImageBase);i=next(md.disasm(data[off:off+15],va));assert (i.mnemonic,i.op_str)==want,(hex(va),i.mnemonic,i.op_str)
 old=load('mode_frozen46_static','tools/python/verify_terrain_fractal_sampler.py').static(data,args.packages)
 # Ordered comparisons under finite inputs. TEST AH,5 / JP keeps equal zero, including -0.
 for status,keep in ((0,True),(1,False),(0x40,True)):
  assert ((status&5).bit_count()%2==0)==keep
 for status,keep in ((0,False),(1,True),(0x40,True)):assert bool(status&0x41)==keep
 return dict(ModeAnchors=len(ANCHORS),Frozen46Anchors=old['InstructionAnchors'],ModeTargets=old['ModeTargets'],FiniteBranchChecks=6,OriginalExecuted=False)
def contribution(v,mode,p):
 add,_,_=n.arithmetic(p);absolute=(abs(v[0]),False);clamped=n.exact_int(0) if v[0]<0 else n.exact_int(1) if v[0]>1 else v
 return v if mode<=1 else add(n.exact_int(1),n.negate(absolute)) if mode==2 else absolute if mode==3 else add(n.exact_int(1),n.negate(clamped)) if mode==4 else clamped
class Model(b.Model):
 def __init__(self,c):
  w=n.parse(bytes.fromhex(c['Hex']));mode=w[12]
  if mode>5:raise ValueError('Unsupported mode')
  w0=list(w);w0[12]=0;cc=dict(c,Hex=b.frame(w0).hex());super().__init__(cc);self.words=w
 def miss(self,r,p):
  w=self.words;add,mul,_=n.arithmetic(p);x=b.stored(mul(n.finite_scalar(r['XBits']),n.finite_scalar(w[8])));z=b.stored(mul(n.finite_scalar(r['ZBits']),n.finite_scalar(w[9])))
  z=b.stored(add(n.finite_scalar(z),n.finite_scalar(w[11])));x=b.stored(add(n.finite_scalar(x),n.finite_scalar(w[10])))
  frequency=amplitude=0x3f800000;total=0
  for _ in range(self.octaves):
   nz=b.stored(mul(n.finite_scalar(z),n.finite_scalar(frequency)));nx=b.stored(mul(n.finite_scalar(x),n.finite_scalar(frequency)));noise,_=n.noise(nx,nz,w[0],self.c['TablePrecision'],p)
   total=b.stored(add(mul(contribution(noise,w[12],p),n.finite_scalar(amplitude)),n.finite_scalar(total)))
   frequency=b.stored(mul(n.finite_scalar(frequency),n.finite_scalar(w[6])));amplitude=b.stored(mul(n.finite_scalar(amplitude),n.finite_scalar(w[7])))
  value=mul(n.finite_scalar(total),n.finite_scalar(self.norm))
  return mul(add(value,n.exact_int(1)),n.finite_scalar(0x3f000000)) if w[12]<=1 else value

def generate(args):
 n.setup();occ,raw=n.selected(args.asset_root,args.config,args.out);cases=[]
 authored=[47,0,0x7fc12345,0,0x7f800000,3,0x3fc00000,0x3eb33333,0x3f000000,0x3e800000,0xbe800000,0x3f000000,0]
 def r(x,z,col=0,row=0):return dict(Column=col,Row=row,XBits=x,ZBits=z)
 def q(points,p,**kw):return dict(Kind='Query',Precision=p,Requests=points,**kw)
 negative=list(authored);negative[5]=2;negative[6]=0x3f800000;negative[7]=0xc0000000;negative[8]=negative[9]=0x3f800000;negative[10]=negative[11]=0
 for original,w0 in ((True,occ['Words']),(False,authored),(False,negative)):
  for mode in range(6):
   w=list(w0);w[12]=mode
   for tp in (24,53,64):
    for cp in (24,53,64):
     for p in (24,53,64):
      actions=[]
      for x,z in ((0,0),(0x80000000,0x80000000),(0x3f000001,0x3e800001),(0xbf000001,0xbe800001),(0xc5800800,0xc5801000),(1,0x80000001),(0x43800000,0xc37f0000)):
       point=r(x,z);actions.extend([q([point,point],p),q([r(x,z,1,1)],p),dict(Kind='Clear')])
      actions.extend([q([r(0,0)],p),q([r(0x3727c5ac,0)],p),q([r(0,0),r(0x7fc12345,0,1,0)],p),q([r(0,0)],p),q([r(0x4f800000,0)],p),q([r(0,0,2,0)],p),q([r(0,0)],p,Cancel=True),dict(Kind='Clear',Cancel=True)])
      cases.append(dict(Hex=b.frame(w).hex(),Width=2,Rows=2,TablePrecision=tp,ConstructionPrecision=cp,Actions=actions,Mode=mode,OriginalMetadata=original,SyntheticModeOverride=original and mode!=0))
 for mode in range(6):
  for index,value in ((1,1),(3,1),(5,0),(5,17),(6,0x7f800000),(7,0x7fc12345),(0,0x7fffffff)):
   w=list(authored);w[12]=mode;w[index]=value;cases.append(dict(Hex=b.frame(w).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53,Actions=[],Mode=mode))
 for mode in (6,0xffffffff):
  w=list(authored);w[12]=mode;cases.append(dict(Hex=b.frame(w).hex(),Width=2,Rows=2,TablePrecision=53,ConstructionPrecision=53,Actions=[],Mode=mode))
 for i,c in enumerate(cases):c['Name']=str(i)
 return dict(Occurrence=occ,Cases=cases)
if __name__=='__main__':
 p=argparse.ArgumentParser()
 for key in ('exe','packages','asset-root','config','out'):p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native',type=Path);a=p.parse_args()
 if not a.out.resolve().is_relative_to(ROOT/'local-private'):raise ValueError('Private output only')
 proof=static(a);d=generate(a);b.Model=Model;e=[b.expected(c) for c in d['Cases']];alias=0
 for index,c in enumerate(d['Cases']):
  if c['Mode']==0 and c['Actions']:
   other=d['Cases'][index+27];assert other['Mode']==1
   assert {k:v for k,v in e[index].items() if k!='Name'}=={k:v for k,v in e[index+27].items() if k!='Name'};alias+=1
 a.out.mkdir(parents=True,exist_ok=True)
 # Pure model boundaries complement static finite branch evidence; no injected kernel values.
 checks=0
 for p in (24,53,64):
  for v in (n.exact_int(-2),(Fraction(0),True),n.exact_int(0),n.exact_int(1),n.exact_int(2)):
   assert contribution(v,5,p)==(n.exact_int(0) if v[0]<0 else n.exact_int(1) if v[0]>1 else v);assert contribution(v,3,p)==(abs(v[0]),False);checks+=2
 summary=dict(Cases=len(e),FactoryRejects=sum(c['Reject']=='Input' for c in e),Actions=sum(len(c.get('Actions',[])) for c in e),Queries=sum(len(ac['Results']) for c in e for ac in c.get('Actions',[])),ActionRejects=sum(ac['Reject']=='Input' for c in e for ac in c.get('Actions',[])),MissHitWitnesses=sum(len(ac['Results'])==2 and ac['Results'][0]['Retained']!=ac['Results'][1]['Retained'] for c in e for ac in c.get('Actions',[])),NegativeZeroQueries=sum(r['NegativeZero'] for c in e for ac in c.get('Actions',[]) for r in ac['Results']),BoundaryModelChecks=checks,ModeOneAliasChecks=alias,Static=proof,OriginalNoiseSamples=0)
 if a.native:
  native=json.loads(a.native.read_text(encoding='utf-8'));assert native['Cases']==e,'Native modes differ from independent arithmetic';summary.update(Passed=True,NativeChecks=native['NegativeChecks']);(a.out/'mode-receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 else:(a.out/'mode-fixtures.json').write_text(json.dumps(d),encoding='utf-8');(a.out/'mode-expected.json').write_text(json.dumps(e),encoding='utf-8')
 print(json.dumps(summary))
