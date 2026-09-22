import argparse
import pefile

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("va", type=lambda s: int(s, 0))
p.add_argument("length", type=lambda s: int(s, 0))
a = p.parse_args()
pe = pefile.PE(a.exe, fast_load=True)
data = pe.get_data(a.va - pe.OPTIONAL_HEADER.ImageBase, a.length)
for i in range(0, len(data), 16):
    chunk = data[i:i+16]
    print(f"{a.va+i:08X}  {' '.join(f'{b:02X}' for b in chunk)}")
