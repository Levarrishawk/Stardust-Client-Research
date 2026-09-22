import argparse
import sys
from pathlib import Path

WORKSPACE_ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(WORKSPACE_ROOT / "local-private" / "toolchain" / "python-static-analysis-packages"))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

p = argparse.ArgumentParser()
p.add_argument("exe")
p.add_argument("start", type=lambda s: int(s, 0))
p.add_argument("end", type=lambda s: int(s, 0))
a = p.parse_args()
pe = pefile.PE(a.exe, fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
rva = a.start - base
# Slice the PE's memory-mapped image.  pe.get_data() can return an empty span
# when a requested VA range crosses a raw-section boundary or virtual padding.
mapped = pe.get_memory_mapped_image()
data = mapped[rva:rva + (a.end - a.start)]
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = False
for ins in md.disasm(data, a.start):
    print(f"{ins.address:08X}\t{ins.mnemonic}\t{ins.op_str}")
