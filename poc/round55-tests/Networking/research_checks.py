"""Independent small static byte/call witnesses for two controller constructor carriers and unresolved queue lifetime."""
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
    for name in ['exe', 'packages', 'output-dir']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    api = runpy.run_path(str(repo / 'tools/python/verify_player_controller_queue_lifecycle.py'))
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

    witnesses = [(0x6295B8, 'c7034c035e01'), (0x6385D9, '8b4508'),
                 (0x6385DE, '8bf1'), (0x6385E0, '50'), (0x6386B4, 'c706ac045e01'),
                 (0x6E4A39, '8b4508'), (0x6E4A3D, '8bf1'), (0x6E4A3F, '50'),
                 (0x6E4A57, 'c706b81a5e01'), (0xB3D340, '8b4104c3'), (0x62A9B0, '8b4104c3')]
    for va, expected in witnesses:
        literal = bytes.fromhex(expected)
        if read(va, len(literal)) != literal:
            raise ValueError('Independent byte witness mismatch')
    calls = [(0x629425, 0x6385C0), (0x6385E4, 0x6E4A20),
             (0x6E4A43, 0xB4AE50), (0x6E4A5D, 0xB4B070),
             (0x6295D6, 0x629600), (0x6386D6, 0x638700), (0x6E4A86, 0x6E4AB0),
             (0x6295E2, 0xAC1640), (0x6386E2, 0xAC1640), (0x6E4A92, 0xAC1640)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent relative-call mismatch')
    for va, text in [(0x186F48C, b'.?AVPlayerCreatureController@@\0'),
                     (0x186AE9C, b'.?AVCreatureController@@\0'),
                     (0x186D074, b'.?AVClientController@@\0')]:
        if read(va, len(text)) != text:
            raise ValueError('Independent RTTI string mismatch')
    for va, value in [(0x15E034C, 0x6295D0), (0x15E04AC, 0x6386D0), (0x15E1AB8, 0x6E4A80)]:
        if int.from_bytes(read(va, 4), 'little') != value:
            raise ValueError('Independent dispatch/RTTI data mismatch')
    def carrier(get, start, prefix, target):
        literal = bytes.fromhex(prefix)
        data = get(start, len(literal) + 5)
        if len(data) != len(literal) + 5 or data[:len(literal)] != literal or data[len(literal)] != 0xE8:
            raise ValueError('Independent constructor carrier mismatch')
        actual = start + len(data) + struct.unpack('<i', data[-4:])[0]
        if actual != target:
            raise ValueError('Independent constructor base target mismatch')
        return dict(Receiver='incoming ECX unchanged', ObjectArgument='caller stack+8', Target=target)

    carriers = [carrier(read, 0x6385D9, '8b450853568bf1508975f0', 0x6E4A20),
                carrier(read, 0x6E4A39, '8b4508568bf1508975f0', 0xB4AE50)]

    def thunk(get, entry, body):
        data = get(entry, 33)
        if len(data) != 33 or data[:6] != bytes.fromhex('558bec568bf1') or data[6] != 0xE8:
            raise ValueError('Independent deleting thunk prefix mismatch')
        if entry + 11 + struct.unpack_from('<i', data, 7)[0] != body:
            raise ValueError('Independent deleting thunk body mismatch')
        if data[11:18] != bytes.fromhex('f6450801740956') or data[18] != 0xE8:
            raise ValueError('Independent deleting thunk flags/branch mismatch')
        if entry + 23 + struct.unpack_from('<i', data, 19)[0] != 0xAC1640:
            raise ValueError('Independent deleting thunk release mismatch')
        if data[23:] != bytes.fromhex('83c4048bc65e5dc20400'):
            raise ValueError('Independent deleting thunk tail mismatch')
        return dict(Entry=entry, UnexpandedBody=body, FlagsMask=1, ReleaseAfterBody=True)

    thunks = [thunk(read, 0x6295D0, 0x629600), thunk(read, 0x6386D0, 0x638700),
              thunk(read, 0x6E4A80, 0x6E4AB0)]
    positive = api['audit'](args.exe, args.packages, repo)
    if positive['PrimaryLifecycleBodies'] != 2 or positive['ControllerQueueAllocationClosed'] or positive['QueueClassRtti'] != 'Unknown':
        raise AssertionError('Bounded lifecycle evidence overstated')
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
    reject('wrong executable before package import', lambda: api['audit'](wrong, output / 'absent', repo))
    reject('missing executable', lambda: api['audit'](output / 'missing', args.packages, repo))
    anchors = api['audit'].__globals__['ANCHORS']
    former = anchors[0]
    anchors[0] = (former[0], 'nop', '')
    try:
        reject('wrong anchor expectation', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        anchors[0] = former
    reject('truncated mapped helper', lambda: guard['exact'](lambda va, n: b'\0' * (n - 1), 0x1000, 4))
    reject('empty mapped helper', lambda: guard['exact'](lambda va, n: b'', 0x1000, 1))
    reject('unmapped independent span', lambda: read(0xffffffff, 8))
    reject('existing output file', lambda: guard['fresh_private_path'](wrong, repo))
    reject('existing output directory', lambda: guard['fresh_private_path'](output, repo, directory=True))
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r55.json', repo))
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
    reject('xref budget21', lambda: api['capped'](range(21)))
    if len(api['capped'](range(20))) != 20:
        raise AssertionError('Budget boundary20 rejected')
    rtti = api['audit'].__globals__['RTTI']
    former = rtti[0]
    rtti[0] = (former[0], former[1], former[2], b'synthetic-wrong-rtti')
    try:
        reject('wrong RTTI expectation', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        rtti[0] = former
    spans = api['audit'].__globals__['SPANS']
    former = spans[0]
    spans[0] = (former[0], former[1], '0' * 64, former[3])
    try:
        reject('wrong bounded-span expectation', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        spans[0] = former
    def altered(va, replacement):
        return lambda at, n: replacement if at == va else read(at, n)

    reject('truncated carrier', lambda: carrier(altered(0x6385D9, b''), 0x6385D9, '8b450853568bf1508975f0', 0x6E4A20))
    bad = bytearray(read(0x6385D9, 16)); bad[6] = 0xF9
    reject('wrong receiver carrier', lambda: carrier(altered(0x6385D9, bytes(bad)), 0x6385D9, '8b450853568bf1508975f0', 0x6E4A20))
    bad = bytearray(read(0x6295D0, 33)); bad[14] = 2
    reject('wrong deleting flags mask', lambda: thunk(altered(0x6295D0, bytes(bad)), 0x6295D0, 0x629600))
    edges = api['audit'].__globals__['EDGES']; former = edges[0]; edges[0] = (former[0], 0, former[2])
    try:
        reject('wrong lifecycle edge', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        edges[0] = former
    if (repo / 'docs/forbidden-r55.json').exists() or wrong.read_bytes() != b'synthetic identity failure':
        raise AssertionError('Rejected output changed fixture/public state')
    nested = guard['fresh_private_path'](output / 'nested/new-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent":true}\n')
    result = dict(Positive=positive, IndependentBytes=len(witnesses), IndependentCalls=len(calls),
                  IndependentRttiStrings=3, IndependentDataPointers=3, IndependentCarriers=carriers, IndependentDeletingThunks=thunks, RejectedOutputEffectsChecked=True, NegativeChecks=negatives, SyntheticHelpersOnly=True,
                  OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS {len(witnesses)} bytes/{len(calls)} calls/three RTTI strings/three virtual slots/two carriers/three deleting thunks; {len(negatives)} rejection checks')


if __name__ == '__main__':
    main()
