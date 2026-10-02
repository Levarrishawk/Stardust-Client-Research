"""Independent original source/factor/program/mip and selected albedo-color audit; reuses accepted albedo/DDS parsers."""
import argparse
import contextlib
import hashlib
import io
import json
import math
import struct
from pathlib import Path
from PIL import Image
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_creature_template_profile import parse
from verify_skeletal_descriptors import sat, mlod
from verify_skeletal_albedo import verify as verify_albedo
from verify_dds_pixels import inspect as pixels
from verify_scene_creature_resources import OBJECT, OBJECT_PIN, SAT_PIN, CONFIG_PIN, checksum

PROGRAM_PINS = {
    'pixel_program/h_color2_specmap_ps11.psh': ('95E44154F4107866CF23FD5C77E082573211135BE17E1CE679243A2AEB85EB92', 1399),
    'pixel_program/h_specmap_ps11.psh': ('4A3155359640270C2274495AFD1E8E68E6139CCBC358DDA14B66D50BAF8B340C', 1124)}


def audit(root, configuration, profile, output):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper() == CONFIG_PIN
    assert output.stat().st_size < 1048576
    content = output.read_text(encoding='utf-8-sig'); assert 'PASS R10-ASSETS-01:' in content
    groups = {}
    for line in content.splitlines():
        if line.startswith('creature_albedo_'):
            tag, value = line.split('=', 1); groups.setdefault(tag, []).append(json.loads(value))
    create, = groups['creature_albedo_create']; default, = groups['creature_albedo_default']; validation, = groups['creature_albedo_validation']
    catalog = json.loads(profile.read_text(encoding='utf-8')); assert catalog['version'] == 1
    supplied, = [e for e in catalog['entries'] if e['templateCrc'] == 2937962913]
    assert supplied['recipe'] == 'creatureDetail0' and supplied['objectPath'] == OBJECT and supplied['objectSha256'] == OBJECT_PIN and supplied['appearanceSha256'] == SAT_PIN
    assert create['Recipe'] == dict(TemplateCrc=checksum(OBJECT), ObjectPath=OBJECT, ObjectSha256=OBJECT_PIN, AppearanceSha256=SAT_PIN, Recipe=0)
    assert create['root'] == OBJECT
    wire = bytes.fromhex(create['wire']); assert len(wire) == 47
    assert struct.unpack_from('<HIQ', wire) == (5, 0xfe89ddea, 0xfedcba9876543210)
    assert struct.unpack_from('<7I', wire, 14) == (0x80000000, 0x3e800000, 0xbf000000, 0x40000000, 0x42f70000, 0xc2350000, 1)
    assert struct.unpack_from('<I', wire, 42)[0] == 2937962913 and wire[46] == 255
    sources = {s['Path']: s for s in groups['creature_albedo_source']}; assert len(sources) == len(groups['creature_albedo_source']) == 39
    previous = {s['Path']: s for s in default['sources']}; assert len(previous) == len(default['sources']) == 37
    assert set(sources) == set(previous) | set(PROGRAM_PINS) and not (set(previous) & set(PROGRAM_PINS))
    assert all(sources[p] == s for p, s in previous.items())
    for record, expected, count, size in ((create, sources, 39, 4686371), (default, previous, 37, 4683848)):
        assert record['ExternalReads'] == count and record['SourceBytes'] == sum(s['Bytes'] for s in expected.values()) == size
        assert record['counts'] == dict.fromkeys(expected, 1)
    raw = {}
    with contextlib.ExitStack() as stack:
        indices = [(p, Index(stack.enter_context(f.open('rb')))) for p, f in archives(root, configuration)]
        for path, identity in sources.items():
            for priority, index in indices:
                blob = index.lookup(path)
                if blob is not None: break
            else: raise ValueError('Missing effective original source ' + path)
            assert identity == dict(Path=path, Priority=priority, Bytes=len(blob), Sha256=hashlib.sha256(blob).hexdigest().upper())
            raw[path] = blob
    assert sources[OBJECT]['Sha256'] == OBJECT_PIN and sources[create['appearance']]['Sha256'] == SAT_PIN
    for path, (pin, size) in PROGRAM_PINS.items(): assert sources[path]['Priority'] == 40 and sources[path]['Bytes'] == size and sources[path]['Sha256'] == pin
    inherited = []; path = OBJECT
    while path:
        assert path not in inherited; inherited.append(path); node, = nodes(raw[path]); path, _ = parse(node)
    assert len(inherited) == 5
    appearance = sat(raw[create['appearance']]); selected_meshes = {mlod(raw[p])['SerializedPaths'][0] for p in appearance['MeshGenerators']}
    assert selected_meshes == {'appearance/mesh/hum_m_body_l0.mgn', 'appearance/mesh/hum_m_head_l0.mgn'} and selected_meshes <= set(raw)
    materials = {m['path']: m for m in groups['creature_albedo_material']}; programs = {p['path']: p for p in groups['creature_albedo_program']}
    assert len(materials) == len(groups['creature_albedo_material']) == 4 and set(programs) == set(PROGRAM_PINS) and len(groups['creature_albedo_program']) == 2
    required, albedo_report = verify_albedo(raw, materials, programs, {})
    assert required == set(PROGRAM_PINS)
    textures = {t['path']: t['mips'] for t in groups['creature_albedo_texture']}; assert len(textures) == len(groups['creature_albedo_texture']) == 10
    assert set(textures) == {p for p in sources if p.endswith('.dds')}
    with contextlib.redirect_stdout(io.StringIO()):
        for path, mips in textures.items(): assert mips == pixels(raw[path])
    colors = groups['creature_albedo_color']; assert len(colors) == 4 and {r['shader'] for r in colors} == set(materials)
    changed = 0
    for color in colors:
        shader, = nodes(raw[color['shader']]); ssht = shader[1][0][1][0]; fields = dict(ssht[1][0][1]); main_paths = []
        for tag, binding in fields[b'TXMS']:
            assert tag == b'TXM '; version, = binding; values = dict(version[1])
            if struct.unpack_from('<I', values[b'DATA'])[0] == 0x4d41494e: main_paths.append(values[b'NAME'][:-1].decode('ascii'))
        assert main_paths == [color['texture']] and color['texture'] in textures
        with Image.open(io.BytesIO(raw[color['texture']])) as image:
            rgba_image = image.convert('RGBA'); index = color['pixel']; assert 0 <= index < image.width * image.height
            rgba = rgba_image.getpixel((index % image.width, index // image.width))
        assert bytes(rgba).hex().upper() == color['rgba']
        factor = materials[color['shader']]['factors']; first = factor['FirstArgb']; second = factor['SecondArgb']
        rgb = [v / 255 for v in rgba[:3]]; alpha = rgba[3] / 255
        a = [(first >> shift & 255) / 255 for shift in (16, 8, 0)]; b = [(second >> shift & 255) / 255 for shift in (16, 8, 0)]
        hue = [x + (y - x) * alpha for x, y in zip(a, b)] if factor['UsesAlphaHueMask'] else a
        expected = [x * y for x, y in zip(rgb, hue)]
        assert all(math.isclose(x, y, rel_tol=0, abs_tol=2e-7) for x, y in zip(rgb, color['main']))
        assert all(math.isclose(x, y, rel_tol=0, abs_tol=2e-7) for x, y in zip(expected, color['color']))
        differs = math.dist(rgb, expected) > 1e-5; assert color['differs'] == differs; changed += differs
    assert changed == 3
    assert validation == dict(rejects=16, changedColors=3, sourceCloneChecks=39, programOwnershipChecks=2, textureOwnershipChecks=10,
                             cancellationReads=1, initializerPrepared=True, initializerBudgetFailures=2, archivesClosedBeforeTransfer=1,
                             defaultSourceCount=37, albedoSourceCount=39, configSha256=CONFIG_PIN, gpu=False, fullLighting=False)
    return dict(default_sources=37, default_bytes=4683848, albedo_sources=39, albedo_bytes=4686371, added_source_bytes=2523,
                materials=4, programs=[sources[p] for p in sorted(PROGRAM_PINS)], textures=10,
                texture_mips=sum(map(len, textures.values())), selected_colors=colors, changed_colors=changed, original_inherited_templates=5,
                exact_source_union=True, source_hashes=True, pixel_hashes=True, factors=albedo_report, native_rejections=16,
                initializer_accounting=True, gpu=False, full_lighting=False)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('root', 'configuration', 'catalog_profile', 'native_output'): parser.add_argument(name, type=Path)
    args = parser.parse_args(); print(json.dumps(audit(args.root, args.configuration, args.catalog_profile, args.native_output), indent=2))
