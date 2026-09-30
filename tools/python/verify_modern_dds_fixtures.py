"""Author synthetic DDS fixtures independently, then compare native pixels to Pillow/raw-channel references."""
import argparse
import struct
import subprocess
import sys
from pathlib import Path


def header(fourcc, payload, dxgi=None):
    output = bytearray(148 if dxgi is not None else 128)
    output[:4] = b"DDS "
    for offset, value in ((4,124),(8,0x1007),(12,4),(16,4),(28,1),(76,32),(80,4),(108,0x1000)):
        struct.pack_into("<I",output,offset,value)
    output[84:88] = b"DX10" if dxgi is not None else fourcc
    if dxgi is not None: struct.pack_into("<5I",output,128,dxgi,3,0,1,2)
    return bytes(output)+payload


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output",type=Path)
    parser.add_argument("core_project",type=Path)
    args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    color=struct.pack("<HHI",0xF800,0x001F,0xE4E4E4E4)
    channel=bytes.fromhex("00ff88c6fa88c6fa")
    fixtures={71:color,72:color,74:bytes.fromhex("1032547698badcfe")+color,
        77:channel+color,80:channel,83:channel+bytes.fromhex("ff0088c6fa88c6fa"),
        28:bytes(range(64)),29:bytes(range(64)),87:bytes(range(64)),91:bytes(range(64)),
        61:bytes(range(16)),49:bytes(range(32))}
    for dxgi,payload in fixtures.items():
        target=args.output/f"synthetic_dxgi_{dxgi}.dds"
        content=header(b"DX10",payload,dxgi)
        if target.exists():
            if target.read_bytes()!=content: raise ValueError("Existing synthetic fixture differs; refusing replacement")
        else:
            with target.open("xb") as handle: handle.write(content)
        subprocess.run([sys.executable,str(Path(__file__).with_name("verify_dds_pixels.py")),str(target),
            "--core-project",str(args.core_project)],check=True,timeout=120)
    for tag,payload in ((b"ATI1",channel),(b"ATI2",channel+channel),(b"BC4U",channel),(b"BC5U",channel+channel)):
        target=args.output/f"synthetic_{tag.decode()}.dds"
        content=header(tag,payload)
        if target.exists():
            if target.read_bytes()!=content: raise ValueError("Existing synthetic fixture differs; refusing replacement")
        else:
            with target.open("xb") as handle: handle.write(content)
        subprocess.run([sys.executable,str(Path(__file__).with_name("verify_dds_pixels.py")),str(target),
            "--core-project",str(args.core_project)],check=True,timeout=120)
    print(f"PASS: {len(fixtures)+4} independently authored modern/ATI fixtures")


if __name__ == "__main__": main()
