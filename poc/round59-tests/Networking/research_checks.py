"""Independent small static byte/call witnesses for record storage cleanup byte ordering and bounded rejection gates."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_queue_record_storage_cleanup.py'))
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
        (11167509, '684ba44b01'),
        (11167529, '8bd1'),
        (11167531, '8b4204'),
        (11167534, '8b0a'),
        (11167536, '3bc8'),
        (11167538, '8955f0'),
        (11167541, '740c'),
        (11167543, 'e804f9ffff'),
        (11167548, '83c110'),
        (11167551, '3bc8'),
        (11167553, '75f4'),
        (11167555, '8b0a'),
        (11167557, '85c9'),
        (11167559, 'c745fc00000000'),
        (11167566, '7433'),
        (11167568, '8b4208'),
        (11167571, '2bc1'),
        (11167573, 'c1f804'),
        (11167576, 'c1e004'),
        (11167579, '3d80000000'),
        (11167584, '7617'),
        (11167586, '51'),
        (11167587, 'e8d8ae0100'),
        (11167592, '83c404'),
        (11167595, '8b4df4'),
        (11167598, '64890d00000000'),
        (11167608, 'c3'),
        (11167609, '50'),
        (11167610, '51'),
        (11167611, 'e8a0418400'),
        (11167616, '83c408'),
        (11167619, '8b4df4'),
        (11167622, '64890d00000000'),
        (11167632, 'c3'),
        (11165760, 'c7410800000000'),
        (11165767, 'c3'),
    ]
    def witnesses_check(get):
        for va, expected in witnesses:
            literal = bytes.fromhex(expected)
            if get(va, len(literal)) != literal:
                raise ValueError('Independent byte witness mismatch')
    witnesses_check(read)
    for va,literal in [(0xAA6331,'8bcf'),(0xAA6338,'57'),(0xAA6348,'8bcf'),(0xAA634F,'57')]:
        expected=bytes.fromhex(literal)
        if read(va,len(expected))!=expected:
            raise ValueError('Outer record receiver/release pointer mismatch')
    calls = [(0xAA6333,0xAA6710),(0xAA634A,0xAA6710),
             (0xAA6737,0xAA6040),(0xAA6763,0xAC1640),(0xAA677B,0x12EA920),
             (0xAA6339,0xAC1640),(0xAA6350,0xAC1640)]
    for va,target in calls:
        ins=read(va,5)
        if ins[0] != 0xe8 or va+5+struct.unpack('<i',ins[1:])[0] != target:
            raise ValueError('Independent relative call mismatch')
    # Raw branch displacements and register identities are decoded without Capstone.
    for va,target in [(0xAA6735,0xAA6743),(0xAA6741,0xAA6737),
                      (0xAA674E,0xAA6783),(0xAA6760,0xAA6779)]:
        ins=read(va,2)
        if va+2+struct.unpack('b',ins[1:])[0] != target:
            raise ValueError('Independent branch target mismatch')
    def identity(get):
        if get(0xAA6729,2) != bytes.fromhex('8bd1') or get(0xAA6732,3) != bytes.fromhex('8955f0'):
            raise ValueError('Record receiver identity mismatch')
        if get(0xAA6040,7) != bytes.fromhex('c7410800000000') or get(0xAA6047,1) != bytes.fromhex('c3'):
            raise ValueError('Entry-only wrapper mismatch')
    identity(read)
    def routing(get,start,limit):
        if get(0xAA6755,3) != bytes.fromhex('c1f804') or get(0xAA6758,3) != bytes.fromhex('c1e004') or get(0xAA6760,2)[0] != 0x76:
            raise ValueError('Storage arithmetic/comparison mismatch')
        rawdiff=(limit-start)&0xffffffff
        signed=rawdiff if rawdiff<0x80000000 else rawdiff-0x100000000
        size=((signed>>4)<<4)&0xffffffff
        return size, ('none' if start==0 else 'small' if size<=128 else 'large')
    arithmetic=[]
    for start,limit,expected in [(0,0,(0,'none')),(4096,4096,(0,'small')),
        (4096,4112,(16,'small')),(4096,4224,(128,'small')),
        (4096,4240,(144,'large')),(4096,4225,(128,'small')),
        (4096,4080,(0xfffffff0,'large')),(0xfffffff0,0,(16,'small'))]:
        actual=routing(read,start,limit);assert actual==expected
        arithmetic.append(dict(Start=start,Limit=limit,Bytes=actual[0],Route=actual[1]))
    positive=api['audit'](args.exe,args.packages,repo)
    assert positive['PrimaryInstructionCounts']==[45,2]
    assert positive['PrimaryLifecycleBodies']==1 and positive['TinyDirectWrappers']==1
    assert positive['IntraBodyDirectCallSites']==3 and positive['DistinctCalleeCandidates']==2
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r59.json', repo))
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
    reject('callee lead budget13', lambda: api['capped'](range(13)))
    if len(api['capped'](range(12))) != 12:
        raise AssertionError('Budget boundary12 rejected')
    spans = api['audit'].__globals__['SPANS']
    former = spans[0]
    spans[0] = (former[0], former[1], '0' * 64, former[3])
    try:
        reject('wrong bounded-span expectation', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        spans[0] = former
    for va,replacement,label in [(0xAA672B,bytes.fromhex('8b4208'),'wrong end field'),
        (0xAA6750,bytes.fromhex('8b4204'),'wrong limit field'),
        (0xAA673C,bytes.fromhex('83c108'),'wrong stride'),
        (0xAA6743,bytes.fromhex('8b4a04'),'wrong release pointer'),
        (0xAA6779,bytes.fromhex('51'),'wrong small size argument'),
        (0xAA6778,bytes.fromhex('c20800'),'wrong stack return')]:
        reject(label,lambda va=va,replacement=replacement:witnesses_check(
            lambda at,n:replacement if at==va else read(at,n)))
    reject('wrong receiver identity',lambda:identity(lambda at,n:bytes.fromhex('8bc1') if at==0xAA6729 else read(at,n)))
    reject('wrong wrapper field',lambda:identity(lambda at,n:bytes.fromhex('c7410400000000') if at==0xAA6040 else read(at,n)))
    reject('wrong signed shift',lambda:routing(lambda at,n:bytes.fromhex('c1e804') if at==0xAA6755 else read(at,n),4096,4224))
    reject('wrong unsigned threshold branch',lambda:routing(lambda at,n:bytes.fromhex('7e17') if at==0xAA6760 else read(at,n),4096,4224))
    reject('missing call inventory',lambda:api['candidates']([]))
    reject('wrong direct target',lambda:api['candidates']([SimpleNamespace(address=0xAA6737,mnemonic='call',op_str='0xaa6370')]))
    # Synthetic event oracle only: no original allocator, callback or exception handler.
    def events(start,end,limit):
        if end<start or (end-start)%16 or end-start>256:
            raise ValueError('Synthetic fixture range policy, not legacy rejection')
        clears=list(range(start+8,end+8,16));size,route=routing(read,start,limit)
        return dict(EntryClearAddresses=clears,Route=route,Bytes=size,
                    DescriptorBefore=[start,end,limit],DescriptorAfter=[start,end,limit])
    fixtures=[events(0,0,0),events(4096,4096,4224),events(4096,4128,4240)]
    assert fixtures[1]['Route']=='small' and fixtures[1]['EntryClearAddresses']==[]
    assert fixtures[2]['EntryClearAddresses']==[4104,4120]
    reject('synthetic misaligned entry range',lambda:events(4096,4097,4224))
    reject('synthetic excessive entry range',lambda:events(4096,8192,8192))
    if (repo/'docs/forbidden-r59.json').exists() or wrong.read_bytes()!=b'synthetic identity failure':
        raise AssertionError('Rejected output mutated fixture/public state')
    nested=guard['fresh_private_path'](output/'nested/new-parent/check.json',repo)
    with nested.open('x',encoding='utf-8',newline='\n') as stream:stream.write('{"fresh_parent":true}\n')
    result=dict(Positive=positive,IndependentBytes=len(witnesses),IndependentCalls=len(calls),
                IndependentBranches=4,IndependentCallerRegisterWitnesses=4,ArithmeticCases=arithmetic,SyntheticFixtures=fixtures,
                NegativeChecks=negatives,SyntheticPolicy='Bounded event oracle only; malformed range rejection is harness policy',
                Ownership='Unknown; descriptor/backing/payload effects separated',
                Cancellation='No cancel argument or check in these bodies; delegate policy Unknown',
                OriginalExecuted=False,LiveContact=False)
    with (output/'checks.json').open('x',encoding='utf-8',newline='\n') as stream:stream.write(json.dumps(result,indent=2)+'\n')
    print(f'PASS {len(witnesses)} bytes/seven calls/four branches/eight arithmetic cases; {len(negatives)} negatives')


if __name__=='__main__':
    main()
