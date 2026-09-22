import argparse
import pefile

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("--minimum", type=int, default=4)
a = p.parse_args()
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for section in pe.sections:
    data = section.get_data()
    start = None
    for index, value in enumerate(data + b"\0"):
        if 32 <= value < 127:
            if start is None:
                start = index
        elif start is not None:
            if index - start >= a.minimum:
                value_text = data[start:index].decode("ascii", errors="replace")
                print(f"{base + section.VirtualAddress + start:08X}\t{value_text}")
            start = None
