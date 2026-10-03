"""Original DDS/header, TRTS and independent float/pixel oracle for R19. No original code execution."""
import contextlib, hashlib, json, runpy, struct, sys
from pathlib import Path
repo=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(repo/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_face_composition import verify, verify_bindings
root,config,output=map(Path,sys.argv[1:4])
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
lines=output.read_text(encoding='utf-8-sig').splitlines()
assert lines[-1].startswith('PASS:')
r=json.loads(next(line.split('=',1)[1] for line in lines if line.startswith('face_completion=')))
assert r['ArchiveScopes']==0 and r['NoReads'] and r['Unchanged'] and r['SeparateInputAliases']==1 and r['Reads']==115
assert len(set(r['Negatives']))==len(r['Negatives'])>=35
with contextlib.ExitStack() as stack:
    indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
    def lookup(path):
        for priority,index in indices:
            value=index.lookup(path)
            if value is not None:return priority,value
        raise AssertionError('Missing original '+path)
    raw={}
    for source in r['Sources']:
        priority,data=lookup(source['Path']);raw[source['Path']]=data
        assert source==dict(Path=source['Path'],Priority=priority,Bytes=len(data),Sha256=hashlib.sha256(data).hexdigest().upper())
    assert len(raw)==len(r['Sources'])==76
    decoded=0;mips=0
    for texture in r['Textures']:
        data=raw[texture['Path']];assert data[:4]==b'DDS '
        height,width=struct.unpack_from('<2I',data,12);count=max(1,struct.unpack_from('<I',data,28)[0])
        assert len(texture['Mips'])==count
        for i,mip in enumerate(texture['Mips']):
            w,h=max(1,width>>i),max(1,height>>i)
            assert mip==dict(Width=w,Height=h,Bytes=w*h*4)
            decoded+=w*h*4;mips+=1
    compositions=[verify(raw,c['Blueprint'],c['Prepared'],c) for c in r['Compositions']]
    bindings=verify_bindings(raw,sorted({b['MeshPath'] for b in r['Bindings']}),r['Compositions'],r['Bindings'])
    unique={c['path']:c for c in r['Compositions'] if c['requested']==0}
    generated_bytes=sum(c['Width']*c['Height']*4 for c in unique.values())
    encoded=sum(len(data) for data in raw.values())
    expected=dict(Sources=len(raw),SourceBytes=encoded,DecodedTextures=len(r['Textures']),Mips=mips,DecodedTextureBytes=decoded,GeneratedFaces=len(unique),GeneratedFaceBytes=generated_bytes,PendingWeight=encoded+decoded+generated_bytes)
    assert r['Receipt']==expected
    assert expected==dict(Sources=76,SourceBytes=5940390,DecodedTextures=41,Mips=320,DecodedTextureBytes=17203152,GeneratedFaces=1,GeneratedFaceBytes=262144,PendingWeight=23405686)
    assert r['DefaultReceipt']==dict(Sources=39,SourceBytes=4686371,DecodedTextures=10,Mips=80,DecodedTextureBytes=14330528,GeneratedFaces=0,GeneratedFaceBytes=0,PendingWeight=19016899)
print(json.dumps(dict(passed=True,receipt=expected,compositions=compositions,bindings=bindings,negative_cases=len(r['Negatives']),source_hashes=len(raw),dds_headers=len(r['Textures']),logical_not_heap=True),indent=2))
# The older oracle requires exact old receipt keys. Assert the appended defaults, then run it on a private projection.
if len(sys.argv)==5:
    original=Path(sys.argv[4]);legacy_lines=original.read_text(encoding='utf-8-sig').splitlines()
    legacy=json.loads(next(line.split('=',1)[1] for line in legacy_lines if line.startswith('accounting_receipt=')))
    for row in legacy['Originals']:
        assert row['Receipt'].pop('GeneratedFaces')==0 and row['Receipt'].pop('GeneratedFaceBytes')==0
    projected=original.with_name('accounting-legacy-projection.txt')
    projected.write_text('accounting_receipt='+json.dumps(legacy)+'\n'+legacy_lines[-1]+'\n',encoding='utf-8')
    sys.argv=['verify_accounting.py',str(root),str(config),str(projected)]
    runpy.run_path(str(repo/'poc/round16-tests/Assets/verify_accounting.py'),run_name='__main__')
