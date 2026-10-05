"""Bounded controller caller and scheduler-unit carrier audit; static only."""
import argparse,hashlib,json,runpy,struct,sys
from pathlib import Path
EXE_SHA='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
BODIES=[(0xb23100,398,152,'35ff7d2e4c18c3923b603194f018a84f9d058e50dfc15b226b89c30f867c7eee'),(0xb3d1c0,30,14,'3ed9930225632212619fd5a20ef7ec4be3d584d346f99f9c4d3dc9c5cb685f37')]
ANCHORS=[
(0xb23107,'mov','ebx, ecx'),(0xb23109,'mov','al, byte ptr [ebx + 0xc]'),(0xb2310c,'test','al, 4'),(0xb23120,'test','al, 2'),(0xb23131,'je','0xb23283'),(0xb2313c,'mov','esi, dword ptr [ebp + 8]'),(0xb23143,'push','esi'),(0xb23144,'call','dword ptr [eax + 0x20]'),(0xb2315a,'mov','ecx, dword ptr [ebx + 0x2c]'),(0xb2315d,'test','ecx, ecx'),(0xb2315f,'je','0xb2317a'),(0xb23161,'push','esi'),(0xb23162,'call','0xb3d1c0'),(0xb2328b,'ret','4'),
(0xb3d1c4,'mov','esi, ecx'),(0xb3d1c6,'mov','ecx, dword ptr [esi + 4]'),(0xb3d1c9,'call','0xaa6670'),(0xb3d1ce,'mov','ecx, dword ptr [ebp + 8]'),(0xb3d1d1,'mov','eax, dword ptr [esi]'),(0xb3d1d3,'push','ecx'),(0xb3d1d4,'mov','ecx, esi'),(0xb3d1d6,'call','dword ptr [eax + 0x14]'),(0xb3d1db,'ret','4'),
(0xb40ee7,'call','0xb24700'),(0xb40ef2,'sub','ecx, eax'),(0xb40efc,'sbb','eax, edx'),(0xb40f0e,'fild','qword ptr [ebp - 8]'),(0xb40f2d,'fmul','dword ptr [0x194702c]'),(0xb40f33,'fstp','dword ptr [ebp - 4]'),(0xb40f48,'jne','0xb40f52'),(0xb40f4c,'jne','0xb40f52'),(0xb40f66,'mov','ecx, dword ptr [ebp - 4]'),(0xb40f69,'mov','eax, dword ptr [esi]'),(0xb40f6b,'push','ecx'),(0xb40f6c,'mov','ecx, esi'),(0xb40f6e,'call','dword ptr [eax + 0x10]'),
(0xb3f6c0,'fld','dword ptr [0x15dc6f8]'),(0xb3f6c6,'fdiv','dword ptr [0x15ec614]'),(0xb3f6cc,'fstp','dword ptr [0x194702c]'),(0xb3fe66,'fld','dword ptr [0x15ec614]'),(0xb3fe6f,'fmul','dword ptr [ebp + 8]'),(0xb3fe8b,'call','0x131e27c'),
(0xaa4bbe,'call','dword ptr [0x15dc244]'),(0xaa4bf1,'fild','qword ptr [ebp - 0x1c]'),(0xaa4bfa,'fstp','dword ptr [0x19446d4]'),(0xaa4dea,'call','dword ptr [0x15dc240]'),(0xaa4e0f,'sub','ecx, edx'),(0xaa4e14,'sbb','edx, eax'),(0xaa4e1c,'fild','qword ptr [ebp - 8]'),(0xaa4e24,'fdiv','dword ptr [0x19446d4]'),(0xaa4e6d,'mov','dword ptr [0x1944720], ecx'),(0xaa4ec0,'fld','dword ptr [0x1944720]'),(0x42383c,'call','0xaa4ec0'),
(0x42f5f1,'mov','dword ptr [esi], 0x15dcc34'),(0x62a00e,'fmul','dword ptr [ebp + 8]'),(0x62a045,'fmul','dword ptr [ebp + 8]')]

def exact(read,va,n):
    if n<0:raise ValueError('Negative span')
    b=read(va,n)
    if len(b)!=n:raise ValueError('Truncated mapped span')
    return b

def audit(exe,packages,server_root,repo=None):
    raw=exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper()!=EXE_SHA:raise ValueError('Wrong executable SHA before imports/VA')
    repo=repo or Path(__file__).resolve().parents[2]
    frozen=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['audit'](exe,packages,server_root,repo)
    sys.path.insert(0,str(packages));import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=raw,fast_load=True);read=lambda va,n:pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n);md=Cs(CS_ARCH_X86,CS_MODE_32)
    bodies=[]
    for va,n,count,digest in BODIES:
        b=exact(read,va,n)
        if hashlib.sha256(b).hexdigest()!=digest:raise ValueError('Caller body digest mismatch')
        ins=list(md.disasm(b,va));addresses={i.address for i in ins}
        if len(ins)!=count or sum(i.size for i in ins)!=n or ins[-1].mnemonic!='ret':raise ValueError('Incomplete caller extent')
        branches=[]
        for i in ins:
            if i.mnemonic.startswith('j') and i.op_str.startswith('0x'):
                if int(i.op_str,16) not in addresses:raise ValueError('Caller branch misses instruction boundary')
                branches.append(dict(Va=f'{i.address:08X}',Target=i.op_str))
        bodies.append(dict(Va=f'{va:08X}',Bytes=n,Instructions=count,Sha256=digest,DirectBranches=branches))
    for va,m,o in ANCHORS:
        i=next(md.disasm(exact(read,va,15),va))
        if (i.mnemonic,i.op_str)!=(m,o):raise ValueError(f'Anchor mismatch {va:08X}')
    pe.parse_data_directories(directories=[pefile.DIRECTORY_ENTRY['IMAGE_DIRECTORY_ENTRY_IMPORT'],pefile.DIRECTORY_ENTRY['IMAGE_DIRECTORY_ENTRY_EXPORT']])
    imports={i.address:i.name for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports}
    if imports.get(0x15dc244)!=b'QueryPerformanceFrequency' or imports.get(0x15dc240)!=b'QueryPerformanceCounter':raise ValueError('Clock import identity')
    exports={s.name:pe.OPTIONAL_HEADER.ImageBase+s.address for s in pe.DIRECTORY_ENTRY_EXPORT.symbols}
    if exports.get(b'?frameTime@Clock@@SAMXZ')!=0xaa4ec0:raise ValueError('Clock getter export identity')
    if exact(read,0x15ec614,4)!=struct.pack('<f',1000) or exact(read,0x15dc6f8,4)!=struct.pack('<f',1):raise ValueError('Unit conversion constants')
    vtables=[]
    for vt,name,slot,target in [(0x15ec274,b'.?AVObject@@',0x10,0xb23100),(0x15dcc34,b'.?AVCreatureObject@@',0x10,0x430260),(0x15e034c,b'.?AVPlayerCreatureController@@',0x14,0x629750)]:
        col=struct.unpack('<I',exact(read,vt-4,4))[0];td=struct.unpack('<I',exact(read,col+12,4))[0]
        if exact(read,td+8,len(name)+1)!=name+b'\0' or struct.unpack('<I',exact(read,vt+slot,4))[0]!=target:raise ValueError('Dispatch/RTTI identity')
        vtables.append(dict(Vtable=f'{vt:08X}',Slot=slot,Target=f'{target:08X}',TypeDescriptor=f'{td:08X}'))
    window=exact(read,0xb40ed0,161)
    if hashlib.sha256(window).hexdigest()!='918fdcc38cf3e71d5b47e580e891a0794d16203aeb3a63595e68a28a9f272f55':raise ValueError('Scheduler carrier window digest')
    return dict(ExecutableSha256=EXE_SHA,Anchors=len(ANCHORS),Bodies=bodies,Dispatch=vtables,ClockImports=2,ClockExports=1,Constants=2,SchedulerCarrierBytes=161,CallerUnitConversion=False,ImmediateCaller='00B3D1C0 slot+14 to00629750 when PlayerCreatureController vtable is installed',ObjectCarrier='00B23100 incoming raw F32 through+2C controller',SchedulerUnits='seconds on established ordinary GroundScene route',PlayerUnits='Unknown across CreatureObject slot+10 override00430260',ConditionalPlayerUnits='seconds if00430260 reaches Object::alter without changing carrier units',UnresolvedCarrier='00430260 to00B23100/00B3D1C0',NewCalleesFollowed=['00B23100','00B3D1C0'],WholeProgramCallerCensus=False,StartupFpuSurveyRepeated=False,ArithmeticControlAtCallback='Unknown',OriginalExecuted=False,NativeAccelerationAdded=False,FrozenControllerEvidence=frozen)

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for n in ['exe','packages','server-root','receipt']:ap.add_argument('--'+n,type=Path,required=True)
    a=ap.parse_args();repo=Path(__file__).resolve().parents[2];guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    try:
        target=guard(a.receipt,repo);result=audit(a.exe,a.packages,a.server_root,repo)
        with target.open('x',encoding='utf-8',newline='\n') as f:f.write(json.dumps(result,indent=2)+'\n')
    except (ValueError,OSError,ImportError,StopIteration,struct.error) as e:ap.exit(1,str(e)+'\n')
    print(f'PASS {result["Anchors"]} anchors /two complete callers /three RTTI dispatch slots; player unit edge00430260 remains Unknown')
if __name__=='__main__':main()
