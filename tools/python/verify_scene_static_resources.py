"""Independent pinned static-template inheritance/source/geometry and partial-material identity audit."""
import argparse
import contextlib
import hashlib
import io
import json
import struct
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes, inspect
from verify_creature_template_profile import parse
from dtla_corpus_inventory import parse as detail
from verify_dds_pixels import inspect as pixels
from verify_scene_creature_resources import checksum

ROOT='object/tangible/terminal/shared_terminal_mission.iff'
ROOT_PIN='E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281'
APT='appearance/ksk_all_mission.apt'
APT_PIN='74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021'
CONFIG='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'

def text(b):assert b.endswith(b'\0') and b'\0' not in b[:-1];return b[:-1].decode('ascii').lower().replace('\\','/')
def audit(root,configuration,output):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper()==CONFIG
    assert output.stat().st_size<1048576
    content=output.read_text(encoding='utf-8-sig');assert 'PASS R7-ASSETS-01:' in content
    groups={}
    for line in content.splitlines():
        if line.startswith('static_'):
            tag,value=line.split('=',1);groups.setdefault(tag,[]).append(json.loads(value,parse_int=lambda s:-0.0 if s=='-0' else int(s)))
    catalog,=groups['static_catalog'];create,=groups['static_create'];validation,=groups['static_validation']
    assert catalog==dict(TemplateCrc=checksum(ROOT),ObjectPath=ROOT,ObjectSha256=ROOT_PIN,AppearanceSha256=APT_PIN,Recipe=1)
    wire=bytes.fromhex(create['wire']);assert len(wire)==47 and struct.unpack_from('<HIQ',wire)==(5,0xfe89ddea,0xfedcba9876543210)
    words=list(struct.unpack_from('<7I',wire,14));assert words==[0x80000000,0x3e800000,0xbf000000,0x40000000,0x42f70000,0xc2350000,1]
    assert struct.unpack_from('<I',wire,42)[0]==catalog['TemplateCrc']==2907007746 and wire[46]==255
    raw=create['Create'];names=['QuaternionX','QuaternionY','QuaternionZ','QuaternionW','PositionX','PositionZ','PositionY']
    assert [struct.unpack('<I',struct.pack('<f',raw[n]))[0] for n in names]==words
    assert raw['ObjectId']==0xfedcba9876543210 and raw['TemplateCrc']==catalog['TemplateCrc'] and raw['RawHyperspaceFlag']==255
    sources={s['Path']:s for s in groups['static_source']};assert len(sources)==len(groups['static_source'])==24
    assert set(create['counts'])==set(sources) and set(create['counts'].values())=={1}
    assert create['ExternalReads']==24 and create['SourceBytes']==sum(s['Bytes'] for s in sources.values())==1389982
    assert sources[ROOT]['Sha256']==ROOT_PIN and sources[APT]['Sha256']==APT_PIN
    assert create['materialPolicy']==1 and create['geometryMeshes']==4 and create['materialMeshes']==2 and create['shaders']==create['textures']==create['effects']==1
    assert validation==dict(rejects=84,syntheticProfiles=4,neutralCases=2,preflightReads=0,canceledResolverCalls=1,sourceCloneChecks=24,geometryOwnershipChecks=4,textureOwnershipChecks=1,configSha256=CONFIG,gpu=False)
    data={}
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(root,configuration)]
        for path,s in sources.items():
            for priority,index in indices:
                blob=index.lookup(path)
                if blob is not None:break
            else:raise ValueError('Missing source '+path)
            assert s['Priority']==priority and s['Bytes']==len(blob) and s['Sha256']==hashlib.sha256(blob).hexdigest().upper()
            data[path]=blob
    expected=set();path=ROOT;chain=[];selected=None;field=None
    while path:
        assert path not in expected;expected.add(path)
        node,=nodes(data[path]);parent,layers=parse(node)
        evidence=[]
        for family,(version,parameters) in layers.items():
            encoded=parameters.get('appearanceFilename')
            evidence.append(dict(Family=family,Version=version,ParentPath=parent,appearance=None if encoded is None else encoded.hex().upper()))
            if family=='SHOT' and encoded is not None:
                assert encoded and encoded[0] in (0,1)
                if encoded[0]==0:assert encoded==b'\0'
                elif selected is None:selected=text(encoded[1:]);field=dict(SourcePath=path,Family=family,Version=version)
        chain.append(dict(SourcePath=path,layers=evidence));path=parent
    assert chain==groups['static_inheritance'] and len(chain)==4 and chain[0]['layers'][1]['Version']=='0008'
    assert selected==APT and field==create['profile']['AppearanceField'] and create['profile']==dict(RootPath=ROOT,AppearancePath=APT,AppearanceField=field)
    node,=nodes(data[APT]);assert node[0]==b'APT ';version,=node[1];name,=version[1];assert version[0]==b'0000' and name[0]==b'NAME'
    lod=text(name[1]);expected|={APT,lod};assert lod=='appearance/lod/ksk_all_mission.lod'
    children=['appearance/'+p if not p.startswith('appearance/') else p for p in detail(data[lod])[3]]
    geometry={r['path']:r for r in groups['static_geometry']};assert len(geometry)==len(groups['static_geometry'])==4 and list(geometry)==children
    expected_material_meshes={};expected_materials={};expected_textures=set();unsupported=set();diagnostics=[]
    for mesh_path in children:
        expected.add(mesh_path);info=inspect(data[mesh_path]);native=geometry[mesh_path]
        mesh,=nodes(data[mesh_path]);mesh_version,=mesh[1];sps=mesh_version[1][1];sps_version,=sps[1]
        shader_paths=[text(group[0][1]) for _,group in sps_version[1][1:]]
        assert native['source']==mesh_path and native['vertices']==info['vertices'] and native['triangles']==info['triangles'] and native['hash']==info['geometry_sha256'] and native['shaders']==list(dict.fromkeys(shader_paths))
        shaders=[];textures=set();effects=set();complete=True
        for shader_path in shader_paths:
            if shader_path in shaders:continue
            shaders.append(shader_path);expected.add(shader_path);shader,=nodes(data[shader_path])
            if shader[0]!=b'SSHT':
                unsupported.add((shader_path,shader[0].decode('ascii')));complete=False;break
            fields=dict(shader[1][0][1]);effect=text(fields[b'NAME']);effect=effect if '/' in effect else 'effect/'+effect;effects.add(effect);expected.add(effect)
            bindings=[]
            for tag,versions in fields.get(b'TXMS',[]):
                assert tag==b'TXM ';version,=versions;f=dict(version[1]);binding_path=text(f[b'NAME']);expected.add(binding_path);textures.add(binding_path)
                bindings.append(dict(Tag=struct.unpack_from('<I',f[b'DATA'])[0],SourceFlag=f[b'DATA'][4],AssetPath=binding_path))
            if complete:expected_materials.setdefault(shader_path,dict(path=shader_path,effect=effect,textures=bindings))
        if complete:
            expected_material_meshes[mesh_path]=dict(path=mesh_path,shaders=shaders,textures=sorted(textures),effects=sorted(effects));expected_textures|=textures
        else:diagnostics.append('UnavailableMaterialBundle:'+mesh_path+':Unsupported static shader envelope.')
    # Only COMPLETE mesh bundles retain material dictionaries; staged successful pieces of failed meshes are not returned.
    kept_shaders={p for m in expected_material_meshes.values() for p in m['shaders']};expected_materials={p:r for p,r in expected_materials.items() if p in kept_shaders}
    assert set(sources)==expected and create['diagnostics']==diagnostics and len(diagnostics)==2
    material_meshes={r['path']:r for r in groups['static_material_mesh']};assert set(material_meshes)==set(expected_material_meshes)
    for p,wanted in expected_material_meshes.items():
        got=material_meshes[p];assert set(got['shaders'])==set(wanted['shaders']) and set(got['textures'])==set(wanted['textures']) and set(got['effects'])==set(wanted['effects'])
    assert {r['path']:r for r in groups['static_material']}==expected_materials
    native_textures={r['path']:r['mips'] for r in groups['static_texture']};assert set(native_textures)==expected_textures
    with contextlib.redirect_stdout(io.StringIO()):
        for path,mips in native_textures.items():assert mips==pixels(data[path])
    assert len(groups['static_neutral'])==2 and {r['kind'] for r in groups['static_neutral']}=={'Missing','Masked'}
    for r in groups['static_neutral']:assert r['geometryMeshes']==4 and r['materialMeshes']==0 and len(r['diagnostics'])==4 and r['reads']==22
    return dict(template_crc=catalog['TemplateCrc'],original_sources=24,source_bytes=1389982,inherited_templates=4,root_shot_version='0008',geometry_vertices=[geometry[p]['vertices'] for p in children],geometry_triangles=[geometry[p]['triangles'] for p in children],material_meshes=2,retained_shaders=1,retained_textures=1,texture_mips=sum(map(len,native_textures.values())),unavailable_material_meshes=2,unsupported_shader_families=sorted(unsupported),native_rejections=84,synthetic_profiles=4,raw_identity=True,full_texture_coverage=False,gpu=False)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','native_output'):p.add_argument(name,type=Path)
    a=p.parse_args();print(json.dumps(audit(a.root,a.configuration,a.native_output),indent=2))
