"""Independent small static byte/call witnesses for queue receiver identity, five fields and reserve/cleanup boundaries."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_queue_initialization.py'))
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

    witnesses = [(0xAA606E, '8bf9'), (0xAA6074, '897de8'), (0xAA62F5, '8b45e8'),
                 (0xAA60B8, '8937'), (0xAA60D4, '894704'), (0xAA60D7, '89470c'),
                 (0xAA60DD, '897708'), (0xAA60E0, '895f10'), (0xAA60FC, 'c1f904'),
                 (0xAA620B, 'c1f904'), (0xAA61A3, 'c1e004'), (0xAA62B2, 'c1e004'),
                 (0xAA61A6, '3d80000000'), (0xAA62B5, '3d80000000'),
                 (0xAA6312, '8bf1'), (0xAA6314, '8b06'), (0xAA631E, '8b4e04'),
                 (0xAA6324, '8bce'), (0xAA632B, '8b3e'), (0xAA6341, '8b7e04'),
                 (0xAA6359, 'c7460800000000'), (0xAA6360, 'c7460c00000000'),
                 (0xAA6367, 'c7461000000000')]
    for va, expected in witnesses:
        literal = bytes.fromhex(expected)
        if read(va, len(literal)) != literal:
            raise ValueError('Independent byte witness mismatch')
    calls = [(0xAA607A, 0xAC15C0), (0xAA60A6, 0xAA67C0), (0xAA60BA, 0xAC15C0),
             (0xAA61AE, 0xAC15C0), (0xAA61B8, 0x12EA770),
             (0xAA62BD, 0xAC15C0), (0xAA62C7, 0x12EA770),
             (0xAA6319, 0xAA6370), (0xAA6326, 0xAA6370),
             (0xAA6333, 0xAA6710), (0xAA6339, 0xAC1640),
             (0xAA634A, 0xAA6710), (0xAA6350, 0xAC1640)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent relative-call mismatch')
    def identity(get):
        incoming = get(0xAA606E, 2); saved = get(0xAA6074, 3); returned = get(0xAA62F5, 3)
        if incoming != b'\x8b\xf9' or saved[:2] != b'\x89\x7d' or returned[:2] != b'\x8b\x45' or len(saved) != 3 or len(returned) != 3 or saved[2] != returned[2]:
            raise ValueError('Independent receiver return identity mismatch')
        return dict(ReturnsIncomingReceiver=True, StackSlot=struct.unpack('b', saved[2:])[0])

    def fields(get):
        for va, literal in [(0xAA60B8, '8937'), (0xAA60D4, '894704'), (0xAA60D7, '89470c'),
                            (0xAA60DD, '897708'), (0xAA60E0, '895f10')]:
            expected = bytes.fromhex(literal)
            if get(va, len(expected)) != expected:
                raise ValueError('Independent field/alias mismatch')
        return dict(Offsets=[0,4,8,12,16], Widths=[4]*5, Aliases=[(8,0),(12,4)])

    def short_branch(get, va, opcode, target):
        data = get(va, 2)
        if len(data) != 2 or data[0] != opcode or va + 2 + struct.unpack('b', data[1:])[0] != target:
            raise ValueError('Independent selected branch mismatch')

    def arithmetic(get):
        # Local instruction arithmetic only, not a queue or allocator emulation.
        for va, literal in [(0xAA60DA, '395d08'), (0xAA60E3, '895de4'), (0xAA60E6, '8d45e4'),
                            (0xAA60EB, '8d4508'), (0xAA60EE, '8b18'),
                            (0xAA61EB, 'c745dc00000000'), (0xAA61F2, '8d45dc'),
                            (0xAA61F7, '8d4508'), (0xAA61FD, '8b18')]:
            expected = bytes.fromhex(literal)
            if get(va, len(expected)) != expected:
                raise ValueError('Independent signed demand mismatch')
        short_branch(get, 0xAA60E9, 0x7C, 0xAA60EE); short_branch(get, 0xAA61F5, 0x7C, 0xAA61FA)
        short_branch(get, 0xAA61AC, 0x76, 0xAA61B8); short_branch(get, 0xAA62BB, 0x76, 0xAA62C7)
        rows = []
        for value, demand, byte_count, helper in [(-2147483648,0,0,'growth skipped'), (-1,0,0,'growth skipped'),
                                                (0,0,0,'growth skipped'), (8,8,128,'small'),
                                                (9,9,144,'large'), (16,16,256,'large'),
                                                (268435456,268435456,0,'small'), (2147483647,2147483647,4294967280,'large')]:
            n = max(value, 0); size = (n << 4) & 0xFFFFFFFF
            route = 'growth skipped' if n == 0 else ('small' if size <= 128 else 'large')
            if (n,size,route) != (demand,byte_count,helper):
                raise AssertionError('Independent local arithmetic mismatch')
            rows.append(dict(Input=value, Demand=n, WrappedByteRequest=size, RouteIfGrowthNeeded=route))
        return rows

    def cleanup(get):
        for va, literal in [(0xAA6314,'8b06'),(0xAA6316,'6a01'),(0xAA6318,'50'),
                            (0xAA631E,'8b4e04'),(0xAA6321,'6a01'),(0xAA6323,'51'),(0xAA6324,'8bce'),
                            (0xAA632B,'8b3e'),(0xAA6341,'8b7e04'),
                            (0xAA6359,'c7460800000000'),(0xAA6360,'c7460c00000000'),(0xAA6367,'c7461000000000')]:
            expected=bytes.fromhex(literal)
            if get(va,len(expected)) != expected:
                raise ValueError('Independent cleanup fields/flag mismatch')
        short_branch(get,0xAA632F,0x74,0xAA6341);short_branch(get,0xAA6346,0x74,0xAA6358)
        return dict(RawRecordOperationFlag=1, CurrentRecordOrder=[0,4], ClearedFields=[8,12,16],
                    EntryOwnershipAndCancelMeaning='Unknown')

    identity_result=identity(read);field_result=fields(read);arithmetic_cases=arithmetic(read);cleanup_result=cleanup(read)
    positive = api['audit'](args.exe, args.packages, repo)
    if positive['PrimaryLifecycleBodies'] != 2 or positive['DistinctCalleeCandidates'] != 10 or positive['IntraBodyDirectCallSites'] != 23 or positive['ConcreteQueueClassRtti'].split(';')[0] != 'Unknown':
        raise AssertionError('Bounded initializer evidence overstated')
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r57.json', repo))
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
    reject('callee lead budget21', lambda: api['capped'](range(21)))
    if len(api['capped'](range(20))) != 20:
        raise AssertionError('Budget boundary20 rejected')
    spans = api['audit'].__globals__['SPANS']
    former = spans[0]
    spans[0] = (former[0], former[1], '0' * 64, former[3])
    try:
        reject('wrong bounded-span expectation', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        spans[0] = former
    def altered(va, replacement):
        return lambda at,n: replacement if at == va else read(at,n)

    reject('wrong returned receiver slot',lambda: identity(altered(0xAA62F5,b'\x8b\x45\xe4')))
    reject('truncated receiver save',lambda: identity(altered(0xAA6074,b'')))
    reject('wrong alias field',lambda: fields(altered(0xAA60DD,b'\x89\x77\x0c')))
    reject('wrong signed clamp branch',lambda: arithmetic(altered(0xAA60E9,b'\x7d\x03')))
    reject('wrong small allocator threshold branch',lambda: arithmetic(altered(0xAA61AC,b'\x77\x0a')))
    reject('wrong cleanup flag',lambda: cleanup(altered(0xAA6316,b'\x6a\x00')))
    reject('wrong cleanup alias clear',lambda: cleanup(altered(0xAA6359,bytes.fromhex('c7460400000000'))))
    targets=api['audit'].__globals__['EXPECTED_CALL_TARGETS'];former=targets[0];targets[0]=0
    try:
        reject('wrong callee lead inventory',lambda: api['audit'](args.exe,args.packages,repo))
    finally:
        targets[0]=former
    if (repo/'docs/forbidden-r57.json').exists() or wrong.read_bytes()!=b'synthetic identity failure':
        raise AssertionError('Rejected output mutated fixture/public state')
    nested = guard['fresh_private_path'](output / 'nested/new-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent":true}\n')
    result = dict(Positive=positive, IndependentBytes=len(witnesses), IndependentCalls=len(calls),
                  IndependentIdentity=identity_result, IndependentFields=field_result, LocalArithmeticCases=arithmetic_cases, IndependentCleanup=cleanup_result, RejectedOutputEffectsChecked=True, CancellationGate='Not applicable to synchronous static tool; queue cancellation meaning Unknown', NegativeChecks=negatives, SyntheticHelpersOnly=True,
                  OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS {len(witnesses)} bytes/{len(calls)} calls/receiver identity/five fields/eight local arithmetic cases/cleanup; {len(negatives)} rejection checks')


if __name__ == '__main__':
    main()
