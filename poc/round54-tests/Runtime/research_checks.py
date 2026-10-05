"""Independent raw PE witnesses and synthetic carrier checks; static only."""
import argparse, hashlib, json, runpy, struct
from pathlib import Path

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'output-dir']:
        ap.add_argument('--'+name, type=Path, required=True)
    a=ap.parse_args(); repo=Path(__file__).resolve().parents[3]
    api=runpy.run_path(str(repo/'tools/python/verify_creature_controller_update_carrier.py'))
    guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    out=guard(a.output_dir,repo,True); raw=a.exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper()!='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150': raise ValueError('Independent SHA gate')
    nt=struct.unpack_from('<I',raw,0x3c)[0]; count=struct.unpack_from('<H',raw,nt+6)[0]
    optional=struct.unpack_from('<H',raw,nt+20)[0]; base=struct.unpack_from('<I',raw,nt+52)[0]
    if raw[nt:nt+4]!=b'PE\0\0': raise ValueError('PE signature')
    def read(va,n):
        if n<0: raise ValueError('Negative span')
        rv=va-base
        for j in range(count):
            _,start,size,disk=struct.unpack_from('<4I',raw,nt+24+optional+j*40+8)
            if start<=rv and rv+n<=start+size:
                b=raw[disk+rv-start:disk+rv-start+n]
                if len(b)==n:return b
        raise ValueError('Unmapped/truncated span')
    # Literal expectations independent of producer Capstone anchors/tables.
    witnesses=[(0x430282,'8bf9'),(0x4302b3,'8b7508'),(0x4302b8,'568bc8'),(0x4302c0,'568bcf'),(0x4302c8,'d81dc4c45e01'),(0x4302ea,'c20400'),(0x430b0e,'894508'),(0x430bc2,'894508'),(0x430c67,'894508'),(0x430d7d,'c20400'),(0x63f3b6,'8b4508'),(0x63f3ba,'508bf1'),(0x63f3c2,'d95508'),(0x63f3c5,'d81dc4c45e01'),(0x63f4db,'d94508'),(0x63f4e2,'c20400')]
    for va,h in witnesses:
        if read(va,len(bytes.fromhex(h)))!=bytes.fromhex(h): raise ValueError(f'Independent byte mismatch {va:08X}')
    calls=[(0x4302bb,0x660e10),(0x4302c3,0x63f3b0),(0x63f3bd,0x554c60)]
    for va,target in calls:
        b=read(va,5)
        if b[0]!=0xe8 or va+5+struct.unpack('<i',b[1:])[0]!=target:raise ValueError('Independent relative call')
    b=read(0x4302b6,2)
    if b[0]!=0x74 or 0x4302b8+struct.unpack('<b',b[1:])[0]!=0x4302c0:raise ValueError('Optional branch')
    if struct.unpack('<I',read(0x15dcc44,4))[0]!=0x430260:raise ValueError('Creature slot+10')
    col=struct.unpack('<I',read(0x15dcc30,4))[0];td=struct.unpack('<I',read(col+12,4))[0]
    if read(td+8,21)!=b'.?AVCreatureObject@@\0':raise ValueError('Creature RTTI')
    for va,n,h in [(0x430260,2848,'59f2babe65ab48b18824dbab63e77f2d6a1f4ce21ba487f9274902edc777dd28'),(0x63f3b0,309,'af01c6b4ad50658f6fbc6355fec4bdf3a0fe5af8705ff5e5f6787598e9462bc0')]:
        if hashlib.sha256(read(va,n)).hexdigest()!=h:raise ValueError('Independent body digest')
    fixture=json.loads(Path(__file__).with_name('carrier-fixtures.json').read_text(encoding='utf-8'));assert fixture['Synthetic'] is True
    cases=0
    for word in fixture['Words']:
        for receiver in fixture['Receivers']:
            for optional_call in fixture['OptionalSideCall']:
                # Synthetic model of push/mov, assuming ordinary ABI callee saves.
                esi,edi=word,receiver
                if optional_call:assert esi==word
                intermediate_word,intermediate_ecx=esi,edi
                eax=intermediate_word
                boundary_word,boundary_ecx=eax,intermediate_ecx
                assert (boundary_word,boundary_ecx)==(word,receiver)
                argument_scratch=fixture['ReturnedWord']
                assert boundary_word==word and argument_scratch==fixture['ReturnedWord']
                cases+=1
    units=0
    for seconds in fixture['ConditionalUnits']:
        word=struct.unpack('<I',struct.pack('<f',seconds))[0]
        assert struct.pack('<I',word)==struct.pack('<f',seconds);units+=1
    negatives=[]
    def reject(name,action):
        try:action()
        except (ValueError,OSError):negatives.append(name)
        else:raise AssertionError('Negative accepted: '+name)
    bad=out/'wrong.exe';bad.write_bytes(b'wrong synthetic input')
    reject('wrong executable before VA/imports',lambda:api['audit'](bad,out/'absent-packages',a.server_root,repo))
    reject('missing executable',lambda:api['audit'](out/'missing.exe',a.packages,a.server_root,repo))
    reject('negative span',lambda:api['exact'](read,0x430260,-1))
    reject('truncated mapped span',lambda:api['exact'](lambda va,n:b'',0x430260,1))
    reject('unmapped PE span',lambda:read(0,1))
    reject('public output',lambda:guard(repo/'docs/refused.json',repo))
    reject('existing output',lambda:guard(bad,repo))
    reject('directory traversal output',lambda:guard(repo/'local-private/../docs/refused.json',repo))
    positive=api['audit'](a.exe,a.packages,a.server_root,repo)
    if positive['NewCalleesFollowed']!=['00430260','0063F3B0'] or not positive['PlayerUnits'].startswith('Unknown'):raise ValueError('Scope/uncertainty regression')
    result=dict(IndependentBytes=len(witnesses),IndependentCalls=len(calls),IndependentBodyDigests=2,IndependentDispatch=1,IndependentRtti=1,OptionalBranch=1,ForwardingCases=cases,ConditionalUnitCases=units,Negatives=negatives,SyntheticFixtures=True,OriginalExecuted=False,Positive=positive)
    (out/'checks.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(f'PASS {cases} synthetic forwarding cases; {units} conditional unit cases; {len(negatives)} negatives; two independent body digests')
if __name__=='__main__':main()
