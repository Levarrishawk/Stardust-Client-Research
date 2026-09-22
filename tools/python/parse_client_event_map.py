from __future__ import annotations

import pathlib
import struct
import sys


def chunk(data: bytes, offset: int):
    tag = data[offset : offset + 4].decode("ascii")
    size = struct.unpack_from(">I", data, offset + 4)[0]
    return tag, offset + 8, offset + 8 + size


data = pathlib.Path(sys.argv[1]).read_bytes()
tag, start, end = chunk(data, 0)
assert tag == "FORM" and data[start : start + 4] == b"ESDM"
tag, start, end = chunk(data, start + 4)
assert tag == "FORM" and data[start : start + 4] == b"0000"
offset = start + 4
print("source\tdestination\teffect")
while offset < end:
    tag, payload_start, payload_end = chunk(data, offset)
    assert tag == "EMAP"
    fields = data[payload_start:payload_end].rstrip(b"\0").split(b"\0")
    assert len(fields) == 3
    print("\t".join(field.decode("ascii") for field in fields))
    offset = payload_end
