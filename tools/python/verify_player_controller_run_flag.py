"""Bounded static witnesses for player controller runWhenMoving preference."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
GLOBAL = 0x1882394
REFERENCES = [0x629354, 0x6293B8, 0x629A14, 0x629B85, 0x62CEA7, 0x62CEB1]
ANCHORS = [
    (0x62933F, 'call', '0x504af0'),
    (0x629353, 'mov', 'byte ptr [0x1882394], al'),
    (0x6293AD, 'push', '0x1874bd0'), (0x6293B2, 'push', '0x188239c'),
    (0x6293B7, 'push', '0x1882394'), (0x6293BC, 'call', '0xb8da10'),
    (0x504AF0, 'mov', 'al, byte ptr [0x191112a]'),
    (0x62CEA3, 'mov', 'al, byte ptr [ebp + 8]'),
    (0x62CEA6, 'mov', 'byte ptr [0x1882394], al'),
    (0x62CEB0, 'mov', 'al, byte ptr [0x1882394]'),
    (0xB8DA19, 'mov', 'edx, dword ptr [ebp + 8]'),
    (0xB8DA25, 'call', '0xb9b3b0'),
    (0xB9B3CE, 'mov', 'ebx, dword ptr [ebp + 8]'),
    (0xB9B4D5, 'mov', 'cl, byte ptr [ebx]'),
    (0xB9B4DC, 'call', '0xb9bac0'),
    (0xB9B4E3, 'mov', 'byte ptr [ebx], al'),
    (0xEA5653, 'push', '0x18cc9d8'), (0xEA566A, 'push', '0x504af0'),
    (0xEA566F, 'push', '0x62ceb0'), (0xEA5674, 'push', '0x62cea0'),
    (0xEA567C, 'call', '0xff0b80'),
    (0xFF0BF0, 'mov', 'dword ptr [esi + 0xc], ecx'),
    (0xFF0BFA, 'mov', 'dword ptr [esi + 0x10], edx'),
    (0xFF0BFD, 'mov', 'dword ptr [esi + 0x14], eax'),
    (0x629A12, 'mov', 'cl, byte ptr [0x1882394]'),
    (0x629A2D, 'mov', 'byte ptr [ebp - 0x1b], cl'),
    (0x629A64, 'call', '0xaa6660'), (0x629A6B, 'jle', '0x629ce9'),
    (0x629A89, 'call', '0xaa63e0'),
    (0x629A8E, 'mov', 'eax, dword ptr [ebp - 0x34]'),
    (0x629A91, 'cmp', 'eax, 0x99'), (0x629A9C, 'je', '0x629b84'),
    (0x629B84, 'mov', 'al, byte ptr [0x1882394]'),
    (0x629B89, 'test', 'al, al'),
    (0x629B8B, 'sete', 'byte ptr [ebp - 0x1b]'),
    (0x629B8F, 'jmp', '0x629ccc'),
    (0x629CCF, 'inc', 'esi'), (0x629CE3, 'jl', '0x629a71'),
]
SPANS = [
    (0x629310, 231, '152a57b1c9dd5a4653a1f44b14cde9b17403a4965c0106ea7ece70851575b946'),
    (0x62CEA0, 13, 'd5e950dc25488ef814af4c1f85d5fa45e6c9af53edfc8aaba0ca4dca5b6f39f8'),
    (0x62CEB0, 6, 'c393d4dcbe30515522e19a9f05b36cc70f5427ea9b4278d83edc71d19328362c'),
    (0xB9B3B0, 316, '390fb32fc5aea116c33c6344b7372db3f4118a439f3091a36959543c77733c38'),
]
STRINGS = [(0x1874BD0, b'runWhenMoving'), (0x188239C, b'clientGame'),
           (0x18CC9D8, b'checkRunDefault')]


def frozen_api(repo):
    return runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))


def audit(exe, packages, server_root, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
    repo = repo or Path(__file__).resolve().parents[2]
    frozen = frozen_api(repo)
    prior = frozen['audit'](exe, packages, server_root, repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    read = lambda va, size: frozen['exact'](
        lambda at, count: pe.get_data(at - pe.OPTIONAL_HEADER.ImageBase, count), va, size)
    # Exact-address byte candidates only: absence of aliases/indirect writers is not inferred.
    needle = struct.pack('<I', GLOBAL)
    offsets = []
    at = 0
    while True:
        at = raw.find(needle, at)
        if at < 0:
            break
        offsets.append(at)
        if len(offsets) > 32:
            raise ValueError('Global reference candidate cap exceeded')
        at += 1
    references = [pe.OPTIONAL_HEADER.ImageBase + pe.get_rva_from_offset(v) for v in offsets]
    if references != REFERENCES:
        raise ValueError('Direct-address reference inventory mismatch')
    if read(GLOBAL, 1) != b'\x01':
        raise ValueError('File-backed pre-initialization byte mismatch')
    counts = []
    for va, size, digest in SPANS:
        data = read(va, size)
        if hashlib.sha256(data).hexdigest() != digest:
            raise ValueError('Bounded body digest mismatch')
        instructions = list(md.disasm(data, va))
        if sum(i.size for i in instructions) != size or instructions[-1].mnemonic != 'ret':
            raise ValueError('Incomplete bounded body')
        counts.append(len(instructions))
    for va, mnemonic, operand in ANCHORS:
        ins = next(md.disasm(read(va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operand):
            raise ValueError(f'Instruction mismatch {va:08X}')
    for va, text in STRINGS:
        if read(va, len(text) + 1) != text + b'\0':
            raise ValueError('Preference/UI string mismatch')
    return dict(ExecutableSha256=EXPECTED_EXE, DirectAddressCandidates=len(references),
                CandidateCap=32, WriterFunctionCap=3, InstructionAnchors=len(ANCHORS),
                BoundedBodyInstructionCounts=counts, Preference='clientGame/runWhenMoving',
                UIWitness='checkRunDefault', InputMessage=153,
                LocalOverride='logical inverse of global default, not previous local',
                HeldOrReleasedSemantics='Unknown', ResetInvocation='Unknown',
                EffectiveDefault='Unknown; initializer overwrites file-backed one',
                AliasedWritersExhausted=False, CanonicalRename=False,
                FrozenControllerEvidence=prior, OriginalExecuted=False, LiveContact=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    try:
        target = frozen_api(repo)['fresh_private_path'](args.receipt, repo)
        result = audit(args.exe, args.packages, args.server_root, repo)
        with target.open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(json.dumps(result, indent=2) + '\n')
    except (ValueError, OSError, ImportError, StopIteration) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['InstructionAnchors']} run-default anchors; six direct-address candidates")


if __name__ == '__main__':
    main()
