import argparse, csv, struct
import pefile

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("inventory")
p.add_argument("vtable", type=lambda s: int(s, 0))
p.add_argument("count", type=int)
a = p.parse_args()
names = {}
with open(a.inventory, encoding="utf-8") as f:
    for row in csv.DictReader(f, delimiter="\t"):
        names[int(row["entry"], 16)] = row["name"]
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = pe.get_data(a.vtable - base, a.count * 4)
for index in range(a.count):
    value = struct.unpack_from("<I", data, index * 4)[0]
    print(f"{a.vtable + index * 4:08X}\t{value:08X}\t{names.get(value, '')}")
