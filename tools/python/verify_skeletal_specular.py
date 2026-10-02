"""Independent raw MATL packing and selected PS20 instruction execution audit.

Executes only the fixed observed opcode subset on supplied point texel fixtures.
This is not a shader compiler, rasterizer or original-device precision emulator.
"""
import base64
import hashlib
import io
import math
import struct
from PIL import Image
from verify_static_mesh_slice import nodes


def execute(code, constants, inputs, samples):
    words=struct.unpack('<%dI'%(len(code)//4),code);assert words[0]==0xffff0200
    registers={(2,k):list(v) for k,v in constants.items()};registers.update(inputs);at=1
    def key(token):return ((token>>28)&7)|((token>>8)&24),token&2047
    def source(token):
        value=registers[key(token)];swizzle=(token>>16)&255;result=[value[(swizzle>>(2*i))&3] for i in range(4)]
        modifier=(token>>24)&15;assert modifier in (0,1)
        return [-v for v in result] if modifier else result
    while at<len(words):
        token=words[at];opcode=token&65535
        if opcode==65534:at+=1+((token>>16)&32767);continue
        if opcode==65535:assert at==len(words)-1;break
        count=(token>>24)&15;args=words[at+1:at+1+count];assert len(args)==count;at+=count+1
        if opcode==31:continue
        destination=args[0]
        if opcode==81:result=list(struct.unpack('<4f',struct.pack('<4I',*args[1:])))
        elif opcode==66:result=list(samples[key(args[2])[1]])
        else:
            values=[source(a) for a in args[1:]]
            if opcode==1:result=values[0]
            elif opcode==2:result=[a+b for a,b in zip(*values)]
            elif opcode==4:result=[a*b+c for a,b,c in zip(*values)]
            elif opcode==5:result=[a*b for a,b in zip(*values)]
            elif opcode==6:result=[1/values[0][0]]*4
            elif opcode==7:result=[1/math.sqrt(abs(values[0][0]))]*4
            elif opcode==8:result=[sum(a*b for a,b in zip(values[0][:3],values[1][:3]))]*4
            elif opcode==32:result=[abs(values[0][0])**values[1][0]]*4
            elif opcode==36:
                length=math.sqrt(sum(v*v for v in values[0][:3]));assert length>0;result=[v/length for v in values[0][:3]]+[0]
            elif opcode==88:result=[b if a>=0 else c for a,b,c in zip(*values)]
            else:raise AssertionError('Unsupported selected shader opcode '+str(opcode))
        modifier=(destination>>20)&15;assert modifier in (0,1)
        if modifier:result=[max(0,min(1,v)) for v in result]
        old=registers.setdefault(key(destination),[0]*4);mask=(destination>>16)&15
        for i in range(4):
            if mask&(1<<i):old[i]=result[i]
    return registers[8,0]


def verify(raw,fields,materials,samples):
    decoded={};maximum=0;images={}
    def f32s(values):return list(struct.unpack("<%df"%len(values),struct.pack("<%df"%len(values),*values)))
    for path,row in fields.items():
        root,=nodes(raw[path]);version,=root[1]
        if root[0]==b'CSHD':root=version[1][0];version,=root[1]
        mats,=dict(version[1])[b'MATS'];parts=mats[1]
        records={struct.unpack('<I',parts[i][1])[0]:struct.unpack('<17f',parts[i+1][1]) for i in range(0,len(parts),2)}
        values=list(records[row['tag']]);assert f32s(row['raw'])==values
        colors=[values[i+1:i+4]+[values[i]] for i in (0,4,8,12)]
        assert [f32s(row[k]) for k in ('ambient','diffuse','emissive','specular')]==colors and row['power']==values[16]
        assert [f32s(v) for v in row['vertexConstants']]==[colors[1],colors[0],colors[3],colors[2],[values[16],0,0,0]]
        assert f32s(row['pixelSpecular'])==colors[3] and row['pixelPower']==[values[16],0,0,0];decoded[path]=row
    def color(argb):return [(argb>>s&255)/255 for s in (16,8,0)]+[1]
    def pixel(row):
        path=row['path']
        if path not in images:
            with Image.open(io.BytesIO(raw[path])) as image:images[path]=image.convert('RGBA').tobytes()
        data=images[path];index=row['index']*4;rgba=data[index:index+4];assert rgba==base64.b64decode(row['rgba'])
        return [v/255 for v in rgba]
    for sample in samples:
        path=sample['path'];material=materials[path];fields=decoded[path]
        assert sample['hue']==(not material['MaskUsesMainAlpha'])
        root,=nodes(raw[material['PixelProgram']]);version,=root[1];code=dict(version[1])[b'PEXE']
        constants={1:sample['diffuseLight']+[1],2:sample['specularLight']+[1],3:color(sample['first']),4:color(sample['second']),5:fields['specular'],6:[fields['power'],0,0,0],7:[int(sample['AlphaFadeEnabled']),int(sample['BloomEnabled']),0,sample['AlphaFadeOpacity']]}
        inputs={(1,0):sample['vertexDiffuse']+[1],(1,1):sample['vertexSpecular']+[1],(3,0):[0,0,0,1],(3,1):[0,0,0,1],(3,2):sample['light']+[0],(3,3):sample['half']+[0]}
        result=execute(code,constants,inputs,{0:pixel(sample['diffuse']),1:pixel(sample['normal']),2:pixel(sample['mask'])})
        error=max(abs(a-b) for a,b in zip(result,sample['output']));maximum=max(maximum,error);assert error<=1e-5,(path,sample['index'],result,sample['output'],error)
    assert len(decoded)==4 and len(materials)==3 and len(samples)==192
    return dict(original_material_fields=4,specular_materials=3,supplied_point_texel_lighting_cases=len(samples),compiled_instruction_agreement=True,maximum_error=maximum)


def verify_vertex_lit(raw,programs,materials,samples):
    maximum=0;images={};required=set(programs)
    for path,row in programs.items():
        root,=nodes(raw[path]);version,=root[1];parts=dict(version[1]);code=parts[b'PEXE'];words=struct.unpack('<%dI'%(len(code)//4),code);at=1
        assert words[0]==0xffff0200 and hashlib.sha256(code).hexdigest().upper()==row['bytecodeSha256']
        while words[at]&65535==65534:at+=1+((words[at]>>16)&32767)
        assert hashlib.sha256(code[at*4:]).hexdigest().upper()==row['InstructionsSha256']
        assert row['UsesAlphaHueMask'] and not row['ScalesBloomRgb']  # Selected original mouth family.
    for path,row in materials.items():
        root,=nodes(raw[path]);version,=root[1];static=version[1][0];sv,=static[1];fields=dict(sv[1]);effect=fields[b'NAME'][:-1].decode('ascii').lower().replace('\\','/');effect='effect/'+effect if '/' not in effect else effect
        root,=nodes(raw[effect]);version,=root[1];candidates=[]
        for tag,parts in version[1][1:]:
            iv,=parts
            for tag,parts in iv[1]:
                if tag!=b'PASS':continue
                pv,=parts;ps=dict(pv[1]).get(b'PPSH')
                if not ps:continue
                psv,=ps;program=psv[1][0][1][1:-1].decode('ascii')
                if not program.endswith('_ps20_nodot3.psh'):continue
                stages=[]
                for tag,parts in psv[1][1:]:
                    assert tag==b'PTXM';stage,=parts;assert stage[0]==b'0002';stages.append(struct.unpack('<BI',stage[1]))
                candidates.append((program,stages))
        assert candidates==[(row['PixelProgram'],[(0,0x4d41494e),(1,0x53504543)])]
        assert row['TextureTag']==0x53504543 and row['CoordinateSet']==0 and row['power']==300
    def color(value):return [(value>>s&255)/255 for s in (16,8,0)]+[1]
    def pixel(row):
        path=row['path']
        if path not in images:
            with Image.open(io.BytesIO(raw[path])) as image:images[path]=image.convert('RGBA').tobytes()
        index=row['index']*4;data=images[path][index:index+4];assert data==base64.b64decode(row['rgba']);return [v/255 for v in data]
    for row in samples:
        root,=nodes(raw[materials[row['path']]['PixelProgram']]);version,=root[1];code=dict(version[1])[b'PEXE']
        constants={3:color(row['first']),4:color(row['second']),7:[int(row['AlphaFadeEnabled']),int(row['BloomEnabled']),0,row['AlphaFadeOpacity']]}
        inputs={(1,0):row['vertexDiffuse']+[1],(1,1):row['vertexSpecular']+[1],(3,0):[0,0,0,1]}
        result=execute(code,constants,inputs,{0:pixel(row['diffuse']),1:pixel(row['mask'])});error=max(abs(a-b) for a,b in zip(result,row['output']));maximum=max(maximum,error);assert error<=1e-5
    assert len(programs)==1 and len(materials)==1 and len(samples)==64
    return required,dict(original_vertex_lit_materials=1,programs=1,supplied_point_texel_lighting_cases=64,compiled_instruction_agreement=True,two_color_bloom_rgb_unscaled=True,maximum_error=maximum)
