import argparse, struct, pefile
p=argparse.ArgumentParser(); p.add_argument('image'); p.add_argument('addresses', nargs='+')
a=p.parse_args(); pe=pefile.PE(a.image, fast_load=True); base=pe.OPTIONAL_HEADER.ImageBase
for s in a.addresses:
    va=int(s,0); raw=pe.get_data(va-base,8)
    print(f'{va:08X}\t{raw.hex()}\tu32={struct.unpack_from("<I",raw)[0]}\tf32={struct.unpack_from("<f",raw)[0]:.12g}\tf64={struct.unpack_from("<d",raw)[0]:.12g}')
