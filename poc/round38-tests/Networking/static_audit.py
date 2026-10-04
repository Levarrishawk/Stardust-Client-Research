"""Hash-gated static CREO4 registration, exact snapshot/scalar and lifecycle audit."""
import argparse,hashlib,json,re,struct,sys
from pathlib import Path
EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
OFFSETS=[0x690,0x67c,0x4f8,0x770,0x640,0x62c,0x848,0x668,0x6b8,0x6cc,0x6a4,0x654,0x6e0,0x6f4]
TYPES={2:(0x15dd03c,0x43c070,0x43c1b0,'AutoDeltaVector@HUDefaultObjectType'),3:(0x15dce70,0x43f190,0x43f490,'AutoDeltaMap@V?$basic_string'),6:(0x15dcde0,0x440310,0x440340,'AutoDeltaVariable@VNetworkId'),13:(0x15dcedc,0x43e380,0x43e510,'AutoDeltaSet@U?$pair@VNetworkId')}
ANCHORS=[(0x439a90,'mov','eax, dword ptr [ecx + 0xc]'),(0x439a93,'mov','dword ptr [ecx + 0x10], eax'),(0x440260,'mov','eax, dword ptr [ecx + 0x10]'),(0x440263,'mov','dword ptr [ecx + 0x18], eax'),(0x440266,'mov','edx, dword ptr [ecx + 0x14]'),(0x440269,'mov','dword ptr [ecx + 0x1c], edx'),(0x555446,'mov','eax, dword ptr [ecx + 0xc4]'),(0x55544c,'add','ecx, 0xc4'),(0x554d6c,'mov','edx, dword ptr [esi + 0xc4]'),(0x554d72,'lea','ecx, [esi + 0xc4]'),(0x118b747,'push','2'),(0x118b765,'call','dword ptr [edx + 8]'),(0x118b770,'jne','0x118b760'),(0x439b39,'push','4'),(0x439b3f,'call','0x439910'),(0x439b59,'push','4'),(0x439b5f,'call','0x439910'),(0x44031e,'call','0xaa5830'),(0x44034e,'call','0xaa5830'),(0xaa5851,'call','0xaa57f0'),(0xaa57f9,'push','8'),(0xaa57fc,'call','0x439910'),(0x43c08a,'call','0x404d50'),(0x43c09f,'push','4'),(0x43c0ac,'push','4'),(0x43c0ae,'lea','edx, [edi + 0x18]'),(0x43c0d0,'push','4'),(0x43c18e,'jb','0x43c0cd'),(0x43f1c7,'call','0x44fcc0'),(0x43f1f6,'push','4'),(0x43f217,'add','edx, 0x18'),(0x43f21b,'call','0x118b030'),(0x43f240,'push','1'),(0x43f24e,'push','2'),(0x43f25f,'cmp','ax, 0xffff'),(0x43f26f,'push','4'),(0x43f29a,'call','0x43cad0'),(0x43f2bc,'add','eax, edx'),(0x43f2c0,'push','4'),(0x43f2cd,'push','4'),(0x43f353,'mov','dword ptr [eax + 0x1c], edx'),(0x43f35f,'mov','dword ptr [edx + 0x20], ecx'),(0x43f393,'jb','0x43f235'),(0x43f3a3,'call','0x4061f0'),(0x43e3b7,'call','0x44fa70'),(0x43e3fd,'push','4'),(0x43e417,'push','4'),(0x43e41a,'add','ebx, 0x18'),(0x43e438,'call','0xaa5830'),(0x43e442,'call','0xaa5830'),(0x43e454,'call','0x44fab0'),(0x43e45f,'jb','0x43e433'),(0x43e485,'call','0xaa4940'),(0x43e494,'call','0xaa4940'),(0x42ef13,'push','9'),(0x42ef1c,'call','0x446860'),(0x42ef21,'mov','dword ptr [edi + 0x18], 0'),(0x42f2f5,'mov','dword ptr [edi + 0x18], 0'),(0x42f258,'mov','dword ptr [edi + 0x18], 0'),(0x4468d1,'rep stosd','dword ptr es:[edi], eax'),(0x4454d6,'call','0x43b5d0'),(0x43b5f1,'mov','dword ptr [esi], 0x15dd03c'),(0x43b681,'cmp','eax, 0x80'),(0x445876,'call','0x43ebf0'),(0x43ec57,'call','0x4016b0'),(0x43ecba,'call','0x44fcc0'),(0x445696,'call','0x43dc50'),(0x43dcbd,'call','0xaa4940'),(0x43dcc8,'call','0xaa4940'),(0x118b062,'cmp','edi, ebx'),(0x118b065,'ja','0x118b092')]
ANCHORS += [(4430534, 'push', '4'), (4430540, 'call', '0x118b0e0'), (4457115, 'call', '0xaa5890'), (11163839, 'call', '0xaa5810'), (11163673, 'push', '8'), (11163676, 'call', '0x118b0e0'), (4437778, 'push', '4'), (4437783, 'sar', 'eax, 2'), (4437797, 'push', '4'), (4437799, 'lea', 'edx, [edi + 0x18]'), (4437824, 'push', '4'), (4437842, 'jne', '0x43b740'), (4452256, 'push', '4'), (4452275, 'push', '4'), (4452277, 'lea', 'edx, [esi + 0x18]'), (4452304, 'push', '1'), (4452312, 'mov', 'byte ptr [ebp + 0xb], 0'), (4452327, 'cmp', 'ecx, 0xffff'), (4452333, 'push', '2'), (4452382, 'push', '4'), (4452407, 'call', '0x118b0e0'), (4452412, 'push', '4'), (4452425, 'push', '4'), (4452457, 'jne', '0x43efd0'), (4447823, 'push', '4'), (4447839, 'push', '4'), (4447841, 'lea', 'edx, [edi + 0x18]'), (4447867, 'call', '0xaa5890'), (4447877, 'call', '0xaa5890'), (4447898, 'jne', '0x43de76'), (5590399, 'jmp', '0x554e44'), (5590606, 'call', '0x118ac90'), (18394256, 'mov', 'dword ptr [ecx + 4], 0'), (18394263, 'ret', ''), (4389038, 'mov', 'eax, 0x438e90'), (4389043, 'mov', 'dword ptr [edx + 8], eax')]
def audit(exe,packages):
 raw=exe.read_bytes();sha=hashlib.sha256(raw).hexdigest().upper()
 if sha!=EXPECTED:raise ValueError('Wrong executable SHA256; rejected before PE imports/VA use')
 sys.path.insert(0,str(packages));import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(data=raw,fast_load=True);read=lambda va,n:pe.get_data(va-pe.OPTIONAL_HEADER.ImageBase,n);md=Cs(CS_ARCH_X86,CS_MODE_32)
 for va,m,o in ANCHORS:
  i=next(md.disasm(read(va,15),va));assert (i.mnemonic,i.op_str)==(m,o),(hex(va),i.mnemonic,i.op_str)
 ctor=list(md.disasm(read(0x42ecf0,0xd0d),0x42ecf0));registered=[]
 for n,i in enumerate(ctor):
  if i.mnemonic=='call' and i.op_str=='0x555440':
   lead=ctor[n-3];assert lead.mnemonic=='lea';registered.append(lead.op_str)
 assert registered==[('edi' if ix==13 else 'eax')+f', [esi + 0x{off:x}]' for ix,off in enumerate(OFFSETS)]
 installed={};edi=None
 for i in ctor:
  m=re.fullmatch(r'edi, \[esi \+ 0x([0-9a-f]+)\]',i.op_str)
  if i.mnemonic=='lea' and m:edi=int(m[1],16)
  m=re.fullmatch(r'dword ptr \[edi\], 0x([0-9a-f]+)',i.op_str)
  if i.mnemonic=='mov' and m and edi is not None:installed[edi]=int(m[1],16)
  m=re.fullmatch(r'dword ptr \[esi \+ 0x([0-9a-f]+)\], 0x([0-9a-f]+)',i.op_str)
  if i.mnemonic=='mov' and m:installed[int(m[1],16)]=int(m[2],16)
 rows=[]
 for ix,off in enumerate(OFFSETS):
  vt,snap,delta,token=TYPES.get(ix,(0x15dcf0c,0x439b30,0x439b50,'AutoDeltaVariable@M'))
  slots=struct.unpack('<7I',read(vt,28));assert (slots[2],slots[6])==(snap,delta);assert slots[1]=={2:0x43b700,3:0x43ef90,6:0x440290,13:0x43de40}.get(ix,0x439ac0)
  col=struct.unpack('<I',read(vt-4,4))[0];td=struct.unpack('<I',read(col+12,4))[0];rtti=read(td+8,400).split(b'\0')[0].decode('ascii');assert token in rtti
  assert installed[off]==vt,(ix,off,installed.get(off))
  rows.append(dict(Index=ix,Member=f'{off:X}',Vtable=f'{vt:08X}',Snapshot=f'{snap:08X}',SnapshotWriter=f'{slots[1]:08X}',Delta=f'{delta:08X}',RTTI=rtti))
 skill=list(md.disasm(read(0x43f235,0x164),0x43f235));uses=[i for i in skill if '0x7c' in i.op_str];assert len(uses)==1 and uses[0].mnemonic=='lea' # raw snapshot command is consumed and never interpreted
 return dict(ExeSha256=sha,InstructionAnchors=len(ANCHORS),Registrations=14,DistinctVtables=5,Fields=rows,SkillSnapshotCommandIgnored=True,SnapshotDeclaredCountLoopBound=False,OriginalExecuted=False)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args();repo=Path(__file__).resolve().parents[3];assert a.receipt.resolve().is_relative_to((repo/'local-private').resolve())
 try:r=audit(a.exe,a.packages)
 except ValueError as e:p.exit(1,str(e)+'\n')
 a.receipt.write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8');print(f"PASS {r['InstructionAnchors']} direct anchors /14 registrations /5 RTTI vtables; static only")
