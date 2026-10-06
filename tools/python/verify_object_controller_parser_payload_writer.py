"""Parser receiver+38 zero/init and exact factory EAX-return writer; factory unexpanded."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (11310720, 'mov', 'esi, ecx'),
    (11310723, 'mov', 'dword ptr [ebp - 0x24], esi'),
    (11310726, 'xor', 'ebx, ebx'),
    (11310825, 'mov', 'dword ptr [esi + 0x38], ebx'),
    (11310856, 'mov', 'dword ptr [ebx + 4], 0'),
    (11310927, 'mov', 'dword ptr [esi + 0x64], 0'),
    (11310945, 'mov', 'dword ptr [esi], 0x15eac08'),
    (11310951, 'mov', 'dword ptr [esi + 0x10], 0x15eac00'),
    (11310996, 'mov', 'edi, dword ptr [ebp + 8]'),
    (11310999, 'push', 'edi'),
    (11311000, 'mov', 'ecx, esi'),
    (11311002, 'call', '0x118b740'),
    (11311007, 'mov', 'eax, dword ptr [esi + 0x48]'),
    (11311010, 'push', 'edi'),
    (11311011, 'push', 'eax'),
    (11311012, 'call', '0xb0d190'),
    (11311017, 'mov', 'edx, dword ptr [esi + 0x48]'),
    (11311020, 'mov', 'dword ptr [esi + 0x38], eax'),
    (11311052, 'push', 'edx'),
    (11311053, 'push', '0x18afa7c'),
    (11311066, 'push', '0xff'),
    (11311071, 'push', 'eax'),
    (11311072, 'call', '0xaa5980'),
    (11311077, 'add', 'esp, 0x18'),
    (11311167, 'call', '0x12b48e0'),
    (11311192, 'call', '0x414d00'),
    (11311197, 'mov', 'eax, esi'),
    (11311215, 'ret', '4'),
]
SPANS = [(11310688, 530, 'a75e091a364e4153604d88a29fe948562f12430156b88d10abdfc2b1b247fc37', 'ret')]

def frozen_api(repo):
    # Reuse only private output/span guards; do not repeat the accepted producer audit.
    return runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))


def capped(values, limit=16):
    result = []
    for value in values:
        if len(result) >= limit:
            raise ValueError('Candidate xref budget exceeded')
        result.append(value)
    return result


EXPECTED_FULL_BODY_CALLS = [
    (11310745, '0x406e50'),
    (11310784, '0x415be0'),
    (11310799, '0xacb270'),
    (11310811, '0x4061f0'),
    (11310828, '0x118b780'),
    (11310851, '0x118b780'),
    (11310881, '0x118b780'),
    (11310901, '0xaa4920'),
    (11310922, '0x118b780'),
    (11310961, '0x118b5f0'),
    (11310969, '0x118b5f0'),
    (11310980, '0x118b5f0'),
    (11310991, '0x118b5f0'),
    (11311002, '0x118b740'),
    (11311012, '0xb0d190'),
    (11311072, '0xaa5980'),
    (11311100, '0x406e50'),
    (11311153, '0x407250'),
    (11311167, '0x12b48e0'),
    (11311192, '0x414d00'),
]
EXPECTED_WRITER_PROBE_SITES = [(0xAC979A,'0x118b740'),(0xAC97A4,'0xb0d190')]
SEALED_INPUTS = {
    'tools/python/verify_object_controller_dispatch_payload_source.py': '72daf1502c19185b6a05842e842cfaaf3e5283594e6b7f8a0b06d662a06c7130',
    'docs/OBJECT_CONTROLLER_DISPATCH_PAYLOAD_SOURCE_CONTRACT.md': '3d2faa77645cf7e13e27c7ac6f149e8c7f1d6cf851a87ff8461ada291970db9f',
}


def seal_inputs(repo):
    for name,digest in SEALED_INPUTS.items():
        if hashlib.sha256((repo/name).read_bytes()).hexdigest()!=digest:
            raise ValueError('Sealed accepted62 input identity mismatch: '+name)
    return dict(SEALED_INPUTS)


def full_call_inventory(instructions, site_limit=20):
    calls=capped([(i.address,i.op_str) for i in instructions if i.mnemonic=='call'],site_limit)
    if calls!=EXPECTED_FULL_BODY_CALLS:
        raise ValueError('Complete authorized body call inventory mismatch')
    return calls


def writer_probe_sites(sites):
    selected=capped(sites)
    if selected!=EXPECTED_WRITER_PROBE_SITES:
        raise ValueError('Selected writer-probe edge mismatch')
    return selected


def candidates(instructions, lead_limit=16):
    calls=full_call_inventory(instructions)
    writer_probe_sites([c for c in calls if c[0] in [0xAC979A,0xAC97A4]])
    targets=sorted({int(op,16) for va,op in calls})
    complete=[dict(Kind='authorized primary',Target=0xAC9660)]
    complete.extend(dict(Kind='observed direct callee; unexpanded',Target=t) for t in targets)
    complete.append(dict(Kind='registered SEH reference; unexpanded',Target=0x14BC29C))
    return capped(complete,lead_limit)


def audit(exe, packages, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
    repo = repo or Path(__file__).resolve().parents[2]
    sealed=seal_inputs(repo)
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
    calls=full_call_inventory(decoded)
    if any(i.mnemonic.startswith('j') for i in decoded):
        raise ValueError('Unexpected explicit branch in fixed parser body')
    field_writes=[(i.address,i.op_str) for i in decoded
                  if i.mnemonic=='mov' and i.op_str.startswith('dword ptr [esi + 0x38],')]
    if field_writes!=[(0xAC96E9,'dword ptr [esi + 0x38], ebx'),
                      (0xAC97AC,'dword ptr [esi + 0x38], eax')]:
        raise ValueError('Receiver+38 direct write inventory mismatch')
    return dict(ExecutableSha256=EXPECTED_EXE,SealedAccepted62Inputs=sealed,
                InstructionAnchors=len(ANCHORS),PrimaryBodies=1,TinyDirectWrappers=0,
                InstructionCounts=counts,ExitVAs=[0xAC986F],
                FullBodyCallSites=len(calls),FullBodyDistinctDirectTargets=13,
                FullBodyIndirectSites=0,FullBodyCallInventory=calls,
                DistinctTotalLeadCandidates=len(leads),CompleteLeadInventory=leads,ExpandedCalleeBodies=0,
                SelectedProbeCallSites=EXPECTED_WRITER_PROBE_SITES,SelectedRawProbeSiteCount=2,
                CandidateBudget=16,RawSiteBudget=20,ExternalXrefSearches=0,
                BudgetDefinition='lead-authorized complete inventory:20 sites and16 total leads including primary/SEH; writer sites are semantic witnesses only',
                Receiver='incoming ECX -> ESI; normal EAX return=ESI',
                InputArchive='stack argument [ebp+8] -> EDI at00AC9794',
                InitialPayloadFieldWrite='00AC96E9: receiver+38=zero EBX',
                FactoryDiscriminator='DWORD receiver+48 after00AC979A reader call',
                FactoryPushOrder=['same archive parameter EDI','discriminator EAX'],
                Factory=0xB0D190,FactoryReturnWriter=0xAC97AC,
                FactoryReturnSource='EAX unchanged by intervening00AC97A9 EDX load',
                PayloadFieldOffset=56,PayloadFieldWidth=4,DirectPayloadWrites=field_writes,
                LocalPriorPayloadReadOrDisposalObserved=False,PostFactoryDirectFieldClear=False,
                FactoryResultNullGuard=False,ExplicitNormalConditionalJumps=0,
                PostFactoryHelpersRunAfterNullReturn=True,
                NormalReturnIdentity='incoming receiver',ReturnStackBytes=4,
                ReceiverConstruction='Strongly inferred from base/member initialization and installed receiver tables',
                InstalledReceiverTables=[0x15EAC08,0x15EAC00],ReceiverTablesAreNestedPayloadProof=False,
                ConcretePayloadTypeVtable='Unknown',LifetimeOwnership='Unknown',
                RetryOverwriteSafety='Unknown; initialization shape is not a reusable replacement contract',
                Handler=0x14BC29C,ExceptionRecovery='Unknown; registered handler/delegates unexpanded',
                Cancellation='No explicit local check; delegated policy Unknown',
                FirstUnresolvedBoundary='factory(type DWORD from+48, same input archive)',
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
    print(f"PASS {result['InstructionAnchors']} anchors/one parser body; factory EAX to+38; factory and payload ownership Unknown")


if __name__ == '__main__':
    main()
