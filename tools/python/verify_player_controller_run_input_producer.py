"""SHA-gated static run-input producer and bounded command-asset witness."""
import argparse
import ast
import hashlib
import json
from pathlib import Path
import runpy
import struct
import sys
import zlib

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
EXPECTED_ASSET = 'bbff0aad5678600493b89a4c27d85da05842548163bb7f44921bfa64347571f4'
ANCHORS = [
    (0xAAE1D3, 'mov', 'edi, dword ptr [edi + 0xc]'),
    (0xAAE1D6, 'test', 'edi, edi'),
    (0xAAE1E1, 'push', '0'), (0xAAE1E3, 'push', 'edi'),
    (0xAAE1E4, 'lea', 'ecx, [eax + 0x30]'), (0xAAE1E7, 'call', '0xab33e0'),
    (0xAAE1F9, 'call', '0x12ea920'),
    (0xAAE00E, 'add', 'ecx, 0x1c'), (0xAAE011, 'call', '0xab33e0'),
    (0xAAE5F4, 'lea', 'ecx, [eax + 0x44]'), (0xAAE5F7, 'call', '0xab33e0'),
    (0xAB33FE, 'mov', 'eax, dword ptr [esi]'),
    (0xAB3405, 'je', '0xab3552'),
    (0xAB340B, 'mov', 'ecx, dword ptr [ebp + 0xc]'),
    (0xAB3419, 'mov', 'edx, dword ptr [esi + 4]'),
    (0xAB341F, 'mov', 'ecx, dword ptr [esi + 8]'),
    (0xAB3425, 'cmp', 'ecx, edx'), (0xAB342B, 'jne', '0xab3450'),
    (0xAB3430, 'mov', 'ecx, dword ptr [ebp + 8]'),
    (0xAB3433, 'push', '0'), (0xAB3435, 'push', 'edx'),
    (0xAB3436, 'push', 'eax'), (0xAB3437, 'call', '0xaa6640'),
    (0xAA664D, 'push', '0'), (0xAA6651, 'call', '0xaa6480'),
    (0xAA64A6, 'jne', '0xaa64c6'),
    (0xAA64CF, 'mov', 'ecx, dword ptr [ebp + 0xc]'),
    (0xAA64E0, 'mov', 'esi, dword ptr [edx + 0xc]'),
    (0xAA64FF, 'mov', 'dword ptr [edi], eax'),
    (0xAA6504, 'mov', 'dword ptr [edi + 4], eax'),
    (0xAA650A, 'mov', 'dword ptr [edi + 8], ecx'),
    (0xAA6510, 'mov', 'dword ptr [edi + 0xc], eax'),
    (0xAA6618, 'mov', 'edx, dword ptr [edx + 0x10]'),
    (0xAA6624, 'call', 'dword ptr [eax + 4]'),
    (0xAA6677, 'mov', 'eax, dword ptr [esi + 8]'),
    (0xAA667D, 'mov', 'dword ptr [esi + 8], ecx'),
    (0xAA6687, 'mov', 'dword ptr [esi + 0xc], eax'),
    (0xAA668A, 'call', '0xaa6370'),
    (0xAA63E3, 'mov', 'ecx, dword ptr [ecx + 8]'),
    (0xAA63EE, 'shl', 'eax, 4'),
    (0xAA63F5, 'mov', 'dword ptr [ecx], edx'),
    (0xAA63FD, 'mov', 'dword ptr [ecx], edx'),
    (0xAA6405, 'mov', 'dword ptr [ecx], edx'),
    (0x62A9B0, 'mov', 'eax, dword ptr [ecx + 4]'),
]
SPANS = [
    (0xAAE150, 185, '99e6743bcc4b242e910fdaa12c26ff55d128ab82dc74317981040761ae802410'),
    (0xAB33E0, 389, '44c51dcc2a2865922dd20e836df7d75f2e8eb2862c086d8e5a4f7d4f1c94c236'),
    (0xAA6480, 448, 'f434feee4249caed508fa79d966b41a9fc235a4900684a54bc5bc45ce40eef7d'),
    (0xAA6640, 26, 'b1cd3067fcda29990f7fe27c89030629d87cb06e306b4fc2a7a4ca7003a3c96d'),
    (0xAA63E0, 55, 'b5b9090c46a070286d7206e6489f93256b687a75e94fda0347b0c7134776b972'),
    (0xAA6670, 132, 'ecb04ddf7846841dd1b4bea4112439d9281138f1b9b8217d0708da6f2cb4ffe5'),
]


def frozen_api(repo):
    return runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))


def command_bytes(path, repo):
    if path.suffix.lower() != '.tre':
        if not 12 <= path.stat().st_size <= 2 * 1024 * 1024:
            raise ValueError('Explicit command asset size cap before read')
        return path.read_bytes()
    if not 36 <= path.stat().st_size <= 1024 * 1024 * 1024:
        raise ValueError('Explicit command archive size cap')
    # Reuse the inspected archive reader's functions without running its CLI.
    source = (repo / 'tools/python/input_command_inventory.py').read_text(encoding='utf-8')
    tree = ast.parse(source)
    functions = [node for node in tree.body if isinstance(node, ast.FunctionDef)]
    namespace = dict(Path=Path, struct=struct, zlib=zlib)
    exec(compile(ast.Module(body=functions, type_ignores=[]), 'existing-command-reader', 'exec'), namespace)
    matches = [data for name, present, data in namespace['recs'](path)
               if name == 'input/groundinputmap_cmds.iff' and present]
    if len(matches) != 1:
        raise ValueError('Explicit archive command source absent/ambiguous')
    return matches[0]


def chunks(data, start, end):
    if not 0 <= start <= end <= len(data):
        raise ValueError('Invalid chunk bounds')
    at = start
    while at < end:
        if end - at < 8:
            raise ValueError('Truncated chunk header')
        stop = at + 8 + struct.unpack_from('>I', data, at + 4)[0]
        if stop > end:
            raise ValueError('Truncated chunk payload')
        yield data[at:at + 4], at + 8, stop
        at = stop


def cstring(data, at, end, cap=256):
    stop = data.find(b'\0', at, min(end, at + cap + 1))
    if stop < 0:
        raise ValueError('Unterminated or oversized string')
    try:
        text = data[at:stop].decode('ascii')
    except UnicodeDecodeError as error:
        raise ValueError('Non-ASCII command string') from error
    return text, stop + 1


def selected_command(data):
    """Bounded independently authored reader; original names stay in private receipts."""
    if not 12 <= len(data) <= 2 * 1024 * 1024:
        raise ValueError('Command asset size cap')
    top = list(chunks(data, 0, len(data)))
    if len(top) != 1 or top[0][0] != b'FORM' or data[top[0][1]:top[0][1] + 4] != b'CMDS':
        raise ValueError('Expected CMDS outer form')
    _, begin, end = top[0]
    versions = list(chunks(data, begin + 4, end))
    if len(versions) != 1 or versions[0][0] != b'FORM':
        raise ValueError('Expected one command version')
    _, begin, end = versions[0]
    if data[begin:begin + 4] != b'0006':
        raise ValueError('Unsupported command version')
    names = set()
    matches = []
    for tag, begin, end in chunks(data, begin + 4, end):
        if tag != b'FORM' or data[begin:begin + 4] != b'CATE':
            raise ValueError('Unexpected command category')
        category = None
        for tag, begin, end in chunks(data, begin + 4, end):
            if tag == b'NAME':
                category, stop = cstring(data, begin, end)
                if stop != end:
                    raise ValueError('Category trailing bytes')
                continue
            if tag != b'CMD ' or category is None:
                raise ValueError('Command without category')
            command, at = cstring(data, begin, end)
            if not command or command in names or len(names) >= 512:
                raise ValueError('Duplicate/empty command or command cap')
            names.add(command)
            if at + 6 > end:
                raise ValueError('Truncated command metadata')
            mask = struct.unpack_from('<I', data, at)[0]
            flags = list(data[at + 4:at + 6])
            at += 6
            actions = []
            for slot in range(4):
                if at + 8 > end:
                    raise ValueError('Truncated action words')
                operation, parameter = struct.unpack_from('<II', data, at)
                payload, at = cstring(data, at + 8, end, 4096)
                actions.append(dict(Slot=slot, Operation=operation, Parameter=parameter, Payload=payload))
            if at != end:
                raise ValueError('Command trailing bytes')
            if any(action['Operation'] == 153 for action in actions):
                matches.append(dict(Command=command, Category=category, Mask=mask, Flags=flags, Actions=actions))
                if len(matches) > 32:
                    raise ValueError('Producer candidate cap')
    if len(matches) != 1:
        raise ValueError('Producer is absent or ambiguous')
    return matches[0]


def audit(exe, packages, server_root, command_asset, repo=None):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
    repo = repo or Path(__file__).resolve().parents[2]
    data = command_bytes(command_asset, repo)
    if hashlib.sha256(data).hexdigest() != EXPECTED_ASSET:
        raise ValueError('Wrong original command-asset SHA256')
    selected = selected_command(data)
    one = struct.unpack('<I', struct.pack('<f', 1.0))[0]
    expected = [(0, 0, ''), (153, one, ''), (0, 0, ''), (0, 0, '')]
    if [(a['Operation'], a['Parameter'], a['Payload']) for a in selected['Actions']] != expected:
        raise ValueError('Selected original action-slot witness mismatch')
    prior = runpy.run_path(str(repo / 'tools/python/verify_player_controller_run_flag.py'))['audit'](
        exe, packages, server_root, repo)
    frozen = frozen_api(repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    read = lambda va, size: frozen['exact'](
        lambda at, n: pe.get_data(at - pe.OPTIONAL_HEADER.ImageBase, n), va, size)
    counts = []
    for va, size, digest in SPANS:
        body = read(va, size)
        if hashlib.sha256(body).hexdigest() != digest:
            raise ValueError('Bounded producer body mismatch')
        instructions = list(md.disasm(body, va))
        if sum(i.size for i in instructions) != size or instructions[-1].mnemonic != 'ret':
            raise ValueError('Incomplete producer body')
        counts.append(len(instructions))
    for va, mnemonic, operand in ANCHORS:
        ins = next(md.disasm(read(va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operand):
            raise ValueError(f'Instruction mismatch {va:08X}')
    return dict(ExecutableSha256=EXPECTED_EXE, CommandAssetSha256=EXPECTED_ASSET,
                CommandInput=str(command_asset),
                CommandAssetBytes=len(data), SelectedCommand=selected,
                InstructionAnchors=len(ANCHORS), BoundedBodyInstructionCounts=counts,
                Producer='InputMap deactivation slot1 -> AB33E0 -> AA6640 -> AA6480',
                QueueTuple='operation/scalar word/null payload/null fourth word',
                HeldEmissionForSelectedRecord=False, PressEmissionForSelectedRecord=False,
                CancellationEmissionForSelectedRecord=False, ReleaseSlotEmission=True,
                EffectivePhysicalBinding='Unknown', ControllerQueueBinding='Unknown',
                EffectiveOverlayRecomputed=False, FrozenRunFlagEvidence=prior,
                OriginalExecuted=False, LiveContact=False, CanonicalRename=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'command-asset', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    try:
        target = frozen_api(repo)['fresh_private_path'](args.receipt, repo)
        result = audit(args.exe, args.packages, args.server_root, args.command_asset, repo)
        with target.open('x', encoding='utf-8', newline='\n') as stream:
            stream.write(json.dumps(result, indent=2) + '\n')
    except (ValueError, OSError, ImportError, StopIteration) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['InstructionAnchors']} producer anchors; original release-slot occurrence")


if __name__ == '__main__':
    main()
