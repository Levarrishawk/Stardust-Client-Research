"""Independent original overlay/source/factor/program/mip/sample audit for the world loader."""
import contextlib,hashlib,io,json,math,struct,sys
from pathlib import Path
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_creature_template_profile import parse
from verify_skeletal_descriptors import sat,mlod
from verify_skeletal_albedo import verify as albedo
from verify_dds_pixels import inspect as pixels
root,configuration,catalog_path,native_path,gpu_path,headless_path=map(Path,sys.argv[1:])
def split_log(path):
    blocks={};current=None
    for line in path.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('two_source_begin='):current=int(line.split('=',1)[1]);blocks[current]=[]
        elif current is not None and any(line.startswith(prefix) for prefix in ['source=','factor=','program=','texture=','sample=']):blocks[current].append(line)
    assert set(blocks)=={0xF122334455667788,0xE122334455667799}
    return blocks
blocks=split_log(native_path)
for path in [gpu_path,headless_path]:
    actual=split_log(path)
    for oid in blocks:assert sorted(actual[oid])==sorted(blocks[oid])
assert sorted(blocks[0xF122334455667788])==sorted(blocks[0xE122334455667799])
text='\n'.join(blocks[0xF122334455667788])
assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
def rows(prefix):return [json.loads(s[len(prefix):]) for s in text.splitlines() if s.startswith(prefix)]
source_rows=rows('source=');sources={r['Path']:r for r in source_rows}
assert len(sources)==len(source_rows)==39 and sum(r['Bytes'] for r in source_rows)==4686371
closure_hash=hashlib.sha256(json.dumps(sorted(source_rows,key=lambda r:r['Path']),separators=(',',':')).encode()).hexdigest().upper()
for path in [gpu_path,headless_path]:
    load_rows=[json.loads(line[len('two_loads='):]) for line in path.read_text(encoding='utf-8-sig').splitlines() if line.startswith('two_loads=')]
    assert len(load_rows)==1 and len(load_rows[0])==3 and all(r['sourceHash']==closure_hash for r in load_rows[0])
raw={}
with contextlib.ExitStack() as stack:
    indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(root,configuration)]
    for path,row in sources.items():
        for priority,index in indices:
            data=index.lookup(path)
            if data is not None:break
        else:raise ValueError('Missing effective '+path)
        assert row==dict(Path=path,Priority=priority,Bytes=len(data),Sha256=hashlib.sha256(data).hexdigest().upper());raw[path]=data
catalog=json.loads(catalog_path.read_text(encoding='utf-8'));entry,=[e for e in catalog['entries'] if e['templateCrc']==2937962913]
assert sources[entry['objectPath']]['Sha256']==entry['objectSha256']
path=entry['objectPath'];inherited=[]
while path:
    assert path not in inherited;inherited.append(path);node,=nodes(raw[path]);path,_=parse(node)
assert len(inherited)==5
appearance_path,=[path for path,row in sources.items() if path.endswith('.sat') and row['Sha256']==entry['appearanceSha256']]
appearance=sat(raw[appearance_path]);selected={mlod(raw[p])['SerializedPaths'][0] for p in appearance['MeshGenerators']}
assert selected=={'appearance/mesh/hum_m_body_l0.mgn','appearance/mesh/hum_m_head_l0.mgn'}
materials={r['path']:r for r in rows('factor=')};programs={r['path']:r for r in rows('program=')};textures={r['path']:r['mips'] for r in rows('texture=')}
assert len(materials)==4 and len(programs)==2 and len(textures)==10 and sum(map(len,textures.values()))==80
required,_=albedo(raw,materials,programs,{})
assert required==set(programs)=={'pixel_program/h_color2_specmap_ps11.psh','pixel_program/h_specmap_ps11.psh'}
pins={'pixel_program/h_color2_specmap_ps11.psh':('95E44154F4107866CF23FD5C77E082573211135BE17E1CE679243A2AEB85EB92',1399),'pixel_program/h_specmap_ps11.psh':('4A3155359640270C2274495AFD1E8E68E6139CCBC358DDA14B66D50BAF8B340C',1124)}
for path,(pin,size) in pins.items():assert sources[path]['Priority']==40 and sources[path]['Sha256']==pin and sources[path]['Bytes']==size
with contextlib.redirect_stdout(io.StringIO()):
    for path,mips in textures.items():assert pixels(raw[path])==mips
changed=0
for sample in rows('sample='):
    with Image.open(io.BytesIO(raw[sample['texture']])) as image:rgba=image.convert('RGBA').getpixel((0,0))
    assert bytes(rgba).hex().upper()==sample['rgba']
    factor=materials[sample['shader']]['factors'];a=[(factor['FirstArgb']>>shift&255)/255 for shift in (16,8,0)];b=[(factor['SecondArgb']>>shift&255)/255 for shift in (16,8,0)]
    hue=[x+(y-x)*rgba[3]/255 for x,y in zip(a,b)] if factor['UsesAlphaHueMask'] else a
    expected=[component/255*h for component,h in zip(rgba[:3],hue)]
    assert all(math.isclose(x,y,rel_tol=0,abs_tol=2e-7) for x,y in zip(expected,sample['color']))
    changed+=math.dist(expected,[c/255 for c in rgba[:3]])>1e-5
assert changed>=2
print('PASS: original39 source bytes/priority/SHA; five inherited templates; two programs/four factors; ten textures/80 mip hashes; four sampled pixels; two distinct closures agree native/GPU/headless')

