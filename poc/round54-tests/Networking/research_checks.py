"""Independent small static byte/call witnesses for one ground map assignment lifecycle."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_ground_scene_input_map_queue_assignment.py'))
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

    witnesses = [(0x519F66, '8bce'), (0x519218, '6858010000'),
                 (0x51923D, '89450c'), (0x519259, '897e1c'),
                 (0x6295B8, 'c7034c035e01'), (0xB22BCB, '897e2c'),
                 (0x51FADA, 'c7400c00000000'), (0x51FAED, 'c7464c00000000'),
                 (0x51FAFC, '89464c'), (0x51FB0A, '89420c'),
                 (0x6E204C, '8bc6'), (0x6E20D6, '33c0'),
                 (0xB3D340, '8b4104c3'), (0x62A9B0, '8b4104c3')]
    for va, expected in witnesses:
        literal = bytes.fromhex(expected)
        if read(va, len(literal)) != literal:
            raise ValueError('Independent byte witness mismatch')
    calls = [(0x519F68, 0x518EB0), (0x519238, 0x629400),
             (0x519252, 0xB22BB0), (0x519616, 0x51FAD0),
             (0x51FAE5, 0x6E2350), (0x51FAF4, 0x6E1E70),
             (0x51FB02, 0xB3D340)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent relative-call mismatch')
    for va, text in [(0x1868F14, b'.?AVGroundScene@@\0'),
                     (0x186F48C, b'.?AVPlayerCreatureController@@\0')]:
        if read(va, len(text)) != text:
            raise ValueError('Independent RTTI string mismatch')
    for va, value in [(0x15DD984, 0x160CAB4), (0x15E0348, 0x16113A0)]:
        if int.from_bytes(read(va, 4), 'little') != value:
            raise ValueError('Independent dispatch/RTTI data mismatch')
    def instance_expression(get):
        # Independently parse only the seven-instruction assignment window.
        block = get(0x51FAF4, 25)
        for offset, opcode in [(0, b'\xe8'), (5, b'\x8b\x4e'), (8, b'\x89\x46'),
                               (11, b'\x8b\x49'), (14, b'\xe8'),
                               (19, b'\x8b\x56'), (22, b'\x89\x42')]:
            if block[offset:offset + len(opcode)] != opcode:
                raise ValueError('Independent instance opcode mismatch')
        if 0x51FAF4 + 5 + struct.unpack_from('<i', block, 1)[0] != 0x6E1E70:
            raise ValueError('Independent instance acquisition call mismatch')
        if 0x51FAF4 + 19 + struct.unpack_from('<i', block, 15)[0] != 0xB3D340:
            raise ValueError('Independent instance getter call mismatch')
        getter = get(0xB3D340, 4)
        if getter[:2] != b'\x8b\x41' or getter[3:] != b'\xc3' or getter != get(0x62A9B0, 4):
            raise ValueError('Independent instance getter mismatch')
        fields = (block[10], block[24], block[7], block[13], getter[2])
        if fields != (0x4C, 0x0C, 0x1C, 0x2C, 0x04) or block[21] != fields[0]:
            raise ValueError('Independent instance offsets mismatch')
        return 'scene[0x%02X][0x%02X] = scene[0x%02X][0x%02X][0x%02X]' % fields

    expression = instance_expression(read)
    positive = api['audit'](args.exe, args.packages, repo)
    if not positive['MapAssignmentClosed'] or expression != positive['QueueInstanceExpression']:
        raise AssertionError('Independent instance expression disagrees')
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r54.json', repo))
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

    block = bytearray(read(0x51FAF4, 25)); block[10] = 0x50
    reject('wrong selected map instance offset', lambda: instance_expression(altered(0x51FAF4, bytes(block))))
    reject('wrong controller queue field', lambda: instance_expression(altered(0xB3D340, b'\x8b\x41\x08\xc3')))
    reject('wrong player getter agreement', lambda: instance_expression(altered(0x62A9B0, b'\x8b\x41\x08\xc3')))
    with patch.dict(api['audit'].__globals__, candidates=lambda raw, pe: []):
        reject('missing acquisition candidate', lambda: api['audit'](args.exe, args.packages, repo))
    nested = guard['fresh_private_path'](output / 'nested/new-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent":true}\n')
    result = dict(Positive=positive, IndependentBytes=len(witnesses), IndependentCalls=len(calls),
                  IndependentRttiStrings=2, IndependentDataPointers=2, IndependentInstanceExpression=expression, RejectedOutputsCreated=False, NegativeChecks=negatives, SyntheticHelpersOnly=True,
                  OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS {len(witnesses)} independent bytes/{len(calls)} calls/two RTTI strings/two data pointers/instance expression; {len(negatives)} rejection checks')


if __name__ == '__main__':
    main()
