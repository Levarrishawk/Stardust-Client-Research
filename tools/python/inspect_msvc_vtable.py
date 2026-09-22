import argparse
import struct

import pefile


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("vtables", nargs="+", type=lambda value: int(value, 0))
args = parser.parse_args()

pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase


def read_va(va: int, size: int) -> bytes:
    return pe.get_data(va - base, size)


for vtable in args.vtables:
    locator = struct.unpack("<I", read_va(vtable - 4, 4))[0]
    signature, offset, cd_offset, type_descriptor, hierarchy = struct.unpack(
        "<IIIII", read_va(locator, 20)
    )
    raw_name = read_va(type_descriptor + 8, 512).split(b"\0", 1)[0]
    print(
        f"vtable=0x{vtable:08X}\tlocator=0x{locator:08X}\t"
        f"offset={offset}\ttype=0x{type_descriptor:08X}\t"
        f"name={raw_name.decode('ascii', errors='replace')}"
    )
