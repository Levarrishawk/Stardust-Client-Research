"""Independent small static byte/call witnesses for queue entry insertion byte ordering and bounded rejection gates."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_queue_entry_producer.py'))
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
        (11166876, '8b7508'),
        (11166881, '8bd1'),
        (11166883, '8955f0'),
        (11166886, '751e'),
        (11166915, 'c21000'),
        (11166919, '8d4de0'),
        (11166933, '8b4d10'),
        (11166936, '894de8'),
        (11166944, '8b720c'),
        (11166956, '3b7e08'),
        (11166969, '7421'),
        (11166983, '8b4de8'),
        (11166986, '894f08'),
        (11166995, '83460410'),
        (11167011, 'c1f804'),
        (11167014, '83f801'),
        (11167030, '7203'),
        (11167037, '03d8'),
        (11167046, 'c1e004'),
        (11167049, '3d80000000'),
        (11167055, '760d'),
        (11167122, '8b5a08'),
        (11167125, '895f08'),
        (11167167, '8b55e8'),
        (11167170, '895108'),
        (11167222, 'c1f904'),
        (11167245, '895604'),
        (11167248, '8b55f0'),
        (11167251, '891f'),
        (11167253, '8906'),
        (11167256, '8b5210'),
        (11167262, '7407'),
        (11167264, '8b02'),
        (11167266, '8bca'),
        (11167268, 'ff5004'),
        (11167293, 'c21000'),
        (11165744, '8bc1'),
        (11165746, '33c9'),
        (11165753, '894808'),
        (11165759, 'c3'),
    ]
    def witnesses_check(get):
        for va, expected in witnesses:
            literal = bytes.fromhex(expected)
            if get(va, len(literal)) != literal:
                raise ValueError('Independent byte witness mismatch')
    witnesses_check(read)
    calls=[(0xAA64AD,0xAA2170),(0xAA64CA,0xAA6030),(0xAA6551,0xAC15C0),
           (0xAA655E,0x12EA770),(0xAA65E0,0xAA6040),(0xAA65FD,0xAA6810),(0xAA662A,0xAA6040)]
    for va,target in calls:
        ins=read(va,5)
        if ins[0]!=0xe8 or va+5+struct.unpack('<i',ins[1:])[0]!=target:
            raise ValueError('Independent direct call mismatch')
    for va,target in [(0xAA64A6,0xAA64C6),(0xAA64F9,0xAA651C),
                      (0xAA6536,0xAA653B),(0xAA654F,0xAA655E),(0xAA661E,0xAA6627)]:
        ins=read(va,2)
        if va+2+struct.unpack('b',ins[1:])[0]!=target:
            raise ValueError('Independent branch displacement mismatch')
    def provenance(get):
        expected=[(0xAA64D5,'8b4d10'),(0xAA64D8,'894de8'),
                  (0xAA6507,'8b4de8'),(0xAA650A,'894f08'),
                  (0xAA65BF,'8b55e8'),(0xAA65C2,'895108')]
        for va,literal in expected:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:
                raise ValueError('Independent third-argument payload provenance mismatch')
        return dict(StackArgument=3,ArgumentStackOffset=16,TemporaryStackOffset=-24,
                    FastDestinationOffset=8,GrowthDestinationOffset=8)
    provenance_result=provenance(read)
    def receiver(get):
        for va,literal in [(0xAA64A1,'8bd1'),(0xAA64A3,'8955f0'),
                           (0xAA64E0,'8b720c'),(0xAA6610,'8b55f0'),
                           (0xAA6618,'8b5210'),(0xAA6622,'8bca')]:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:raise ValueError('Queue/record/callback receiver mismatch')
        return dict(QueueSavedStackOffset=-16,SelectedRecordOffset=12,CallbackObjectOffset=16)
    receiver_result=receiver(read)
    def growth(get,used):
        for va,literal in [(0xAA6523,'c1f804'),(0xAA6526,'83f801'),
                           (0xAA6536,'7203'),(0xAA653D,'03d8'),(0xAA6546,'c1e004')]:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:raise ValueError('Growth arithmetic mismatch')
        count=(used+max(used,1))&0xffffffff
        size=(count<<4)&0xffffffff
        return count,size,('none' if count==0 else 'small' if size<=128 else 'large')
    arithmetic=[]
    for used,expected in [(0,(1,16,'small')),(1,(2,32,'small')),(4,(8,128,'small')),
                          (5,(10,160,'large')),(8,(16,256,'large')),
                          (0x80000000,(0,0,'none')),(0xffffffff,(0xfffffffe,0xffffffe0,'large'))]:
        actual=growth(read,used);assert actual==expected
        arithmetic.append(dict(Used=used,Count=actual[0],Bytes=actual[1],Route=actual[2]))
    positive=api['audit'](args.exe,args.packages,repo)
    assert positive['PrimaryInstructionCounts']==[159,7]
    assert positive['DistinctTotalLeadCandidates']==10 and not positive['PayloadVtableEstablished']
    assert positive['IntraBodyDirectCallSites']==7 and positive['IntraBodyIndirectCallSites']==1
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r60.json', repo))
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
    for va,replacement,label in [(0xAA64D5,bytes.fromhex('8b4d0c'),'wrong argument source'),
        (0xAA64D8,bytes.fromhex('894dec'),'wrong temporary slot'),
        (0xAA650A,bytes.fromhex('894f04'),'wrong fast payload field'),
        (0xAA65C2,bytes.fromhex('895104'),'wrong growth payload field')]:
        reject(label,lambda va=va,replacement=replacement:provenance(
            lambda at,n:replacement if at==va else read(at,n)))
    reject('wrong selected record',lambda:receiver(lambda at,n:bytes.fromhex('8b7204') if at==0xAA64E0 else read(at,n)))
    reject('wrong callback receiver',lambda:receiver(lambda at,n:bytes.fromhex('8b5218') if at==0xAA6618 else read(at,n)))
    reject('wrong signed span shift',lambda:growth(lambda at,n:bytes.fromhex('c1e804') if at==0xAA6523 else read(at,n),4))
    reject('wrong unsigned demand comparison',lambda:growth(lambda at,n:bytes.fromhex('7c03') if at==0xAA6536 else read(at,n),4))
    expected_xrefs=api['audit'].__globals__['EXPECTED_INITIALIZER_XREFS']
    former=expected_xrefs[0];expected_xrefs[0]=0
    try:
        reject('wrong exact initializer xref',lambda:api['audit'](args.exe,args.packages,repo))
    finally:
        expected_xrefs[0]=former
    reject('missing direct/indirect call inventory',lambda:api['candidates']([]))
    # Synthetic field-value oracle, not original execution or pointer ownership proof.
    def vector(arguments):
        if len(arguments)!=4 or any(type(x)!=int or not 0<=x<=0xffffffff for x in arguments):
            raise ValueError('Synthetic DWORD fixture policy')
        return list(arguments)
    fixtures=[]
    for args4 in [(1,2,0,4),(1,2,0x12345678,4),(0xffffffff,0,0xffffffff,0)]:
        entry=vector(args4);assert entry[2]==args4[2]
        copied=list(entry);old=list(entry);old[2]=0
        assert copied[2]==args4[2] and old[2]==0
        fixtures.append(dict(Arguments=list(args4),Entry=entry,GrowthCopy=copied,OldAfterClear=old))
    # Duplicate payloads are representable; no uniqueness enforcement is established.
    duplicate=[vector((1,2,7,4)),vector((2,3,7,5))];assert duplicate[0][2]==duplicate[1][2]
    reject('synthetic malformed argument count',lambda:vector((1,2,3)))
    reject('synthetic oversized DWORD',lambda:vector((1,2,0x100000000,4)))
    if (repo/'docs/forbidden-r60.json').exists() or wrong.read_bytes()!=b'synthetic identity failure':
        raise AssertionError('Rejected output mutated fixture/public state')
    nested=guard['fresh_private_path'](output/'nested/new-parent/check.json',repo)
    with nested.open('x',encoding='utf-8',newline='\n') as stream:stream.write('{"fresh_parent":true}\n')
    result=dict(Positive=positive,IndependentBytes=len(witnesses),IndependentCalls=len(calls),
                IndependentBranches=5,Provenance=provenance_result,Receiver=receiver_result,
                ArithmeticCases=arithmetic,SyntheticFieldFixtures=fixtures,
                DuplicatePayloadRepresentable=True,UniqueOwnership='Unknown',
                PayloadVtable='Unknown; no actual caller construction established',
                NegativeChecks=negatives,SyntheticPolicy='DWORD/field oracle only, no original pointer execution',
                Cancellation='No explicit local check; callback/handler/delegates Unknown',
                OriginalExecuted=False,LiveContact=False)
    with (output/'checks.json').open('x',encoding='utf-8',newline='\n') as stream:stream.write(json.dumps(result,indent=2)+'\n')
    print(f'PASS {len(witnesses)} bytes/seven calls/five branches/seven arithmetic cases; {len(negatives)} negatives; payload class Unknown')


if __name__=='__main__':
    main()
