"""Static, SHA-gated two-body CreatureObject update carrier audit."""
import argparse
import hashlib
import json
import runpy
import sys
from pathlib import Path

EXE_SHA = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
BODIES = [
    (0x430260, 2848, 882, '59f2babe65ab48b18824dbab63e77f2d6a1f4ce21ba487f9274902edc777dd28', [0x4302ea, 0x430d7d], 127),
    (0x63f3b0, 309, 96, 'af01c6b4ad50658f6fbc6355fec4bdf3a0fe5af8705ff5e5f6787598e9462bc0', [0x63f4e2], 8),
]
ANCHORS = [
    (0x430282, 'mov', 'edi, ecx'),
    (0x4302b3, 'mov', 'esi, dword ptr [ebp + 8]'),
    (0x4302b6, 'je', '0x4302c0'),
    (0x4302b8, 'push', 'esi'), (0x4302b9, 'mov', 'ecx, eax'),
    (0x4302bb, 'call', '0x660e10'),
    (0x4302c0, 'push', 'esi'), (0x4302c1, 'mov', 'ecx, edi'),
    (0x4302c3, 'call', '0x63f3b0'),
    (0x4302c8, 'fcomp', 'dword ptr [0x15ec4c4]'),
    (0x4302d3, 'jp', '0x4302ed'), (0x4302ea, 'ret', '4'),
    (0x430b0e, 'mov', 'dword ptr [ebp + 8], eax'),
    (0x430bc2, 'mov', 'dword ptr [ebp + 8], eax'),
    (0x430c67, 'mov', 'dword ptr [ebp + 8], eax'),
    (0x430d7d, 'ret', '4'),
    (0x63f3b6, 'mov', 'eax, dword ptr [ebp + 8]'),
    (0x63f3ba, 'push', 'eax'), (0x63f3bb, 'mov', 'esi, ecx'),
    (0x63f3bd, 'call', '0x554c60'),
    (0x63f3c2, 'fst', 'dword ptr [ebp + 8]'),
    (0x63f3c5, 'fcomp', 'dword ptr [0x15ec4c4]'),
    (0x63f3d0, 'jnp', '0x63f4db'),
    (0x63f4db, 'fld', 'dword ptr [ebp + 8]'),
    (0x63f4e2, 'ret', '4'),
]


def exact(read, va, size):
    if size < 0:
        raise ValueError('Negative span')
    data = read(va, size)
    if len(data) != size:
        raise ValueError('Truncated span')
    return data


def audit(exe, packages, server_root, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXE_SHA:
        raise ValueError('Wrong executable SHA before imports/VA')
    repo = repo or Path(__file__).resolve().parents[2]
    frozen = runpy.run_path(str(repo / 'tools/python/verify_player_controller_update_units.py'))['audit'](exe, packages, server_root, repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    read = lambda va, size: pe.get_data(va - pe.OPTIONAL_HEADER.ImageBase, size)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    bodies = []
    decoded = {}
    for va, size, count, digest, returns, branch_count in BODIES:
        data = exact(read, va, size)
        if hashlib.sha256(data).hexdigest() != digest:
            raise ValueError('Carrier body digest mismatch')
        ins = list(md.disasm(data, va))
        addresses = {i.address for i in ins}
        if len(ins) != count or sum(i.size for i in ins) != size or ins[-1].mnemonic != 'ret':
            raise ValueError('Incomplete carrier extent')
        if [(i.address, i.op_str) for i in ins if i.mnemonic == 'ret'] != [(r, '4') for r in returns]:
            raise ValueError('Carrier return forms')
        branches = []
        for i in ins:
            if i.mnemonic.startswith('j') and i.op_str.startswith('0x'):
                target = int(i.op_str, 16)
                if target not in addresses:
                    raise ValueError('Carrier branch misses instruction boundary')
                branches.append((i.address, target))
        if len(branches) != branch_count:
            raise ValueError('Carrier branch count')
        decoded[va] = ins
        bodies.append(dict(Va=f'{va:08X}', Bytes=size, Instructions=count, Sha256=digest, DirectBranches=branch_count, Returns=[f'{r:08X}' for r in returns]))
    for va, mnemonic, operands in ANCHORS:
        i = next(md.disasm(exact(read, va, 15), va))
        if (i.mnemonic, i.op_str) != (mnemonic, operands):
            raise ValueError(f'Anchor mismatch {va:08X}')
    for start, site, target in [(0x430260, 0x4302c3, '0x63f3b0'), (0x63f3b0, 0x63f3bd, '0x554c60')]:
        ins = decoded[start]
        if [i.address for i in ins if i.mnemonic == 'call' and i.op_str == target] != [site]:
            raise ValueError('Carrier call uniqueness')
        if any(i.address < site and i.op_str.startswith('dword ptr [ebp + 8],') for i in ins):
            raise ValueError('Input slot overwritten before carrier call')
        if any(i.address < site and i.mnemonic.startswith('j') and i.op_str.startswith('0x') and int(i.op_str, 16) > site for i in ins):
            raise ValueError('Pre-call branch skips carrier call')
        if any(i.address < site and i.mnemonic == 'ret' for i in ins):
            raise ValueError('Return before carrier call')
        if any(i.address > site and i.mnemonic.startswith('j') and i.op_str.startswith('0x') and int(i.op_str, 16) <= site for i in ins):
            raise ValueError('Post-call branch re-enters carrier prefix')
    return dict(ExecutableSha256=EXE_SHA, Anchors=len(ANCHORS), Bodies=bodies,
                NewCalleesFollowed=['00430260', '0063F3B0'], NewBodyBudget=2,
                Carrier='00430260 ->0063F3B0 ->00554C60: unchanged incoming word and original object ECX on ordinary normal-return paths',
                ConditionalBoundaryUnits='seconds at00554C60 on accepted ordinary scheduler route',
                PlayerUnits='Unknown across00554C60 and selected controller receiver binding',
                UnresolvedCarrier='00554C60 ->00B23100/00B3D1C0',
                OptionalSideCall='00660E10 receives same word with separate ECX; body not followed',
                LaterArgumentScratch=['00430B0E', '00430BC2', '00430C67'],
                IntermediateReturnScratch='0063F3C2 stores returned ST0 into argument slot;0063F4DB reloads result',
                OriginalExecuted=False, NativeAccelerationAdded=False,
                WholeProgramCallerCensus=False, StartupFpuSurveyRepeated=False,
                FrozenCallerEvidence=frozen)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'receipt']:
        ap.add_argument('--' + name, type=Path, required=True)
    args = ap.parse_args()
    repo = Path(__file__).resolve().parents[2]
    guard = runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    try:
        target = guard(args.receipt, repo)
        result = audit(args.exe, args.packages, args.server_root, repo)
        with target.open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(json.dumps(result, indent=2) + '\n')
    except (ValueError, OSError, ImportError, StopIteration) as error:
        ap.exit(1, str(error) + '\n')
    print('PASS two complete carrier bodies; unchanged word/receiver to00554C60; actual player units remain Unknown')


if __name__ == '__main__':
    main()
