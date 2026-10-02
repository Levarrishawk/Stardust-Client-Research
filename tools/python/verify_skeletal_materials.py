"""Independent original CSHD authored defaults, inline bindings and DDS mip agreement."""
import contextlib
import io
import struct
from verify_static_mesh_slice import nodes
from verify_customizable_shaders import parse
from verify_dds_pixels import inspect


def verify(raw, materials, textures, palettes=None,values=None,all_texture_choices=False):
    expected_textures = set()
    for path, native in materials.items():
        data = raw[path]
        root, = nodes(data)
        custom = parse(data) if root[0] == b'CSHD' else None
        if custom:
            if all_texture_choices:expected_textures.update(custom['choices'])
            version, = root[1]
            root = version[1][0]
            c = native['customization']
            assert c['TextureChoices'] == custom['choices']
            assert [tuple(s[k] for k in ('TextureTag','FirstChoice','ChoiceCount','VariableName','RawPrivateFlag','AuthoredDefault')) for s in c['TextureSelections']] == custom['selections']
            assert [tuple(s[k] for k in ('VariableName','RawPrivateFlag','FactorTag','PalettePath','AuthoredDefault')) for s in c['PaletteFactors']] == custom['factors']
        else:
            assert native['customization'] is None
        assert root[0] == b'SSHT'
        version, = root[1]
        fields = dict(version[1])
        effect = fields[b'NAME'][:-1].decode('ascii').lower().replace('\\','/')
        if '/' not in effect: effect = 'effect/'+effect
        assert native['effect'] == effect and effect in raw
        bindings = []
        for tag, payload in fields[b'TXMS']:
            assert tag == b'TXM '
            v, = payload
            parts = dict(v[1]); tag, flag = struct.unpack_from('<IB',parts[b'DATA'])
            assert flag == 0
            name = parts[b'NAME'][:-1].decode('ascii').lower().replace('\\','/')
            if custom:
                for target, first, count, variable, private, default in custom['selections']:
                    if target == tag: name = custom['choices'][first+min(max(default,0),count-1)]
            bindings.append(dict(Tag=tag,SourceFlag=flag,AssetPath=name)); expected_textures.add(name)
        assert native['textures'] == bindings
        coordinate_chunk, = fields[b'TCSS']; assert coordinate_chunk[0] == b'0000'
        coordinates = {str(tag):coordinate for tag,coordinate in struct.iter_unpack('<IB',coordinate_chunk[1])}
        assert native['coordinates'] == coordinates
    assert set(textures) == expected_textures
    palette_paths = set(); factor_count = 0
    if palettes is not None:
        from verify_palettes import parse as parse_palette
        assert set(palettes) == {p for p,m in materials.items() if m['customization'] is not None}
        for shader, row in palettes.items():
            custom = parse(raw[shader]); expected_factors = []
            for i,(variable,flag,target,path,default) in enumerate(custom['factors']):
                palette_paths.add(path)
                palette = parse_palette(raw[path]); entries = palette['entries']
                requested=default if values is None else values[variable,bool(flag)]
                selected = requested if 0 <= requested < len(entries) else 0
                color = None
                if entries:
                    r,g,b,raw_flag = entries[selected]
                    color = dict(RequestedIndex=requested,SelectedIndex=selected,OpaqueArgb=0xff000000|(r<<16)|(g<<8)|b,RawFlags=raw_flag)
                expected_factors.append(dict(AuthoredIndex=i,VariableName=variable,Private=bool(flag),FactorTag=target,PalettePath=path,RequestedIndex=requested,Color=color,UsedAuthoredDefault=values is None,MissingPalette=False))
            diagnostics=[f"PaletteIndexFallback:{f['AuthoredIndex']}:{f['RequestedIndex']}:0" for f in expected_factors if f['Color'] is not None and f['RequestedIndex']!=f['Color']['SelectedIndex']]
            assert row['factors'] == expected_factors and not row['missing'] and row['diagnostics']==diagnostics
            factor_count += len(expected_factors)
    mips = 0
    for path, native in textures.items():
        data = raw[path]
        height,width = struct.unpack_from('<II',data,12)
        assert (native['Width'],native['Height']) == (width,height)
        with contextlib.redirect_stdout(io.StringIO()): hashes = inspect(data)
        assert hashes == native['mips'], path
        mips += len(hashes)
    return dict(original_materials=len(materials),original_textures=len(textures),matched_mips=mips,
                authored_texture_defaults=True,palette_sources=len(palette_paths),authored_palette_factors=factor_count,palette_translation=False,generated_face=False,agreement=True)


def verify_texture_choices(raw,materials,rows):
    import base64
    seen=set();operations=0
    for row in rows:
        path,requested=row['path'],row['requested'];assert (path,requested) not in seen;seen.add((path,requested))
        custom=parse(raw[path]);root,=nodes(raw[path]);version,=root[1];inline=version[1][0];static_version,=inline[1];fields=dict(static_version[1])
        bindings=[]
        for tag,payload in fields[b'TXMS']:
            version,=payload;parts=dict(version[1]);tag,flag=struct.unpack_from('<IB',parts[b'DATA']);assert flag==0
            bindings.append(dict(Tag=tag,SourceFlag=0,Version=version[0].decode('ascii'),AssetPath=parts[b'NAME'][:-1].decode('ascii').lower().replace('\\','/'),BindingMetadata=base64.b64encode(parts[b'DATA']).decode('ascii')))
        selected=[]
        for ordinal,(tag,first,count,variable,private,default) in enumerate(custom['selections']):
            clamped=min(max(requested,0),count-1);name=custom['choices'][first+clamped]
            selected.append(dict(AuthoredIndex=ordinal,TextureTag=tag,RequestedValue=requested,ClampedValue=clamped,AssetPath=name,UsedAuthoredDefault=False));operations+=1
            for binding in bindings:
                if binding['Tag']==tag:binding['AssetPath']=name;break
        assert row['selections']==selected and row['textures']==bindings
    assert seen=={(path,value) for path in materials for value in (-2147483648,0,1,2,3,2147483647)}
    return dict(explicit_texture_selection_records=operations,profiles=6,namespace_and_clamped_path_agreement=True)
