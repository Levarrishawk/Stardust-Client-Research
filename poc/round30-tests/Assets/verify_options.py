"""Literal option fixtures and independent public-field/original-input oracle.

Synthetic option values do not claim asset/parser acceptance. No reflection-derived
expected values, additional corpus, or original executable execution is used.
"""
import contextlib, copy, hashlib, io, json, runpy, struct, sys
from pathlib import Path
REPO=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(REPO/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes, inspect
from verify_creature_template_profile import parse
from dtla_corpus_inventory import parse as detail
from verify_scene_static_resources import ROOT, ROOT_PIN, APT, APT_PIN, CONFIG, text
PIN='B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62'
SHADER='shader/anim_mission_terminal.sht'
GRAPH=dict(Resources=64,SourceBytes=16777216,Depth=32,Edges=4096)

def static(name,material=0,selection=None,graph=None,decoded=None,policy=None):
    source=dict(Kind='StaticAppearance',Material=material,Selection=selection,Graph=graph,Decoded=decoded,Policy=policy)
    expected=dict(Kind='StaticAppearance',Material=material,Selection=selection,Graph=graph or GRAPH,Decoded=134217728 if decoded is None else decoded,Policy=policy)
    return dict(Name=name,Input=source,Expected=expected)

def fixtures():
    selected=dict(Path=SHADER,Pin=PIN,Index=0)
    implicit=dict(Limits=None,RetainedMips=4096,PendingWeight=268435456)
    limits=dict(Frames=17,Sources=20,SourceBytes=123456,Decoded=222222,Mips=64,ShaderBytes=1234,Dds=None)
    exact=dict(Frames=8,Sources=10,SourceBytes=89350,Decoded=349536,Mips=64,ShaderBytes=475,Dds=dict(Dimension=128,Input=11088,Decoded=43692,Tail=False))
    nondefault=copy.deepcopy(limits);nondefault['Dds']=dict(Dimension=512,Input=33333,Decoded=65536,Tail=True)
    cases=[static('static-neutral-default',1),static('static-complete-default'),static('static-selected-default',selection=selected),
           static('static-selected-nondefault',1,dict(Path=SHADER,Pin=PIN.lower(),Index=7),dict(Resources=123,SourceBytes=4567890,Depth=7,Edges=321),7890123),
           static('static-all-default',selection=selected,policy=implicit),
           static('static-all-null-dds',selection=selected,policy=dict(Limits=limits,RetainedMips=555,PendingWeight=5555555)),
           static('static-all-nondefault',selection=dict(Path=SHADER,Pin=PIN.lower(),Index=3),graph=dict(Resources=37,SourceBytes=3141590,Depth=12,Edges=77),decoded=4444444,policy=dict(Limits=nondefault,RetainedMips=987,PendingWeight=12345678)),
           static('static-all-original-exact',selection=selected,graph=dict(Resources=34,SourceBytes=1479410,Depth=32,Edges=4096),decoded=2490372,policy=dict(Limits=exact,RetainedMips=126,PendingWeight=3969782))]
    def clip(name,pin,route):return dict(Path='APPEARANCE\\LAT\\SYNTHETIC.LAT',Name=name,Pin=pin*64,Route=route)
    for name,albedo,route,face,resources,bytes_ in [('creature-default',False,None,None,64,16777216),('creature-face-nondefault',True,[1,2],dict(Decoded=76543210,Values=[dict(Name='/private/test_a',FactoryFlag=False,Value=-9),dict(Name='/shared/test_b',FactoryFlag=True,Value=123)]),37,12345678)]:
        source=dict(Kind='CreatureDetail0',Idle=clip('idle_probe','a',route),Movement=dict(walk=clip('walk_probe','b',None if route is None else [0]),run=clip('run_probe','c',None if route is None else [2,0,3])),Albedo=albedo,Resources=resources,SourceBytes=bytes_,Face=face)
        expected=copy.deepcopy(source)
        for c in [expected['Idle'],*expected['Movement'].values()]:c['Path']='appearance/lat/synthetic.lat';c['Pin']=c['Pin'].upper()
        cases.append(dict(Name=name,Input=source,Expected=expected))
    return cases

if sys.argv[1]=='generate':
    Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Literal option fixtures:',len(fixtures()));sys.exit()

root,config,output=map(Path,sys.argv[1:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:')
receipt=json.loads(next(line.split('=',1)[1] for line in lines if line.startswith('option_preservation=')))
assert len(receipt['Options'])==len(fixtures())
for wanted,got in zip(fixtures(),receipt['Options']):
    assert got['Name']==wanted['Name'] and got['Request']==wanted['Expected'] and got['Callback']==wanted['Expected']
    assert got['Owned'] and got['Canceled'] and got['Drained']
assert receipt['AllFrameOmissionDetected'] and receipt['Original']['ArchiveClosed'] and receipt['Original']['WorkerThread']!=receipt['Original']['OwnerThread']
assert [(c['Name'],c['Kind']) for c in receipt['Completions']]==[
    ('matching','Prepared'),('missing-bundle','Faulted'),('default-completion','Faulted'),('altered-policy','Faulted'),('canceled','Canceled'),('stale','Stale'),('registry-closed','Stale')]
assert all(c['Outstanding']==0 and c['Resources']==(c['Kind']=='Prepared') for c in receipt['Completions'])
assert receipt['ClosedDrains']==2 and receipt['LateFaultObserved'] and receipt['NullOptionRejected'] and receipt['UnsupportedRecipeRejected'] and receipt['MismatchedRecipesRejected']==2
saved=sys.argv
try:
    sys.argv=['frame-oracle','library'];accepted=runpy.run_path(str(REPO/'poc/round28-tests/Assets/verify_frame_bundle.py'))
finally:sys.argv=saved
bundle=accepted['original'](root,config)
assert receipt['Original']['Frames']==bundle['Frames']
assert {p['Path']:p['Mips'] for p in receipt['Original']['Images']}=={p:i['Mips'] for p,i in bundle['Images'].items()}
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()==CONFIG
identities={};expected=set();default=set()
with contextlib.ExitStack() as stack:
    indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
    def read(path):
        expected.add(path)
        if path in identities:return blobs[path]
        for priority,index in indices:
            raw=index.lookup(path)
            if raw is not None:break
        else:raise AssertionError(path)
        identities[path]=dict(Path=path,Priority=priority,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper());blobs[path]=raw;return raw
    blobs={};path=ROOT;appearance=None
    while path:
        node,=nodes(read(path));parent,layers=parse(node);value=layers.get('SHOT',('',{}))[1].get('appearanceFilename')
        if appearance is None and value is not None and value[0]==1:appearance=text(value[1:])
        path=parent
    assert appearance==APT and identities[ROOT]['Sha256']==ROOT_PIN
    apt,=nodes(read(APT));version,=apt[1];name,=version[1];assert identities[APT]['Sha256']==APT_PIN
    lod=text(name[1]);children=['appearance/'+p if not p.startswith('appearance/') else p for p in detail(read(lod))[3]]
    for path in children:
        with contextlib.redirect_stdout(io.StringIO()):mesh=inspect(read(path))
        mesh_node,=nodes(blobs[path]);mesh_version,=mesh_node[1];sps_version,=mesh_version[1][1][1]
        shader_paths=list(dict.fromkeys(text(group[0][1]) for _,group in sps_version[1][1:]))
        for path in shader_paths:
            node,=nodes(read(path))
            if path==SHADER:node,=nodes(read(bundle['BaseShaderPath']))
            assert node[0]==b'SSHT';version,=node[1];fields=dict(version[1]);effect=text(fields[b'NAME']);read(effect if '/' in effect else 'effect/'+effect)
            for tag,versions in fields.get(b'TXMS',[]):
                binding_version,=versions;binding=dict(binding_version[1]);texture=text(binding[b'NAME'])
                if path==SHADER:texture=bundle['Frames'][0]['TexturePath']
                read(texture)
    default=set(expected)
    for path,source in bundle['Sources'].items():read(path);assert identities[path]==source
    for i,pin in enumerate(accepted['FRAME_PINS']):assert identities[bundle['Frames'][i]['TexturePath']]['Sha256']==pin
    actual={s['Path']:s for s in receipt['Original']['Sources']};assert actual==identities and len(actual)==34
    assert len(receipt['Original']['Calls'])==len(set(receipt['Original']['Calls']))==34 and set(receipt['Original']['Calls'])==expected
    default_pixels=0;default_mips=0
    for path in default:
        if path.endswith('.dds'):
            raw=blobs[path];height,width=struct.unpack_from('<II',raw,12);count=max(1,struct.unpack_from('<I',raw,28)[0]);default_mips+=count
            default_pixels+=sum(max(1,width>>i)*max(1,height>>i)*4 for i in range(count))
assert receipt['Original']['Receipt']['PendingWeight']==sum(s['Bytes'] for s in identities.values())+default_pixels+bundle['DecodedBytes']==3969782
assert receipt['Original']['Receipt']['Mips']==default_mips+bundle['MipCount']==126
print(json.dumps(dict(passed=True,option_cases=len(fixtures()),projected_pairs=2*len(fixtures()),completion_cases=len(receipt['Completions']),closed_drains=2,source_hashes=34,frame_mip_hashes=64,copy_omission_detected=True,production_changes=0),indent=2))
