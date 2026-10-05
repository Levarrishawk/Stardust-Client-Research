"""Independent small static byte/call witnesses for record helper byte ordering and bounded rejection gates."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_queue_record_cleanup.py'))
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

    witnesses = [
        (11167683, '8bc1'),
        (11167685, '8b4d0c'),
        (11167688, '8908'),
        (11167691, 'c20800'),
        (11166579, '8b4508'),
        (11166583, '8b30'),
        (11166586, '8b7804'),
        (11166589, '3bf7'),
        (11166591, '741b'),
        (11166593, '8b4e08'),
        (11166596, '85c9'),
        (11166598, '740d'),
        (11166600, '8b01'),
        (11166602, '6a01'),
        (11166604, 'ff10'),
        (11166606, 'c7460800000000'),
        (11166613, '83c610'),
        (11166616, '3bf7'),
        (11166618, '75e5'),
        (11166623, 'c20800'),
    ]
    def witnesses_check(get):
        for va, expected in witnesses:
            literal = bytes.fromhex(expected)
            if get(va, len(literal)) != literal:
                raise ValueError('Independent byte witness mismatch')
    witnesses_check(read)
    calls = [(0xAA60A6, 0xAA67C0), (0xAA6319, 0xAA6370), (0xAA6326, 0xAA6370)]
    for va, target in calls:
        instruction = read(va, 5)
        if instruction[0] != 0xe8 or va + 5 + struct.unpack('<i', instruction[1:])[0] != target:
            raise ValueError('Independent caller mismatch')
    positive = api['audit'](args.exe, args.packages, repo)
    assert positive['PrimaryInstructionCounts'] == [7, 23]
    assert positive['IntraBodyDirectCallSites'] == 0 and positive['IntraBodyIndirectCallSites'] == 1
    assert positive['CallerSecondArgumentIgnored'] and positive['InternalVirtualArgument'] == 1
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r58.json', repo))
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
    for va, replacement, label in [
        (0xAA67C5, bytes.fromhex('8b4d08'), 'wrong initializer argument'),
        (0xAA6381, bytes.fromhex('8b4e04'), 'wrong payload field'),
        (0xAA638A, bytes.fromhex('6a00'), 'wrong internal virtual argument'),
        (0xAA638C, bytes.fromhex('ff5004'), 'wrong virtual slot'),
        (0xAA638E, bytes.fromhex('c7460400000000'), 'wrong cleared field'),
        (0xAA6395, bytes.fromhex('83c608'), 'wrong entry stride'),
        (0xAA637F, bytes.fromhex('751b'), 'wrong empty branch')]:
        reject(label, lambda va=va, replacement=replacement: witnesses_check(
            lambda at,n: replacement if at == va else read(at,n)))
    # Synthetic event fixture: it models only the observed store-after-call cutpoint.
    # A scripted failure is not execution of the original callback or its exception ABI.
    def trace(payloads, caller_flag, fail_at=None):
        result=list(payloads); events=[]
        for i,payload in enumerate(result):
            if payload:
                events.append(['virtual',i,payload,1])
                if i == fail_at:
                    return result,events
                result[i]=0; events.append(['zero',i])
        return result,events
    cases=[]
    for flag in [0,1,255]:
        cleared,events=trace([7,0,9],flag)
        assert cleared == [0,0,0]
        assert events == [['virtual',0,7,1],['zero',0],['virtual',2,9,1],['zero',2]]
        cases.append(dict(CallerFlag=flag,Result=cleared,Events=events))
    assert trace([],1) == ([],[])
    partial,events=trace([7,9],1,1)
    assert partial == [0,9] and events[-1] == ['virtual',1,9,1]
    if (repo / 'docs/forbidden-r58.json').exists() or wrong.read_bytes() != b'synthetic identity failure':
        raise AssertionError('Rejected output mutated fixture/public state')
    nested = guard['fresh_private_path'](output / 'nested/new-parent/check.json', repo)
    with nested.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('{"fresh_parent":true}\n')
    result = dict(Positive=positive, IndependentBytes=len(witnesses), IndependentCallerCalls=len(calls),
                  NegativeChecks=negatives, SyntheticEventCases=cases,
                  SyntheticPartialCutpoint=partial, OriginalCallbackExecuted=False,
                  SyntheticFixturePolicy='Only observed event order; no native callback/ownership claim',
                  RejectedOutputEffectsChecked=True, OriginalExecuted=False, LiveContact=False)
    with (output / 'checks.json').open('x', encoding='utf-8', newline='\n') as stream:
        stream.write(json.dumps(result, indent=2) + '\n')
    print(f'PASS {len(witnesses)} independent byte witnesses/three caller calls; {len(negatives)} negative checks; synthetic cutpoints')


if __name__ == '__main__':
    main()
