"""Independent PE/byte/call witnesses and synthetic branch-routing checks."""
import argparse, hashlib, json, math, runpy, struct
from pathlib import Path

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for n in ['exe','packages','server-root','output-dir']:ap.add_argument('--'+n,type=Path,required=True)
    a=ap.parse_args();repo=Path(__file__).resolve().parents[3]
    api=runpy.run_path(str(repo/'tools/python/verify_creature_acceleration_helper.py'))
    guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    output=guard(a.output_dir,repo,directory=True);raw=a.exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper()!='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':raise ValueError('Independent identity rejection')
    nt=struct.unpack_from('<I',raw,0x3c)[0]
    if raw[nt:nt+4]!=b'PE\0\0':raise ValueError('PE signature')
    count=struct.unpack_from('<H',raw,nt+6)[0];opt=struct.unpack_from('<H',raw,nt+20)[0];base=struct.unpack_from('<I',raw,nt+52)[0]
    def read(va,size):
        if size<0:raise ValueError('Negative span')
        rva=va-base
        for j in range(count):
            _,start,dsize,disk=struct.unpack_from('<4I',raw,nt+24+opt+j*40+8)
            if start<=rva and rva+size<=start+dsize:
                data=raw[disk+rva-start:disk+rva-start+size]
                if len(data)==size:return data
        raise ValueError('Unmapped/truncated independent span')
    witnesses=[(0x431840,'558bec56'),(0x431844,'6a1c'),(0x431851,'8a450c'),(0x431858,'6800000100'),(0x43187e,'8b4030'),(0x43189c,'8b8e9c060000'),(0x4318a2,'8b5508'),(0x4318a5,'894d0c'),(0x4318a8,'528bc8'),(0x4318b5,'c20800'),(0x4318b8,'6a008bce'),(0x4318c1,'d85d08'),(0x4318ca,'8b4e10'),(0x4318d2,'f6c401'),(0x4318d5,'8b8688060000'),(0x4318db,'894508'),(0x4318e0,'6a01'),(0x4318e7,'d84d0c'),(0x4318eb,'d84d08'),(0x4318f2,'6a00'),(0x431901,'c20800'),(0x431923,'8b450850'),(0x43192d,'c20400'),(0x62a001,'8b53646a00528bcf'),(0x62a036,'8b4b646a00518bcf'),(0x62a043,'d9e0d84d08')]
    for va,h in witnesses:
        b=bytes.fromhex(h)
        if read(va,len(b))!=b:raise ValueError(f'Independent bytes {va:08X}')
    calls=[(0x431848,0x435250),(0x43185f,0x557360),(0x431865,0x11b5a80),(0x431872,0x648080),(0x431890,0x131e6f1),(0x4318ab,0x11ac4c0),(0x4318bc,0x431920),(0x4318e2,0x11add30),(0x4318f4,0x11add30),(0x431927,0x438950),(0x62a009,0x431840),(0x62a03e,0x431840)]
    for va,target in calls:
        b=read(va,5)
        if b[0]!=0xe8 or va+5+struct.unpack('<i',b[1:])[0]!=target:raise ValueError('Independent call mismatch')
    for va,size,sha in [(0x431840,196,'03d39d78a24d6c95f9c14bba2a41e7883521b54134380dea627ab78d63523700'),(0x431920,16,'662d0bcba876c776f85d14c55da198ca9d0a176f4e5c95d0070fb1121cc81d63')]:
        if hashlib.sha256(read(va,size)).hexdigest()!=sha:raise ValueError('Independent body digest')
    fixtures=json.loads((Path(__file__).parent/'route-fixtures.json').read_text(encoding='utf-8'))
    for f in fixtures['Cases']:
        enabled=all(f['Guards']) and f['SecondArgument']&255==0
        walk=struct.unpack('<f',struct.pack('<I',int(f['WalkBits'],16)))[0];speed=struct.unpack('<f',struct.pack('<I',int(f['CurrentBits'],16)))[0]
        c0=math.isnan(walk) or math.isnan(speed) or walk<speed
        actual='vehicle/base' if enabled else ('template0/base/modifier' if c0 else 'template1/base/modifier')
        if actual!=f['Expected']:raise ValueError('Synthetic route disagreement')
    positive=api['audit'](a.exe,a.packages,a.server_root,repo);negative=[]
    def reject(label,action):
        try:action()
        except (ValueError,OSError,StopIteration):negative.append(label)
        else:raise AssertionError('Accepted '+label)
    wrong=output/'synthetic-wrong.bin';wrong.write_bytes(b'synthetic wrong input')
    reject('wrong SHA before unavailable imports',lambda:api['audit'](wrong,output/'absent',a.server_root,repo))
    reject('missing executable',lambda:api['audit'](output/'absent.exe',a.packages,a.server_root,repo))
    globals_=api['audit'].__globals__;saved=globals_['BODY_SHA'];globals_['BODY_SHA']='0'*64
    try:reject('wrong helper digest',lambda:api['audit'](a.exe,a.packages,a.server_root,repo))
    finally:globals_['BODY_SHA']=saved
    anchors=globals_['ANCHORS'];saved=anchors[0];anchors[0]=(saved[0],'nop','')
    try:reject('wrong instruction expectation',lambda:api['audit'](a.exe,a.packages,a.server_root,repo))
    finally:anchors[0]=saved
    exact=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['exact']
    reject('truncated span',lambda:exact(lambda va,n:b'0'*(n-1),0x1000,4))
    reject('empty span',lambda:exact(lambda va,n:b'',0x1000,4))
    reject('unmapped span',lambda:read(0xffffffff,4));reject('negative span',lambda:read(0x431840,-1))
    reject('existing output file',lambda:guard(wrong,repo));reject('existing output directory',lambda:guard(output,repo,True))
    forbidden=repo/'docs/round51-forbidden.json';reject('public output',lambda:guard(forbidden,repo))
    if forbidden.exists():raise AssertionError('Public rejection wrote file')
    result=dict(Positive=positive,IndependentBytes=len(witnesses),IndependentCalls=len(calls),IndependentBodyDigests=2,SyntheticRoutes=len(fixtures['Cases']),Rejections=negative,OriginalExecuted=False)
    (output/'checks.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(f'PASS {len(witnesses)} bytes/{len(calls)} calls/2 bodies/{len(fixtures["Cases"])} synthetic routes/{len(negative)} rejections')

if __name__=='__main__':main()
