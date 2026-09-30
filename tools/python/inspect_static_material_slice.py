"""Print bounded independent shader/effect metadata from an explicit local TRE member."""
import argparse
import hashlib
import struct
from pathlib import Path
from iff_shader_structure import fetch
from verify_static_mesh_slice import nodes


def walk(node, stack=()):
    tag, value = node
    path = stack + (tag.decode("ascii"),)
    if isinstance(value, list):
        for child in value:
            walk(child, path)
    elif tag == b"MATL":
        print("/".join(path), "f32=" + str(struct.unpack("<17f", value)))
    elif tag == b"0000" and stack[-1] == "TCSS":
        print("/".join(path), "entries=" + str([(value[i:i+4][::-1].decode("ascii"), value[i+4]) for i in range(0, len(value), 5)]))
    elif tag == b"0000" and stack[-1] == "PVSH":
        print("/".join(path), "name=" + value.split(b"\0")[0].decode("ascii"))
    elif tag == b"DATA" and "PPSH" in stack:
        print("/".join(path), "count=" + str(value[0]), "name=" + value[1:].rstrip(b"\0").decode("ascii"))
    elif tag == b"0002" and stack[-1] == "PTXM":
        print("/".join(path), "bytes=" + value.hex())
    elif tag == b"0001" and stack[-1] == "PFFP":
        print("/".join(path), "fields=" + str(list(value)))
    elif tag == b"0000" and stack[-1] == "STAG":
        texture,coordinate=struct.unpack_from("<II",value,18)
        print("/".join(path), f"texture_tag={texture:08X} coordinate_tag={coordinate:08X} bytes=" + value.hex())


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("member")
    args = parser.parse_args()
    data = fetch(args.archive, args.member)
    print(f"member={args.member} bytes={len(data)} sha256={hashlib.sha256(data).hexdigest().upper()}")
    root, = nodes(data)
    walk(root)
