from __future__ import annotations

import pathlib
import struct
import sys


def chunks(data: bytes, start: int = 0, end: int | None = None):
    if end is None:
        end = len(data)
    offset = start
    while offset + 8 <= end:
        tag = data[offset : offset + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", data, offset + 4)[0]
        payload_start = offset + 8
        payload_end = payload_start + size
        yield tag, payload_start, payload_end
        # This SOE IFF corpus packs chunks back-to-back even for odd sizes.
        offset = payload_end


def main(path: pathlib.Path) -> None:
    data = path.read_bytes()
    top = list(chunks(data))
    assert len(top) == 1 and top[0][0] == "FORM"
    _, form_start, form_end = top[0]
    assert data[form_start : form_start + 4] == b"CSMV"
    version_form = list(chunks(data, form_start + 4, form_end))
    assert len(version_form) == 1 and version_form[0][0] == "FORM"
    _, version_start, version_end = version_form[0]
    version = data[version_start : version_start + 4].decode("ascii")
    print(f"version={version}")
    print("id\tasset\tduration")
    for tag, payload_start, payload_end in chunks(data, version_start + 4, version_end):
        assert tag == "MOVE"
        payload = data[payload_start:payload_end]
        move_id = payload[0]
        string_end = payload.index(0, 1)
        asset = payload[1:string_end].decode("ascii")
        duration = struct.unpack_from("<f", payload, string_end + 1)[0]
        assert string_end + 5 == len(payload)
        print(f"{move_id}\t{asset}\t{duration:g}")


if __name__ == "__main__":
    main(pathlib.Path(sys.argv[1]))
