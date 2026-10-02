"""Independent original source/geometry/pixel audit and UI-excluded rendered difference."""
import contextlib
import hashlib
import io
import json
import struct
import sys
from pathlib import Path
from PIL import Image, ImageChops
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import inspect, nodes
from verify_dds_pixels import inspect as pixels
from verify_creature_template_profile import parse
from verify_scene_static_resources import text
from dtla_corpus_inventory import parse as detail

root, config, native_path, gpu_path, headless_path, cancel_path, capture0, capture1 = map(Path, sys.argv[1:])
assert hashlib.sha256(config.read_bytes()).hexdigest().upper() == '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
def frames(path):
    content = path.read_text(encoding='utf-8-sig')
    result = [json.loads(s.split('=',1)[1]) for s in content.splitlines() if s.startswith('switch_frame=')]
    assert len(result) == 2 and [r['frame'] for r in result] == [0,1]
    return content, result
native, cases = frames(native_path)
gpu, gpu_cases = frames(gpu_path)
headless, headless_cases = frames(headless_path)
assert cases == gpu_cases == headless_cases
assert '"rejects":7' in native and '"preflightReads":0' in native
geometry = []; selected_hashes = []; source_pins = []
with contextlib.ExitStack() as stack:
    indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority,path in archives(root,config)]
    def read(path):
        for priority,index in indices:
            value = index.lookup(path)
            if value is not None: return priority,value
        raise ValueError('Missing original source '+path)
    for case in cases:
        sources = {r['Path']:r for r in case['sources']}
        assert len(sources) == len(case['sources']) == case['ExternalReads'] == 27
        assert case['SourceBytes'] == sum(r['Bytes'] for r in sources.values()) == 1401794
        assert case['counts'] == dict.fromkeys(sources,1)
        for path,row in sources.items():
            priority,blob = read(path)
            assert row['Priority'] == priority and row['Bytes'] == len(blob) and row['Sha256'] == hashlib.sha256(blob).hexdigest().upper()
        # Original inheritance selects the APT; APT resolves the actual four-child detail graph.
        path = 'object/tangible/terminal/shared_terminal_mission.iff'; seen = []; appearance = None
        while path:
            assert path not in seen and path in sources; seen.append(path)
            node, = nodes(read(path)[1]); parent,layers = parse(node)
            value = layers.get('SHOT',('',{}))[1].get('appearanceFilename')
            if appearance is None and value is not None and value[0] == 1: appearance = text(value[1:])
            path = parent
        assert len(seen) == 4 and appearance in sources
        apt, = nodes(read(appearance)[1]); version, = apt[1]; name, = version[1]
        assert apt[0] == b'APT ' and name[0] == b'NAME'
        lod = text(name[1]); children = ['appearance/'+p if not p.startswith('appearance/') else p for p in detail(read(lod)[1])[3]]
        assert [m['path'] for m in case['meshes']] == children and len(children) == 4
        hashes = []
        with contextlib.redirect_stdout(io.StringIO()):
            for mesh in case['meshes']:
                actual = inspect(read(mesh['path'])[1])
                assert actual['geometry_sha256'] == mesh['hash'] and actual['vertices'] == mesh['vertices'] and actual['triangles'] == mesh['triangles']
                hashes.append(mesh['hash'])
            assert len(case['textures']) == 7 and sum(len(t['mips']) for t in case['textures']) == 62
            for texture in case['textures']: assert pixels(read(texture['path'])[1]) == texture['mips']
        geometry.append(hashes)
        swts, = nodes(read('shader/anim_mission_terminal.sht')[1]); v, = swts[1]; fields = v[1]
        assert swts[0] == b'SWTS' and v[0] == b'0000' and text(fields[0][1]) == 'shader/defaultemis.sht'
        state, = fields[1][1]; assert struct.unpack('<2I',state[1]) == (8,0x3DCCCCCD)
        original_frames = [dict(TextureTag=struct.unpack_from('<I',body)[0],TexturePath=text(body[4:])) for tag,body in fields[2:] if tag == b'TEXT']
        assert original_frames == case['frames'] and case['raw0'] == 8 and case['raw1'] == 0x3DCCCCCD
        chosen = original_frames[case['frame']]['TexturePath']; assert chosen == case['selectedPath']
        assert not any(f['TexturePath'] in sources for i,f in enumerate(original_frames) if i != case['frame']) and 'texture/defaulttexture.dds' not in sources
        assert case['baseSource'] == sources['shader/defaultemis.sht']
        assert case['wire'] == struct.pack('<HIQ7fIB',5,0xFE89DDEA,0xFEDCBA9876543210,-0.,.25,-.5,2,123.5,-45.25,struct.unpack('<f',struct.pack('<I',1))[0],2907007746,255).hex().upper()
        selected_hashes.append(next(t['mips'][0] for t in case['textures'] if t['path'] == chosen)); source_pins.append(sources[chosen]['Sha256'])
assert geometry[0] == geometry[1] and selected_hashes[0] != selected_hashes[1] and source_pins[0] != source_pins[1]
for content in [gpu,headless]:
    for marker in ['selections=64','negative_choices=4','resource_ids_fixed=true','mip_readback=true','PASS: switch teardown']: assert marker in content
assert 'PASS: switch canceled first_lookup=true archives_closed=true gpu_publications=0' in cancel_path.read_text(encoding='utf-8-sig')
summary = next(json.loads(s.split('=',1)[1]) for s in gpu.splitlines() if s.startswith('switch_capture='))
with Image.open(capture0) as a, Image.open(capture1) as b:
    assert a.size == b.size == (summary['width'],summary['height'])
    region = (0,summary['regionY'],a.width,a.height); assert summary['regionY'] == 180
    difference = ImageChops.difference(a.convert('RGB').crop(region),b.convert('RGB').crop(region))
    raw = difference.tobytes(); changed = sum(raw[i:i+3] != b"\0\0\0" for i in range(0,len(raw),3))
    assert changed == summary['changed'] and changed > 10 and summary['camera_fixed'] and summary['geometry_fixed'] and summary['detail'] == 3
    bbox = difference.getbbox(); assert bbox is not None
    # Report the observed changed rectangle; captures are inspected against the screen.
    screen_box = [bbox[0],bbox[1]+180,bbox[2],bbox[3]+180]
print(json.dumps(dict(pass_=True,source_frames=2,sources_each=27,mips_each=62,geometry_unchanged=True,
    selected_mip0=selected_hashes,ui_excluded=True,rendered_changed_pixels=changed,changed_screen_bbox=screen_box),indent=2))
