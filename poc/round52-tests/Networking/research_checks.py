"""Independent small static byte/call witnesses for one release-slot input producer."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import stat
import struct
from types import SimpleNamespace
from unittest.mock import patch


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'command-asset', 'output-dir']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    api = runpy.run_path(str(repo / 'tools/python/verify_player_controller_run_input_producer.py'))
    guard = api['frozen_api'](repo)
    output = guard['fresh_private_path'](args.output_dir, repo, directory=True)
    raw = args.exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':
        raise ValueError('Independent identity rejection before address use')
    nt = struct.unpack_from('<I', raw, 0x3c)[0]
    count = struct.unpack_from('<H', raw, nt + 6)[0]
    section_start = nt + 24 + struct.unpack_from('<H', raw, nt + 20)[0]
    base = struct.unpack_from('<I', raw, nt + 52)[0]

    def read(va, size):
        rva = va - base
        for index in range(count):
            at = section_start + 40 * index
            virtual, start, disk_size, disk = struct.unpack_from('<4I', raw, at + 8)
            if start <= rva and rva + size <= start + disk_size:
                result = raw[disk + rva - start:disk + rva - start + size]
                if len(result) == size:
                    return result
        raise ValueError('Independent unmapped/truncated span')

    witnesses = [(0xAAE1E1, '6a00'), (0xAAE1E4, '8d4830'),
                 (0xAB33FE, '8b06'), (0xAB3419, '8b5604'),
                 (0xAA64FF, '8907'), (0xAA6504, '894704'),
                 (0xAA63EE, 'c1e004'), (0xAA667D, '894e08'),
                 (0xAA6687, '89460c'), (0x62A9B0, '8b4104c3')]
    for va, expected in witnesses:
        literal = bytes.fromhex(expected)
        if read(va, len(literal)) != literal:
            raise ValueError('Independent byte witness mismatch')
    calls = [(0xAAE1E7, 0xAB33E0), (0xAB3437, 0xAA6640),
             (0xAA6651, 0xAA6480), (0xAA668A, 0xAA6370),
             (0xAAE5F7, 0xAB33E0), (0xAAE011, 0xAB33E0)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent relative-call mismatch')
    positive = api['audit'](args.exe, args.packages, args.server_root, args.command_asset, repo)
    asset = args.command_asset.read_bytes()
    key = positive['SelectedCommand']['Command'].encode('ascii') + b'\0'
    if asset.count(key) != 1:
        raise ValueError('Independent command string uniqueness')
    at = asset.index(key) + len(key)
    mask = int.from_bytes(asset[at:at + 4], 'little')
    flags = list(asset[at + 4:at + 6]); at += 6
    independent = []
    for slot in range(4):
        operation = int.from_bytes(asset[at:at + 4], 'little')
        parameter = int.from_bytes(asset[at + 4:at + 8], 'little'); at += 8
        end = asset.index(b'\0', at)
        payload = asset[at:end].decode('ascii'); at = end + 1
        independent.append((operation, parameter, payload))
    one = int.from_bytes(struct.pack('<f', 1.0), 'little')
    if independent != [(0, 0, ''), (153, one, ''), (0, 0, ''), (0, 0, '')]:
        raise ValueError('Independent original four-slot witness')
    if (mask, flags) != (positive['SelectedCommand']['Mask'], positive['SelectedCommand']['Flags']):
        raise ValueError('Independent metadata mismatch')
    negatives = []

    def reject(label, action):
        try:
            action()
        except (ValueError, OSError, StopIteration):
            negatives.append(label)
        else:
            raise AssertionError('Accepted negative: ' + label)

    wrong = output / 'wrong-exe.bin'
    wrong.write_bytes(b'synthetic identity failure')
    reject('wrong executable before package import', lambda: api['audit'](wrong, output / 'absent', args.server_root, args.command_asset, repo))
    reject('missing executable', lambda: api['audit'](output / 'missing', args.packages, args.server_root, args.command_asset, repo))
    anchors = api['audit'].__globals__['ANCHORS']
    former = anchors[0]
    anchors[0] = (former[0], 'nop', '')
    try:
        reject('wrong anchor expectation', lambda: api['audit'](args.exe, args.packages, args.server_root, args.command_asset, repo))
    finally:
        anchors[0] = former
    reject('truncated mapped helper', lambda: guard['exact'](lambda va, n: b'\0' * (n - 1), 0x1000, 4))
    reject('empty mapped helper', lambda: guard['exact'](lambda va, n: b'', 0x1000, 1))
    reject('unmapped independent span', lambda: read(0xffffffff, 8))
    reject('existing output file', lambda: guard['fresh_private_path'](wrong, repo))
    reject('existing output directory', lambda: guard['fresh_private_path'](output, repo, directory=True))
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r52.json', repo))
    original_lstat = Path.lstat
    # Synthetic lstat attributes exercise lexical rejection without creating host links.
    for label, attributes, mode in [('symlink ancestor', 0, stat.S_IFLNK),
                                    ('reparse ancestor', 0x400, stat.S_IFDIR)]:
        ancestor = output / 'synthetic-ancestor'

        def fake_lstat(path):
            if path == ancestor:
                return SimpleNamespace(st_mode=mode, st_file_attributes=attributes)
            return original_lstat(path)

        with patch.object(Path, 'lstat', fake_lstat), patch.object(Path, 'resolve', side_effect=AssertionError('resolved before lexical rejection')):
            reject(label, lambda: guard['fresh_private_path'](ancestor / 'new.json', repo))
        if ancestor.exists():
            raise AssertionError('Negative created output ancestor')
    def chunk(tag, payload):
        return tag + len(payload).to_bytes(4, 'big') + payload

    def fixture(names=(b'synthetic-run-producer',), payload=b'', trailing=b'', version=b'0006', truncate_metadata=False):
        records = []
        for name in names:
            fields = name + b'\0' + (3).to_bytes(4, 'little') + b'\0\0'
            if truncate_metadata:
                fields = name + b'\0' + b'\0'
            else:
                for slot in range(4):
                    fields += (153 if slot == 1 else 0).to_bytes(4, 'little')
                    fields += (one if slot == 1 else 0).to_bytes(4, 'little')
                    fields += payload + b'\0'
            records.append(chunk(b'CMD ', fields + trailing))
        category = chunk(b'FORM', b'CATE' + chunk(b'NAME', b'synthetic\0') + b''.join(records))
        return chunk(b'FORM', b'CMDS' + chunk(b'FORM', version + category))

    synthetic = fixture()
    parsed = api['selected_command'](synthetic)
    if parsed['Command'] != 'synthetic-run-producer' or parsed['Actions'][1]['Operation'] != 153:
        raise AssertionError('Synthetic positive reader')
    malformed = [
        ('empty command asset', b''), ('truncated outer header', synthetic[:7]),
        ('truncated original-size framing', synthetic[:-1]),
        ('wrong outer tag', b'FAIL' + synthetic[4:]),
        ('oversized outer length', synthetic[:4] + b'\xff' * 4 + synthetic[8:]),
        ('wrong command version', fixture(version=b'0005')),
        ('missing producer', synthetic.replace((153).to_bytes(4, 'little'), b'\0' * 4)),
        ('duplicate command', fixture(names=(b'synthetic', b'synthetic'))),
        ('ambiguous producer', fixture(names=(b'synthetic-one', b'synthetic-two'))),
        ('command name cap', fixture(names=(b'x' * 257,))),
        ('non-ASCII name', fixture(names=(b'\xff',))),
        ('truncated metadata', fixture(truncate_metadata=True)),
        ('command trailing bytes', fixture(trailing=b'x')),
        ('payload cap', fixture(payload=b'x' * 4097)),
        ('asset size cap', b'\0' * (2 * 1024 * 1024 + 1)),
        ('extra top-level form', synthetic + synthetic),
    ]
    for label, data in malformed:
        reject(label, lambda data=data: api['selected_command'](data))
    wrong_asset = output / 'synthetic-command.iff'
    wrong_asset.write_bytes(synthetic)
    reject('wrong original asset identity', lambda: api['audit'](args.exe, args.packages, args.server_root, wrong_asset, repo))
    for extension, cap in [('.tre', 1024 * 1024 * 1024), ('.iff', 2 * 1024 * 1024)]:
        with patch.object(Path, 'stat', return_value=SimpleNamespace(st_size=cap + 1)), patch.object(Path, 'read_bytes', side_effect=AssertionError('read before size cap')):
            reject('input pre-read size cap ' + extension, lambda extension=extension: api['command_bytes'](output / ('uncreated' + extension), repo))
    nested = guard['fresh_private_path'](output / 'nested/new-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent":true}\n')
    result = dict(Positive=positive, IndependentBytes=len(witnesses), IndependentCalls=len(calls),
                  IndependentOriginalActionSlots=4, IndependentCommandString=1, NegativeChecks=negatives, SyntheticHelpersOnly=True,
                  OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS ten independent bytes/six calls/four original slots; {len(negatives)} rejection checks')


if __name__ == '__main__':
    main()
