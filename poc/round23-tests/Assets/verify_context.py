"""Independent selected context fixtures; accepted ACST oracle plus CIDM/palette byte counts, no player seed claim."""
import ast, contextlib, copy, hashlib, json, struct, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index

def chunk(tag,raw):return tag.encode()+struct.pack('>I',len(raw))+raw
def form(tag,raw):return chunk('FORM',tag.encode()+raw)
def dictionary(names):return form('CIDM',form('0001',chunk('DATA',b''.join(struct.pack('<H',i)+n.encode()+b'\0' for i,n in enumerate(names,1))))).hex().upper()
def fixtures():
    base=dict(Name='positive',Dictionary=dictionary(['/private/ranged','/shared_owner/color','/private/other']),Descriptions=[dict(Kind='Ranged',FullName='/private/ranged',PalettePath=None,Minimum=-8,Maximum=8,Default=-2),dict(Kind='Palette',FullName='/shared_owner/color',PalettePath='palette/synthetic.pal',Minimum=0,Maximum=0,Default=1)],Keys=[dict(Name='ranged',Private=True),dict(Name='color',Private=False)],Palettes={'palette/synthetic.pal':2},Reject=False)
    result=[]
    def add(name,mutate=lambda f:None,reject=False):
        f=copy.deepcopy(base);f['Name']=name;f['Reject']=reject;mutate(f);result.append(f)
    add('positive')
    add('same-path-once',lambda f:(f['Descriptions'].append(dict(Kind='Palette',FullName='/private/other',PalettePath='palette/synthetic.pal',Minimum=0,Maximum=0,Default=0)),f['Keys'].append(dict(Name='other',Private=True))))
    add('unselected-missing',lambda f:f['Descriptions'].append(dict(Kind='Palette',FullName='/private/other',PalettePath='palette/unused.pal',Minimum=0,Maximum=0,Default=0)))
    add('identical-duplicate',lambda f:f['Descriptions'].append(copy.deepcopy(f['Descriptions'][0])))
    add('int32-range',lambda f:(f.update(Keys=[dict(Name='ranged',Private=True)]),f['Descriptions'][0].update(Minimum=-2147483648,Maximum=2147483647,Default=-2147483648)))
    add('palette-upper-positive',lambda f:(f['Palettes'].update({'palette/synthetic.pal':1024}),f['Descriptions'][1].update(Default=1023)))
    add('unselected-unsupported',lambda f:f['Descriptions'].append(dict(Kind='Ranged',FullName='private/anomaly',PalettePath=None,Minimum=0,Maximum=1,Default=0)))
    add('empty-selected',lambda f:f.update(Keys=[]),True)
    add('duplicate-selected',lambda f:f['Keys'].append(copy.deepcopy(f['Keys'][0])),True)
    add('bad-key',lambda f:f['Keys'][0].update(Name='bad name'),True)
    add('unknown-selected',lambda f:f['Keys'][0].update(Name='absent'),True)
    add('no-cidm',lambda f:(f['Descriptions'][0].update(FullName='/private/absent'),f['Keys'][0].update(Name='absent')),True)
    add('unsupported-selected',lambda f:f['Descriptions'][1].update(FullName='shared_owner/color'),True)
    add('conflicting-duplicate',lambda f:f['Descriptions'].append(f['Descriptions'][0] | dict(Default=-1)),True)
    add('conflicting-type',lambda f:f['Descriptions'].append(dict(Kind='Palette',FullName='/private/ranged',PalettePath='palette/synthetic.pal',Minimum=0,Maximum=0,Default=0)),True)
    add('missing-palette',lambda f:f.update(Palettes={}),True)
    add('empty-palette',lambda f:f['Palettes'].update({'palette/synthetic.pal':0}),True)
    add('large-palette',lambda f:f['Palettes'].update({'palette/synthetic.pal':1025}),True)
    add('equal-range',lambda f:f['Descriptions'][0].update(Minimum=8),True)
    add('reverse-range',lambda f:f['Descriptions'][0].update(Minimum=9),True)
    add('ranged-default-low',lambda f:f['Descriptions'][0].update(Default=-9),True)
    add('ranged-default-high',lambda f:f['Descriptions'][0].update(Default=8),True)
    add('palette-default-low',lambda f:f['Descriptions'][1].update(Default=-1),True)
    add('palette-default-high',lambda f:f['Descriptions'][1].update(Default=2),True)
    add('bad-palette-path',lambda f:f['Descriptions'][1].update(PalettePath=''),True)
    return result
def id_names(raw):
    assert raw[:4]==b'FORM' and raw[8:12]==b'CIDM' and raw[20:24]==b'0001' and raw[24:28]==b'DATA'
    assert [struct.unpack_from('>I',raw,i)[0] for i in (4,16,28)]==[len(raw)-8,len(raw)-20,len(raw)-32]
    result={};cursor=32
    while cursor<len(raw):ident=struct.unpack_from('<H',raw,cursor)[0];cursor+=2;end=raw.index(0,cursor);result[raw[cursor:end].decode('ascii')]=ident;cursor=end+1
    return result
def expected(descriptions,keys,ids,counts):
    rows=[]
    for k in keys:
        name=k['Name'];private=k['Private'];full=('/private/' if private else '/shared_owner/')+name;d=next(d for d in descriptions if d['FullName']==full)
        low,high=(0,counts[d['PalettePath']]) if d['Kind']=='Palette' else (d['Minimum'],d['Maximum'])
        rows.append(dict(VariableName=name,Private=private,Id=ids[full],FullName=full,Kind=d['Kind'],MinimumInclusive=low,MaximumExclusive=high,AuthoredDefault=d['Default'],PalettePath=d['PalettePath']))
    return dict(Bindings=rows,Defaults=[dict(VariableName=r['VariableName'],Private=r['Private'],Value=r['AuthoredDefault']) for r in sorted(rows,key=lambda r:(r['VariableName'],r['Private']))])
if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Independent context fixtures:',len(fixtures()));sys.exit()
root,config,profile,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:')
r={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]};native=r['synthetic_context'];assert len(native['Cases'])==len(fixtures())
for fixture,actual in zip(fixtures(),native['Cases']):
    assert actual['Name']==fixture['Name'] and actual['Rejected']==fixture['Reject']
    if not fixture['Reject']:
        ids=id_names(bytes.fromhex(fixture['Dictionary']));expectedResult=expected(fixture['Descriptions'],fixture['Keys'],ids,fixture['Palettes']);assert actual['Result']==expectedResult
        assert actual['ResolverCalls']==len({b['PalettePath'] for b in expectedResult['Bindings'] if b['Kind']=='Palette'})
assert native['Owned'] and native['ConcurrentBuilds']==16 and native['ExactBudgets'] and len(native['Failures'])==17
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
    indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
    def lookup(path):
        for priority,index in indices:
            raw=index.lookup(path)
            if raw is not None:return raw,priority
        raise AssertionError(path)
    acst,pri=lookup('customization/asset_customization_manager.iff');assert pri==53 and len(acst)==458340 and hashlib.sha256(acst).hexdigest().upper()=='5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20'
    cidm,pri=lookup('customization/customization_id_manager.iff');assert pri==53 and len(cidm)==3697 and hashlib.sha256(cidm).hexdigest().upper()=='FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2'
    # Reuse accepted independent ACST helper functions without invoking its CLI or unrelated queries.
    oracleSource=Path(__file__).resolve().parents[2]/'round22-tests/Assets/verify_directory.py'
    module=ast.parse(oracleSource.read_text(encoding='utf-8'));namespace={'struct':struct,'TAGS':['NAME','PNOF','VNOF','DEFV','IRNG','RTYP','UCMP','ULST','UIDX','LLST','LIDX','CIDX'],'WIDTHS':[1,2,2,4,8,2,3,2,5,2,5,6]}
    functions=[n for n in module.body if isinstance(n,ast.FunctionDef) and n.name in ('decode','tables','query')]
    exec(compile(ast.Module(body=functions,type_ignores=[]),str(oracleSource),'exec'),namespace)
    q=namespace['query'](namespace['tables'](namespace['decode'](acst)),0xD700D576);assert q['Found'] and q['RootKey']==11821 and len(q['Variables'])==36
    descriptions=[dict(Kind=v['Kind'],FullName=v['FullName'],PalettePath=v['PalettePath'],Minimum=v['MinimumInclusive'],Maximum=v['MaximumExclusive'],Default=v['AuthoredDefault']) for v in q['Variables']]
    original=r['asset_context'];counts={};sources=[]
    for palette in original['PaletteUnion']:
        path=palette['Path'];raw,priority=lookup(path)
        assert raw[:4]==b'RIFF' and raw[8:16]==b'PAL data';riff,data=struct.unpack_from('<I',raw,4)[0],struct.unpack_from('<I',raw,16)[0];version,count=struct.unpack_from('<HH',raw,20)
        assert version>>8==3 and count>0 and data==4+4*count and riff==data+12 and data+20<=len(raw)
        assert count==palette['Count'] and palette['Source']==dict(Path=path,Priority=priority,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper());counts[path]=count;sources.append(dict(path=path,count=count))
    supplied=json.loads(profile.read_text())['values'];keys=sorted([dict(Name=v['variableName'],Private=v['factoryFlag']) for v in supplied],key=lambda k:(k['Name'],k['Private']));assert len(keys)==8 and all(v['value']==0 for v in supplied)
    expectedResult=expected(descriptions,keys,id_names(cidm),counts)
    assert original['Result']==expectedResult and (original['QueryCrc'],original['RootKey'],original['QueryVariables'])==(0xD700D576,11821,36)
    chosen={b['PalettePath'] for b in expectedResult['Bindings'] if b['Kind']=='Palette'};assert chosen==set(counts)==set(original['ResolvedPaths']) and len(original['ResolvedPaths'])==5
    assert counts=={'palette/pc_skin_hum.pal':256,'palette/pc_hair_hum.pal':256,'palette/pc_skin_hum_highlights.pal':256,'palette/pc_eye_hum.pal':256,'palette/white.pal':2}
    assert {b['VariableName']:(b['MinimumInclusive'],b['MaximumExclusive']) for b in expectedResult['Bindings'] if b['Kind']=='Ranged'}=={'index_style_eyebrow':(0,7),'index_style_beard':(0,18),'index_style_freckles':(0,5)}
    assert original['ArchiveClosed'] and original['AuthoredNotPlayerState'] and (original['ExternalReads'],original['SourceBytes'])==(76,5940390)
print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),synthetic_rejections=sum(f['Reject'] for f in fixtures()),callback_budget_cancellation_failures=len(native['Failures']),selected_keys=8,ranged_keys=3,palette_keys=5,unique_selected_palettes=5,palette_entries=sum(counts.values()),authored_defaults_all_zero=True,palettes=sources,source_bundle_unchanged=True,live_player_seed_recovered=False,gpu=False),indent=2))
