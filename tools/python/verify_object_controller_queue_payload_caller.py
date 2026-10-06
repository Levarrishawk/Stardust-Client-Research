"""One exact queue-producer caller: unchanged incoming third argument; payload construction Unknown."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import sys
import struct

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (11784787, 'mov', 'eax, dword ptr [ebp + 0x14]'),
    (11784790, 'mov', 'edx, dword ptr [ebp + 0xc]'),
    (11784794, 'push', 'eax'),
    (11784795, 'mov', 'eax, dword ptr [ebp + 8]'),
    (11784798, 'mov', 'esi, ecx'),
    (11784800, 'mov', 'ecx, dword ptr [ebp + 0x10]'),
    (11784803, 'push', 'ecx'),
    (11784804, 'mov', 'ecx, dword ptr [esi + 4]'),
    (11784807, 'push', 'edx'),
    (11784808, 'push', 'eax'),
    (11784809, 'call', '0xaa6480'),
    (11784814, 'mov', 'ecx, dword ptr [esi + 8]'),
    (11784817, 'test', 'ecx, ecx'),
    (11784819, 'je', '0xb3d286'),
    (11784821, 'mov', 'edx, dword ptr [ecx]'),
    (11784823, 'call', 'dword ptr [edx + 4]'),
    (11784826, 'test', 'al, al'),
    (11784828, 'je', '0xb3d286'),
    (11784830, 'mov', 'ecx, dword ptr [esi + 8]'),
    (11784833, 'mov', 'eax, dword ptr [ecx]'),
    (11784835, 'call', 'dword ptr [eax + 0x3c]'),
    (11784840, 'ret', '0x10'),
]
SPANS = [(11784784, 59, '203326319dab0ffc8e614c5153b84b44340e22b4699b129d876758862eeb577b', 'ret')]

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


EXPECTED_CALLS = [(0xB3D269,'0xaa6480'),(0xB3D277,'dword ptr [edx + 4]'),
                  (0xB3D283,'dword ptr [eax + 0x3c]')]
EXPECTED_PRODUCER_XREFS = [0xAA6651,0xAB3538,0xB3D269]


def candidates(instructions):
    calls=[(i.address,i.op_str) for i in instructions if i.mnemonic=='call']
    if calls!=EXPECTED_CALLS:
        raise ValueError('Selected caller call inventory mismatch')
    return capped([dict(Kind='raw xref owner; unexpanded',Target=0xAA6640),
                   dict(Kind='raw xref owner; unexpanded',Target=0xAB33E0),
                   dict(Kind='selected caller',Target=0xB3D250),
                   dict(Kind='accepted60 producer; reused',Target=0xAA6480),
                   dict(Kind='receiver+8 virtual slot+4; unexpanded',Target='Unknown slot4'),
                   dict(Kind='reread receiver+8 virtual slot+3C; unexpanded',Target='Unknown slot3C')])


def producer_xrefs(pe):
    sites=[]
    base=pe.OPTIONAL_HEADER.ImageBase
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        data=section.get_data();at=0
        while True:
            at=data.find(b'\xe8',at)
            if at<0:
                break
            if at+5<=len(data):
                va=base+section.VirtualAddress+at
                if va+5+struct.unpack_from('<i',data,at+1)[0]==0xAA6480:
                    if len(sites)>=12:
                        raise ValueError('Exact-target raw call-site budget exceeded')
                    sites.append(va)
            at+=1
    if sites!=EXPECTED_PRODUCER_XREFS:
        raise ValueError('Exact producer xref inventory mismatch')
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
    xrefs=producer_xrefs(pe)
    if not any(i.address==0xB3D269 and i.mnemonic=='call' and i.op_str=='0xaa6480'
               for i in decoded):
        raise ValueError('Selected xref lacks complete-body instruction boundary')
    return dict(ExecutableSha256=EXPECTED_EXE,InstructionAnchors=len(ANCHORS),
                PrimaryCallerBodies=1,TinyDirectWrappers=0,InstructionCounts=counts,
                ExitVAs=[0xB3D288],CandidateBudget=12,RawSiteBudget=12,
                ExactTargetQuery=0xAA6480,RawXrefSites=xrefs,RawXrefCount=len(xrefs),
                DistinctTotalLeadCandidates=len(leads),ConservativeLeadUnion=leads,
                IntraBodyDirectCallSites=1,IntraBodyIndirectCallSites=2,
                SelectedBoundaryVerified=True,UnexpandedCandidateBodies=2,
                ThirdArgumentProvenance='[ebp+10] -> ECX -> push ECX at00B3D263',
                CalleeStackPushOrder=['argument4','argument3','argument2','argument1'],
                ProducerReceiver='[saved incoming receiver+4]',
                CallerArgumentTransformation=False,CallerFirstArgumentGuard=False,QueueNullGuard=False,
                PayloadConstructionObserved=False,
                PayloadVtableEstablished=False,PayloadClassRtti='Unknown',
                FirstUnresolvedBoundary='incoming stack argument3 of00B3D250',
                PostProducerFirstObjectSource='incoming receiver+8',FirstVirtualSlotOffset=4,
                FirstVirtualReturnTest='AL only, zero skips second call',
                SecondObjectSource='reread incoming receiver+8; no second null guard',
                SecondVirtualSlotOffset=60,PostProducerPayloadAlias='Unknown',
                RetainedTransferUniqueDuplicateOwnership='Unknown',
                LocalAllocationFreeRefcountCalls=False,LocalCancellationCheck=False,
                ExceptionHandlerObserved=False,ExceptionRecovery='Unknown beyond normal-return order',
                ThreadAndPhase='Unknown for selected execution; earlier protocol path is separate reused evidence',
                ReturnIdentity='No stable receiver/payload result; EAX/AL vary by delegated paths',
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
    print(f"PASS {result['InstructionAnchors']} anchors/one caller; incoming argument3 forwarded; payload construction Unknown")


if __name__ == '__main__':
    main()
