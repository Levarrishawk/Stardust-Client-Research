"""Independent496-byte peer and deterministic bounded coalescing/state model."""
import json,struct,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from verify_status_vectors import frame
from verify_creature_view3_baselines import fixture
from verify_creature_view3_composite_deltas import parse,apply
import contextlib,hashlib,io
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_dds_pixels import inspect as pixels
from verify_creature_wounds_legacy_policy import emulate
from verify_scene_object_lifecycle import decode
from verify_zone_scene_protocol import parse as scene
peer=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));actual=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
def unframe(wire):
 assert 5<=len(wire)<=496
 data=bytearray(wire[:-2]);chain=0xa1b2c3d4;cursor=2
 while cursor+4<=len(data):
  cipher,=struct.unpack_from('<I',data,cursor);struct.pack_into('<I',data,cursor,cipher^chain);chain=cipher;cursor+=4
 while cursor<len(data):data[cursor]^=chain&255;cursor+=1
 assert data[-1]==0 and frame(bytes(data[:-1]),True)==wire
 return bytes(data[:-1])
assert actual['NegotiatedBytes']==496 and bytes.fromhex(actual['SessionRequest'])==struct.pack('>HIII',1,2,0x12345678,496)
assert bytes.fromhex(peer['response'])==struct.pack('>HII3BI',2,0x12345678,0xa1b2c3d4,2,1,4,496)
assert actual['SelectDatagram']==peer['expectedSelect'] and actual['ReadyDatagram']==peer['expectedReady']
selected=unframe(bytes.fromhex(actual['SelectDatagram']));assert selected[:4]==struct.pack('>HH',9,0);scene(selected[4:],False)
assert unframe(bytes.fromhex(actual['ReadyDatagram']))==struct.pack('>HH',9,1)+bytes.fromhex('0100221CFD43')
names=['start','create','staticCreate','baseline','staticBaseline','composite','end','staticEnd']
assert actual['DeliveredApplications']==[peer[n] for n in names]
for seq,n in enumerate(names):assert unframe(bytes.fromhex(peer['peer_'+n]))==struct.pack('>HH',9,seq)+bytes.fromhex(peer[n])
seed=bytearray(fixture());seed[-16:-12]=struct.pack('<I',0);seed[73:77]=struct.pack('<I',0);seed[85:89]=struct.pack('<i',-1)
assert bytes.fromhex(peer['baseline'])==seed
scene(bytes.fromhex(peer['start']),True)
for name,kind in [('create','create'),('staticCreate','create'),('end','end'),('staticEnd','end')]:decode(kind,bytes.fromhex(peer[name]))
state=apply({'Policy':1,'ComponentsCounter':0},parse(bytes.fromhex(peer['composite']),{'Policy':1}))
assert actual['InitialState']==dict(Revision=2,Posture=state['Posture'],HeightBits=state['HeightBits'],WoundsCounter=state['WoundsCounter'],Wounds=state['Wounds'],ComponentCounter=state['ComponentsCounter'],Components=state['Components'])
assert 119+16+4*len(state['Wounds'])+16+4*len(state['Components'])==175
assert actual['InitialMutation']==7 and actual['AfterMutation']==9 and actual['StaticEnvelopeCount']==2
assert actual['FreshJobs']==2 and actual['FinalJobs']==actual['FinalBytes']==actual['ArchiveScopes']==0
root=Path(sys.argv[3]);configuration=Path(sys.argv[4]);catalog=json.loads(Path(sys.argv[5]).read_text())
assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
entries={r['templateCrc']:r for r in catalog['entries']};routes={r['Kind']:r for r in actual['Routes']};assert set(routes)=={'CreatureDetail0','StaticAppearance'}
source_count=mip_count=0
with contextlib.ExitStack() as stack:
 indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(root,configuration)]
 def lookup(path):
  for priority,index in indices:
   raw=index.lookup(path)
   if raw is not None:return priority,raw
  raise ValueError(path)
 for name,create,count,size,texture_count,mips in [('CreatureDetail0','create',39,4686371,10,80),('StaticAppearance','staticCreate',27,1401794,7,62)]:
  row=routes[name];wire=bytes.fromhex(row['Wire']);assert wire==bytes.fromhex(peer[create])
  crc=struct.unpack_from('<I',wire,42)[0];entry=entries[crc]
  assert row['ExternalReads']==len(row['Sources'])==row['BorrowedBuffers']==count and row['SourceBytes']==size
  assert row['ArchiveClosed'] and row['WorkerThread']!=row['CallerThread']
  sources={r['Path']:r for r in row['Sources']};assert len(sources)==count and sum(r['Bytes'] for r in sources.values())==size
  assert sources[entry['objectPath']]['Sha256']==entry['objectSha256']
  for path,r in sources.items():
   priority,raw=lookup(path);assert r==dict(Path=path,Priority=priority,Bytes=len(raw),Sha256=hashlib.sha256(raw).hexdigest().upper());source_count+=1
  assert len(row['Textures'])==texture_count and sum(len(t['Mips']) for t in row['Textures'])==mips
  for t in row['Textures']:
   assert t['Path'] in sources
   _,raw=lookup(t['Path'])
   with contextlib.redirect_stdout(io.StringIO()):expected=pixels(raw)
   assert t['Mips']==expected;mip_count+=len(expected)
assert source_count==66 and mip_count==142
print('PASS: independent496-byte framing/order, mixed-world state/observer model,66 original source identities and142 independently decoded mip hashes')
