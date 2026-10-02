"""Independent original SWTS/base binding, selected-frame mip, geometry and exact source-union audit. No asset export."""
import argparse
import contextlib
import hashlib
import io
import json
import struct
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes, inspect
from verify_dds_pixels import inspect as pixels
from verify_creature_template_profile import parse
from dtla_corpus_inventory import parse as detail
from verify_scene_static_resources import ROOT, ROOT_PIN, APT, APT_PIN, CONFIG, text

SWTS = 'shader/anim_mission_terminal.sht'
SWTS_PIN = 'B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62'
BASE = 'shader/defaultemis.sht'
BASE_PIN = 'BD33741659BD3C322AE37DC8C0F4DDADBE940502DB273538DB6A34A9FE14D5F7'


def audit(root, configuration, output):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper() == CONFIG
    assert output.stat().st_size < 1048576
    content = output.read_text(encoding='utf-8-sig')
    assert 'PASS R8-ASSETS-01:' in content
    groups = {}
    for line in content.splitlines():
        if line.startswith('switch_'):
            key, value = line.split('=', 1)
            groups.setdefault(key, []).append(json.loads(value))
    cases = groups['switch_frame']; assert [c['frame'] for c in cases] == [0, 1]
    validation, = groups['switch_validation']
    assert validation['rejects'] >= 154 and validation['preflightReads'] == 0
    assert validation['originalFrames'] == 2 and validation['completeMeshesPerFrame'] == 4
    assert validation['distinctPixels'] and not validation['gpu'] and validation['configSha256'] == CONFIG
    summaries = []; frame_hashes = []; frame_pins = []
    with contextlib.ExitStack() as stack:
        indices = [(p, Index(stack.enter_context(f.open('rb')))) for p, f in archives(root, configuration)]
        for case in cases:
            data = {}; expected = set(); identities = {}
            def read(path):
                if path in data: return data[path]
                for priority, index in indices:
                    blob = index.lookup(path)
                    if blob is not None: break
                else: raise ValueError('Missing source ' + path)
                data[path] = blob; expected.add(path)
                identities[path] = dict(Path=path, Priority=priority, Bytes=len(blob), Sha256=hashlib.sha256(blob).hexdigest().upper())
                return blob
            path = ROOT; seen = set(); appearance = None
            while path:
                assert path not in seen; seen.add(path)
                node, = nodes(read(path)); parent, layers = parse(node)
                value = layers.get('SHOT', ('', {}))[1].get('appearanceFilename')
                if appearance is None and value is not None and value[0] == 1: appearance = text(value[1:])
                path = parent
            assert len(seen) == 4 and appearance == APT and identities[ROOT]['Sha256'] == ROOT_PIN
            apt, = nodes(read(APT)); version, = apt[1]; name, = version[1]
            assert apt[0] == b'APT ' and version[0] == b'0000' and name[0] == b'NAME' and identities[APT]['Sha256'] == APT_PIN
            lod = text(name[1]); children = ['appearance/' + p if not p.startswith('appearance/') else p for p in detail(read(lod))[3]]
            swts, = nodes(read(SWTS)); swts_version, = swts[1]
            assert identities[SWTS]['Sha256'] == SWTS_PIN and swts[0] == b'SWTS' and swts_version[0] == b'0000'
            fields = swts_version[1]; assert len(fields) == 10 and fields[0][0] == b'NAME' and text(fields[0][1]) == BASE
            assert fields[1][0] == b'DTST'; state, = fields[1][1]
            assert state[0] == b'0000' and len(state[1]) == 8 and struct.unpack('<2I', state[1]) == (8, 0x3dcccccd)
            frames = []
            for tag, payload in fields[2:]:
                assert tag == b'TEXT'
                frames.append(dict(TextureTag=struct.unpack_from('<I', payload)[0], TexturePath=text(payload[4:])))
            assert frames == case['frames'] and case['raw0'] == 8 and case['raw1'] == 0x3dcccccd
            assert [f['TexturePath'] for f in frames] == [f'texture/anim_mission_terminal_{i:02d}.dds' for i in range(1, 9)]
            selected = frames[case['frame']]; assert selected['TextureTag'] == 0x4d41494e and case['selectedPath'] == selected['TexturePath']
            materials = {}; expected_meshes = []; texture_paths = set()
            for mesh_path in children:
                raw = read(mesh_path); info = inspect(raw)
                mesh, = nodes(raw); mesh_version, = mesh[1]; sps_version, = mesh_version[1][1][1]
                shader_paths = list(dict.fromkeys(text(g[0][1]) for _, g in sps_version[1][1:]))
                mesh_textures = set(); effects = set()
                for shader_path in shader_paths:
                    shader, = nodes(read(shader_path))
                    if shader_path == SWTS:
                        shader, = nodes(read(BASE)); assert identities[BASE]['Sha256'] == BASE_PIN
                    assert shader[0] == b'SSHT'; shader_version, = shader[1]; assert shader_version[0] in (b'0000', b'0001')
                    ssht = dict(shader_version[1]); effect = text(ssht[b'NAME']); effect = effect if '/' in effect else 'effect/' + effect
                    effect_root, = nodes(read(effect)); assert effect_root[0] == b'EFCT'; effects.add(effect)
                    bindings = []
                    for tag, versions in ssht.get(b'TXMS', []):
                        assert tag == b'TXM '; binding_version, = versions; binding = dict(binding_version[1]); raw_binding = binding[b'DATA']
                        assert (binding_version[0] == b'0002' and len(raw_binding) == 12) or (binding_version[0] == b'0001' and (len(raw_binding) == 11 or (len(raw_binding) == 12 and raw_binding[-1] == 0)))
                        texture_tag = struct.unpack_from('<I', raw_binding)[0]; flag = raw_binding[4]; texture_path = text(binding[b'NAME'])
                        assert flag == 0
                        if shader_path == SWTS:
                            assert texture_tag == selected['TextureTag'] and texture_path == 'texture/defaulttexture.dds'
                            texture_path = selected['TexturePath']
                        read(texture_path); texture_paths.add(texture_path); mesh_textures.add(texture_path)
                        bindings.append(dict(Tag=texture_tag, SourceFlag=flag, AssetPath=texture_path))
                    material = dict(path=shader_path, effect=effect, textures=bindings)
                    if shader_path in materials: assert materials[shader_path] == material
                    materials[shader_path] = material
                expected_meshes.append(dict(path=mesh_path, vertices=info['vertices'], triangles=info['triangles'], hash=info['geometry_sha256'], shaders=shader_paths, textures=sorted(mesh_textures), effects=sorted(effects)))
            assert len(case['meshes']) == len(expected_meshes) == 4
            for got, wanted in zip(case['meshes'], expected_meshes):
                assert {**got, 'textures': sorted(got['textures']), 'effects': sorted(got['effects'])} == wanted
            assert {m['path']: m for m in case['materials']} == materials and len(materials) == 5
            native_textures = {t['path']: t['mips'] for t in case['textures']}; assert set(native_textures) == texture_paths and len(texture_paths) == 7
            with contextlib.redirect_stdout(io.StringIO()):
                for path, mips in native_textures.items(): assert pixels(read(path)) == mips
            assert {s['Path']: s for s in case['sources']} == identities and len(case['sources']) == len(expected) == 27
            assert case['counts'] == dict.fromkeys(expected, 1) and case['ExternalReads'] == 27
            assert case['SourceBytes'] == sum(i['Bytes'] for i in identities.values()) == 1401794
            assert case['baseSource'] == identities[BASE]
            assert not any(f['TexturePath'] in expected for i, f in enumerate(frames) if i != case['frame']) and 'texture/defaulttexture.dds' not in expected
            wire = bytes.fromhex(case['wire']); assert len(wire) == 47
            assert struct.unpack_from('<HIQ', wire) == (5, 0xfe89ddea, 0xfedcba9876543210)
            assert struct.unpack_from('<7I', wire, 14) == (0x80000000, 0x3e800000, 0xbf000000, 0x40000000, 0x42f70000, 0xc2350000, 1)
            assert struct.unpack_from('<I', wire, 42)[0] == 2907007746 and wire[-1] == 255
            frame_hashes.append(native_textures[selected['TexturePath']][0]); frame_pins.append(identities[selected['TexturePath']]['Sha256'])
            summaries.append(dict(frame=case['frame'], sources=len(expected), source_bytes=case['SourceBytes'], complete_meshes=4, materials=5, effects=4, textures=7, mips=sum(map(len, native_textures.values())), selected_source=identities[selected['TexturePath']], selected_mip0=frame_hashes[-1]))
    assert frame_hashes[0] != frame_hashes[1] and frame_pins[0] != frame_pins[1]
    return dict(frames=summaries, native_rejections=validation['rejects'], exact_source_union=True, raw_create_identity=True, gpu=False, runtime_switcher=False)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('root', 'configuration', 'native_output'): parser.add_argument(name, type=Path)
    args = parser.parse_args(); print(json.dumps(audit(args.root, args.configuration, args.native_output), indent=2))
