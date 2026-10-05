"""Selected ground normalized input edge; queue instance binding remains explicit."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (0x519C61, 'mov', 'esi, ecx'),
    (0x519C78, 'mov', 'dword ptr [esi + 0x4c], ebx'),
    (0x519E7F, 'mov', 'dword ptr [esi], 0x15dd988'),
    (0x519FFD, 'mov', 'eax, dword ptr [esi + 0x4c]'),
    (0x51A004, 'mov', 'dword ptr [eax + 0xc], ebx'),
    (0x51A00B, 'call', '0x6e2350'),
    (0x51A013, 'mov', 'dword ptr [esi + 0x4c], ebx'),
    (0x51AF30, 'mov', 'edi, dword ptr [ebp + 8]'),
    (0x51AF33, 'push', 'edi'), (0x51AF34, 'mov', 'esi, ecx'),
    (0x51AF36, 'call', '0x51aa40'),
    (0x51AA45, 'mov', 'edi, dword ptr [ebp + 8]'),
    (0x51AA48, 'mov', 'esi, ecx'),
    (0x51AA69, 'mov', 'eax, dword ptr [esi + 0x9c]'),
    (0x51AA6F, 'sub', 'eax, 3'), (0x51AA72, 'je', '0x51aa8a'),
    (0x51AA74, 'sub', 'eax, edx'), (0x51AA76, 'je', '0x51aa85'),
    (0x51AA78, 'dec', 'eax'), (0x51AA79, 'je', '0x51aa80'),
    (0x51AA7B, 'mov', 'esi, dword ptr [esi + 0x4c]'),
    (0x51AA80, 'mov', 'esi, dword ptr [esi + 0x54]'),
    (0x51AA85, 'mov', 'esi, dword ptr [esi + 0x50]'),
    (0x51AA8A, 'mov', 'esi, dword ptr [esi + 0x58]'),
    (0x51AA8F, 'test', 'esi, esi'),
    (0x51AA91, 'je', '0x51aad6'),
    (0x51AABE, 'mov', 'ecx, esi'), (0x51AAC0, 'call', '0xaae2e0'),
    (0xAAE2E6, 'mov', 'edx, dword ptr [eax + 4]'),
    (0xAAE2E9, 'add', 'edx, -5'),
    (0xAAE2EC, 'cmp', 'edx, 0xb'),
    (0xAAE2F1, 'ja', '0xaae434'),
    (0xAAE2F7, 'jmp', 'dword ptr [edx*4 + 0xaae43c]'),
    (0xAAE320, 'mov', 'edx, dword ptr [eax + 8]'),
    (0xAAE323, 'cmp', 'edx, dword ptr [ecx + 0x10]'),
    (0xAAE326, 'jne', '0xaae434'),
    (0xAAE32C, 'mov', 'eax, dword ptr [eax + 0xc]'),
    (0xAAE32F, 'push', 'eax'), (0xAAE330, 'push', '1'),
    (0xAAE332, 'call', '0xaae150'),
    (0x62A9B0, 'mov', 'eax, dword ptr [ecx + 4]'),
    (0x6E1FBF, 'push', '0'), (0x6E1FC5, 'call', '0xaacd50'),
    (0xAACD85, 'mov', 'dword ptr [esi + 0xc], eax'),
]
SPANS = [
    (0x519C40, 834, '236701041aead5b3007f293ec06d73cb2185220af8ae5ace88b5745a3e3017ae', 'ret'),
    (0x519F90, 949, '00d3495579234f9c083eb7216a46c432a25a4d9f251628fd81c42d1b35b05a40', 'ret'),
    (0x51AA40, 156, '042a434085c8213a8ec7d42d6c1e8cc5b80948b38fe464fb93c2a1b94d3341b6', 'ret'),
    (0xAAE2E0, 346, '569d4fa324288ce1b79279e13da0a85bdaafa0b28289d8a754abcb835b891dfb', 'ret'),
    (0x62A9B0, 4, 'f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f', 'ret'),
    (0x51AF10, 43, '5f2d40ab631210591834afc81862d5042fcc455a5b755af3fc911f9f1a2d857d', 'call'),
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
                if target in (0xAAE150, 0xAACD50, 0xAAE2E0):
                    result = capped([*result, dict(VA=va, Target=target, Kind='relative-call')])
            at += 1
    at = 0
    while True:
        at = raw.find(struct.pack('<I', 0x191D214), at)
        if at < 0:
            break
        va = base + pe.get_rva_from_offset(at)
        result = capped([*result, dict(VA=va, Target=0x191D214, Kind='global-operand')])
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
    if len(xrefs) != 20 or sum(x['Kind'] == 'relative-call' for x in xrefs) != 11:
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
    if struct.unpack('<I', read(0x15DD98C, 4))[0] != 0x51AF10:
        raise ValueError('Ground event virtual slot mismatch')
    if struct.unpack('<I', read(0xAAE448, 4))[0] != 0xAAE320:
        raise ValueError('Normalized type8 dispatch target mismatch')
    return dict(ExecutableSha256=EXPECTED_EXE, InstructionAnchors=len(ANCHORS),
                CompleteBodyInstructionCounts=counts[:5], EventPrefixInstructions=counts[5],
                CandidateXrefs=xrefs, CandidateBudget=20, GroundRttiConfirmed=True,
                GroundSelection='selector+9C default branch -> map+4C; cases3/5/6 use other maps',
                NormalizedEndBinding='type8; device+8 matches map+10; input index+0C; family1',
                GroundEventCallerConfirmed=True, ControllerQueueInstanceIdentity='Unknown',
                ConstructorMapNull=True, TeardownMapQueueNull=True,
                ProfileTemplateConstructedWithNullQueue=True, ActiveAssignmentClosed=False,
                PhysicalBinding='Unknown', EffectiveProfile='Unknown',
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
    print(f"PASS {result['InstructionAnchors']} selected edge anchors; normalized caller closed; queue identity Unknown")


if __name__ == '__main__':
    main()
