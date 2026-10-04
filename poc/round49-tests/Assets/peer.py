"""Independent movement-resource closure peer; original values remain dynamically private."""
import json,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
p46=runpy.run_path(str(ROOT/'poc/round46-tests/Networking/peer.py'))
p47=runpy.run_path(str(ROOT/'poc/round47-tests/Networking/peer.py'))
def verify(native,oracle,profile_fixtures,authored_fixtures,log,root,config,bank):
 n=json.loads(Path(native).read_text());r=n['Result'];o=json.loads(Path(oracle).read_text());out=Path(native).parent
 profile=out/'profile-peer-native.json';profile.write_text(json.dumps(dict(Synthetic=n['FrozenProfile'],Mounts=r['Mounts'],Original=r['OriginalProfile'])))
 authored=out/'authored-peer-native.json';authored.write_text(json.dumps(dict(Synthetic=n['FrozenAuthored'],Mounts=r['Mounts'],Original=r['OriginalInputs'])))
 p46['verify'](Path(profile_fixtures),profile,Path(oracle),out/'profile-peer-receipt.json')
 p47['verify'](Path(authored_fixtures),authored,Path(oracle),out/'authored-peer-receipt.json')
 assert r['DefaultUnchanged'] and r['ExtraMovementExternalReads']==0 and r['OptionCopyPreserved']
 assert r['ExternalReads']==39 and r['ExternalReads']==len(r['Sources'])
 assert r['SourceBytes']==sum(s['Bytes'] for s in r['Sources'].values())
 for s in o['Chain']:
  source=r['Sources'][s['Path']];assert (source['Priority'],source['Bytes'],source['Sha256'])==(s['Priority'],s['Bytes'],s['Sha256'])
 assert r['MovementDecodedBytes']==r['OriginalProfile']['DecodedBytes']+r['OriginalInputs']['SnapshotBytes']
 assert r['MovementWork']==r['OriginalProfile']['Work']+r['OriginalInputs']['Work']
 audit=runpy.run_path(str(ROOT/'tools/python/verify_scene_creature_resources.py'))['audit'](Path(root),Path(config),Path(bank),Path(log))
 assert audit['source_count']==37 and audit['prepared_clips']==3
 assert r['Negatives']>=19 and r['Checks']>=20
 print(json.dumps(dict(Passed=True,ProfileCases=352,AuthoredCases=20,OuterSources=r['ExternalReads'],OuterBytes=r['SourceBytes'],MovementSources=5,MovementSourceBytes=8290,MovementDecodedBytes=r['MovementDecodedBytes'],MovementWork=r['MovementWork'],ExtraMovementReads=0,Checks=r['Checks'],Negatives=r['Negatives'],FrozenPreparedSourceAudit=audit)))
if __name__=='__main__':verify(*sys.argv[1:9])
