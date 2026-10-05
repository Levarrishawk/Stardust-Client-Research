"""Independent raw-PE carrier/attachment witnesses and synthetic gate cases."""
import argparse,hashlib,json,runpy,struct
from pathlib import Path

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for name in ['exe','packages','server-root','output-dir']:ap.add_argument('--'+name,type=Path,required=True)
    a=ap.parse_args();repo=Path(__file__).resolve().parents[3]
    api=runpy.run_path(str(repo/'tools/python/verify_tangible_controller_update_carrier.py'))
    guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.output_dir,repo,True)
    raw=a.exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper()!='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':raise ValueError('Independent SHA gate')
    nt=struct.unpack_from('<I',raw,0x3c)[0];count=struct.unpack_from('<H',raw,nt+6)[0];opt=struct.unpack_from('<H',raw,nt+20)[0];base=struct.unpack_from('<I',raw,nt+52)[0]
    if raw[nt:nt+4]!=b'PE\0\0':raise ValueError('PE signature')
    def read(va,n):
        if n<0:raise ValueError('Negative span')
        rv=va-base
        for j in range(count):
            _,start,size,disk=struct.unpack_from('<4I',raw,nt+24+opt+j*40+8)
            if start<=rv and rv+n<=start+size:
                b=raw[disk+rv-start:disk+rv-start+n]
                if len(b)==n:return b
        raise ValueError('Unmapped/truncated PE span')
    def byte_witness(va,h):
        if read(va,len(bytes.fromhex(h)))!=bytes.fromhex(h):raise ValueError('Independent byte mismatch')
    witnesses=[(0x554c60,'558bec5153'),(0x554c65,'8b5d08'),(0x554c69,'538bf1'),(0x554c71,'d95508'),(0x554c74,'d81dc4c45e01'),(0x554c8c,'c20400'),(0x554c97,'8b8684020000'),(0x554caa,'8d9b00000000'),(0x554cb0,'8b0e85c9'),(0x554cb8,'53ff5010'),(0x554cc3,'8d550852'),(0x554cd6,'d94508'),(0x554cdf,'c20400'),(0xb2310c,'a804'),(0xb23120,'a802'),(0xb2315a,'8b4b2c85c9'),(0xb23161,'56'),(0xb3d1ce,'8b4d088b06518bce'),(0xb3d1d6,'ff5014'),(0x519245,'8b4d0c518bcf'),(0x519259,'897e1c'),(0x6295b8,'c7034c035e01'),(0xb22bcb,'897e2c'),(0x51faf9,'8b4e1c'),(0x51faff,'8b492c')]
    for va,h in witnesses:byte_witness(va,h)
    def call_witness(va,target):
        b=read(va,5)
        if b[0]!=0xe8 or va+5+struct.unpack('<i',b[1:])[0]!=target:raise ValueError('Independent relative call')
    calls=[(0x554c6c,0xb23100),(0x4302c3,0x63f3b0),(0x63f3bd,0x554c60),(0xb23162,0xb3d1c0),(0x519238,0x629400),(0x519252,0xb22bb0),(0x51fb02,0xb3d340),(0xb3d1c9,0xaa6670)]
    for va,target in calls:call_witness(va,target)
    def branch_witness(va,opcode,target):
        b=read(va,2)
        if b[0]!=opcode or va+2+struct.unpack('<b',b[1:])[0]!=target:raise ValueError('Independent branch')
    branches=[(0x554c7f,0x7a,0x554c8f),(0x554c9f,0x74,0x554cd6),(0x554ca8,0x74,0x554cd6),(0x554cb4,0x74,0x554ccf),(0x554cd4,0x75,0x554cb0)]
    for row in branches:branch_witness(*row)
    types=[(0x15de26c,0x10,0x554c60,b'.?AVClientObject@@'),(0x15e034c,0x14,0x629750,b'.?AVPlayerCreatureController@@')]
    for vt,slot,target,name in types:
        if struct.unpack('<I',read(vt+slot,4))[0]!=target:raise ValueError('Independent dispatch')
        col=struct.unpack('<I',read(vt-4,4))[0];td=struct.unpack('<I',read(col+12,4))[0]
        if read(td+8,len(name)+1)!=name+b'\0':raise ValueError('Independent RTTI')
    for va,n,h in [(0x554c60,130,'3a248d6f027f1e0b90b17575b22e150ba129313716b24a549c14ad0fac0e6ad0'),(0xb23100,398,'35ff7d2e4c18c3923b603194f018a84f9d058e50dfc15b226b89c30f867c7eee'),(0xb3d1c0,30,'3ed9930225632212619fd5a20ef7ec4be3d584d346f99f9c4d3dc9c5cb685f37'),(0x629400,463,'7e0ece87fff30632c54c37e069ba2b13bd945e9c7ad7dcba2089013cdb5d9ee9'),(0xb22bb0,36,'a9c68691bf0ae8e8958e9abd3539e21bb361e6f11f2ce8ef69edbc8731c862b3')]:
        if hashlib.sha256(read(va,n)).hexdigest()!=h:raise ValueError('Independent body digest')
    f=json.loads(Path(__file__).with_name('carrier-fixtures.json').read_text(encoding='utf-8'));assert f['Synthetic'] is True
    cases=callbacks=0
    for word in f['Words']:
        for owner in f['Owners']:
            for flags in f['Flags']:
                for current in f['CurrentControllers']:
                    # Model observed copies/gates only; current is read after setup side effects.
                    ebx=word;object_word,object_ecx=ebx,owner
                    assert (object_word,object_ecx)==(word,owner)
                    accepted=(flags&4)==0 and (flags&2)!=0 and current['Pointer']!=0
                    target_is_player=accepted and current['Vtable']==0x15e034c
                    if target_is_player:
                        wrapper_word,wrapper_ecx=object_word,current['Pointer']
                        callback_word,callback_ecx=wrapper_word,wrapper_ecx
                        assert (callback_word,callback_ecx)==(word,current['Pointer']);callbacks+=1
                    result_scratch=f['ReturnedWord'];assert object_word==word and result_scratch==f['ReturnedWord'];cases+=1
    units=0
    for seconds in f['ConditionalSeconds']:
        word=struct.unpack('<I',struct.pack('<f',seconds))[0];assert struct.pack('<I',word)==struct.pack('<f',seconds);units+=1
    if (cases,callbacks)!=(288,36):raise ValueError('Fixture gate expectation')
    negatives=[]
    def reject(name,action):
        try:action()
        except (ValueError,OSError):negatives.append(name)
        else:raise AssertionError('Negative accepted: '+name)
    bad=out/'wrong.exe';bad.write_bytes(b'synthetic wrong executable')
    reject('wrong executable before imports/VA',lambda:api['audit'](bad,out/'absent-packages',a.server_root,repo))
    reject('missing executable',lambda:api['audit'](out/'missing.exe',a.packages,a.server_root,repo))
    reject('negative span',lambda:api['exact'](read,0x554c60,-1))
    reject('truncated span',lambda:api['exact'](lambda va,n:b'',0x554c60,1))
    reject('unmapped raw PE span',lambda:read(0,1))
    reject('wrong input witness',lambda:byte_witness(0x554c65,'8b5d0c'))
    reject('wrong carrier target',lambda:call_witness(0x554c6c,0xb23101))
    reject('wrong branch boundary',lambda:branch_witness(0x554cd4,0x75,0x554cb1))
    reject('wrong receiver slot',lambda:byte_witness(0xb22bcb,'897e30'))
    reject('public output',lambda:guard(repo/'docs/refused.json',repo))
    reject('existing output',lambda:guard(bad,repo))
    reject('traversal output',lambda:guard(repo/'local-private/../docs/refused.json',repo))
    assert bad.read_bytes()==b'synthetic wrong executable'
    assert not (repo/'docs/refused.json').exists()
    assert not (out/'missing.exe').exists()
    positive=api['audit'](a.exe,a.packages,a.server_root,repo)
    if positive['NewCalleesFollowed']!=['00554C60'] or not positive['PlayerUnits'].startswith('seconds under Conditions'):raise ValueError('Scope/unit regression')
    result=dict(IndependentBytes=len(witnesses),IndependentCalls=len(calls),IndependentBranches=len(branches),IndependentBodyDigests=5,IndependentDispatch=2,IndependentRtti=2,ForwardingGateCases=cases,PlayerCallbackCases=callbacks,ConditionalUnitCases=units,Negatives=negatives,SyntheticFixtures=True,OutputGuardCleanupChecks=3,OriginalExecuted=False,Positive=positive)
    (out/'checks.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(f'PASS {cases} synthetic forwarding/gate cases, {callbacks} player callbacks, {len(negatives)} negatives; independent bytes/calls/branches/identity')
if __name__=='__main__':main()
