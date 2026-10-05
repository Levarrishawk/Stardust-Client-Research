"""Independent small static byte/call witnesses for selected ground normalized input edge."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_player_controller_input_queue_binding.py'))
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

    witnesses = [(0x519C78, '895e4c'), (0x519E7F, 'c70688d95d01'),
                 (0x51A004, '89580c'), (0x51A013, '895e4c'),
                 (0x51AA7B, '8b764c'), (0x51AABE, '8bce'),
                 (0xAAE2E9, '83c2fb'), (0xAAE323, '3b5110'),
                 (0xAAE330, '6a01'), (0x62A9B0, '8b4104c3')]
    for va, expected in witnesses:
        literal = bytes.fromhex(expected)
        if read(va, len(literal)) != literal:
            raise ValueError('Independent byte witness mismatch')
    calls = [(0x51AF36, 0x51AA40), (0x51AAC0, 0xAAE2E0),
             (0xAAE332, 0xAAE150), (0x51A00B, 0x6E2350), (0x6E1FC5, 0xAACD50)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent relative-call mismatch')
    for va, text in [(0x1868F14, b'.?AVGroundScene@@\0'),
                     (0x186F48C, b'.?AVPlayerCreatureController@@\0')]:
        if read(va, len(text)) != text:
            raise ValueError('Independent RTTI string mismatch')
    for va, value in [(0xAAE448, 0xAAE320), (0x15DD98C, 0x51AF10), (0x15DD984, 0x160CAB4)]:
        if int.from_bytes(read(va, 4), 'little') != value:
            raise ValueError('Independent dispatch/RTTI data mismatch')
    positive = api['audit'](args.exe, args.packages, repo)
    if positive['ActiveAssignmentClosed'] or positive['ControllerQueueInstanceIdentity'] != 'Unknown':
        raise AssertionError('Partial identity evidence was overstated')
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r53.json', repo))
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
    nested = guard['fresh_private_path'](output / 'nested/new-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent":true}\n')
    result = dict(Positive=positive, IndependentBytes=len(witnesses), IndependentCalls=len(calls),
                  IndependentRttiStrings=2, IndependentDataPointers=3, NegativeChecks=negatives, SyntheticHelpersOnly=True,
                  OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS ten independent bytes/five calls/two RTTI strings/three data pointers; {len(negatives)} rejection checks')


if __name__ == '__main__':
    main()
