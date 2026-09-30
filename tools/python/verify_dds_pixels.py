"""Independently decode each original DDS mip using Pillow; print identities and RGBA hashes only."""
import argparse
import hashlib
import io
import struct
import re
import subprocess
from pathlib import Path
from PIL import Image
from iff_shader_structure import fetch


def inspect(data):
    if data[:4] != b"DDS " or len(data) < 128:
        raise ValueError("Invalid DDS envelope")
    height, width = struct.unpack_from("<II", data, 12)
    count = max(1, struct.unpack_from("<I", data, 28)[0])
    fourcc = data[84:88]
    offset = 128
    if fourcc == b"DX10":
        dxgi, dimension, misc, array, alpha = struct.unpack_from("<5I", data, 128)
        if (dimension, misc, array) != (3, 0, 1):
            raise ValueError("Reference probe expects one 2D surface")
        fourcc = {71:b"DXT1",72:b"DXT1",74:b"DXT3",75:b"DXT3",77:b"DXT5",78:b"DXT5",
            80:b"ATI1",83:b"ATI2",98:b"BC7 ",99:b"BC7 ",28:b"RGBA",29:b"RGBA",87:b"BGRA",91:b"BGRA",61:b"R8  ",49:b"RG8 "}[dxgi]
        offset = 148
    elif struct.unpack_from("<I",data,80)[0] == 0x41 and struct.unpack_from("<5I",data,88) == (32,0xFF0000,0xFF00,0xFF,0xFF000000):
        fourcc = b"BGRA"
    elif fourcc == b"BC4U": fourcc = b"ATI1"
    elif fourcc == b"BC5U": fourcc = b"ATI2"
    if fourcc not in (b"DXT1", b"DXT3", b"DXT5", b"ATI1", b"ATI2", b"RGBA", b"BGRA", b"R8  ", b"RG8 ", b"BC7 "):
        raise ValueError("Unsupported reference pixel format")
    hashes = []
    print(f"format={fourcc.decode()} width={width} height={height} mips={count}")
    for level in range(count):
        w, h = max(1, width >> level), max(1, height >> level)
        pixel_width = {b"RGBA":4,b"BGRA":4,b"R8  ":1,b"RG8 ":2}.get(fourcc)
        size = w*h*pixel_width if pixel_width else ((w + 3) // 4) * ((h + 3) // 4) * (8 if fourcc in (b"DXT1",b"ATI1") else 16)
        header = bytearray(data[:148] if fourcc==b"BC7 " else data[:128])
        struct.pack_into("<II", header, 12, h, w)
        struct.pack_into("<I", header, 20, w*pixel_width if pixel_width else size)
        struct.pack_into("<I", header, 28, 1)
        payload = data[offset:offset + size]
        if fourcc == b"RG8 ":
            rgba = bytes(value for i in range(0,len(payload),2) for value in (payload[i],payload[i+1],0,255))
        else:
            if fourcc in (b"RGBA",b"BGRA"):
                red, blue = (0xFF,0xFF0000) if fourcc == b"RGBA" else (0xFF0000,0xFF)
                struct.pack_into("<8I",header,76,32,0x41,0,32,red,0xFF00,blue,0xFF000000)
            elif fourcc == b"BC7 ": pass
            elif fourcc == b"R8  ": struct.pack_into("<8I",header,76,32,0x20000,0,8,0xFF,0,0,0)
            else:
                struct.pack_into("<I",header,80,4); header[84:88]=fourcc
            mip = bytes(header) + payload
            with Image.open(io.BytesIO(mip)) as image:
                if fourcc in (b"ATI1",b"R8  "):
                    rgba = bytes(value for red in image.convert("L").tobytes() for value in (red,0,0,255))
                elif fourcc == b"ATI2":
                    rgb = image.convert("RGB").tobytes()
                    rgba = bytes(value for i in range(0,len(rgb),3) for value in (rgb[i],rgb[i+1],0,255))
                else: rgba = image.convert("RGBA").tobytes()
        if fourcc==b"BC7 " and any(payload[block]==0 for block in range(0,len(payload),16)):
            print("pillow_reserved_rgba_sha256="+hashlib.sha256(rgba).hexdigest().upper())
            corrected=bytearray(rgba)
            for block in range(len(payload)//16):
                if payload[block*16]!=0:continue
                bx=block%((w+3)//4);by=block//((w+3)//4)
                for y in range(by*4,min(by*4+4,h)):
                    for x in range(bx*4,min(bx*4+4,w)):
                        corrected[(y*w+x)*4:(y*w+x+1)*4]=bytes(4)
            rgba=bytes(corrected)
            print("reference_policy=reserved_BC7_zero_RGBA_per_format_specification")
        digest = hashlib.sha256(rgba).hexdigest().upper()
        hashes.append(digest)
        print(f"mip={level} width={w} height={h} rgba_sha256={digest}")
        offset += size
    if offset != len(data):
        raise ValueError("DDS declared mip length mismatch")
    return hashes


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("member", nargs="?")
    parser.add_argument("--expected-sha256")
    parser.add_argument("--core-project", type=Path, help="Run the native probe and require every mip hash to match")
    args = parser.parse_args()
    data = fetch(args.archive, args.member) if args.member else args.archive.read_bytes()
    identity = hashlib.sha256(data).hexdigest().upper()
    if args.expected_sha256 and identity != args.expected_sha256.upper():
        raise ValueError("DDS member identity mismatch")
    print(f"member={args.member} bytes={len(data)} sha256={identity}")
    expected = inspect(data)
    if args.core_project:
        probe = ["--dds-tre",str(args.archive),"--member",args.member] if args.member else ["--dds-file",str(args.archive)]
        result = subprocess.run(["dotnet", "run", "--no-restore", "--project", str(args.core_project), "--",
            *probe, "--expected-sha256", identity],
            capture_output=True, text=True, timeout=120, check=True)
        actual = re.findall(r"^mip=\d+ rgba_sha256=([0-9A-F]{64})$", result.stdout, re.MULTILINE)
        if actual != expected:
            raise ValueError("Native/Pillow mip pixels differ")
        print(f"PASS: native/reference pixels agree for all {len(expected)} mip levels (Pillow or raw RG channel reference)")
