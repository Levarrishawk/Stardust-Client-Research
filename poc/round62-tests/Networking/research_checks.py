"""Independent small static byte/call witnesses for dispatch payload source byte ordering and bounded rejection gates."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_dispatch_payload_source.py'))
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
        (4599649, '8bdc'),
        (4599697, '8b7b08'),
        (4599730, '8bf0'),
        (4599746, '0f848e000000'),
        (4599770, '8b482c'),
        (4599775, '7518'),
        (4599801, '8b762c'),
        (4599824, '8bf0'),
        (4599831, '743d'),
        (4599858, '7422'),
        (4599860, '8b4f40'),
        (4599863, 'd94764'),
        (4599866, '8b5738'),
        (4599869, '8b4748'),
        (4599872, '83e1fe'),
        (4599875, '83c904'),
        (4599878, '51'),
        (4599879, '52'),
        (4599880, '51'),
        (4599881, 'd91c24'),
        (4599884, '50'),
        (4599885, '8bce'),
        (4599892, 'eb0d'),
        (4599894, '8b4f38'),
        (4599897, '85c9'),
        (4599899, '7406'),
        (4599901, '8b11'),
        (4599903, '6a01'),
        (4599905, 'ff12'),
        (4599925, 'c20400'),
        (4599744, '85f6'),
        (4599752, '8b16'),
        (4599754, '8bce'),
        (4599756, 'ff5244'),
        (4599767, 'ff5044'),
        (4599781, 'ff5244'),
        (4599784, '8b10'),
        (4599786, '8bc8'),
        (4599788, 'ff9280000000'),
        (4599794, '8b06'),
        (4599796, '8bce'),
        (4599798, 'ff503c'),
        (4599829, '85f6'),
        (4599856, '85c0'),
    ]
    def witnesses_check(get):
        for va, expected in witnesses:
            literal = bytes.fromhex(expected)
            if get(va, len(literal)) != literal:
                raise ValueError('Independent byte witness mismatch')
    witnesses_check(read)
    calls = [(4599706, 4599936), (4599719, 11763936), (4599739, 11159872), (4599819, 20047601), (4599848, 20047601), (4599887, 11784784)]
    for va,target in calls:
        ins=read(va,5)
        if ins[0]!=0xe8 or va+5+struct.unpack('<i',ins[1:])[0]!=target:
            raise ValueError('Independent direct call target mismatch')
    def branch(get,va,target):
        ins=get(va,6 if va==0x462FC2 else 2)
        if len(ins)==6 and ins[:2]==bytes.fromhex('0f84'):
            resolved=va+6+struct.unpack('<i',ins[2:])[0]
        elif len(ins)==2 and ins[0] in [0x74,0x75,0xeb]:
            resolved=va+2+struct.unpack('b',ins[1:])[0]
        else:raise ValueError('Branch encoding mismatch')
        if resolved!=target:raise ValueError('Branch target mismatch')
    for va,target in [(0x462FC2,0x463056),(0x462FDF,0x462FF9),
                      (0x463017,0x463056),(0x463032,0x463056),
                      (0x463054,0x463063),(0x46305B,0x463063)]:branch(read,va,target)
    def payload(get):
        patterns=[(0x462F61,'8bdc'),(0x462F91,'8b7b08'),(0x46303A,'8b5738'),
                  (0x463047,'52'),(0x463049,'d91c24'),(0x46304C,'50'),
                  (0x46304D,'8bce'),(0x463010,'8bf0'),(0x463075,'c20400')]
        for va,literal in patterns:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:raise ValueError('Input/field/stack/controller receiver identity mismatch')
        return dict(InputOffsetFromEntryEspAfterPush=8,MessageRegister='EDI',PayloadField=56,
                    WidthBytes=4,PushRegister='EDX',CalleeArgument=3,
                    ScalarOverwritesSecondArgument=True,ReceiverIsFirstCastReturn=True)
    source=payload(read)
    def fallback(get):
        patterns=[(0x463056,'8b4f38'),(0x463059,'85c9'),(0x46305B,'7406'),
                  (0x46305D,'8b11'),(0x46305F,'6a01'),(0x463061,'ff12')]
        for va,literal in patterns:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:raise ValueError('Failure-source/null/virtual argument mismatch')
        return dict(SourceOffset=56,NullSkip=True,FirstWordSlot=0,Argument=1,DirectFieldClear=False)
    failed=fallback(read)
    def normalize(get,word):
        if get(0x463040,3)!=bytes.fromhex('83e1fe') or get(0x463043,3)!=bytes.fromhex('83c904'):
            raise ValueError('Priority mask instruction mismatch')
        return (word&0xfffffffe)|4
    flag_cases=[]
    for word,expected in [(0,4),(1,4),(2,6),(4,4),(0x80000001,0x80000004),(0xffffffff,0xfffffffe)]:
        got=normalize(read,word);assert got==expected;flag_cases.append(dict(Input=word,Output=got))
    # Finite synthetic binary32 fixtures model the stack's scalar representation only.
    scalar_cases=[]
    for bits in [0,0x80000000,0x3f800000,0xbf800000,0x00800000,0x7f7fffff]:
        value=struct.unpack('<f',struct.pack('<I',bits))[0]
        restored=struct.unpack('<I',struct.pack('<f',value))[0];assert restored==bits
        scalar_cases.append(dict(Bits=bits,RoundTripBits=restored))
    positive=api['audit'](args.exe,args.packages,repo)
    assert positive['InstructionCounts']==[102] and positive['TinyDirectWrappers']==0
    assert positive['ActualCallSites']==12 and positive['DistinctTotalLeadCandidates']==12
    assert positive['PayloadCalleeArgument']==3 and not positive['FailurePayloadFieldClearedLocally']
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r62.json', repo))
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
    for va,replacement,label in [(0x462F91,bytes.fromhex('8b7b04'),'wrong incoming stack argument'),
        (0x46303A,bytes.fromhex('8b573c'),'wrong nested field'),
        (0x463047,bytes.fromhex('51'),'wrong payload pushed register'),
        (0x463049,bytes.fromhex('d95c2404'),'scalar overwrites payload argument'),
        (0x463010,bytes.fromhex('8bf2'),'wrong first cast result save'),
        (0x46304D,bytes.fromhex('8bc8'),'wrong second-cast controller receiver'),
        (0x463075,bytes.fromhex('c21000'),'wrong dispatch return adjustment')]:
        reject(label,lambda va=va,replacement=replacement:payload(
            lambda at,n:replacement if at==va else read(at,n)))
    for va,replacement,label in [(0x463056,bytes.fromhex('8b4f3c'),'wrong fallback field'),
        (0x46305F,bytes.fromhex('6a00'),'wrong fallback call argument'),
        (0x463061,bytes.fromhex('ff5204'),'wrong fallback slot')]:
        reject(label,lambda va=va,replacement=replacement:fallback(
            lambda at,n:replacement if at==va else read(at,n)))
    reject('wrong near failure target',lambda:branch(lambda at,n:bytes.fromhex('0f8400000000') if at==0x462FC2 else read(at,n),0x462FC2,0x463056))
    reject('wrong normalization mask',lambda:normalize(lambda at,n:bytes.fromhex('83e1ff') if at==0x463040 else read(at,n),1))
    reject('wrong normalization set bit',lambda:normalize(lambda at,n:bytes.fromhex('83c908') if at==0x463043 else read(at,n),1))
    reject('wrong second-cast test width',lambda:witnesses_check(
        lambda at,n:bytes.fromhex('84c0') if at==0x463030 else read(at,n)))
    reject('object callback confused with payload slot',lambda:witnesses_check(
        lambda at,n:bytes.fromhex('ff12') if at==0x462FCC else read(at,n)))
    reject('slot80 receiver confused with payload register',lambda:witnesses_check(
        lambda at,n:bytes.fromhex('8bcf') if at==0x462FEA else read(at,n)))
    reject('missing call-site inventory',lambda:api['call_sites']([]))
    stubs=[SimpleNamespace(address=va,op_str=op,mnemonic='call') for va,op in api['EXPECTED_CALLS']]
    assert len(api['call_sites'](stubs))==12
    reject('raw call-site budget13',lambda:api['call_sites'](stubs+[stubs[0]]))
    # Authored success/failure cutpoint vectors do not invoke legacy targets or infer deletion.
    route_cases=[]
    for root,first,second,pointer,expected in [(False,False,False,0,'skip'),
        (False,False,False,7,'slot0'),(True,False,False,7,'slot0'),
        (True,True,False,7,'slot0'),(True,True,True,0,'enqueue'),
        (True,True,True,7,'enqueue')]:
        route='enqueue' if root and first and second else 'slot0' if pointer else 'skip'
        assert route==expected;route_cases.append(dict(Root=root,FirstCast=first,SecondCast=second,
            PayloadValue=pointer,Route=route,SourceFieldAfter='No direct store; delegated effects Unknown'))
    cast_cases=[]
    for first,second in [(0x100,0x200),(1,1),(0x80000000,0x100),(0,1),(1,0)]:
        reaches=bool(first) and bool(second)
        cast_cases.append(dict(FirstReturn=first,SecondReturn=second,ReachesCall=reaches,
                              CallReceiver=first if reaches else None))
    if (repo/'docs/forbidden-r62.json').exists() or wrong.read_bytes()!=b'synthetic identity failure':
        raise AssertionError('Rejected output mutated fixture/public state')
    nested=guard['fresh_private_path'](output/'nested/new-parent/check.json',repo)
    with nested.open('x',encoding='utf-8',newline='\n') as stream:stream.write('{"fresh_parent":true}\n')
    result=dict(Positive=positive,IndependentBytes=len(witnesses),IndependentCalls=len(calls),
                IndependentBranches=6,PayloadProvenance=source,Fallback=failed,
                FlagsCases=flag_cases,FiniteScalarRepresentationCases=scalar_cases,
                SyntheticRouteCases=route_cases,FullWidthCastReceiverCases=cast_cases,
                RouteMeaning="enqueue denotes caller invocation, not proved insertion success",NegativeChecks=negatives,
                Ownership='field storage source confirmed; lifetime/deletion/unique ownership Unknown',
                Cancellation='no explicit local check; delegates Unknown',
                ScalarLimits='Finite representation fixtures only; original x87/NaN/FPU exception behavior unrun',
                SyntheticPolicy='Authored vectors and call-site stubs only; no original instructions/pointers executed',
                OriginalExecuted=False,LiveContact=False)
    with (output/'checks.json').open('x',encoding='utf-8',newline='\n') as stream:stream.write(json.dumps(result,indent=2)+'\n')
    print(f'PASS {len(witnesses)} bytes/six direct calls/six branches/payload stack identity; {len(negatives)} negatives')


if __name__=='__main__':
    main()
