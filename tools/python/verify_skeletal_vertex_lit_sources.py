"""Offline original mouth EFCT/PVSH/include audit and independent modern CPU color cases.

Uses existing raw TRE/IFF readers. Prints compact evidence, never original source.
Original generated vertex bytecode and LIT/power300 precision remain unknown.
"""
import argparse
import contextlib
import hashlib
import json
import math
import posixpath
import re
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes

VERTEX='vertex_program/a_specmap_vs20_nodot3.vsh'
EXPECTED={VERTEX:'AD5D1F09358DA7B9F20A82E126538297C333A502E291CB9F0DD005C9B6B5E408',
 'vertex_program/include/functions.inc':'CA006C0ACE67624377C00C63F00EBA7DBC0FA30210A57CF156F736B759FB7D15',
 'vertex_program/include/vertex_shader_constants.inc':'C54630E798DE028B6FC7A734D962DE356D9E177EB075162682B87A47B04F10F9',
 'shared_program/functions.inc':'A9E9C7A2E6D41545BDBD0F08A57F14FD4D5C39899788F7FB94B510B20B85B09A'}

def verify(raw,priorities,native):
    for path,sha in EXPECTED.items():assert hashlib.sha256(raw[path]).hexdigest().upper()==sha
    assert len(raw[VERTEX])==1346 and priorities[VERTEX]==52
    visited=set();edges=[]
    def walk(path,active):
        assert path not in active
        if path in visited:return
        visited.add(path)
        for include in re.findall(r'^\s*#\s*include\s*"([^"]+)"',raw[path].decode('ascii'),re.M):
            target=include if include in EXPECTED else posixpath.normpath(posixpath.join(posixpath.dirname(path),include))
            assert target in EXPECTED;edges.append([path,target]);walk(target,active|{path})
    walk(VERTEX,set());assert visited==set(EXPECTED) and len(edges)==3
    root,=nodes(raw['shader/hum_m_mouth.sht']);cv,=root[1];static=cv[1][0];sv,=static[1];fields=dict(sv[1])
    effect=fields[b'NAME'][:-1].decode('ascii').lower().replace(chr(92),'/');effect='effect/'+effect if '/' not in effect else effect;assert effect=='effect/h_color2_specmap.eft'
    import struct
    assert dict(struct.iter_unpack('<IB',fields[b'TCSS'][0][1]))=={0x4d41494e:0}
    mats=fields[b'MATS'][0][1];tag=struct.unpack('<I',mats[0][1])[0];values=struct.unpack('<17f',mats[1][1]);assert tag==0x4d41494e and values==(1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,300)
    root,=nodes(raw[effect]);ev,=root[1];bindings=[]
    for _,parts in ev[1][1:]:
        iv,=parts
        for tag,parts in iv[1]:
            if tag!=b'PASS':continue
            pv,=parts;pass_fields=dict(pv[1]);ps=pass_fields.get(b'PPSH')
            if not ps:continue
            psv,=ps;pixel=psv[1][0][1][1:-1].decode('ascii')
            if pixel!='pixel_program/h_color2_specmap_ps20_nodot3.psh':continue
            vs,=pass_fields[b'PVSH'];assert vs[0]==b'0000';bindings.append(vs[1][:-1].decode('ascii'))
    assert bindings==[VERTEX]
    source=re.sub(r'\s+',' ',raw[VERTEX].decode('ascii'))
    for text in ['calculateDiffuseSpecularLighting(false, inputVertex.position, inputVertex.normal)', 'lightData.ambient.ambientColor + diffuseSpecular.diffuse','diffuseSpecular.specular * material.specularColor','diffuse : COLOR0','specular : COLOR1']:
        assert text in source
    functions=re.sub(r'\s+',' ',raw['vertex_program/include/functions.inc'].decode('ascii'))
    start=functions.index('DiffuseSpecular calculateDiffuseSpecularLighting(bool')
    body=functions[start:functions.index('return output;',start)]
    assert 'output.diffuse = material.emissiveColor;' in body and 'output.specular = float4(0.0, 0.0, 0.0, 0.0);' in body
    assert 'if (!dot3)' in body and 'calculateDiffuseSpecularParallelLight(lightData.parallelSpecular[0]' in body
    assert 'calculateDiffuseSpecularPointLight(lightData.pointSpecular[0]' in body
    assert all(f'calculateDiffuseParallelLight(lightData.parallel[{i}]' in body for i in range(2))
    assert all(f'calculateDiffusePointLight(lightData.point[{i}]' in body for i in range(4))
    assert 'material.diffuseColor' not in body and 'material.ambientColor' not in body
    assert functions.count('lit(nDotL, nDotH, material.specularPower.x)')==2
    reported=[];maximum=0;source_markers=0
    def unit(v):
        length=math.sqrt(sum(x*x for x in v));return [x/max(length,1e-6) for x in v]
    def dot(a,b):return sum(x*y for x,y in zip(a,b))
    for line in native.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('mouth_vertex_sources='):
            source_markers+=1;assert json.loads(line.split('=',1)[1])==EXPECTED
        if not line.startswith('mouth_vertex_case='):continue
        r=json.loads(line.split('=',1)[1]);assert r['powerPolicy']=='modern-literal'
        n=unit(r['normal']);view=unit([a-b for a,b in zip(r['viewer'],r['position'])]);d=[a+b for a,b in zip(r['ambient'],r['emissive'])];s=[0.,0.,0.]
        assert r['lights'][1]['specular']==[0,0,0] and r['lights'][3]['specular']==[0,0,0]
        for light in r['lights']:
            delta=[a-b for a,b in zip(light['vector'],r['position'])] if light['Point'] else light['vector']
            direction=unit(delta) if light['Point'] else delta;distance=math.sqrt(dot(delta,delta));attenuation=1
            if light['Point']:attenuation=1/sum(a*b for a,b in zip(light['attenuation'],[1,distance,distance*distance]))
            ndl=dot(n,direction);half=unit([a+b for a,b in zip(direction,view)])
            spec=max(0,min(1,dot(n,half)))**r['power'] if ndl>0 else 0
            for i in range(3):d[i]+=light['diffuse'][i]*max(0,ndl)*attenuation;s[i]+=light['specular'][i]*r['materialSpecular'][i]*spec*attenuation
        error=max(abs(a-b) for a,b in zip(d+s,r['diffuse']+r['specular']));assert error<1e-5,(len(reported),error)
        maximum=max(maximum,error);reported.append(r)
    assert source_markers==1
    assert len(reported)==64 and {r['power'] for r in reported}=={0,2,20,300}
    return dict(root='shader/hum_m_mouth.sht',binding=bindings[0],sources=[dict(path=p,bytes=len(raw[p]),priority=priorities[p],sha256=EXPECTED[p]) for p in EXPECTED],include_edges=edges,closure_bytes=sum(len(raw[p]) for p in EXPECTED),modern_cpu_cases=len(reported),maximum_error=maximum,compiled_vertex_LIT='UNKNOWN',power300_policy='modern-literal')

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('native_log',type=Path);a=p.parse_args()
    paths=set(EXPECTED)|{'shader/hum_m_mouth.sht','effect/h_color2_specmap.eft'};raw={};priorities={}
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        for path in sorted(paths):
            for priority,index in indices:
                data=index.lookup(path)
                if data is not None:raw[path]=data;priorities[path]=priority;break
            assert path in raw,path
        result=verify(raw,priorities,a.native_log)
    result['configuration_sha256']=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()
    print(json.dumps(result,indent=2))

if __name__=='__main__':main()
