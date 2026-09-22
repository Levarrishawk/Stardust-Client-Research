import argparse
import struct
import pefile

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("address", type=lambda s: int(s, 0))
p.add_argument("count", type=int)
a = p.parse_args()
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = pe.get_data(a.address - base, a.count * 4)
for i in range(0, len(data), 4):
    if i + 4 <= len(data):
        print(f"0x{a.address+i:08X}\t0x{struct.unpack_from('<I', data, i)[0]:08X}")
