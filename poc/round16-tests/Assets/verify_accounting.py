"""Independent original DDS-header accounting oracle; no decoder or additional corpus."""
import contextlib,hashlib,json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
root,config,output=map(Path,sys.argv[1:])
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
lines=output.read_text(encoding='utf-8-sig').splitlines()
r=json.loads(next(s.split('=',1)[1] for s in lines if s.startswith('accounting_receipt=')))
assert lines[-1].startswith('PASS:') and r['Reads']==66 and r['ArchiveScopes']==0 and r['ImmutableReceipt']
expected_negatives={'null','null-image','empty-mips','null-mip','mip-length','mip-dimensions','dimension-overflow','source-total','source-count','negative-source','source-path','distinct-shared-image','inconsistent-shared-metadata','missing-texture-source','rgba-hdr-path-collision'}
assert len(r['Negatives'])==len(expected_negatives) and set(r['Negatives'])==expected_negatives
summaries=[]
with contextlib.ExitStack() as stack:
    indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
    def raw(path):
        for priority,index in indices:
            b=index.lookup(path)
            if b is not None:return priority,b
        raise AssertionError('Missing original source: '+path)
    for row,(kind,count,size,textures,mips) in zip(r['Originals'],[('CreatureDetail0',39,4686371,10,80),('StaticAppearance',27,1401794,7,62)]):
        assert row['Kind']==kind and row['ArchiveClosed'] and row['Unchanged'] and row['NoReads']
        sources={s['Path']:s for s in row['Sources']};assert len(sources)==len(row['Sources'])==count
        assert sum(s['Bytes'] for s in sources.values())==size
        for path,source in sources.items():
            priority,b=raw(path)
            assert source==dict(Path=path,Priority=priority,Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper())
        images={t['Path']:t for t in row['Textures']};assert len(images)==len(row['Textures'])==textures
        byte_total=0;levels=0
        for path,image in images.items():
            _,b=raw(path);assert b[:4]==b'DDS ' and len(b)>=128
            height,width=struct.unpack_from('<II',b,12);n=max(1,struct.unpack_from('<I',b,28)[0]);assert width>0 and height>0 and len(image['Mips'])==n
            for level,mip in enumerate(image['Mips']):
                w,h=max(1,width>>level),max(1,height>>level);expected=w*h*4
                assert (mip['Width'],mip['Height'],mip['Bytes'])==(w,h,expected)
                assert len(mip['Sha256'])==64
                byte_total+=expected;levels+=1
        assert levels==mips
        expected_receipt=dict(Sources=count,SourceBytes=size,DecodedTextures=textures,Mips=mips,DecodedTextureBytes=byte_total,PendingWeight=size+byte_total)
        assert row['Receipt']==expected_receipt
        summaries.append(dict(kind=kind,**expected_receipt))
assert len(r['Originals'])==2
print(json.dumps(dict(passed=True,originals=summaries,negative_cases=len(expected_negatives),source_hashes=66,dds_headers=17,decoded_mips=142,no_redecode=True,no_new_corpus=True),indent=2))
