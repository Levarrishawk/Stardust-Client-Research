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
    locomotion_bank=[]
    compressed = binding = None; quaternion_rows = []; sample_rows = []; sampler_row = None
    blend_targets=[]; blend_applications=[];texture_choice_rows=[];normal_materials={};normal_programs={};normal_samples=[];normal_images=[];material_fields={};specular_materials={};specular_samples=[];vertex_lit_materials={};vertex_lit_programs={};vertex_lit_samples=[]
    customization_values=None
    animation_names = {}; animation_poses = {}; animation_vertices = {}; materials = {}; textures = {}; palettes = {}; blueprints = {}; face_resources = None; face_preparations = []; face_programs = []; face_compositions = []; generated_bindings = []; albedo_materials = {}; albedo_programs = {}
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('skeletal_locomotion_bank='):
            locomotion_bank.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_albedo='):
            value=json.loads(line.split('=',1)[1]); assert value['path'] not in albedo_materials; albedo_materials[value['path']]=value
        elif line.startswith('skeletal_vertex_lit_material='):
            r=json.loads(line.split('=',1)[1]);assert r['path'] not in vertex_lit_materials;vertex_lit_materials[r['path']]=r
        elif line.startswith('skeletal_vertex_lit_program='):
            r=json.loads(line.split('=',1)[1]);assert r['path'] not in vertex_lit_programs;vertex_lit_programs[r['path']]=r
        elif line.startswith('skeletal_vertex_lit_sample='):
            vertex_lit_samples.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_material_fields='):
            r=json.loads(line.split('=',1)[1]);assert r['path'] not in material_fields;material_fields[r['path']]=r
        elif line.startswith('skeletal_specular_material='):
            r=json.loads(line.split('=',1)[1]);assert r['path'] not in specular_materials;specular_materials[r['path']]=r
        elif line.startswith('skeletal_specular_sample='):
            specular_samples.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_compressed_normal='):
            r=json.loads(line.split('=',1)[1]);assert r['path'] not in normal_materials;normal_materials[r['path']]=r
        elif line.startswith('skeletal_normal_program='):
            r=json.loads(line.split('=',1)[1]);assert r['path'] not in normal_programs;normal_programs[r['path']]=r
        elif line.startswith('skeletal_normal_sample='):
            normal_samples.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_normal_image='):
            normal_images.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_texture_choice='):
            texture_choice_rows.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_blend_application='):
            blend_applications.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_blend_target='):
            blend_targets.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_customization_values='):
            assert customization_values is None;rows=json.loads(line.split('=',1)[1]);customization_values={(r['VariableName'],r['Private']):r['Value'] for r in rows};assert len(customization_values)==len(rows)
        elif line.startswith('skeletal_albedo_program='):
            value=json.loads(line.split('=',1)[1]); assert value['path'] not in albedo_programs; albedo_programs[value['path']]=value
        elif line.startswith('generated_face_binding='):
            generated_bindings.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('face_composition='):
            face_compositions.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('face_pixel_program='):
            face_programs.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('face_preparation='):
            face_preparations.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('face_resources='):
            assert face_resources is None; face_resources=json.loads(line.split('=',1)[1])
        elif line.startswith('face_blueprint='):
            r = json.loads(line.split('=',1)[1]); assert r['path'] not in blueprints; blueprints[r['path']] = r
        elif line.startswith('skeletal_palette_defaults='):
            r = json.loads(line.split('=',1)[1]); assert r['path'] not in palettes; palettes[r['path']] = r
        elif line.startswith('skeletal_material='):
            r = json.loads(line.split('=',1)[1]); assert r['path'] not in materials; materials[r['path']] = r
        elif line.startswith('skeletal_texture='):
            r = json.loads(line.split('=',1)[1]); assert r['path'] not in textures; textures[r['path']] = r
        elif line.startswith('skeleton_animation_names='):
            r = json.loads(line.split('=',1)[1]); assert r['frame'] not in animation_names; animation_names[r['frame']] = r
        elif line.startswith('skeleton_animation_pose='):
            r = json.loads(line.split('=',1)[1]); key = r['frame'],r['joint']; assert key not in animation_poses; animation_poses[key] = r
        elif line.startswith('skeleton_animation_vertex='):
            r = json.loads(line.split('=',1)[1]); key = r['frame'],r['path'],r['shader'],r['vertex']; assert key not in animation_vertices; animation_vertices[key] = r
        elif line.startswith('compressed_sample='):
            sample_rows.append(json.loads(line.split('=', 1)[1]))
        elif line.startswith('compressed_sampler='):
            assert sampler_row is None; sampler_row = json.loads(line.split('=', 1)[1])
        elif line.startswith('packed_quaternion='):
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
    if locomotion_bank:
        from verify_compressed_locomotion import verify_bank_routes
        expected |= verify_bank_routes(raw,locomotion_bank)
    if blend_targets:
        from verify_skeletal_blends import verify as verify_blends
        print(json.dumps(verify_blends(raw,skin_rows,blend_targets),indent=2))
    if blend_applications:
        from verify_skeletal_blends import verify_application
        print(json.dumps(verify_application({path:raw[path] for path in skin_rows},blend_applications),indent=2))
    if materials:
        shader_paths = set()
        for mesh in skin_rows:
            mesh_root, = nodes(raw[mesh]); mesh_version, = mesh_root[1]
            shader_paths |= {parts[0][1][:-1].decode('ascii').lower().replace('\\','/') for tag,parts in mesh_version[1] if tag == b'PSDT'}
        assert set(materials) == shader_paths
        from verify_skeletal_materials import verify as verify_materials
        print(json.dumps(verify_materials(raw,materials,textures,palettes if palettes else None,customization_values,bool(texture_choice_rows)),indent=2))
        if texture_choice_rows:
            from verify_skeletal_materials import verify_texture_choices
            print(json.dumps(verify_texture_choices(raw,materials,texture_choice_rows),indent=2))
        expected |= set(materials) | set(textures) | {m['effect'] for m in materials.values()}
        if palettes: expected |= {f['PalettePath'] for p in palettes.values() for f in p['factors']}
    if albedo_materials:
        assert set(albedo_materials)==set(materials)
        from verify_skeletal_albedo import verify as verify_albedo
        required,result=verify_albedo(raw,albedo_materials,albedo_programs,palettes,customization_values)
        expected |= required;print(json.dumps(result,indent=2))
    if normal_materials:
        from verify_skeletal_normals import verify as verify_normals
        required,result=verify_normals(raw,normal_materials,normal_programs,normal_samples,normal_images)
        expected|=required;print(json.dumps(result,indent=2))
    if vertex_lit_materials:
        from verify_skeletal_specular import verify_vertex_lit
        required,result=verify_vertex_lit(raw,vertex_lit_programs,vertex_lit_materials,vertex_lit_samples);expected|=required;print(json.dumps(result,indent=2))
    if material_fields:
        from verify_skeletal_specular import verify as verify_specular
        print(json.dumps(verify_specular(raw,material_fields,specular_materials,specular_samples),indent=2))
    if blueprints:
        references = set()
        for mesh in skin_rows:
            mesh_root, = nodes(raw[mesh]); mesh_version, = mesh_root[1]
            for tag, parts in mesh_version[1]:
                if tag != b'TRTS': continue
                for chunk, record in parts[1:]:
                    assert chunk == b'TRT '; end = record.index(0)
                    references.add(record[:end].decode('ascii').lower().replace('\\','/'))
        assert references == set(blueprints)
        if face_resources:
            from verify_face_resources import verify as verify_face_resources
            required_shaders={path for blueprint in blueprints.values() for path in blueprint['ShaderPaths']}
            required_textures={path for blueprint in blueprints.values() for path in blueprint['TexturePaths']}
            required_palettes={op['PalettePath'] for blueprint in blueprints.values() for group in blueprint['PrepareLists'] for op in group if op['PalettePath']}
            assert set(face_resources['shaders'])==required_shaders
            assert {image['path'] for image in face_resources['textures']}==required_textures
            assert {palette['path'] for palette in face_resources['palettes']}==required_palettes
            if face_programs:
                from verify_face_resources import verify_programs
                expected |= verify_programs(raw,required_shaders,face_programs)
                print(json.dumps(dict(original_face_pixel_programs=len(face_programs),instruction_agreement=True)))
            expected |= required_shaders | required_textures | required_palettes
            print(json.dumps(verify_face_resources(raw,face_resources),indent=2))
        from verify_face_blueprint import verify as verify_blueprint
        for path,native in blueprints.items(): print(json.dumps(verify_blueprint(raw[path],native),indent=2))
        from verify_face_resources import verify_preparation
        for prepared in face_preparations: print(json.dumps(verify_preparation(raw,blueprints[prepared['path']],prepared),indent=2))
        from verify_face_composition import verify as verify_composition
        for composed in face_compositions:
            prepared,=[value for value in face_preparations if (value['path'],value['requested'])==(composed['path'],composed['requested'])]
            print(json.dumps(verify_composition(raw,blueprints[composed['path']],prepared,composed),indent=2))
        if generated_bindings:
            from verify_face_composition import verify_bindings
            print(json.dumps(verify_bindings(raw,skin_rows,face_compositions,generated_bindings),indent=2))
        expected |= references
    if customization_values is not None:
        required={(f['VariableName'],bool(f['RawPrivateFlag'])) for m in materials.values() for f in m['customization']['PaletteFactors']}
        required|={(op['VariableName'],op['Kind']=='SST1' or bool(op['RawFactoryFlag'])) for b in blueprints.values() for group in b['PrepareLists'] for op in group if op['VariableName']}
        assert set(customization_values)==required
        for prepared in face_preparations:
            assert all(customization_values[v['VariableName'],v['FactoryFlag']]==v['Value'] for v in prepared['values'])
        print(json.dumps(dict(shared_customization_keys=len(required),exact_namespace_value_agreement=True)))
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
            assert len(packed) == len(quaternion_rows); maximum = 0; repaired = 0; decoded_quaternions = []
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
                decoded_quaternions.append(([f32(v/magnitude) for v in reference], radicand < 0))
                errors = [abs(native[key]-value) for key,value in zip(('X','Y','Z','W'),reference)]
                errors += [abs(native['Radicand']-radicand)]
                errors += [abs(value-expected/magnitude) for value,expected in zip(native['publication'],reference)]
                maximum = max(maximum,*errors); assert max(errors) <= 3e-7
            print(json.dumps(dict(packed_quaternions=len(packed), repaired=repaired, maximum_error=maximum,
                                  executable_table_verified=True, interpolation=False, agreement=True), indent=2))
            if sample_rows:
                assert sampler_row == dict(FrameRate=decoded['FrameRate'], EndFrame=decoded['EndFrame'],
                                           DurationSeconds=decoded['EndFrame']/decoded['FrameRate'],
                                           RepairedPackedValues=repaired, HasUninterpretedSuffix=False, playback=False)
                cursor = 0; rotations = []
                for _, channel in fields[b'AROT']:
                    times = [time for time, _ in struct.iter_unpack('<HI', channel[5:])]
                    rotations.append((times, decoded_quaternions[cursor:cursor+len(times)])); cursor += len(times)
                static_rotations = decoded_quaternions[cursor:]
                translations = [list(struct.iter_unpack('<Hf', channel[2:])) for _, channel in fields[b'ATRN']]
                static_translations = [v for (v,) in struct.iter_unpack('<f', fields[b'STRN'])]
                transforms = []
                for _, record in fields[b'XFRM']:
                    stop = record.index(0); transforms.append((record[:stop].decode('ascii'), struct.unpack('<BHB3H', record[stop+1:])))
                def interval(times, frame):
                    if frame <= times[0]: return 0,0,0
                    if frame >= times[-1]: return len(times)-1,len(times)-1,0
                    low = max(i for i,t in enumerate(times) if t <= frame)
                    if frame == times[low]: return low,low,0
                    return low,low+1,f32((frame-times[low])/(times[low+1]-times[low]))
                def slerp(a,b,t):
                    dot = sum(x*y for x,y in zip(a,b))
                    if dot < 0: b = [-v for v in b]; dot = -dot
                    dot = min(1,dot)
                    if dot > .999999: values = [(1-t)*x+t*y for x,y in zip(a,b)]
                    else:
                        angle = math.acos(dot); inverse = 1/math.sin(angle)
                        values = [math.sin((1-t)*angle)*inverse*x+math.sin(t*angle)*inverse*y for x,y in zip(a,b)]
                    norm = math.sqrt(sum(v*v for v in values)); return [v/norm for v in values]
                maximum = 0; seen = []; validated_samples = {}
                for native in sample_rows:
                    frame = native['frame']; index = native['transform']; seen.append((frame,index))
                    name, (animated,rotation,mask,x,y,z) = transforms[index]
                    if animated:
                        times, values = rotations[rotation]; low,high,t = interval(times,frame)
                        q = values[low][0] if low == high else slerp(values[low][0],values[high][0],t)
                        was_repaired = values[low][1] or values[high][1]
                    else: q,was_repaired = static_rotations[rotation]
                    position = []
                    for axis, channel_index in enumerate((x,y,z)):
                        if mask & (8<<axis):
                            channel = translations[channel_index]; low,high,t = interval([time for time,_ in channel],frame)
                            value = channel[low][1] if low == high else (1-t)*channel[low][1]+t*channel[high][1]
                        else: value = static_translations[channel_index]
                        position.append(value)
                    assert native['Name'] == name and native['RotationRepaired'] == was_repaired
                    validated_samples[frame,name] = (q,position)
                    rotation_error = min(max(abs(a-b) for a,b in zip(native['rotation'],q)), max(abs(a+b) for a,b in zip(native['rotation'],q)))
                    error = max(rotation_error,*[abs(a-b) for a,b in zip(native['translation'],position)])
                    assert error <= 2e-6; maximum = max(maximum,error)
                expected_frames = [0,.5,15.25,30,59.5,90,119.5,120,15.25,0]
                assert seen == [(frame,index) for frame in expected_frames for index in range(len(transforms))]
                print(json.dumps(dict(original_transform_samples=len(sample_rows), maximum_error=maximum,
                                      reverse_sampling=True, playback=False, agreement=True), indent=2))
                if animation_poses:
                    from verify_skeleton_animation_pose import verify
                    print(json.dumps(verify(raw,row['Appearance'],validated_samples,animation_names,animation_poses,animation_vertices),indent=2))
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
