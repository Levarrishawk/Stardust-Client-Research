"""Independent ordered set/vector kernels and original-resource world integration audit."""
import json, struct, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_composite_deltas import parse
from verify_creature_view3_components_deltas import apply as apply_components
from verify_creature_wounds_legacy_policy import emulate
from verify_scene_creature_palette_customization import profile
from verify_status_vectors import frame
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
v=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
assert v['Delivered']==[peer[k] for k in ('start','create','baseline','delta','end','contain','serverReady')]
assert v['OriginalBaselineHex']==peer['baseline']
assert bytes.fromhex(peer['response'])==struct.pack('>HII3BI',2,0x12345678,0xA1B2C3D4,2,1,4,496)
for seq,name in enumerate(('start','create','baseline','delta','end','contain','serverReady')):
    raw=frame(struct.pack('>HH',9,seq)+bytes.fromhex(peer[name]),True)
    assert raw.hex().upper()==peer['peer_'+name] and len(raw)<=496
scalars=dict(Posture=4,Rank=9,LinkId=0xF102030405060708,HeightBits=0x3F800000,Shock=0xF1234567,States=0xFEDCBA9876543210)
wound_counter,values=9,[]
component_counter,keys=0,[-2147483648,-1]
revision,mutation=1,4
names={11:'Posture',12:'Rank',13:'LinkId',14:'HeightBits',15:'Shock',16:'States'}
assert len(v['States'])==3
for name,row in zip(('composite','equal','growth'),v['States']):
    for index,value in parse(bytes.fromhex(peer[name]),{'Policy':1}):
        if index==5:
            state=apply_components(component_counter,keys,[value],16)
            component_counter,keys=state['Counter'],state['Keys']
        elif index==17:
            state=emulate(wound_counter,values,[value],16)
            wound_counter,values=state['Counter'],state['Values']
        else:scalars[names[index]]=value
    revision+=1;mutation+=1
    expected=dict(Name=name,Revision=revision,MutationRevision=mutation,**scalars,WoundsCounter=wound_counter,Wounds=values,ComponentsCounter=component_counter,Components=keys,RetainedDecodedBytes=93+16+4*len(values)+16+4*len(keys))
    assert row==expected,(row,expected)
assert keys==[-2147483648,2147483647] and component_counter==5
assert values==[-2,2147483647,-2,0,0,-7] and wound_counter==14
try:
    for index,value in parse(bytes.fromhex(peer['lateGrowth']),{'Policy':1}):
        if index==5:apply_components(component_counter,keys,[value],16)
        elif index==17:emulate(wound_counter,values,[value],16)
except ValueError:pass
else:raise AssertionError('late wound growth must fail')
expected_profile=profile(Path(sys.argv[3]).read_bytes())
assert {(p['VariableName'],p['Private']):p['Value'] for p in v['Colors']}==expected_profile
assert v['ExternalReads']==39 and v['SourceBytes']==4686371
assert v['SourceObject']=='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
assert all(v[k] is True for k in ('WorkerOffOwner','ArchiveClosed','NoExtraReads','SharedGeometryAndTextures','StateSupersedesResourceRetained','ExplicitRouter','CombinedStoreRollback','CompositeWorldBounds','StaleAckRetried','LateFailureRetired','Drained'))
print('PASS: independently framed composite world, distinct counters,93-byte raw baseline, normalized ownership/accounting, original palette route and atomic late failure')
