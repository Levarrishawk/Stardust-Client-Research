"""SHA-gated wearable specialization/default/equality/callback static audit."""
import argparse,hashlib,json,runpy,struct,sys
from pathlib import Path
EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS=[(4462965, 'sub', 'ebx, eax'), (4462970, 'add', 'ebx, eax'), (4462977, 'jbe', '0x441988'), (4463050, 'inc', 'eax'), (4463055, 'call', '0xb94c60'), (4463078, 'inc', 'eax'), (4463130, 'jmp', 'dword ptr [eax*4 + 0x441e70]'), (4463186, 'jae', '0x441aec'), (4463243, 'mov', 'dword ptr [esi + 0x18], edi'), (4463273, 'call', '0x447750'), (4463295, 'call', '0x44ce30'), (4463431, 'call', '0xb94c60'), (4463502, 'inc', 'dword ptr [esi + 0x18]'), (4463526, 'call', '0x4477a0'), (4463547, 'call', '0x447550'), (4463662, 'mov', 'edx, dword ptr [eax + 0x10]'), (4463665, 'cmp', 'edx, dword ptr [ebp - 0x3c]'), (4463670, 'mov', 'eax, dword ptr [eax + 0x14]'), (4463673, 'cmp', 'eax, dword ptr [ebp - 0x38]'), (4463676, 'je', '0x441ae9'), (4463741, 'inc', 'dword ptr [esi + 0x18]'), (4463762, 'call', '0x4477a0'), (4463800, 'call', '0xb94bc0'), (4463829, 'call', '0x44ce90'), (4464064, 'inc', 'dword ptr [ebp - 0x14]'), (4464072, 'call', '0xb94c60'), (4464147, 'call', '0x4412a0'), (4464227, 'call', '0x440d50'), (4461360, 'inc', 'dword ptr [esi + 0x18]'), (4461779, 'inc', 'dword ptr [eax + 0x18]'), (4459892, 'je', '0x440eb5'), (4459990, 'inc', 'dword ptr [esi + 0x18]'), (12143725, 'call', '0x439f60'), (12143735, 'lea', 'eax, [esi + 0xc]'), (12143751, 'call', '0xaa5830'), (12143761, 'add', 'esi, 0x18'), (12143767, 'call', '0x439910'), (12143250, 'push', '0x19446c8'), (12143262, 'mov', 'dword ptr [esi + 0xc], 0xffffffff'), (12143269, 'call', '0xaa4920'), (12143278, 'mov', 'dword ptr [esi + 0x18], 0'), (4487107, 'call', '0xb949d0'), (4487248, 'call', '0xb94ad0'), (4487455, 'call', '0x449300'), (4509200, 'mov', 'eax, dword ptr [ecx + 0x28]'), (4509235, 'mov', 'eax, dword ptr [ecx + 0x2c]'), (4509283, 'mov', 'eax, dword ptr [ecx + 0x30]'), (4509331, 'mov', 'eax, dword ptr [ecx + 0x34]'), (4462541, 'push', '4'), (4462570, 'lea', 'eax, [ebx + 0x18]'), (4462605, 'call', '0xb94c60'), (4388135, 'lea', 'edi, [esi + 0x91c]'), (4388196, 'mov', 'dword ptr [edi + 0x28], eax'), (4388199, 'mov', 'dword ptr [edi + 0x2c], eax'), (4388202, 'mov', 'dword ptr [edi + 0x30], eax'), (4388205, 'mov', 'dword ptr [edi + 0x34], eax'), (11159728, 'push', '0'), (11159730, 'push', '0'), (11159732, 'mov', 'ecx, 0x19446c8'), (11159737, 'call', '0xaa4900'), (11159819, 'mov', 'dword ptr [eax], ecx'), (11159821, 'mov', 'dword ptr [eax + 4], edx'), (11159848, 'mov', 'edx, dword ptr [ecx]'), (11159852, 'mov', 'ecx, dword ptr [ecx + 4]')]

def audit(exe,packages):
 raw=exe.read_bytes();digest=hashlib.sha256(raw).hexdigest().upper()
 if digest!=EXPECTED:raise ValueError('Wrong executable identity; rejected before PE imports/VAs')
 repo=Path(__file__).resolve().parents[3]
 inherited=runpy.run_path(str(repo/'poc/round31-tests/Networking/static_audit.py'))['audit'](exe,packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=raw,fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;read=lambda va,n:pe.get_data(va-base,n);md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,mnemonic,operand in ANCHORS:
  i=next(md.disasm(read(va,15),va));assert (i.mnemonic,i.op_str)==(mnemonic,operand),(hex(va),i.mnemonic,i.op_str)
 assert struct.unpack('<5I',read(0x441e70,20))==(0x441a38,0x441b35,0x441be9,0x441cff,0x441e61)
 # Empty literal is loader-zero virtual tail of .data, not a missing file read interpreted as bytes.
 rva=0x1908090-base;s=next(s for s in pe.sections if s.VirtualAddress<=rva<s.VirtualAddress+s.Misc_VirtualSize)
 assert rva-s.VirtualAddress>=s.SizeOfRawData
 assert inherited['Fields'][15]['Vtable']=='015DCD34'
 return dict(ExeSha256=digest,InheritedAnchors=inherited['InstructionAnchors'],WearableAnchors=len(ANCHORS),Field=15,Element='String/U32/U64/U32',Equality='U64 object ID only',Default=dict(Customization='',Arrangement=4294967295,ObjectId=0,TemplateCrc=0),Callbacks='constructor null; nullable helper dispatch; no lifetime absence claim',OriginalExecuted=False)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args();repo=Path(__file__).resolve().parents[3];assert a.receipt.resolve().is_relative_to((repo/'local-private').resolve())
 try:r=audit(a.exe,a.packages)
 except ValueError as e:p.exit(1,str(e)+'\n')
 a.receipt.write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8');print(f"PASS {r['WearableAnchors']} wearable + {r['InheritedAnchors']} inherited anchors; exact default/equality; static only")
