"""Independent lower-detail original material/source closure and selected PS20 pixel audit."""
import argparse
import base64
import contextlib
import hashlib
import io
import json
import posixpath
import re
import struct
from pathlib import Path
from PIL import Image
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_creature_template_profile import parse
from verify_skeletal_descriptors import sat,mlod,strings
from verify_skeletal_geometry import inspect
from verify_skeletal_materials import verify
from verify_skeletal_specular import execute
from verify_skeletal_vertex_lit_sources import EXPECTED,VERTEX

SHADER='shader/hum_m_body_sm.sht'; EFFECT='effect/h_specmap.eft'; TEXTURE='texture/hum_m_sm.dds'; PIXEL='pixel_program/h_specmap_ps20_nodot3.psh'; ALBEDO='pixel_program/h_specmap_ps11.psh'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
OFFSETS=[(0x10,1),(0x14,1),(0x18,1),(0x19,1),(0x1A,1),(0x1C,1),(0x20,1),(0x24,1),(0x28,1),(0x2C,1),(0x30,1),(0x34,4),(0x38,1),(0x3C,1),(0x40,4),(0x44,4),(0x48,4),(0x4C,1),(0x4D,1),(0x50,4),(0x54,1),(0x58,1),(0x5C,1),(0x60,1),(0x64,1),(0x68,1),(0x6C,1),(0x70,1),(0x74,4),(0x78,4),(0x7C,4)]


def audit(root,configuration,output):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper()==CONFIG
    assert output.stat().st_size<2*1048576
    text=output.read_text(encoding='utf-8-sig'); assert 'PASS R3-ASSETS-01:' in text
    runs={d:dict(sources={},materials={},textures={},palettes={},passes=[],implementations=[],geometry={}) for d in (2,3)}; samples=[]; detail=None
    for line in text.splitlines():
        if '=' not in line:continue
        tag,value=line.split('=',1)
        if tag in ('lower_shading_materials_begin','lower_shading_materials_end'):detail=int(value) if tag.endswith('begin') else None;continue
        if tag.startswith('lower_shading_'):
            r=json.loads(value)
            if tag=='lower_shading_sample':samples.append(r);continue
            run=runs[r['detail']]
            if tag=='lower_shading_source':assert r['source']['Path'] not in run['sources'];run['sources'][r['source']['Path']]=r['source']
            elif tag=='lower_shading_descriptor':assert 'descriptor' not in run;run['descriptor']=r
            elif tag=='lower_shading_material':assert 'description' not in run;run['description']=r
            elif tag=='lower_shading_pass':run['passes'].append(r)
            elif tag=='lower_shading_implementation':run['implementations'].append(r)
            elif tag=='lower_shading_geometry':assert r['path'] not in run['geometry'];run['geometry'][r['path']]=r
        elif tag in ('skeletal_material','skeletal_texture','skeletal_palette_defaults'):
            assert detail is not None;r=json.loads(value);group={'skeletal_material':'materials','skeletal_texture':'textures','skeletal_palette_defaults':'palettes'}[tag];assert r['path'] not in runs[detail][group];runs[detail][group][r['path']]=r
    raw={};priorities={}
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(root,configuration)]
        for run in runs.values():
            for path,pin in run['sources'].items():
                if path not in raw:
                    for priority,index in indices:
                        data=index.lookup(path)
                        if data is not None:raw[path]=data;priorities[path]=priority;break
                    else:raise ValueError('Missing original source '+path)
                assert priorities[path]==pin['Priority'] and len(raw[path])==pin['Bytes'] and hashlib.sha256(raw[path]).hexdigest().upper()==pin['Sha256']
    summaries=[];identities=[]
    for detail,run in runs.items():
        desc=run['descriptor'];sources=run['sources'];r=run['description'];assert set(desc['counts'])==set(sources) and set(desc['counts'].values())=={1}
        assert sources[desc['RootPath']]['Sha256']=='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243' and sources[desc['AppearancePath']]['Sha256']=='951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C'
        expected=set();path=desc['RootPath'];appearance_path=None
        while path:
            assert path not in expected;expected.add(path);node,=nodes(raw[path]);path,layers=parse(node);value=layers['SHOT'][1].get('appearanceFilename')
            if appearance_path is None and value is not None and value[0]==1:appearance_path=strings(value[1:],1)[0]
        assert appearance_path==desc['AppearancePath'];appearance=sat(raw[appearance_path]);assert appearance==desc['Appearance'];expected.add(appearance_path)
        expected|={s['Path'] for s in appearance['Skeletons']};selected=set()
        for path in appearance['MeshGenerators']:
            expected.add(path);levels=mlod(raw[path]);assert levels==desc['LodMeshes'][path];selected.add(levels['SerializedPaths'][:4][detail])
        assert selected==set(run['geometry']);expected|=selected
        for path,g in run['geometry'].items():
            recovered=inspect(raw[path]);assert g['sha256']==recovered['sha256'] and g['tangents']==recovered['tangents']==0
            node,=nodes(raw[path]);version,=node[1];assert {dict(parts)[b'NAME'][:-1].decode('ascii') for tag,parts in version[1] if tag==b'PSDT'}=={SHADER}
        assert set(run['materials'])=={SHADER} and set(run['textures'])=={TEXTURE};material_result=verify(raw,run['materials'],run['textures'],run['palettes'])
        node,=nodes(raw[SHADER]);cv,=node[1];static=cv[1][0];sv,=static[1];fields=dict(sv[1]);mats=fields[b'MATS'][0][1];assert mats[0][1]==struct.pack('<I',0x4d41494e);values=list(struct.unpack('<17f',mats[1][1]));f32=lambda v:list(struct.unpack('<%df'%len(v),struct.pack('<%df'%len(v),*v)))
        assert f32(r['raw'])==values
        for name,offset in [('ambient',0),('diffuse',4),('emissive',8),('specular',12)]:assert f32(r[name])==values[offset+1:offset+4]+[values[offset]]
        assert r['power']==values[16]==20 and f32(r['specular'])==f32([.2,.2,.2,1])
        bindings=[]
        for _,parts in fields[b'TXMS']:
            version,=parts;parts=dict(version[1]);metadata=parts[b'DATA'];tag,flag=struct.unpack_from('<IB',metadata);assert flag==0 and parts[b'NAME'][:-1].decode('ascii')==TEXTURE
            bindings.append(dict(Tag=tag,Version=version[0].decode('ascii'),metadata=metadata.hex().upper()))
        assert bindings==r['bindingMetadata'] and {b['Tag'] for b in bindings}=={0x4d41494e,0x53504543};assert all(b['Version']=='0001' and len(bytes.fromhex(b['metadata']))==11 and bytes.fromhex(b['metadata'])[5:]==bytes([0,0,0,2,2,2]) for b in bindings)
        assert dict(struct.iter_unpack('<IB',fields[b'TCSS'][0][1]))=={0x4d41494e:0} and r['CoordinateSet']==0 and r['TextureTag']==0x53504543
        palette_factor,=run['palettes'][SHADER]['factors'];factors=r['factors'];first=palette_factor['Color']['OpaqueArgb'];assert factors==dict(PixelProgram=ALBEDO,UsesAlphaHueMask=False,FirstTag=0x4d41494e,SecondTag=0,FirstArgb=first,SecondArgb=first)
        assert r['albedoPrograms']==[ALBEDO] and r['PixelProgram']==PIXEL and not r['UsesAlphaHueMask'] and r['ScalesBloomRgb'] and r['sourceIdentity']==EXPECTED
        node,=nodes(raw[EFFECT]);version,=node[1];assert version[1][0][1]==bytes([5,0]);assert len(run['passes'])==6 and len(run['implementations'])==5;candidates=[];pass_rows=iter(run['passes'])
        for (_,parts),implementation in zip(version[1][1:],run['implementations']):
            iv,=parts;impl=dict((tag,value) for tag,value in iv[1] if tag!=b'PASS');info=impl[b'DATA']
            assert implementation['Version']==iv[0].decode('ascii') and implementation['Capabilities']==[v for v, in struct.iter_unpack('<I',impl[b'SCAP'])] and implementation['Options']==[v for v, in struct.iter_unpack('<I',impl.get(b'OPTN',b''))]
            assert implementation['PhaseTag']==struct.unpack_from('<I',info,1)[0] and implementation['Flag5']==info[5] and implementation['Flag6']==info[6] and implementation['passes']==info[0]
            pass_nodes=[parts for tag,parts in iv[1] if tag==b'PASS'];assert len(pass_nodes)==implementation['passes']
            for pass_node in pass_nodes:
                native=next(pass_rows);pv,=pass_node;parts=dict(pv[1]);state={};at=1
                for offset,width in OFFSETS:
                    if pv[0]!=b'0009' and offset in (0x4D,0x64,0x68,0x6C,0x70):continue
                    if pv[0]==b'0007' and offset==0x48:continue
                    state[str(offset)]=int.from_bytes(parts[b'DATA'][at:at+width],'little');at+=width
                assert at==len(parts[b'DATA']) and native['Fields']==state and native['passVersion']==pv[0].decode('ascii') and native['FixedStageCount']==parts[b'DATA'][0]
                if b'PVSH' in parts:
                    vs,=parts[b'PVSH'];pp,=parts[b'PPSH'];pixel_parts=pp[1];vertex=vs[1].split(bytes([0]))[0].decode('ascii');pixel=pixel_parts[0][1][1:-1].decode('ascii');assert (native['VertexProgram'],native['PixelProgram'])==(vertex,pixel)
                    textures=[]
                    for _,parts in pixel_parts[1:]:
                        v,=parts;stage,tag=struct.unpack('<BI',v[1]);textures.append(dict(Stage=stage,TextureTag=tag,Version=v[0].decode('ascii'),metadata=v[1].hex().upper()))
                    assert native['textures']==textures and [(t['Stage'],t['TextureTag']) for t in textures]==[(0,0x4d41494e),(1,0x53504543)]
                    assert not native['fixedStages'] and not native['fixedVertexLighting']
                    assert state['25']==state['26']==1 and state['32']==state['48']==state['68']==0 and state['60']==15 and state['64']==state['124']==0x4d41494e
                    candidates.append((vertex,pixel))
                else:
                    fixed,=parts[b'PFFP'];assert native['VertexProgram'] is None and native['PixelProgram'] is None and not native['textures'] and native['fixedVertexLighting']==fixed[1].hex().upper()
                    stages=[]
                    for tag,records in pv[1]:
                        if tag!=b'STAG':continue
                        stage,=records;data=stage[1];assert stage[0]==b'0000' and len(data)==33;texture,coordinate=struct.unpack_from('<II',data,18);stages.append(dict(TextureTag=texture,CoordinateTag=coordinate,metadata=data.hex().upper()))
                    assert native['fixedStages']==stages and len(stages)==native['FixedStageCount']
        assert candidates==[('vertex_program/a_spec_pp_vs20_for_ps20.vsh','pixel_program/h_specmap_pp_ps20.psh'),(VERTEX,PIXEL),('vertex_program/a_specmap_vs11_for_ps11.vsh',ALBEDO)]
        node,=nodes(raw[PIXEL]);version,=node[1];code=dict(version[1])[b'PEXE'];words=struct.unpack('<%dI'%(len(code)//4),code);at=1;assert words[0]==0xffff0200
        while words[at]&65535==65534:at+=1+((words[at]>>16)&32767)
        assert hashlib.sha256(code).hexdigest().upper()==r['bytecodeSha256'] and hashlib.sha256(code[at*4:]).hexdigest().upper()==r['InstructionsSha256']=='D794150290AC181EEF093C4FD974832CFF55039DA606FE749C6BC4A8E94212F2'
        graph=set();edges=[]
        def visit(path):
            if path in graph:return
            graph.add(path);assert hashlib.sha256(raw[path]).hexdigest().upper()==EXPECTED[path]
            for include in re.findall(r'^\s*#include\s*["<]([^">]+)[">]',raw[path].decode('ascii'),re.M):
                child=posixpath.normpath(posixpath.join(posixpath.dirname(path),include)) if include.startswith('.') else include
                assert child in EXPECTED;edges.append((path,child));visit(child)
        visit(VERTEX);assert graph==set(EXPECTED) and len(edges)==3
        expected|={SHADER,EFFECT,TEXTURE,ALBEDO,PIXEL,'palette/pc_skin_hum.pal'}|graph;assert expected==set(sources)
        identities.append({k:v for k,v in r.items() if k!='detail'});summaries.append(dict(detail=detail,sources=len(sources),source_bytes=sum(p['Bytes'] for p in sources.values()),materials=material_result,samplers=[b['metadata'][10:] for b in bindings]))
    assert identities[0]==identities[1] and len(samples)==64 and {r['index'] for r in samples}==set(range(64))
    with Image.open(io.BytesIO(raw[TEXTURE])) as image:pixels=image.convert('RGBA').tobytes()
    maximum=0.;discriminators=dict(unscaled_bloom_rgb=0.,spec_red_instead_of_alpha=0.,scaled_alpha_input=0.,ignored_fade=0.);flags=set();texels=set()
    first=runs[2]['description']['factors']['FirstArgb'];color=[(first>>s&255)/255 for s in (16,8,0)]+[1]
    for r in samples:
        assert r['path']==SHADER and r['texture']==TEXTURE and r['first']==first;rgba=pixels[r['pixel']*4:r['pixel']*4+4];assert rgba==base64.b64decode(r['rgba']);sample=[v/255 for v in rgba];texels.add(r['pixel']);flags.add((r['bloom'],r['fade']))
        constants={3:color,4:color,7:[int(r['fade']),int(r['bloom']),0,r['opacity']]};inputs={(1,0):r['vertexDiffuse']+[1],(1,1):r['vertexSpecular']+[1],(3,0):[0,0,0,1]};result=execute(code,constants,inputs,{0:sample,1:sample});error=max(abs(a-b) for a,b in zip(result,r['output']));maximum=max(maximum,error);assert error<=1e-5
        raw_spec=[v*sample[3] for v in r['vertexSpecular']];albedo=[sample[i]*color[i]*r['vertexDiffuse'][i] for i in range(3)];expected_rgb=[a+s*(.5 if r['bloom'] else 1) for a,s in zip(albedo,raw_spec)];luma=sum(a*b for a,b in zip(raw_spec,(.3,.59,.11)));expected_alpha=r['opacity'] if r['fade'] else luma*(.65 if r['bloom'] else 1)
        assert max(abs(a-b) for a,b in zip(result,expected_rgb+[expected_alpha]))<1e-7
        if r['bloom']:discriminators['unscaled_bloom_rgb']=max(discriminators['unscaled_bloom_rgb'],max(raw_spec)*.5)
        discriminators['spec_red_instead_of_alpha']=max(discriminators['spec_red_instead_of_alpha'],max(r['vertexSpecular'])*abs(sample[0]-sample[3]))
        if r['bloom'] and not r['fade']:discriminators['scaled_alpha_input']=max(discriminators['scaled_alpha_input'],luma*.65*.5)
        if r['fade']:discriminators['ignored_fade']=max(discriminators['ignored_fade'],abs(r['opacity']-luma*(.65 if r['bloom'] else 1)))
    assert len(texels)==64 and flags=={(False,False),(False,True),(True,False),(True,True)} and all(v>1e-3 for v in discriminators.values())
    return dict(closures=summaries,shared_original_cases=64,distinct_texels=len(texels),maximum_compiled_pixel_error=maximum,tolerance=1e-5,discriminators=discriminators,vertex_graph_sources=4,vertex_graph_edges=3,compiled_vertex_lit='Unknown',hardware_candidate_selection='Unknown',gpu=False)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('native_output',type=Path);a=p.parse_args();print(json.dumps(audit(a.root,a.configuration,a.native_output),indent=2))
