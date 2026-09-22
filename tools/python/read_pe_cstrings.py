import argparse
import pefile

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("addresses", nargs="+", type=lambda s: int(s, 0))
a = p.parse_args()
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for va in a.addresses:
    raw = pe.get_data(va - base, 1024)
    value = raw.split(b"\0", 1)[0].decode("latin-1", errors="replace")
    print(f"0x{va:08X}\t{value}")
