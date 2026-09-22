#!/usr/bin/env python3
"""Print the compact header contract of an SWG DTI IFF table."""
from pathlib import Path
import struct, sys

for value in (item for item in sys.argv[1:] if item != "--rows"):
    path = Path(value)
    raw = path.read_bytes()
    cols_at = raw.find(b"COLS")
    rows_at = raw.find(b"ROWS")
    if min(cols_at, rows_at) < 0:
        print(f"{path}: missing COLS/TYPE/ROWS")
        continue
    col_size = struct.unpack_from(">I", raw, cols_at + 4)[0]
    type_at = cols_at + 8 + col_size
    if raw[type_at:type_at + 4] != b"TYPE":
        print(f"{path}: TYPE does not follow COLS")
        continue
    col_payload = raw[cols_at + 8:cols_at + 8 + col_size]
    col_count = struct.unpack_from("<I", col_payload, 0)[0]
    names = col_payload[4:].split(b"\0")[:col_count]
    type_size = struct.unpack_from(">I", raw, type_at + 4)[0]
    types = raw[type_at + 8:type_at + 8 + type_size].split(b"\0")
    row_count = struct.unpack_from("<I", raw, rows_at + 8)[0]
    print(f"{path}: columns={col_count} rows={row_count}")
    for index, name in enumerate(names):
        code = types[index].decode("ascii", "replace") if index < len(types) else "?"
        print(f"  {index:02d} {name.decode('ascii', 'replace')} [{code}]")
    if "--rows" in sys.argv and all(t.startswith((b"s", b"b")) for t in types[:col_count]):
        cursor = rows_at + 12
        for row_index in range(row_count):
            values = []
            for spec in types[:col_count]:
                if spec.startswith(b"s"):
                    end = raw.index(b"\0", cursor)
                    values.append(raw[cursor:end].decode("ascii", "replace"))
                    cursor = end + 1
                else:
                    values.append(str(struct.unpack_from("<I", raw, cursor)[0]))
                    cursor += 4
            print(f"  row {row_index}: " + " | ".join(values))
