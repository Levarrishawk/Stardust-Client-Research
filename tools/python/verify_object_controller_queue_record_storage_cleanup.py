"""Bounded static record backing-storage cleanup evidence; no original execution."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (11167509, 'push', '0x14ba44b'),
    (11167529, 'mov', 'edx, ecx'),
    (11167531, 'mov', 'eax, dword ptr [edx + 4]'),
    (11167534, 'mov', 'ecx, dword ptr [edx]'),
    (11167536, 'cmp', 'ecx, eax'),
    (11167538, 'mov', 'dword ptr [ebp - 0x10], edx'),
    (11167541, 'je', '0xaa6743'),
    (11167543, 'call', '0xaa6040'),
    (11167548, 'add', 'ecx, 0x10'),
    (11167551, 'cmp', 'ecx, eax'),
    (11167553, 'jne', '0xaa6737'),
    (11167555, 'mov', 'ecx, dword ptr [edx]'),
    (11167557, 'test', 'ecx, ecx'),
    (11167559, 'mov', 'dword ptr [ebp - 4], 0'),
    (11167566, 'je', '0xaa6783'),
    (11167568, 'mov', 'eax, dword ptr [edx + 8]'),
    (11167571, 'sub', 'eax, ecx'),
    (11167573, 'sar', 'eax, 4'),
    (11167576, 'shl', 'eax, 4'),
    (11167579, 'cmp', 'eax, 0x80'),
    (11167584, 'jbe', '0xaa6779'),
    (11167586, 'push', 'ecx'),
    (11167587, 'call', '0xac1640'),
    (11167592, 'add', 'esp, 4'),
    (11167595, 'mov', 'ecx, dword ptr [ebp - 0xc]'),
    (11167598, 'mov', 'dword ptr fs:[0], ecx'),
    (11167608, 'ret', ''),
    (11167609, 'push', 'eax'),
    (11167610, 'push', 'ecx'),
    (11167611, 'call', '0x12ea920'),
    (11167616, 'add', 'esp, 8'),
    (11167619, 'mov', 'ecx, dword ptr [ebp - 0xc]'),
    (11167622, 'mov', 'dword ptr fs:[0], ecx'),
    (11167632, 'ret', ''),
    (11165760, 'mov', 'dword ptr [ecx + 8], 0'),
    (11165767, 'ret', ''),
]
SPANS = [
    (11167504, 129, '203197aa56cd00dc435839a3bf228ee3bd4e1481517b89d13d8c9972ff3c2909', 'ret'),
    (11165760, 8, '35647cd18c19f4482ebc2ce05d5e7caabe8ffc5b5c2147adc62a917b4a9fc6b1', 'ret'),
]
def frozen_api(repo):
    # Reuse only private output/span guards; do not repeat the accepted producer audit.
    return runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))


def capped(values, limit=12):
    result = []
    for value in values:
        if len(result) >= limit:
            raise ValueError('Candidate xref budget exceeded')
        result.append(value)
    return result


def candidates(instructions):
    calls = [(i.address, i.op_str) for i in instructions if i.mnemonic == 'call']
    if calls != [(0xAA6737, '0xaa6040'), (0xAA6763, '0xac1640'), (0xAA677B, '0x12ea920')]:
        raise ValueError('Selected-body call inventory mismatch')
    return capped([dict(Target=0xAC1640, CallSites=[0xAA6763]),
                   dict(Target=0x12EA920, CallSites=[0xAA677B])])


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
    writes = [(i.address, i.op_str) for i in decoded
              if i.mnemonic == 'mov' and i.op_str.startswith('dword ptr [ecx + 8],')]
    if writes != [(0xAA6040, 'dword ptr [ecx + 8], 0')]:
        raise ValueError('Tiny entry clear mismatch')
    return dict(ExecutableSha256=EXPECTED_EXE, InstructionAnchors=len(ANCHORS),
                PrimaryLifecycleBodies=1, TinyDirectWrappers=1,
                PrimaryInstructionCounts=counts, ExitVAs=[0xAA6778,0xAA6790,0xAA6047],
                FreshDirectCalleeTargets=3, DistinctCalleeCandidates=len(leads),
                UnexpandedCalleeLeads=leads, IntraBodyDirectCallSites=3,
                IntraBodyIndirectCallSites=0, CandidateBudget=12, ExternalXrefSearches=0,
                RecordReadOffsets=[0,4,8], DirectRecordWriteOffsets=[],
                EntryWriteOffset=8, EntryWriteValue=0, EntryStride=16,
                EntryWrapperCalls=0, EntryWrapperPreservesLoopRegisters=True,
                EmptyRangeSkipsEntryWrites=True, NullBackingPointerSkipsRelease=True,
                NullRecordGuard=False, NormalReturnDescriptorReset=False,
                ByteCount='32-bit ((limit-start) arithmetic-right-shift4) left-shift4',
                ReleaseThreshold=128, ThresholdComparison='unsigned jbe',
                LargeReleaseTarget=0xAC1640, SmallReleaseTarget=0x12EA920,
                SmallReleaseArguments='backing pointer, computed byte count',
                ConcreteClassRtti='Unknown', UniqueOwnership='Unknown',
                ReturnIdentity='No stable receiver return; EAX varies with branch/delegate',
                ExceptionRegistrationObserved=True, UnexpandedHandler=0x14BA44B,
                ExceptionCleanupPolicy='Unknown; handler and release delegates unexpanded',
                PayloadDisposal='Not performed by inspected entry wrapper',
                CallerDescriptorRelease='Separate subsequent 00AC1640(record) on normal return',
                OriginalExecuted=False, LiveContact=False)


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
    print(f"PASS {result['InstructionAnchors']} anchors/one primary plus tiny wrapper; storage release routes closed; release internals Unknown")


if __name__ == '__main__':
    main()
