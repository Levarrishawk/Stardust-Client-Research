"""Verify the bounded SOE CRC gate directly in the documented executable.

Reads bytes only; never runs the executable. Detailed dumps stay private.
"""
import argparse
import hashlib
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "local-private/toolchain/python-static-analysis-packages"))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

BASELINE = "e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150"


def verify(executable):
    digest = hashlib.sha256(executable.read_bytes()).hexdigest()
    if digest != BASELINE:
        raise ValueError("Executable identity differs from the documented baseline")
    pe = pefile.PE(str(executable), fast_load=True)
    image = pe.get_memory_mapped_image()
    base = pe.OPTIONAL_HEADER.ImageBase
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    instructions = {i.address: (i.mnemonic, i.op_str) for i in decoder.disasm(
        image[0x012C383C - base:0x012C39A0 - base], 0x012C383C)}
    expected = {
        0x012C383E: ("cmp", "byte ptr [edi], 0"),
        0x012C3841: ("jne", "0x12c386a"),
        0x012C3846: ("cmp", "cl, 1"),
        0x012C3849: ("je", "0x12c3a2f"),
        0x012C384F: ("cmp", "cl, 2"),
        0x012C3852: ("je", "0x12c3a2f"),
        0x012C3858: ("cmp", "cl, 0x1d"),
        0x012C385B: ("je", "0x12c3a2f"),
        0x012C3861: ("cmp", "cl, 0x1e"),
        0x012C3864: ("je", "0x12c3a2f"),
        0x012C3875: ("mov", "ecx, dword ptr [esi + 0x120]"),
        0x012C3881: ("jle", "0x12c399b"),
        0x012C38B0: ("call", "0x12c8d30"),
        0x012C3925: ("cmp", "ecx, eax"),
        0x012C3927: ("je", "0x12c3991"),
        0x012C396A: ("je", "0x12c3a38"),
        0x012C3981: ("call", "dword ptr [edx + 0xc]"),
        0x012C398E: ("ret", "4"),
    }
    for address, instruction in expected.items():
        if instructions.get(address) != instruction:
            raise ValueError(f"Instruction guard failed at {address:08X}")
    pe.close()
    print("SHA256=" + digest.upper())
    print("PASS: 18 instruction guards; opcode 08 has no CRC bypass at positive trailer width")
    print("CRC mismatch exits after optional notification; receive transforms/dispatch are not reached")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    verify(parser.parse_args().executable)
