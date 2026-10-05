"""Independent raw PE, original-record and synthetic getter-boundary checks."""
import argparse,contextlib,hashlib,json,math,runpy,struct,sys
from pathlib import Path

def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def nominal(c,p,op):
    if op=='+':return c+p
    if op=='-':return p-c
    if op=='=':return p*(1+c*struct.unpack('<f',bytes.fromhex('0ad7233c'))[0])
    if op=='_':return p-p*(c*struct.unpack('<f',bytes.fromhex('0ad7233c'))[0])
    return c

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for n in ['exe','packages','server-root','root','configuration','oracle','output-dir']:ap.add_argument('--'+n,type=Path,required=True)
    a=ap.parse_args();repo=Path(__file__).resolve().parents[3];api=runpy.run_path(str(repo/'tools/python/verify_creature_acceleration_template_getter.py'))
    guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];output=guard(a.output_dir,repo,True)
    raw=a.exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper()!='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':raise ValueError('Independent SHA rejection')
    nt=struct.unpack_from('<I',raw,0x3c)[0];count=struct.unpack_from('<H',raw,nt+6)[0];opt=struct.unpack_from('<H',raw,nt+20)[0];base=struct.unpack_from('<I',raw,nt+52)[0]
    if raw[nt:nt+4]!=b'PE\0\0':raise ValueError('PE signature')
    def read(va,n):
        if n<0:raise ValueError('Negative extent')
        rv=va-base
        for j in range(count):
            _,start,size,disk=struct.unpack_from('<4I',raw,nt+24+opt+j*40+8)
            if start<=rv and rv+n<=start+size:
                b=raw[disk+rv-start:disk+rv-start+n]
                if len(b)==n:return b
        raise ValueError('Unmapped extent')
    witnesses=[(0x11add3d,'8d1c49'),(0x11add41,'c1e303'),(0x11add67,'8a843378040000'),(0x11add7f,'8bf7ebc1'),(0x11add92,'8d84498d0000008d0cc6'),(0x11adda1,'d955f8'),(0x11adda4,'8a9c337c040000'),(0x11addc4,'c745fc00000000'),(0x11addd1,'8b4d08ddd8518bcf'),(0x11addde,'d95dfcd945f8'),(0x11adde9,'d845fc'),(0x11ade25,'d86dfc'),(0x11ade2e,'c20400'),(0x13fa1e0,'8b410848'),(0x13fa1e6,'83e802'),(0x13fa1f2,'8b410cd94004c3'),(0x13fa1f9,'d94104c3'),(0x11b1640,'bfa0e88d01'),(0x11b1668,'8dba68040000'),(0x11b1681,'83c71883fe02'),(0x13fa3b5,'8b4d08'),(0x13fa3bb,'8908'),(0x13fa3c0,'8b45e4'),(0x13fa3c7,'894204')]
    for va,h in witnesses:
        if read(va,len(bytes.fromhex(h)))!=bytes.fromhex(h):raise ValueError('Raw byte witness')
    calls=[(0x11add5a,0x131e6f1),(0x11add9c,0x13fa1e0),(0x11addd9,0x11add30),(0x11b165e,0xa94f00),(0x11b16b3,0x13fa200),(0x11b16be,0x13fa0b0),(0x4318e2,0x11add30),(0x4318f4,0x11add30)]
    for va,target in calls:
        b=read(va,5)
        if b[0]!=0xe8 or va+5+struct.unpack('<i',b[1:])[0]!=target:raise ValueError('Relative call')
    branches=[(0x11add4b,0x11add67),(0x11add70,0x11add92),(0x11add79,0x11add7f),(0x11add7d,0x11add83),(0x11add81,0x11add44),(0x11addae,0x11addbf),(0x11addb3,0x11addbf),(0x11addb8,0x11addbf),(0x11addbd,0x11ade28),(0x11addcb,0x11adde4),(0x11addcf,0x11adde4),(0x11adde7,0x11addf5),(0x11addf8,0x11ade25),(0x11addfd,0x11ade17),(0x11ade1a,0x11ade28)]
    for va,target in branches:
        b=read(va,2)
        if b[0] not in [0x74,0x75,0xeb] or va+2+struct.unpack('<b',b[1:])[0]!=target:raise ValueError('Independent short branch')
    if read(0x18de8a0,13)!=b'acceleration\0' or read(0x18de898,6)!=b'speed\0':raise ValueError('Independent parameter strings')
    for va,n,h in [(0x11add30,257,'98776f201b8d00320bc0bd542cf98129e7ef8144591eda4affbae8eaf2405f1e'),(0x13fa1e0,29,'049de918f66be46a39d92ecea6a400d7e676301ae0c78945fb607d9c92087638')]:
        if hashlib.sha256(read(va,n)).hexdigest()!=h:raise ValueError('Independent body digest')
    fixtures=json.loads((Path(__file__).parent/'getter-fixtures.json').read_text(encoding='utf-8'))
    for case in fixtures['EndpointRelative']:
        mode=case['Mode'];current=2.0 if mode==1 else (8.0 if mode==3 else 0.0);op=case['Relative'];parent=10.0 if case['Parent'] else 0.0
        actual=nominal(current,parent,op)
        if not math.isclose(actual,case['ExpectedNominal'],rel_tol=0,abs_tol=1e-12):raise ValueError('Synthetic nominal arithmetic')
        called=case['Parent'] and op in '+-=_'
        if called!=case['ParentCalled'] or mode==3 and current!=8:raise ValueError('Synthetic sample/call classification')
    for c in fixtures['Inheritance']:
        actual='parent-same-index' if c['Parent'] else ('F32-zero' if c['GlobalNonzero'] else 'unguarded-null-reentry')
        if actual!=c['Expected']:raise ValueError('Inheritance routing')
    # Parent return precision is a synthetic trace witness: recursive relative arithmetic may exceed F32 before its caller spills it.
    extended=f32(1.0)+f32(2**-30)
    if extended==f32(extended):raise ValueError('Synthetic parent-return precision witness lacks difference')
    decoder=runpy.run_path(str(repo/'tools/python/verify_creature_template_movement.py'))['decode_parameter']
    valid=struct.pack('<i',2)+b'\1 '+struct.pack('<f',2)+b'\3 '+struct.pack('<2f',4,8)
    array=api['decode_array'](valid,decoder)
    if array['Parameters'][1]['RangeF32LE']!=['00008040','00000041']:raise ValueError('Synthetic endpoint layout')
    extra=api['decode_array'](struct.pack('<i',3)+valid[4:]+b'\0 ',decoder)
    if extra['Assigned']!=2 or extra['Discarded']!=1:raise ValueError('Extra parameter discard')
    positive=api['static_audit'](a.exe,a.packages,a.server_root,repo);negative=[]
    def reject(label,action):
        try:action()
        except (ValueError,OSError,StopIteration,struct.error):negative.append(label)
        else:raise AssertionError('Accepted negative '+label)
    wrong=output/'wrong.bin';wrong.write_bytes(b'synthetic identity')
    reject('wrong SHA before packages',lambda:api['static_audit'](wrong,output/'absent',a.server_root,repo));reject('missing exe',lambda:api['static_audit'](output/'missing.exe',a.packages,a.server_root,repo))
    from argparse import Namespace
    bad_config=Namespace(exe=a.exe,packages=a.packages,server_root=a.server_root,root=a.root,configuration=wrong)
    reject('wrong configuration before asset lookup',lambda:api['original_audit'](bad_config,repo))
    g=api['static_audit'].__globals__;saved=g['GETTER_SHA'];g['GETTER_SHA']='0'*64
    try:reject('wrong extent digest',lambda:api['static_audit'](a.exe,a.packages,a.server_root,repo))
    finally:g['GETTER_SHA']=saved
    anchors=g['ANCHORS'];at=7;saved=anchors[at];anchors[at]=(saved[0],saved[1],'0x11add93')
    try:reject('unaligned branch assertion',lambda:api['static_audit'](a.exe,a.packages,a.server_root,repo))
    finally:anchors[at]=saved
    for n in range(len(valid)):reject(f'truncation-{n}',lambda n=n:api['decode_array'](valid[:n],decoder))
    for label,b in [('negative count',struct.pack('<i',-1)),('count cap',struct.pack('<i',65)),('unsupported mode',struct.pack('<i',1)+b'\4 '),('trailing bytes',valid+b'0')]:reject(label,lambda b=b:api['decode_array'](b,decoder))
    reject('unmapped span',lambda:read(0xffffffff,8));reject('negative span',lambda:read(0x11add30,-1));reject('existing file',lambda:guard(wrong,repo));reject('existing directory',lambda:guard(output,repo,True));reject('public output',lambda:guard(repo/'docs/round52-forbidden.json',repo))
    # Independently reread only the winning original record through the accepted overlay resolver.
    oracle=json.loads(a.oracle.read_text(encoding='utf-8'));resource=oracle['Original']['ResourceOracle'];entry=resource['Chain'][1];record=oracle['Original']['AccelerationRecords'][1]
    if hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()!=resource['ConfigurationSha256']:raise ValueError('Independent config identity')
    sys.path.insert(0,str(repo/'tools/python'));from cmpa_corpus_inventory import archives
    from verify_textured_mesh_bundle import Index
    with contextlib.ExitStack() as stack:
        for priority,archive in archives(a.root,a.configuration):
            ix=Index(stack.enter_context(archive.open('rb')));b=ix.lookup(entry['Path'])
            if b is not None:break
        else:raise ValueError('Required original record missing')
        if priority!=entry['Priority'] or len(b)!=entry['Bytes'] or hashlib.sha256(b).hexdigest().upper()!=entry['Sha256']:raise ValueError('Independent record identity')
        encoded=bytes.fromhex(record['RawHex']);needle=b'acceleration\0'+encoded;offset=b.find(needle)
        if offset<0 or b.find(needle,offset+1)>=0 or offset+13!=record['PayloadOffset']:raise ValueError('Independent acceleration occurrence')
        if struct.unpack_from('<i',encoded)[0]!=2 or len(encoded)!=16 or encoded[4:6]!=b'\1 ' or encoded[10:12]!=b'\1 ':raise ValueError('Independent original literal layout')
        bits=[encoded[6:10].hex(),encoded[12:16].hex()]
        if bits!=[p['F32LE'] for p in record['Parameters']]:raise ValueError('Independent raw original words')
    result=dict(Positive=positive,ByteWitnesses=len(witnesses),Calls=len(calls),ShortBranches=len(branches),Strings=2,BodyDigests=2,EndpointRelativeCases=len(fixtures['EndpointRelative']),InheritanceCases=len(fixtures['Inheritance']),PrecisionTraceWitnesses=2,ArrayPositives=2,Rejections=negative,OriginalRecordChecks=8,OriginalLiteralWords=bits,OriginalExecuted=False,RuntimeAcceleration=False)
    (output/'checks.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(f'PASS {len(witnesses)} bytes/{len(calls)} calls/{len(branches)} branches/2 strings/2 bodies/{len(fixtures["EndpointRelative"])} nominal cases/{len(negative)} negatives/one original record')
if __name__=='__main__':main()
