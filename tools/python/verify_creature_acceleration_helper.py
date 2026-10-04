"""Bounded static acceleration-helper gate; never executes the original client."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
BODY_SHA = '03d39d78a24d6c95f9c14bba2a41e7883521b54134380dea627ab78d63523700'
WALK_SHA = '662d0bcba876c776f85d14c55da198ca9d0a176f4e5c95d0070fb1121cc81d63'
# Compact witnesses; complete decoded bodies stay in ignored private output.
ANCHORS = [
    (0x431840, 'push', 'ebp'), (0x431846, 'mov', 'esi, ecx'),
    (0x431844, 'push', '0x1c'), (0x431848, 'call', '0x435250'),
    (0x43184F, 'je', '0x4318b8'), (0x431851, 'mov', 'al, byte ptr [ebp + 0xc]'),
    (0x431856, 'jne', '0x4318b8'), (0x431858, 'push', '0x10000'),
    (0x43185F, 'call', '0x557360'), (0x431865, 'call', '0x11b5a80'),
    (0x43186F, 'je', '0x4318b8'), (0x431872, 'call', '0x648080'),
    (0x43187C, 'je', '0x4318b8'), (0x43187E, 'mov', 'eax, dword ptr [eax + 0x30]'),
    (0x431890, 'call', '0x131e6f1'), (0x43189A, 'je', '0x4318b8'),
    (0x43189C, 'mov', 'ecx, dword ptr [esi + 0x69c]'),
    (0x4318A2, 'mov', 'edx, dword ptr [ebp + 8]'),
    (0x4318A5, 'mov', 'dword ptr [ebp + 0xc], ecx'),
    (0x4318A8, 'push', 'edx'), (0x4318A9, 'mov', 'ecx, eax'),
    (0x4318AB, 'call', '0x11ac4c0'), (0x4318B0, 'fmul', 'dword ptr [ebp + 0xc]'),
    (0x4318B5, 'ret', '8'), (0x4318B8, 'push', '0'),
    (0x4318BA, 'mov', 'ecx, esi'), (0x4318BC, 'call', '0x431920'),
    (0x4318C1, 'fcomp', 'dword ptr [ebp + 8]'),
    (0x4318C4, 'mov', 'edx, dword ptr [esi + 0x69c]'),
    (0x4318CA, 'mov', 'ecx, dword ptr [esi + 0x10]'),
    (0x4318CD, 'mov', 'dword ptr [ebp + 0xc], edx'),
    (0x4318D0, 'fnstsw', 'ax'), (0x4318D2, 'test', 'ah, 1'),
    (0x4318D5, 'mov', 'eax, dword ptr [esi + 0x688]'),
    (0x4318DB, 'mov', 'dword ptr [ebp + 8], eax'), (0x4318DE, 'jne', '0x4318f2'),
    (0x4318E0, 'push', '1'), (0x4318E2, 'call', '0x11add30'),
    (0x4318E7, 'fmul', 'dword ptr [ebp + 0xc]'),
    (0x4318EB, 'fmul', 'dword ptr [ebp + 8]'), (0x4318EF, 'ret', '8'),
    (0x4318F2, 'push', '0'), (0x4318F4, 'call', '0x11add30'),
    (0x4318F9, 'fmul', 'dword ptr [ebp + 0xc]'),
    (0x4318FD, 'fmul', 'dword ptr [ebp + 8]'), (0x431901, 'ret', '8'),
    (0x431923, 'mov', 'eax, dword ptr [ebp + 8]'),
    (0x431926, 'push', 'eax'), (0x431927, 'call', '0x438950'), (0x43192D, 'ret', '4'),
    (0x62A001, 'mov', 'edx, dword ptr [ebx + 0x64]'), (0x62A004, 'push', '0'),
    (0x62A006, 'push', 'edx'), (0x62A007, 'mov', 'ecx, edi'),
    (0x62A009, 'call', '0x431840'), (0x62A00E, 'fmul', 'dword ptr [ebp + 8]'),
    (0x62A036, 'mov', 'ecx, dword ptr [ebx + 0x64]'), (0x62A039, 'push', '0'),
    (0x62A03B, 'push', 'ecx'), (0x62A03C, 'mov', 'ecx, edi'),
    (0x62A03E, 'call', '0x431840'), (0x62A043, 'fchs', ''),
    (0x62A045, 'fmul', 'dword ptr [ebp + 8]'),
]


def audit(exe, packages, server_root, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
    repo = repo or Path(__file__).resolve().parents[2]
    frozen_api = runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))
    frozen = frozen_api['audit'](exe, packages, server_root, repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    read = lambda va, size: pe.get_data(va - pe.OPTIONAL_HEADER.ImageBase, size)
    exact = frozen_api['exact']
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    bodies = []
    for start, size, count, digest in [(0x431840, 196, 70, BODY_SHA), (0x431920, 16, 7, WALK_SHA)]:
        body = exact(read, start, size)
        if hashlib.sha256(body).hexdigest() != digest:
            raise ValueError(f'Body digest mismatch at {start:08X}')
        instructions = list(md.disasm(body, start))
        if len(instructions) != count or sum(i.size for i in instructions) != size:
            raise ValueError('Incomplete bounded instruction extent')
        addresses = {i.address for i in instructions}
        branches = []
        for ins in instructions:
            if ins.mnemonic.startswith('j'):
                if not ins.op_str.startswith('0x') or int(ins.op_str, 16) not in addresses:
                    raise ValueError('Unbounded/unaligned helper branch')
                branches.append(dict(va=f'{ins.address:08X}', target=ins.op_str))
        returns = [i for i in instructions if i.mnemonic == 'ret']
        if instructions[-1].mnemonic != 'ret' or len(returns) != (3 if start == 0x431840 else 1):
            raise ValueError('Incomplete helper exits')
        if exact(read, start + size, 12 if start == 0x431840 else 0) != (b'\x90' * 12 if start == 0x431840 else b''):
            raise ValueError('Helper trailing extent separator mismatch')
        bodies.append(dict(Start=f'{start:08X}', Bytes=size, Instructions=count, Sha256=digest,
                           DirectBranches=branches, Returns=len(returns),
                           DirectCalls=[dict(va=f'{i.address:08X}', target=i.op_str) for i in instructions if i.mnemonic == 'call']))
    for va, mnemonic, operand in ANCHORS:
        ins = next(md.disasm(exact(read, va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operand):
            raise ValueError(f'Instruction mismatch at {va:08X}')
    return dict(ExecutableSha256=EXPECTED_EXE, Bodies=bodies, InstructionAnchors=len(ANCHORS),
                FrozenControllerEvidence=frozen, Receiver='ECX saved in ESI; selected controller object',
                Arguments=['F32 current speed word', 'low byte of second stack argument'],
                Return='x87 ST0; callee cleans eight argument bytes',
                Fallback='walk reader(0) C0 selects template argument0 if less/unordered, else1',
                CurrentFields={'Base': 'CREO4[0] wrapper+0xC at receiver+0x69C',
                               'Modifier': 'CREO4[1] wrapper+0xC at receiver+0x688'},
                TemplateGetterSemantics='Unknown', VehicleGetterSemantics='Unknown', Units='Unknown',
                ThreadAffinity='Unknown', TransitiveOwnership='Unknown', DirectReceiverWrites=0,
                CanonicalRename=False, NativeAccelerationAdded=False, OriginalExecuted=False, LiveContact=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    guard = runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    try:
        target = guard(args.receipt, repo)
        result = audit(args.exe, args.packages, args.server_root, repo)
        with target.open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(json.dumps(result, indent=2) + '\n')
    except (ValueError, OSError, ImportError, StopIteration) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['InstructionAnchors']} anchors; complete 196-byte helper and 16-byte walk wrapper")


if __name__ == '__main__':
    main()
