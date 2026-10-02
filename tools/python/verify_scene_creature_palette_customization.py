"""Independent source/palette range, explicit profile, selected factor and pixel/color audit. No asset export."""
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
from verify_customizable_shaders import parse
from verify_skeletal_albedo import verify as albedo
from verify_dds_pixels import inspect as pixels
from verify_scene_creature_resources import OBJECT, OBJECT_PIN, SAT_PIN, CONFIG_PIN


def fields(value, expected):
    if type(value) is not dict or set(value) != set(expected): raise ValueError('Profile properties')


def pairs(items):
    result = {}
    for key, value in items:
        if key in result: raise ValueError('Duplicate property')
        result[key] = value
    return result


def integer(token):
    if not token or any(c not in '-0123456789' for c in token): raise ValueError('Integer syntax')
    value = int(token)
    if not -2147483648 <= value <= 2147483647: raise ValueError('Int32 range')
    return value


def profile(raw):
    if not 1 <= len(raw) <= 65536: raise ValueError('Profile bytes')
    def invalid(_): raise ValueError('Noninteger number')
    root = json.loads(raw.decode('utf-8', errors='strict'), object_pairs_hook=pairs, parse_int=integer, parse_float=invalid, parse_constant=invalid)
    def depth(value):
        if type(value) is dict: return 1 + max(map(depth, value.values()), default=0)
        if type(value) is list: return 1 + max(map(depth, value), default=0)
        return 0
    if depth(root) > 4: raise ValueError('Profile depth')
    fields(root, ('version', 'values'))
    if type(root['version']) is not int or root['version'] != 1 or type(root['values']) is not list or not 1 <= len(root['values']) <= 4096: raise ValueError('Profile version/count')
    result = {}
    for row in root['values']:
        fields(row, ('variableName', 'factoryFlag', 'value'))
        name, flag, value = row['variableName'], row['factoryFlag'], row['value']
        if type(name) is not str or not 1 <= len(name) <= 256 or any(not 33 <= ord(c) <= 126 for c in name) or type(flag) is not bool or type(value) is not int: raise ValueError('Profile types')
        key = name, flag
        if key in result: raise ValueError('Duplicate namespace key')
        result[key] = value
    return result


def audit(root, configuration, color_profile, output):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper() == CONFIG_PIN and output.stat().st_size < 1048576
    supplied = profile(color_profile.read_bytes())
    content = output.read_text(encoding='utf-8-sig'); assert 'PASS R11-ASSETS-01:' in content
    groups = {}
    for line in content.splitlines():
        if line.startswith('scene_colors_'):
            key, value = line.split('=', 1); groups.setdefault(key, []).append(json.loads(value))
    input_record, = groups['scene_colors_input']; validation, = groups['scene_colors_validation']; controls, = groups['scene_colors_controls']
    sources = {s['Path']: s for s in input_record['sources']}; assert len(sources) == len(input_record['sources']) == 39
    assert input_record['reads'] == dict.fromkeys(sources, 1) and input_record['ExternalReads'] == 39 and input_record['SourceBytes'] == sum(s['Bytes'] for s in sources.values()) == 4686371
    assert sources[OBJECT]['Sha256'] == OBJECT_PIN and input_record['recipe']['ObjectSha256'] == OBJECT_PIN and input_record['recipe']['AppearanceSha256'] == SAT_PIN
    wire = bytes.fromhex(input_record['wire']); assert len(wire) == 47 and struct.unpack_from('<HIQ', wire) == (5, 0xfe89ddea, 0xfedcba9876543210)
    assert struct.unpack_from('<7I', wire, 14) == (0x80000000, 0x3e800000, 0xbf000000, 0x40000000, 0x42f70000, 0xc2350000, 1)
    assert struct.unpack_from('<I', wire, 42)[0] == 2937962913 and wire[46] == 255
    raw = {}
    with contextlib.ExitStack() as stack:
        indices = [(p, Index(stack.enter_context(f.open('rb')))) for p, f in archives(root, configuration)]
        for path, identity in sources.items():
            for priority, index in indices:
                blob = index.lookup(path)
                if blob is not None: break
            else: raise ValueError('Missing original source ' + path)
            assert identity == dict(Path=path, Priority=priority, Bytes=len(blob), Sha256=hashlib.sha256(blob).hexdigest().upper())
            raw[path] = blob
    programs = {p['path']: p for p in groups['scene_colors_program']}; assert len(programs) == len(groups['scene_colors_program']) == 2
    textures = {p['path']: p['mips'] for p in groups['scene_colors_texture']}; assert len(textures) == len(groups['scene_colors_texture']) == 10
    assert set(textures) == {p for p in sources if p.endswith('.dds')}
    with contextlib.redirect_stdout(io.StringIO()):
        for path, mips in textures.items(): assert pixels(raw[path]) == mips
    choices = {c['name']: c for c in groups['scene_colors_choice']}; assert set(choices) == {'zero', 'profile'}
    shader_paths = {r['path'] for r in choices['zero']['factors']}; ranges = {}; factor_rows = []
    for shader in shader_paths:
        for name, flag, tag, palette, default in parse(raw[shader])['factors']:
            count = struct.unpack_from('<H', raw[palette], 22)[0]; assert 1 <= count <= 1024
            key = name, bool(flag); ranges[key] = min(ranges.get(key, 1023), count - 1)
            factor_rows.append((shader, name, bool(flag), tag, palette))
    expected_controls = [dict(Key=dict(VariableName=k[0], Private=k[1]), MaximumValue=v) for k, v in sorted(ranges.items())]
    assert controls == expected_controls and ranges == {('index_color_skin', False): 255, ('index_color_2', True): 255, ('index_color_3', True): 1}
    assert set(supplied) == set(ranges) and all(0 <= v <= ranges[k] for k, v in supplied.items())
    for name, choice in choices.items():
        values = {(r['VariableName'], r['Private']): r['value'] for r in choice['values']}; assert len(values) == len(choice['values']) == 3
        assert values == (supplied if name == 'profile' else dict.fromkeys(ranges, 0))
        material = {r['path']: r for r in choice['factors']}; required, _ = albedo(raw, material, programs, {}, values)
        assert required == set(programs)
        expected_selected = []
        for shader, variable, private, tag, palette in factor_rows:
            index = values[variable, private]; red, green, blue, flags = struct.unpack_from('<4B', raw[palette], 24 + index * 4)
            expected_selected.append(dict(shader=shader, VariableName=variable, Private=private, PalettePath=palette, RequestedIndex=index,
                UsedAuthoredDefault=False, MissingPalette=False, Color=dict(RequestedIndex=index, SelectedIndex=index, OpaqueArgb=0xff000000 | red << 16 | green << 8 | blue, RawFlags=flags)))
        assert sorted(choice['selected'], key=lambda r: (r['shader'], r['VariableName'])) == sorted(expected_selected, key=lambda r: (r['shader'], r['VariableName']))
    changed = []
    pixel_records = groups['scene_colors_pixel']; assert len(pixel_records) == 8
    for record in pixel_records:
        with Image.open(io.BytesIO(raw[record['texture']])) as image: rgba = image.convert('RGBA').getpixel((0, 0))
        assert record['pixel'] == 0 and record['rgba'] == bytes(rgba).hex().upper()
        factor, = [r['factors'] for r in choices[record['name']]['factors'] if r['path'] == record['shader']]
        first = [(factor['FirstArgb'] >> s & 255) / 255 for s in (16, 8, 0)]; second = [(factor['SecondArgb'] >> s & 255) / 255 for s in (16, 8, 0)]
        hue = [a + (b - a) * rgba[3] / 255 for a, b in zip(first, second)] if factor['UsesAlphaHueMask'] else first
        expected = [rgba[i] / 255 * hue[i] for i in range(3)]
        assert all(math.isclose(a, b, rel_tol=0, abs_tol=2e-7) for a, b in zip(expected, record['color']))
    for shader in shader_paths:
        a, = [p for p in pixel_records if p['name'] == 'zero' and p['shader'] == shader]
        b, = [p for p in pixel_records if p['name'] == 'profile' and p['shader'] == shader]
        if math.dist(a['color'], b['color']) > 1e-5: changed.append(shader)
    assert set(changed) == {'shader/hum_m_body.sht', 'shader/hum_m_head.sht'}
    for case in groups['scene_colors_bad_profile']:
        try: profile(bytes.fromhex(case['hex']))
        except (ValueError, UnicodeError): pass
        else: raise AssertionError('Native rejected profile accepted by oracle')
    assert len(groups['scene_colors_bad_profile']) == 11
    assert validation == dict(rejects=38, controls=3, choices=2, sharingChecks=50, noExtraReads=True, originalUnchanged=True, callerValuesOwned=True,
        sourceCloneChecks=39, boundedEnumeration=4097, enumerationDisposed=1, intersectionMaximum=1, initializerPrepared=True, faceGenerated=False, gpu=False)
    return dict(source_count=39, source_bytes=4686371, controls=expected_controls, profiles=2, factor_materials=4, palette_selections_per_choice=len(factor_rows),
        texture_mips=sum(map(len, textures.values())), independently_evaluated_pixels=8, changed_sample_materials=sorted(changed), negatives=38,
        public_profile_sha256=hashlib.sha256(color_profile.read_bytes()).hexdigest().upper(), source_hashes=True, palette_indices=True, factor_colors=True, no_source_reload=True, face_generation=False, gpu=False)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('root', 'configuration', 'color_profile', 'native_output'): parser.add_argument(name, type=Path)
    args = parser.parse_args(); print(json.dumps(audit(args.root, args.configuration, args.color_profile, args.native_output), indent=2))
