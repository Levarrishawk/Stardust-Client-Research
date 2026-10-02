"""Independent original character SHT/EFCT/TCSS vertex-family and source identity audit."""
import hashlib
import struct
from verify_static_mesh_slice import nodes


def verify(raw,programs,materials):
    (program,)=programs
    assert program['path']=='vertex_program/a_specmap_bump_vs20_for_ps20.vsh'
    assert program['functionsPath']=='vertex_program/include/functions.inc'
    assert program['sha256']==hashlib.sha256(raw[program['path']]).hexdigest().upper()=='0159BA23D91FA517A6BDF16488B4C95A0CA599DC448AF12CEACC48C3448362D6'
    assert program['functionsSha256']==hashlib.sha256(raw[program['functionsPath']]).hexdigest().upper()=='CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15'
    assert {r['path'] for r in materials}=={'shader/hum_m_body.sht','shader/hum_m_head.sht','shader/hum_m_face.sht'}
    for row in materials:
        root,=nodes(raw[row['path']]);version,=root[1];static=version[1][0];sv,=static[1];fields=dict(sv[1])
        assert dict(struct.iter_unpack('<IB',fields[b'TCSS'][0][1]))=={0x444f5433:1,0x4d41494e:0,0x4e524d4c:0}
        name=fields[b'NAME'][:-1].decode('ascii').lower().replace('\\','/');name='effect/'+name if '/' not in name else name
        eroot,=nodes(raw[name]);ev,=eroot[1];candidates=[]
        for tag,parts in ev[1][1:]:
            iv,=parts
            for tag,parts in iv[1]:
                if tag!=b'PASS':continue
                pv,=parts;fields=dict(pv[1]);ps=fields.get(b'PPSH')
                if not ps:continue
                psv,=ps;pixel=psv[1][0][1][1:-1].decode('ascii')
                if pixel!=row['PixelProgram']:continue
                vs,=fields[b'PVSH'];vertex=vs[1][:-1].decode('ascii');candidates.append(vertex)
        assert candidates==[program['path']] and row['VertexProgram']==program['path'] and row['TangentCoordinateSet']==1
    return dict(original_materials=len(materials),shared_vertex_programs=1,shared_functions=1,compiled_vertex_precision='unknown',original_normalize_zero_behavior='unknown')
