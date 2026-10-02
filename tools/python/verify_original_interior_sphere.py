"""Audit original sphere-scene source identity, inherited dimensions and every recorded floor locator.

This checks frame invariants, not an independent dynamic door-controller implementation.
"""
import argparse
import collections
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes, indexed_triangles
from verify_triangle_box_sweeps import cross
from verify_creature_template_profile import parse
from verify_floor_walks import f32, project
from verify_portal_floor_boxes import transform32
from verify_skeletal_geometry import inspect as inspect_character_geometry
from verify_skeletal_descriptors import logical_table, compressed_clip, strings
from verify_skeleton_bind_pose import skeletons


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path); p.add_argument('configuration', type=Path); p.add_argument('native_output', type=Path)
    p.add_argument('--configuration-sha256', required=True); a = p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper() == a.configuration_sha256.upper()
    assert a.native_output.stat().st_size <= 8*1048576
    sources, templates, content, counts, guards, rows, volume = {}, {}, {}, None, {}, [], None
    locomotion_bank=[];camera_queries=[];camera_doors=[];camera_triangles=[];camera_cell_rows=[];cell_light_rows=[];cell_lighting_rows=[];static_color_rows=[];room_programs=[];room_materials=[];room_cases=[];static_normal_programs=[];static_normal_materials=[];static_tangents=[];static_preview_tangents=[];static_normal_cases=[];legacy_light_slots=[];synthetic_light_slots=[];legacy_cell_lighting=[];tangent_functions=[];tangent_vertex_cases=[];skeletal_tangent_vertex=[];skeletal_tangent_materials=[];legacy_point_original=[];legacy_point_synthetic=[]
    customization_values=None;blend_targets=[];blend_applications=[];texture_choice_rows=[];normal_materials={};normal_programs={};normal_samples=[];normal_images=[];material_fields={};specular_materials={};specular_samples=[];vertex_lit_materials={};vertex_lit_programs={};vertex_lit_samples=[];mouth_vertex_sources=[]
    blueprints = {}; face_resources = None; preparations = []; compositions = []; bindings = []; face_programs = []; albedo = {}; albedo_programs = {}
    characters = {}; animation = None; materials = {}; textures = {}; palettes = {}; raw_content = {}
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith(('interior_sphere_source=', 'interior_sphere_template_source=')):
            target = templates if line.startswith('interior_sphere_template_source=') else sources
            r = json.loads(line.split('=', 1)[1]); assert r['Path'] not in target; target[r['Path']] = r
        elif line.startswith('interior_sphere_tick='):
            rows.append(json.loads(line.split('=', 1)[1]))
        elif line.startswith('interior_sphere_volume='):
            assert volume is None; volume = json.loads(line.split('=', 1)[1])
        elif line.startswith('interior_sphere_content_source='):
            r = json.loads(line.split('=', 1)[1]); assert r['Path'] not in content; content[r['Path']] = r
        elif line.startswith('interior_sphere_content_counts='):
            assert counts is None; counts = json.loads(line.split('=', 1)[1])
        elif line.startswith('interior_sphere_guard_geometry='):
            r = json.loads(line.split('=', 1)[1]); assert r['cell'] not in guards; guards[r['cell']] = r
        elif line.startswith(('face_blueprint=','face_resources=','face_preparation=','face_composition=','generated_face_binding=','face_pixel_program=','skeletal_albedo=','skeletal_albedo_program=')):
            prefix,value=line.split('=',1);r=json.loads(value)
            if prefix=='face_resources':assert face_resources is None;face_resources=r
            elif prefix in ('face_blueprint','skeletal_albedo','skeletal_albedo_program'):
                target={'face_blueprint':blueprints,'skeletal_albedo':albedo,'skeletal_albedo_program':albedo_programs}[prefix]
                assert r['path'] not in target;target[r['path']]=r
            else:{'face_preparation':preparations,'face_composition':compositions,'generated_face_binding':bindings,'face_pixel_program':face_programs}[prefix].append(r)
        elif line.startswith(('original_static_normal_program=','original_static_normal_material=','original_static_tangents=','original_static_normal_case=','original_static_preview_tangents=')):
            prefix,value=line.split('=',1);{'original_static_normal_program':static_normal_programs,'original_static_normal_material':static_normal_materials,'original_static_tangents':static_tangents,'original_static_normal_case':static_normal_cases,'original_static_preview_tangents':static_preview_tangents}[prefix].append(json.loads(value))
        elif line.startswith('original_tangent_vertex_functions='):
            tangent_functions.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_tangent_vertex_case='):
            tangent_vertex_cases.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_legacy_point_attenuation='):
            legacy_point_original.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('synthetic_legacy_point_attenuation='):
            legacy_point_synthetic.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_legacy_cell_lighting='):
            legacy_cell_lighting.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('synthetic_legacy_light_slots='):
            synthetic_light_slots.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_legacy_light_slots='):
            legacy_light_slots.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_room_pixel_program='):
            room_programs.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_room_material='):
            room_materials.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_room_pixel_case='):
            room_cases.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_static_vertex_colors='):
            static_color_rows.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_cell_lighting='):
            cell_lighting_rows.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_cell_light='):
            cell_light_rows.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_camera_cell='):
            camera_cell_rows.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_chase_doors='):
            camera_doors.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('original_chase_camera='):
            camera_queries.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_locomotion_bank='):
            locomotion_bank.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_tangent_vertex='):
            skeletal_tangent_vertex.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_tangent_material='):
            skeletal_tangent_materials.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('mouth_vertex_sources='):
            mouth_vertex_sources.append(json.loads(line.split('=',1)[1]))
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
        elif line.startswith('skeletal_blend_target='):
            blend_targets.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_blend_application='):
            blend_applications.append(json.loads(line.split('=',1)[1]))
        elif line.startswith('skeletal_customization_values='):
            assert customization_values is None;values=json.loads(line.split('=',1)[1]);customization_values={(v['VariableName'],v['Private']):v['Value'] for v in values};assert len(customization_values)==len(values)
        elif line.startswith('skeletal_palette_defaults='):
            r = json.loads(line.split('=',1)[1]); assert r['path'] not in palettes; palettes[r['path']] = r
        elif line.startswith('skeletal_material='):
            r = json.loads(line.split('=',1)[1]); assert r['path'] not in materials; materials[r['path']] = r
        elif line.startswith('skeletal_texture='):
            r = json.loads(line.split('=',1)[1]); assert r['path'] not in textures; textures[r['path']] = r
        elif line.startswith('interior_character_animation='):
            assert animation is None; animation = json.loads(line.split('=',1)[1])
        elif line.startswith('interior_character_geometry='):
            r = json.loads(line.split('=', 1)[1]); assert r['path'] not in characters; characters[r['path']] = r
    complete_vertex_sources=next((r['vertexSources'] for r in tangent_functions+skeletal_tangent_vertex if r.get('vertexSources')),None)
    assert len(sources) == 9 and len(templates) == 5 and len(rows) == 2880 and volume
    if content:
        assert len(content) == (1 if mouth_vertex_sources else 0)+(2 if complete_vertex_sources else 0)+(1 if tangent_functions else 2 if skeletal_tangent_vertex else 0)+(2 if static_normal_programs else 0)+(6 if room_programs else 0)+(1 if camera_queries else 0)+(2 if locomotion_bank else 0)+(145 if vertex_lit_materials else 144 if normal_materials else 139 if blueprints else 100 if palettes and animation else 97 if materials and animation else 80 if animation else 77 if characters else 70) and set(counts) == set(content) and set(counts.values()) == {1}
        assert all(content[path] == row for path, row in (sources | templates).items())
        assert sum(row['Bytes'] for row in content.values()) == (1346 if mouth_vertex_sources else 0)+(5737 if complete_vertex_sources else 0)+(12318 if tangent_functions else 14409 if skeletal_tangent_vertex else 0)+(5100 if static_normal_programs else 0)+(6924 if room_programs else 0)+(212 if camera_queries else 0)+(13352 if locomotion_bank else 0)+(16598491 if vertex_lit_materials else 16596575 if normal_materials else 15541123 if blueprints else 14284581 if palettes and animation else 14282453 if materials and animation else 10340219 if animation else 9943381 if characters else 9622375)
    floors, chain = {}, []
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(a.root, a.configuration)]
        def read(path, registry):
            for priority, index in indices:
                data = index.lookup(path)
                if data is not None:
                    pin = registry[path]
                    assert priority == pin['Priority'] and len(data) == pin['Bytes'] and hashlib.sha256(data).hexdigest().upper() == pin['Sha256']
                    return data
            raise ValueError('Missing original scene input')
        for path in content or sources:
            data = read(path, content or sources); raw_content[path] = data
            if path.endswith('.flr'):
                cell = next(c for c in (5, 6, 7) if '_r'+str(c)+'_' in path)
                root, = nodes(data); version, = root[1]
                positions, records = (entry[1] for entry in version[1][:2])
                if version[0] == b'0006':
                    positions, records = positions[4:], records[4:]
                floors[cell] = (list(struct.iter_unpack('<3f', positions)),
                                [struct.unpack_from('<3iI3i3f4Bi3i', records, i) for i in range(0, len(records), 60)])
        if cell_light_rows:
            from verify_cell_lights import verify as verify_lights
            print('cell_light_audit='+json.dumps(verify_lights(raw_content['appearance/thm_must_mining_outpost.pob'],cell_light_rows)))
            if cell_lighting_rows:
                from verify_cell_lighting_preview import verify as verify_lighting
                print('cell_lighting_audit='+json.dumps(verify_lighting(cell_light_rows,cell_lighting_rows)))
        if static_color_rows:
            from verify_static_vertex_colors import verify as verify_colors
            print('static_vertex_color_audit='+json.dumps(verify_colors(raw_content,static_color_rows)))
        if room_programs:
            from verify_static_room_lighting import verify as verify_room
            print('room_lighting_audit='+json.dumps(verify_room(raw_content,room_programs,room_materials,room_cases)))
        if static_normal_programs:
            from verify_static_normal_materials import verify as verify_static_normals
            print('static_normal_audit='+json.dumps(verify_static_normals(raw_content,static_normal_programs,static_normal_materials,static_tangents,static_normal_cases,static_preview_tangents)))
        if legacy_light_slots:
            from verify_legacy_light_selection import verify as verify_slots
            print('legacy_light_slots_audit='+json.dumps(verify_slots(cell_light_rows,legacy_light_slots,synthetic_light_slots)))
        if legacy_point_original:
            from verify_legacy_point_attenuation import verify as verify_point_attenuation
            print('legacy_point_attenuation_audit='+json.dumps(verify_point_attenuation(cell_light_rows,legacy_point_original,legacy_point_synthetic)))
        if legacy_cell_lighting:
            from verify_cell_lighting_preview import verify as verify_legacy_lighting
            print('legacy_cell_lighting_audit='+json.dumps(verify_legacy_lighting(cell_light_rows,legacy_cell_lighting,True,legacy_light_slots)))
        if complete_vertex_sources:
            from verify_tangent_source_closure import verify as verify_vertex_closure
            print('tangent_source_closure_audit='+json.dumps(verify_vertex_closure(raw_content,complete_vertex_sources)))
        if skeletal_tangent_vertex:
            from verify_skeletal_tangent_lighting import verify as verify_skeletal_tangent
            print('skeletal_tangent_vertex_audit='+json.dumps(verify_skeletal_tangent(raw_content,skeletal_tangent_vertex,skeletal_tangent_materials)))
        if tangent_functions:
            from verify_tangent_lighting_varyings import verify as verify_tangent_varyings
            print('tangent_vertex_audit='+json.dumps(verify_tangent_varyings(raw_content,tangent_functions,tangent_vertex_cases)))
        if camera_cell_rows:
            from verify_camera_cells import verify as verify_cells
            print('camera_cell_audit='+json.dumps(verify_cells(raw_content['appearance/thm_must_mining_outpost.pob'],camera_cell_rows)))
        if camera_queries:
            from verify_chase_camera import verify as verify_camera
            from verify_mesh_facing import verify as verify_facing
            facing_audit=verify_facing(raw_content)
            print('original_mesh_facing='+json.dumps(facing_audit))
        if characters:
            assert set(characters) == {'appearance/mesh/hum_m_body_l0.mgn', 'appearance/mesh/hum_m_head_l0.mgn'}
            for path, row in characters.items():
                independent = inspect_character_geometry(read(path, content))
                assert all(row[key] == independent[key] for key in ('source', 'degenerate', 'triangles', 'sha256'))
            assert sum(row['triangles'] for row in characters.values()) == 2810
        if mouth_vertex_sources:
            from verify_skeletal_vertex_lit_sources import EXPECTED,verify as verify_mouth
            assert complete_vertex_sources and mouth_vertex_sources==[EXPECTED]
            print("mouth_vertex_source_audit="+json.dumps(verify_mouth(raw_content,{p:r["Priority"] for p,r in content.items()},a.native_output)))
        if vertex_lit_materials:
            from verify_skeletal_specular import verify_vertex_lit,verify as verify_specular
            required,result=verify_vertex_lit(raw_content,vertex_lit_programs,vertex_lit_materials,vertex_lit_samples);assert required<=set(content);print(json.dumps(result,indent=2))
            print(json.dumps(verify_specular(raw_content,material_fields,specular_materials,specular_samples),indent=2))
        if normal_materials:
            from verify_skeletal_normals import verify as verify_normals
            required,result=verify_normals(raw_content,normal_materials,normal_programs,normal_samples,normal_images)
            assert required<=set(content);print(json.dumps(result,indent=2))
        if texture_choice_rows:
            from verify_skeletal_materials import verify_texture_choices
            print(json.dumps(verify_texture_choices(raw_content,materials,texture_choice_rows),indent=2))
        if blend_targets:
            from verify_skeletal_blends import verify as verify_blends, verify_application
            print(json.dumps(verify_blends(raw_content,characters,blend_targets),indent=2))
            print(json.dumps(verify_application({path:raw_content[path] for path in characters},blend_applications),indent=2))
        if locomotion_bank:
            from verify_compressed_locomotion import verify_bank_routes
            for r in locomotion_bank:read(r['path'],content);read(r['table'],content)
            verify_bank_routes(raw_content,locomotion_bank)
        if animation:
            assert characters and animation['table'] == 'appearance/lat/all_m.lat'
            body_table = read(animation['table'],content); face_table = read('appearance/lat/hum_m_face.lat',content)
            assert logical_table(body_table)['entries'] == 908 and logical_table(face_table)['entries'] == 138
            root, = nodes(body_table); version, = root[1]
            entry, = [records[1] for tag,records in version[1][1:] if records[0][1][:-1].decode('ascii').rstrip(' ') == animation['name']]
            assert entry[0] == b'PXAT'; proxy_version, = entry[1]; info,*metadata = proxy_version[1]
            path, = strings(info[1],1); assert path == animation['path']
            assert content[path]['Sha256'] == '798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B'
            decoded, clip_names = compressed_clip(read(path,content))
            names,_,_ = skeletons(read('appearance/skeleton/all_b.skt',content))[0]
            body_names = [n.decode('ascii') for n in names]
            unused = [n for n in clip_names if n.lower() not in {v.lower() for v in body_names}]
            missing = [n for n in body_names if n.lower() not in {v.lower() for v in clip_names}]
            assert animation == dict(table=animation['table'],name='emt_standing_idle_ambient_1_ag',path=path,
                                     FrameRate=decoded['FrameRate'],EndFrame=decoded['EndFrame'],unused=unused,missing=missing,RepairedPackedValues=0)
        if materials:
            shader_paths = set()
            for mesh in characters:
                root, = nodes(raw_content[mesh]); version, = root[1]
                shader_paths |= {parts[0][1][:-1].decode('ascii').lower().replace('\\','/') for tag,parts in version[1] if tag == b'PSDT'}
            assert set(materials) == shader_paths
            from verify_skeletal_materials import verify
            print(json.dumps(verify(raw_content,materials,textures,palettes if palettes else None,customization_values,bool(texture_choice_rows)),indent=2))
        if blueprints:
            from verify_face_blueprint import verify as verify_blueprint
            from verify_face_resources import verify as verify_resources, verify_preparation, verify_programs
            from verify_face_composition import verify as verify_composition, verify_bindings
            from verify_skeletal_albedo import verify as verify_albedo
            assert len(blueprints)==len(preparations)==len(compositions)==len(bindings)==1
            required_shaders={path for blueprint in blueprints.values() for path in blueprint['ShaderPaths']}
            required_textures={path for blueprint in blueprints.values() for path in blueprint['TexturePaths']}
            required_palettes={op['PalettePath'] for blueprint in blueprints.values() for group in blueprint['PrepareLists'] for op in group if op['PalettePath']}
            assert set(face_resources['shaders'])==required_shaders
            assert {image['path'] for image in face_resources['textures']}==required_textures
            assert {palette['path'] for palette in face_resources['palettes']}==required_palettes
            assert verify_programs(raw_content,required_shaders,face_programs)<=set(content)
            print(json.dumps(verify_resources(raw_content,face_resources),indent=2))
            for path,row in blueprints.items():print(json.dumps(verify_blueprint(raw_content[path],row),indent=2))
            for row in preparations:
                assert row['requested']==0
                assert all(v['Value']==(0 if customization_values is None else customization_values[v['VariableName'],v['FactoryFlag']]) for v in row['values'])
                print(json.dumps(verify_preparation(raw_content,blueprints[row['path']],row),indent=2))
            for row in compositions:
                prepared,=[p for p in preparations if p['path']==row['path']]
                print(json.dumps(verify_composition(raw_content,blueprints[row['path']],prepared,row),indent=2))
            print(json.dumps(verify_bindings(raw_content,characters,compositions,bindings),indent=2))
            required,result=verify_albedo(raw_content,albedo,albedo_programs,palettes,customization_values)
            assert required<=set(content) and set(albedo)==set(materials)
            if customization_values is not None:
                required_keys={(f['VariableName'],bool(f['RawPrivateFlag'])) for m in materials.values() for f in m['customization']['PaletteFactors']}
                required_keys|={(op['VariableName'],op['Kind']=='SST1' or bool(op['RawFactoryFlag'])) for b in blueprints.values() for group in b['PrepareLists'] for op in group if op['VariableName']}
                assert set(customization_values)==required_keys
                print(json.dumps(dict(shared_customization_keys=len(required_keys),exact_namespace_value_agreement=True)))
            print(json.dumps(result,indent=2))
        if guards:
            assert set(guards) == {5, 6, 7}
            paths = {5:'appearance/mesh/thm_must_mining_outpost_r5_entrance_room_01_msh_r5.msh',
                     6:'appearance/mesh/thm_must_mining_outpost_r6_connecting_hall_mesh_r6.msh',
                     7:'appearance/mesh/thm_must_mining_outpost_r7_entrance_room_02_mesh_r7.msh'}
            for cell, path in paths.items():
                raw = indexed_triangles(read(path, content)); selected = []
                for tid, points in raw:
                    u = [points[1][j]-points[0][j] for j in range(3)]; v = [points[2][j]-points[0][j] for j in range(3)]
                    if cross(u, v) != (0, 0, 0):
                        selected.append((tid, points))
                camera_triangles.extend(points for _,points in selected)
                g = guards[cell]
                assert g['selector'] == 1 and g['source'] == g['eligible'] == len(raw) and g['degenerate'] == 0 and g['triangles'] == len(selected) == len(raw)
                canonical = struct.pack('<4i', len(raw), len(raw), 0, len(selected))
                for tid, points in selected:
                    canonical += struct.pack('<i9f', tid, *(v for point in points for v in point))
                assert hashlib.sha256(canonical).hexdigest().upper() == g['sha256']
        if camera_queries:
            print('chase_camera_audit='+json.dumps(verify_camera(camera_queries,camera_doors,camera_triangles)))
        path = 'object/creature/player/shared_human_male.iff'
        while path:
            assert path not in [r[0] for r in chain]
            root, = nodes(read(path, templates)); parent, layers = parse(root)
            chain.append((path, layers)); path = parent
    assert len(chain) == 5 and set(floors) == {5, 6, 7}
    values = {}
    for name in ('stepHeight', 'collisionRadius', 'collisionOffsetX', 'collisionOffsetZ'):
        for _, layers in chain:
            raw = layers.get('SCOT', (None, {}))[1].get(name)
            if raw is None or raw == b'\0 ':
                continue
            assert len(raw) == 6 and raw[:2] == b'\1 '
            values[name], = struct.unpack('<f', raw[2:]); break
        assert name in values
    assert volume['center_offset'] == [values['collisionOffsetX'], values['stepHeight']+values['collisionRadius'], values['collisionOffsetZ']]
    assert volume['radius'] == values['collisionRadius'] == .5
    phases = collections.Counter(); statuses = collections.Counter(); contacts = transfers = held = 0
    for row in rows:
        local = [f32(v) for v in row['local']]; vertices, triangles = floors[row['cell']]
        assert 0 <= row['triangle'] < len(triangles) and 0 <= row['parameter'] <= 1 and row['transfers'] in (0, 1)
        points = [vertices[i] for i in triangles[row['triangle']][:3]]
        assert abs(project(points, local)[1]-local[1]) < 1e-4
        pose = [[0, 0, 1], [0, 1, 0], [-1, 0, 0], [20, 4, -9]] if row['transformed'] else [[1, 0, 0], [0, 1, 0], [0, 0, 1], [0, 0, 0]]
        assert transform32(local, pose) == [f32(v) for v in row['world']]
        assert row['diagnostic'] is None and len(row['doors']) == 2
        assert all(0 <= d['fraction'] <= 1 and 0 <= d['retained'] <= 1 for d in row['doors'])
        if row['phase'] in ('deny', 'retreat', 'unknown'):
            assert row['cell'] in (5, 6)
        if row['phase'] in ('forward', 'recover') and row['index'] == 199:
            assert row['cell'] == 7 and row['status'] == 'Accepted'
        if row['phase'] in ('reverse', 'return') and row['index'] == 199:
            assert row['cell'] == 5 and row['status'] == 'Accepted'
        transfers += row['transfers']; contacts += row['kind'] is not None; held += any(d['held'] for d in row['doors'])
        phases[row['phase']] += 1; statuses[row['status']] += 1
    assert transfers == 24 and contacts == 182
    assert phases == {'forward':800,'reverse':800,'deny':400,'retreat':40,'unknown':40,'recover':400,'return':400}
    print(json.dumps(dict(original_assets=True, sources=len(sources)+len(templates), source_bytes=sum(r['Bytes'] for r in list(sources.values())+list(templates.values())),
                          content_sources=len(content), content_bytes=sum(r['Bytes'] for r in content.values()), content_external_reads_once=bool(content),
                          guard_triangles=sum(g['triangles'] for g in guards.values()), guard_geometry_independently_matches=bool(guards),
                          character_triangles=sum(g['triangles'] for g in characters.values()), character_geometry_independently_matches=bool(characters),
                          selected_character_animation=bool(animation),
                          ticks=len(rows), floor_plane_checks=len(rows), transfers=transfers, contacts=contacts, closing_hold_ticks=held, phases=phases,
                          statuses=statuses, template_volume_matches=True, reflected_owner_positions_match=True, dynamic_controller_independently_reimplemented=False), indent=2))


if __name__ == '__main__':
    main()
