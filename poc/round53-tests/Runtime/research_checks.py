"""Independent byte/call/constant/dispatch witnesses; original execution forbidden."""
import argparse,hashlib,json,runpy,struct
from pathlib import Path

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for n in ['exe','packages','server-root','output-dir']:ap.add_argument('--'+n,type=Path,required=True)
    a=ap.parse_args();repo=Path(__file__).resolve().parents[3];api=runpy.run_path(str(repo/'tools/python/verify_player_controller_update_units.py'))
    guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.output_dir,repo,True);raw=a.exe.read_bytes()
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
        raise ValueError('Unmapped/truncated span')
    witnesses=[(0xb2310c,'a804'),(0xb23120,'a802'),(0xb2313c,'8b7508'),(0xb2315a,'8b4b2c85c9'),(0xb23161,'56'),(0xb3d1c4,'8bf18b4e04'),(0xb3d1ce,'8b4d088b06518bce'),(0xb3d1d6,'ff5014'),(0xb3d1db,'c20400'),(0xb40ef2,'2bc8'),(0xb40efc,'1bc2'),(0xb40f2d,'d80d2c709401'),(0xb40f33,'d95dfc'),(0xb40f66,'8b4dfc8b06518bceff5010'),(0xb3f6c6,'d83514c65e01'),(0xb3f6cc,'d91d2c709401'),(0xb3fe6f,'d84d08'),(0xaa4bbe,'ff1544c25d01'),(0xaa4dea,'ff1540c25d01'),(0xaa4e24,'d835d4469401'),(0xaa4ec0,'d90520479401c3'),(0x42f5f1,'c70634cc5d01'),(0x62a00e,'d84d08'),(0x62a045,'d84d08')]
    for va,h in witnesses:
        if read(va,len(bytes.fromhex(h)))!=bytes.fromhex(h):raise ValueError(f'Raw byte mismatch {va:08X}')
    calls=[(0xb23162,0xb3d1c0),(0xb3d1c9,0xaa6670),(0xb40ee7,0xb24700),(0xb3fe8b,0x131e27c),(0x42383c,0xaa4ec0)]
    for va,target in calls:
        b=read(va,5)
        if b[0]!=0xe8 or va+5+struct.unpack('<i',b[1:])[0]!=target:raise ValueError('Independent relative call')
    if read(0x15ec614,4)!=bytes.fromhex('00007a44') or read(0x15dc6f8,4)!=bytes.fromhex('0000803f'):raise ValueError('Independent scale constants')
    types=[(0x15ec274,0x10,0xb23100,b'.?AVObject@@'),(0x15dcc34,0x10,0x430260,b'.?AVCreatureObject@@'),(0x15e034c,0x14,0x629750,b'.?AVPlayerCreatureController@@')]
    for vt,slot,target,name in types:
        if struct.unpack('<I',read(vt+slot,4))[0]!=target:raise ValueError('Independent dispatch target')
        col=struct.unpack('<I',read(vt-4,4))[0];td=struct.unpack('<I',read(col+12,4))[0]
        if read(td+8,len(name)+1)!=name+b'\0':raise ValueError('Independent RTTI type')
    for va,n,h in [(0xb23100,398,'35ff7d2e4c18c3923b603194f018a84f9d058e50dfc15b226b89c30f867c7eee'),(0xb3d1c0,30,'3ed9930225632212619fd5a20ef7ec4be3d584d346f99f9c4d3dc9c5cb685f37')]:
        if hashlib.sha256(read(va,n)).hexdigest()!=h:raise ValueError('Independent caller extent')
    # Raw PE import-thunk association, independently of pefile/producer metadata.
    irva=struct.unpack_from('<I',raw,nt+24+104)[0];found={}
    for j in range(256):
        original,_,_,_,first=struct.unpack('<5I',read(base+irva+j*20,20))
        if not first:break
        for k in range(4096):
            hint=struct.unpack('<I',read(base+(original or first)+k*4,4))[0]
            if hint==0:break
            if hint&0x80000000:continue
            slot=base+first+k*4
            if slot in [0x15dc244,0x15dc240]:
                name=bytearray()
                for c in range(128):
                    b=read(base+hint+2+c,1)
                    if b==b'\0':break
                    name.extend(b)
                found[slot]=bytes(name)
        if len(found)==2:break
    if found!={0x15dc244:b'QueryPerformanceFrequency',0x15dc240:b'QueryPerformanceCounter'}:raise ValueError('Independent import-slot association')
    fixtures=json.loads((Path(__file__).parent/'carrier-fixtures.json').read_text(encoding='utf-8'))
    for c in fixtures['Forwarding']:
        bits=int(c['Bits'],16);flags=c['ObjectFlags'];called=(flags&4)==0 and bool(flags&2) and c['ControllerPresent']
        if called!=c['ExpectedCalled']:raise ValueError('Synthetic gate')
        if called:
            # Model only the observed integer moves/pushes under normally returning callees.
            frame_word=struct.unpack('<I',struct.pack('<I',bits))[0];esi=frame_word;wrapper_frame=esi;callee_word=wrapper_frame
            if callee_word!=int(c['ExpectedWord'],16):raise ValueError('Opaque carrier changed')
    for c in fixtures['NominalScheduler']:
        if c['Current']<c['Last']:raise ValueError('Synthetic ordinary-time fixture')
        delta=c['Current']-c['Last'];num,den=c['ExpectedSecondsFraction']
        if delta*den!=num*1000 or c['ExpectedDispatch']!=(delta!=0):raise ValueError('Synthetic nominal unit/skip')
    positive=api['audit'](a.exe,a.packages,a.server_root,repo);negative=[]
    def reject(label,action):
        try:action()
        except (ValueError,OSError,StopIteration,struct.error):negative.append(label)
        else:raise AssertionError('Accepted '+label)
    wrong=out/'wrong.bin';wrong.write_bytes(b'synthetic wrong identity')
    reject('wrong executable before absent packages',lambda:api['audit'](wrong,out/'missing',a.server_root,repo));reject('missing executable',lambda:api['audit'](out/'missing.exe',a.packages,a.server_root,repo))
    g=api['audit'].__globals__;saved=g['BODIES'][0];g['BODIES'][0]=(saved[0],saved[1]-1,saved[2],saved[3])
    try:reject('truncated complete-body expectation',lambda:api['audit'](a.exe,a.packages,a.server_root,repo))
    finally:g['BODIES'][0]=saved
    anchors=g['ANCHORS'];saved=anchors[4];anchors[4]=(saved[0],saved[1],'0xb23284')
    try:reject('unaligned branch expectation',lambda:api['audit'](a.exe,a.packages,a.server_root,repo))
    finally:anchors[4]=saved
    reject('truncated synthetic span',lambda:api['exact'](lambda va,n:b'0'*(n-1),0x1000,8));reject('empty span',lambda:api['exact'](lambda va,n:b'',0x1000,8));reject('negative span',lambda:api['exact'](read,0xb23100,-1));reject('unmapped span',lambda:read(0xffffffff,8));reject('existing file',lambda:guard(wrong,repo));reject('existing directory',lambda:guard(out,repo,True));reject('public output',lambda:guard(repo/'docs/round53-forbidden.json',repo))
    if (repo/'docs/round53-forbidden.json').exists():raise ValueError('Public rejection wrote output')
    result=dict(Positive=positive,IndependentBytes=len(witnesses),RelativeCalls=len(calls),Constants=2,DispatchSlots=3,RttiTypes=3,ImportSlots=2,BodyDigests=2,ForwardingCases=len(fixtures['Forwarding']),NominalUnitCases=len(fixtures['NominalScheduler']),Negatives=negative,PlayerUnits='Unknown across00430260',OriginalExecuted=False,NativeAcceleration=False)
    (out/'checks.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(f'PASS {len(witnesses)} bytes/{len(calls)} calls/2 constants/3 dispatch/2 imports/{len(fixtures["Forwarding"])} opaque carrier cases/{len(negative)} negatives; override edge Unknown')
if __name__=='__main__':main()
