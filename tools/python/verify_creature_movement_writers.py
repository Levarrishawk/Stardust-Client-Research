"""SHA-gated static speed wrapper writers/readers; no original process execution."""
import argparse
import hashlib
import json
import runpy
import struct
import sys
from pathlib import Path

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (0x439AC3, 'add', 'ecx, 0xc'), (0x439AC6, 'push', '4'),
    (0x439ACC, 'call', '0x118b0e0'),
    (0x439B39, 'push', '4'), (0x439B3B, 'lea', 'eax, [esi + 0xc]'),
    (0x439B3F, 'call', '0x439910'), (0x439B48, 'call', 'dword ptr [edx + 0xc]'),
    (0x439B59, 'push', '4'), (0x439B5B, 'lea', 'eax, [esi + 0xc]'),
    (0x439B5F, 'call', '0x439910'), (0x439B64, 'mov', 'ecx, dword ptr [esi + 8]'),
    (0x439B69, 'je', '0x439b71'), (0x439B6C, 'call', '0x118a7c0'),
    (0x439A90, 'mov', 'eax, dword ptr [ecx + 0xc]'),
    (0x439A93, 'mov', 'dword ptr [ecx + 0x10], eax'),
    (0x439B03, 'fld', 'dword ptr [ecx + 0xc]'),
    (0x439B09, 'fcomp', 'dword ptr [edx]'), (0x439B0D, 'test', 'ah, 0x44'),
    (0x439B10, 'jnp', '0x439b26'), (0x439B14, 'mov', 'dword ptr [ecx + 0xc], eax'),
    (0x439B1C, 'je', '0x439b26'), (0x439B21, 'call', '0x118a7c0'),
    (0x43993F, 'call', '0x118b030'), (0x43994B, 'mov', 'dword ptr [esi], eax'),
    (0x118B062, 'cmp', 'edi, ebx'), (0x118B065, 'ja', '0x118b092'),
    (0x118B076, 'rep movsd', 'dword ptr es:[edi], dword ptr [esi]'),
    (0x118B07D, 'rep movsb', 'byte ptr es:[edi], byte ptr [esi]'),
    (0x118AA86, 'mov', 'dword ptr [ecx + 8], 0'),
    (0x43003A, 'lea', 'ecx, [esi + 0x668]'), (0x43004A, 'call', '0x118aa80'),
    (0x43004F, 'lea', 'ecx, [esi + 0x654]'), (0x43005F, 'call', '0x118aa80'),
    (0x4388A7, 'mov', 'eax, dword ptr [esi + 0x674]'),
    (0x4388B5, 'jne', '0x43892b'), (0x4388B7, 'push', '0x1c'),
    (0x4388B9, 'call', '0x435250'), (0x4388C0, 'je', '0x43892b'),
    (0x4388CF, 'call', '0x11b5a80'), (0x4388D9, 'je', '0x43890e'),
    (0x4388DC, 'call', '0x648080'), (0x4388E6, 'je', '0x43892b'),
    (0x4388E8, 'mov', 'eax, dword ptr [eax + 0x30]'),
    (0x4388FA, 'call', '0x131e6f1'), (0x438904, 'je', '0x43892b'),
    (0x438906, 'mov', 'ecx, dword ptr [eax + 0xc]'),
    (0x438910, 'call', '0x438640'), (0x438917, 'je', '0x43892b'),
    (0x438919, 'call', '0x425200'), (0x438920, 'je', '0x43892b'),
    (0x438922, 'mov', 'edx, dword ptr [eax + 0x674]'),
    (0x43892D, 'call', '0x438a70'), (0x438932, 'fstp', 'dword ptr [ebp + 8]'),
    (0x438937, 'call', '0x438a20'), (0x43893C, 'fmul', 'dword ptr [ebp + 8]'),
    (0x438940, 'fmul', 'dword ptr [ebp - 4]'),
    (0x438957, 'mov', 'eax, dword ptr [esi + 0x660]'),
    (0x438966, 'jne', '0x4389f3'), (0x438975, 'je', '0x4389f3'),
    (0x43898E, 'je', '0x4389c9'), (0x4389AF, 'call', '0x131e6f1'),
    (0x4389B9, 'je', '0x4389f3'), (0x4389BB, 'fld', 'dword ptr [eax + 0xc]'),
    (0x4389BE, 'fmul', 'dword ptr [0x15dc5e4]'),
    (0x4389C4, 'fstp', 'dword ptr [ebp - 4]'),
    (0x4389D2, 'je', '0x4389f3'), (0x4389D4, 'call', '0x425200'),
    (0x4389DD, 'je', '0x4389f3'), (0x4389E1, 'call', '0x438690'),
    (0x4389E6, 'cmp', 'eax, esi'), (0x4389E8, 'jne', '0x4389f3'),
    (0x4389EA, 'mov', 'ecx, dword ptr [edi + 0x660]'),
    (0x4389F5, 'call', '0x438a70'), (0x4389FA, 'fstp', 'dword ptr [ebp + 8]'),
    (0x4389FF, 'call', '0x438a20'), (0x438A04, 'fmul', 'dword ptr [ebp + 8]'),
    (0x438A09, 'fmul', 'dword ptr [ebp - 4]'),
    (0x438A27, 'mov', 'eax, dword ptr [esi + 0x638]'),
    (0x438A39, 'je', '0x438a5b'), (0x438A44, 'je', '0x438a5b'),
    (0x438A4D, 'je', '0x438a5b'), (0x438A51, 'call', '0x438a20'),
    (0x438A5B, 'fld', 'dword ptr [ebp - 4]'),
    (0x438A77, 'mov', 'eax, dword ptr [esi + 0x64c]'),
    (0x438A89, 'je', '0x438aab'), (0x438A94, 'je', '0x438aab'),
    (0x438A9D, 'je', '0x438aab'), (0x438AA1, 'call', '0x438a70'),
    (0x438AAB, 'fld', 'dword ptr [ebp - 4]'),
    (0x438640, 'mov', 'al, byte ptr [ecx + 0x994]'),
    (0x438690, 'jmp', '0x438650'),
    (0x425200, 'mov', 'eax, dword ptr [0x190885c]'),
    (0x431917, 'call', '0x4388a0'), (0x431927, 'call', '0x438950'),
    (0x43193C, 'call', '0x438950'), (0x431941, 'fstp', 'dword ptr [ebp + 8]'),
    (0x431947, 'call', '0x4388a0'), (0x43194C, 'fst', 'dword ptr [ebp - 4]'),
    (0x43194F, 'fcomp', 'dword ptr [ebp + 8]'),
    (0x431959, 'lea', 'eax, [ebp + 8]'), (0x43195C, 'jnp', '0x431961'),
    (0x43195E, 'lea', 'eax, [ebp - 4]'), (0x431961, 'fld', 'dword ptr [eax]'),
    (0x629F7A, 'push', '0'), (0x629F7E, 'call', '0x4388a0'),
    (0x629F83, 'fstp', 'dword ptr [ebp - 0x14]'),
    (0x629F86, 'push', '0'), (0x629F8A, 'call', '0x438950'),
    (0x629F8F, 'fstp', 'dword ptr [ebp - 0x34]'),
]
FLOAT_VTABLE = (0x445370, 0x439AC0, 0x439B30, 0x439A90, 0x439AA0, 0x439AE0, 0x439B50, 0x439B00)

def audit(exe, packages, server_root, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before optional imports/VA use')
    repo = repo or Path(__file__).resolve().parents[2]
    prerequisite = runpy.run_path(str(repo / 'tools/python/verify_creature_view4_initialization.py'))['audit'](exe, packages, server_root, repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    read = lambda va, n: pe.get_data(va - pe.OPTIONAL_HEADER.ImageBase, n)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    for va, mnemonic, operand in ANCHORS:
        ins = next(md.disasm(read(va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operand):
            raise ValueError(f'Instruction mismatch at {va:08X}')
    if struct.unpack('<8I', read(0x15DCF0C, 32)) != FLOAT_VTABLE:
        raise ValueError('Float wrapper vtable mismatch')
    for va, name in [(0x186AFDC, b'.?AVVehicleHoverDynamics@@'), (0x186AFC4, b'.?AVDynamics@@')]:
        if read(va + 8, len(name) + 1) != name + b'\0':
            raise ValueError('Reader cast RTTI mismatch')
    # Bounded complete reader bodies: exclude hidden direct calls to template speed getter.
    counts = []
    for start, size in [(0x4388A0, 0xA9), (0x438950, 0xC2)]:
        instructions = list(md.disasm(read(start, size), start))
        if sum(i.size for i in instructions) != size or instructions[-1].mnemonic != 'ret':
            raise ValueError('Incomplete reader body')
        if any(i.mnemonic == 'call' and i.op_str == '0x11ade40' for i in instructions):
            raise ValueError('Unexpected direct template getter in runtime reader')
        counts.append(len(instructions))
    return dict(ExecutableSha256=EXPECTED_EXE, InstructionAnchors=len(ANCHORS), ReaderInstructionCounts=counts,
                FloatVtableEntries=8, ReaderRtti=2, Prerequisite=prerequisite,
                WrapperOffsets={'Run': 0x668, 'Walk': 0x654}, CurrentOffsets={'Run': 0x674, 'Walk': 0x660},
                RuntimeReaders=['004388A0', '00438950'], DirectCallerWitness='00629F7E/00629F8A',
                AllWritersExhausted=False, IndirectSideEffects='Unknown', Units='Unknown',
                OriginalExecuted=False, LiveContact=False, ModernRuntimeAdded=False)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args(); repo = Path(__file__).resolve().parents[2]
    try:
        target = runpy.run_path(str(repo / 'tools/python/verify_creature_view4_initialization.py'))['receipt_path'](args.receipt, repo)
        result = audit(args.exe, args.packages, args.server_root, repo)
        target.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    except (ValueError, AssertionError, OSError, ImportError, StopIteration) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['InstructionAnchors']} movement writer/reader anchors; two complete readers; transitive writers remain unknown")

if __name__ == '__main__':
    main()
