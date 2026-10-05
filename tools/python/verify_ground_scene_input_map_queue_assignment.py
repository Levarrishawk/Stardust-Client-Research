"""One GroundScene map acquisition and controller queue assignment path."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (5348449, 'mov', 'esi, ecx'),
    (5348991, 'mov', 'dword ptr [esi], 0x15dd988'),
    (5349222, 'mov', 'ecx, esi'),
    (5349224, 'call', '0x518eb0'),
    (5344977, 'mov', 'esi, ecx'),
    (5344987, 'mov', 'edi, dword ptr [ebp + 0xc]'),
    (5345816, 'push', '0x158'),
    (5345843, 'je', '0x519242'),
    (5345848, 'call', '0x629400'),
    (5345853, 'mov', 'dword ptr [ebp + 0xc], eax'),
    (5345861, 'mov', 'ecx, dword ptr [ebp + 0xc]'),
    (5345864, 'push', 'ecx'),
    (5345865, 'mov', 'ecx, edi'),
    (5345874, 'call', '0xb22bb0'),
    (5345881, 'mov', 'dword ptr [esi + 0x1c], edi'),
    (5346833, 'mov', 'ecx, esi'),
    (5346838, 'call', '0x51fad0'),
    (6460447, 'mov', 'ebx, ecx'),
    (6460856, 'mov', 'dword ptr [ebx], 0x15e034c'),
    (6460863, 'mov', 'eax, ebx'),
    (11676596, 'mov', 'esi, ecx'),
    (11676602, 'mov', 'edi, dword ptr [ebp + 8]'),
    (11676605, 'cmp', 'ecx, edi'),
    (11676607, 'je', '0xb22bce'),
    (11676611, 'je', '0xb22bcb'),
    (11676617, 'call', 'dword ptr [eax]'),
    (11676619, 'mov', 'dword ptr [esi + 0x2c], edi'),
    (7216925, 'mov', 'eax, dword ptr [0x191d214]'),
    (7216932, 'jne', '0x6e208c'),
    (7217087, 'push', '0'),
    (7217093, 'call', '0xaacd50'),
    (7217107, 'mov', 'dword ptr [0x191d214], eax'),
    (7217197, 'mov', 'esi, dword ptr [0x191d214]'),
    (7217203, 'test', 'esi, esi'),
    (7217205, 'je', '0x6e203d'),
    (7217207, 'inc', 'dword ptr [0x191d218]'),
    (7217228, 'mov', 'eax, esi'),
    (7217366, 'xor', 'eax, eax'),
    (5372624, 'push', 'esi'),
    (5372625, 'mov', 'esi, ecx'),
    (5372627, 'mov', 'eax, dword ptr [esi + 0x4c]'),
    (5372630, 'test', 'eax, eax'),
    (5372632, 'je', '0x51faf4'),
    (5372634, 'mov', 'dword ptr [eax + 0xc], 0'),
    (5372641, 'mov', 'eax, dword ptr [esi + 0x4c]'),
    (5372644, 'push', 'eax'),
    (5372645, 'call', '0x6e2350'),
    (5372650, 'add', 'esp, 4'),
    (5372653, 'mov', 'dword ptr [esi + 0x4c], 0'),
    (5372660, 'call', '0x6e1e70'),
    (5372665, 'mov', 'ecx, dword ptr [esi + 0x1c]'),
    (5372668, 'mov', 'dword ptr [esi + 0x4c], eax'),
    (5372671, 'mov', 'ecx, dword ptr [ecx + 0x2c]'),
    (5372674, 'call', '0xb3d340'),
    (5372679, 'mov', 'edx, dword ptr [esi + 0x4c]'),
    (5372682, 'mov', 'dword ptr [edx + 0xc], eax'),
    (5372685, 'pop', 'esi'),
    (5372686, 'ret', ''),
    (11785024, 'mov', 'eax, dword ptr [ecx + 4]'),
    (6465968, 'mov', 'eax, dword ptr [ecx + 4]'),
]
SPANS = [
    (5348416, 834, '236701041aead5b3007f293ec06d73cb2185220af8ae5ace88b5745a3e3017ae', 'ret'),
    (5344944, 2394, '35b62fb2580a413bec6f159bc3b21e0d786f913b17f1af108c640fd230b97629', 'ret'),
    (6460416, 463, '7e0ece87fff30632c54c37e069ba2b13bd945e9c7ad7dcba2089013cdb5d9ee9', 'ret'),
    (11676592, 36, 'a9c68691bf0ae8e8958e9abd3539e21bb361e6f11f2ce8ef69edbc8731c862b3', 'ret'),
    (5372624, 63, '3efad07595ec4d9155674d19837703c190e357481d498cb8c36eeb71616079e1', 'ret'),
    (7216752, 628, '3e1c6f356cc40fd4249226b71d1baec4ead66368f12c68acc05d6ffd7dd1a5a0', 'ret'),
    (7218000, 82, '78c97df4a57c218428169508a6aca125a0b1d62b8ddcdcc09247660b74a87b18', 'ret'),
    (11785024, 4, 'f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f', 'ret'),
    (6465968, 4, 'f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f', 'ret'),
]
RTTI = [(0x15DD984, 0x160CAB4, 0x1868F0C, b'.?AVGroundScene@@'),
        (0x15E0348, 0x16113A0, 0x186F484, b'.?AVPlayerCreatureController@@')]


def frozen_api(repo):
    # Reuse only private output/span guards; do not repeat the accepted producer audit.
    return runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))


def capped(values, limit=20):
    result = []
    for value in values:
        if len(result) >= limit:
            raise ValueError('Candidate xref budget exceeded')
        result.append(value)
    return result


def candidates(raw, pe):
    result = []
    base = pe.OPTIONAL_HEADER.ImageBase
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        data = section.get_data()
        start = base + section.VirtualAddress
        at = 0
        while True:
            at = data.find(b'\xe8', at)
            if at < 0:
                break
            if at + 5 <= len(data):
                va = start + at
                target = va + 5 + struct.unpack('<i', data[at + 1:at + 5])[0]
                if target in (0x6E1E70, 0x51FAD0):
                    result = capped([*result, dict(VA=va, Target=target, Kind='relative-call')])
            at += 1
    return result


def audit(exe, packages, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
    repo = repo or Path(__file__).resolve().parents[2]
    guard = frozen_api(repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    read = lambda va, size: guard['exact'](
        lambda at, n: pe.get_data(at - pe.OPTIONAL_HEADER.ImageBase, n), va, size)
    xrefs = candidates(raw, pe)
    if sorted((x['VA'], x['Target']) for x in xrefs) != [(0x519616, 0x51FAD0), (0x51FAF4, 0x6E1E70), (0xBAA610, 0x51FAD0)]:
        raise ValueError('Bounded candidate inventory mismatch')
    counts = []
    for va, size, digest, last in SPANS:
        body = read(va, size)
        if hashlib.sha256(body).hexdigest() != digest:
            raise ValueError('Selected span digest mismatch')
        instructions = list(md.disasm(body, va))
        if sum(i.size for i in instructions) != size or instructions[-1].mnemonic != last:
            raise ValueError('Incomplete selected body/prefix')
        counts.append(len(instructions))
    for va, mnemonic, operand in ANCHORS:
        ins = next(md.disasm(read(va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operand):
            raise ValueError(f'Instruction mismatch {va:08X}')
    for slot, col, td, name in RTTI:
        if struct.unpack('<I', read(slot, 4))[0] != col or struct.unpack('<I', read(col + 12, 4))[0] != td:
            raise ValueError('Selected RTTI pointer mismatch')
        if read(td + 8, len(name) + 1) != name + b'\0':
            raise ValueError('Selected RTTI name mismatch')
    if read(0xB3D340, 4) != read(0x62A9B0, 4):
        raise ValueError('Controller queue getter expression mismatch')
    return dict(ExecutableSha256=EXPECTED_EXE, InstructionAnchors=len(ANCHORS),
                CompleteBodyInstructionCounts=counts, CandidateXrefs=xrefs,
                CandidateBudget=20, GroundRttiConfirmed=True, PlayerRttiConfirmed=True,
                QueueInstanceExpression='scene[0x4C][0x0C] = scene[0x1C][0x2C][0x04]',
                MapAssignmentClosed=True, ControllerQueueInstanceIdentity='Confirmed at selected bind boundary',
                SelectedLifecycle='GroundScene constructor -> setup -> 0051FAD0',
                ControllerClassInstallation='PlayerCreatureController successful allocation branch',
                TransitiveInterveningMutation='Unknown', ReplacementDisconnectBeforeRelease=True,
                NullAcquisitionGuard=False, ControllerNullGuard=False,
                SharedMapGlobal='0191D214', ReferenceCount='0191D218',
                PhysicalBinding='Unknown', EffectiveProfile='Unknown', RuntimeOccurrence='Unknown',
                Frozen44AnchorAuditRepeated=False, OriginalExecuted=False, LiveContact=False)



def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    try:
        target = frozen_api(repo)['fresh_private_path'](args.receipt, repo)
        result = audit(args.exe, args.packages, repo)
        with target.open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(json.dumps(result, indent=2) + '\n')
    except (ValueError, OSError, ImportError, StopIteration) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['InstructionAnchors']} selected lifecycle anchors; queue instance expression closed")


if __name__ == '__main__':
    main()
