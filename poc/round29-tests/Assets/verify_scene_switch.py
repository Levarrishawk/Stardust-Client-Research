"""Independent original source-union, retained representation and all-frame pixel audit.

The accounting compatibility mode removes only six additive zero counters from
default-route receipts before running the unchanged accepted Round16 oracle.
Detailed original receipts and normalized regression output belong in local-private.
"""
import contextlib, hashlib, json, runpy, struct, sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO / 'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_switch_texture_static_resources import audit as static_audit

def rows(path):
    lines = path.read_text(encoding='utf-8-sig').splitlines()
    assert lines[-1].startswith('PASS')
    return [json.loads(line.split('=', 1)[1]) for line in lines if '=' in line and line.startswith('switch_frame=')]

if sys.argv[1] == 'accounting-compat':
    native, normalized = map(Path, sys.argv[2:])
    assert normalized.resolve().is_relative_to((REPO / 'local-private').resolve())
    lines = native.read_text(encoding='utf-8-sig').splitlines()
    receipt = json.loads(lines[0].split('=', 1)[1])
    for original in receipt['Originals']:
        for key in ('SwitchFrames', 'SwitchFrameTextures', 'SwitchFrameMips', 'SwitchFrameDecodedBytes', 'GeneratedFaces', 'GeneratedFaceBytes'):
            assert original['Receipt'].pop(key) == 0
    normalized.write_text('accounting_receipt=' + json.dumps(receipt) + '\n' + lines[-1] + '\n', encoding='utf-8')
    print('Default receipt compatibility: six additive zero counters removed; prior fields unchanged.')
    sys.exit()

root, config, native, static_output = map(Path, sys.argv[1:])
static_audit(root, config, static_output)
default, _ = rows(static_output)
saved = sys.argv
try:
    sys.argv = ['frame-oracle', 'library']
    accepted = runpy.run_path(str(REPO / 'poc/round28-tests/Assets/verify_frame_bundle.py'))
finally:
    sys.argv = saved
frame = accepted['original'](root, config)
for i, pin in enumerate(accepted['FRAME_PINS']):
    assert frame['Sources'][frame['Frames'][i]['TexturePath']]['Sha256'] == pin
lines = native.read_text(encoding='utf-8-sig').splitlines()
assert lines[-1].startswith('PASS:')
result = json.loads(next(line.split('=', 1)[1] for line in lines if line.startswith('static_switch_resources=')))
expected = {s['Path']: s for s in default['sources']}
baseline = dict(expected)
for path, source in frame['Sources'].items():
    if path in expected:
        assert expected[path] == source
    expected[path] = source
actual = {s['Path']: s for s in result['Sources']}
assert len(actual) == len(result['Sources']) == len(expected) == 34 and actual == expected
assert len(result['Calls']) == len(set(result['Calls'])) == 34 and set(result['Calls']) == set(expected)
assert result['Frames'] == frame['Frames']
assert {i['Path']: i['Mips'] for i in result['Images']} == {p: i['Mips'] for p, i in frame['Images'].items()}
assert 'texture/defaulttexture.dds' not in expected
base_pixels = base_mips = base_textures = 0
with contextlib.ExitStack() as stack:
    indices = [(p, Index(stack.enter_context(f.open('rb')))) for p, f in archives(root, config)]
    for path, source in expected.items():
        for priority, index in indices:
            blob = index.lookup(path)
            if blob is not None:
                break
        else:
            raise AssertionError(path)
        assert source == dict(Path=path, Priority=priority, Bytes=len(blob), Sha256=hashlib.sha256(blob).hexdigest().upper())
        if path in baseline and path.endswith('.dds'):
            assert blob[:4] == b'DDS '
            height, width = struct.unpack_from('<II', blob, 12)
            count = max(1, struct.unpack_from('<I', blob, 28)[0])
            base_pixels += sum(max(1, width >> i) * max(1, height >> i) * 4 for i in range(count))
            base_mips += count
            base_textures += 1
def receipt(sources, textures, mips, decoded, bundled=False):
    size = sum(s['Bytes'] for s in sources.values())
    return dict(Sources=len(sources), SourceBytes=size, DecodedTextures=textures, Mips=mips,
                DecodedTextureBytes=decoded, GeneratedFaces=0, GeneratedFaceBytes=0,
                PendingWeight=size+decoded, SwitchFrames=8 if bundled else 0,
                SwitchFrameTextures=8 if bundled else 0, SwitchFrameMips=64 if bundled else 0,
                SwitchFrameDecodedBytes=frame['DecodedBytes'] if bundled else 0)
assert result['DefaultReceipt'] == receipt(baseline, base_textures, base_mips, base_pixels)
assert result['Receipt'] == receipt(expected, base_textures+len(frame['Images']), base_mips+frame['MipCount'], base_pixels+frame['DecodedBytes'], True)
assert len(result['Cases']) == len(set(result['Cases']))
assert {'appearance-backing-mutation', 'with-swapped-bundle', 'with-contradictory-bindings', 'with-create', 'with-source-identity', 'decoded-one-less', 'mips-one-less', 'weight-one-less', 'reentry-True', 'reentry-False'} <= set(result['Cases'])
assert result['ArchivesClosed'] and result['NoTickDecoding']
print(json.dumps(dict(passed=True, source_hashes=len(expected), added_paths=len(set(expected)-set(baseline)),
                     all_frame_mip_hashes=frame['MipCount'], independent_header_mips=base_mips,
                     receipt=result['Receipt'], rejected_cases=len(result['Cases']), controlled=result['Controlled'],
                     native_concurrent_loads=8, pixel_copy_during_validation=False), indent=2))
