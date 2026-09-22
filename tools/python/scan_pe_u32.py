import argparse
import struct
import pefile

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("values", nargs="+", type=lambda s: int(s, 0))
a = p.parse_args()
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    data = section.get_data()
    section_va = base + section.VirtualAddress
    for value in a.values:
        needle = struct.pack("<I", value & 0xffffffff)
        start = 0
        while True:
            offset = data.find(needle, start)
            if offset < 0:
                break
            print(f"0x{section_va + offset:08X}\t0x{value & 0xffffffff:08X}\t{section.Name.rstrip(bytes([0])).decode('ascii', 'replace')}")
            start = offset + 1
