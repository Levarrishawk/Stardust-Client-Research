"""Private launch profiles over accepted84 independently authored inputs and hash oracle."""
import argparse
import copy
import importlib.util
import json
from pathlib import Path

REPO=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('assets84',REPO/'poc/round84-tests/Assets/peer.py')
oracle=importlib.util.module_from_spec(spec);spec.loader.exec_module(oracle)


def profile(root, config, object_path):
    return dict(Version=1,AssetRoot=root,ConfigurationPath=config,ObjectPath=object_path,
                Idle=dict(TablePath=oracle.TABLE,LogicalName=oracle.IDLE),
                Walk=dict(TablePath=oracle.TABLE,LogicalName='loop_standing',ExplicitChildRoute=[0,1]),
                Run=dict(TablePath=oracle.TABLE,LogicalName='loop_standing',ExplicitChildRoute=[0,2]))


def generate(out, asset_root=None, config=None):
    oracle.generate(out,asset_root,config)
    out=out.resolve();f=json.loads((out/'fixtures.json').read_text())
    p=profile(str(out/'good'),str(out/'good/mounts.cfg'),'object/root.iff')
    invalid=['null','[]','{}','{','true']
    def add(value):invalid.append(json.dumps(value))
    for key in p:
        value=copy.deepcopy(p);del value[key];add(value)
        value=copy.deepcopy(p);value[key]=None;add(value)
    for key in ['Password','Token','Endpoint','version']:
        value=copy.deepcopy(p);value[key]='unaccepted';add(value)
    raw=json.dumps(p)
    for key in p:invalid.append(raw[:-1]+','+json.dumps(key)+':'+json.dumps(p[key])+'}')
    invalid.append(raw[:-1]+',"\\u0056ersion":1}')
    for version in [0,2,True,'1',1.5,2147483648]:
        value=copy.deepcopy(p);value['Version']=version;add(value)
    for field in ['AssetRoot','ConfigurationPath']:
        for value in ['relative','C:relative','https://host/a','file:///a','\\\\host\\a','//host/a',p[field]+'/../a',p[field]+'/./a',p[field]+'\0',' '+p[field]]:
            changed=copy.deepcopy(p);changed[field]=value;add(changed)
    for path in ['../object/root.iff','/object/root.iff','object/a/../root.iff','object//root.iff','object/root.iff\0','https://host/a','appearance/test.sat']:
        value=copy.deepcopy(p);value['ObjectPath']=path;add(value)
    for choice in ['Idle','Walk','Run']:
        for key in ['TablePath','LogicalName']:
            value=copy.deepcopy(p);del value[choice][key];add(value)
            value=copy.deepcopy(p);value[choice][key]=False;add(value)
        for key,value0 in [('Extra',0),('ExplicitChildRoute','0'),('ExplicitChildRoute',[-1]),('ExplicitChildRoute',[4096]),('ExplicitChildRoute',[0.5]),('ExplicitChildRoute',[True]),('ExplicitChildRoute',[0]*33),('LogicalName','default'),('LogicalName',''),('TablePath','../appearance/lat/all_m.lat')]:
            value=copy.deepcopy(p);value[choice][key]=value0;add(value)
        value=copy.deepcopy(p);child=json.dumps(value[choice]);duplicate=child[:-1]+',"TablePath":'+json.dumps(oracle.TABLE)+'}'
        invalid.append(json.dumps(value).replace(child,duplicate))
    f['Profile']=p;f['InvalidJson']=invalid
    if f['Original']:f['OriginalProfile']=profile(asset_root,config,oracle.OBJECT)
    (out/'fixtures.json').write_text(json.dumps(f,indent=2),encoding='utf-8')
    (out/'launch-profile.json').write_text(json.dumps(p,indent=2),encoding='utf-8')
    print(f'PASS independent launch profiles; invalid JSON cases={len(invalid)}')


def verify(out):
    f=json.loads((out/'fixtures.json').read_text());r=json.loads((out/'receipt.json').read_text())
    assert r['Synthetic']['Sources']==f['Expected']
    assert r['Synthetic']['Catalog']['ObjectSha256']==f['Expected']['object/root.iff']['Sha256']
    assert r['Synthetic']['Catalog']['TemplateCrc']==oracle.preflight.crc(b'object/root.iff')
    for key,name in [('Idle','idle'),('Walk','walk'),('Run','run')]:
        assert r['Synthetic'][key]['ClipSha256']==f['Expected'][f'appearance/animation/{name}.ans']['Sha256']
    assert r['JsonRejected']==len(f['InvalidJson']) and r['NegativeCases']>=r['JsonRejected']+25
    assert r['CleanupChecks']==17
    if f['Original']:assert r['Original']==f['Original']['Expected']
    else:assert r['Original'] is None
    print('PASS launch ownership/negative suite and independent identity agreement')


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('mode',choices=['generate','verify']);p.add_argument('out',type=Path)
    p.add_argument('--asset-root');p.add_argument('--config');a=p.parse_args()
    generate(a.out,a.asset_root,a.config) if a.mode=='generate' else verify(a.out)
