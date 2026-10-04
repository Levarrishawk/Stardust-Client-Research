"""Independent byte witnesses and rejection checks for round 50 static research."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'output-dir']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    api = runpy.run_path(str(repo / 'tools/python/verify_creature_controller_speed_inputs.py'))
    output = api['fresh_private_path'](args.output_dir, repo, directory=True)
    raw = args.exe.read_bytes()
    expected = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
    if hashlib.sha256(raw).hexdigest().upper() != expected:
        raise ValueError('Independent executable identity rejection')
    # Independent PE file mapping: no pefile/capstone or producer anchor table.
    nt = struct.unpack_from('<I', raw, 0x3c)[0]
    if raw[nt:nt + 4] != b'PE\0\0':
        raise ValueError('PE signature')
    count = struct.unpack_from('<H', raw, nt + 6)[0]
    optional_size = struct.unpack_from('<H', raw, nt + 20)[0]
    base = struct.unpack_from('<I', raw, nt + 24 + 28)[0]
    sections = nt + 24 + optional_size

    def read(va, size):
        rva = va - base
        for index in range(count):
            at = sections + index * 40
            virtual, start, disk_size, disk = struct.unpack_from('<4I', raw, at + 8)
            if start <= rva and rva + size <= start + disk_size:
                result = raw[disk + rva - start:disk + rva - start + size]
                if len(result) == size:
                    return result
        raise ValueError('Independent unmapped/truncated span')

    byte_witnesses = [(0x62975A, '8bd9'), (0x62975C, '8b4308'),
                      (0x629894, '8bf8'), (0x629898, '8b7de0'),
                      (0x629F7A, '6a00'), (0x629F7C, '8bcf'),
                      (0x629F83, 'd95dec'), (0x629F86, '6a00'),
                      (0x629F88, '8bcf'), (0x629F8F, 'd95dcc'),
                      (0x62A05B, '895364'), (0x62A62D, 'c20400')]
    for va, text in byte_witnesses:
        expected_bytes = bytes.fromhex(text)
        if read(va, len(expected_bytes)) != expected_bytes:
            raise ValueError(f'Independent bytes mismatch {va:08X}')
    calls = [(0x62988F, 0x438650), (0x629F7E, 0x4388A0),
             (0x629F8A, 0x438950), (0x629FA6, 0x628DF0),
             (0x62A009, 0x431840), (0x62A03E, 0x431840),
             (0x62A39F, 0x7CA1F0), (0x62A3AA, 0x638810)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent direct-call mismatch')
    positive = api['audit'](args.exe, args.packages, args.server_root, repo)
    negatives = []

    def reject(label, action):
        try:
            action()
        except (ValueError, OSError, StopIteration):
            negatives.append(label)
        else:
            raise AssertionError('Accepted negative: ' + label)

    wrong = output / 'wrong-exe.bin'
    wrong.write_bytes(b'synthetic wrong executable')
    reject('wrong executable', lambda: api['audit'](wrong, output / 'absent-packages', args.server_root, repo))
    reject('missing executable', lambda: api['audit'](output / 'missing.exe', args.packages, args.server_root, repo))
    # Deliberately corrupt the assertion, never the proprietary input.
    anchors = api['audit'].__globals__['ANCHORS']
    former = anchors[0]
    anchors[0] = (former[0], 'nop', '')
    try:
        reject('wrong instruction expectation', lambda: api['audit'](args.exe, args.packages, args.server_root, repo))
    finally:
        anchors[0] = former
    reject('truncated mapped helper span', lambda: api['exact'](lambda va, size: b'\0' * (size - 1), 0x1000, 4))
    reject('empty mapped helper span', lambda: api['exact'](lambda va, size: b'', 0x1000, 1))
    reject('unmapped independent span', lambda: read(0xffffffff, 8))
    reject('existing output file', lambda: api['fresh_private_path'](wrong, repo))
    reject('existing output directory', lambda: api['fresh_private_path'](output, repo, directory=True))
    reject('public output', lambda: api['fresh_private_path'](repo / 'docs/round50-forbidden.json', repo))
    # Fresh nested parent is legal; outputs remain private and are created once.
    nested = api['fresh_private_path'](output / 'nested/missing-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent": true}\n')
    result = dict(Positive=positive, IndependentByteWitnesses=len(byte_witnesses),
                  IndependentRelativeCalls=len(calls), NegativeChecks=negatives,
                  SyntheticHelpersOnly=True, OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS independent {len(byte_witnesses)} byte witnesses + {len(calls)} calls; {len(negatives)} rejection checks')


if __name__ == '__main__':
    main()
