"""Independent ordered legacy kernel and acknowledged world/publication integration audit."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_mixed_deltas import parse
from verify_creature_wounds_legacy_policy import emulate
from verify_scene_creature_palette_customization import profile
from verify_status_vectors import frame
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
v=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
assert v['Delivered']==[peer[k] for k in ('start','create','baseline','delta','end','contain','serverReady')]
assert v['OriginalBaselineHex']==peer['baseline']
assert bytes.fromhex(peer['response'])==struct.pack('>HII3BI',2,0x12345678,0xA1B2C3D4,2,1,4,496)
for seq,name in enumerate(('start','create','baseline','delta','end','contain','serverReady')):
    expected=frame(struct.pack('>HH',9,seq)+bytes.fromhex(peer[name]),True)
    assert expected.hex().upper()==peer['peer_'+name] and len(expected)<=496
scalars=dict(Posture=4,Rank=9,LinkId=0xF102030405060708,HeightBits=0x3F800000,Shock=0xF1234567,States=0xFEDCBA9876543210)
local,values=9,[];revision=1;mutation=4
assert len(v['States'])==3
for name,row in zip(('mixed','equal','growth'),v['States']):
    for index,value in parse(bytes.fromhex(peer[name]),True):
        if index==17:
            next_state=emulate(local,values,[value],16);local,values=next_state['Counter'],next_state['Values']
        else:scalars[{11:'Posture',12:'Rank',13:'LinkId',14:'HeightBits',15:'Shock',16:'States'}[index]]=value
    revision+=1;mutation+=1
    expected=dict(Name=name,Revision=revision,MutationRevision=mutation,**scalars,WoundsCounter=local,Wounds=values,RetainedDecodedBytes=81+16+4*len(values))
    assert row==expected,(row,expected)
assert values==[-2,2147483647,-2,0,0,-7] and local==14
# The valid scalar prefix of the last update must never become a publication.
try:
    for index,value in parse(bytes.fromhex(peer['lateGrowth']),True):
        if index==17:emulate(local,values,[value],16)
except ValueError:pass
else:raise AssertionError('late bound must fail')
expected_profile=profile(Path(sys.argv[3]).read_bytes())
assert {(p['VariableName'],p['Private']):p['Value'] for p in v['Colors']}==expected_profile
assert v['ExternalReads']==39 and v['SourceBytes']==4686371
assert v['SourceObject']=='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
assert all(v[k] is True for k in ('WorkerOffOwner','ArchiveClosed','NoExtraReads','SharedGeometryAndTextures','StateSupersedesResourceRetained','StaleAckRetried','LateFailureRetired','Drained'))
print('PASS: independent496-byte framing, ordered mixed state/counters/no-ops/growth, current acknowledged batches, original palette closure and atomic failure receipts')
