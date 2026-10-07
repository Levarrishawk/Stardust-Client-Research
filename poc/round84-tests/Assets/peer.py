"""Authored installed-creature preparation fixtures and independent source identity audit. No extraction."""
import argparse
import contextlib
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import sys
import re

REPO = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('preflight_peer', REPO/'poc/round83-tests/Assets/peer.py')
preflight = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preflight)
TABLE = 'appearance/lat/all_m.lat'
IDLE = 'emt_standing_idle_ambient_1_ag'
OBJECT = 'object/creature/player/shared_human_male.iff'


def chunk(tag, value):
    return tag.encode() + struct.pack('>I', len(value)) + value


def form(tag, *children):
    return chunk('FORM', tag.encode() + b''.join(children))


def text(value):
    return value.encode('ascii') + b'\0'


def template(parent=None, appearance=None, family='SCOT'):
    fields = [(name, b'\1\x20' + struct.pack('<f', value)) for name, value in
              [('stepHeight', .5), ('collisionHeight', 2), ('collisionRadius', .25),
               ('collisionOffsetX', 0), ('collisionOffsetZ', 0), ('collisionLength', 1), ('warpTolerance', 8)]] if parent is None else []
    def layer(tag, version, fields, child=None):
        records = [form('DERV', chunk('XXXX', text(parent)))] if parent else []
        records += [form(version, chunk('PCNT', struct.pack('<I', len(fields))),
                         *[chunk('XXXX', text(name)+value) for name, value in fields])]
        if child: records.append(child)
        return form(tag, *records)
    shot = layer('SHOT', '0010', [('appearanceFilename', b'\1'+text(appearance))] if appearance else [])
    stot = layer('STOT', '0009', [], shot)
    return layer(family, '0013', fields, stot) if family == 'SCOT' else stot


def proxy(path):
    return form('PXAT', form('0000', chunk('INFO', text(path))))


def table(duplicate=False):
    idle = proxy('appearance/animation/idle.ans')
    speed = form('SPAT', form('0000', chunk('INFO', b'\3'), idle,
                 proxy('appearance/animation/walk.ans'), proxy('appearance/animation/run.ans')))
    selected = form('SSAT', form('0000', chunk('INFO', text('speed')),
                    form('ANMS', chunk('INFO', struct.pack('<h', 1)), speed)))
    rows = [('default', idle), (IDLE, idle), ('loop_standing', selected)]
    if duplicate: rows.append((IDLE.upper(), idle))
    return form('LATT', form('0000', chunk('INFO', text('appearance/skeleton/test.skt')+struct.pack('<h', len(rows))),
                            *[form('ANIM', chunk('INFO', text(name)), value) for name, value in rows]))


def sat(table_path=TABLE):
    return form('SMAT', form('0003', chunk('INFO', struct.pack('<IIB', 0, 1, 0)), chunk('MSGN', b''),
                chunk('SKTI', text('appearance/skeleton/test.skt')+text('root')),
                chunk('LATX', struct.pack('<H', 1)+text('appearance/skeleton/test.skt')+text(table_path))))


def identity(path, value, priority):
    return dict(Path=path, Priority=priority, Bytes=len(value), Sha256=hashlib.sha256(value).hexdigest().upper())


def installed_expected(root, config):
    sys.path.insert(0, str(REPO/'tools/python'))
    from verify_textured_mesh_bundle import Index
    from pst_reference_audit import configured_archives
    from verify_static_mesh_slice import nodes
    from verify_creature_template_profile import parse
    from verify_skeletal_descriptors import sat as parse_sat
    sources = {}
    with contextlib.ExitStack() as stack:
        indexes = [(priority, Index(stack.enter_context(path.open('rb')))) for priority,path in configured_archives(root,config)]
        def read(path):
            for priority,index in indexes:
                raw=index.lookup(path)
                if raw is not None:
                    sources[path]=identity(path,raw,priority);return raw
            raise ValueError('Required installed preparation input absent')
        current=OBJECT;appearance=None;visited=set()
        while current:
            if current in visited or len(visited)>=16:raise ValueError('Template chain budget/cycle')
            visited.add(current);tree,=nodes(read(current));current,layers=parse(tree)
            value=layers['SHOT'][1].get('appearanceFilename')
            if appearance is None and value and value[0]==1:appearance=value[1:-1].decode('ascii').lower()
        raw=read(appearance);declared={r['AnimationTablePath'] for r in parse_sat(raw)['AnimationTables']}
        tables={path: nodes(read(path))[0] for path in sorted(declared)}
        selections={}
        for key,name,route in [('Idle',IDLE,None),('Walk','loop_standing',[0,1]),('Run','loop_standing',[0,2])]:
            assert TABLE in tables;version,=tables[TABLE][1];matching=[]
            for tag,parts in version[1][1:]:
                if parts[0][1][:-1].decode('ascii').rstrip(' ').lower()==name.lower():matching.append(parts[1])
            assert len(matching)==1;selected=matching[0]
            for child in route or []:
                version,=selected[1]
                if selected[0]==b'SSAT':
                    count=struct.unpack('<h',version[1][1][1][0][1])[0]
                    assert 0<=child<count and len(version[1][1][1])==count+1
                    selected=version[1][1][1][child+1]
                else:
                    assert selected[0]==b'SPAT';count=version[1][0][1][0]
                    assert 0<=child<count and len(version[1])==count+1;selected=version[1][child+1]
            while selected[0]==b'TSCL':version,=selected[1];selected=version[1][1]
            assert selected[0]==b'PXAT';version,=selected[1];clip=version[1][0][1][:-1].decode('ascii').lower()
            read(clip)
            selections[key]=dict(TablePath=TABLE,LogicalName=name,ClipSha256=sources[clip]['Sha256'],ExplicitChildRoute=route)
        return dict(Catalog=dict(TemplateCrc=preflight.crc(OBJECT.encode()),ObjectPath=OBJECT,
                                 ObjectSha256=sources[OBJECT]['Sha256'],AppearanceSha256=sources[appearance]['Sha256'],Recipe=0),
                    Sources=sources,**selections)


def generate(out, asset_root=None, config=None):
    if bool(asset_root) != bool(config): raise ValueError('Both original inputs required')
    original = None
    if asset_root:
        # Admit all explicit original inputs before the first output write.
        for raw in (asset_root, config):
            path = Path(raw)
            remainder = raw[2:] if len(raw) > 1 and raw[1] == ':' else raw
            if (not path.is_absolute() or raw.startswith(('\\\\', '//')) or ':' in remainder or raw != raw.strip()
                    or any(ord(c) < 32 for c in raw) or any(p in ('.', '..') for p in raw.replace('\\', '/').split('/'))):
                raise ValueError('Local explicit original inputs required')
            for ancestor in (path, *path.parents):
                if ancestor.is_symlink() or getattr(ancestor.stat(), 'st_file_attributes', 0) & 0x400:
                    raise ValueError('Reparse input rejected')
        if Path(config).stat().st_size > 65536: raise ValueError('Config byte budget')
        for line in Path(config).read_bytes().decode('utf-8-sig').splitlines():
            if not line.lstrip().startswith('searchTree_00_'):continue
            match=re.fullmatch(r'\s*searchTree_00_([0-9]+)\s*=\s*([^\s;]+)\s*(?:;.*)?',line)
            if not match or any(c in match[2] for c in '/\\:'):raise ValueError('Local original mount required')
            mount=Path(asset_root)/match[2]
            if mount.is_symlink() or getattr(mount.stat(),'st_file_attributes',0)&0x400:raise ValueError('Reparse mount rejected')
        original = dict(Root=asset_root, Config=config, ObjectPath=OBJECT,
                        Expected=installed_expected(Path(asset_root),Path(config)))
    out = out.resolve(); out.relative_to((REPO/'local-private').resolve())
    if out.exists(): raise ValueError('Fresh private output required')
    out.mkdir(parents=True)
    members = {'object/root.iff': template('object/base.iff'), 'object/base.iff': template(appearance='appearance/test.sat'),
               'appearance/test.sat': sat(), TABLE: table(), 'appearance/animation/idle.ans': b'synthetic idle',
               'appearance/animation/walk.ans': b'synthetic walk', 'appearance/animation/run.ans': b'synthetic run'}
    variants = {
        'rootmissing': {k:v for k,v in members.items() if k != 'object/root.iff'},
        'rootmasked': dict(members, **{'object/root.iff': b''}),
        'appearancemissing': {k:v for k,v in members.items() if k != 'appearance/test.sat'},
        'tablemasked': dict(members, **{TABLE: b''}),
        'missing': {k:v for k,v in members.items() if k != 'appearance/animation/walk.ans'},
        'masked': dict(members, **{'appearance/animation/walk.ans': b''}),
        'parentmissing': {k:v for k,v in members.items() if k != 'object/base.iff'},
        'parentmasked': dict(members, **{'object/base.iff': b''}),
        'cycle': dict(members, **{'object/root.iff': template('object/root.iff')}),
        'family': dict(members, **{'object/root.iff': template(family='STOT')}),
        'appearance': dict(members, **{'appearance/test.sat': form('APT ', form('0000'))}),
        'duplicate': dict(members, **{TABLE: table(True)}),
        'undeclared': dict(members, **{'appearance/test.sat': sat('appearance/lat/other.lat'), 'appearance/lat/other.lat': table()}),
        'badtable': dict(members, **{TABLE: b'bad LATT'}),
        'limit': dict(members, **{'object/root.iff': members['object/root.iff'] + bytes(1048577)})}
    for name, values in [('good', members), *variants.items()]:
        directory=out/name;directory.mkdir()
        preflight.archive(directory/'data.tre', [(path, value, 0) for path,value in values.items()])
        (directory/'mounts.cfg').write_bytes(b'searchTree_00_5=data.tre\n')
    expected = {path:identity(path,value,5) for path,value in members.items()}
    result=dict(Root=str(out), Expected=expected, Original=original)
    (out/'fixtures.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print('PASS authored TREE/SCOT/SMAT/LATT/proxy fixtures before native')


def verify(out):
    fixture=json.loads((out/'fixtures.json').read_text());native=json.loads((out/'receipt.json').read_text())
    assert native['Synthetic']['Sources']==fixture['Expected']
    assert native['Synthetic']['Catalog']['ObjectSha256']==fixture['Expected']['object/root.iff']['Sha256']
    assert native['Synthetic']['Catalog']['TemplateCrc']==preflight.crc(b'object/root.iff')
    for key, path in [('Idle','idle'),('Walk','walk'),('Run','run')]:
        assert native['Synthetic'][key]['ClipSha256']==fixture['Expected'][f'appearance/animation/{path}.ans']['Sha256']
    if fixture['Original']:
        sys.path.insert(0,str(REPO/'tools/python'))
        from verify_textured_mesh_bundle import Index
        from pst_reference_audit import configured_archives
        from verify_static_mesh_slice import nodes
        from verify_creature_template_profile import parse
        from verify_skeletal_descriptors import sat as parse_sat, logical_table
        original=fixture['Original']; real=native['Original']
        assert real==original['Expected']
        with contextlib.ExitStack() as stack:
            indexes=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in
                     configured_archives(Path(original['Root']),Path(original['Config']))]
            def read(path):
                for priority,index in indexes:
                    value=index.lookup(path)
                    if value is not None:return value,priority
                raise ValueError('Required original source absent')
            for group in [real['Sources'],native['ClosureSources']]:
                for path, source in group.items():
                    value,priority=read(path);assert source==identity(path,value,priority)
            current=original['ObjectPath'];appearance=None
            while current:
                raw,_=read(current);tree,=nodes(raw);current,layers=parse(tree)
                value=layers['SHOT'][1].get('appearanceFilename')
                if appearance is None and value and value[0]==1:appearance=value[1:-1].decode('ascii').lower()
            entry=real['Catalog'];raw,_=read(appearance)
            assert entry['AppearanceSha256']==hashlib.sha256(raw).hexdigest().upper()
            assert entry['TemplateCrc']==preflight.crc(original['ObjectPath'].encode())
            declared={r['AnimationTablePath'] for r in parse_sat(raw)['AnimationTables']}
            for key, logical, route in [('Idle',IDLE,None),('Walk','loop_standing',[0,1]),('Run','loop_standing',[0,2])]:
                selection=real[key];assert selection['TablePath'] in declared and selection['LogicalName']==logical and selection['ExplicitChildRoute']==route
                data,_=read(selection['TablePath']);description=logical_table(data)
                # The independent tuple parser selects exact serialized entries and counted explicit branches.
                tree,=nodes(data);version,=tree[1];matching=[]
                for tag,parts in version[1][1:]:
                    name=parts[0][1][:-1].decode('ascii').rstrip(' ')
                    if name.lower()==logical.lower():matching.append(parts[1])
                assert len(matching)==1; selected=matching[0]
                for child in route or []:
                    version,=selected[1]
                    if selected[0]==b'SSAT':selected=version[1][1][1][child+1]
                    else:assert selected[0]==b'SPAT';selected=version[1][child+1]
                while selected[0]==b'TSCL':version,=selected[1];selected=version[1][1]
                assert selected[0]==b'PXAT';version,=selected[1];clip=version[1][0][1][:-1].decode('ascii').lower()
                raw,_=read(clip);assert selection['ClipSha256']==hashlib.sha256(raw).hexdigest().upper()
        assert native['ClosureSources'] and native['ClosureGeometryVertices']>0
    else:assert native['Original'] is None
    assert native['NegativeCases']==34
    print('PASS independent catalog/choice/source identities and original closure' if fixture['Original'] else 'PASS independent synthetic identities')


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('mode',choices=['generate','verify']);p.add_argument('out',type=Path)
    p.add_argument('--asset-root');p.add_argument('--config');a=p.parse_args()
    generate(a.out,a.asset_root,a.config) if a.mode=='generate' else verify(a.out)
