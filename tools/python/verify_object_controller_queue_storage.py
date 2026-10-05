"""Selected NetworkController/Controller queue writer and cleanup call boundary; initializer/cleanup internals remain unresolved."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (11841107, 'mov', 'eax, dword ptr [ebp + 8]'),
    (11841111, 'push', 'eax'),
    (11841112, 'mov', 'esi, ecx'),
    (11841114, 'call', '0xb3d0f0'),
    (11841119, 'mov', 'dword ptr [esi], 0x15ec8f0'),
    (11841125, 'mov', 'eax, esi'),
    (11841184, 'mov', 'dword ptr [ecx], 0x15ec8f0'),
    (11841190, 'jmp', '0xb3d190'),
    (11841140, 'mov', 'esi, ecx'),
    (11841142, 'call', '0xb4aea0'),
    (11841147, 'test', 'byte ptr [ebp + 8], 1'),
    (11841151, 'je', '0xb4ae8a'),
    (11841154, 'call', '0xac1640'),
    (7227971, 'call', '0xb4ae50'),
    (6465968, 'mov', 'eax, dword ptr [ecx + 4]'),
    (11784458, 'push', '0x14'),
    (11784460, 'mov', 'esi, ecx'),
    (11784462, 'call', '0xac15c0'),
    (11784473, 'test', 'eax, eax'),
    (11784482, 'je', '0xb3d12f'),
    (11784484, 'push', '0x10'),
    (11784486, 'mov', 'ecx, eax'),
    (11784488, 'call', '0xaa6050'),
    (11784493, 'jmp', '0xb3d131'),
    (11784495, 'xor', 'eax, eax'),
    (11784500, 'mov', 'dword ptr [esi + 4], eax'),
    (11784506, 'mov', 'dword ptr [esi + 8], eax'),
    (11784509, 'mov', 'dword ptr [esi], 0x15ec5b4'),
    (11784515, 'mov', 'eax, esi'),
    (11784593, 'mov', 'esi, ecx'),
    (11784596, 'mov', 'edi, dword ptr [esi + 4]'),
    (11784599, 'test', 'edi, edi'),
    (11784601, 'mov', 'dword ptr [esi], 0x15ec5b4'),
    (11784607, 'je', '0xb3d1b1'),
    (11784609, 'mov', 'ecx, edi'),
    (11784611, 'call', '0xaa6310'),
    (11784616, 'push', 'edi'),
    (11784617, 'call', '0xac1640'),
    (11784626, 'mov', 'dword ptr [esi + 8], 0'),
    (11784548, 'mov', 'esi, ecx'),
    (11784550, 'call', '0xb3d190'),
    (11784555, 'test', 'byte ptr [ebp + 8], 1'),
    (11784559, 'je', '0xb3d17a'),
    (11784562, 'call', '0xac1640'),
]
SPANS = [
    (11841104, 28, '9d6d772d48527848b2226a72f470023a18363daa574fdda9502adc415614d5fd', 'ret'),
    (11841184, 11, 'd04b89a92f0e594ac81ace05f87bfa83fc72a72454d9a44946927d2c250aee1d', 'jmp'),
    (11784432, 99, '31441fd0e2da7aa47586f8c001c581e9ae32ecc3fefa0f5faca3fcf5dbc3171a', 'ret'),
    (11784592, 43, '1634c945005d87a200c3210b918288e9e1ec16080c9a38c93b646f69b89baf18', 'ret'),
    (11841136, 33, '9d26e7d36f9b21f9c4801f45bf746ca507b85d70fd4543892ce328318110b6e2', 'ret'),
    (11784544, 33, 'e3e964bb8e4a6054579bf6652a1103fc331981c1ba42530f05f3f1c98e45adb7', 'ret'),
    (6465968, 4, 'f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f', 'ret'),
]
RTTI = [
    (22989036, 23221588, 25612428, b'.?AVNetworkController@@'),
    (22988208, 23220472, 25603704, b'.?AVController@@'),
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
    (7227971, 11841104, 'call'),
    (11841114, 11784432, 'call'),
    (22989040, 11841136, 'vtable'),
    (11841142, 11841184, 'call'),
    (11841154, 11277888, 'call'),
    (11841190, 11784592, 'jump'),
    (11784462, 11277760, 'call'),
    (11784488, 11165776, 'call'),
    (11784611, 11166480, 'call'),
    (11784617, 11277888, 'call'),
    (22988212, 11784544, 'vtable'),
    (11784550, 11784592, 'call'),
    (11784562, 11277888, 'call'),
]

def candidates(read):
    result = []
    for va, expected, kind in EDGES:
        data = read(va, 5 if kind in ('call', 'jump') else 4)
        if kind in ('call', 'jump'):
            if data[0] != (0xE8 if kind == 'call' else 0xE9):
                raise ValueError('Selected candidate is not the expected direct control transfer')
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
    return dict(ExecutableSha256=EXPECTED_EXE, InstructionAnchors=len(ANCHORS),
                PrimaryLifecycleBodies=4, PrimaryInstructionCounts=counts[:4],
                DeletingThunkInstructionCounts=counts[4:6], AccessorInstructionCounts=counts[6:],
                CandidateEdges=xrefs, CandidateBudget=20, ActualClasses=['NetworkController','Controller'],
                ConstructorBoundary='00B4AE50 -> 00B3D0F0 -> 00AA6050 (unexpanded)',
                DestructorBoundary='00B4AEA0 -> 00B3D190 -> 00AA6310 (unexpanded)',
                ReceiverAndObjectArgumentPreserved=True, BaseDestructorReceiverUnadjusted=True,
                ControllerQueueFieldOffset=4, ControllerQueueLoadWidth=4,
                AllocationRequestAndWriterClosed=True, DirectQueueWriterClosed=True,
                AllocationRequestBytes=20, QueueConstructorRawArgument=16,
                ConstructorReturnMatchesAllocation='Unknown', QueueConstructionInternalsClosed=False,
                QueueClassRtti='Unknown', QueueCountAndLayout='Unknown', QueueOwnership='Strongly inferred cleanup ownership of current +4 member',
                CurrentQueueUnchangedSinceConstruction='Unknown',
                QueueNullPolicy='allocation zero stores zero; destruction skips zero; deeper failures Unknown',
                QueueDestructionOrder='stored Q helper 00AA6310 before 00AC1640 on normal return',
                QueueMemberCleanupBoundaryClosed=True, QueueFieldClearedBySelectedDestructor=False,
                DeletingThunkBodyThenConditionalSelfRelease=True,
                SceneDisconnectVersusControllerDestructionOrder='Unknown',
                PhysicalBinding='Unknown', EffectiveProfile='Unknown', RuntimeOccurrence='Unknown',
                FrozenConstructorAndGroundMapAuditsRepeated=False, OriginalExecuted=False, LiveContact=False)



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
    print(f"PASS {result['InstructionAnchors']} anchors/constructor and destructor boundary; queue writer/null branch/cleanup call boundary closed; queue internals Unknown")


if __name__ == '__main__':
    main()
