"""Independent modern catalog checksum and effective original source-identity audit. No asset output."""
import argparse
import contextlib
import hashlib
import json
import struct
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_creature_template_profile import parse
from verify_skeletal_descriptors import sat, mlod
from verify_compressed_locomotion import verify_bank_routes

OBJECT='object/creature/player/shared_human_male.iff'
OBJECT_PIN='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
SAT_PIN='951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C'
CONFIG_PIN='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
IDLE_PIN='798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B'

def checksum(path):
    # Independent polynomial long division, rather than the native bit-loop.
    polynomial=0x104C11DB7
    remainder=0xffffffff
    for byte in path.encode('ascii'):
        remainder=(remainder<<8)^(byte<<32)
        for degree in range(39,31,-1):
            if remainder>>degree&1:remainder^=polynomial<<(degree-32)
    return remainder^0xffffffff

def audit(root,configuration,profile,output):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper()==CONFIG_PIN
    assert output.stat().st_size<1048576
    text=output.read_text(encoding='utf-8-sig');assert 'PASS R5-ASSETS-01:' in text
    groups={}
    for line in text.splitlines():
        if line.startswith('scene_'):
            tag,value=line.split('=',1);groups.setdefault(tag,[]).append(json.loads(value,parse_int=lambda s: -0.0 if s=='-0' else int(s)))
    catalog,=groups['scene_catalog'];create,=groups['scene_create'];validation,=groups['scene_validation']
    entry=catalog['original'];synthetic=catalog['synthetic']
    assert catalog['kind']=='modern supplied manifest'
    assert entry['ObjectPath']==OBJECT and entry['TemplateCrc']==checksum(OBJECT)==2937962913
    assert entry['ObjectSha256']==OBJECT_PIN and entry['AppearanceSha256']==SAT_PIN and entry['Recipe']==0
    assert synthetic['ObjectPath']=='object/synthetic/fixture.iff' and synthetic['TemplateCrc']==checksum(synthetic['ObjectPath'])
    assert catalog['collisionA']!=catalog['collisionB'] and checksum(catalog['collisionA'])==checksum(catalog['collisionB'])==catalog['collisionCrc']==0xbfcb749d
    wire=bytes.fromhex(create['wire']);assert len(wire)==47
    assert struct.unpack_from('<HIQ',wire)==(5,0xfe89ddea,0xfedcba9876543210)
    assert list(struct.unpack_from('<7I',wire,14))==create['rawBits']==[0x80000000,0x3e800000,0xbf000000,0x40000000,0x42f70000,0xc2350000,1]
    assert struct.unpack_from('<I',wire,42)[0]==entry['TemplateCrc'] and wire[46]==255
    message=create['Create'];assert message['ObjectId']==0xfedcba9876543210 and message['TemplateCrc']==entry['TemplateCrc'] and message['RawHyperspaceFlag']==255
    names=['QuaternionX','QuaternionY','QuaternionZ','QuaternionW','PositionX','PositionZ','PositionY']
    assert [struct.unpack('<I',struct.pack('<f',message[n]))[0] for n in names]==create['rawBits']
    sources={r['Path']:r for r in groups['scene_source']};assert len(sources)==len(groups['scene_source'])
    assert len(sources)==37 and create['SourceBytes']==4683848 and len(groups['scene_main'])==4
    assert set(create['counts'])==set(sources) and set(create['counts'].values())=={1}
    assert create['ExternalReads']==len(sources) and create['SourceBytes']==sum(r['Bytes'] for r in sources.values())
    assert sources[OBJECT]['Sha256']==OBJECT_PIN and sources[create['appearance']]['Sha256']==SAT_PIN
    assert create['root']==OBJECT and create['detail']==0 and create['joints']==53
    assert validation['preflightReads']==0 and validation['missingReads']==1 and validation['cancellationResolverCalls']==1
    assert validation['sourceCloneChecks']==len(sources) and validation['meshOwnershipChecks']==2 and validation['poseOwnershipChecks']==3
    assert validation['textureOwnershipChecks']==10 and validation['rejections']==75 and validation['configSha256']==CONFIG_PIN
    raw={}
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(p.open('rb')))) for priority,p in archives(root,configuration)]
        for path,row in sources.items():
            for priority,index in indices:
                data=index.lookup(path)
                if data is not None:break
            else:raise ValueError('Missing effective source '+path)
            assert priority==row['Priority'] and len(data)==row['Bytes'] and hashlib.sha256(data).hexdigest().upper()==row['Sha256']
            raw[path]=data
    # Reuse closed parsers solely to check the selected dependency identities.
    inherited=[];path=OBJECT
    while path:
        assert path not in inherited;inherited.append(path)
        node,=nodes(raw[path]);path,_=parse(node)
    assert len(inherited)==5
    appearance=sat(raw[create['appearance']]);mesh_paths=[]
    for generator in appearance['MeshGenerators']:
        mesh_paths.append(mlod(raw[generator])['SerializedPaths'][0])
    assert set(mesh_paths)=={'appearance/mesh/hum_m_body_l0.mgn','appearance/mesh/hum_m_head_l0.mgn'} and all(p in raw for p in mesh_paths)
    assert all(r['Path'] in raw for r in appearance['Skeletons']) and all(r['AnimationTablePath'] in raw for r in appearance['AnimationTables'])
    clips={r['key']:r for r in groups['scene_clip']};assert set(clips)=={'idle','walk','run'} and len(groups['scene_clip'])==3
    bank=json.loads(profile.read_text(encoding='utf-8'));assert bank['version']==1 and {r['key'] for r in bank['animations']}=={'walk','run'}
    pins={'idle':IDLE_PIN,**{r['key']:r['sha256'] for r in bank['animations']}}
    idle,=groups['scene_idle']
    assert idle==dict(TablePath='appearance/lat/all_m.lat',LogicalName='emt_standing_idle_ambient_1_ag',ClipSha256=IDLE_PIN,ExplicitChildRoute=None)
    for row in groups['scene_bank']:
        supplied,=[r for r in bank['animations'] if r['key']==row['key']]
        assert all(row[k]==supplied[k] for k in ('table','name','route'))
    assert verify_bank_routes(raw,groups['scene_bank'])=={clips[k]['ClipPath'] for k in ('walk','run')}
    for key,row in clips.items():
        assert row['ClipSha256']==pins[key] and sources[row['ClipPath']]['Sha256']==pins[key]
        assert row['SkeletonPath']=='appearance/skeleton/all_b.skt' and row['FrameRate']==30
    # Audit native MAIN references against unchanged original shader bytes.
    for row in groups['scene_main']:
        shader,=nodes(raw[row['shader']])
        if shader[0]==b'CSHD':shader=shader[1][0][1][0]
        assert shader[0]==b'SSHT';fields=dict(shader[1][0][1]);mains=[]
        for tag,children in fields[b'TXMS']:
            assert tag==b'TXM ';version,=children;values=dict(version[1])
            if struct.unpack('<I',values[b'DATA'][:4])[0]==0x4d41494e:
                mains.append(values[b'NAME'][:-1].decode('ascii'))
        assert mains==[r['AssetPath'] for r in row['bindings']] and mains and all(p in raw for p in mains)
    return dict(template_crc=entry['TemplateCrc'],catalog='modern supplied configuration; no global catalog claim',source_count=len(sources),source_bytes=create['SourceBytes'],external_reads=create['ExternalReads'],original_inherited_templates=len(inherited),detail=0,prepared_clips=3,main_shaders=len(groups['scene_main']),rejections=validation['rejections'],raw_transform_bits_preserved=True,gpu=False)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','profile','native_output'):parser.add_argument(name,type=Path)
    args=parser.parse_args();print(json.dumps(audit(args.root,args.configuration,args.profile,args.native_output),indent=2))
