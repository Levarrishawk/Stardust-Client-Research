from __future__ import annotations

import collections
import pathlib
import struct
import sys


def read_chunk(data: bytes, offset: int):
    tag = data[offset : offset + 4].decode("ascii", "replace")
    size = struct.unpack_from(">I", data, offset + 4)[0]
    return tag, offset + 8, offset + 8 + size


data = pathlib.Path(sys.argv[1]).read_bytes()
tag, start, end = read_chunk(data, 0)
assert tag == "FORM" and data[start : start + 4] == b"CLDF"
tag, start, end = read_chunk(data, start + 4)
assert tag == "FORM"
version = data[start : start + 4].decode("ascii")
print(f"version={version}")
offset = start + 4
types: collections.Counter[str] = collections.Counter()
rows = []
while offset < end:
    tag, payload_start, payload_end = read_chunk(data, offset)
    payload = data[payload_start:payload_end]
    if tag == "EVNT":
        fields = payload.rstrip(b"\0").split(b"\0")
        assert len(fields) == 2
        event, resource = (field.decode("ascii") for field in fields)
        suffix = pathlib.PurePosixPath(resource.lower()).suffix or "<none>"
        types[suffix] += 1
        rows.append((event, resource))
    offset = payload_end
print(f"events={len(rows)} resource_types={dict(types)}")
for event, resource in rows:
    print(f"{event}\t{resource}")
