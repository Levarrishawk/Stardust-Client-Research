"""Independent read-only reference for the bounded 1105 static-mesh slice.

Prints compact identity/count/hash metadata; never saves original asset bytes.
"""
import argparse
import hashlib
import math
import struct
from pathlib import Path
from scan_tre_sound_references import archive_records, expand


def nodes(data, start=0, end=None, depth=0):
    end = len(data) if end is None else end
    if depth > 32:
        raise ValueError("IFF depth limit")
    result = []
    while start < end:
        if end - start < 8:
            raise ValueError("Truncated IFF node")
        tag, size = data[start:start + 4], struct.unpack_from(">I", data, start + 4)[0]
        stop = start + 8 + size
        if stop > end:
            raise ValueError("IFF child exceeds parent")
        if tag == b"FORM":
            if size < 4:
                raise ValueError("FORM lacks type")
            result.append((data[start + 8:start + 12], nodes(data, start + 12, stop, depth + 1)))
        else:
            result.append((tag, data[start + 8:stop]))
        start = stop  # deliberately no padding
    return result


def inspect(data):
    root, = nodes(data)
    assert root[0] == b"MESH"
    version, = root[1]
    assert version[0] in (b"0004", b"0005")
    appearance, sps = version[1]
    assert appearance[0] == b"APPR" and sps[0] == b"SPS "
    sps_version, = sps[1]
    assert sps_version[0] == b"0001"
    count, *groups = sps_version[1]
    assert count[0] == b"CNT " and struct.unpack("<I", count[1])[0] == len(groups)
    geometry = bytearray()
    sorted_geometry=bytearray();sorted_variants=0
    vertices = indices = batches = 0
    shaders = []
    for _, children in groups:
        name, info, *primitives = children
        assert name[0] == b"NAME" and name[1][-1] == 0 and info[0] == b"INFO"
        shaders.append(name[1][:-1].decode("ascii"))
        assert struct.unpack("<I", info[1])[0] == len(primitives)
        for primitive_version, children in primitives:
            assert primitive_version in (b"0000", b"0001")
            info, vtxa, indx, *sorted_chunks = children
            topology,indexed,sorted_flag=struct.unpack("<IBB",info[1])
            assert info[0] == b"INFO" and topology==9 and indexed==1 and sorted_flag in (0,1) and len(sorted_chunks)==sorted_flag
            assert vtxa[0] == b"VTXA"
            vtx_version, = vtxa[1]
            assert vtx_version[0] == b"0003"
            vinfo, vdata = vtx_version[1]
            declaration, vertex_count = struct.unpack("<II", vinfo[1])
            assert vinfo[0] == b"INFO" and vdata[0] == b"DATA" and declaration in (0x1105,0x55405,0xD205,0xD5405,0x5205,0x35305,0x110D)
            components = 6 + sum(((declaration>>(12+2*s)) & 3)+1 for s in range((declaration>>8)&15))
            stride=components*4+(4 if declaration==0x110D else 0)
            assert len(vdata[1]) == vertex_count * stride
            for vi in range(vertex_count):
                raw=vdata[1][vi*stride:(vi+1)*stride]
                floats=raw[:24]+raw[28:] if declaration==0x110D else raw
                assert all(math.isfinite(v) for v in struct.unpack("<" + "f" * components, floats))
            assert indx[0] == b"INDX"
            index_count, = struct.unpack_from("<I", indx[1])
            width = 4 if primitive_version == b"0000" else 2
            assert index_count % 3 == 0 and len(indx[1]) == 4 + index_count * width
            format = "<" + ("I" if width == 4 else "H") * index_count
            values = [value & 65535 for value in struct.unpack(format, indx[1][4:])]
            assert all(value < vertex_count for value in values)
            geometry.extend(struct.pack("<II", vertex_count, index_count))
            geometry.extend(vdata[1])
            geometry.extend(struct.pack("<" + "H" * index_count, *values))
            if sorted_chunks:
                tag,payload=sorted_chunks[0];assert tag==b'SIDX'
                count,=struct.unpack_from('<I',payload);cursor=4
                sorted_variants+=count;sorted_geometry.extend(struct.pack('<I',count))
                for _ in range(count):
                    direction=struct.unpack_from('<3f',payload,cursor);cursor+=12
                    assert all(math.isfinite(v) for v in direction)
                    entries,=struct.unpack_from('<I',payload,cursor);cursor+=4
                    assert entries%3==0
                    raw=struct.unpack_from('<'+('I' if width==4 else 'H')*entries,payload,cursor);cursor+=entries*width
                    variant=[v & 65535 for v in raw];assert all(v<vertex_count for v in variant)
                    sorted_geometry.extend(struct.pack('<3fI',*direction,entries));sorted_geometry.extend(struct.pack('<'+'H'*entries,*variant))
                assert cursor==len(payload)
            else:sorted_geometry.extend(struct.pack('<I',0))
            vertices += vertex_count; indices += index_count; batches += 1
    return {"mesh_version": version[0].decode(), "batches": batches, "vertices": vertices,
            "triangles": indices // 3, "shader_dependencies": len(set(shaders)),
            "geometry_sha256": hashlib.sha256(geometry).hexdigest().upper(),"sorted_variants":sorted_variants,
            "sorted_indices_sha256":hashlib.sha256(sorted_geometry).hexdigest().upper()}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("member")
    parser.add_argument("expected_sha256")
    args = parser.parse_args()
    wanted = args.member.lower().replace("\\", "/")
    matches = [(usize, kind, packed) for name, usize, kind, packed in archive_records(args.archive) if name == wanted]
    if len(matches) != 1 or matches[0][0] == 0:
        raise ValueError("Expected one live member")
    usize, kind, packed = matches[0]
    data = expand(kind, packed, usize)
    identity = hashlib.sha256(data).hexdigest().upper()
    if identity != args.expected_sha256.upper():
        raise ValueError("Member identity mismatch")
    print("member_sha256=" + identity)
    for key, value in inspect(data).items():
        print(f"{key}={value}")
