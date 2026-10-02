"""Independent raw BTRT live envelope/command agreement; no rendering or proprietary output."""
import hashlib
import struct
from verify_static_mesh_slice import nodes


def verify(data, native):
    root, = nodes(data); assert root[0] == b'BTRT'
    version, = root[1]; assert version[0] == b'0001'
    parts = dict(version[1])
    assert list(parts) == [b'INFO',b'DEST',b'SHTM',b'TXTS',b'VBS ',b'IBS ',b'CAM ',b'RCMS',b'PCMS']
    words = struct.unpack('<'+'I'*(len(parts[b'DEST'])//4),parts[b'DEST'])
    assert len(words) == 3+words[2]
    assert (native['Width'],native['Height']) == words[:2] and native['DestinationFormatIds'] == list(words[3:])
    assert native['TemplateValue'] == struct.unpack('<I',parts[b'INFO'])[0]
    def text(value): return value[:-1].decode('ascii').lower().replace('\\','/')
    def table(value):
        info,*entries = value; assert info[0] == b'INFO' and struct.unpack('<I',info[1])[0] == len(entries)
        return entries
    assert native['ShaderPaths'] == [text(value) for tag,value in table(parts[b'SHTM'])]
    txinfo, = parts[b'TXTS']; assert txinfo[0] == b'INFO'
    count, = struct.unpack_from('<I',txinfo[1]); names = txinfo[1][4:].split(b'\0'); assert names.pop() == b'' and len(names) == count
    assert native['TexturePaths'] == [n.decode('ascii').lower().replace('\\','/') for n in names]
    vertices = []
    for tag, vertex in table(parts[b'VBS ']):
        assert tag == b'VTXA'; v, = vertex; assert v[0] == b'0003'; info, payload = v[1]
        declaration,count = struct.unpack('<2I',info[1]); assert declaration == 0x1101 and len(payload[1]) == count*20
        vertices.append(dict(declaration=declaration,count=count,sha256=hashlib.sha256(payload[1]).hexdigest().upper()))
    assert native['vertices'] == vertices and not table(parts[b'IBS '])
    camera, = parts[b'CAM ']; assert camera[0] == b'PCAM' and native['CameraScalar'] == struct.unpack('<f',camera[1])[0]
    render_sets = []
    for tag, render in table(parts[b'RCMS']):
        assert tag == b'SRSC'; info,*commands = render; shader,count = struct.unpack('<2I',info[1]); assert len(commands) == count
        assert all(tag == b'RTFC' for tag,_ in commands)
        render_sets.append(dict(ShaderIndex=shader,VertexArrayIndices=[struct.unpack('<I',value)[0] for _,value in commands]))
    assert native['RenderSets'] == render_sets
    prepares = []
    for tag, prepare in table(parts[b'PCMS']):
        assert tag == b'PCMD'; tops, = prepare; assert tops[0] == b'TOPS'; operations = []
        for tag, raw in table(tops[1]):
            first = count = variable = palette = flag = None
            if tag == b'SSTC': target,texture,first = struct.unpack('<3I',raw)
            elif tag == b'SST1':
                target,texture,first = struct.unpack_from('<3I',raw); end = raw.index(0,12)
                variable = raw[12:end].decode('ascii'); count, = struct.unpack('<I',raw[end+1:])
            else:
                assert tag == b'STFP'; target,texture = struct.unpack_from('<HI',raw)
                first_end = raw.index(0,6); second_end = raw.index(0,first_end+1)
                assert second_end+2 == len(raw)
                palette = raw[6:first_end].decode('ascii').lower().replace('\\','/')
                variable = raw[first_end+1:second_end].decode('ascii'); flag = raw[-1]
            operations.append(dict(Kind=tag.decode('ascii'),TargetIndex=target,TextureTag=texture,SourceIndex=first,SourceCount=count,VariableName=variable,PalettePath=palette,RawFactoryFlag=flag))
        prepares.append(operations)
    assert native['PrepareLists'] == prepares
    return dict(original_face_blueprint=True,destination_formats=len(words)-3,texture_names=len(names),
                shaders=len(native['ShaderPaths']),vertex_arrays=len(vertices),render_sets=len(render_sets),prepare_lists=len(prepares),agreement=True,generated_face=False)
