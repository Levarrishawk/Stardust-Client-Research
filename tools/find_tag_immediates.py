#!/usr/bin/env python3
"""Locate x86 little-endian immediates for selected four-character IFF tags."""
import struct, sys
from pathlib import Path

data=Path(sys.argv[1]).read_bytes()
peoff=struct.unpack_from('<I',data,0x3c)[0]
nsec=struct.unpack_from('<H',data,peoff+6)[0]
optsz=struct.unpack_from('<H',data,peoff+20)[0]
imagebase=struct.unpack_from('<I',data,peoff+24+28)[0]
sections=[]; sh=peoff+24+optsz
for i in range(nsec):
    p=sh+i*40; name=data[p:p+8].split(b'\0')[0].decode('ascii','replace')
    vsize,va,rsize,roff=struct.unpack_from('<IIII',data,p+8)
    sections.append((name,roff,roff+rsize,imagebase+va))
for tag in sys.argv[2:]:
    value=int.from_bytes(tag.encode('ascii'),'big')
    needle=struct.pack('<I',value)
    start=0
    while True:
        off=data.find(needle,start)
        if off<0: break
        desc=f'file={off:#x}'
        for name,lo,hi,base in sections:
            if lo<=off<hi: desc=f'VA={base+off-lo:#010x} section={name} file={off:#x}'; break
        print(tag,desc)
        start=off+1
