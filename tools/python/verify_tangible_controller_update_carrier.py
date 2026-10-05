"""One new complete update carrier; conditional player seconds, static only."""
import argparse,hashlib,json,runpy,struct,sys
from pathlib import Path
EXE_SHA='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
BODY=(0x554c60,130,54,'3a248d6f027f1e0b90b17575b22e150ba129313716b24a549c14ad0fac0e6ad0')
ANCHORS=[(0x554c65,'mov','ebx, dword ptr [ebp + 8]'),(0x554c69,'push','ebx'),(0x554c6a,'mov','esi, ecx'),(0x554c6c,'call','0xb23100'),(0x554c71,'fst','dword ptr [ebp + 8]'),(0x554c74,'fcomp','dword ptr [0x15ec4c4]'),(0x554c7f,'jp','0x554c8f'),(0x554c8c,'ret','4'),(0x554c92,'call','0x5558e0'),(0x554c97,'mov','eax, dword ptr [esi + 0x284]'),(0x554ca1,'mov','edi, dword ptr [eax + 4]'),(0x554ca4,'mov','esi, dword ptr [eax]'),(0x554caa,'lea','ebx, [ebx]'),(0x554cb0,'mov','ecx, dword ptr [esi]'),(0x554cb2,'test','ecx, ecx'),(0x554cb8,'push','ebx'),(0x554cb9,'call','dword ptr [eax + 0x10]'),(0x554cc3,'lea','edx, [ebp + 8]'),(0x554cc7,'call','0xb35110'),(0x554ccf,'add','esi, 4'),(0x554cd4,'jne','0x554cb0'),(0x554cd6,'fld','dword ptr [ebp + 8]'),(0x554cdf,'ret','4')]
ATTACHMENT=[(0x519238,'call','0x629400'),(0x51923d,'mov','dword ptr [ebp + 0xc], eax'),(0x519245,'mov','ecx, dword ptr [ebp + 0xc]'),(0x519248,'push','ecx'),(0x519249,'mov','ecx, edi'),(0x519252,'call','0xb22bb0'),(0x519259,'mov','dword ptr [esi + 0x1c], edi'),(0x6295b8,'mov','dword ptr [ebx], 0x15e034c'),(0xb22bba,'mov','edi, dword ptr [ebp + 8]'),(0xb22bcb,'mov','dword ptr [esi + 0x2c], edi'),(0x51faf9,'mov','ecx, dword ptr [esi + 0x1c]'),(0x51faff,'mov','ecx, dword ptr [ecx + 0x2c]'),(0x51fb02,'call','0xb3d340')]
CONDITIONS=['ordinary accepted scheduler route selects the CreatureObject receiver','ordinary normal return and nonvolatile-register ABI across preceding callees','Object+0C flag04 clear and flag02 set','current object+2C is non-null when Object reads it','current controller retains PlayerCreatureController dispatch table at wrapper slot+14','queue update returns normally and receiver remains valid through dispatch']

def exact(read,va,n):
    if n<0:raise ValueError('Negative span')
    b=read(va,n)
    if len(b)!=n:raise ValueError('Truncated span')
    return b

def audit(exe,packages,server_root,repo=None):
    raw=exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper()!=EXE_SHA:raise ValueError('Wrong executable SHA before imports/VA')
    repo=repo or Path(__file__).resolve().parents[2]
    frozen=runpy.run_path(str(repo/'tools/python/verify_creature_controller_update_carrier.py'))['audit'](exe,packages,server_root,repo)
    sys.path.insert(0,str(packages));import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=raw,fast_load=True);read=lambda va,n:pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n);md=Cs(CS_ARCH_X86,CS_MODE_32)
    va,n,count,digest=BODY;b=exact(read,va,n);ins=list(md.disasm(b,va));addresses={i.address for i in ins}
    if hashlib.sha256(b).hexdigest()!=digest or len(ins)!=count or sum(i.size for i in ins)!=n:raise ValueError('Incomplete/mismatched carrier body')
    if [(i.address,i.op_str) for i in ins if i.mnemonic=='ret']!=[(0x554c8c,'4'),(0x554cdf,'4')]:raise ValueError('Carrier return forms')
    branches=[]
    for i in ins:
        if i.mnemonic.startswith('j'):
            if not i.op_str.startswith('0x') or int(i.op_str,16) not in addresses:raise ValueError('Carrier branch boundary')
            branches.append((i.address,int(i.op_str,16)))
    if len(branches)!=5 or any(src<0x554c6c<dst or src>0x554c6c>=dst for src,dst in branches):raise ValueError('Carrier bypass/reentry')
    if [i.address for i in ins if i.mnemonic=='call' and i.op_str=='0xb23100']!=[0x554c6c]:raise ValueError('Object call uniqueness')
    if any(i.address<0x554c6c and i.op_str.startswith('dword ptr [ebp + 8],') for i in ins):raise ValueError('Pre-call input write')
    # EBX remains the original word for the later per-element dispatch loop.
    writes=[i.address for i in ins if i.mnemonic not in ('push','pop','cmp','test') and i.op_str.startswith('ebx,')]
    if writes!=[0x554c65,0x554caa]:raise ValueError('Unexpected input register write')
    for address,m,o in ANCHORS+ATTACHMENT:
        i=next(md.disasm(exact(read,address,15),address))
        if (i.mnemonic,i.op_str)!=(m,o):raise ValueError(f'Anchor mismatch {address:08X}')
    # Reuse accepted Networking body identities without caller scanning/new semantic follow.
    receiver=runpy.run_path(str(repo/'tools/python/verify_ground_scene_input_map_queue_assignment.py'))
    identities=[]
    for address,size,h,_ in receiver['SPANS']:
        if address in (0x518eb0,0x629400,0xb22bb0,0x51fad0):
            if hashlib.sha256(exact(read,address,size)).hexdigest()!=h:raise ValueError('Frozen attachment body identity')
            identities.append(dict(Va=f'{address:08X}',Bytes=size,Sha256=h))
    if len(identities)!=4:raise ValueError('Frozen attachment count')
    def rtti(vt,slot,target,name):
        if struct.unpack('<I',exact(read,vt+slot,4))[0]!=target:raise ValueError('Dispatch target')
        col=struct.unpack('<I',exact(read,vt-4,4))[0];td=struct.unpack('<I',exact(read,col+12,4))[0]
        if exact(read,td+8,len(name)+1)!=name+b'\0':raise ValueError('RTTI identity')
        return dict(Vtable=f'{vt:08X}',Slot=slot,Target=f'{target:08X}',CompleteObjectLocator=f'{col:08X}',TypeDescriptor=f'{td:08X}',Type=name.decode('ascii'))
    types=[rtti(0x15de26c,0x10,0x554c60,b'.?AVClientObject@@'),rtti(0x15e034c,0x14,0x629750,b'.?AVPlayerCreatureController@@')]
    return dict(ExecutableSha256=EXE_SHA,Anchors=len(ANCHORS),AttachmentAnchors=len(ATTACHMENT),Body=dict(Va='00554C60',Bytes=n,Instructions=count,Sha256=digest,DirectBranches=5,Returns=['00554C8C','00554CDF']),NewCalleesFollowed=['00554C60'],NewBodyBudget=2,Dispatch=types,FrozenAttachmentBodies=identities,Carrier='scheduler seconds ->00430260 ->0063F3B0 ->00554C60 ->00B23100 ->00B3D1C0 ->00629750 under stated current receiver/gate conditions',CarrierContinuity='Closed static ordinary-route raw-word path',PlayerUnits='seconds under Conditions; no unconditional runtime type/lifetime claim',Conditions=CONDITIONS,RuntimeOccurrence='Unknown; original not executed',UnconditionalPlayerUnits='Unknown outside evidenced route/receiver conditions',Attachment='accepted setup constructs PlayerCreatureController, installs object+2C and stores scene+1C; wrapper uses current object+2C, not cached constructor result',InterveningSideEffects='Unknown; do not assume setup-installed pointer/type persists',LaterLoop='per-element+284 range sends same EBX word at+10 after parent call; not temporal subdivision of parent call',ResultScratch='00554C71 returned ST0 to F32 argument slot;00554CC3 passes that result slot address onward',NativeAccelerationAdded=False,OriginalExecuted=False,WholeProgramCallerCensus=False,StartupFpuSurveyRepeated=False,FrozenCarrierEvidence=frozen)

def main():
    ap=argparse.ArgumentParser(description=__doc__)
    for name in ['exe','packages','server-root','receipt']:ap.add_argument('--'+name,type=Path,required=True)
    a=ap.parse_args();repo=Path(__file__).resolve().parents[2];guard=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    try:
        target=guard(a.receipt,repo);result=audit(a.exe,a.packages,a.server_root,repo)
        with target.open('x',encoding='utf-8',newline='\n') as f:f.write(json.dumps(result,indent=2)+'\n')
    except (ValueError,OSError,ImportError,StopIteration,struct.error) as e:ap.exit(1,str(e)+'\n')
    print('PASS one complete carrier closes ordinary word path; player seconds conditional on current installed receiver/gates/normal ABI')
if __name__=='__main__':main()
