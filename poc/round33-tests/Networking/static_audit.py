"""Pinned HAM specialization/registration/callback audit; static bytes only."""
import argparse,hashlib,json,runpy,sys,struct
from pathlib import Path
EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS=[
 (0x0042ED7C,'lea','edi, [esi + 0x404]'),
 (0x0042EDDB,'mov','dword ptr [edi], 0x15dd07c'),
 (0x0042EDE1,'lea','edi, [esi + 0x43c]'),
 (0x0042EE2E,'mov','dword ptr [edi], 0x15dd07c'),
 (0x0042F9B7,'lea','eax, [esi + 0x404]'),
 (0x0042F9C0,'call','0x5554c0'),
 (0x0042F9C5,'lea','eax, [esi + 0x43c]'),
 (0x0042F9CE,'call','0x5554c0'),
 (0x0042EDCF,'mov','dword ptr [edi + 0x28], eax'),
 (0x0042EDD2,'mov','dword ptr [edi + 0x2c], eax'),
 (0x0042EDD5,'mov','dword ptr [edi + 0x30], eax'),
 (0x0042EDD8,'mov','dword ptr [edi + 0x34], eax'),
 (0x0042EE22,'mov','dword ptr [edi + 0x28], eax'),
 (0x0042EE25,'mov','dword ptr [edi + 0x2c], eax'),
 (0x0042EE28,'mov','dword ptr [edi + 0x30], eax'),
 (0x0042EE2B,'mov','dword ptr [edi + 0x34], eax'),
 (0x0042F6FB,'mov','edi, 0x434a30'),
 (0x0042F72A,'mov','dword ptr [esi + 0x438], eax'),
 (0x0042F738,'mov','dword ptr [ecx + 8], edi'),
 (0x0043B1E8,'sub','ebx, ecx'),
 (0x0043B1EA,'add','ebx, edx'),
 (0x0043B1F1,'jbe','0x43b1f8'),
 (0x0043B233,'inc','dword ptr [ebp - 0x14]'),
 (0x0043B252,'inc','eax'),
 (0x0043B280,'jmp','dword ptr [eax*4 + 0x43b5a0]'),
 (0x0043B2B8,'jae','0x43b3a7'),
 (0x0043B2DC,'mov','dword ptr [esi + 0x18], eax'),
 (0x0043B2E8,'call','0x446a60'),
 (0x0043B34D,'inc','ecx'),
 (0x0043B351,'mov','dword ptr [esi + 0x18], ecx'),
 (0x0043B367,'mov','dword ptr [ebp - 0x1c], 0'),
 (0x0043B36E,'call','0x448620'),
 (0x0043B37F,'call','0x4468f0'),
 (0x0043B404,'je','0x43b3a4'),
 (0x0043B41D,'inc','dword ptr [esi + 0x18]'),
 (0x0043B436,'mov','dword ptr [ebp - 0x1c], 0'),
 (0x0043B43D,'call','0x448620'),
 (0x0043B450,'mov','dword ptr [eax], ecx'),
 (0x0043B46A,'call','0x44c760'),
 (0x0043B528,'inc','dword ptr [ebp - 0x14]'),
 (0x0043B579,'call','0x43ad50'),
 (0x0043B594,'call','0x43a7f0'),
 (0x0043A7FF,'je','0x43a8c7'),
 (0x0043A83D,'inc','dword ptr [esi + 0x18]'),
 (0x0043ADA9,'inc','dword ptr [esi + 0x18]'),
 (0x0043AEEE,'inc','dword ptr [eax + 0x18]'),
 (0x0044C6E0,'mov','eax, dword ptr [ecx + 0x28]'),
 (0x0044C703,'mov','eax, dword ptr [ecx + 0x2c]'),
 (0x0044C733,'mov','eax, dword ptr [ecx + 0x30]'),
 (0x00434A3D,'cmp','edx, eax'),
 (0x00434A3F,'jg','0x434a6e'),
 (0x00434A41,'sub','eax, edx'),
 (0x00434A4A,'add','dword ptr [ecx + 0x8b8], eax'),
 (0x00434A59,'add','dword ptr [ecx + 0x8bc], eax'),
 (0x00434A68,'add','dword ptr [ecx + 0x8c0], eax'),
]

def audit(exe,packages):
 raw=exe.read_bytes();digest=hashlib.sha256(raw).hexdigest().upper()
 if digest!=EXPECTED:raise ValueError('Wrong executable identity; rejected before PE imports/VAs')
 repo=Path(__file__).resolve().parents[3]
 prior=runpy.run_path(str(repo/'poc/round31-tests/Networking/static_audit.py'))['audit'](exe,packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=raw,fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase;read=lambda va,n:pe.get_data(va-base,n);md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,mnemonic,operand in ANCHORS:
  i=next(md.disasm(read(va,15),va));assert (i.mnemonic,i.op_str)==(mnemonic,operand),(hex(va),i.mnemonic,i.op_str)
 assert struct.unpack('<5I',read(0x0043B5A0,20))==(0x0043B29E,0x0043B312,0x0043B3CC,0x0043B474,0x0043B592)
 assert prior['Fields'][13]['Vtable']==prior['Fields'][14]['Vtable']=='015DD07C'
 return dict(ExeSha256=digest,InheritedAnchors=prior['InstructionAnchors'],HamAnchors=len(ANCHORS),Fields=[13,14],OriginalExecuted=False,Kernel='unsigned skip; whole assignment; zero growth; modulo counter; conditional no-ops',CallbackPolicy='HAM replacement has owner callback; modern receipts do not execute it')
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
 repo=Path(__file__).resolve().parents[3];assert a.receipt.resolve().is_relative_to((repo/'local-private').resolve())
 try:r=audit(a.exe,a.packages)
 except ValueError as e:p.exit(1,str(e)+'\n')
 a.receipt.write_bytes((json.dumps(r,indent=2)+'\n').encode());print(f"PASS {r['HamAnchors']} HAM anchors plus {r['InheritedAnchors']} inherited anchors, exact jump table/RTTI; static only")
