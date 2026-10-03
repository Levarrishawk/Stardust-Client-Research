"""Independent float32/body/provenance expectations; no asset access or decoding."""
import json,struct,sys
from pathlib import Path
f32=lambda x:struct.unpack('<f',struct.pack('<f',x))[0]
bits=lambda x:struct.pack('<f',x)
lines=Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines()
r=json.loads(next(s.split('=',1)[1] for s in lines if s.startswith('body_receipt=')))
assert lines[-1].startswith('PASS:')
assert (r['Reads'],r['ProfileSources'],r['ProfileSourceBytes'])==(39,5,8290)
assert all(r[k] for k in ['Unchanged','NoReads','ArchiveClosed','Detached','RotationNeutral'])
root='object/creature/player/shared_human_male.iff';appearance='appearance/hum_m.sat'
root_pin='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
appearance_pin='951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C'
field_paths={name:dict(SourcePath='object/creature/base/shared_base_creature.iff',Family='SCOT',Version='0013') for name in ['collisionHeight','collisionRadius','collisionOffsetX','collisionOffsetZ','collisionLength','warpTolerance']}
field_paths['stepHeight']=dict(SourcePath='object/creature/player/base/shared_base_player.iff',Family='SCOT',Version='0013')
field_paths['appearanceFilename']=dict(SourcePath=root,Family='SHOT',Version='0007')
assert len(r['Samples'])==8
for sample,(h,special) in zip(r['Samples'],[(h,s) for h in [1,1.4,.25,2] for s in [False,True]]):
    h=f32(h);step=1 if special else .5;radius=.5
    assert bits(sample['Height'])==bits(h) and sample['SpecialDimensions']==special
    expected={'Radius':f32(radius*h),'StepHeight':f32(step*h),'CollisionHeight':f32(f32(1.8)*h),'Length':f32(1.5*h),'WarpTolerance':f32(17*h)}
    assert all(bits(sample[k])==bits(v) for k,v in expected.items())
    assert [bits(v) for v in sample['LocalCenter']]==[bits(0),bits(f32(f32(step+radius)*h)),bits(0)]
    assert sample['RootPath']==root and sample['AppearancePath']==appearance and sample['Fields']==field_paths
    sources=sample['ProfileSources'];assert len(sources)==5 and sum(s['Bytes'] for s in sources.values())==8290
    assert all(key==v['Path'] and v['Bytes']>0 and len(v['Sha256'])==64 for key,v in sources.items())
    assert sample['RootSource']==sources[root] and sample['RootSource']['Sha256']==root_pin
    assert sample['AppearanceSource']['Path']==appearance and sample['AppearanceSource']['Sha256']==appearance_pin
    assert sources==r['Samples'][0]['ProfileSources']
assert [bits(v) for v in r['Offset']['LocalCenter']]==[bits(f32(2*f32(1.4))),bits(f32(1.4)),bits(f32(-3*f32(1.4)))]
expected_names={'null-resources','static','missing-profile','missing-collision','root-path','appearance-path','recipe-crc','root-pin','appearance-pin','missing-fields','field-source','null-field-source','profile-source','negative-step','zero-radius','negative-radius','zero-height','zero-length','negative-warp','nan-offset','infinite-radius','center-overflow','height-overflow','length-overflow','warp-overflow','offset-overflow','radius-underflow','receipt-fields-readonly','receipt-sources-readonly'}
height_names={n for n in r['Negatives'] if n.startswith('height-') and n!='height-overflow'}
assert len(height_names)==7
assert len(r['Negatives'])==36 and set(r['Negatives'])==expected_names|height_names
print(json.dumps(dict(passed=True,original_sources=39,profile_sources=5,profile_source_bytes=8290,samples=8,height_bounds=[.25,2],negative_cases=36,local_axes=True,provenance=True,no_asset_IO=True,no_redecode=True,no_active_original_participation_claim=True),indent=2))
