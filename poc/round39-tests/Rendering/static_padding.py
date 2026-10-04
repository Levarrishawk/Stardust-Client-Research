"""SHA-first padded cell-count/border/stride/call/lifecycle evidence."""
import hashlib,struct,sys
from pathlib import Path
EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
def verify(exe,packages):
 raw=Path(exe).read_bytes();assert hashlib.sha256(raw).hexdigest().upper()==EXPECTED,'Executable identity mismatch; rejected before decoding'
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 p=pefile.PE(data=raw,fast_load=True);b=p.get_memory_mapped_image();base=p.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32)
 anchors={
  0x84a57d:('call','0xb6e520'),0xb6e520:('mov','eax, dword ptr [ecx + 0x2c]'),0x84a58a:('mov','dword ptr [ebp - 0x2c], ebx'),
  0x84a64a:('mov','eax, dword ptr [ebp - 0x2c]'),0x84a65c:('mov','dword ptr [ebp - 0x80], eax'),0x84a68f:('mov','ecx, dword ptr [esi + 0x58]'),0x84a69e:('mov','dword ptr [ebp - 0x68], ecx'),
  0x84a761:('mov','eax, dword ptr [ebp - 0x40]'),0x84a776:('mov','dword ptr [ebp - 0x614], eax'),0x84a79e:('call','0xb65400'),
  0x84a7c7:('lea','ecx, [ebp - 0x90]'),0x84a7cd:('push','ecx'),0x84a7d0:('call','0x8618d0'),
  0x8618f1:('mov','edi, dword ptr [ebp + 8]'),0x8618fd:('call','dword ptr [eax + 4]'),0x8490c0:('jmp','0xb5cfc0'),0xb5cfc0:('mov','ecx, dword ptr [ecx + 4]'),0xb5cfc3:('jmp','0xb62ec0'),0xb62ec0:('ret',''),
  0x861912:('mov','ecx, dword ptr [edi + 0x28]'),0x861915:('mov','dword ptr [esi + 0x68], ecx'),0x861927:('mov','ecx, dword ptr [edi + 0x10]'),0x86192d:('mov','ebx, dword ptr [edi + 4]'),0x861930:('mov','dword ptr [ebp - 0x18], ecx'),
  0x861bf0:('cmp','dword ptr [ebp - 0x18], edx'),0x861c05:('jle','0x861fe4'),0x861c18:('mov','eax, dword ptr [edi + 0x68]'),0x861c1e:('lea','edx, [eax + esi*2]'),0x861c24:('lea','edx, [eax + ecx*2]'),
  0x861d6e:('mov','eax, 0x15e71c8'),0x861d81:('mov','edi, dword ptr [eax]'),0x861d89:('add','edi, ecx'),0x861d8b:('mov','ecx, dword ptr [eax + 4]'),0x861d8e:('add','ecx, dword ptr [ebp - 0x60]'),
  0x861d91:('mov','eax, dword ptr [ebx + 0x60]'),0x861d94:('imul','eax, ecx'),0x861d97:('add','eax, edi'),0x861d9c:('lea','eax, [esi + eax*4]'),
  0x861dc8:('mov','eax, dword ptr [ebx + 0x6c]'),0x861dce:('imul','eax, ecx'),0x861df3:('mov','eax, dword ptr [ebx + 0xc]'),0x861df6:('imul','eax, ecx'),
  0x861efe:('add','eax, 0x10'),0x861f07:('cmp','eax, 0x15e7258'),0x861f15:('jl','0x861d81'),0x861faa:('call','0x865da0'),0x861fb3:('mov','eax, dword ptr [ebp - 0x18]'),0x861fbb:('call','0x865ff0'),
  0x861fc3:('inc','esi'),0x861fc4:('cmp','esi, eax'),0x861fc9:('jl','0x861c15'),0x861fd2:('inc','ecx'),0x861fd3:('cmp','ecx, eax'),0x861fd8:('jl','0x861c10'),
  0x861cd2:('test','eax, eax'),0x861cd8:('je','0x861cea'),0x861cea:('xor','eax, eax'),0x861fea:('mov','dword ptr [ebp - 4], 0xffffffff'),0x861ff1:('call','0x125f510'),0x862605:('ret','4'),
  0x84a808:('call','0xb632d0'),0x84a817:('call','0x8490a0'),0x84a826:('call','0xa89ae0'),0x84a83d:('ret','0x10'),
  0xb65739:('mov','eax, dword ptr [ecx + 0x18]'),0xb6573c:('mov','edx, dword ptr [ecx + 0x38]'),0xb657b1:('fmul','dword ptr [ebp - 4]'),0xb657b9:('fstp','dword ptr [ebp - 0x24]'),
  0xb657bc:('fld','dword ptr [eax + ebx*4]'),0xb657bf:('mov','eax, dword ptr [edx]'),0xb657c4:('imul','eax, edi'),0xb657c7:('add','eax, ecx'),0xb657d4:('fadd','dword ptr [ebp - 0x5c]'),0xb657dd:('fstp','dword ptr [ebp - 0x20]'),
  0xb65381:('call','0xb65730'),0xb65389:('mov','byte ptr [edi + 0x570], 0'),0xb5d7f1:('mov','dword ptr [esi + 0x58], 3')}
 for va,want in anchors.items():
  ins=next(md.disasm(b[va-base:va-base+15],va,count=1));assert (ins.mnemonic,ins.op_str)==want,(hex(va),ins.mnemonic,ins.op_str,want)
 assert struct.unpack_from('<I',b,0x15e66a0-base)[0]==0x8490c0
 offsets=[struct.unpack_from('<2i',b,0x15e71c8-base+16*i) for i in range(9)];assert offsets==[(1,1),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0),(0,0)]
 return dict(ExecutableSha256=EXPECTED,Anchors=len(anchors),NineOffsets=9,RenderBlockSlot=0x8490c0,ActiveCells='tiles',SampleBorder='margin',OriginalStridePolicy='per-channel descriptor widths; modern common stride explicitly normalized',LegacyErrorEquivalence=False)
