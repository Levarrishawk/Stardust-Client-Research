"""Bounded object-controller dispatch: message+38 payload source, argument order and fallback."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (4599649, 'mov', 'ebx, esp'),
    (4599697, 'mov', 'edi, dword ptr [ebx + 8]'),
    (4599704, 'mov', 'ecx, edi'),
    (4599706, 'call', '0x463080'),
    (4599719, 'call', '0xb380e0'),
    (4599730, 'mov', 'esi, eax'),
    (4599739, 'call', '0xaa4940'),
    (4599744, 'test', 'esi, esi'),
    (4599746, 'je', '0x463056'),
    (4599756, 'call', 'dword ptr [edx + 0x44]'),
    (4599767, 'call', 'dword ptr [eax + 0x44]'),
    (4599770, 'mov', 'ecx, dword ptr [eax + 0x2c]'),
    (4599775, 'jne', '0x462ff9'),
    (4599781, 'call', 'dword ptr [edx + 0x44]'),
    (4599788, 'call', 'dword ptr [edx + 0x80]'),
    (4599798, 'call', 'dword ptr [eax + 0x3c]'),
    (4599801, 'mov', 'esi, dword ptr [esi + 0x2c]'),
    (4599819, 'call', '0x131e6f1'),
    (4599824, 'mov', 'esi, eax'),
    (4599829, 'test', 'esi, esi'),
    (4599831, 'je', '0x463056'),
    (4599848, 'call', '0x131e6f1'),
    (4599856, 'test', 'eax, eax'),
    (4599858, 'je', '0x463056'),
    (4599860, 'mov', 'ecx, dword ptr [edi + 0x40]'),
    (4599863, 'fld', 'dword ptr [edi + 0x64]'),
    (4599866, 'mov', 'edx, dword ptr [edi + 0x38]'),
    (4599869, 'mov', 'eax, dword ptr [edi + 0x48]'),
    (4599872, 'and', 'ecx, 0xfffffffe'),
    (4599875, 'or', 'ecx, 4'),
    (4599878, 'push', 'ecx'),
    (4599879, 'push', 'edx'),
    (4599880, 'push', 'ecx'),
    (4599881, 'fstp', 'dword ptr [esp]'),
    (4599884, 'push', 'eax'),
    (4599885, 'mov', 'ecx, esi'),
    (4599887, 'call', '0xb3d250'),
    (4599892, 'jmp', '0x463063'),
    (4599894, 'mov', 'ecx, dword ptr [edi + 0x38]'),
    (4599897, 'test', 'ecx, ecx'),
    (4599899, 'je', '0x463063'),
    (4599901, 'mov', 'edx, dword ptr [ecx]'),
    (4599903, 'push', '1'),
    (4599905, 'call', 'dword ptr [edx]'),
    (4599925, 'ret', '4'),
]
SPANS = [(4599648, 280, '2f672b9e0a39fd81e6df0ce425aebdc17db1cc0874fc1043a9b0d2b6a245b949', 'ret')]

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


EXPECTED_CALLS = [
    (4599706, '0x463080'),
    (4599719, '0xb380e0'),
    (4599739, '0xaa4940'),
    (4599756, 'dword ptr [edx + 0x44]'),
    (4599767, 'dword ptr [eax + 0x44]'),
    (4599781, 'dword ptr [edx + 0x44]'),
    (4599788, 'dword ptr [edx + 0x80]'),
    (4599798, 'dword ptr [eax + 0x3c]'),
    (4599819, '0x131e6f1'),
    (4599848, '0x131e6f1'),
    (4599887, '0xb3d250'),
    (4599905, 'dword ptr [edx]'),
]

def call_sites(instructions):
    sites=capped([(i.address,i.op_str) for i in instructions if i.mnemonic=='call'])
    if sites!=EXPECTED_CALLS:
        raise ValueError('Selected dispatch call-site inventory mismatch')
    return sites


def candidates(instructions):
    call_sites(instructions)
    return capped([dict(Kind='selected primary',Target=0x462F60),
                   dict(Kind='message accessor; unexpanded',Target=0x463080),
                   dict(Kind='lookup; unexpanded',Target=0xB380E0),
                   dict(Kind='temporary helper; unexpanded',Target=0xAA4940),
                   dict(Kind='cast-shaped library; unexpanded',Target=0x131E6F1),
                   dict(Kind='accepted61 caller; reused',Target=0xB3D250),
                   dict(Kind='root object first-word slot44; unexpanded',Target='Unknown root-slot44'),
                   dict(Kind='slot44 result first-word slot80; unexpanded',Target='Unknown result-slot80'),
                   dict(Kind='root object first-word slot3C; unexpanded',Target='Unknown root-slot3C'),
                   dict(Kind='message+38 first-word slot0; unexpanded',Target='Unknown payload-slot0'),
                   dict(Kind='registered handler; unexpanded',Target=0x141A7D8),
                   dict(Kind='unresolved cast target descriptor; unexpanded',Target=0x186D06C)])


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
    sites=call_sites(decoded)
    return dict(ExecutableSha256=EXPECTED_EXE,InstructionAnchors=len(ANCHORS),
                PrimaryBodies=1,TinyDirectWrappers=0,InstructionCounts=counts,ExitVAs=[0x463075],
                CandidateBudget=12,RawSiteBudget=12,ActualCallSites=len(sites),
                DirectCallSites=6,IndirectCallSites=6,DistinctDirectTargets=5,
                DistinctIndirectSourceSlotLeads=4,DistinctTotalLeadCandidates=len(leads),
                ConservativeLeadUnion=leads,ExternalXrefSearches=0,
                ReusedDescriptorEvidence=[0x186AE78,0x186D08C],NewDescriptorReads=0,
                InputCarrier='one stack argument [saved entry-ESP+8] -> EDI; ret4',
                PayloadSourceOffset=56,PayloadLoadWidth=4,PayloadReadVa=0x46303A,
                PayloadPushVa=0x463047,PayloadCalleeArgument=3,
                ControllerReceiver='first cast return saved ESI, not second cast result',
                Callee=0xB3D250,TypeSourceOffset=72,ScalarSourceOffset=100,FlagsSourceOffset=64,
                FlagsNormalization='(word &0xFFFFFFFE)|4',
                ScalarTreatment='x87 fld DWORD then fstp DWORD to overwrite second-argument placeholder',
                CalleePushOrder=['normalized flags','message+38 value','scalar F32','message+48 value'],
                SuccessPayloadNullGuard=False,DispatchChecksInsertionResult=False,
                DirectPayloadSourceFieldWrites=[],
                FailurePayloadSourceOffset=56,FailureNullGuard=True,
                FailureIndirectSlotOffset=0,FailureCallArgument=1,
                FailurePayloadFieldClearedLocally=False,
                ConcretePayloadVtableRtti='Unknown; runtime first-word slot0 use does not identify class',
                PayloadVirtualCallUsageObserved=True,
                StorageOwner='current incoming argument at+38; semantic lifetime ownership Unknown',
                RetainedTransferredUniqueDuplicateOwnership='Unknown',
                CallbacksArePayload='Only fallback uses payload; earlier object callbacks are separate sources; alias Unknown',
                ExceptionHandler=0x141A7D8,ExceptionRecovery='Unknown; handler/delegates unexpanded',
                ThreadPhaseAndOccurrence='Unknown in this body; inbound interpretation reused from protocol docs',
                FirstUnresolvedPayloadBoundary='incoming message field+38; factory not expanded',
                Cancellation='no explicit cancel check; delegate policy Unknown',
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
    print(f"PASS {result['InstructionAnchors']} anchors/one dispatch body; message+38 to argument3; concrete payload class Unknown")


if __name__ == '__main__':
    main()
