"""Hash-first independent recheck of the general sample combine graph."""
import hashlib,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
EXPECTED='e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150'
def verify(exe,packages):
 if hashlib.sha256(exe.read_bytes()).hexdigest()!=EXPECTED:raise ValueError('Executable identity mismatch; rejected before decoding')
 sys.path.insert(0,str(ROOT/'tools/python'));sys.path.insert(0,str(packages))
 from verify_terrain_sample_coordinates import verify as coordinate_verify
 from verify_terrain_layer_influence import ANCHORS
 inherited=coordinate_verify(exe,packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(str(exe),fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;data=pe.get_memory_mapped_image()
 rows={i.address:(i.mnemonic,i.op_str) for i in Cs(CS_ARCH_X86,CS_MODE_32).disasm(data[0xB63D20-base:0xB642A0-base],0xB63D20)}
 extra={
  0xB63F44:('mov','eax, dword ptr [esi + 0x40]'),0xB63F56:('mov','byte ptr [ebp - 0xe], 0'),0xB64054:('mov','eax, dword ptr [esi + 0x44]'),
  0xB63F78:('mov','eax, dword ptr [esi + 0x18]'),0xB63F88:('mov','eax, dword ptr [esi + 0x14]'),
  0xB63F97:('mov','edi, dword ptr [ecx + 0x14]'),0xB63FB2:('cmp','edi, 3'),0xB63FB5:('ja','0xb63fd8'),
  0xB63FBE:('fld','st(0)'),0xB63FC0:('fmulp','st(1)'),0xB63FC4:('fsqrt',''),
  0xB63FC8:('fld','st(0)'),0xB63FCA:('fadd','st(1)'),0xB63FCC:('fsubr','dword ptr [0x15dd850]'),
  0xB63FD2:('fmul','st(1)'),0xB63FD4:('fmulp','st(1)'),0xB63FD8:('fstp','st(0)'),0xB63FDA:('fld','dword ptr [0x15dc6a0]'),
  0xB64013:('inc','edx'),0xB64019:('jl','0xb63f88'),0xB6401F:('mov','al, byte ptr [ebp - 0xe]'),
  0xB6405F:('mov','eax, dword ptr [esi + 0x24]'),0xB64070:('mov','ecx, dword ptr [esi + 0x20]'),
  0xB64083:('cmp','dword ptr [ecx + 0x10], 5'),0xB6408D:('call','0xb79a20'),
  0xB6409B:('mov','edi, dword ptr [ecx + 0x14]'),0xB640BB:('cmp','edi, 3'),0xB640BE:('ja','0xb640e1'),
  0xB640C7:('fld','st(0)'),0xB640C9:('fmulp','st(1)'),0xB640CD:('fsqrt',''),
  0xB640D1:('fld','st(0)'),0xB640D3:('fadd','st(1)'),0xB640D5:('fsubr','dword ptr [0x15dd850]'),
  0xB640DB:('fmul','st(1)'),0xB640DD:('fmulp','st(1)'),0xB640E1:('fstp','st(0)'),0xB640E3:('fld','dword ptr [0x15dc6a0]'),
  0xB64118:('inc','edx'),0xB6411E:('jl','0xb64070'),0xB6416F:('fld','dword ptr [ebp - 0x14]'),
 }
 for va,want in {**ANCHORS,**extra}.items():assert rows.get(va)==want,(f'{va:08X}',rows.get(va),want)
 for va,want in [(0xB642A0,[0xB63FE0,0xB63FBE,0xB63FC4,0xB63FC8]),(0xB642B0,[0xB640E9,0xB640C7,0xB640CD,0xB640D1])]:assert list(struct.unpack_from('<4I',data,va-base))==want
 for va,bits in [(0x15DC6A0,0),(0x15DC6F8,0x3F800000),(0x15DD850,0x40400000)]:assert struct.unpack_from('<I',data,va-base)[0]==bits
 enabled=list(Cs(CS_ARCH_X86,CS_MODE_32).disasm(data[0xB62F20-base:0xB62F24-base],0xB62F20));assert [(i.mnemonic,i.op_str) for i in enabled]==[('mov','al, byte ptr [ecx + 8]'),('ret','')]
 outcomes={'less':1,'equal':0x40,'greater':0,'unordered':0x45};odd=lambda x:x.bit_count()%2==1
 assert [odd(x&5) for x in outcomes.values()]==[True,False,False,False]
 assert [odd(x&0x44) for x in outcomes.values()]==[False,True,False,False]
 assert [bool(x&0x41) for x in outcomes.values()]==[True,True,False,True]
 return dict(ExecutableSha256=EXPECTED,Inherited=inherited,LayerAnchors=len(ANCHORS),AdditionalAnchors=len(extra),EnabledAnchors=2,SelectorTables=2,Constants=3,StatusCases=12,OriginalExecution=False,OriginalProfiles='Unknown')

