"""Two bounded controller constructor carriers and deleting-destructor boundaries; queue lifecycle remains unknown."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (6522329, 'mov', 'eax, dword ptr [ebp + 8]'),
    (6522334, 'mov', 'esi, ecx'),
    (6522336, 'push', 'eax'),
    (6522340, 'call', '0x6e4a20'),
    (6522369, 'cmp', 'eax, ebx'),
    (6522375, 'je', '0x638612'),
    (6522393, 'mov', 'dword ptr [esi + 0x18], eax'),
    (6522548, 'mov', 'dword ptr [esi], 0x15e04ac'),
    (6522554, 'mov', 'eax, esi'),
    (7227961, 'mov', 'eax, dword ptr [ebp + 8]'),
    (7227965, 'mov', 'esi, ecx'),
    (7227967, 'push', 'eax'),
    (7227971, 'call', '0xb4ae50'),
    (7227980, 'mov', 'ecx, esi'),
    (7227985, 'mov', 'byte ptr [esi + 0xc], al'),
    (7227988, 'mov', 'dword ptr [esi + 0x10], eax'),
    (7227991, 'mov', 'dword ptr [esi], 0x15e1ab8'),
    (7227997, 'call', '0xb4b070'),
    (7228005, 'mov', 'eax, esi'),
    (6460884, 'mov', 'esi, ecx'),
    (6460886, 'call', '0x629600'),
    (6460891, 'test', 'byte ptr [ebp + 8], 1'),
    (6460895, 'je', '0x6295ea'),
    (6460898, 'call', '0xac1640'),
    (6522580, 'mov', 'esi, ecx'),
    (6522582, 'call', '0x638700'),
    (6522587, 'test', 'byte ptr [ebp + 8], 1'),
    (6522591, 'je', '0x6386ea'),
    (6522594, 'call', '0xac1640'),
    (7228036, 'mov', 'esi, ecx'),
    (7228038, 'call', '0x6e4ab0'),
    (7228043, 'test', 'byte ptr [ebp + 8], 1'),
    (7228047, 'je', '0x6e4a9a'),
    (7228050, 'call', '0xac1640'),
    (6460453, 'call', '0x6385c0'),
    (6460856, 'mov', 'dword ptr [ebx], 0x15e034c'),
    (11785024, 'mov', 'eax, dword ptr [ecx + 4]'),
    (6465968, 'mov', 'eax, dword ptr [ecx + 4]'),
]
SPANS = [
    (6522304, 267, 'a4b53f3370a13de4f2ff7ca43dc0548b8e013a83b202d74d824871be4ff6a354', 'ret'),
    (7227936, 85, '11a0d30ff1bc7ba39e81b02a8425cb9759b36066dcfa318db26aea27013207a4', 'ret'),
    (6460880, 33, '4ed06a54a54a23be97302134bf105af4add71a2d388883e05e252790fc427972', 'ret'),
    (6522576, 33, '21fc2f7505047d7a781c57c602a0a4c4a42a398c536cfaab9a67b1429735524c', 'ret'),
    (7228032, 33, '8a6e6c079a4ac487f218ca866e21637c0780321ea79801d33663dd6c29d4c0e0', 'ret'),
    (11785024, 4, 'f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f', 'ret'),
    (6465968, 4, 'f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f', 'ret'),
]
RTTI = [
    (22938440, 23139232, 25621636, b'.?AVPlayerCreatureController@@'),
    (22938792, 23140020, 25603732, b'.?AVCreatureController@@'),
    (22944436, 23148636, 25612396, b'.?AVClientController@@'),
]

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


EDGES = [
    (6460453, 6522304, 'call'),
    (6522340, 7227936, 'call'),
    (7227971, 11841104, 'call'),
    (7227997, 11841648, 'call'),
    (22938444, 6460880, 'vtable'),
    (6460886, 6460928, 'call'),
    (6460898, 11277888, 'call'),
    (22938796, 6522576, 'vtable'),
    (6522582, 6522624, 'call'),
    (6522594, 11277888, 'call'),
    (22944440, 7228032, 'vtable'),
    (7228038, 7228080, 'call'),
    (7228050, 11277888, 'call'),
]

def candidates(read):
    result = []
    for va, expected, kind in EDGES:
        data = read(va, 5 if kind == 'call' else 4)
        if kind == 'call':
            if data[0] != 0xE8:
                raise ValueError('Selected candidate is not a direct call')
            target = va + 5 + struct.unpack('<i', data[1:])[0]
        else:
            target = struct.unpack('<I', data)[0]
        if target != expected:
            raise ValueError('Selected lifecycle edge mismatch')
        result = capped([*result, dict(VA=va, Target=target, Kind=kind)])
    if len(result) != 13:
        raise ValueError('Selected candidate count mismatch')
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
    xrefs = candidates(read)
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
                PrimaryLifecycleBodies=2, PrimaryInstructionCounts=counts[:2],
                DeletingThunkInstructionCounts=counts[2:5], AccessorInstructionCounts=counts[5:],
                CandidateEdges=xrefs, CandidateBudget=20, ConstructorCarrierConfirmed=True,
                ConstructorChain='00629400 -> 006385C0 -> 006E4A20 -> 00B4AE50 (unexpanded)',
                ReceiverAndObjectArgumentPreserved=True, ControllerQueueAllocationClosed=False,
                QueueClassRtti='Unknown', QueueCountAndLayout='Unknown', QueueOwnership='Unknown',
                QueueNonNullPreconditions='Unknown', QueueDestructionOrder='Unknown',
                DestructorBoundaries=['006295D0 -> 00629600','006386D0 -> 00638700','006E4A80 -> 006E4AB0'],
                SceneDisconnectVersusControllerDestructionOrder='Unknown',
                PhysicalBinding='Unknown', EffectiveProfile='Unknown', RuntimeOccurrence='Unknown',
                FrozenGroundMapAuditRepeated=False, OriginalExecuted=False, LiveContact=False)




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
    print(f"PASS {result['InstructionAnchors']} anchors/two constructor carriers; queue lifecycle remains Unknown")


if __name__ == '__main__':
    main()
