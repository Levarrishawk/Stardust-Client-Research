"""Original static-door coordinate/tangent packing and compiled PS20 point-input audit."""
import hashlib
import struct
import math
from verify_static_mesh_slice import nodes
from verify_skeletal_specular import execute


def verify(raw,programs,materials,tangents,cases,preview_tangents=()):
    assert len(programs)==len(materials)==1 and len(tangents)==2 and len(cases)==64
    program,=programs;material,=materials
    root,=nodes(raw[program['path']]);version,=root[1];code=dict(version[1])[b'PEXE'];words=struct.unpack('<%dI'%(len(code)//4),code);at=1
    while words[at]&65535==65534:at+=1+((words[at]>>16)&32767)
    assert hashlib.sha256(code[at*4:]).hexdigest().upper()==program['InstructionsSha256']
    root,=nodes(raw[material['path']]);version,=root[1];fields=dict(version[1]);coordinates=dict(struct.iter_unpack('<IB',fields[b'TCSS'][0][1]))
    assert coordinates=={0x444f5433:1,0x4d41494e:0,0x4e524d4c:0} and material['TangentCoordinateSet']==1
    bindings={}
    for _,parts in fields[b'TXMS']:
        tv,=parts;tf=dict(tv[1]);tag,flag=struct.unpack_from('<IB',tf[b'DATA']);assert flag==0;bindings[tag]=tf[b'NAME'].rstrip(b'\0').decode('ascii').replace('\\','/').lower()
    assert bindings=={0x4d41494e:material['MainTexture'],0x434e524d:material['NormalTexture']}
    effect=fields[b'NAME'].rstrip(b'\0').decode('ascii').replace('\\','/').lower();er,=nodes(raw[effect]);ev,=er[1];assert ev[1][0][1][1]==0
    selected=[]
    for _,parts in ev[1][1:]:
        iv,=parts
        for tag,parts in iv[1]:
            if tag!=b'PASS':continue
            pv,=parts;pf=dict(pv[1]);ps=pf.get(b'PPSH');vs=pf.get(b'PVSH')
            if ps is None or vs is None:continue
            pp,=ps;psfields=dict(pp[1]);pixel=psfields[b'DATA'][1:].rstrip(b'\0').decode('ascii');vertex=vs[0][1].rstrip(b'\0').decode('ascii')
            if pixel==material['PixelProgram'] and vertex==material['VertexProgram']:
                samplers=[struct.unpack('<BI',parts[0][1]) for tag,parts in pp[1] if tag==b'PTXM'];selected.append(samplers)
    assert selected==[[(0,0x4d41494e),(1,0x434e524d)]]
    mv,=fields[b'MATS'];parts=mv[1];mats={struct.unpack('<I',parts[i][1])[0]:struct.unpack('<17f',parts[i+1][1]) for i in range(0,len(parts),2)};values=mats[0x4d41494e]
    assert material['specular']==list(values[13:16])+[values[12]] and material['power']==values[16]
    count=degenerate=0;preview_error=0;preview_by_path={row['path']:row for row in preview_tangents}
    for row in tangents:
        root,=nodes(raw[row['path']]);version,=root[1];sv,=version[1][1][1];group=sv[1][1];primitive=group[1][2];parts=dict(primitive[1]);vv,=parts[b'VTXA'];info,data=vv[1];declaration,n=struct.unpack('<II',info[1]);assert declaration==0xd205 and len(data[1])==n*48 and row['count']==n
        records=list(struct.iter_unpack('<12f',data[1]));packed=b''.join(struct.pack('<4f',*v[8:12]) for v in records)
        assert hashlib.sha256(packed).hexdigest().upper()==row['sha256'];bad=sum(sum(x*x for x in v[8:11])<1e-12 for v in records);assert bad==row['degenerate']==2
        if row['path'] in preview_by_path:
            preview=preview_by_path[row['path']];assert preview['count']==n and preview['fallbacks']==bad
            for source,actual in zip(records,preview['values']):
                normal=source[3:6];nl=math.sqrt(sum(v*v for v in normal));normal=[v/nl for v in normal];t=source[8:11];d=sum(a*b for a,b in zip(normal,t));t=[a-d*b for a,b in zip(t,normal)]
                if sum(v*v for v in t)<1e-12:
                    index=min(range(3),key=lambda i:abs(normal[i]));axis=[int(i==index) for i in range(3)];d=normal[index];t=[a-d*b for a,b in zip(axis,normal)]
                length=math.sqrt(sum(v*v for v in t));expected=[v/length for v in t]+[source[11]];preview_error=max(preview_error,max(abs(a-b) for a,b in zip(actual,expected)))
        assert set(v[11] for v in records)=={-1,1};count+=n;degenerate+=bad
    def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
    maximum=0
    for row in cases:
        constants={1:list(map(f32,row['diffuse']))+[1],2:list(map(f32,row['specular']))+[1],5:material['specular'],6:[material['power'],0,0,0],7:[int(row['fade']),int(row['bloom']),0,f32(row['opacity'])]}
        inputs={(1,0):list(map(f32,row['vertexDiffuse']))+[1],(1,1):list(map(f32,row['vertexSpecular']))+[1],(3,0):[0,0,0,1],(3,1):[0,0,0,1],(3,2):list(map(f32,row['light']))+[0],(3,3):list(map(f32,row['half']))+[0]}
        result=execute(code,constants,inputs,{0:list(map(f32,row['main'])),1:list(map(f32,row['normal']))});maximum=max(maximum,max(abs(a-b) for a,b in zip(result,map(f32,row['result']))))
    assert not preview_tangents or len(preview_by_path)==2 and preview_error<2e-6
    assert count==110 and degenerate==4 and maximum<1e-5
    return dict(materials=1,tangent_vertices=count,zero_tangents=degenerate,compiled_pixel_cases=len(cases),maximum_error=maximum,preview_tangent_error=preview_error,preview_tangents_checked=bool(preview_tangents),original_zero_tangent_device_behavior='unknown')
