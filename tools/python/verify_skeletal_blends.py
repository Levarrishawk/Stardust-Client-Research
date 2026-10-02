"""Independent original SKMG0004 sparse blend target/index/vector digest agreement."""
import hashlib
import struct
from verify_static_mesh_slice import nodes


def inspect(data):
    root,=nodes(data);version,=root[1];assert root[0]==b'SKMG' and version[0]==b'0004'
    fields=dict(version[1]);words=struct.unpack('<9i4h',fields[b'INFO']);count=words[8]
    form,=[value for tag,value in version[1] if tag==b'BLTS'];assert len(form)==count
    tangent_count=struct.unpack_from('<I',fields[b'DOT3'])[0] if b'DOT3' in fields else 0
    result=[]
    for index,(tag,parts) in enumerate(form):
        assert tag==b'BLT ' and parts[0][0]==b'INFO';info=parts[0][1];positions,normals=struct.unpack_from('<2i',info)
        assert info[-1]==0 and b'\0' not in info[8:-1];name=info[8:-1].decode('ascii');fields=dict(parts)
        sizes=[positions,normals,struct.unpack_from('<i',fields[b'DOT3'])[0] if b'DOT3' in fields else 0]
        canonical=info[8:];arrays=[]
        for tag,count,bound in zip((b'POSN',b'NORM',b'DOT3'),sizes,(words[4],words[6],tangent_count)):
            raw=fields.get(tag,b'');raw=raw[4:] if tag==b'DOT3' else raw
            assert len(raw)==count*16
            records=list(struct.iter_unpack('<I3f',raw));assert all(row[0]<bound for row in records)
            arrays.append(records);canonical+=struct.pack('<i',count)+raw
        hardpoints=fields.get(b'HPTS');canonical+=struct.pack('<i',-1 if hardpoints is None else len(hardpoints))+(hardpoints or b'')
        result.append(dict(index=index,VariableName=name,private=False,positions=sizes[0],normals=sizes[1],tangents=sizes[2],hardpoints=hardpoints is not None,sha256=hashlib.sha256(canonical).hexdigest().upper()))
    return result


def verify(raw,meshes,native):
    expected=[]
    for path in meshes:expected.extend(dict(path=path,**row) for row in inspect(raw[path]))
    order=lambda row:(row['path'],row['index'])
    assert sorted(expected,key=order)==sorted(native,key=order)
    return dict(original_blend_targets=len(expected),unique_shared_owner_variables=len({row['VariableName'] for row in expected}),
                position_deltas=sum(row['positions'] for row in expected),normal_deltas=sum(row['normals'] for row in expected),
                tangent_deltas=sum(row['tangents'] for row in expected),hardpoint_targets=sum(row['hardpoints'] for row in expected),sparse_index_vector_digest_agreement=True)


def verify_application(raw, rows):
    """Double reference from raw sparse records; explicit modern float32 tolerance."""
    import math
    maximum = 0.0
    total = 0
    seen = set()
    for row in rows:
        path, selected = row['path'], row['selected']
        assert (path, selected) not in seen
        seen.add((path, selected))
        root, = nodes(raw[path]); version, = root[1]; fields = dict(version[1])
        arrays = [list(map(list, struct.iter_unpack('<3f', fields[b'POSN']))),
                  list(map(list, struct.iter_unpack('<3f', fields[b'NORM']))),
                  list(map(list, struct.iter_unpack('<4f', fields[b'DOT3'][4:])))]
        target_names=[parts[0][1][8:-1].decode('ascii') for tag,parts in fields[b'BLTS']]
        values=row.get('values',{name:selected for name in target_names})
        assert all(name in values and 0<=values[name]<=255 for name in target_names)
        multiplier=struct.unpack('<f',bytes.fromhex('8180803b'))[0];epsilon=struct.unpack('<f',bytes.fromhex('bc74133c'))[0]
        active = any(abs(values[name]*multiplier)>epsilon for name in target_names)
        if active:
            for tag, parts in fields[b'BLTS']:
                assert tag == b'BLT '
                target = dict(parts)
                name=target[b'INFO'][8:-1].decode('ascii');weight=values[name]*multiplier
                if abs(weight)<=epsilon:continue
                assert b'HPTS' not in target
                for kind, output in zip((b'POSN', b'NORM', b'DOT3'), arrays):
                    data = target.get(kind, b'')
                    if kind == b'DOT3': data = data[4:]
                    for index, x, y, z in struct.iter_unpack('<I3f', data):
                        for axis, value in enumerate((x, y, z)): output[index][axis] += value * weight
            for output in arrays[1:]:
                for vector in output:
                    length = math.sqrt(sum(v*v for v in vector[:3]))
                    if length > 9.999999747378752e-6:
                        vector[:3] = [v / length for v in vector[:3]]
        for name, expected in zip(('positions', 'normals', 'tangents'), arrays):
            actual = row[name]
            assert len(actual) == len(expected)
            for a, b in zip(actual, expected):
                assert len(a) == len(b)
                error = max(abs(x-y) for x, y in zip(a, b))
                assert error <= 2e-5, (path, selected, name, error)
                maximum = max(maximum, error); total += 1
        # Handedness must retain exact original float32 values.
        assert [v[3] for v in row['tangents']] == [v[3] for v in arrays[2]]
    selections=(0,2,3,127,255,-1) if any(row['selected']==-1 for row in rows) else (0,2,3,127,255)
    assert seen == {(path, selected) for path in raw if path.endswith('.mgn') for selected in selections}
    return dict(original_shape_vectors=total, selection_profiles=len(selections), maximum_component_error=maximum,
                raw_sparse_application_agreement=True, threshold_and_handedness=True)
