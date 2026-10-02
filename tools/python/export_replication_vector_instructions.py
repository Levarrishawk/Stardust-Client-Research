"""Export bounded pinned x86 instruction evidence to an ignored private directory."""
import argparse
import hashlib
import json
from pathlib import Path
import capstone
import pefile

SHA = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('exe',type=Path); p.add_argument('--range',action='append',required=True,help='Inclusive start:end VAs, hexadecimal')
    p.add_argument('--output',type=Path,required=True); a=p.parse_args()
    assert hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()==SHA,'Executable identity mismatch'
    ranges=[tuple(int(v,16) for v in text.split(':')) for text in a.range]
    assert all(0<=start<=end and end-start<16384 for start,end in ranges),'Extent exceeds bounded helper export'
    assert sum(end-start+1 for start,end in ranges)<=65536,'Total export budget exceeded'
    target=a.output.resolve(); assert 'local-private' in target.parts,'Derived instructions must remain private'
    pe=pefile.PE(str(a.exe),fast_load=True); md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32)
    result=dict(executable_sha256=SHA,image_base=pe.OPTIONAL_HEADER.ImageBase,capstone=capstone.__version__,pefile=pefile.__version__,ranges=[])
    for start,end in ranges:
        data=pe.get_data(start-pe.OPTIONAL_HEADER.ImageBase,end-start+1); assert len(data)==end-start+1
        rows=[dict(va=f'{i.address:08X}',bytes=i.bytes.hex().upper(),mnemonic=i.mnemonic,operands=i.op_str) for i in md.disasm(data,start)]
        result['ranges'].append(dict(start=f'{start:08X}',end=f'{end:08X}',sha256=hashlib.sha256(data).hexdigest(),raw_hex=data.hex().upper(),instructions=rows))
    target.parent.mkdir(parents=True,exist_ok=True); target.write_text(json.dumps(result,indent=2),encoding='utf-8',newline='\n')
    print(json.dumps(dict(executable_sha256=SHA,ranges=len(ranges),bytes=sum(e-s+1 for s,e in ranges),output=str(target))))


if __name__=='__main__': main()
