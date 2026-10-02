"""Independently audit owned CKAT MSGS/LOCT rows against an effective original asset."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes


def verify_bank_routes(raw, locomotion_bank):
    selected_paths=set()
    assert len(locomotion_bank)==2 and {r["key"] for r in locomotion_bank}=={"walk","run"}
    for r in locomotion_bank:
        table,=nodes(raw[r['table']]);entries=table[1][0][1][1:]
        selected=next(e[1][1] for e in entries if e[1][0][1]==(r['name']+'\0').encode('ascii'))
        for child in r['route']:
            version,=selected[1];assert version[0]==b'0000'
            if selected[0]==b'SSAT':
                info,anms,*_=version[1];assert info[0]==b'INFO' and anms[0]==b'ANMS'
                count,=struct.unpack('<h',anms[1][0][1]);children=anms[1][1:]
            else:
                assert selected[0]==b'SPAT';info,*children=version[1];assert info[0]==b'INFO'
                count,=struct.unpack('<B',info[1])
            assert count==len(children) and 0<=child<count;selected=children[child]
        assert selected[0]==b'PXAT';version,=selected[1];info,*_=version[1];path=info[1][:-1].decode("ascii")
        assert path==r['path'] and r['sha256']==hashlib.sha256(raw[path]).hexdigest().upper()
        clip,=nodes(raw[path]);parts=dict(clip[1][0][1]);rate,end,transforms,*_=struct.unpack('<f6h',parts[b'INFO'])
        speed,count=struct.unpack_from('<fh',parts[b'LOCT'])
        assert r['FrameRate']==rate and r['EndFrame']==end and r['keys']==count and r['poses']==end*4+1
        assert struct.pack('<f',r['AuthoredSpeed'])==struct.pack('<f',speed)
        assert r['UnusedAnimationTransforms']==['polearm1_polearm','l0'] and r['MissingSkeletonTransforms']==['hold_l']
        assert r['joints']==53 and not r['root_motion_applied'] and not r['event_dispatch']
        selected_paths.add(path)

    return selected_paths


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive_root", type=Path)
    parser.add_argument("configuration", type=Path)
    parser.add_argument("member")
    parser.add_argument("native_rows", type=Path)
    parser.add_argument("--configuration-sha256", required=True)
    args = parser.parse_args()
    assert hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper() == args.configuration_sha256
    with contextlib.ExitStack() as stack:
        for priority, path in archives(args.archive_root, args.configuration):
            data = Index(stack.enter_context(path.open("rb"))).lookup(args.member)
            if data is not None:
                break
        else:
            raise ValueError("Missing effective original clip")
    records = {}
    for line in args.native_rows.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith(("locomotion_clip=", "locomotion_sample=")):
            tag, value = line.split("=", 1)
            records.setdefault(tag, []).append(json.loads(value))
    row, = records["locomotion_clip"]
    root, = nodes(data)
    assert root[0] == b"CKAT" and root[1][0][0] == b"0001"
    parts = dict(root[1][0][1])
    rate, end, transforms, *_ = struct.unpack("<f6h", parts[b"INFO"])
    speed, count = struct.unpack_from("<fh", parts[b"LOCT"])
    assert count >= 2 and len(parts[b"LOCT"]) == 6 + count * 14
    keys = [struct.unpack_from("<H3f", parts[b"LOCT"], 6 + i * 14) for i in range(count)]
    messages = []
    msgparts = parts.get(b"MSGS", [(b"INFO", b"\0\0")])
    assert struct.unpack("<H", msgparts[0][1])[0] == len(msgparts) - 1
    for tag, payload in msgparts[1:]:
        assert tag == b"MESG"
        occurrences, = struct.unpack_from("<H", payload)
        stop = payload.index(0, 2)
        frames = struct.unpack_from("<" + "H" * occurrences, payload, stop + 1)
        assert len(payload) == stop + 1 + occurrences * 2
        messages.append({"name": payload[2:stop].decode("ascii"), "frames": list(frames)})
    assert row["sha256"] == hashlib.sha256(data).hexdigest().upper() and row["bytes"] == len(data)
    assert row["FrameRate"] == rate and row["EndFrame"] == end and row["transforms"] == transforms
    same_float = lambda a, b: struct.pack("<f", a) == struct.pack("<f", b)
    assert same_float(row["AuthoredSpeed"], speed) and row["messages"] == messages
    assert len(row["keys"]) == count
    for actual, expected in zip(row["keys"], keys):
        assert actual["frame"] == expected[0]
        assert all(same_float(a, b) for a, b in zip(actual["position"], expected[1:]))
    samples = records["locomotion_sample"]
    assert len(samples) == end * 4 + 1
    maximum = 0
    for i, actual in enumerate(samples):
        frame = i / 4
        assert actual["frame"] == frame and actual["pose_transforms"] == transforms
        if frame <= keys[0][0]:
            expected = keys[0][1:]
        elif frame >= keys[-1][0]:
            expected = keys[-1][1:]
        else:
            lo = max(j for j, key in enumerate(keys) if key[0] <= frame)
            first, second = keys[lo:lo+2]
            fraction = (frame - first[0]) / (second[0] - first[0])
            expected = [a + (b-a)*fraction for a, b in zip(first[1:], second[1:])]
        maximum = max(maximum, *(abs(a-b) for a, b in zip(actual["position"], expected)))
    assert maximum <= 2e-6, maximum
    displacement = sum((b-a)**2 for a, b in zip(keys[0][1:], keys[-1][1:]))**.5
    derived_speed = displacement * rate / (keys[-1][0] - keys[0][0])
    assert abs(derived_speed - speed) < 1e-4
    print(json.dumps({"member": args.member, "priority": priority, "sha256": row["sha256"],
                      "frames": end, "fps": rate, "authored_speed": speed, "derived_speed": derived_speed,
                      "position_keys": count, "messages": messages, "samples": len(samples),
                      "max_error": maximum, "tolerance": 2e-6}))


if __name__ == "__main__":
    main()
