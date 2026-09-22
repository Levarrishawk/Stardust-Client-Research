import argparse
import pefile

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("pattern", nargs="?", default="")
a = p.parse_args()
pe = pefile.PE(a.exe)
needle = a.pattern.lower()
for module in getattr(pe, "DIRECTORY_ENTRY_IMPORT", []):
    dll = module.dll.decode("latin-1", errors="replace")
    for entry in module.imports:
        name = entry.name.decode("latin-1", errors="replace") if entry.name else f"ordinal_{entry.ordinal}"
        if needle in dll.lower() or needle in name.lower():
            print(f"0x{entry.address:08X}\t{dll}\t{name}")
