"""Independent small static byte/call witnesses for selected NetworkController delegated queue boundaries."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_queue_storage.py'))
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

    witnesses = [(0xB4AE53, '8b4508'), (0xB4AE57, '50'), (0xB4AE58, '8bf1'),
                 (0xB4AE5F, 'c706f0c85e01'), (0xB4AE65, '8bc6'),
                 (0xB4AEA0, 'c701f0c85e01'), (0xB4AE7B, 'f6450801'),
                 (0x62A9B0, '8b4104c3'), (0xB3D10A, '6a14'), (0xB3D124, '6a10'),
                 (0xB3D12F, '33c0'), (0xB3D134, '894604'), (0xB3D194, '8b7e04'),
                 (0xB3D1A1, '8bcf'), (0xB3D1A8, '57'), (0xB3D1B2, 'c7460800000000'),
                 (0xB3D13D, 'c706b4c55e01')]
    for va, expected in witnesses:
        literal = bytes.fromhex(expected)
        if read(va, len(literal)) != literal:
            raise ValueError('Independent byte witness mismatch')
    calls = [(0x6E4A43, 0xB4AE50), (0xB4AE5A, 0xB3D0F0),
             (0xB4AE76, 0xB4AEA0), (0xB4AE82, 0xAC1640), (0xB3D10E, 0xAC15C0), (0xB3D128, 0xAA6050),
             (0xB3D1A3, 0xAA6310), (0xB3D1A9, 0xAC1640),
             (0xB3D166, 0xB3D190), (0xB3D172, 0xAC1640)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent relative-call mismatch')
    name = b'.?AVNetworkController@@\0'
    if read(0x186D094, len(name)) != name:
        raise ValueError('Independent controller RTTI mismatch')
    base_name = b'.?AVController@@\0'
    if read(0x186AE80, len(base_name)) != base_name:
        raise ValueError('Independent base RTTI mismatch')
    for va, value in [(0x15EC8F0, 0xB4AE70), (0x15EC8EC, 0x1625554),
                      (0x15EC5B4, 0xB3D160), (0x15EC5B0, 0x16250F8)]:
        if int.from_bytes(read(va, 4), 'little') != value:
            raise ValueError('Independent controller slot/COL mismatch')

    def carrier(get):
        data = get(0xB4AE53, 12)
        if len(data) != 12 or data[:7] != bytes.fromhex('8b450856508bf1') or data[7] != 0xE8:
            raise ValueError('Independent constructor carrier mismatch')
        target = 0xB4AE53 + 12 + struct.unpack_from('<i', data, 8)[0]
        if target != 0xB3D0F0:
            raise ValueError('Independent constructor base mismatch')
        return dict(Receiver='incoming ECX unchanged', ObjectArgument='caller stack+8', UnexpandedBase=target)

    def destruction(get):
        data = get(0xB4AEA0, 11)
        if len(data) != 11 or data[:6] != bytes.fromhex('c701f0c85e01') or data[6] != 0xE9:
            raise ValueError('Independent destruction tail shape mismatch')
        target = 0xB4AEA0 + 11 + struct.unpack_from('<i', data, 7)[0]
        if target != 0xB3D190:
            raise ValueError('Independent destruction base mismatch')
        return dict(Receiver='incoming ECX unchanged', VtableRestored=0x15EC8F0, UnexpandedBase=target)

    def deleting_thunk(get, entry=0xB4AE70, body=0xB4AEA0):
        data = get(entry, 33)
        if len(data) != 33 or data[:6] != bytes.fromhex('558bec568bf1') or data[6] != 0xE8:
            raise ValueError('Independent deleting thunk prefix mismatch')
        if entry + 11 + struct.unpack_from('<i', data, 7)[0] != body:
            raise ValueError('Independent deleting thunk body mismatch')
        if data[11:18] != bytes.fromhex('f6450801740956') or data[18] != 0xE8:
            raise ValueError('Independent deleting thunk flags/branch mismatch')
        if entry + 23 + struct.unpack_from('<i', data, 19)[0] != 0xAC1640 or data[23:] != bytes.fromhex('83c4048bc65e5dc20400'):
            raise ValueError('Independent deleting thunk release/tail mismatch')
        return dict(BodyBeforeConditionalSelfRelease=True, FlagsMask=1)

    carrier_result = carrier(read); destruction_result = destruction(read); deleting_result = deleting_thunk(read)
    base_deleting_result = deleting_thunk(read, 0xB3D160, 0xB3D190)
    def queue_binding(get):
        data = get(0xB3D10A, 57)
        literals = [(0, '6a148bf1'), (9, '83c4048945f085c0c745fc00000000740b'),
                    (26, '6a108bc8'), (35, 'eb0233c08b4df48946048b4508894608c706b4c55e01')]
        if len(data) != 57:
            raise ValueError('Independent queue binding truncation')
        for offset, expected in literals:
            raw_literal = bytes.fromhex(expected)
            if data[offset:offset + len(raw_literal)] != raw_literal:
                raise ValueError('Independent queue binding branch/store mismatch')
        for offset, expected in [(4, 0xAC15C0), (30, 0xAA6050)]:
            if data[offset] != 0xE8 or 0xB3D10A + offset + 5 + struct.unpack_from('<i', data, offset + 1)[0] != expected:
                raise ValueError('Independent queue binding call mismatch')
        return dict(AllocationArgument=data[1], ConstructorArgument=data[27], StoredField=data[44],
                    NullAllocationStoresZero=True, StoredValue='constructor return, not assumed allocation identity')

    def queue_cleanup(get):
        data = get(0xB3D190, 43)
        literals = [(0, '568bf1578b7e0485ffc706b4c55e0174108bcf'),
                    (24, '57'), (30, '83c4045fc74608000000005ec3')]
        if len(data) != 43:
            raise ValueError('Independent queue cleanup truncation')
        for offset, expected in literals:
            raw_literal = bytes.fromhex(expected)
            if data[offset:offset + len(raw_literal)] != raw_literal:
                raise ValueError('Independent queue cleanup branch/field mismatch')
        for offset, expected in [(19, 0xAA6310), (25, 0xAC1640)]:
            if data[offset] != 0xE8 or 0xB3D190 + offset + 5 + struct.unpack_from('<i', data, offset + 1)[0] != expected:
                raise ValueError('Independent queue cleanup call mismatch')
        return dict(LoadedField=data[6], SkipNull=True, HelperBeforeRelease=True,
                    ClearedObjectField=data[36], QueueFieldCleared=False)

    binding_result = queue_binding(read); cleanup_result = queue_cleanup(read)
    if binding_result['StoredField'] != cleanup_result['LoadedField'] or binding_result['StoredField'] != 4:
        raise AssertionError('Independent queue instance field disagrees')
    positive = api['audit'](args.exe, args.packages, repo)
    if positive['PrimaryLifecycleBodies'] != 4 or positive['ActualClasses'] != ['NetworkController','Controller'] or not positive['DirectQueueWriterClosed'] or positive['QueueClassRtti'] != 'Unknown':
        raise AssertionError('Bounded queue storage evidence overstated')
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r56.json', repo))
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

    reject('truncated constructor carrier', lambda: carrier(altered(0xB4AE53, b'')))
    bad = bytearray(read(0xB4AEA0, 11)); bad[-1] ^= 1
    reject('wrong destruction tail target', lambda: destruction(altered(0xB4AEA0, bytes(bad))))
    bad = bytearray(read(0xB4AE70, 33)); bad[14] = 2
    reject('wrong deleting flags mask', lambda: deleting_thunk(altered(0xB4AE70, bytes(bad))))
    edges = api['audit'].__globals__['EDGES']; former = edges[0]; edges[0] = (former[0], 0, former[2])
    try:
        reject('wrong lifecycle edge', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        edges[0] = former
    if (repo / 'docs/forbidden-r56.json').exists() or wrong.read_bytes() != b'synthetic identity failure':
        raise AssertionError('Rejected output changed fixture/public state')
    reject('truncated queue binding', lambda: queue_binding(altered(0xB3D10A, b'')))
    bad = bytearray(read(0xB3D10A, 57)); bad[44] = 8
    reject('wrong queue writer field', lambda: queue_binding(altered(0xB3D10A, bytes(bad))))
    bad = bytearray(read(0xB3D190, 43)); bad[6] = 8
    reject('wrong queue cleanup field', lambda: queue_cleanup(altered(0xB3D190, bytes(bad))))
    bad = bytearray(read(0xB3D190, 43)); bad[16] = 0
    reject('wrong cleanup null branch', lambda: queue_cleanup(altered(0xB3D190, bytes(bad))))
    nested = guard['fresh_private_path'](output / 'nested/new-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent":true}\n')
    result = dict(Positive=positive, IndependentBytes=len(witnesses), IndependentCalls=len(calls),
                  IndependentRttiStrings=2, IndependentDataPointers=4, IndependentQueueBinding=binding_result, IndependentQueueCleanup=cleanup_result, IndependentConstructorCarrier=carrier_result, IndependentDestructionTail=destruction_result, IndependentDeletingThunk=deleting_result, IndependentBaseDeletingThunk=base_deleting_result, RejectedOutputEffectsChecked=True, NegativeChecks=negatives, SyntheticHelpersOnly=True,
                  OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS {len(witnesses)} bytes/{len(calls)} calls/two RTTI/four data pointers/constructor and queue carrier/destruction and queue cleanup/deleting thunk; {len(negatives)} rejection checks')


if __name__ == '__main__':
    main()
