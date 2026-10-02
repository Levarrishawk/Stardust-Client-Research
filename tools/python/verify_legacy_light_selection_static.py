"""Read-only baseline identity and instruction anchors for recovered light slots/masks/ranking."""
import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'local-private/toolchain/python-static-analysis-packages'))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('exe',type=Path);parser.add_argument('dll',type=Path);a=parser.parse_args()
    expected=[(a.exe,'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'),(a.dll,'D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D')]
    images=[]
    for path,sha in expected:
        assert hashlib.sha256(path.read_bytes()).hexdigest().upper()==sha;images.append(pefile.PE(str(path),fast_load=True))
    anchors=[(0,0x00773606,'mov','al, byte ptr [edi + 0x11c]'),(0,0x00773635,'mov','dword ptr [0x1923520], eax'),(0,0x0077363a,'mov','al, byte ptr [edi + 0x11d]'),(0,0x00773662,'or','dword ptr [0x192351c], eax'),(0,0x006e5ee1,'mov','byte ptr [eax + 0x11d], 0'),(0,0x006e5fcd,'mov','byte ptr [esi + 0x11c], 0'),(0,0x00773938,'push','0x192351c'),(0,0x0077394e,'push','0x1923520'),(1,0x62a0a0c0,'lea','eax, [ecx + 0xa4]'),(1,0x62a0a0d0,'fld','dword ptr [ecx + 0xb4]'),(1,0x62a0a0e0,'lea','eax, [ecx + 0xbc]'),(1,0x62a0a0f0,'fld','dword ptr [ecx + 0xcc]'),(1,0x62a0a100,'lea','eax, [ecx + 0xd0]'),(1,0x62a0a110,'fld','dword ptr [ecx + 0xe0]'),(1,0x62a0a120,'lea','eax, [ecx + 0xe8]'),(1,0x62a0a130,'fld','dword ptr [ecx + 0xf8]'),(1,0x62a0a221,'mov','edi, 0x62b8389c'),(1,0x62a0a226,'cmp','edi, 0x62b838a0'),(1,0x62a0a270,'cmp','edi, 0x62b838b0'),(1,0x62a0a2b0,'mov','edi, 0x62b83890'),(1,0x62a0a2b5,'cmp','edi, 0x62b83894'),(1,0x62a0a2f7,'cmp','edi, 0x62b8389c'),(1,0x62a0a2d5,'fcomp','dword ptr [ebp - 8]'),(1,0x62a0a2da,'test','ah, 5'),(1,0x62a0a2dd,'jp','0x62a0a2e5')]
    md=Cs(CS_ARCH_X86,CS_MODE_32)
    for image,va,mnemonic,operand in anchors:
        pe=images[image];instruction=next(md.disasm(pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,15),va));assert (instruction.mnemonic,instruction.op_str)==(mnemonic,operand),(hex(va),instruction.mnemonic,instruction.op_str)
    pe=images[0];weights=struct.unpack('<3f',pe.get_data(0x015dd878-pe.OPTIONAL_HEADER.ImageBase,12));assert weights==tuple(struct.unpack('<3f',struct.pack('<3f',.3,.59,.11)))
    print(json.dumps(dict(baseline_hashes_verified=2,instruction_anchors=len(anchors),absolute_rgb_score_weights=weights,directional_slots=[1,2],point_slots=[1,4],dynamic_original_execution=False)))

if __name__=='__main__':main()
