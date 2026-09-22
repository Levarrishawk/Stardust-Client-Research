import argparse, struct, re, pefile
p=argparse.ArgumentParser(); p.add_argument('image'); p.add_argument('pattern')
a=p.parse_args(); pe=pefile.PE(a.image,fast_load=True); base=pe.OPTIONAL_HEADER.ImageBase; size=pe.OPTIONAL_HEADER.SizeOfImage; rx=re.compile(a.pattern,re.I); data=pe.get_memory_mapped_image()
def u32(va):
    o=va-base
    if o<0 or o+4>len(data): raise ValueError()
    return struct.unpack_from('<I',data,o)[0]
def cstr(va):
    o=va-base; e=data.find(b'\0',o,o+512)
    if o<0 or e<0: raise ValueError()
    return data[o:e].decode('ascii','replace')
for sec in pe.sections:
    start=base+sec.VirtualAddress; end=start+max(sec.Misc_VirtualSize,sec.SizeOfRawData)
    for vt in range((start+3)&~3,end,4):
        try:
            col=u32(vt-4); td=u32(col+12); name=cstr(td+8); first=u32(vt)
            if base<=first<base+size and rx.search(name): print(f'{vt:08X}\t{col:08X}\t{td:08X}\t{name}')
        except Exception: pass
