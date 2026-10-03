"""Hash-gated CachedNetworkId specialization audit. Static bytes only."""
import argparse,hashlib,json,struct,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('exe',type=Path);p.add_argument('--packages',type=Path,required=True);a=p.parse_args()
raw=a.exe.read_bytes();digest=hashlib.sha256(raw).hexdigest().upper()
assert digest=='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
sys.path.insert(0,str(a.packages))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
pe=pefile.PE(data=raw,fast_load=True);base=pe.OPTIONAL_HEADER.ImageBase
read=lambda va,n:pe.get_data(va-base,n)
md=Cs(CS_ARCH_X86,CS_MODE_32)
slots=struct.unpack('<8I',read(0x015E0614,32))
assert slots[1]==0x00641690 and slots[2]==0x006421A0 and slots[5]==0x00641700 and slots[6]==0x006423E0
col=struct.unpack('<I',read(0x015E0610,4))[0];td=struct.unpack('<I',read(col+12,4))[0]
rtti=read(td+8,200).split(b'\0')[0].decode()
assert rtti=='.?AV?$AutoDeltaVector@VCachedNetworkId@@UDefaultObjectType@Archive@@@Archive@@'
anchors=[(0x0063F079,'lea','eax, [esi + 0x2f0]'),(0x0063F082,'call','0x5554c0'),
 (0x00542DA9,'push','4'),(0x00542DC9,'push','4'),
 (0x006416A4,'push','4'),(0x006416B7,'push','4'),(0x006416DA,'push','8'),
 (0x00642241,'push','4'),(0x0064225E,'push','4'),(0x0064228E,'push','8'),(0x006422A3,'add','edx, 8'),(0x0064231A,'jb','0x642283'),
 (0x00642421,'push','4'),(0x00642435,'push','4'),(0x00642448,'sub','eax, ecx'),(0x0064244D,'add','eax, ecx'),(0x00642454,'jbe','0x64245b'),
 (0x00642470,'push','1'),(0x00642484,'push','2'),(0x00642494,'cmp','al, 3'),(0x006424B0,'inc','dword ptr [ebp - 0x1c]'),(0x006424B3,'push','8'),(0x00642502,'push','8'),(0x00642544,'inc','eax'),
 (0x00642560,'push','1'),(0x0064257A,'jmp','dword ptr [eax*4 + 0x642a00]'),
 (0x00642581,'push','2'),(0x00642673,'push','2'),(0x00642680,'push','8'),(0x00642756,'push','2'),(0x00642763,'push','8'),
 (0x00642891,'push','2'),(0x0064297A,'inc','ecx'),(0x0064297F,'call','0x43ceb0'),(0x0043CEEA,'push','8'),(0x0043CEFB,'add','ecx, 8'),
 (0x006429A3,'jb','0x642970'),(0x00642643,'inc','eax'),(0x00642649,'jb','0x642560'),(0x006429F6,'call','0x641850')]
for va,mnemonic,operand in anchors:
 i=next(md.disasm(read(va,15),va));assert (i.mnemonic,i.op_str)==(mnemonic,operand),(hex(va),i.mnemonic,i.op_str)
assert struct.unpack('<5I',read(0x00642A00,20))==(0x00642581,0x00642673,0x00642756,0x00642891,0x006429F4)
print(json.dumps(dict(ExeSha256=digest,InstructionAnchors=len(anchors),Vtable='015E0614',Rtti=rtti,
 SnapshotParse='006421A0',DeltaParse='006423E0',IdBytes=8,AssignmentConsumes='U16 N then N U64; raw loop advances N+1',
 AssignmentOvershoot='final assignment consumes every declared ID before outer count comparison',OriginalExecuted=False)))
