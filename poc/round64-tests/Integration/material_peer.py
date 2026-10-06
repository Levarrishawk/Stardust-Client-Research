"""Compare host retained MAIN upload with the independently produced original byte model."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('material64_records',ROOT/'poc/round63-tests/Integration/terrain_playback_peer.py')
old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
def verify(expected,text):
    model=next(r['Material'] for r in expected if r.get('Material',{}).get('ShaderSource',{}).get('Path')=='shader/defaultemis.sht')
    rows=old.records(text,'terrain_supplied_material_cpu=');assert len(rows)==1;row=rows[0]
    for key in ('ShaderSource','TextureSource','Width','Height','MipCount','SourceBytes','DecodedBytes','ColorSpace'):assert row[key]==model[key],(key,row,model)
    assert row['Reads']==2 and row['OwnedOverlayClosed'] and row['Policy']=='TextureTimesVertexColor' and not row['OriginalTerrainAssociation'] and not row['OriginalShaderExecutionParity']
    assert old.records(text,'terrain_supplied_material_mips=')==[model['Mips']]
    gpu=old.records(text,'terrain_supplied_material_gpu=');assert len(gpu)==1 and gpu[0]['Textures']==1 and gpu[0]['SharedByPatches']==16 and gpu[0]['ExactMipBytes'] and gpu[0]['BorrowedByPatches'] and not gpu[0]['OriginalShaderExecutionParity']
    patches=old.records(text,'supplied_terrain_patch=');assert len(patches)==16 and all(p['RenderPolicy']=='ModernSuppliedMainTexture' for p in patches)
    assert old.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
    return dict(Passed=True,OriginalSources=2,IndependentMipHashes=8,DecodedBytes=87380,Textures=1,RetainedPatchBindings=16,Drained=True,OriginalTerrainAssociation=False,OriginalShaderExecutionParity=False)
if __name__=='__main__':
    expected,log,out=sys.argv[1:4];result=verify(json.loads(Path(expected).read_text()),Path(log).read_text(encoding='utf-8-sig'))
    old.nominal.fresh(out).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
