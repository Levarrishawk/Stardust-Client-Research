"""Static fresh-margin and worker-record anchors; original executable is never run."""
import argparse,json
from pathlib import Path
from verify_terrain_sample_coordinates import verify

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--exe',type=Path,required=True);p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
 if 'local-private' not in a.receipt.parts:p.error('Private receipt required')
 inherited=verify(a.exe,a.packages)
 import pefile
 from capstone import Cs,CS_ARCH_X86,CS_MODE_32
 pe=pefile.PE(str(a.exe),fast_load=True);mapped=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase;md=Cs(CS_ARCH_X86,CS_MODE_32);rows={}
 for start,end in [(0x849350,0x849400),(0xB5D7C0,0xB5D9D9),(0x85EE60,0x85EFC9),(0x84A530,0x84A7A3)]:
  rows.update({i.address:(i.mnemonic,i.op_str) for i in md.disasm(mapped[start-base:end-base],start)})
 anchors={
  0x84937C:('call','0xb5d7c0'),0xB5D7F1:('mov','dword ptr [esi + 0x58], 3'),0xB5D7F8:('mov','dword ptr [esi + 0x5c], 2'),
  0xB5D970:('mov','edi, dword ptr [esi + 0x58]'),0xB5D973:('add','edi, dword ptr [ebp + 8]'),0xB5D976:('shl','edi, 1'),
  0x85EECA:('lea','esi, [eax + 0x14]'),0x85EECD:('mov','ecx, 5'),0x85EED2:('lea','edi, [ebp - 0x2c]'),0x85EED8:('rep movsd','dword ptr es:[edi], dword ptr [esi]'),
  0x85EF28:('mov','ecx, 5'),0x85EF32:('rep movsd','dword ptr es:[edi], dword ptr [esi]'),0x85EF36:('add','dword ptr [eax + 4], 0x14'),
  0x85EF5A:('mov','edx, dword ptr [ebp - 0x24]'),0x85EF5D:('mov','eax, dword ptr [ebp - 0x28]'),0x85EF60:('mov','ecx, dword ptr [ebp - 0x2c]'),
  0x85EF64:('push','edx'),0x85EF65:('push','eax'),0x85EF66:('push','ecx'),0x85EF69:('call','0x84a530'),0x85EF84:('mov','dword ptr [eax - 4], ecx'),
  0x84A782:('mov','edx, dword ptr [esi + 0x5c]'),0x84A798:('mov','dword ptr [ebp - 0x60c], edx'),
 }
 for va,want in anchors.items():
  if rows.get(va)!=want:raise ValueError(f'{va:08X}: {rows.get(va)} != {want}')
 result={**inherited,'RequestInstructionAnchors':len(anchors),'FreshMember58':3,'FreshMember5C':2,'RecordBytes':20,'LaterMutationAudit':'Not performed','ActualWorkRecordCapture':False}
 a.receipt.parent.mkdir(parents=True,exist_ok=True);a.receipt.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
if __name__=='__main__':main()
