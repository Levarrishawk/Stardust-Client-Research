"""Independent original PS1.1 albedo instruction, factor identity and selected-color audit."""
import hashlib
import struct
from verify_static_mesh_slice import nodes
from verify_customizable_shaders import parse


def verify(raw,materials,programs,palettes,values=None):
    assert len(materials)==4 and len(programs)==2
    required=set()
    for path,native in materials.items():
        root,=nodes(raw[path]);version,=root[1];static=version[1][0];static_version,=static[1];fields=dict(static_version[1])
        effect=fields[b'NAME'][:-1].decode('ascii').lower().replace('\\','/');effect='effect/'+effect if '/' not in effect else effect
        eroot,=nodes(raw[effect]);ev,=eroot[1];candidates=[]
        for tag,parts in ev[1][1:]:
            assert tag==b'IMPL';iv,=parts
            for tag,pv in iv[1]:
                if tag!=b'PASS':continue
                version,=pv;parts=dict(version[1]);ps=parts.get(b'PPSH')
                if not ps:continue
                psv,=ps;data=psv[1][0][1];name=data[1:-1].decode('ascii')
                if name.endswith('_ps11.psh'):candidates.append((name,parts[b'DATA']))
        (program,state),=candidates;required.add(program);first,second=struct.unpack_from('<2I',state,18)
        tfns,=fields[b'TFNS'];assert tfns[0]==b'0000';colors=dict(struct.iter_unpack('<2I',tfns[1]))
        for variable,flag,tag,palette,default in parse(raw[path])['factors']:
            entries=struct.unpack_from('<H',raw[palette],22)[0];requested=default if values is None else values[variable,bool(flag)];selected=requested if 0<=requested<entries else 0
            r,g,b,_=struct.unpack_from('<4B',raw[palette],24+selected*4);colors[tag]=0xff000000|(r<<16)|(g<<8)|b
        mask=programs[program]['UsesAlphaHueMask'];assert first==0x4d41494e and second==(0x48554542 if mask else 0)
        expected=dict(PixelProgram=program,UsesAlphaHueMask=mask,FirstTag=first,SecondTag=second,FirstArgb=colors[first],SecondArgb=colors[second] if mask else colors[first])
        assert native['factors']==expected
    assert required==set(programs)
    for path,native in programs.items():
        root,=nodes(raw[path]);version,=root[1];fields=dict(version[1]);code=fields[b'PEXE'];words=struct.unpack('<'+'I'*(len(code)//4),code)
        assert root[0]==b'PSHP' and version[0]==b'0000' and words[0]==0xffff0101
        assert native['bytecodeSha256']==hashlib.sha256(code).hexdigest().upper()
        cursor=1
        while words[cursor]&65535==65534:cursor+=1+((words[cursor]>>16)&32767)
        instructions=[]
        while cursor<len(words):
            opcode=words[cursor]&65535
            if opcode==65535:assert cursor==len(words)-1;break
            width={1:3,4:5,5:4,18:5,66:2}[opcode];instructions.append(words[cursor:cursor+width]);cursor+=width
        mask=native['UsesAlphaHueMask'];assert instructions[:2]==[(66,0xb00f0000),(66,0xb00f0001)]
        if mask:
            assert [row[0]&65535 for row in instructions]==[66,66,5,18,5,4,1]
            assert instructions[3]==(18,0x80070001,0xb0ff0000,0xa0e40004,0xa0e40003)
        else:
            assert [row[0]&65535 for row in instructions]==[66,66,5,5,4,1]
            assert instructions[4]==(4,0x80070000,0x80e40000,0xa0e40003,0x80e40001)
        source=fields[b'PSRC'].rstrip(b'\0').decode('ascii')
        assert ('lerp(textureFactor.rgb, textureFactor2.rgb, hueMask)' in source)==mask
    return required,dict(original_albedo_materials=4,pixel_programs=2,alpha_mask_materials=sum(row['factors']['UsesAlphaHueMask'] for row in materials.values()),instruction_factor_color_agreement=True,full_lighting=False)
