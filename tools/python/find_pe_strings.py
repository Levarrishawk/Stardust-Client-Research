import argparse, re, pefile
p=argparse.ArgumentParser(); p.add_argument('image'); p.add_argument('pattern'); p.add_argument('--min',type=int,default=4)
a=p.parse_args(); pe=pefile.PE(a.image,fast_load=True); base=pe.OPTIONAL_HEADER.ImageBase; rx=re.compile(a.pattern,re.I)
for sec in pe.sections:
    data=sec.get_data(); sva=base+sec.VirtualAddress
    for m in re.finditer(rb'[ -~]{4,}',data):
        s=m.group().decode('ascii','replace')
        if rx.search(s): print(f'{sva+m.start():08X}\t{s}')
