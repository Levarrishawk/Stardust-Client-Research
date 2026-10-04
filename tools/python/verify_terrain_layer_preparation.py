"""Hash-first exact enabled-count/bounds preparation; original process never runs."""
import argparse,importlib.util,json,struct
from pathlib import Path

ANCHORS={
    0x00B63C54: ('mov', 'eax, dword ptr [esi + 0x18]'),
    0x00B63C5E: ('mov', 'dword ptr [esi + 0x40], edi'),
    0x00B63C63: ('mov', 'eax, dword ptr [esi + 0x14]'),
    0x00B63C6C: ('call', '0xb62f20'),
    0x00B63C73: ('je', '0xb63c7f'),
    0x00B63C75: ('inc', 'dword ptr [esi + 0x40]'),
    0x00B63C7C: ('call', 'dword ptr [edx + 4]'),
    0x00B63C87: ('mov', 'eax, dword ptr [esi + 0x24]'),
    0x00B63C8E: ('mov', 'dword ptr [esi + 0x44], edi'),
    0x00B63C93: ('mov', 'eax, dword ptr [esi + 0x20]'),
    0x00B63C9C: ('call', '0xb62f20'),
    0x00B63CA3: ('je', '0xb63caf'),
    0x00B63CA5: ('inc', 'dword ptr [esi + 0x44]'),
    0x00B63CAC: ('call', 'dword ptr [edx + 4]'),
    0x00B63CB7: ('mov', 'eax, dword ptr [esi + 0x30]'),
    0x00B63CBE: ('mov', 'dword ptr [esi + 0x48], edi'),
    0x00B63CC3: ('mov', 'eax, dword ptr [esi + 0x2c]'),
    0x00B63CCC: ('call', '0xb62f20'),
    0x00B63CD3: ('je', '0xb63cdf'),
    0x00B63CD5: ('inc', 'dword ptr [esi + 0x48]'),
    0x00B63CDC: ('call', 'dword ptr [edx + 4]'),
    0x00B63CEA: ('mov', 'dword ptr [esi + 0x4c], edi'),
    0x00B63CF0: ('mov', 'eax, dword ptr [esi + 0x38]'),
    0x00B63CF9: ('call', '0xb62f20'),
    0x00B63D00: ('je', '0xb63d0c'),
    0x00B63D02: ('inc', 'dword ptr [esi + 0x4c]'),
    0x00B63D09: ('call', 'dword ptr [edx + 4]'),
    0x00B64733: ('mov', 'al, byte ptr [esi + 0x50]'),
    0x00B64739: ('mov', 'byte ptr [esi + 0x52], 0'),
    0x00B6473D: ('jne', '0xb647ac'),
    0x00B6473F: ('mov', 'eax, 0xff7fffff'),
    0x00B64744: ('mov', 'dword ptr [esi + 0x5c], eax'),
    0x00B64747: ('mov', 'dword ptr [esi + 0x60], eax'),
    0x00B6474E: ('lea', 'ebx, [esi + 0x54]'),
    0x00B64755: ('mov', 'dword ptr [ebx], 0x7f7fffff'),
    0x00B6475B: ('mov', 'dword ptr [esi + 0x58], 0x7f7fffff'),
    0x00B64764: ('mov', 'eax, dword ptr [esi + 0x14]'),
    0x00B6476A: ('call', '0xb62f20'),
    0x00B64771: ('je', '0xb6477d'),
    0x00B64773: ('mov', 'byte ptr [esi + 0x52], 1'),
    0x00B6477A: ('call', 'dword ptr [edx + 0x28]'),
    0x00B64785: ('mov', 'eax, dword ptr [esi + 0x3c]'),
    0x00B64790: ('mov', 'eax, dword ptr [esi + 0x38]'),
    0x00B64796: ('call', '0xb62f20'),
    0x00B6479D: ('je', '0xb647a4'),
    0x00B6479F: ('call', '0xb64730'),
    0x00B63D3F: ('mov', 'ecx, dword ptr [esi + 0x48]'),
    0x00B63D46: ('mov', 'eax, dword ptr [esi + 0x4c]'),
    0x00B63D4B: ('je', '0xb6428e'),
    0x00B63D51: ('mov', 'al, byte ptr [esi + 0x52]'),
    0x00B63D59: ('je', '0xb63dab'),
    0x00B63D5E: ('fcomp', 'dword ptr [ebx + 0x574]'),
    0x00B63D69: ('jnp', '0xb6428e'),
    0x00B63D72: ('fcomp', 'dword ptr [ebx + 0x57c]'),
    0x00B63D7D: ('je', '0xb6428e'),
    0x00B63D86: ('fcomp', 'dword ptr [ebx + 0x578]'),
    0x00B63D91: ('jnp', '0xb6428e'),
    0x00B63D9A: ('fcomp', 'dword ptr [ebx + 0x580]'),
    0x00B63DA5: ('jp', '0xb6428e'),
    0x00B63DAB: ('mov', 'eax, dword ptr [esi + 0x40]'),
    0x00B63DB2: ('mov', 'eax, dword ptr [esi + 0x44]'),
    0x00B63DB9: ('test', 'ecx, ecx'),
    0x00B63DBB: ('mov', 'byte ptr [ebp - 0xd], 1'),
    0x00B63DC1: ('mov', 'byte ptr [ebp - 0xd], 0'),
    0x00B63E04: ('mov', 'eax, dword ptr [esi + 0x4c]'),
    0x00B63E14: ('jne', '0xb63e53'),
    0x00B63E18: ('cmp', 'edi, dword ptr [esi + 0x70]'),
    0x00B63E1B: ('jg', '0xb63e22'),
    0x00B63E1D: ('cmp', 'edi, dword ptr [esi + 0x74]'),
    0x00B63E20: ('jle', '0xb63e53'),
    0x00B63E2A: ('call', '0xac1660'),
    0x00B63E3B: ('imul', 'eax, edi'),
    0x00B63E3E: ('shl', 'eax, 2'),
    0x00B63E42: ('mov', 'dword ptr [esi + 0x70], edi'),
    0x00B63E45: ('mov', 'dword ptr [esi + 0x74], edi'),
    0x00B63E48: ('call', '0xac15e0'),
    0x00B63E50: ('mov', 'dword ptr [esi + 0x78], eax'),
    0x00B65F36: ('call', '0xb62f20'),
    0x00B65F3D: ('je', '0xb65f44'),
    0x00B65F3F: ('call', '0xb64730'),
    0x00B64DBB: ('push', '0x30303033'),
    0x00B64DDD: ('mov', 'cl, byte ptr [edi + 0x50]'),
    0x00B64DF7: ('mov', 'cl, byte ptr [edi + 0x51]'),
    0x00B64E11: ('mov', 'dl, byte ptr [edi + 0x68]'),
    0x00B64E26: ('call', '0xa94870'),
    0x00612B4E: ('call', '0xb64730'),
    0x00612B83: ('mov', 'al, byte ptr [esi + 0x52]'),
    0x00612C0B: ('call', '0xb64730'),
}

def verify(exe,packages):
    source=Path(__file__).with_name('verify_terrain_layer_adta.py')
    spec=importlib.util.spec_from_file_location('preparation_adta',source);adta=importlib.util.module_from_spec(spec);spec.loader.exec_module(adta)
    prerequisite=adta.static(exe,packages) # Identity check precedes third-party imports/VA use.
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    raw=exe.read_bytes();pe=pefile.PE(data=raw,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase
    md=Cs(CS_ARCH_X86,CS_MODE_32)
    for va,expected in ANCHORS.items():
        ins=next(md.disasm(image[va-base:va-base+15],va,count=1));assert (ins.mnemonic,ins.op_str)==expected,(hex(va),ins.mnemonic,ins.op_str)
    assert struct.unpack_from('<5I',image,0x15ed1a4-base)==(0xb63600,0xb63c50,0xb62fa0,0xb64da0,0xb647c0)
    getter=list(md.disasm(image[0xb62f20-base:0xb62f24-base],0xb62f20));assert [(i.mnemonic,i.op_str) for i in getter]==[('mov','al, byte ptr [ecx + 8]'),('ret','')]
    status_checks=0
    # Independent finite x87 status cases, TEST result parity and Jcc interpretation.
    status={-1:1,0:0x40,1:0};even=lambda v:v.bit_count()%2==0
    for relation,ah in status.items():
        assert (not even(ah&5))==(relation<0);status_checks+=1
        assert ((ah&0x41)==0)==(relation>0);status_checks+=1
        assert even(ah&0x41)==(relation>0);status_checks+=1
    return dict(ExecutableSha256=prerequisite['ExecutableSha256'],InstructionAnchors=len(ANCHORS),PrerequisiteAnchors=prerequisite['InstructionAnchors'],GetterAnchors=len(getter),FiniteStatusChecks=status_checks,LayerVtableEntries=5,OriginalExecuted=False,OriginalCacheStateObserved=False,Flag68Meaning='Unknown')

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    if 'local-private' not in a.receipt.parts:p.error('Private receipt required')
    result=verify(a.exe,a.packages);a.receipt.parent.mkdir(parents=True,exist_ok=True);a.receipt.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
