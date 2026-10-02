"""Independent original room material bindings and compiled PS1.1/PS2.0 point-input execution."""
import hashlib
import struct
import numpy as np
from verify_static_mesh_slice import nodes
from verify_skeletal_specular import execute


def execute11(code, diffuse, specular, samples, opacity):
    words=struct.unpack('<%dI'%(len(code)//4),code);assert words[0]==0xffff0101
    registers={(1,0):diffuse+[1],(1,1):specular+[1],(2,7):[0,0,0,opacity]};at=1
    def key(t):return ((t>>28)&7)|((t>>8)&24),t&2047
    def source(t):
        value=registers[key(t)];swizzle=(t>>16)&255;assert (t>>24)&15==0
        return [value[(swizzle>>(2*i))&3] for i in range(4)]
    while at<len(words):
        token=words[at];op=token&65535
        if op==65534:at+=1+((token>>16)&32767);continue
        if op==65535:assert at==len(words)-1;break
        arity={66:1,5:3,4:4,1:2}[op];args=words[at+1:at+1+arity];at+=1+arity;destination=args[0]
        if op==66:result=list(samples[key(destination)[1]])
        else:
            values=[source(t) for t in args[1:]]
            if op==5:result=[a*b for a,b in zip(*values)]
            elif op==4:result=[a*b+c for a,b,c in zip(*values)]
            else:result=values[0]
        assert (destination>>20)&15==0;mask=(destination>>16)&15;old=registers.setdefault(key(destination),[0]*4)
        for i in range(4):
            if mask&(1<<i):old[i]=result[i]
    return registers[0,0]


def verify(raw, programs, materials, cases):
    maximum=0;codes={};f32=lambda v:np.asarray(v,dtype=np.float32).astype(float).tolist()
    for row in programs:
        root,=nodes(raw[row['path']]);version,=root[1];code=dict(version[1])[b'PEXE'];words=struct.unpack('<%dI'%(len(code)//4),code);at=1
        while words[at]&65535==65534:at+=1+((words[at]>>16)&32767)
        assert hashlib.sha256(code[at*4:]).hexdigest().upper()==row['InstructionsSha256'];codes[row['path']]=code
    def name(v):return v.rstrip(b'\0').decode('ascii').lower().replace('\\','/')
    no_factor=0
    for row in materials:
        root,=nodes(raw[row['path']]);version,=root[1];fields=dict(version[1]);effect=name(fields[b'NAME']);effect='effect/'+effect if '/' not in effect else effect
        er,=nodes(raw[effect]);ev,=er[1];assert ev[1][0][1][1]==1
        selected=[]
        for _, impl in ev[1][1:]:
            iv,=impl
            for tag,parts in iv[1]:
                if tag!=b'PASS':continue
                pv,=parts;parts=dict(pv[1]);ps=parts.get(b'PPSH');vs=parts.get(b'PVSH')
                if ps is None or vs is None:continue
                pp,=ps;pf=dict(pp[1]);pixel=name(pf[b'DATA'][1:]);vertex=name(vs[0][1])
                if (pixel,vertex)==(row['PixelProgram'],row['VertexProgram']):selected.append((pv[0],parts,pf))
        (version_tag,parts,pf),=selected
        offsets=[(0x10,1),(0x14,1),(0x18,1),(0x19,1),(0x1A,1),(0x1C,1),(0x20,1),(0x24,1),(0x28,1),(0x2C,1),(0x30,1),(0x34,4),(0x38,1),(0x3C,1),(0x40,4),(0x44,4),(0x48,4),(0x4C,1),(0x4D,1),(0x50,4),(0x54,1),(0x58,1),(0x5C,1),(0x60,1),(0x64,1),(0x68,1),(0x6C,1),(0x70,1),(0x74,4),(0x78,4),(0x7C,4)]
        cursor=1;state={}
        for offset,width in offsets:
            if version_tag!=b'0009' and offset in (0x4D,0x64,0x68,0x6C,0x70):continue
            if version_tag==b'0007' and offset==0x48:continue
            state[offset]=int.from_bytes(parts[b'DATA'][cursor:cursor+width],'little');cursor+=width
        assert cursor==len(parts[b'DATA']) and row['TextureFactorTag']==state[0x40]
        bindings={}
        for _, txm in fields[b'TXMS']:
            tv,=txm;tf=dict(tv[1]);tag,flag=struct.unpack_from('<IB',tf[b'DATA']);assert flag==0;bindings[tag]=name(tf[b'NAME'])
        assert row['MainTexture']==bindings[0x4d41494e]
        if row['SecondaryTexture'] is not None:assert row['SecondaryTexture']==bindings[0x53504543 if row['kind']==0 else 0x454d4953]
        coordinates=dict(struct.iter_unpack('<IB',fields[b'TCSS'][0][1]));assert coordinates[0x4d41494e]==0
        factors=dict(struct.iter_unpack('<II',fields[b'TFNS'][0][1])) if b'TFNS' in fields else {}
        if state[0x40]:
            packed=factors[state[0x40]];assert f32(row['factor'])==f32([(packed>>s&255)/255 for s in (16,8,0,24)])
        else:assert row['factor'] is None;no_factor+=1
        mats={}
        if b'MATS' in fields:
            mv,=fields[b'MATS'];mp=mv[1];mats={struct.unpack('<I',mp[i][1])[0]:struct.unpack('<17f',mp[i+1][1]) for i in range(0,len(mp),2)}
        values=mats.get(0x4d41494e,(1,1,1,1,1,1,1,1,1,0,0,0,1,0,0,0,0))
        for offset,key in ((0,'ambient'),(4,'diffuse'),(8,'emissive'),(12,'specular')):assert f32(row[key])==list(values[offset+1:offset+4])+[values[offset]]
        assert row['power']==values[16]
    for case in cases:
        constants={3:[float(np.float32(case['strength']))]*3+[1],7:[int(case['fade']),int(case['bloom']),0,float(np.float32(case['opacity']))]}
        main=f32(case['main']);secondary=[0,0,0,float(np.float32(case['secondary']))];diffuse=f32(case['diffuse']);specular=f32(case['specular'])
        if case['kind']==0:result=execute11(codes[case['path']],diffuse,specular,{0:main,1:secondary},constants[7][3])
        else:result=execute(codes[case['path']],constants,{(1,0):diffuse+[1],(1,1):specular+[1],(3,0):[0,0,0,1],(3,1):[0,0,0,1]},{0:main,1:secondary})
        maximum=max(maximum,max(abs(a-b) for a,b in zip(result,f32(case['result']))))
    assert len(programs)==3 and len(materials)==18 and len(cases)==192 and maximum<1e-5
    return dict(original_room_materials=len(materials),original_pixel_programs=len(programs),compiled_pixel_cases=len(cases),maximum_error=maximum,unselected_factor_records=no_factor,original_vertex_runtime_equivalence=False)
