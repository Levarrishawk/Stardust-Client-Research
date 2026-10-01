"""Audit a bounded object-root SMAT/MLOD descriptor probe against effective original bytes."""
import argparse
import contextlib
import collections
import hashlib
import json
import math
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_creature_template_profile import parse


def strings(data, count):
    result = data.split(b'\0')
    assert len(result) == count+1 and result[-1] == b''
    return [v.decode('ascii') for v in result[:-1]]


def sat(data):
    root, = nodes(data); assert root[0] == b'SMAT'
    version, = root[1]; assert version[0] == b'0003'
    parts = dict(version[1]); assert len(parts) == len(version[1])
    mc, sc, flag = struct.unpack('<IIB', parts.pop(b'INFO'))
    mesh = strings(parts.pop(b'MSGN'), mc); sk = strings(parts.pop(b'SKTI'), sc*2)
    tables = []
    if b'LATX' in parts:
        raw = parts.pop(b'LATX'); count, = struct.unpack_from('<H', raw)
        pairs = strings(raw[2:], count*2)
        tables = [dict(SkeletonPath=pairs[i*2], AnimationTablePath=pairs[i*2+1]) for i in range(count)]
    intervals = []
    if b'LDTB' in parts:
        ver, = parts.pop(b'LDTB'); assert ver[0] == b'0000'
        info, = ver[1]; assert info[0] == b'INFO'
        count, = struct.unpack_from('<H', info[1]); assert len(info[1]) == 2+8*count
        intervals = [dict(Lower=l, Upper=u) for l, u in struct.iter_unpack('<2f', info[1][2:])]
    sfsk = bool(parts.pop(b'SFSK')[0]) if b'SFSK' in parts else None
    apag = bool(parts.pop(b'APAG')[0]) if b'APAG' in parts else None
    assert not parts
    return dict(InfoFlag=bool(flag), MeshGenerators=mesh,
                Skeletons=[dict(Path=sk[i*2], AttachmentTransform=sk[i*2+1]) for i in range(sc)],
                AnimationTables=tables, DistanceIntervals=intervals, Sfsk=sfsk, Apag=apag)


def mlod(data):
    root, = nodes(data); assert root[0] == b'MLOD'
    version, = root[1]; assert version[0] == b'0000'
    info, *names = version[1]; assert info[0] == b'INFO'
    count = max(0, struct.unpack('<h', info[1])[0]); assert len(names) == count
    assert all(n[0] == b'NAME' for n in names)
    return dict(SerializedPaths=[strings(n[1], 1)[0] for n in names])


def logical_table(data):
    root, = nodes(data); assert root[0] == b'LATT'; version, = root[1]; assert version[0] == b'0000'
    info, *entries = version[1]; assert info[0] == b'INFO'; end = info[1].index(0)+1
    hierarchy, = strings(info[1][:end], 1); count, = struct.unpack('<h', info[1][end:]); assert count == len(entries) and count >= 0
    def text(value): return value.encode('ascii')+b'\0'
    def encoded(node):
        tag, payload = node
        if isinstance(payload, list):
            children = b''.join(encoded(child) for child in payload); raw = tag+children
            return b'FORM'+struct.pack('>I', len(raw))+raw
        return tag+struct.pack('>I', len(payload))+payload
    def canonical_node(node):
        tag, payload = node
        if isinstance(payload, list):
            raw = b''.join(encoded(child) for child in payload)
            return b'FORM\0'+tag+b'\0'+struct.pack('<i', len(raw))+raw+struct.pack('<i', len(payload))+b''.join(canonical_node(child) for child in payload)
        return tag+b'\0\0'+struct.pack('<i', len(payload))+payload+struct.pack('<i', 0)
    canonical = text(hierarchy)+struct.pack('<i', count); names, families = [], collections.Counter()
    for tag, entry in entries:
        assert tag == b'ANIM' and len(entry) == 2 and entry[0][0] == b'INFO' and isinstance(entry[1][1], list)
        raw, = strings(entry[0][1], 1); name = raw.rstrip(' '); assert name
        names.append(name); families[entry[1][0].decode('ascii')] += 1
        canonical += text(raw)+text(name)+canonical_node(entry[1])
    synthetic = 'default' not in names; canonical += bytes([synthetic])
    return dict(AssociatedHierarchy=hierarchy, entries=count, lookup=count+int(synthetic), synthetic_default=synthetic,
                duplicates=sum(c > 1 for c in collections.Counter(n.lower() for n in names).values()),
                families=dict(families), sha256=hashlib.sha256(canonical).hexdigest().upper())


def compressed_clip(data):
    root, = nodes(data); assert root[0] == b'CKAT'
    version, = root[1]; assert version[0] == b'0001'
    parts = version[1]; assert [tag for tag, _ in parts] == [b'INFO', b'XFRM', b'AROT', b'SROT', b'ATRN', b'STRN']
    rate, end, transforms, ar, sr, at, st = struct.unpack('<f6h', parts[0][1])
    assert math.isfinite(rate) and rate > 0 and end > 0 and min(transforms, ar, sr, at, st) >= 0
    canonical = struct.pack('<fHi', rate, end, transforms); names = []
    assert len(parts[1][1]) == transforms
    for tag, raw in parts[1][1]:
        assert tag == b'XFIN'; stop = raw.index(0)
        name = raw[:stop].decode('ascii'); names.append(name)
        animated, rotation, mask, x, y, z = struct.unpack('<BHB3H', raw[stop+1:])
        assert animated in (0, 1) and mask & ~56 == 0 and rotation < (ar if animated else sr)
        assert all(index < (at if mask & (8 << axis) else st) for axis, index in enumerate((x, y, z)))
        canonical += raw
    assert len(set(n.lower() for n in names)) == transforms
    rotation_keys = translation_keys = 0
    for part_index, count, rotation in [(2, ar, True), (4, at, False)]:
        if part_index == 4:
            assert len(parts[3][1]) == sr*7
            canonical += struct.pack('<i', sr)+parts[3][1]
        channels = parts[part_index][1]; assert len(channels) == count
        canonical += struct.pack('<i', count)
        for tag, raw in channels:
            assert tag == (b'QCHN' if rotation else b'CHNL')
            key_count, = struct.unpack('<H', raw[:2]); prefix = 5 if rotation else 2
            assert key_count >= 2 and len(raw) == prefix+6*key_count
            canonical += (raw[2:5] if rotation else b'')+struct.pack('<i', key_count)+raw[prefix:]
            keys = list(struct.iter_unpack('<HI' if rotation else '<Hf', raw[prefix:]))
            times = [t for t, _ in keys]; assert times == sorted(set(times)) and times[-1] <= end
            assert rotation or all(math.isfinite(v) and abs(v) <= 1e6 for _, v in keys)
            if rotation: rotation_keys += key_count
            else: translation_keys += key_count
    assert len(parts[5][1]) == st*4
    assert all(math.isfinite(v) and abs(v) <= 1e6 for (v,) in struct.iter_unpack('<f', parts[5][1]))
    canonical += struct.pack('<i', st)+parts[5][1]+struct.pack('<i', 0)
    return dict(FrameRate=rate, EndFrame=end, transforms=transforms, animated_rotations=ar, static_rotations=sr,
                animated_translations=at, static_translations=st, rotation_keys=rotation_keys,
                translation_keys=translation_keys, suffix=[], sha256=hashlib.sha256(canonical).hexdigest().upper()), names


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path); p.add_argument('configuration', type=Path); p.add_argument('native_output', type=Path)
    p.add_argument('--configuration-sha256', required=True); a = p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper() == a.configuration_sha256.upper()
    assert a.native_output.stat().st_size <= 4*1048576
    sources, row, skeleton_rows, skin_rows, logical_rows, selected_clip = {}, None, {}, {}, {}, None
    compressed = binding = None; quaternion_rows = []
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('packed_quaternion='):
            quaternion_rows.append(json.loads(line.split('=', 1)[1]))
        elif line.startswith('compressed_animation_clip='):
            assert compressed is None; compressed = json.loads(line.split('=', 1)[1])
        elif line.startswith('compressed_animation_binding='):
            assert binding is None; binding = json.loads(line.split('=', 1)[1])
        elif line.startswith('skeletal_descriptor_source='):
            r = json.loads(line.split('=', 1)[1]); assert r['Path'] not in sources; sources[r['Path']] = r
        elif line.startswith('selected_logical_clip='):
            assert selected_clip is None; selected_clip = json.loads(line.split('=', 1)[1])
        elif line.startswith('logical_animation_table='):
            r = json.loads(line.split('=', 1)[1]); assert r['path'] not in logical_rows; logical_rows[r['path']] = r
        elif line.startswith('skeletal_skin_source='):
            r = json.loads(line.split('=', 1)[1]); assert r['path'] not in skin_rows; skin_rows[r['path']] = r
        elif line.startswith('skeletal_skeleton='):
            r = json.loads(line.split('=', 1)[1]); key = (r['path'], r['level']); assert key not in skeleton_rows; skeleton_rows[key] = r
        elif line.startswith('skeletal_descriptor='):
            assert row is None; row = json.loads(line.split('=', 1)[1])
    assert row and set(row['counts']) == set(sources) and set(row['counts'].values()) == {1}
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(a.root, a.configuration)]
        raw = {}
        for path, pin in sources.items():
            for priority, index in indices:
                data = index.lookup(path)
                if data is None: continue
                assert priority == pin['Priority'] and len(data) == pin['Bytes']
                assert hashlib.sha256(data).hexdigest().upper() == pin['Sha256']
                raw[path] = data; break
            else: raise ValueError('Missing descriptor source')
    current, chain, appearance = row['RootPath'], [], None
    while current:
        assert current not in chain; chain.append(current)
        root, = nodes(raw[current]); parent, layers = parse(root)
        value = layers['SHOT'][1].get('appearanceFilename')
        if appearance is None and value is not None and value[0] == 1:
            appearance = strings(value[1:], 1)[0]
        current = parent
    assert appearance == row['AppearancePath'] and sat(raw[appearance]) == row['Appearance']
    assert {path: mlod(raw[path]) for path in row['Appearance']['MeshGenerators']} == row['LodMeshes']
    expected = set(chain) | {appearance} | set(row['Appearance']['MeshGenerators'])
    if skeleton_rows:
        paths = {v['Path'] for v in row['Appearance']['Skeletons']}
        expected |= paths
        recovered = {}
        for path in paths:
            root, = nodes(raw[path])
            if root[0] == b'SLOD':
                ver, = root[1]; assert ver[0] == b'0000'
                info, *levels = ver[1]; assert info[0] == b'INFO'
                assert len(levels) == max(0, struct.unpack('<h', info[1])[0])
            else: levels = [root]
            for index, level in enumerate(levels):
                assert level[0] == b'SKTM'; ver, = level[1]; assert ver[0] == b'0002'
                assert [p[0] for p in ver[1]] == [b'INFO', b'NAME', b'PRNT', b'RPRE', b'RPST', b'BPTR', b'BPRO', b'JROR']
                fields = dict(ver[1]); count, = struct.unpack('<I', fields[b'INFO']); names = strings(fields[b'NAME'], count)
                canonical = struct.pack('<i', count)
                for i in range(count):
                    canonical += names[i].encode('ascii') + bytes([0])
                    for tag, stride in [(b'PRNT', 4), (b'RPRE', 16), (b'RPST', 16), (b'BPTR', 12), (b'BPRO', 16), (b'JROR', 4)]:
                        assert len(fields[tag]) == stride*count
                        canonical += fields[tag][i*stride:(i+1)*stride]
                key = (path, index)
                assert skeleton_rows[key] == dict(path=path, level=index, joints=count, sha256=hashlib.sha256(canonical).hexdigest().upper())
                recovered[key] = names
        assert set(recovered) == set(skeleton_rows)
        body = row['Appearance']['Skeletons'][0]['Path']; attachment = row['Appearance']['Skeletons'][1]['AttachmentTransform']
        assert all([name.lower() for name in names].count(attachment.lower()) == 1 for (path, level), names in recovered.items() if path == body and level < 4)
    if skin_rows:
        selected = {v['SerializedPaths'][0] for v in row['LodMeshes'].values()}
        assert set(skin_rows) == selected
        expected |= selected
        for path, native in skin_rows.items():
            root, = nodes(raw[path]); assert root[0] == b'SKMG'
            ver, = root[1]; assert ver[0] == b'0004'
            parts = ver[1]; assert [n[0] for n in parts[:6]] == [b'INFO', b'SKTM', b'XFNM', b'POSN', b'TWHD', b'TWDT']
            fields = dict(parts[:6]); assert len(fields[b'INFO']) == 44
            info = struct.unpack('<9i4h', fields[b'INFO'])
            paths = strings(fields[b'SKTM'], info[2]); names = strings(fields[b'XFNM'], info[3])
            assert len(fields[b'POSN']) == info[4]*12 and len(fields[b'TWHD']) == info[4]*4 and len(fields[b'TWDT']) == info[5]*8
            counts = struct.unpack('<'+'i'*info[4], fields[b'TWHD']); weights = list(struct.iter_unpack('<If', fields[b'TWDT']))
            assert sum(counts) == len(weights) and all(0 < c <= 64 for c in counts)
            cursor = 0
            for count in counts:
                group = weights[cursor:cursor+count]; cursor += count
                assert all(index < len(names) and 0 <= value <= 1 for index, value in group)
                assert abs(sum(value for _, value in group)-1) <= .0001
            available = [name.lower() for pth in paths for name in recovered[pth, 0]]
            assert all(available.count(name.lower()) == 1 for name in names)
            next_index = 6; normals = b''
            if parts[next_index][0] == b'NORM':
                normals = parts[next_index][1]; next_index += 1
                assert len(normals) == info[6]*12
            canonical = b''.join(fields[tag] for tag in [b'INFO', b'SKTM', b'XFNM', b'POSN', b'TWHD', b'TWDT']) + normals
            assert native == dict(path=path, positions=info[4], weights=info[5], normals=len(normals)//12, transforms=info[3],
                                  suffix=[tag.decode('ascii') for tag, _ in parts[next_index:]], sha256=hashlib.sha256(canonical).hexdigest().upper())
    if logical_rows:
        selected = {r['AnimationTablePath'] for r in row['Appearance']['AnimationTables']}; assert set(logical_rows) == selected; expected |= selected
        for path, native in logical_rows.items():
            assert native == dict(path=path, **logical_table(raw[path]))
        assert sum(r['entries'] for r in logical_rows.values()) == 1046
    if selected_clip:
        table, = nodes(raw[selected_clip['table']]); version, = table[1]
        entry, = [parts[1] for tag, parts in version[1][1:] if parts[0][1][:-1].decode('ascii').rstrip(' ') == selected_clip['name']]
        scales = []
        while entry[0] == b'TSCL':
            ver, = entry[1]; assert ver[0] == b'0000'; info, entry = ver[1]; assert info[0] == b'INFO'
            scale, = struct.unpack('<f', info[1]); scales.append(scale)
        assert entry[0] == b'PXAT'; ver, = entry[1]; assert ver[0] == b'0000'
        info, *metadata = ver[1]; assert info[0] == b'INFO'; path, = strings(info[1], 1)
        root, = nodes(raw[path]); clip_version, = root[1]
        assert selected_clip == dict(table=selected_clip['table'], name=selected_clip['name'], path=path, scales=scales,
                                     metadata=metadata[0][0].decode('ascii') if metadata else None,
                                     kind=root[0].decode('ascii'), version=clip_version[0].decode('ascii'))
        expected.add(path)
    assert set(sources) == expected
    if compressed:
        assert selected_clip and compressed['path'] == selected_clip['path']
        decoded, names = compressed_clip(raw[compressed['path']])
        assert compressed == dict(path=compressed['path'], **decoded)
        available = {name.lower() for name in recovered[body, 0]}
        unmatched = [name for name in names if name.lower() not in available]
        assert binding == dict(matched=len(names)-len(unmatched), unmatched=unmatched, playback=False)
        print(json.dumps(dict(compressed_original_clip=decoded, binding=binding, agreement=True), indent=2))
        if quaternion_rows:
            from ckat_float32_overshoot_probe import build_table, f32
            image = (a.root/'Stardust.exe').read_bytes()
            assert hashlib.sha256(image).hexdigest().upper() == 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
            table = build_table(image); packed = []
            clip_root, = nodes(raw[compressed['path']]); ver, = clip_root[1]; fields = dict(ver[1])
            for tag, channel in fields[b'AROT']:
                selectors = channel[2:5]
                packed.extend((word, selectors) for _, word in struct.iter_unpack('<HI', channel[5:]))
            packed.extend((word, bytes((x,y,z))) for x,y,z,word in struct.iter_unpack('<BBBI', fields[b'SROT']))
            assert len(packed) == len(quaternion_rows); maximum = 0; repaired = 0
            for ordinal, ((word, selectors), native) in enumerate(zip(packed, quaternion_rows)):
                values = []
                for axis, (field, selector) in enumerate(zip(((word>>21)&2047,(word>>10)&2047,word&1023), selectors)):
                    assert 128 <= selector <= 254
                    basis, scale11, scale10 = table[selector]; sign = 512 if axis == 2 else 1024
                    offset = (field&(sign-1))*(scale10 if axis == 2 else scale11)
                    values.append(f32(basis-offset if field&sign else basis+offset))
                radicand = 1-sum(v*v for v in values); w = f32(math.sqrt(max(0,radicand)))
                assert native['ordinal'] == ordinal and native['Repaired'] == (radicand < 0)
                repaired += radicand < 0
                reference = values+[w]; magnitude = math.sqrt(sum(v*v for v in reference))
                errors = [abs(native[key]-value) for key,value in zip(('X','Y','Z','W'),reference)]
                errors += [abs(native['Radicand']-radicand)]
                errors += [abs(value-expected/magnitude) for value,expected in zip(native['publication'],reference)]
                maximum = max(maximum,*errors); assert max(errors) <= 3e-7
            print(json.dumps(dict(packed_quaternions=len(packed), repaired=repaired, maximum_error=maximum,
                                  executable_table_verified=True, interpolation=False, agreement=True), indent=2))
    print(json.dumps(dict(sources=len(sources), source_bytes=sum(r['Bytes'] for r in sources.values()),
                          template_depth=len(chain), generators=len(row['LodMeshes']),
                          mesh_levels=sum(len(m['SerializedPaths']) for m in row['LodMeshes'].values()),
                          skeleton_references=len(row['Appearance']['Skeletons']),
                          distance_intervals=len(row['Appearance']['DistanceIntervals']), skeleton_levels=len(skeleton_rows),
                          skeleton_joints=sum(r['joints'] for r in skeleton_rows.values()), skin_sources=len(skin_rows),
                          skin_positions=sum(r['positions'] for r in skin_rows.values()), skin_weights=sum(r['weights'] for r in skin_rows.values()), agreement=True), indent=2))
    if logical_rows: print(json.dumps(dict(logical_tables=len(logical_rows), entries=sum(r['entries'] for r in logical_rows.values()),
                                          duplicated_names=sum(r['duplicates'] for r in logical_rows.values()), agreement=True), indent=2))
    if selected_clip: print(json.dumps(dict(selected_original_clip=True, path=selected_clip['path'], kind=selected_clip['kind'],
                                           version=selected_clip['version'], playback=False, agreement=True), indent=2))


if __name__ == '__main__': main()
