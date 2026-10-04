"""Static receiver and speed-consumption witnesses; never executes the client."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import runpy
import stat
import struct
import sys

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
BODY_SHA = 'fcc6a2b5711874f2f8d6bea09bb54b388a696859b44b2c038a339a4d1b241b0a'
# Small independently selected witnesses, not an exported decompiler body.
ANCHORS = [
    (0x629750, 'push', 'ebp'), (0x62975A, 'mov', 'ebx, ecx'),
    (0x62975C, 'mov', 'eax, dword ptr [ebx + 8]'),
    (0x629763, 'mov', 'dword ptr [ebp - 0x20], eax'),
    (0x62976C, 'call', '0x638810'),
    (0x62987D, 'mov', 'ecx, dword ptr [ebp - 0x20]'),
    (0x629880, 'call', '0x438620'), (0x62988A, 'je', '0x629898'),
    (0x62988C, 'mov', 'ecx, dword ptr [ebp - 0x20]'),
    (0x62988F, 'call', '0x438650'), (0x629894, 'mov', 'edi, eax'),
    (0x629898, 'mov', 'edi, dword ptr [ebp - 0x20]'),
    (0x62989B, 'test', 'edi, edi'),
    (0x62989D, 'mov', 'dword ptr [ebp - 0x3c], edi'),
    (0x6298A8, 'call', '0x638810'),
    (0x629A12, 'mov', 'cl, byte ptr [0x1882394]'),
    (0x629A2D, 'mov', 'byte ptr [ebp - 0x1b], cl'),
    (0x629B84, 'mov', 'al, byte ptr [0x1882394]'),
    (0x629B8B, 'sete', 'byte ptr [ebp - 0x1b]'),
    (0x629F7A, 'push', '0'), (0x629F7C, 'mov', 'ecx, edi'),
    (0x629F7E, 'call', '0x4388a0'),
    (0x629F83, 'fstp', 'dword ptr [ebp - 0x14]'),
    (0x629F86, 'push', '0'), (0x629F88, 'mov', 'ecx, edi'),
    (0x629F8A, 'call', '0x438950'),
    (0x629F8F, 'fstp', 'dword ptr [ebp - 0x34]'),
    (0x629F94, 'call', '0x639ea0'), (0x629F9D, 'fld', 'dword ptr [0x15dc6f8]'),
    (0x629FA6, 'call', '0x628df0'),
    (0x629FAE, 'mov', 'al, byte ptr [ebp - 0x1b]'),
    (0x629FB5, 'fld', 'dword ptr [ebp - 0x14]'),
    (0x629FB8, 'fcomp', 'dword ptr [0x15dc6a0]'),
    (0x629FC0, 'test', 'ah, 0x41'), (0x629FC3, 'jne', '0x629fca'),
    (0x629FC5, 'fld', 'dword ptr [ebp - 0x14]'),
    (0x629FCA, 'fld', 'dword ptr [ebp - 0x34]'),
    (0x629FE3, 'fsqrt', ''), (0x629FE5, 'fmulp', 'st(1)'),
    (0x629FE7, 'fst', 'dword ptr [ebx + 0x154]'),
    (0x629FED, 'fmul', 'st(1)'),
    (0x629FEF, 'fstp', 'dword ptr [ebp - 0x14]'),
    (0x629FF7, 'fcomp', 'dword ptr [ebx + 0x64]'),
    (0x62A009, 'call', '0x431840'), (0x62A00E, 'fmul', 'dword ptr [ebp + 8]'),
    (0x62A011, 'fadd', 'dword ptr [ebx + 0x64]'),
    (0x62A014, 'fst', 'dword ptr [ebx + 0x64]'),
    (0x62A024, 'mov', 'dword ptr [ebx + 0x64], eax'),
    (0x62A03E, 'call', '0x431840'), (0x62A043, 'fchs', ''),
    (0x62A045, 'fmul', 'dword ptr [ebp + 8]'),
    (0x62A04B, 'fst', 'dword ptr [ebx + 0x64]'),
    (0x62A05B, 'mov', 'dword ptr [ebx + 0x64], edx'),
    (0x62A1D6, 'mov', 'edi, dword ptr [ebp - 0x3c]'),
    (0x62A367, 'mov', 'dword ptr [ebx + 0x64], 0'),
    (0x62A36E, 'mov', 'ecx, dword ptr [edi + 0x28]'),
    (0x62A375, 'fld', 'dword ptr [ebx + 0x64]'),
    (0x62A384, 'fstp', 'dword ptr [ebp - 0x2c]'),
    (0x62A38F, 'fstp', 'dword ptr [ebp - 0x28]'),
    (0x62A39A, 'fstp', 'dword ptr [ebp - 0x24]'),
    (0x62A39F, 'call', '0x7ca1f0'),
    (0x62A3AA, 'call', '0x638810'), (0x62A62D, 'ret', '4'),
    (0x438620, 'mov', 'al, byte ptr [ecx + 0x98d]'),
    (0x639EA0, 'mov', 'al, byte ptr [ecx + 0x55]'),
    (0x6295B8, 'mov', 'dword ptr [ebx], 0x15e034c'),
    (0x7CA1F8, 'mov', 'ecx, dword ptr [ecx + 0xcc]'),
    (0x7CA1FE, 'mov', 'dword ptr [ecx], edx'),
    (0x7CA203, 'mov', 'dword ptr [ecx + 4], edx'),
    (0x7CA209, 'mov', 'dword ptr [ecx + 8], eax'),
    (0x7CA20D, 'ret', '4'),
]


def fresh_private_path(path, repo, directory=False):
    """Inspect lexical ancestors before resolve/mkdir, including dangling links."""
    lexical = Path(os.path.abspath(path))
    for item in (lexical, *lexical.parents):
        try:
            info = item.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('Output has a lexical symlink/reparse ancestor')
    private = (repo / 'local-private').resolve()
    target = lexical.resolve()
    if target == private or private not in target.parents:
        raise ValueError('Output must be below repository local-private')
    if lexical.exists():
        raise ValueError('Output must be fresh')
    if directory:
        lexical.mkdir(parents=True)
    else:
        lexical.parent.mkdir(parents=True, exist_ok=True)
    return lexical


def exact(read, va, size):
    data = read(va, size)
    if len(data) != size:
        raise ValueError(f'Truncated mapped span at {va:08X}')
    return data


def audit(exe, packages, server_root, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
    repo = repo or Path(__file__).resolve().parents[2]
    frozen = runpy.run_path(str(repo / 'tools/python/verify_creature_movement_writers.py'))['audit'](
        exe, packages, server_root, repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    read = lambda va, size: pe.get_data(va - pe.OPTIONAL_HEADER.ImageBase, size)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    body = exact(read, 0x629750, 3808)
    if hashlib.sha256(body).hexdigest() != BODY_SHA:
        raise ValueError('Controller body digest mismatch')
    instructions = list(md.disasm(body, 0x629750))
    if len(instructions) != 1163 or sum(i.size for i in instructions) != len(body):
        raise ValueError('Incomplete controller instruction extent')
    addresses = {i.address for i in instructions}
    branches = 0
    for ins in instructions:
        if ins.mnemonic.startswith('j') and ins.op_str.startswith('0x'):
            if int(ins.op_str, 16) not in addresses:
                raise ValueError('Direct branch misses controller instruction boundary')
            branches += 1
    for va, mnemonic, operand in ANCHORS:
        ins = next(md.disasm(exact(read, va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operand):
            raise ValueError(f'Instruction mismatch at {va:08X}')
    for va, expected in [(0x15E0348, 0x16113A0), (0x15E0360, 0x629750),
                         (0x16113AC, 0x186F484)]:
        if struct.unpack('<I', exact(read, va, 4))[0] != expected:
            raise ValueError('Controller vtable/COL/RTTI pointer mismatch')
    name = b'.?AVPlayerCreatureController@@\0'
    if exact(read, 0x186F48C, len(name)) != name:
        raise ValueError('Controller RTTI name mismatch')
    if exact(read, 0x15DC6A0, 4) != struct.pack('<f', 0) or exact(read, 0x15DC6F8, 4) != struct.pack('<f', 1):
        raise ValueError('Selected zero/one constants mismatch')
    sink = list(md.disasm(exact(read, 0x7CA1F0, 32), 0x7CA1F0))
    if sum(i.size for i in sink) != 32 or sink[-1].mnemonic != 'ret':
        raise ValueError('Incomplete vector sink')
    return dict(ExecutableSha256=EXPECTED_EXE, ControllerBodySha256=BODY_SHA,
                ControllerBytes=3808, ControllerInstructions=1163,
                InstructionAnchors=len(ANCHORS), DirectBranchesChecked=branches,
                VectorSinkInstructions=len(sink), FrozenMovementEvidence=frozen,
                Receiver='owner +8 or related lookup result; null falls back',
                ProposedRole='PlayerCreatureController input update and speed consumption',
                CanonicalRename=False, FlagSemantics='Unknown', Units='Unknown',
                IndirectControlFlowExhausted=False, OriginalExecuted=False, LiveContact=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    try:
        target = fresh_private_path(args.receipt, repo)
        result = audit(args.exe, args.packages, args.server_root, repo)
        with target.open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(json.dumps(result, indent=2) + '\n')
    except (ValueError, OSError, ImportError, StopIteration) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['InstructionAnchors']} controller anchors; receiver and direct speed use")


if __name__ == '__main__':
    main()
