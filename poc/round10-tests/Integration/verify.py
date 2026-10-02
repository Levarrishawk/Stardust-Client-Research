import json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_creature_view3_baselines import fixture
from verify_creature_view3_scalar_deltas import parse,KEY
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));v=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
assert v['Delivered']==[peer[k] for k in ('start','create','baseline','delta','end','contain','serverReady')]
assert bytes.fromhex(v['OriginalBaselineHex'])==fixture()
_,updates=parse(bytes.fromhex(peer['delta']))
s=dict(Posture=255,Rank=254,LinkId=0xfe02030405060708,HeightBits=0xbfe00000,Shock=0xf1223344,States=0xf0e1d2c3b4a59687)
for u in updates:s[KEY[u['Index']]]=u['RawBits']
assert v['Scalars']==s and v['ExternalReads']==39 and v['SourceBytes']==4686371
assert v['SourceObject']=='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
assert all(v[k] is True for k in ('StateSupersedesResourceRetained','MalformedGenerationRetired','WorkerOffOwner','ArchiveClosed','Drained'))
print('PASS: independent delivered wire, baseline/scalar bits, original source accounting and ownership/failure receipts')
