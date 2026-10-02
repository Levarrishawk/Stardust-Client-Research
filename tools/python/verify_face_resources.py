"""Independent declared face DDS mip/palette dependency agreement."""
import contextlib
import hashlib
import io
import struct
from verify_dds_pixels import inspect
from verify_static_mesh_slice import nodes


def verify(raw,native):
    mip_count=tail_count=0
    assert len(native['shaders']) == 2 and len(native['textures']) == 32 and len(native['palettes']) == 3
    for path in native['shaders']:
        root,=nodes(raw[path]);version,=root[1]
        effect_values,=[value for tag,value in version[1] if tag==b'EFCT']; effect,=effect_values; _,*impl=effect[1]
        assert next(e['candidates'] for e in native['effects'] if e['path']==path) == len(impl)
    for image in native['textures']:
        data=raw[image['path']];h,w=struct.unpack_from('<2I',data,12);count,=struct.unpack_from('<I',data,28)
        flags,=struct.unpack_from('<I',data,80);compressed=bool(flags&4);cursor=128
        for level in range(max(1,count)):
            width,height=max(1,w>>level),max(1,h>>level)
            cursor+=((width+3)//4)*((height+3)//4)*(8 if data[84:88]==b'DXT1' else 16) if compressed else width*height*4
        tail=data[cursor:]
        assert image['terminalBlock'] == tail.hex().upper()
        if tail:
            assert data[84:88]==b'DXT3' and len(tail)==16 and (w,h,count)==(64,32,6)
            tail_count+=1
        with contextlib.redirect_stdout(io.StringIO()):hashes=inspect(data[:cursor])
        assert [m['sha256'] for m in image['mips']] == hashes
        assert [(m['Width'],m['Height']) for m in image['mips']] == [(max(1,w>>i),max(1,h>>i)) for i in range(count)]
        mip_count+=count
    for palette in native['palettes']:
        data=raw[palette['path']];version,count=struct.unpack_from('<2H',data,20);extent=24+count*4
        canonical=struct.pack('<Hi',version,count)
        for r,g,b,flag in struct.iter_unpack('<4B',data[24:extent]):canonical+=struct.pack('<4BI',r,g,b,flag,0xff000000|(r<<16)|(g<<8)|b)
        canonical+=struct.pack('<i',len(data)-extent)+data[extent:]
        assert count==palette['entries'] and hashlib.sha256(canonical).hexdigest().upper()==palette['sha256']
    return dict(original_face_resources=True,shaders=2,textures=32,mips=mip_count,palettes=3,retained_terminal_blocks=tail_count,agreement=True,generated_face=False)


def verify_preparation(raw,blueprint,native):
    bindings=[{} for _ in blueprint['ShaderPaths']];factors=[]
    for path in blueprint['ShaderPaths']:
        root,=nodes(raw[path]);version,=root[1]
        tfns,=[v for t,v in version[1] if t==b'TFNS'];chunk,=tfns
        factors.append({str(tag):color for tag,color in struct.iter_unpack('<2I',chunk[1])})
    supplied={(v['VariableName'],v['FactoryFlag']):v['Value'] for v in native['values']}
    result=[]
    for op in [op for group in blueprint['PrepareLists'] for op in group]:
        kind,target,tag=op['Kind'],op['TargetIndex'],op['TextureTag'];requested=selected=color=None
        if kind in ('SSTC','SST1'):
            source=op['SourceIndex']
            if kind=='SST1':
                requested=supplied[op['VariableName'],True];selected=min(max(requested,0),op['SourceCount']-1);source+=selected
            path=blueprint['TexturePaths'][source];bindings[target][str(tag)]=path
        else:
            assert kind=='STFP';requested=supplied[op['VariableName'],bool(op['RawFactoryFlag'])];path=op['PalettePath']
            data=raw[path];count=struct.unpack_from('<H',data,22)[0];selected=requested if 0<=requested<count else count-1
            r,g,b,_=struct.unpack_from('<4B',data,24+selected*4);color=0xff000000|(r<<16)|(g<<8)|b;factors[target][str(tag)]=color
        result.append(dict(Kind=kind,TargetIndex=target,TextureTag=tag,RequestedValue=requested,SelectedValue=selected,ResourcePath=path,Argb=color))
    assert native['Operations']==result
    assert native['ShaderSlots']==[dict(Slot=i,ShaderPath=path,TextureBindings=bindings[i],TextureFactors=factors[i]) for i,path in enumerate(blueprint['ShaderPaths'])]
    return dict(original_face_preparation=True,requested=native['requested'],slots=len(bindings),operations=len(result),agreement=True)


def verify_programs(raw,shader_paths,native):
    required=set()
    def visit(records):
        for tag,value in records:
            if tag==b'PPSH':
                version,=value;data=version[1][0][1]
                assert data[0]==1 and data[-1]==0
                required.add(data[1:-1].decode('ascii').lower().replace('\\','/'))
            elif isinstance(value,list):visit(value)
    for path in shader_paths:visit(nodes(raw[path]))
    assert {program['path'] for program in native}==required and len(required)==1
    for program in native:
        root,=nodes(raw[program['path']]);version,=root[1]
        assert root[0]==b'PSHP' and version[0]==b'0000'
        source,code=version[1]
        assert source[0]==b'PSRC' and source[1][-1]==0 and code[0]==b'PEXE'
        words=struct.unpack('<11I',code[1])
        assert words[0]==0xffff0101 and words[-1]==0xffff
        # Decode opcode, masks, register kind/index and source swizzles independently.
        assert words[1]==66 and words[2]==0xb00f0000
        assert words[3]==5 and words[4]==0x80070000
        assert words[5]==0xb0e40000 and words[6]==0xa0e40003
        assert words[7]==0x40000001 and words[8]==0x80080000 and words[9]==0xb0ff0000
        assert program['TextureStage']==words[2]&0x7ff
        assert program['FactorRegister']==words[6]&0x7ff
        assert program['bytecode']==code[1].hex().upper()
    return required
