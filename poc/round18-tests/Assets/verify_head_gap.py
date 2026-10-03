"""Bounded head-gap facts, existing source/mip audit and receipt-tamper negatives."""
import base64,contextlib,copy,hashlib,io,json,math,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_dds_pixels import inspect
root,config,output=map(Path,sys.argv[1:])
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
lines=output.read_text(encoding='utf-8-sig').splitlines();r=json.loads(next(s.split('=',1)[1] for s in lines if s.startswith('head_gap_receipt=')))
assert lines[-1].startswith('PASS:')
def facts(value):
    assert value['Reads']==39 and value['SourceBytes']==4686371 and value['PrecancelReads']==0
    assert value['ArchiveClosed'] and value['NoFurtherReads'] and value['Unchanged']
    assert not any(value[k] for k in ['FaceBlueprints','FaceResources','GeneratedFaces','CustomizationValues'])
    rows=value['Rows'];assert len(rows)==5
    face,=[x for x in rows if x['Shader']=='shader/hum_m_face.sht']
    body,=[x for x in rows if x['Shader']=='shader/hum_m_body.sht']
    heads=[x for x in rows if x['Shader']=='shader/hum_m_head.sht'];assert len(heads)==2
    assert face['Mesh']=='appearance/mesh/hum_m_head_l0.mgn' and face['Slot']==0 and face['Count']==644
    assert face['Main']['AssetPath']=='texture/hum_m_face.dds' and face['MainCoordinates']==0
    assert face['RendererBindings']==[dict(RendererPath='texturerenderer/hum_m_face.trt',ShaderIndex=0,TextureTag=0x4D41494E,SourceLoaded=False)]
    assert face['PaletteFactors']==[] and face['Albedo']['FirstArgb']==face['Albedo']['SecondArgb']==0xFFFFFFFF
    assert body['Albedo']['FirstArgb']==0xFFE2B7AF and all(h['Albedo']['FirstArgb']==0xFFE2B7AF for h in heads)
    assert face['Texture']['WhiteRgbPixels']==356 and face['Texture']['Pixels']==1048576
    assert face['Texture']['Mip0Sha256']=='46F2C0B7003C2FAEBC2EC803E1B4A5DF54DBA3DA56095441FBCACDB59D1CC7E5'
    assert face['Texture']['Min'][:3]==[74,81,74] and face['Texture']['Max'][:3]==[255,255,255]
    for row in rows:
        assert row['Customization'] and row['CshdVersion']=='0001'
        assert row['Main']['SourceFlag']==0 and row['Main']['Tag']==0x4D41494E and row['MainCoordinates']==0
        a=row['Albedo'];sample=[v/255 for v in base64.b64decode(row['Texture']['MidRgba'])]
        colors=lambda argb:[((argb>>s)&255)/255 for s in [16,8,0]]
        first,second=colors(a['FirstArgb']),colors(a['SecondArgb']);alpha=sample[3]
        hue=[u+(v-u)*alpha for u,v in zip(first,second)] if a['UsesAlphaHueMask'] else first
        assert all(math.isclose(actual,s*h,abs_tol=2e-7) for actual,s,h in zip(row['Texture']['MidAlbedo'],sample,hue))
    return face
face=facts(r)
source_map={s['Path']:s for s in r['Sources']};assert len(source_map)==len(r['Sources'])==39
assert sum(s['Bytes'] for s in source_map.values())==4686371
assert 'texturerenderer/hum_m_face.trt' not in source_map
with contextlib.ExitStack() as stack:
    indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
    raw={}
    for path,source in source_map.items():
        for priority,index in indices:
            data=index.lookup(path)
            if data is not None:break
        else:raise AssertionError(path)
        assert source==dict(Path=path,Priority=priority,Bytes=len(data),Sha256=hashlib.sha256(data).hexdigest().upper());raw[path]=data
    unique={row['Main']['AssetPath']:row['Texture']['Mip0Sha256'] for row in r['Rows']};assert len(unique)==4
    for path,digest in unique.items():
        with contextlib.redirect_stdout(io.StringIO()):hashes=inspect(raw[path])
        assert hashes[0]==digest
for row in r['Rows']:
    for path_key,source_key in [('Mesh','MeshSource'),('Shader','ShaderSource'),('EffectPath','EffectSource')]:assert row[source_key]==source_map[row[path_key]]
    assert row['MainSource']==source_map[row['Main']['AssetPath']] and row['ProgramSource']==source_map[row['Albedo']['PixelProgram']]
negatives=[]
for name in ['generated','renderer-loaded','face-tint','wrong-main','wrong-pixel-hash']:
    altered=copy.deepcopy(r);f=next(x for x in altered['Rows'] if x['Shader']=='shader/hum_m_face.sht')
    if name=='generated':altered['GeneratedFaces']=True
    elif name=='renderer-loaded':f['RendererBindings'][0]['SourceLoaded']=True
    elif name=='face-tint':f['Albedo']['FirstArgb']=0xFFE2B7AF
    elif name=='wrong-main':f['Main']['AssetPath']='texture/hum_m_head.dds'
    else:f['Texture']['Mip0Sha256']='0'*64
    try:facts(altered)
    except AssertionError:negatives.append(name)
    else:raise AssertionError('Accepted tampered receipt '+name)
print(json.dumps(dict(passed=True,sources=39,source_bytes=4686371,mesh_slots=5,materials=4,main_textures_independently_decoded=4,receipt_negatives=negatives,face_slot=0,generated_face_missing=True,static_face_factors_white=True,face_not_solid_white=True,skin_head_and_body_match=True,legacy_expected_complexion='Unknown',gpu_parameter_or_screen_mapping_checked=False),indent=2))
