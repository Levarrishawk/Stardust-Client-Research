"""Pinned static SWTS/DTST evidence audit. Never executes the original image."""
import argparse,hashlib,json,struct,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--shader',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
identity=hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()
if identity!='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':raise ValueError('Executable identity mismatch before VA use')
sys.path.insert(0,str(a.packages));import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
pe=pefile.PE(str(a.exe),fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;data=pe.get_memory_mapped_image();md=Cs(CS_ARCH_X86,CS_MODE_32)
def u32(va):return struct.unpack_from('<I',data,va-base)[0]
def rtti(vt):
 td=u32(u32(vt-4)+12);off=td-base+8;return data[off:data.index(b'\0',off)].decode('ascii')
for vt,name in [(0x15e39d4,'SwitchTextureShaderTemplate'),(0x15e3b54,'SwitchTextureShader'),(0x15e3b80,'SwitchShader'),(0x15ecbd0,'DeltaTimeSwitcherTemplate'),(0x15ecc24,'DeltaTimeSwitcher'),(0x15e3984,'LocalShaderPrimitive@ShaderPrimitiveSet')]:assert name in rtti(vt)
checks={
 0x77ebc0:(0x77ebf0,[('push','0x77ecd0'),('push','0x53575453'),('call','0x75a890')]),
 0x77ef30:(0x77ef70,[('call','0x75abe0'),('call','0xb5a0c0'),('mov','dword ptr [esi + 0x18], eax')]),
 0xb5a3f0:(0xb5a4a9,[('push','0x44545354'),('push','0x30303030'),('mov','dword ptr [esi + 8], ecx'),('mov','dword ptr [esi + 4], eax')]),
 0xb5a4f0:(0xb5a553,[('mov','ecx, dword ptr [esi + 8]'),('mov','edx, dword ptr [esi + 4]'),('call','0xb5aca0')]),
 0xb5aca0:(0xb5accc,[('mov','dword ptr [esi + 4], eax'),('xor','eax, eax'),('mov','dword ptr [esi + 8], eax'),('mov','dword ptr [esi + 0x10], eax'),('mov','dword ptr [esi + 0xc], ecx')]),
 0xb5ad20:(0xb5ad71,[('fadd','dword ptr [ecx + 8]'),('fst','dword ptr [ecx + 8]'),('inc','edx'),('cmp','edx, esi'),('jl','0xb5ad53'),('xor','edx, edx'),('fsub','st(1)'),('je','0xb5ad46')]),
 0xb5ad80:(0xb5ad94,[('mov','eax, dword ptr [ecx + 0x10]'),('mov','eax, dword ptr [ecx + 0xc]')]),
 0x785b00:(0x785b5a,[('call','0x76d8e0'),('call','dword ptr [eax]'),('mov','dword ptr [esi + 0xc], eax'),('mov','ecx, dword ptr [edi + 0x20]')]),
 0x785b90:(0x785be6,[('push','1'),('call','dword ptr [eax]'),('call','0x76d970')]),
 0x785c10:(0x785c29,[('call','dword ptr [eax + 8]')]),
 0x785c30:(0x785c49,[('call','dword ptr [eax + 0xc]'),('mov','ecx, dword ptr [edx + eax*4]'),('jmp','dword ptr [eax + 0x14]')]),
 0x77e2c0:(0x77e2d3,[('call','dword ptr [eax + 0x10]')]),
 0x786080:(0x786099,[('call','dword ptr [eax + 8]')]),
 0x7860a0:(0x7860b9,[('call','dword ptr [eax + 0xc]'),('jmp','dword ptr [eax + 0x14]')]),
 0x7860c0:(0x7860d9,[('call','dword ptr [eax + 0xc]'),('jmp','dword ptr [eax + 0x18]')]),
 0x76d8e0:(0x76d903,[('call','0x769910')]),
 0x76d970:(0x76d97e,[('jmp','0x769930')]),
}
ranges=[]
for lo,(hi,expected) in checks.items():
 instructions=[(i.mnemonic,i.op_str) for i in md.disasm(data[lo-base:hi-base],lo)]
 for instruction in expected:assert instruction in instructions,(hex(lo),instruction)
 ranges.append(dict(start=f'{lo:08X}',end=f'{hi:08X}',sha256=hashlib.sha256(data[lo-base:hi-base]).hexdigest().upper(),checks=len(expected)))
assert [u32(0x15ecc24+i*4) for i in range(5)]==[0xb5acd0,0xb5ad10,0xb5ad20,0xb5ad80,0xb5ad90]
b=a.shader.read_bytes();assert len(b)==475 and hashlib.sha256(b).hexdigest().upper()=='B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62'
def nodes(start,end):
 result=[]
 while start<end:
  tag=b[start:start+4];size=struct.unpack_from('>I',b,start+4)[0];stop=start+8+size;assert stop<=end
  result.append((b[start+8:start+12],nodes(start+12,stop)) if tag==b'FORM' else (tag,b[start+8:stop]));start=stop
 assert start==end;return result
root,=nodes(0,len(b));assert root[0]==b'SWTS';version,=root[1];assert version[0]==b'0000'
name,dtst,*frames=version[1];assert name==(b'NAME',b'shader/defaultemis.sht\0') and dtst==(b'DTST',[(b'0000',bytes.fromhex('08000000CDCCCC3D'))])
count,interval=struct.unpack('<if',dtst[1][0][1]);assert count==len(frames)==8 and interval==struct.unpack('<f',bytes.fromhex('CDCCCC3D'))[0]
for i,(tag,payload) in enumerate(frames,1):assert tag==b'TEXT' and struct.unpack_from('<I',payload)[0]==0x4d41494e and payload[4:]==f'texture/anim_mission_terminal_{i:02d}.dds\0'.encode()
receipt=dict(executable_sha256=identity,rtti_classes=6,static_ranges=ranges,frames=count,interval_float32=interval,initial_index=0,callback_driven=True,outer_clock_units='Unknown',original_execution=False)
private=Path(__file__).resolve().parents[3]/'local-private';assert a.receipt.resolve().is_relative_to(private.resolve()) and not a.receipt.exists()
a.receipt.write_text(json.dumps(receipt,indent=2)+'\n');print('PASS: pinned static SWTS/DTST instruction/RTTI/asset audit; 17 ranges; original_execution=false')
