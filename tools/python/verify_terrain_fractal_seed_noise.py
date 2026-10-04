"""One pinned MFRC seed/table/finite-noise research chain; original binary is never executed."""
import argparse,contextlib,hashlib,importlib.util,json,random,struct,sys
from fractions import Fraction
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
EXE='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
MFRC='85A84EFBDF4EE507C0011C9E4D4ED130EBF4719735A521FD491ABD286EBD723E'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
MODULUS=2147483647;DIVISOR=6669205
ANCHORS={
 0x00B59EF1: ('mov', 'esi, ecx'),
 0x00B59EF3: ('mov', 'eax, dword ptr [esi]'),
 0x00B59EF5: ('test', 'eax, eax'),
 0x00B59EF7: ('jle', '0xb59f00'),
 0x00B59EF9: ('mov', 'ecx, dword ptr [esi + 4]'),
 0x00B59EFC: ('test', 'ecx, ecx'),
 0x00B59EFE: ('jne', '0xb59f6b'),
 0x00B59F00: ('neg', 'eax'),
 0x00B59F02: ('cmp', 'eax, 1'),
 0x00B59F05: ('jge', '0xb59f0f'),
 0x00B59F07: ('mov', 'dword ptr [esi], 1'),
 0x00B59F0F: ('mov', 'dword ptr [esi], eax'),
 0x00B59F13: ('mov', 'edi, 0x149'),
 0x00B59F1E: ('mov', 'edi, edi'),
 0x00B59F20: ('mov', 'ecx, dword ptr [esi]'),
 0x00B59F22: ('mov', 'eax, 0x834e0b5f'),
 0x00B59F27: ('imul', 'ecx'),
 0x00B59F29: ('add', 'edx, ecx'),
 0x00B59F2B: ('imul', 'ecx, ecx, 0x41a7'),
 0x00B59F31: ('sar', 'edx, 0x10'),
 0x00B59F34: ('mov', 'eax, edx'),
 0x00B59F36: ('shr', 'eax, 0x1f'),
 0x00B59F39: ('add', 'edx, eax'),
 0x00B59F3B: ('imul', 'edx, edx, 0x7fffffff'),
 0x00B59F41: ('sub', 'ecx, edx'),
 0x00B59F43: ('mov', 'dword ptr [esi], ecx'),
 0x00B59F45: ('jns', '0xb59f4f'),
 0x00B59F47: ('add', 'ecx, 0x7fffffff'),
 0x00B59F4D: ('mov', 'dword ptr [esi], ecx'),
 0x00B59F4F: ('cmp', 'edi, 0x142'),
 0x00B59F55: ('jge', '0xb59f5b'),
 0x00B59F57: ('mov', 'ecx, dword ptr [esi]'),
 0x00B59F59: ('mov', 'dword ptr [ebx], ecx'),
 0x00B59F5B: ('dec', 'edi'),
 0x00B59F5C: ('sub', 'ebx, 4'),
 0x00B59F5F: ('test', 'edi, edi'),
 0x00B59F61: ('jge', '0xb59f20'),
 0x00B59F63: ('mov', 'edx, dword ptr [esi + 8]'),
 0x00B59F67: ('mov', 'dword ptr [esi + 4], edx'),
 0x00B59F6B: ('mov', 'ecx, dword ptr [esi]'),
 0x00B59F6D: ('mov', 'eax, 0x834e0b5f'),
 0x00B59F72: ('imul', 'ecx'),
 0x00B59F74: ('add', 'edx, ecx'),
 0x00B59F76: ('imul', 'ecx, ecx, 0x41a7'),
 0x00B59F7C: ('sar', 'edx, 0x10'),
 0x00B59F7F: ('mov', 'eax, edx'),
 0x00B59F81: ('shr', 'eax, 0x1f'),
 0x00B59F84: ('add', 'edx, eax'),
 0x00B59F86: ('imul', 'edx, edx, 0x7fffffff'),
 0x00B59F8C: ('sub', 'ecx, edx'),
 0x00B59F8E: ('mov', 'dword ptr [esi], ecx'),
 0x00B59F90: ('jns', '0xb59f9a'),
 0x00B59F92: ('add', 'ecx, 0x7fffffff'),
 0x00B59F98: ('mov', 'dword ptr [esi], ecx'),
 0x00B59F9A: ('fild', 'dword ptr [esi + 4]'),
 0x00B59F9D: ('fdiv', 'dword ptr [0x15ecbac]'),
 0x00B59FA3: ('call', '0x131e27c'),
 0x00B59FA8: ('mov', 'ecx, dword ptr [esi + eax*4 + 8]'),
 0x00B59FAC: ('mov', 'edx, dword ptr [esi]'),
 0x00B59FAE: ('mov', 'dword ptr [esi + 4], ecx'),
 0x00B59FB1: ('mov', 'dword ptr [esi + eax*4 + 8], edx'),
 0x00B59FB5: ('mov', 'eax, dword ptr [esi + 4]'),
 0x00B59FB9: ('ret', ''),
 0x0131E27D: ('mov', 'ebp, esp'),
 0x0131E27F: ('add', 'esp, -0xc'),
 0x0131E283: ('fnstcw', 'word ptr [ebp - 2]'),
 0x0131E287: ('mov', 'ax, word ptr [ebp - 2]'),
 0x0131E28B: ('or', 'ah, 0xc'),
 0x0131E28E: ('mov', 'word ptr [ebp - 4], ax'),
 0x0131E292: ('fldcw', 'word ptr [ebp - 4]'),
 0x0131E295: ('fistp', 'qword ptr [ebp - 0xc]'),
 0x0131E298: ('fldcw', 'word ptr [ebp - 2]'),
 0x0131E29B: ('mov', 'eax, dword ptr [ebp - 0xc]'),
 0x0131E29E: ('mov', 'edx, dword ptr [ebp - 8]'),
 0x0131E2A1: ('leave', ''),
 0x0131E2A2: ('ret', ''),
 0x013E64D3: ('sub', 'esp, 0xc'),
 0x013E64DD: ('xor', 'eax, eax'),
 0x013E64DF: ('neg', 'ecx'),
 0x013E64E1: ('lea', 'ebx, [esi + 0x1d28]'),
 0x013E64E8: ('mov', 'dword ptr [esi + 4], eax'),
 0x013E64EB: ('mov', 'dword ptr [esi], ecx'),
 0x013E64F0: ('lea', 'edi, [esi + 0xd18]'),
 0x013E64F9: ('jmp', '0x13e6500'),
 0x013E6505: ('mov', 'dword ptr [ebx - 0x1818], edx'),
 0x013E650B: ('call', '0xb59ef0'),
 0x013E6510: ('and', 'eax, 0x800001ff'),
 0x013E6515: ('jns', '0x13e651e'),
 0x013E6517: ('dec', 'eax'),
 0x013E6518: ('or', 'eax, 0xfffffe00'),
 0x013E651D: ('inc', 'eax'),
 0x013E651E: ('sub', 'eax, 0x100'),
 0x013E6523: ('mov', 'dword ptr [ebp - 8], eax'),
 0x013E6526: ('fild', 'dword ptr [ebp - 8]'),
 0x013E6529: ('mov', 'dword ptr [ebp - 8], 2'),
 0x013E6530: ('fmul', 'dword ptr [0x160961c]'),
 0x013E6536: ('fstp', 'dword ptr [ebx]'),
 0x013E653A: ('lea', 'ebx, [ebx]'),
 0x013E6542: ('call', '0xb59ef0'),
 0x013E6547: ('and', 'eax, 0x800001ff'),
 0x013E654C: ('jns', '0x13e6555'),
 0x013E654E: ('dec', 'eax'),
 0x013E654F: ('or', 'eax, 0xfffffe00'),
 0x013E6554: ('inc', 'eax'),
 0x013E6555: ('sub', 'eax, 0x100'),
 0x013E655A: ('mov', 'dword ptr [ebp - 0xc], eax'),
 0x013E655D: ('fild', 'dword ptr [ebp - 0xc]'),
 0x013E6560: ('mov', 'eax, dword ptr [ebp - 8]'),
 0x013E6563: ('add', 'ebx, 4'),
 0x013E6566: ('dec', 'eax'),
 0x013E6567: ('fmul', 'dword ptr [0x160961c]'),
 0x013E656D: ('mov', 'dword ptr [ebp - 8], eax'),
 0x013E6570: ('fstp', 'dword ptr [ebx - 4]'),
 0x013E6573: ('jne', '0x13e6540'),
 0x013E6575: ('fld', 'dword ptr [edi]'),
 0x013E657A: ('fld', 'dword ptr [edi + 4]'),
 0x013E6580: ('fld', 'st(0)'),
 0x013E6582: ('inc', 'eax'),
 0x013E6583: ('fmul', 'st(1)'),
 0x013E6585: ('add', 'edx, 4'),
 0x013E6588: ('cmp', 'eax, 0x100'),
 0x013E658D: ('fld', 'st(2)'),
 0x013E658F: ('fmul', 'st(3)'),
 0x013E6597: ('faddp', 'st(1)'),
 0x013E6599: ('fsqrt', ''),
 0x013E659B: ('fstp', 'st(2)'),
 0x013E659D: ('fstp', 'st(0)'),
 0x013E659F: ('fdivr', 'dword ptr [0x15dc6f8]'),
 0x013E65A5: ('fld', 'st(0)'),
 0x013E65A7: ('fmul', 'dword ptr [edi]'),
 0x013E65A9: ('fstp', 'dword ptr [edi]'),
 0x013E65AB: ('fmul', 'dword ptr [edi + 4]'),
 0x013E65AE: ('fstp', 'dword ptr [edi + 4]'),
 0x013E65B3: ('jl', '0x13e64fb'),
 0x013E65B9: ('dec', 'eax'),
 0x013E65BA: ('je', '0x13e65f9'),
 0x013E65BC: ('lea', 'edi, [esi + eax*4 + 0x510]'),
 0x013E65C6: ('mov', 'ebx, dword ptr [edi]'),
 0x013E65CA: ('call', '0xb59ef0'),
 0x013E65CF: ('and', 'eax, 0x800000ff'),
 0x013E65D4: ('jns', '0x13e65dd'),
 0x013E65D6: ('dec', 'eax'),
 0x013E65D7: ('or', 'eax, 0xffffff00'),
 0x013E65DC: ('inc', 'eax'),
 0x013E65DD: ('mov', 'ecx, dword ptr [esi + eax*4 + 0x510]'),
 0x013E65E4: ('mov', 'dword ptr [edi], ecx'),
 0x013E65E6: ('mov', 'dword ptr [esi + eax*4 + 0x510], ebx'),
 0x013E65F0: ('sub', 'edi, 4'),
 0x013E65F3: ('dec', 'eax'),
 0x013E65F7: ('jne', '0x13e65c6'),
 0x013E65F9: ('lea', 'eax, [esi + 0xd18]'),
 0x013E65FF: ('add', 'esi, 0x910'),
 0x013E6605: ('mov', 'ecx, 0x102'),
 0x013E660A: ('lea', 'ebx, [ebx]'),
 0x013E6616: ('mov', 'dword ptr [esi], edx'),
 0x013E6631: ('mov', 'dword ptr [edi], ebx'),
 0x013E6633: ('add', 'esi, 4'),
 0x013E6636: ('add', 'eax, 8'),
 0x013E6639: ('dec', 'ecx'),
 0x013E663A: ('mov', 'dword ptr [edi + 4], edx'),
 0x013E663D: ('jne', '0x13e6610'),
 0x013E6645: ('ret', '4'),
 0x013E66F4: ('fld', 'dword ptr [ebp + 8]'),
 0x013E66F8: ('fadd', 'dword ptr [0x15e4f74]'),
 0x013E6702: ('fld', 'st(0)'),
 0x013E6704: ('call', '0x131e27c'),
 0x013E6709: ('fcom', 'dword ptr [0x15dc6a0]'),
 0x013E6714: ('fnstsw', 'ax'),
 0x013E6716: ('test', 'ah, 5'),
 0x013E6719: ('jp', '0x13e6730'),
 0x013E671B: ('fild', 'dword ptr [ebp - 4]'),
 0x013E671E: ('fld', 'st(1)'),
 0x013E6720: ('fcompp', ''),
 0x013E6722: ('fnstsw', 'ax'),
 0x013E6724: ('test', 'ah, 0x44'),
 0x013E6727: ('jnp', '0x13e6730'),
 0x013E6732: ('sub', 'ecx, eax'),
 0x013E6739: ('fild', 'dword ptr [ebp - 4]'),
 0x013E673C: ('and', 'eax, 0xff'),
 0x013E6746: ('fsubr', 'st(1)'),
 0x013E6748: ('and', 'ebx, 0xff'),
 0x013E674E: ('fstp', 'dword ptr [ebp + 8]'),
 0x013E6751: ('fstp', 'st(0)'),
 0x013E6753: ('fld', 'dword ptr [ebp + 8]'),
 0x013E6756: ('fsub', 'dword ptr [0x15dc6f8]'),
 0x013E675C: ('fld', 'dword ptr [ebp + 0xc]'),
 0x013E675F: ('fadd', 'dword ptr [0x15e4f74]'),
 0x013E6765: ('fld', 'st(0)'),
 0x013E6767: ('call', '0x131e27c'),
 0x013E676C: ('fcom', 'dword ptr [0x15dc6a0]'),
 0x013E6777: ('fnstsw', 'ax'),
 0x013E6779: ('test', 'ah, 5'),
 0x013E677C: ('jp', '0x13e6793'),
 0x013E677E: ('fild', 'dword ptr [ebp - 4]'),
 0x013E6781: ('fld', 'st(1)'),
 0x013E6783: ('fcompp', ''),
 0x013E6785: ('fnstsw', 'ax'),
 0x013E6787: ('test', 'ah, 0x44'),
 0x013E678A: ('jnp', '0x13e6793'),
 0x013E679C: ('sub', 'ecx, eax'),
 0x013E67A3: ('fisub', 'dword ptr [ebp - 4]'),
 0x013E67A6: ('and', 'eax, 0xff'),
 0x013E67AD: ('fld', 'dword ptr [0x15dc6f8]'),
 0x013E67B6: ('fsubr', 'st(1)'),
 0x013E67BF: ('fld', 'dword ptr [ebp + 8]'),
 0x013E67C9: ('fadd', 'st(0), st(0)'),
 0x013E67CE: ('and', 'eax, 0xff'),
 0x013E67DC: ('fsubr', 'dword ptr [0x15dd850]'),
 0x013E67E2: ('fmul', 'dword ptr [ebp + 8]'),
 0x013E67E5: ('fmul', 'dword ptr [ebp + 8]'),
 0x013E67E8: ('fld', 'st(2)'),
 0x013E67EA: ('fmul', 'dword ptr [edi + 4]'),
 0x013E67ED: ('fld', 'dword ptr [ebp + 8]'),
 0x013E67F0: ('fmul', 'dword ptr [edi]'),
 0x013E6802: ('faddp', 'st(1)'),
 0x013E680B: ('fld', 'st(3)'),
 0x013E680F: ('fmul', 'dword ptr [ecx + 4]'),
 0x013E6819: ('fld', 'st(5)'),
 0x013E681B: ('fmul', 'dword ptr [ecx]'),
 0x013E682B: ('faddp', 'st(1)'),
 0x013E682D: ('fsub', 'st(1)'),
 0x013E682F: ('fmul', 'st(2)'),
 0x013E6831: ('faddp', 'st(1)'),
 0x013E6833: ('fld', 'st(2)'),
 0x013E6835: ('fmul', 'dword ptr [ecx + 4]'),
 0x013E6838: ('fld', 'dword ptr [ebp + 8]'),
 0x013E683B: ('fmul', 'dword ptr [ecx]'),
 0x013E683D: ('faddp', 'st(1)'),
 0x013E6841: ('fmul', 'dword ptr [eax + 4]'),
 0x013E6846: ('fmul', 'dword ptr [eax]'),
 0x013E6848: ('faddp', 'st(5)'),
 0x013E684C: ('fsub', 'st(2)'),
 0x013E684E: ('fmul', 'st(1)'),
 0x013E6850: ('fadd', 'st(2)'),
 0x013E6852: ('fsub', 'st(4)'),
 0x013E6854: ('fld', 'st(3)'),
 0x013E6856: ('fadd', 'st(0), st(0)'),
 0x013E6858: ('fsubr', 'dword ptr [0x15dd850]'),
 0x013E685E: ('fmul', 'st(4)'),
 0x013E6860: ('fmul', 'st(4)'),
 0x013E6862: ('fmulp', 'st(1)'),
 0x013E6867: ('fadd', 'st(4)'),
 0x013E686B: ('fstp', 'st(2)'),
 0x013E686D: ('fstp', 'st(1)'),
 0x013E686F: ('fstp', 'st(0)'),
 0x013E6871: ('fstp', 'st(0)'),
 0x013E6876: ('ret', '8'),
 0x013E82F2: ('call', '0xa94870'),
 0x013E830C: ('call', '0xa94870'),
 0x013E831F: ('call', '0xa94870'),
 0x013E8339: ('call', '0xa94870'),
 0x013E834C: ('call', '0xa94870'),
 0x013E835F: ('call', '0xa94870'),
 0x013E8372: ('call', '0xa94870'),
 0x013E8385: ('call', '0xa94870'),
 0x013E8398: ('call', '0xa94870'),
 0x013E83AB: ('call', '0xa94870'),
 0x013E83BE: ('call', '0xa94870'),
 0x013E83D1: ('call', '0xa94870'),
 0x013E83E4: ('call', '0xa94870'),
 0x013E8421: ('push', '0x4d465243'),
 0x013E8438: ('sub', 'eax, 0x30303030'),
 0x013E843D: ('je', '0x13e84d0'),
 0x013E8444: ('je', '0x13e84b5'),
 0x013E84BA: ('call', '0x13e8640'),
 0x013E864C: ('push', '0x30303031'),
 0x013E865C: ('push', '0x44415441'),
 0x013E8670: ('call', '0xa94f00'),
 0x013E867E: ('call', '0x13e6b80'),
 0x013E868B: ('call', '0xa94f00'),
 0x013E8698: ('call', '0xa94f00'),
 0x013E86AF: ('call', '0x13e6c50'),
 0x013E86BC: ('call', '0xa94f00'),
 0x013E86C9: ('call', '0xa94f00'),
 0x013E86E0: ('call', '0x13e6c70'),
 0x013E86ED: ('call', '0xa94f00'),
 0x013E86F8: ('call', '0x13e6bf0'),
 0x013E8705: ('call', '0xa94f00'),
 0x013E8710: ('call', '0x13e6c10'),
 0x013E871D: ('call', '0xa94f00'),
 0x013E8728: ('call', '0x13e6c30'),
 0x013E8735: ('call', '0xa94f00'),
 0x013E8742: ('call', '0xa94f00'),
 0x013E8759: ('call', '0x13e6bb0'),
 0x013E8766: ('call', '0xa94f00'),
 0x013E8773: ('call', '0xa94f00'),
 0x013E878A: ('call', '0x13e6bd0'),
 0x013E8797: ('call', '0xa94f00'),
 0x013E87A2: ('call', '0x13e6cb0'),
}

def load(name,path):
 spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def sha(b):return hashlib.sha256(b).hexdigest().upper()
def gate(b,want):
 if sha(b)!=want:raise ValueError('Input identity mismatch before interpretation')
def signed(x):return x if x<0x80000000 else x-0x100000000

def setup():
 global old,a,c,store,family
 old=load('seed_frozen_sampler','tools/python/verify_terrain_fractal_sampler.py');old.setup();a=old.a;c=old.old.fh.c;store=old.store;family=load('seed_family','tools/python/verify_terrain_fractal_family_lookup.py')

def finite_scalar(bits):
 if bits&0x7f800000==0x7f800000:raise ValueError('Modern finite eligibility')
 return a.scalar(bits)

def arithmetic(p):
 if p not in (24,53,64):raise IndexError('Profile')
 return (lambda x,y:a.precision(a.add(x,y),p)),(lambda x,y:a.precision(a.mul(x,y),p)),(lambda x,y:a.precision((x[0]/y[0],not x[0] and ((x[0]<0 or x[1]) != (y[0]<0 or y[1]))),p))
def negate(x):return -x[0],not x[1] if not x[0] else False
def exact_int(x):return Fraction(x),False

def static(data,packages):
 gate(data,EXE);sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=data,fast_load=True);im=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,want in ANCHORS.items():
  i=next(md.disasm(im[va-base:va-base+15],va,count=1));assert (i.mnemonic,i.op_str)==want,(hex(va),want,(i.mnemonic,i.op_str))
 for va,bits in ((0x15ecbac,0x4acb872a),(0x160961c,0x3b800000),(0x15e4f74,0x45800000),(0x15dd850,0x40400000)):
  assert struct.unpack_from('<I',im,va-base)[0]==bits
 ins=list(md.disasm(im[0x13e8640-base:0x13e87bd-base],0x13e8640));assert sum(i.mnemonic=='call' and i.op_str=='0xa94f00' for i in ins)==13
 writer=list(md.disasm(im[0x13e82b0-base:0x13e8408-base],0x13e82b0));assert sum(i.mnemonic=='call' and i.op_str=='0xa94870' for i in writer)==13
 return dict(ExecutableSha256=EXE,InstructionAnchors=len(ANCHORS),LoaderWords=13,WriterWords=13,RngShuffleWords=322,InitializationSteps=330,PermutationWords=514,Gradient1Words=514,Gradient2Pairs=514,OriginalExecuted=False,OriginalFloatingControls='Unknown')


def recurrence_checks():
 # Independent fixed-width instruction arithmetic versus the mathematical recurrence.
 rng=random.Random(4701);values=[0,1,127772,127773,127774,47,MODULUS-1,MODULUS]+[rng.randrange(1,MODULUS) for _ in range(512)]
 for seed in values:
  high=(signed(0x834e0b5f)*seed)>>32;q=signed((high+seed)&0xffffffff)>>16;q+=(q&0xffffffff)>>31
  assert q==seed//127773
  value=signed((signed((seed*16807)&0xffffffff)-signed((q*MODULUS)&0xffffffff))&0xffffffff)
  if value<0:value+=MODULUS
  assert value==(seed*16807)%MODULUS
 return len(values)

def selected(root,config,runroot):
 # Reuse archive precedence/lookup and stop at the same first family; no survey.
 family.gate(config.read_bytes(),family.CONFIG);m=family.load('seed_tgen','poc/round33-tests/Assets/verify_tgen_input.py')
 with contextlib.ExitStack() as stack:
  for priority,path in m.archives(root,config):
   payload=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
   if payload is not None:break
  family.gate(payload,family.SOURCE);assert priority==42 and len(payload)==1188446
  version,=m.records(payload,12,len(payload));_,section,_=m.records(payload,version[2]+12,version[3]);g=payload[section[2]:section[3]];family.gate(g,family.GENERATOR);assert section[2]==210 and len(g)==139608
  raw=g[6626:6626+132];family.gate(raw,family.FAMILY);records=list(m.records(raw,12,len(raw)));assert len(records)==2 and records[0][0]==b'DATA' and struct.unpack_from('<i',records[0][1])[0]==1
  mf=raw[records[1][2]:records[1][3]];gate(mf,MFRC);assert len(mf)==84
  identity_checks=sum(family.negatives(value,expected) for value,expected in ((config.read_bytes(),family.CONFIG),(payload,family.SOURCE),(g,family.GENERATOR),(raw,family.FAMILY),(mf,MFRC)))
  words=parse(mf) # SHA identity above pins the payload; literals stay private.
  return dict(ConfigSha256=family.CONFIG,SourceSha256=family.SOURCE,GeneratorSha256=family.GENERATOR,FamilySha256=family.FAMILY,FamilyKey=1,FamilyOffset=6626,FamilyBytes=132,MfrcSha256=MFRC,MfrcOffset=6674,MfrcBytes=84,Version='0001',Words=words,OriginalNoiseSamples=0,InputIdentityNegatives=identity_checks),mf

def parse(raw):
 if len(raw)!=84:raise ValueError('Exact selected envelope')
 if raw[:4]!=b'FORM' or struct.unpack_from('>I',raw,4)[0]!=76 or raw[8:12]!=b'MFRC' or raw[12:16]!=b'FORM' or struct.unpack_from('>I',raw,16)[0]!=64 or raw[20:24]!=b'0001' or raw[24:28]!=b'DATA' or struct.unpack_from('>I',raw,28)[0]!=52:raise ValueError('Exact selected tags/extents')
 return list(struct.unpack_from('<13I',raw,32))

class Rng:
 def __init__(self,seed,p):self.seed=signed((-signed(seed))&0xffffffff);self.iy=0;self.table=[0]*322;self.p=p;self.steps=0;self.draws=0
 def step(self):self.seed=(self.seed*16807)%MODULUS;self.steps+=1
 def next(self):
  if self.seed<=0 or self.iy==0:
   self.seed=max(1,signed((-self.seed)&0xffffffff))
   for j in range(329,-1,-1):
    self.step()
    if j<322:self.table[j]=self.seed
   self.iy=self.table[0]
  self.step();idx=shuffle_index(self.iy,self.p);assert 0<=idx<322;self.iy,self.table[idx]=self.table[idx],self.seed;self.draws+=1;return self.iy

def shuffle_index(iy,p):
 _,_,div=arithmetic(p);return int(div(exact_int(iy),exact_int(DIVISOR))[0])
def normalize(xbits,ybits,p):
 add,mul,div=arithmetic(p);x,y=finite_scalar(xbits),finite_scalar(ybits);length=c.sqrt_round(add(mul(y,y),mul(x,x)),p);inv=div(exact_int(1),length);return store(mul(inv,x)),store(mul(inv,y))
def hashwords(words):return sha(struct.pack('<'+'I'*len(words),*[w&0xffffffff for w in words]))
TABLES={}
def tables(seed,p):
 key=(seed,p)
 if key in TABLES:return TABLES[key]
 rng=Rng(seed,p);perm=list(range(256));g1=[];g2=[]
 for i in range(256):
  scalar=exact_int(rng.next()%512-256);g1.append(store((scalar[0]/256,False)))
  x=store((Fraction(rng.next()%512-256,256),False));y=store((Fraction(rng.next()%512-256,256),False));g2.append(normalize(x,y,p))
 for i in range(255,0,-1):j=rng.next()%256;perm[i],perm[j]=perm[j],perm[i]
 for i in range(258):perm.append(perm[i]);g1.append(g1[i]);g2.append(g2[i])
 receipt=dict(PermutationSha=hashwords(perm),Gradient1Sha=hashwords(g1),Gradient2Sha=hashwords([w for pair in g2 for w in pair]),RngDraws=rng.draws,RngSteps=rng.steps,FinalSeed=rng.seed,FinalIy=rng.iy)
 TABLES[key]=(perm,g1,g2,receipt);return TABLES[key]

def fraction_text(x):return str(x[0].numerator)+'/'+str(x[0].denominator)
def helper(v):
 value=int(v[0])
 if not -(1<<63)<=value<(1<<63):raise ValueError('Modern conversion eligibility')
 raw=value&0xffffffffffffffff;return value,raw&0xffffffff,raw>>32

def noise(xbits,zbits,seed,tp,p,premature_z=False):
 add,mul,div=arithmetic(p);perm,_,grad,_=tables(seed,tp);x,z=finite_scalar(xbits),finite_scalar(zbits);bx=add(x,exact_int(4096));bz=add(z,exact_int(4096))
 def lattice(t):
  integer,lo,hi=helper(t)
  if not -(1<<31)<=integer<(1<<31):raise ValueError('Modern lattice eligibility')
  integer=int(t[0]//1);return integer
 ix,iz=lattice(bx),lattice(bz);fx=finite_scalar(store(add(bx,exact_int(-ix))));fz=add(bz,exact_int(-iz));original_z=fraction_text(fz)
 if premature_z:fz=finite_scalar(store(fz))
 dx=add(fx,exact_int(-1));dz=add(fz,exact_int(-1));x0,x1=ix&255,(ix+1)&255;z0,z1=iz&255,(iz+1)&255
 corners=[perm[perm[x0]+z0],perm[perm[x1]+z0],perm[perm[x0]+z1],perm[perm[x1]+z1]]
 def dot(i,xx,zz):return add(mul(zz,finite_scalar(grad[i][1])),mul(xx,finite_scalar(grad[i][0])))
 def fade(t):return mul(mul(add(exact_int(3),negate(add(t,t))),t),t)
 def lerp(t,left,right):return add(mul(add(right,negate(left)),t),left)
 d00,d10,d01,d11=[dot(i,xx,zz) for i,xx,zz in zip(corners,(fx,dx,fx,dx),(fz,fz,dz,dz))]
 value=lerp(fade(fz),lerp(fade(fx),d00,d10),lerp(fade(fx),d01,d11))
 return value,dict(LatticeX=ix,LatticeZ=iz,FractionX=store(fx),FractionZ=original_z,Corners=corners,Retained=fraction_text(value),NegativeZero=value[1],Projection=store(value))

def evaluate(f):
 name=f['Name'];p=f.get('Precision',53)
 try:
  arithmetic(p);kind=f['Kind'];out=dict(Name=name,Reject='None')
  if kind=='Raw':out['Words']=parse(bytes.fromhex(f['Hex']))
  elif kind=='Index':out.update(Index=shuffle_index(f['Iy'],p),IntegerDivision=f['Iy']//DIVISOR)
  elif kind=='Rng':
   rng=Rng(f['Seed'],p);words=[rng.next() for _ in range(f['Draws'])];out.update(SequenceSha=hashwords(words),StateSha=hashwords([rng.seed,rng.iy,*rng.table]),Steps=rng.steps,Draws=rng.draws)
  elif kind=='Normalize':out['Words']=list(normalize(f['X'],f['Z'],p))
  elif kind=='Tables':out.update(tables(f['Seed'],p)[3])
  elif kind=='Helper':
   v=finite_scalar(f['X']);value,lo,hi=helper(v);out.update(Low=lo,High=hi,Value=str(value))
  else:
   value,receipt=noise(f['X'],f['Z'],f['Seed'],f['TablePrecision'],p);out.update(receipt)
  return out
 except IndexError:return dict(Name=name,Reject='Options')
 except (ValueError,ZeroDivisionError,OverflowError):return dict(Name=name,Reject='Input')

def fixtures(original,selected_seed):
 fs=[]
 def put(kind,p=53,**kw):fs.append(dict(Name=str(len(fs)),Kind=kind,Precision=p,**kw))
 # Authored frame isolates schema/negative tests; original bytes remain only in private OriginalHex.
 raw=bytearray(original);struct.pack_into('<13I',raw,32,1,0,0x3f000000,0,0x3f000000,2,0x40000000,0x3f000000,0x3f800000,0x3f800000,0,0,0)
 put('Raw',Hex=raw.hex())
 for end in range(len(raw)):put('Raw',Hex=raw[:end].hex())
 for off in (0,4,8,12,16,20,24,28):
  bad=bytearray(raw);bad[off]^=1;put('Raw',Hex=bad.hex())
 put('Raw',Hex=(raw+b'\0').hex())
 for p in (24,53,64):
  for iy in [0,1,MODULUS-1]+[k*DIVISOR+d for k in (1,2,31,127,255,321) for d in (-2,-1,0,1,2)]:put('Index',p,Iy=iy)
  for seed in (0,1,selected_seed,0xffffffff,0x80000000,0x7fffffff):put('Rng',p,Seed=seed,Draws=1024)
  for seed in (0,1,selected_seed):put('Tables',p,Seed=seed)
  for x,y in ((0,0),(0x80000000,0),(0x3f800000,0),(0xbf800000,0x3f800000),(0x3f000000,0x3e800000),(1,1),(0x7fc12345,0)):
   put('Normalize',p,X=x,Z=y)
 for x in (0,0x80000000,0x3fc00000,0xbfc00000,0xcf800000,0x4f800000,0xd0000000,0x5e800000,0xde800000,0x5f000000,0xdf000000,0x7fc12345,0x7f800000):put('Helper',X=x)
 coords=[(0xcf000010,0),(0,0),(0x3f000000,0x3e800000),(0x80000000,0x80000000),(0x3f800000,0x3f800000),(0x3f800001,0xbf800001),(0x45800000,0xc5800000),(0xc5800800,0xc5801000),(0,0xb58637bd),(0xb58637bd,0),(0x4f800000,0),(0x7fc12345,0),(0,0x7f800000)]
 rng=random.Random(47)
 for _ in range(70):
  coords.append((struct.unpack('<I',struct.pack('<f',rng.uniform(-5000,5000)))[0],struct.unpack('<I',struct.pack('<f',rng.uniform(-5000,5000)))[0]))
 for tp in (24,53,64):
  for p in (24,53,64):
   for x,z in coords:put('Noise',p,Seed=selected_seed,TablePrecision=tp,X=x,Z=z)
 for p in (0,23,65):put('Index',p,Iy=1)
 return fs

def main():
 parser=argparse.ArgumentParser();
 for key in ('exe','packages','asset-root','config','run-root'):parser.add_argument('--'+key,type=Path,required=True)
 parser.add_argument('--native',type=Path);args=parser.parse_args();data=args.exe.read_bytes()
 if sha(data)!=EXE:raise ValueError('Executable identity mismatch; rejected before decoding')
 if sha(args.config.read_bytes())!=CONFIG:raise ValueError('Config identity mismatch; rejected before decoding')
 if not args.run_root.resolve().is_relative_to((ROOT/'local-private').resolve()):raise ValueError('Private output required')
 st=static(data,args.packages);st['IntegerRecurrenceChecks']=recurrence_checks();setup();st['WrongExecutableNegatives']=family.negatives(data,EXE);occ,original=selected(args.asset_root,args.config,args.run_root);fs=fixtures(original,occ['Words'][0]);expected=[evaluate(f) for f in fs]
 rounding_witnesses=sum(e.get('Index')!=e.get('IntegerDivision') for e in expected if 'Index' in e)
 noise_witnesses=0
 for f,e in zip(fs,expected):
  if f['Kind']=='Noise' and e['Reject']=='None':noise_witnesses+=noise(f['X'],f['Z'],f['Seed'],f['TablePrecision'],f['Precision'],True)[1]['Projection']!=e['Projection']
 result=dict(Static=st,Occurrence=occ,Cases=len(fs),Rejected=sum(e['Reject']!='None' for e in expected),CasesByKind={kind:sum(f['Kind']==kind for f in fs) for kind in sorted({f['Kind'] for f in fs})},FloatingShuffleIndexWitnesses=rounding_witnesses,PrematureZFractionWitnesses=noise_witnesses,OriginalExecution=False)
 assert rounding_witnesses>0
 args.run_root.mkdir(parents=True,exist_ok=True)
 if args.native:
  native=json.loads(args.native.read_text());assert len(native)==len(expected)
  for f,want,got in zip(fs,expected,native):assert want==got,(f,want,got)
  result['NativeCompared']=True
 else:(args.run_root/'seed-fixtures.json').write_text(json.dumps(fs,separators=(',',':')))
 (args.run_root/'seed-receipt.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
