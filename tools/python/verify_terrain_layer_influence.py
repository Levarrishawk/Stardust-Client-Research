"""SHA-gated static per-sample layer influence anchors; never executes the original."""
import argparse, json, struct
from pathlib import Path
from verify_terrain_sample_coordinates import verify

ANCHORS = {
    0xB63F49: ('fld', 'dword ptr [edx + ecx*4]'),
    0xB63F4C: ('mov', 'dword ptr [ebp - 0x14], 0'),
    0xB63F53: ('fstp', 'dword ptr [ebp - 0x2c]'),
    0xB63F72: ('jle', '0xb64026'),
    0xB63F82: ('jle', '0xb64026'),
    0xB63F8E: ('call', '0xb62f20'),
    0xB63F95: ('je', '0xb64010'),
    0xB63F9A: ('mov', 'byte ptr [ebp - 0xe], 1'),
    0xB63FAF: ('call', 'dword ptr [edx + 0x24]'),
    0xB63FB7: ('jmp', 'dword ptr [edi*4 + 0xb642a0]'),
    0xB63FE0: ('fst', 'dword ptr [ebp - 0x34]'),
    0xB63FE3: ('fld', 'dword ptr [ebp - 0x14]'),
    0xB63FE6: ('fcomp', 'st(1)'),
    0xB63FEA: ('fstp', 'st(0)'),
    0xB63FEC: ('test', 'ah, 5'),
    0xB63FEF: ('lea', 'eax, [ebp - 0x34]'),
    0xB63FF2: ('jnp', '0xb63ff7'),
    0xB63FF4: ('lea', 'eax, [ebp - 0x14]'),
    0xB63FFD: ('fst', 'dword ptr [ebp - 0x14]'),
    0xB64000: ('fcomp', 'dword ptr [0x15dc6f8]'),
    0xB64008: ('test', 'ah, 0x44'),
    0xB6400B: ('jnp', '0xb6402d'),
    0xB64024: ('jne', '0xb6402d'),
    0xB64026: ('mov', 'dword ptr [ebp - 0x14], 0x3f800000'),
    0xB6402D: ('mov', 'al, byte ptr [esi + 0x50]'),
    0xB6403A: ('fsub', 'dword ptr [ebp - 0x14]'),
    0xB6403D: ('fstp', 'dword ptr [ebp - 0x14]'),
    0xB6404B: ('test', 'ah, 0x41'),
    0xB6404E: ('jne', '0xb641c9'),
    0xB64059: ('jle', '0xb64124'),
    0xB64069: ('jle', '0xb64124'),
    0xB64076: ('call', '0xb62f20'),
    0xB6407D: ('je', '0xb64115'),
    0xB640B8: ('call', 'dword ptr [edx + 0x14]'),
    0xB640C0: ('jmp', 'dword ptr [edi*4 + 0xb642b0]'),
    0xB640E9: ('fst', 'dword ptr [ebp - 0x34]'),
    0xB640EC: ('fcomp', 'dword ptr [ebp - 0x14]'),
    0xB640F1: ('test', 'ah, 5'),
    0xB640F4: ('lea', 'eax, [ebp - 0x34]'),
    0xB640F7: ('jnp', '0xb640fc'),
    0xB640F9: ('lea', 'eax, [ebp - 0x14]'),
    0xB64102: ('fst', 'dword ptr [ebp - 0x14]'),
    0xB64105: ('fcomp', 'dword ptr [0x15dc6a0]'),
    0xB6410D: ('test', 'ah, 0x44'),
    0xB64110: ('jnp', '0xb64124'),
    0xB64124: ('mov', 'al, byte ptr [esi + 0x51]'),
    0xB64131: ('fsub', 'dword ptr [ebp - 0x14]'),
    0xB64134: ('fstp', 'dword ptr [ebp - 0x14]'),
    0xB64142: ('test', 'ah, 0x41'),
    0xB64145: ('jne', '0xb641c9'),
    0xB64166: ('call', '0xb62f20'),
    0xB6416D: ('je', '0xb641c1'),
    0xB64175: ('fmul', 'dword ptr [ebp - 0x2c]'),
    0xB6417C: ('fstp', 'dword ptr [esp]'),
    0xB6418C: ('call', 'dword ptr [edx + 0x10]'),
    0xB641DA: ('fmul', 'dword ptr [ebp - 0x2c]'),
    0xB641E3: ('fstp', 'dword ptr [ecx + eax*4]'),
    0xB64237: ('jne', '0xb6423c'),
    0xB64239: ('lea', 'eax, [esi + 0x70]'),
    0xB6423E: ('call', '0xb63d20'),
}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True)
    p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    if 'local-private' not in a.receipt.parts:p.error('Private receipt required')
    inherited=verify(a.exe,a.packages)  # SHA rejected before any address decoding.
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(str(a.exe),fast_load=True);mapped=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase
    decoder=Cs(CS_ARCH_X86,CS_MODE_32)
    rows={i.address:(i.mnemonic,i.op_str) for i in decoder.disasm(mapped[0xB63D20-base:0xB642A0-base],0xB63D20)}
    for va,want in ANCHORS.items():
        if rows.get(va)!=want:raise ValueError(f'{va:08X}: {rows.get(va)} != {want}')
    tables={0xB642A0:[0xB63FE0,0xB63FBE,0xB63FC4,0xB63FC8],0xB642B0:[0xB640E9,0xB640C7,0xB640CD,0xB640D1]}
    for va,want in tables.items():assert list(struct.unpack('<4I',mapped[va-base:va-base+16]))==want
    for va,want in [(0x15DC6A0,0),(0x15DC6F8,0x3F800000),(0x15DD850,0x40400000)]:
        assert struct.unpack('<I',mapped[va-base:va-base+4])[0]==want
    # TEST sets PF on even low-byte parity. JNP is odd parity, not a magnitude test.
    outcomes={'less':1,'equal':0x40,'greater':0,'unordered':0x45}
    odd=lambda value:value.bit_count()%2==1
    assert {name:odd(ah&5) for name,ah in outcomes.items()}==dict(less=True,equal=False,greater=False,unordered=False)
    assert {name:odd(ah&0x44) for name,ah in outcomes.items()}==dict(less=False,equal=True,greater=False,unordered=False)
    assert {name:bool(ah&0x41) for name,ah in outcomes.items()}==dict(less=True,equal=True,greater=False,unordered=True)
    result={**inherited,'LayerInstructionAnchors':len(ANCHORS),'SelectorTables':2,'Constants':3,'StatusBranchCases':12,
            'Scope':'general per-sample local combine and incoming product','OriginalArithmeticProfiles':'Unknown','OriginalExecution':False}
    a.receipt.parent.mkdir(parents=True,exist_ok=True);a.receipt.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result))
if __name__=='__main__':main()
