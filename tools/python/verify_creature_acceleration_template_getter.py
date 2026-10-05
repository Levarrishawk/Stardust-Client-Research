"""SHA-gated indexed acceleration getter and one pinned acceleration record."""
import argparse, hashlib, json, runpy, struct, sys
from pathlib import Path
EXE_SHA='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
GETTER_SHA='98776f201b8d00320bc0bd542cf98129e7ef8144591eda4affbae8eaf2405f1e'
ENDPOINT_SHA='049de918f66be46a39d92ecea6a400d7e676301ae0c78945fb607d9c92087638'
ANCHORS=[
(0x11add38,'mov','esi, ecx'),(0x11add3a,'mov','ecx, dword ptr [ebp + 8]'),(0x11add3d,'lea','ebx, [ecx + ecx*2]'),(0x11add41,'shl','ebx, 3'),(0x11add44,'mov','eax, dword ptr [esi + 0x18]'),(0x11add5a,'call','0x131e6f1'),(0x11add67,'mov','al, byte ptr [ebx + esi + 0x478]'),(0x11add70,'jne','0x11add92'),(0x11add72,'mov','al, byte ptr [0x18de794]'),(0x11add79,'je','0x11add7f'),(0x11add7d,'je','0x11add83'),(0x11add7f,'mov','esi, edi'),(0x11add81,'jmp','0x11add44'),(0x11add83,'fld','dword ptr [0x15dc6a0]'),(0x11add8f,'ret','4'),(0x11add92,'lea','eax, [ecx + ecx*2 + 0x8d]'),(0x11add99,'lea','ecx, [esi + eax*8]'),(0x11add9c,'call','0x13fa1e0'),(0x11adda1,'fst','dword ptr [ebp - 8]'),(0x11adda4,'mov','bl, byte ptr [ebx + esi + 0x47c]'),(0x11addbf,'mov','eax, dword ptr [esi + 0x18]'),(0x11addc4,'mov','dword ptr [ebp - 4], 0'),(0x11addcb,'je','0x11adde4'),(0x11addcf,'je','0x11adde4'),(0x11addd1,'mov','ecx, dword ptr [ebp + 8]'),(0x11addd4,'fstp','st(0)'),(0x11addd6,'push','ecx'),(0x11addd7,'mov','ecx, edi'),(0x11addd9,'call','0x11add30'),(0x11addde,'fstp','dword ptr [ebp - 4]'),(0x11adde1,'fld','dword ptr [ebp - 8]'),(0x11adde9,'fadd','dword ptr [ebp - 4]'),(0x11addff,'fmul','dword ptr [0x15de0e4]'),(0x11ade08,'fadd','dword ptr [0x15dc6f8]'),(0x11ade0e,'fmul','dword ptr [ebp - 4]'),(0x11ade1c,'fmul','dword ptr [0x15de0e4]'),(0x11ade22,'fmul','dword ptr [ebp - 4]'),(0x11ade25,'fsubr','dword ptr [ebp - 4]'),(0x11ade2e,'ret','4'),
(0x13fa1e0,'mov','eax, dword ptr [ecx + 8]'),(0x13fa1e4,'je','0x13fa1f9'),(0x13fa1e6,'sub','eax, 2'),(0x13fa1e9,'je','0x13fa1f2'),(0x13fa1eb,'fld','dword ptr [0x15dc6a0]'),(0x13fa1f2,'mov','eax, dword ptr [ecx + 0xc]'),(0x13fa1f5,'fld','dword ptr [eax + 4]'),(0x13fa1f9,'fld','dword ptr [ecx + 4]'),
(0x11b1640,'mov','edi, 0x18de8a0'),(0x11b164b,'mov','ecx, 0xd'),(0x11b165e,'call','0xa94f00'),(0x11b1668,'lea','edi, [edx + 0x468]'),(0x11b1670,'cmp','esi, dword ptr [ebp + 8]'),(0x11b1673,'jge','0x11b1a6a'),(0x11b167e,'call','dword ptr [eax]'),(0x11b1681,'add','edi, 0x18'),(0x11b1684,'cmp','esi, 2'),(0x11b1687,'jl','0x11b1670'),(0x11b16b3,'call','0x13fa200'),(0x11b16be,'call','0x13fa0b0'),
(0x13fa3a9,'mov','dword ptr [esi + 8], 3'),(0x13fa3b5,'mov','ecx, dword ptr [ebp + 8]'),(0x13fa3bb,'mov','dword ptr [eax], ecx'),(0x13fa3c0,'mov','eax, dword ptr [ebp - 0x1c]'),(0x13fa3c7,'mov','dword ptr [edx + 4], eax'),(0x11ace81,'push','2'),(0x11ace83,'push','0x18'),(0x11ace85,'lea','ecx, [esi + 0x468]'),(0x4318e0,'push','1'),(0x4318e2,'call','0x11add30'),(0x4318f2,'push','0'),(0x4318f4,'call','0x11add30')]

def decode_array(raw, decoder):
    if len(raw)<4 or len(raw)>65536:raise ValueError('Acceleration stream size budget')
    count=struct.unpack_from('<i',raw)[0]
    if not 0<=count<=64:raise ValueError('Acceleration count research budget')
    values=[];offset=4
    for i in range(count):
        value,offset=decoder(raw,offset)
        if i<2:values.append(value)
    if offset!=len(raw):raise ValueError('Trailing acceleration bytes')
    return dict(Count=count,Assigned=min(count,2),Discarded=max(0,count-2),Parameters=values)

def static_audit(exe,packages,server_root,repo=None):
    raw=exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper()!=EXE_SHA:raise ValueError('Wrong executable SHA before imports/VA')
    repo=repo or Path(__file__).resolve().parents[2]
    frozen=runpy.run_path(str(repo/'tools/python/verify_creature_acceleration_helper.py'))['audit'](exe,packages,server_root,repo)
    sys.path.insert(0,str(packages));import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=raw,fast_load=True);md=Cs(CS_ARCH_X86,CS_MODE_32)
    def read(va,n):
        b=pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n)
        if len(b)!=n:raise ValueError('Truncated mapped extent')
        return b
    bodies=[]
    for va,n,count,digest in [(0x11add30,257,99,GETTER_SHA),(0x13fa1e0,29,12,ENDPOINT_SHA)]:
        b=read(va,n)
        if hashlib.sha256(b).hexdigest()!=digest:raise ValueError('Bounded body digest mismatch')
        ins=list(md.disasm(b,va));addresses={i.address for i in ins};branches=[]
        if len(ins)!=count or sum(i.size for i in ins)!=n or ins[-1].mnemonic!='ret':raise ValueError('Incomplete body')
        for i in ins:
            if i.mnemonic.startswith('j'):
                if not i.op_str.startswith('0x') or int(i.op_str,16) not in addresses:raise ValueError('Unaligned/unbounded direct branch')
                branches.append(dict(Va=f'{i.address:08X}',Target=i.op_str))
        bodies.append(dict(Va=f'{va:08X}',Bytes=n,Instructions=count,Sha256=digest,Branches=branches,Calls=[dict(Va=f'{i.address:08X}',Target=i.op_str) for i in ins if i.mnemonic=='call'],Returns=sum(i.mnemonic=='ret' for i in ins)))
    if read(0x11ade31,15)!=b'\x90'*15 or read(0x13fa1fd,3)!=b'\x90'*3:raise ValueError('Extent separator mismatch')
    for va,m,o in ANCHORS:
        i=next(md.disasm(read(va,15),va))
        if (i.mnemonic,i.op_str)!=(m,o):raise ValueError(f'Anchor mismatch {va:08X}')
    if read(0x18de8a0,13)!=b'acceleration\0':raise ValueError('Acceleration loader string mismatch')
    for va,h in [(0x15dc6a0,'00000000'),(0x15dc6f8,'0000803f'),(0x15de0e4,'0ad7233c')]:
        if read(va,4)!=bytes.fromhex(h):raise ValueError('Arithmetic literal mismatch')
    return dict(ExecutableSha256=EXE_SHA,Bodies=bodies,Anchors=len(ANCHORS),FrozenHelper=frozen,IndexBoundCheck=False,Selectors=[0,1],StorageBase=0x468,Stride=24,LoadedOffset=0x10,RelativeOffset=0x14,EndpointReaderModeReturns={'1':'literal +4','3':'second stored endpoint','other':'F32 zero'},RngCalledByEndpointReader=False,UpdateUnits='Unknown',OriginalExecuted=False,NativeAccelerationAdded=False)

def original_audit(args,repo):
    legacy=runpy.run_path(str(repo/'tools/python/verify_creature_template_movement.py'))
    original=legacy['audit'].__globals__['inspect_document'];captured=[]
    def inspect(raw):
        doc=original(raw)
        if len(captured)<2:
            from verify_static_mesh_slice import nodes
            from verify_creature_template_profile import parse
            parent,layers=parse(nodes(raw)[0]);payload=layers.get('SCOT',('',{}))[1].get('acceleration')
            if payload is None:raise ValueError('Expected acceleration record missing')
            array=decode_array(payload,legacy['decode_parameter'])
            needle=b'acceleration\0'+payload;at=raw.find(needle)
            if at<0 or raw.find(needle,at+1)>=0:raise ValueError('Ambiguous acceleration location')
            captured.append(dict(PayloadOffset=at+13,RawHex=payload.hex(),**array))
        return doc
    legacy['audit'].__globals__['inspect_document']=inspect
    resource=legacy['audit'](args,repo)
    if len(captured)!=2 or any(c['Count']!=2 for c in captured):raise ValueError('Acceleration root/parent count mismatch')
    if any(p['Mode']!=0 for p in captured[0]['Parameters']) or any(p['Mode']!=1 or p['RelativeByte']!=32 for p in captured[1]['Parameters']):raise ValueError('Expected inherited literal acceleration record mismatch')
    return dict(ResourceOracle=resource,AccelerationRecords=[dict(Source=resource['Chain'][i]['Path'],SourceSha256=resource['Chain'][i]['Sha256'],**c) for i,c in enumerate(captured)],SelectedRecordCount=1,InheritedSelectors=2,OriginalRuntimeValuesClaimed=False)

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for n in ['exe','packages','server-root','root','configuration','receipt']:ap.add_argument('--'+n,type=Path,required=True)
    a=ap.parse_args();repo=Path(__file__).resolve().parents[2];guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    try:
        target=guard(a.receipt,repo);s=static_audit(a.exe,a.packages,a.server_root,repo);original=original_audit(a,repo)
        with target.open('x',encoding='utf-8',newline='\n') as f:f.write(json.dumps(dict(Static=s,Original=original),indent=2,allow_nan=False)+'\n')
    except (ValueError,AssertionError,OSError,ImportError,StopIteration,struct.error) as e:ap.exit(1,str(e)+'\n')
    print(f'PASS {s["Anchors"]} anchors /257-byte getter/29-byte endpoint /one inherited acceleration record')
if __name__=='__main__':main()
