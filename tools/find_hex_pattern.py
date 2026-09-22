#!/usr/bin/env python3
"""Find raw byte patterns in a PE and report virtual addresses."""
import argparse
import pefile


parser = argparse.ArgumentParser()
parser.add_argument("image")
parser.add_argument("patterns", nargs="+")
args = parser.parse_args()

pe = pefile.PE(args.image, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
for value in args.patterns:
    needle = bytes.fromhex(value.replace("_", "").replace(" ", ""))
    for section in pe.sections:
        data = section.get_data()
        start = 0
        while True:
            offset = data.find(needle, start)
            if offset < 0:
                break
            print(f"{value}\t0x{base + section.VirtualAddress + offset:08X}\t{section.Name.rstrip(bytes([0])).decode('ascii', 'replace')}")
            start = offset + 1
