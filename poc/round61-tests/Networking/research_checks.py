"""Independent small static byte/call witnesses for queue payload caller byte ordering and bounded rejection gates."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_queue_payload_caller.py'))
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
        (11784787, '8b4514'),
        (11784790, '8b550c'),
        (11784794, '50'),
        (11784795, '8b4508'),
        (11784798, '8bf1'),
        (11784800, '8b4d10'),
        (11784803, '51'),
        (11784804, '8b4e04'),
        (11784807, '52'),
        (11784808, '50'),
        (11784809, 'e81292f6ff'),
        (11784814, '8b4e08'),
        (11784817, '85c9'),
        (11784819, '7411'),
        (11784821, '8b11'),
        (11784823, 'ff5204'),
        (11784826, '84c0'),
        (11784828, '7408'),
        (11784830, '8b4e08'),
        (11784833, '8b01'),
        (11784835, 'ff503c'),
        (11784840, 'c21000'),
    ]
    def witnesses_check(get):
        for va, expected in witnesses:
            literal = bytes.fromhex(expected)
            if get(va, len(literal)) != literal:
                raise ValueError('Independent byte witness mismatch')
    witnesses_check(read)
    calls=[(0xAA6651,0xAA6480),(0xAB3538,0xAA6480),(0xB3D269,0xAA6480)]
    for va,target in calls:
        ins=read(va,5)
        if ins[0]!=0xe8 or va+5+struct.unpack('<i',ins[1:])[0]!=target:
            raise ValueError('Independent exact-target relative call mismatch')
    for va,target in [(0xB3D273,0xB3D286),(0xB3D27C,0xB3D286)]:
        ins=read(va,2)
        if ins[0]!=0x74 or va+2+struct.unpack('b',ins[1:])[0]!=target:
            raise ValueError('Independent post-call branch mismatch')
    def forwarding(get):
        patterns=[(0xB3D253,'8b4514'),(0xB3D256,'8b550c'),(0xB3D25A,'50'),
                  (0xB3D25B,'8b4508'),(0xB3D25E,'8bf1'),(0xB3D260,'8b4d10'),
                  (0xB3D263,'51'),(0xB3D264,'8b4e04'),(0xB3D267,'52'),
                  (0xB3D268,'50'),(0xB3D288,'c21000')]
        for va,literal in patterns:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:raise ValueError('Independent stack/receiver identity mismatch')
        # Authored symbolic stack labels, not an instruction emulator or original execution.
        pushed=['arg4','arg3','arg2','arg1']
        return dict(PushOrder=pushed,CalleeArguments=list(reversed(pushed)),
                    ThirdArgumentUnchanged=True,ReceiverMemberOffset=4,ReturnStackBytes=16)
    forward=forwarding(read)
    def callbacks(get):
        patterns=[(0xB3D26E,'8b4e08'),(0xB3D273,'7411'),(0xB3D277,'ff5204'),
                  (0xB3D27A,'84c0'),(0xB3D27C,'7408'),(0xB3D27E,'8b4e08'),
                  (0xB3D283,'ff503c')]
        for va,literal in patterns:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:raise ValueError('Independent callback-source/test/slot mismatch')
        return dict(SourceMember=8,FirstSlot=4,SecondSlot=60,
                    FirstNullGuard=True,SecondSourceReread=True,SecondNullGuard=False,TestWidthBits=8)
    callback=callbacks(read)
    positive=api['audit'](args.exe,args.packages,repo)
    assert positive['InstructionCounts']==[27] and positive['TinyDirectWrappers']==0
    assert positive['DistinctTotalLeadCandidates']==6 and positive['RawXrefCount']==3
    assert not positive['PayloadVtableEstablished'] and not positive['PayloadConstructionObserved']
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r61.json', repo))
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
    for va,replacement,label in [(0xB3D260,bytes.fromhex('8b4d0c'),'wrong payload argument slot'),
        (0xB3D263,bytes.fromhex('52'),'wrong payload push register'),
        (0xB3D264,bytes.fromhex('8b4e08'),'wrong queue receiver member'),
        (0xB3D25E,bytes.fromhex('8bf2'),'wrong saved receiver register'),
        (0xB3D288,bytes.fromhex('c20c00'),'wrong return stack adjustment')]:
        reject(label,lambda va=va,replacement=replacement:forwarding(
            lambda at,n:replacement if at==va else read(at,n)))
    for va,replacement,label in [(0xB3D26E,bytes.fromhex('8b4e04'),'wrong callback object member'),
        (0xB3D27A,bytes.fromhex('85c0'),'wrong callback return test width'),
        (0xB3D283,bytes.fromhex('ff5004'),'wrong second virtual slot'),
        (0xB3D27C,bytes.fromhex('7508'),'wrong predicate branch')]:
        reject(label,lambda va=va,replacement=replacement:callbacks(
            lambda at,n:replacement if at==va else read(at,n)))
    expected=api['audit'].__globals__['EXPECTED_PRODUCER_XREFS'];former=expected[2];expected[2]=0
    try:reject('wrong exact-target xref inventory',lambda:api['audit'](args.exe,args.packages,repo))
    finally:expected[2]=former
    reject('missing direct/indirect call inventory',lambda:api['candidates']([]))
    # A separate authored synthetic PE surface exercises the raw-site ceiling.
    def fake_pe(site_count):
        data=b''.join(b'\xe8'+struct.pack('<i',0xAA6480-(0x400000+i*5+5)) for i in range(site_count))
        section=SimpleNamespace(Characteristics=0x20000000,VirtualAddress=0,get_data=lambda:data)
        return SimpleNamespace(OPTIONAL_HEADER=SimpleNamespace(ImageBase=0x400000),sections=[section])
    reject('raw exact-target site budget13',lambda:api['producer_xrefs'](fake_pe(13)))
    former_sites=list(expected)
    expected[:]=[0x400000+i*5 for i in range(12)]
    try:
        assert api['producer_xrefs'](fake_pe(12))==expected
    finally:
        expected[:]=former_sites
    # Fixture range rejection belongs to this authored oracle, not the legacy caller.
    def forwarded_words(words):
        if len(words)!=4 or any(type(x)!=int or not 0<=x<=0xffffffff for x in words):
            raise ValueError('Synthetic DWORD input policy')
        return list(reversed(list(reversed(words))))
    vectors=[]
    for words in [(0,0,0,0),(1,0,0,0),(1,0x3f800000,0x12345678,0x83),
                  (0xffffffff,0x80000000,0xffffffff,0xffffffff)]:
        got=forwarded_words(words);assert got==list(words);vectors.append(got)
    predicate_cases=[]
    for object_present,raw_return,expected_second in [(False,0,False),(True,0,False),
        (True,1,True),(True,0x100,False),(True,0xffffffff,True),(True,0xffffff00,False)]:
        second=object_present and bool(raw_return&0xff);assert second==expected_second
        predicate_cases.append(dict(ObjectPresent=object_present,RawReturn=raw_return,SecondCall=second))
    reject('synthetic wrong argument count',lambda:forwarded_words((1,2,3)))
    reject('synthetic oversized DWORD',lambda:forwarded_words((1,2,0x100000000,4)))
    if (repo/'docs/forbidden-r61.json').exists() or wrong.read_bytes()!=b'synthetic identity failure':
        raise AssertionError('Rejected output mutated fixture/public state')
    nested=guard['fresh_private_path'](output/'nested/new-parent/check.json',repo)
    with nested.open('x',encoding='utf-8',newline='\n') as stream:stream.write('{"fresh_parent":true}\n')
    result=dict(Positive=positive,IndependentBytes=len(witnesses),IndependentCalls=len(calls),
                IndependentBranches=2,Forwarding=forward,CallbackSources=callback,
                SyntheticWordVectors=vectors,SyntheticLowByteCases=predicate_cases,
                NegativeChecks=negatives,PayloadConstruction='Unknown at incoming arg3 forwarding boundary',
                Ownership='retained/transfer/unique/duplicate policy Unknown',
                Cancellation='no explicit local check; delegates Unknown',
                SyntheticPolicy='authored field/test vectors only; no original code/pointer execution',
                OriginalExecuted=False,LiveContact=False)
    with (output/'checks.json').open('x',encoding='utf-8',newline='\n') as stream:stream.write(json.dumps(result,indent=2)+'\n')
    print(f'PASS {len(witnesses)} bytes/three exact calls/two branches/forwarding identity; {len(negatives)} negatives')


if __name__=='__main__':
    main()
