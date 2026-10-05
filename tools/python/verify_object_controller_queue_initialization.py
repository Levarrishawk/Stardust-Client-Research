"""Exact queue receiver identity, five DWORD fields and reserve/cleanup arithmetic; delegated element semantics remain unresolved."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (11165806, 'mov', 'edi, ecx'),
    (11165810, 'push', '0xc'),
    (11165812, 'mov', 'dword ptr [ebp - 0x18], edi'),
    (11165818, 'call', '0xac15c0'),
    (11165831, 'cmp', 'esi, ebx'),
    (11165836, 'je', '0xaa60ad'),
    (11165849, 'push', 'ebx'),
    (11165853, 'push', 'eax'),
    (11165854, 'lea', 'ecx, [esi + 8]'),
    (11165857, 'mov', 'dword ptr [esi], ebx'),
    (11165859, 'mov', 'dword ptr [esi + 4], ebx'),
    (11165862, 'call', '0xaa67c0'),
    (11165869, 'xor', 'esi, esi'),
    (11165880, 'mov', 'dword ptr [edi], esi'),
    (11165882, 'call', '0xac15c0'),
    (11165892, 'je', '0xaa60d0'),
    (11165894, 'mov', 'dword ptr [eax], ebx'),
    (11165896, 'mov', 'dword ptr [eax + 4], ebx'),
    (11165899, 'mov', 'dword ptr [eax + 8], ebx'),
    (11165908, 'mov', 'dword ptr [edi + 4], eax'),
    (11165911, 'mov', 'dword ptr [edi + 0xc], eax'),
    (11165914, 'cmp', 'dword ptr [ebp + 8], ebx'),
    (11165917, 'mov', 'dword ptr [edi + 8], esi'),
    (11165920, 'mov', 'dword ptr [edi + 0x10], ebx'),
    (11165929, 'jl', '0xaa60ee'),
    (11165934, 'mov', 'ebx, dword ptr [eax]'),
    (11165948, 'sar', 'ecx, 4'),
    (11165951, 'cmp', 'ecx, ebx'),
    (11165956, 'jae', '0xaa61e6'),
    (11165990, 'shl', 'eax, 4'),
    (11165993, 'cmp', 'eax, 0x80'),
    (11166001, 'call', '0xac15c0'),
    (11166016, 'call', '0x12ea770'),
    (11166109, 'test', 'ebx, ebx'),
    (11166115, 'shl', 'eax, 4'),
    (11166118, 'cmp', 'eax, 0x80'),
    (11166126, 'call', '0xac15c0'),
    (11166136, 'call', '0x12ea770'),
    (11166169, 'mov', 'dword ptr [esi], eax'),
    (11166174, 'mov', 'dword ptr [esi + 4], edi'),
    (11166180, 'mov', 'dword ptr [eax], ebx'),
    (11166182, 'mov', 'eax, dword ptr [ebp + 8]'),
    (11166197, 'jl', '0xaa61fa'),
    (11166202, 'mov', 'edi, dword ptr [edi + 4]'),
    (11166219, 'sar', 'ecx, 4'),
    (11166222, 'cmp', 'ecx, ebx'),
    (11166227, 'jae', '0xaa62f2'),
    (11166386, 'shl', 'eax, 4'),
    (11166389, 'cmp', 'eax, 0x80'),
    (11166397, 'call', '0xac15c0'),
    (11166407, 'call', '0x12ea770'),
    (11166440, 'mov', 'dword ptr [edi], eax'),
    (11166445, 'mov', 'dword ptr [edi + 4], esi'),
    (11166448, 'mov', 'dword ptr [eax], ebx'),
    (11166453, 'mov', 'eax, dword ptr [ebp - 0x18]'),
    (11166482, 'mov', 'esi, ecx'),
    (11166484, 'mov', 'eax, dword ptr [esi]'),
    (11166486, 'push', '1'),
    (11166488, 'push', 'eax'),
    (11166489, 'call', '0xaa6370'),
    (11166494, 'mov', 'ecx, dword ptr [esi + 4]'),
    (11166497, 'push', '1'),
    (11166499, 'push', 'ecx'),
    (11166500, 'mov', 'ecx, esi'),
    (11166502, 'call', '0xaa6370'),
    (11166507, 'mov', 'edi, dword ptr [esi]'),
    (11166511, 'je', '0xaa6341'),
    (11166513, 'mov', 'ecx, edi'),
    (11166515, 'call', '0xaa6710'),
    (11166520, 'push', 'edi'),
    (11166521, 'call', '0xac1640'),
    (11166529, 'mov', 'edi, dword ptr [esi + 4]'),
    (11166534, 'je', '0xaa6358'),
    (11166536, 'mov', 'ecx, edi'),
    (11166538, 'call', '0xaa6710'),
    (11166543, 'push', 'edi'),
    (11166544, 'call', '0xac1640'),
    (11166553, 'mov', 'dword ptr [esi + 8], 0'),
    (11166560, 'mov', 'dword ptr [esi + 0xc], 0'),
    (11166567, 'mov', 'dword ptr [esi + 0x10], 0'),
]
SPANS = [
    (11165776, 696, '3d9aecdf55a2b69d44c5becb7ba669254c5d250de66bf9c4c6a04616863f1c5e', 'ret'),
    (11166480, 96, 'e8d84f8aa8b8c1dac2109867b3aec627a21c91169f60645bacca1aeeb6e0787b', 'ret'),
]
EXPECTED_CALL_TARGETS = [
    11166576,
    11167504,
    11167680,
    11167696,
    11167760,
    11167824,
    11167888,
    11277760,
    11277888,
    19834736,
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


def candidates(instructions):
    groups = {}
    for ins in instructions:
        if ins.mnemonic == 'call':
            target = int(ins.op_str, 16)
            groups.setdefault(target, []).append(ins.address)
    if sorted(groups) != EXPECTED_CALL_TARGETS or sum(map(len, groups.values())) != 23:
        raise ValueError('Selected-body direct-call inventory mismatch')
    return capped([dict(Target=target, CallSites=groups[target]) for target in sorted(groups)])


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
    counts = []
    decoded = []
    for va, size, digest, last in SPANS:
        body = read(va, size)
        if hashlib.sha256(body).hexdigest() != digest:
            raise ValueError('Selected span digest mismatch')
        instructions = list(md.disasm(body, va))
        if sum(i.size for i in instructions) != size or instructions[-1].mnemonic != last:
            raise ValueError('Incomplete selected body/prefix')
        counts.append(len(instructions))
        decoded.extend(instructions)
    for va, mnemonic, operand in ANCHORS:
        ins = next(md.disasm(read(va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operand):
            raise ValueError(f'Instruction mismatch {va:08X}')
    leads = candidates(decoded)
    return dict(ExecutableSha256=EXPECTED_EXE, InstructionAnchors=len(ANCHORS),
                PrimaryLifecycleBodies=2, PrimaryInstructionCounts=counts,
                UnexpandedCalleeLeads=leads, DistinctCalleeCandidates=len(leads),
                IntraBodyDirectCallSites=23, CandidateBudget=20, ExternalXrefSearches=0,
                CandidateBudgetDefinition='distinct unexpanded callee leads; call sites counted separately',
                ReturnedReceiverIdentity='Confirmed normal-return saved incoming receiver',
                ObjectFields=[dict(Offset=0, Width=4, Observation='record A allocation result'),
                              dict(Offset=4, Width=4, Observation='record B allocation result'),
                              dict(Offset=8, Width=4, Observation='initial alias of record A'),
                              dict(Offset=12, Width=4, Observation='initial alias of record B'),
                              dict(Offset=16, Width=4, Observation='zero at initialization and cleanup')],
                InternalRecordAllocationRequests=[12,12],
                ReserveDemand='max(signed argument,0) for each record; spans shifted right4',
                EntryStride=16, ByteRequestArithmetic='32-bit demand left shift4',
                AllocatorThresholdBytes=128, Raw16Meaning='reserve/growth demand, not hard maximum',
                ReserveInterpretationConfidence='Strongly inferred; arithmetic confirmed',
                RecordBInitiallyThreeZeroDwords=True, RecordACapacityInitializer='00AA67C0 unexpanded',
                ConcreteQueueClassRtti='Unknown; none established by these bodies',
                RingOrHardCapacitySemantics='Unknown', EntryFieldsAndPayloadOwnership='Unknown',
                RecordCleanupCalls='00AA6370(queue receiver, current record pointer, raw1), A then B',
                RecordReleaseOrder='current A then current B; 00AA6710 then 00AC1640 if non-null/normal return',
                DirectCleanupClears=[8,12,16], DirectCleanupDoesNotClear=[0,4],
                RecordPointerUnchangedAcrossHelpers='Unknown', AllocationFailurePolicy='Unknown beyond observed null branches',
                CancellationMeaning='Unknown; raw cleanup flag1 does not establish cancellation',
                SceneDetachmentAndCurrentPointerLifetime='Unknown', PhysicalProfile='Unknown',
                FrozenControllerAndSceneAuditsRepeated=False, OriginalExecuted=False, LiveContact=False)



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
    print(f"PASS {result['InstructionAnchors']} anchors/two initializer-cleanup bodies; receiver/fields/arithmetic closed; delegated entry semantics Unknown")


if __name__ == '__main__':
    main()
