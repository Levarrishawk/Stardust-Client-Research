"""Independent small static byte/call witnesses for parser payload writer byte ordering and bounded rejection gates."""
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
    api = runpy.run_path(str(repo / 'tools/python/verify_object_controller_parser_payload_writer.py'))
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
        (11310720, '8bf1'),
        (11310723, '8975dc'),
        (11310726, '33db'),
        (11310825, '895e38'),
        (11310856, 'c7430400000000'),
        (11310927, 'c7466400000000'),
        (11310945, 'c70608ac5e01'),
        (11310951, 'c7461000ac5e01'),
        (11310996, '8b7d08'),
        (11310999, '57'),
        (11311000, '8bce'),
        (11311002, 'e8a11f6c00'),
        (11311007, '8b4648'),
        (11311010, '57'),
        (11311011, '50'),
        (11311012, 'e8e7390400'),
        (11311017, '8b5648'),
        (11311020, '894638'),
        (11311052, '52'),
        (11311053, '687cfa8a01'),
        (11311066, '68ff000000'),
        (11311071, '50'),
        (11311072, 'e89bc1fdff'),
        (11311077, '83c418'),
        (11311167, 'e89cb07e00'),
        (11311192, 'e8a3b494ff'),
        (11311197, '8bc6'),
        (11311215, 'c20400'),
    ]
    def witnesses_check(get):
        for va, expected in witnesses:
            literal = bytes.fromhex(expected)
            if get(va, len(literal)) != literal:
                raise ValueError('Independent byte witness mismatch')
    witnesses_check(read)
    # Focused raw edge probes; full primary bytes independently hash to the fixed digest.
    body=read(0xAC9660,530)
    if hashlib.sha256(body).hexdigest()!='a75e091a364e4153604d88a29fe948562f12430156b88d10abdfc2b1b247fc37':
        raise ValueError('Independent full primary digest mismatch')
    calls=[(0xAC979A,0x118B740),(0xAC97A4,0xB0D190)]
    for va,target in calls:
        ins=read(va,5)
        if ins[0]!=0xe8 or va+5+struct.unpack('<i',ins[1:])[0]!=target:
            raise ValueError('Independent focused call target mismatch')
    def writer(get):
        patterns=[(0xAC9680,'8bf1'),(0xAC9686,'33db'),(0xAC96E9,'895e38'),
                  (0xAC9794,'8b7d08'),(0xAC9797,'57'),(0xAC9798,'8bce'),
                  (0xAC979F,'8b4648'),(0xAC97A2,'57'),(0xAC97A3,'50'),
                  (0xAC97A9,'8b5648'),(0xAC97AC,'894638'),
                  (0xAC985D,'8bc6'),(0xAC986F,'c20400')]
        for va,literal in patterns:
            data=bytes.fromhex(literal)
            if get(va,len(data))!=data:raise ValueError('Receiver/archive/discriminator/factory-return identity mismatch')
        return dict(ReceiverRegister='ESI',ArchiveSourceStackOffset=8,
                    TypeFieldOffset=72,PayloadFieldOffset=56,Width=4,
                    TypePushRegister='EAX',ArchivePushRegister='EDI',FactoryResultRegister='EAX',
                    FactoryReturnPreservedAcrossEdxLoad=True,NormalReturnsIncomingReceiver=True)
    identity=writer(read)
    positive=api['audit'](args.exe,args.packages,repo)
    assert positive['InstructionCounts']==[162] and positive['TinyDirectWrappers']==0
    assert positive['FullBodyCallSites']==20 and positive['FullBodyDistinctDirectTargets']==13
    assert positive['DistinctTotalLeadCandidates']==15 and positive['SelectedRawProbeSiteCount']==2
    assert not positive['FactoryResultNullGuard'] and not positive['PostFactoryDirectFieldClear']
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
    reject('public output', lambda: guard['fresh_private_path'](repo / 'docs/forbidden-r63.json', repo))
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
    reject('complete lead budget17', lambda: api['capped'](range(17)))
    if len(api['capped'](range(16))) != 16:
        raise AssertionError('Budget boundary16 rejected')
    spans = api['audit'].__globals__['SPANS']
    former = spans[0]
    spans[0] = (former[0], former[1], '0' * 64, former[3])
    try:
        reject('wrong bounded-span expectation', lambda: api['audit'](args.exe, args.packages, repo))
    finally:
        spans[0] = former
    for va,replacement,label in [(0xAC9680,bytes.fromhex('8bf2'),'wrong message receiver'),
        (0xAC9686,bytes.fromhex('bb01000000'),'nonzero initializer source'),
        (0xAC96E9,bytes.fromhex('895e3c'),'wrong initial clear field'),
        (0xAC9794,bytes.fromhex('8b7d0c'),'wrong archive stack argument'),
        (0xAC979F,bytes.fromhex('8b4640'),'wrong factory discriminant field'),
        (0xAC97A2,bytes.fromhex('56'),'wrong factory archive push'),
        (0xAC97A3,bytes.fromhex('52'),'wrong factory type push'),
        (0xAC97A9,bytes.fromhex('8b4648'),'factory EAX overwritten before store'),
        (0xAC97AC,bytes.fromhex('895638'),'wrong result register'),
        (0xAC985D,bytes.fromhex('8bc7'),'wrong normal return receiver'),
        (0xAC986F,bytes.fromhex('c20800'),'wrong return argument adjustment')]:
        reject(label,lambda va=va,replacement=replacement:writer(
            lambda at,n:replacement if at==va else read(at,n)))
    sealed=api['audit'].__globals__['SEALED_INPUTS'];name=next(iter(sealed));former=sealed[name];sealed[name]='0'*64
    try:reject('wrong sealed accepted62 input',lambda:api['audit'](args.exe,args.packages,repo))
    finally:sealed[name]=former
    reject('missing focused writer edge',lambda:api['writer_probe_sites']([]))
    assert api['writer_probe_sites'](api['EXPECTED_WRITER_PROBE_SITES'])==api['EXPECTED_WRITER_PROBE_SITES']
    full=[SimpleNamespace(address=va,op_str=op,mnemonic='call') for va,op in api['EXPECTED_FULL_BODY_CALLS']]
    assert len(api['full_call_inventory'](full))==20
    assert len(api['candidates'](full))==15
    reject('complete raw site budget21',lambda:api['full_call_inventory'](full+[full[0]]))
    reject('one-less actual call inventory limit19',lambda:api['full_call_inventory'](full,site_limit=19))
    reject('one-less actual lead inventory limit14',lambda:api['candidates'](full,lead_limit=14))
    reject('truncated primary call inventory',lambda:api['full_call_inventory'](full[:-1]))
    # Authored factory-boundary argument/write vectors: no original archive or target execution.
    def vector(discriminant,archive,result):
        if any(type(x)!=int or not 0<=x<=0xffffffff for x in [discriminant,archive,result]):
            raise ValueError('Synthetic DWORD policy')
        return dict(FactoryArguments=[discriminant,archive],DirectFieldWrites=[0,result],
                    PostFactoryDirectFieldClear=False,ReturnedReceiver='same symbolic receiver')
    vectors=[]
    for kind,archive,result in [(0,0,0),(0x117,0x12345678,0),(0x117,0x12345678,0x23456789),
                               (0xffffffff,0xffffffff,0xffffffff)]:
        case=vector(kind,archive,result);assert case['DirectFieldWrites']==[0,result];vectors.append(case)
    failure_cutpoints=[dict(FactoryNormalReturn=False,DirectResultStoreReached=False,LastLocalFieldWrite=0,ActualFieldAfterDelegatedEffects='Unknown',
                           HandlerEffects='Unknown'),
                       dict(FactoryNormalReturn=True,LaterHelperNormalReturn=False,DirectResultStoreReached=True,
                           FieldResult='factory return; handler effects Unknown')]
    reject('synthetic oversized discriminator',lambda:vector(0x100000000,0,0))
    reject('synthetic negative result',lambda:vector(0,0,-1))
    if (repo/'docs/forbidden-r63.json').exists() or wrong.read_bytes()!=b'synthetic identity failure':
        raise AssertionError('Rejected output mutated fixture/public state')
    nested=guard['fresh_private_path'](output/'nested/new-parent/check.json',repo)
    with nested.open('x',encoding='utf-8',newline='\n') as stream:stream.write('{"fresh_parent":true}\n')
    result=dict(Positive=positive,IndependentBytes=len(witnesses),FocusedRawCalls=len(calls),
                FullBodyDigestIndependentlyChecked=True,WriterIdentity=identity,
                SyntheticFactoryVectors=vectors,FailureCutpoints=failure_cutpoints,NegativeChecks=negatives,
                BudgetDefinition='Lead amendment:20 complete-body sites,15 total leads under16 cap; two writer edges remain semantic witnesses',
                Ownership='Unknown; null return retained, reusable overwrite/delete not proved',
                Cancellation='No explicit local check; delegates Unknown',
                OriginalExecuted=False,LiveContact=False,SyntheticPolicy='authored boundary/cutpoint vectors only')
    with (output/'checks.json').open('x',encoding='utf-8',newline='\n') as stream:stream.write(json.dumps(result,indent=2)+'\n')
    print(f'PASS {len(witnesses)} bytes/two focused raw calls/full primary digest; {len(negatives)} negatives; full inventory20/13 retained')


if __name__=='__main__':
    main()
