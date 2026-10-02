"""Read-only original source identity and backend point-constant packing anchors."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import re
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'local-private/toolchain/python-static-analysis-packages'))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('dll',type=Path);a=p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
    assert hashlib.sha256(a.dll.read_bytes()).hexdigest().upper()=='D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D'
    sources={}
    with contextlib.ExitStack() as stack:
        indices=[(n,Index(stack.enter_context(path.open('rb')))) for n,path in archives(a.root,a.configuration)]
        for path,sha in [('vertex_program/include/functions.inc','CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15'),('vertex_program/include/vertex_shader_constants.inc','C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9'),('shared_program/functions.inc','A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A')]:
            priority,data=next((n,d) for n,index in indices if (d:=index.lookup(path)) is not None)
            assert hashlib.sha256(data).hexdigest().upper()==sha;sources[path]=dict(priority=priority,bytes=len(data),sha256=sha,text=data.decode('ascii'))
    source=sources['vertex_program/include/functions.inc']['text']
    compact=re.sub(r'\s+','',re.sub(r'//[^\r\n]*','',source))
    assert compact.count('floatdistanceAttenuation=1.0/dot(light.attenuation,attenuationFactors);')==2
    assert 'attenuationFactors.w=oneOverLightDistance;' in compact and 'attenuationFactors.w=1.0;' in compact
    assert compact.count('lit(nDotL,nDotH,material.specularPower.x)')==2
    assert 'output.diffuse=material.emissiveColor;' in compact
    assert re.search(r'float\s+lengthSquared\(float3 v\)\s*\{\s*return dot\(v, v\);\s*\}',sources['shared_program/functions.inc']['text'])
    constants=sources['vertex_program/include/vertex_shader_constants.inc']['text'];assert 'Material  material : register(c11);' in constants and 'LightData lightData : register(c16);' in constants
    pe=pefile.PE(str(a.dll),fast_load=True);md=Cs(CS_ARCH_X86,CS_MODE_32)
    anchors=[(0x62a0a457,'mov','ecx, 0x70'),(0x62a0a462,'rep stosd','dword ptr es:[edi], eax'),(0x62a0a490,'mov','ebx, dword ptr [edi*4 + 0x62b83890]'),(0x62a0a69a,'mov','ebx, 0x62b83894'),(0x62a0a6ff,'mov','esi, dword ptr [0x62b8389c]'),(0x62a0a76d,'fld','dword ptr [esi + 0x100]'),(0x62a0a779,'fld','dword ptr [esi + 0x104]'),(0x62a0a787,'fld','dword ptr [esi + 0x108]'),(0x62a0a78d,'mov','dword ptr [ebp - 0x140], 0'),(0x62a0a7c8,'mov','dword ptr [ebp - 0x14c], 0x3f800000'),(0x62a0a7d2,'mov','ebx, 0x62b838a0'),(0x62a0a82d,'fld','dword ptr [edi + 0x100]'),(0x62a0a836,'fld','dword ptr [edi + 0x104]'),(0x62a0a83f,'fld','dword ptr [edi + 0x108]'),(0x62a0a845,'mov','dword ptr [esi + 0x24], 0'),(0x62a0a851,'mov','dword ptr [esi + 0x18], 0x3f800000'),(0x62a0a85b,'add','esi, 0x30'),(0x62a0a86a,'push','0x1c'),(0x62a0a873,'push','0x10'),(0x62a0a875,'call','0x62a0e210')]
    for va,mnemonic,operand in anchors:
        instruction=next(md.disasm(pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,15),va));assert (instruction.mnemonic,instruction.op_str)==(mnemonic,operand),(hex(va),instruction.mnemonic,instruction.op_str)
    print(json.dumps(dict(baseline_dll_verified=True,configuration_verified=True,source_identities={k:{f:v for f,v in row.items() if f!='text'} for k,row in sources.items()},instruction_anchors=len(anchors),point_coefficient_offsets=[256,260,264],fourth_coefficient=0,empty_point_constant_coefficient=1,vertex_constant_upload=[16,28],source_reciprocal_uncapped=True,compiled_lit_power_behavior='unknown',dynamic_original_execution=False)))


if __name__=='__main__':main()
