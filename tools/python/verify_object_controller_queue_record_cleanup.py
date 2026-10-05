"""Bounded static evidence for record zero initialization and entry payload cleanup."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (11167683, 'mov', 'eax, ecx'),
    (11167685, 'mov', 'ecx, dword ptr [ebp + 0xc]'),
    (11167688, 'mov', 'dword ptr [eax], ecx'),
    (11167691, 'ret', '8'),
    (11166579, 'mov', 'eax, dword ptr [ebp + 8]'),
    (11166583, 'mov', 'esi, dword ptr [eax]'),
    (11166586, 'mov', 'edi, dword ptr [eax + 4]'),
    (11166589, 'cmp', 'esi, edi'),
    (11166591, 'je', '0xaa639c'),
    (11166593, 'mov', 'ecx, dword ptr [esi + 8]'),
    (11166596, 'test', 'ecx, ecx'),
    (11166598, 'je', '0xaa6395'),
    (11166600, 'mov', 'eax, dword ptr [ecx]'),
    (11166602, 'push', '1'),
    (11166604, 'call', 'dword ptr [eax]'),
    (11166606, 'mov', 'dword ptr [esi + 8], 0'),
    (11166613, 'add', 'esi, 0x10'),
    (11166616, 'cmp', 'esi, edi'),
    (11166618, 'jne', '0xaa6381'),
    (11166623, 'ret', '8'),
]
SPANS = [
    (11167680, 14, '8b18d6a4d23f077e4ae2893f88df892f999a47d113c918c38cad13cbb32e03a6', 'ret'),
    (11166576, 50, '19e098e27ead67e78b06ab077d9d25b1c9489f68f229414657c55cea6df198c5', 'ret'),
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
    calls = [i for i in instructions if i.mnemonic == 'call']
    if [(i.address, i.op_str) for i in calls] != [(0xAA638C, 'dword ptr [eax]')]:
        raise ValueError('Selected-body call inventory mismatch')
    return capped([dict(CallSites=[0xAA638C], Kind='indirect virtual slot zero', Target='Unknown')])


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
                IntraBodyDirectCallSites=0, IntraBodyIndirectCallSites=1,
                CandidateBudget=20, ExternalXrefSearches=0,
                InitializerReturnsIncomingReceiver=True, InitializerUsesSecondArgument=True,
                InitializerIgnoresFirstArgument=True, RecordAPlus8InitiallyZero=True,
                CallerSecondArgumentIgnored=True, IncomingQueueReceiverUnused=True,
                EntryStride=16, PayloadPointerOffset=8, InternalVirtualArgument=1,
                VirtualSlotOffset=0, PointerClearedOnlyAfterNormalVirtualReturn=True,
                DescriptorFieldsWritten=False, RecordEndReset=False,
                ConcretePayloadClassRtti='Unknown', ActualVirtualTarget='Unknown',
                DisposalMeaning='Strongly inferred; actual destructor/free policy Unknown',
                CancellationMeaning='Unknown; caller flag unused',
                OwnershipAndAliasingInvariant='Unknown', CallbackMutationPolicy='Unknown',
                ExceptionPolicy='No local handler; store follows call only on normal return',
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
    print(f"PASS {result['InstructionAnchors']} anchors/two bodies; caller flag ignored; virtual target Unknown")


if __name__ == '__main__':
    main()
