"""Independent original SSHT inline face effect agreement; inputs stay private."""
import argparse
import json
import struct
from pathlib import Path
from verify_static_mesh_slice import nodes


def verify(shader_bytes, native):
    root, = nodes(shader_bytes)
    assert root[0] == b'SSHT'
    version, = root[1]
    assert native['Version'] == version[0].decode() and native['EffectPath'] == ''
    parts = dict(version[1])
    txms = parts[b'TXMS']
    assert len(txms) == len(native['Textures'])
    for (_, bindings), expected in zip(txms, native['Textures']):
        binding, = bindings
        data, = binding[1]
        assert data[0] == b'DATA'
        raw = data[1]
        assert expected == dict(Tag=struct.unpack_from('<I',raw)[0],SourceFlag=raw[4],AssetPath=None,Metadata=raw.hex().upper())
    effect, = parts[b'EFCT']
    data, *implementations = effect[1]
    assert effect[0] == b'0001' and data[1] == bytes([len(implementations),0])
    assert len(native['Implementations']) == len(implementations)
    offsets = [(0x10,1),(0x14,1),(0x18,1),(0x19,1),(0x1a,1),(0x1c,1),(0x20,1),(0x24,1),(0x28,1),(0x2c,1),(0x30,1),(0x34,4),(0x38,1),(0x3c,1),(0x40,4),(0x44,4),(0x4c,1),(0x50,4),(0x54,1),(0x58,1),(0x5c,1),(0x60,1),(0x74,4),(0x78,4),(0x7c,4)]
    for (_, values), expected in zip(implementations,native['Implementations']):
        impl, = values
        info, *passes = impl[1]
        assert impl[0] == b'0003' and expected['Version'] == '0003'
        assert expected['Capabilities'] == [] and expected['Options'] == [] and expected['Flag5'] is None and expected['Flag6'] is None
        assert expected['PhaseTag'] == struct.unpack_from('<I',info[1],1)[0]
        assert info[1][0] == len(passes) == len(expected['Passes'])
        for (_, value), decoded in zip(passes,expected['Passes']):
            version, = value
            raw,*children = version[1]
            assert version[0] == b'0007' and decoded['Version'] == '0007' and len(raw[1]) == 47
            cursor=1; fields={}
            for offset,width in offsets:
                fields[str(offset)] = int.from_bytes(raw[1][cursor:cursor+width],'little'); cursor+=width
            assert cursor == 47 and decoded['Fields'] == fields
            vertex, fragment = children
            if vertex[0] == b'PVSH':
                name, = vertex[1]
                assert decoded['VertexProgram'] == name[1][:-1].decode()
                pp, = fragment[1]; info,*textures = pp[1]
                assert decoded['PixelProgram'] == info[1][1:-1].decode() and info[1][0] == len(textures)
                for (_, value), binding in zip(textures,decoded['PixelTextures']):
                    raw, = value
                    assert raw[0] == b'0000' and len(raw[1]) == 16
                    assert binding == dict(Stage=raw[1][0],TextureTag=struct.unpack_from('<I',raw[1],1)[0],Version='0000',Metadata=raw[1].hex().upper())
            else:
                raw, = fragment[1]; fixed, = decoded['FixedStages']
                assert fixed == dict(TextureTag=struct.unpack_from('<I',raw[1],18)[0],CoordinateTag=struct.unpack_from('<I',raw[1],22)[0],Metadata=raw[1].hex().upper())
    return dict(implementations=len(implementations),pass_fields=25,agreement=True)


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('shader',type=Path);parser.add_argument('native_output',type=Path)
    args=parser.parse_args()
    line=next(line for line in args.native_output.read_text().splitlines() if line.startswith('INLINE_EFFECT_JSON:'))
    print(json.dumps(verify(args.shader.read_bytes(),json.loads(line.split(':',1)[1]))))
