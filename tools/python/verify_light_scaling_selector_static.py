"""Hash-gated client configuration -> StaticShader snapshot -> raster light-color callback proof."""
import argparse
import hashlib
import json
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'local-private/toolchain/python-static-analysis-packages'))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('exe',type=Path);p.add_argument('dll',type=Path);a=p.parse_args()
    expected=[(a.exe,'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'),(a.dll,'D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D')];images=[]
    for path,sha in expected:
        assert hashlib.sha256(path.read_bytes()).hexdigest().upper()==sha;images.append(pefile.PE(str(path),fast_load=True))
    anchors=[(0,0x00779c93,'push','1'),(0,0x00779c95,'push','0x188e990'),(0,0x00779c9a,'push','0x188dac0'),(0,0x00779ca4,'call','0xa9cef0'),(0,0x00779cb5,'mov','byte ptr [0x1923544], al'),(0,0x00779e50,'mov','al, byte ptr [0x1923544]'),(0,0x00769afb,'call','0x779e50'),(0,0x00769b02,'mov','byte ptr [esi + 0x12], al'),(0,0x00769c0a,'call','0x779e50'),(0,0x00769c11,'mov','byte ptr [esi + 0x12], al'),(0,0x00769cdd,'mov','cl, byte ptr [ebx + 0x12]'),(0,0x00769ce0,'mov','byte ptr [edi + 0x12], cl'),(1,0x62a046ba,'mov','ecx, dword ptr [esi + 0xc]'),(1,0x62a046c5,'mov','cl, byte ptr [esi + 0x12]'),(1,0x62a046c9,'call','0x62a0a050'),(1,0x62a0a066,'mov','dword ptr [0x62b837f4], 0x62a0a0f0'),(1,0x62a0a070,'mov','dword ptr [0x62b837f0], 0x62a0a0e0'),(1,0x62a0a07a,'mov','dword ptr [0x62b837ec], 0x62a0a130'),(1,0x62a0a084,'mov','dword ptr [0x62b837e8], 0x62a0a120'),(1,0x62a0a090,'mov','dword ptr [0x62b837f4], 0x62a0a0d0'),(1,0x62a0a09a,'mov','dword ptr [0x62b837f0], 0x62a0a0c0'),(1,0x62a0a0a4,'mov','dword ptr [0x62b837ec], 0x62a0a110'),(1,0x62a0a0ae,'mov','dword ptr [0x62b837e8], 0x62a0a100')]
    md=Cs(CS_ARCH_X86,CS_MODE_32)
    for image,va,mnemonic,operand in anchors:
        pe=images[image];instruction=next(md.disasm(pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,15),va));assert (instruction.mnemonic,instruction.op_str)==(mnemonic,operand),(hex(va),instruction.mnemonic,instruction.op_str)
    pe=images[0]
    def literal(va):return pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,64).split(b'\0')[0].decode('ascii')
    assert literal(0x0188dac0)=='ClientGraphics' and literal(0x0188e990)=='enableLightScaling'
    print(json.dumps(dict(baseline_hashes_verified=2,instruction_anchors=len(anchors),configuration_section='ClientGraphics',configuration_key='enableLightScaling',default=True,global_va='01923544',getter_va='00779E50',shader_snapshot_offset=18,copied_shader_preserves_snapshot=True,true_selects='scaled RGB and scaled cached scores',false_selects='raw RGB and raw cached scores',effective_external_configuration='not inferred from absence',dynamic_original_execution=False)))


if __name__=='__main__':main()
