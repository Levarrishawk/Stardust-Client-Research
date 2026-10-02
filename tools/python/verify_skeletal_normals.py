"""Independent original PS20 compressed-normal stage, instruction and sampled DDS audit."""
import hashlib
import base64
import io
import math
import struct
from PIL import Image
from verify_static_mesh_slice import nodes


def verify(raw,materials,programs,samples,images):
    required=set();maximum=0.0
    for path,row in materials.items():
        root,=nodes(raw[path]);version,=root[1];static=version[1][0];sv,=static[1];fields=dict(sv[1])
        name=fields[b'NAME'][:-1].decode('ascii').lower().replace('\\','/');name='effect/'+name if '/' not in name else name
        eroot,=nodes(raw[name]);ev,=eroot[1];candidates=[]
        for tag,parts in ev[1][1:]:
            iv,=parts
            for tag,parts in iv[1]:
                if tag!=b'PASS':continue
                pv,=parts;fields=dict(pv[1]);ps=fields.get(b'PPSH')
                if not ps:continue
                psv,=ps;program=psv[1][0][1][1:-1].decode('ascii')
                if not program.endswith('_cbmp_ps20.psh'):continue
                stages=[]
                for tag,stage in psv[1][1:]:
                    if tag!=b'PTXM':continue
                    stage_version,=stage;assert stage_version[0]==b'0002';data=stage_version[1]
                    # PTXM0002 is one stage byte followed by its little-endian texture tag.
                    stage,tag=struct.unpack('<BI',data);stages.append((tag,stage))
                candidates.append((program,stages))
        (program,stages),=candidates;assert (0x434e524d,1) in stages
        assert row['description']==dict(PixelProgram=program,TextureTag=0x434e524d,CoordinateSet=0)
        required.add(program)
    assert required==set(programs) and len(materials)==3
    for path,row in programs.items():
        root,=nodes(raw[path]);version,=root[1];fields=dict(version[1]);code=fields[b'PEXE'];words=struct.unpack('<%dI'%(len(code)//4),code)
        assert words[0]==0xffff0200 and row['bytecodeSha256']==hashlib.sha256(code).hexdigest().upper()
        at=1
        while words[at]&65535==65534:at+=1+((words[at]>>16)&32767)
        assert row['InstructionsSha256']==hashlib.sha256(code[at*4:]).hexdigest().upper()
        instructions=[]
        while words[at]&65535!=65535:
            size=(words[at]>>24)&15;instructions.append(words[at:at+size+1]);at+=size+1
        assert at==len(words)-1
        arithmetic=[i for i in instructions if i[0]&65535 not in (81,31,66)]
        assert [i[0]&65535 for i in arithmetic[:9]]==[2,2,2,5,4,2,7,6,36]
        assert arithmetic[0][2]&0xff0000==0xff0000 and arithmetic[1][2]&0xff0000==0x550000
        assert arithmetic[5][2]&0x0f000000==0x01000000  # negate squared XY before adding one
        assert instructions[0][2:]==(0xbf000000,0x3f800000,0x3f000000,0x3f266666)
        assert ('lerp(textureFactor.rgb, textureFactor2.rgb, hueMask)' in fields[b'PSRC'].decode('ascii'))==row['UsesAlphaHueMask']
    pixels={};outside={}
    for row in images:
        path=row['path'];data=raw[path];height,width=struct.unpack_from('<II',data,12);count=struct.unpack_from('<I',data,28)[0];assert data[84:88]==b'DXT5';offset=128;outside[path]=0
        for mip in range(count):
            w,h=max(1,width>>mip),max(1,height>>mip);size=((w+3)//4)*((h+3)//4)*16
            header=bytearray(data[:128]);struct.pack_into('<II',header,12,h,w);struct.pack_into('<I',header,20,size);struct.pack_into('<I',header,28,1)
            with Image.open(io.BytesIO(header+data[offset:offset+size])) as image:rgba=image.convert('RGBA').tobytes()
            pixels[path,mip]=rgba;outside[path]+=sum((rgba[i+3]/255*2-1)**2+(rgba[i+1]/255*2-1)**2>1 for i in range(0,len(rgba),4));offset+=size
        assert offset==len(data) and row['outsideDisk']==outside[path]
    for row in samples:
        rgba=pixels[row['path'],row['mip']];index=row['index']*4;sample=base64.b64decode(row['rgba']);assert sample==rgba[index:index+4]
        x=sample[3]/255*2-1;y=sample[1]/255*2-1;z=math.sqrt(abs(1-x*x-y*y));length=math.sqrt(x*x+y*y+z*z)
        expected=[x/length,y/length,z/length];error=max(abs(a-b) for a,b in zip(expected,row['normal']));assert error<=2e-6;maximum=max(maximum,error)
    assert len(images)==6 and {s['path'] for s in samples}==set(outside)
    return required,dict(original_normal_materials=3,programs=2,normal_images=6,sampled_normals=len(samples),outside_disk_texels=sum(outside.values()),maximum_error=maximum,instruction_stage_pixel_agreement=True)
