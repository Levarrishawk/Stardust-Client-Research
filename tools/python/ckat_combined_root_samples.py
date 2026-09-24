"""List effective original CKAT clips containing both root QCHN and LOCT."""

import pathlib
import math
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/python"))
from pst_reference_audit import configured_archives
from scan_tre_sound_references import archive_records, expand


def children(blob, start, end):
    pos = start
    while pos + 8 <= end:
        tag = blob[pos:pos + 4]
        size = struct.unpack_from(">I", blob, pos + 4)[0]
        stop = pos + 8 + size
        if stop > end:
            raise ValueError("chunk overrun")
        yield tag, pos + 8, stop
        pos = stop
    if pos != end:
        raise ValueError("trailing")


def main():
    root = pathlib.Path(sys.argv[1])
    seen = set()
    for _, archive in configured_archives(root, root / "swgemu_live.cfg"):
        for path, usize, kind, packed in archive_records(archive):
            if path in seen:
                continue
            seen.add(path)
            if not path.endswith(".ans") or not usize:
                continue
            data = expand(kind, packed, usize)
            if data[8:12] != b"CKAT" or data[20:24] != b"0001":
                continue
            chunks = {tag: (a, b) for tag, a, b in children(data, 24, len(data)) if tag in (b"QCHN", b"LOCT")}
            if len(chunks) != 2:
                continue
            qa, qb = chunks[b"QCHN"]
            la, lb = chunks[b"LOCT"]
            qcount = struct.unpack_from("<H", data, qa)[0]
            lcount = struct.unpack_from("<h", data, la + 4)[0]
            first_time = struct.unpack_from("<H", data, la + 6)[0]
            first_xyz = struct.unpack_from("<3f", data, la + 8)
            last_at = la + 6 + 14 * (lcount - 1)
            last_time = struct.unpack_from("<H", data, last_at)[0]
            last_xyz = struct.unpack_from("<3f", data, last_at + 2)
            positions = [(struct.unpack_from("<H", data, la + 6 + 14 * i)[0], struct.unpack_from("<3f", data, la + 8 + 14 * i)) for i in range(lcount)]
            maximum = max(positions, key=lambda pair: math.sqrt(sum(x * x for x in pair[1])))
            print(path, "QCHN", qcount, "LOCT", lcount, "time", first_time, last_time, "positions", first_xyz, last_xyz, "max_offset", maximum)


if __name__ == "__main__":
    main()
