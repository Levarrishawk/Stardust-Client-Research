"""One queue insertion path: third argument entry+8 provenance; concrete payload type Unknown."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import sys
import struct

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (11166876, 'mov', 'esi, dword ptr [ebp + 8]'),
    (11166879, 'test', 'esi, esi'),
    (11166881, 'mov', 'edx, ecx'),
    (11166883, 'mov', 'dword ptr [ebp - 0x10], edx'),
    (11166886, 'jne', '0xaa64c6'),
    (11166893, 'call', '0xaa2170'),
    (11166915, 'ret', '0x10'),
    (11166919, 'lea', 'ecx, [ebp - 0x20]'),
    (11166922, 'call', '0xaa6030'),
    (11166927, 'mov', 'ecx, dword ptr [ebp + 0xc]'),
    (11166930, 'mov', 'dword ptr [ebp - 0x1c], ecx'),
    (11166933, 'mov', 'ecx, dword ptr [ebp + 0x10]'),
    (11166936, 'mov', 'dword ptr [ebp - 0x18], ecx'),
    (11166939, 'mov', 'ecx, dword ptr [ebp + 0x14]'),
    (11166944, 'mov', 'esi, dword ptr [edx + 0xc]'),
    (11166947, 'mov', 'dword ptr [ebp - 0x20], eax'),
    (11166950, 'mov', 'dword ptr [ebp - 0x14], ecx'),
    (11166953, 'mov', 'edi, dword ptr [esi + 4]'),
    (11166956, 'cmp', 'edi, dword ptr [esi + 8]'),
    (11166969, 'je', '0xaa651c'),
    (11166971, 'test', 'edi, edi'),
    (11166973, 'je', '0xaa6513'),
    (11166975, 'mov', 'dword ptr [edi], eax'),
    (11166980, 'mov', 'dword ptr [edi + 4], eax'),
    (11166983, 'mov', 'ecx, dword ptr [ebp - 0x18]'),
    (11166986, 'mov', 'dword ptr [edi + 8], ecx'),
    (11166992, 'mov', 'dword ptr [edi + 0xc], eax'),
    (11166995, 'add', 'dword ptr [esi + 4], 0x10'),
    (11167011, 'sar', 'eax, 4'),
    (11167014, 'cmp', 'eax, 1'),
    (11167030, 'jb', '0xaa653b'),
    (11167035, 'mov', 'ebx, dword ptr [ecx]'),
    (11167037, 'add', 'ebx, eax'),
    (11167042, 'je', '0xaa656b'),
    (11167046, 'shl', 'eax, 4'),
    (11167049, 'cmp', 'eax, 0x80'),
    (11167055, 'jbe', '0xaa655e'),
    (11167057, 'call', '0xac15c0'),
    (11167070, 'call', '0x12ea770'),
    (11167097, 'je', '0xaa65ae'),
    (11167122, 'mov', 'ebx, dword ptr [edx + 8]'),
    (11167125, 'mov', 'dword ptr [edi + 8], ebx'),
    (11167150, 'test', 'eax, eax'),
    (11167167, 'mov', 'edx, dword ptr [ebp - 0x18]'),
    (11167170, 'mov', 'dword ptr [ecx + 8], edx'),
    (11167200, 'call', '0xaa6040'),
    (11167222, 'sar', 'ecx, 4'),
    (11167225, 'push', 'ecx'),
    (11167226, 'push', 'eax'),
    (11167227, 'mov', 'ecx, edi'),
    (11167229, 'call', '0xaa6810'),
    (11167234, 'mov', 'eax, dword ptr [ebp + 8]'),
    (11167237, 'mov', 'edx, dword ptr [ebp + 0xc]'),
    (11167240, 'shl', 'ebx, 4'),
    (11167245, 'mov', 'dword ptr [esi + 4], edx'),
    (11167248, 'mov', 'edx, dword ptr [ebp - 0x10]'),
    (11167251, 'mov', 'dword ptr [edi], ebx'),
    (11167253, 'mov', 'dword ptr [esi], eax'),
    (11167256, 'mov', 'edx, dword ptr [edx + 0x10]'),
    (11167259, 'test', 'edx, edx'),
    (11167262, 'je', '0xaa6627'),
    (11167264, 'mov', 'eax, dword ptr [edx]'),
    (11167266, 'mov', 'ecx, edx'),
    (11167268, 'call', 'dword ptr [eax + 4]'),
    (11167274, 'call', '0xaa6040'),
    (11167293, 'ret', '0x10'),
    (11165744, 'mov', 'eax, ecx'),
    (11165746, 'xor', 'ecx, ecx'),
    (11165748, 'mov', 'dword ptr [eax], ecx'),
    (11165750, 'mov', 'dword ptr [eax + 4], ecx'),
    (11165753, 'mov', 'dword ptr [eax + 8], ecx'),
    (11165756, 'mov', 'dword ptr [eax + 0xc], ecx'),
    (11165759, 'ret', ''),
]
SPANS = [
    (11166848, 448, 'f434feee4249caed508fa79d966b41a9fc235a4900684a54bc5bc45ce40eef7d', 'ret'),
    (11165744, 16, '585eb39d0eb0963eb6c38bed4169fd672f7a11cb3c682b0f69e817c5bda8416b', 'ret'),
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


EXPECTED_CALLS = [(0xAA64AD,'0xaa2170'),(0xAA64CA,'0xaa6030'),
                  (0xAA6551,'0xac15c0'),(0xAA655E,'0x12ea770'),
                  (0xAA65E0,'0xaa6040'),(0xAA65FD,'0xaa6810'),
                  (0xAA6624,'dword ptr [eax + 4]'),(0xAA662A,'0xaa6040')]


def candidates(instructions):
    calls=[(i.address,i.op_str) for i in instructions if i.mnemonic=='call']
    if calls!=EXPECTED_CALLS:
        raise ValueError('Selected insertion call inventory mismatch')
    # Conservative union counts the discarded overlapping owner and reused clear helper.
    return capped([dict(Kind='primary producer',Target=0xAA6480),
                   dict(Kind='tiny constructor',Target=0xAA6030),
                   dict(Kind='discarded inventory overlap; unexpanded',Target=0x7934F0),
                   dict(Kind='diagnostic delegate; unexpanded',Target=0xAA2170),
                   dict(Kind='allocator; unexpanded',Target=0xAC15C0),
                   dict(Kind='allocator; unexpanded',Target=0x12EA770),
                   dict(Kind='entry clear; accepted59 reused',Target=0xAA6040),
                   dict(Kind='old storage delegate; unexpanded',Target=0xAA6810),
                   dict(Kind='queue+10 virtual slot+4; unexpanded',Target='Unknown'),
                   dict(Kind='registered handler; unexpanded',Target=0x14BA428)])


EXPECTED_INITIALIZER_XREFS = [0xAA64CA]


def initializer_xrefs(pe):
    sites=[]
    base=pe.OPTIONAL_HEADER.ImageBase
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        data=section.get_data()
        at=0
        while True:
            at=data.find(b'\xe8',at)
            if at<0:
                break
            if at+5<=len(data):
                va=base+section.VirtualAddress+at
                if va+5+struct.unpack_from('<i',data,at+1)[0]==0xAA6030:
                    if len(sites)>=12:
                        raise ValueError('Exact-target raw call-site budget exceeded')
                    sites.append(va)
            at+=1
    if sites!=EXPECTED_INITIALIZER_XREFS:
        raise ValueError('Exact-target initializer xref inventory mismatch')
    return sites


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
    xrefs=initializer_xrefs(pe)
    if not all(any(i.address==va and i.mnemonic=='call' and i.op_str=='0xaa6030'
                   for i in decoded) for va in xrefs):
        raise ValueError('Initializer candidate lacks selected-body call boundary')
    return dict(ExecutableSha256=EXPECTED_EXE,InstructionAnchors=len(ANCHORS),
                PrimaryLifecycleBodies=1,TinyDirectWrappers=1,PrimaryInstructionCounts=counts,
                ExitVAs=[0xAA64C3,0xAA663D,0xAA603F],CandidateBudget=12,
                DistinctTotalLeadCandidates=len(leads),ConservativeLeadUnion=leads,
                DistinctDirectCalleeTargets=6,IntraBodyDirectCallSites=7,
                IntraBodyIndirectCallSites=1,UnexpandedCalleeTargets=5,
                ExactTargetXrefSearches=1,ExactTargetXrefCallSites=1,
                XrefRoot=0xAA6030,XrefCallVA=0xAA64CA,
                XrefOwner='00AA6480 boundary verified by complete decode',
                DiscardedOverlappingInventoryOwner='007934F0 not decoded',
                QueueReceiverSaved='incoming ECX -> EDX -> [ebp-10]',
                SelectedRecordOffset=12,EntryStride=16,
                EntryFields=[dict(Offset=0,Source='stack argument1'),
                             dict(Offset=4,Source='stack argument2'),
                             dict(Offset=8,Source='stack argument3'),
                             dict(Offset=12,Source='stack argument4')],
                NullFirstArgumentEarlyDelegate=True,NullPayloadArgumentRejected=False,
                SparePathPublishesEnd='old end +16',
                GrowthDemand='used + max_unsigned(used,1), 32-bit arithmetic',
                AllocationByteArithmetic='demand <<4 modulo32',AllocationThresholdBytes=128,
                GrowthCopiesAllFourEntryDwords=True,OldEntryPlus8ClearedBeforeStorageDelegate=True,
                NormalGrowthPublicationOrder=[4,8,0],
                OptionalQueueCallbackOffset=16,CallbackVirtualSlotOffset=4,
                CallbackReceiverSource='queue+10; independent of third-argument load',
                CallbackPayloadPointerAlias='Unknown',
                CallbackRunsAfterEntryOrBoundsPublication=True,
                ConcretePayloadRtti='Unknown; third argument never dereferenced/constructed',
                PayloadVtableEstablished=False,UniqueOwnership='Unknown',
                RelocationOwnership='transfer-shaped; unique ownership not proved',
                ReturnIdentity='No stable receiver return; branch/delegate-dependent EAX',
                ExceptionHandlerPolicy='Unknown; registered handler unexpanded',
                Cancellation='No explicit cancellation argument/check; delegates Unknown',
                OriginalExecuted=False,LiveContact=False)


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
    print(f"PASS {result['InstructionAnchors']} anchors/one producer plus tiny constructor; entry+8 argument3 closed; payload RTTI Unknown")


if __name__ == '__main__':
    main()
