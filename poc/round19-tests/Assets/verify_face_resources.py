"""Original face source union, logical decoded budget and established independent composition oracle."""
import contextlib,hashlib,json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_face_composition import verify,verify_bindings
root,config,profile,output=map(Path,sys.argv[1:])
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
rows={}
for line in output.read_text(encoding='utf-8-sig').splitlines():
 if '=' in line:
  k,v=line.split('=',1);rows.setdefault(k,[]).append(json.loads(v))
assert output.read_text(encoding='utf-8-sig').splitlines()[-1].startswith('PASS:')
r,=rows['face_route'];compact,=rows['face_resource'];summary,=rows['face_summary'];cases,=rows['face_cases']
assert (r['ExternalReads'],r['SourceBytes'],r['DecodedBytes'],r['DefaultReads'],r['DefaultBytes'])==(76,5940390,17465296,39,4686371)
assert r['Unchanged'] and r['ArchiveClosed'] and r['SnapshotOwned']
assert r['ProfileSha256']==compact['profileSHA256']==hashlib.sha256(profile.read_bytes()).hexdigest().upper()=='C4F7CF06AB8C4083A2510556550C047DC0ADD5A5A0040829A81CBB047CCCBD9B'
expected=json.loads(profile.read_text())['values'];assert {(v['VariableName'],v['FactoryFlag'],v['Value']) for v in r['Values']}=={(v['variableName'],v['factoryFlag'],v['value']) for v in expected}
assert summary==dict(Negatives=12,compositionCancelled=True,ArchiveScopes=0,OtherFieldsPreserved=True,NoGlobalCustomization=True,ExactLimits=True)
assert len(cases)==12 and all(c['rejected'] for c in cases)
assert {c['name'] for c in cases}=={'missing-key','extra-key','wrong-domain','invalid-key','source-count','source-bytes','decoded-one-below','decoded-before-compose','direct-face-without-albedo','pre-cancel','first-read-cancel','composition-cancel'}
sources={s['Path']:s for s in r['Sources']};assert len(sources)==len(r['Sources'])==76 and sum(s['Bytes'] for s in sources.values())==5940390
raw={}
with contextlib.ExitStack() as stack:
 indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
 for path,source in sources.items():
  for priority,index in indices:
   data=index.lookup(path)
   if data is not None:break
  else:raise AssertionError(path)
  assert source==dict(Path=path,Priority=priority,Bytes=len(data),Sha256=hashlib.sha256(data).hexdigest().upper());raw[path]=data
assert compact['Aliases']==[dict(Path='texture/hum_m_face.dds',SameImageReference=False,Equivalent=True,Mips=11)]
assert compact['Sources']==r['Sources'] and compact['ExternalReads']==76 and compact['SourceBytes']==5940390 and compact['DecodedBytes']==17465296
textures={}
for texture in r['MaterialTextures']+r['FaceTextures']:
 path=texture['path'];assert path in raw
 if path in textures:assert textures[path]==texture
 else:textures[path]=texture
 h,w=struct.unpack_from('<II',raw[path],12);n=max(1,struct.unpack_from('<I',raw[path],28)[0]);assert len(texture['mips'])==n
 for level,mip in enumerate(texture['mips']):
  width,height=max(1,w>>level),max(1,h>>level);assert (mip['Width'],mip['Height'],mip['Bytes'])==(width,height,width*height*4)
assert (len(r['MaterialTextures']),len(r['FaceTextures']),len(textures))==(10,32,41)
encoded_decoded=sum(m['Bytes'] for t in textures.values() for m in t['mips']);assert encoded_decoded==17203152
bp,=rows['face_blueprint'];prepared,=rows['face_prepared'];composed,=rows['face_composition']
composition=verify(raw,bp,prepared,composed)
bindings=verify_bindings(raw,sorted(p for p in sources if p.endswith('.mgn')),rows['face_composition'],rows['generated_face_binding'])
assert composed['sha256']=='A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808'
assert compact['Outputs']==[dict(RendererPath='texturerenderer/hum_m_face.trt',Width=256,Height=256,Bytes=262144,Mip0Sha256=composed['sha256'])]
assert encoded_decoded+sum(o['Bytes'] for o in compact['Outputs'])==r['DecodedBytes']
assert compact['Bindings']==[dict(MeshPath='appearance/mesh/hum_m_head_l0.mgn',ShaderIndex=0,TextureTag=0x4D41494E,RendererPath='texturerenderer/hum_m_face.trt',ShaderPath='shader/hum_m_face.sht')]
assert rows['face_fallback']==[dict(requested=-2147483648,sha256='BD17F0717182AE3E9DF7F778FAA9EE4DB3F2D0189FBBFBF09C28B434C033B2FF'),dict(requested=2147483647,sha256='63BEF60E9EAE508996944042FCDA26D74E8DB40D099DD19DEEB5BCEFEA5A2E57')]
print(json.dumps(dict(passed=True,sources=76,source_bytes=5940390,unique_texture_paths=41,encoded_texture_decoded_bytes=encoded_decoded,generated_bytes=262144,decoded_budget_bytes=17465296,negative_cases=12,composition=composition,bindings=bindings,default_sources=39,cross_map_aliases=compact['Aliases'],extreme_integer_fallbacks_match_prior=True,gpu=False,legacy_driver_parity=False),indent=2))
