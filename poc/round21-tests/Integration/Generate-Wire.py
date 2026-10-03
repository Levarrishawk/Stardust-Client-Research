"""Generate independent synthetic field-4 packets and an exact private original CIDM preload."""
import argparse,contextlib,hashlib,json,struct,sys
from pathlib import Path
workspace=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(workspace/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('root',type=Path);parser.add_argument('config',type=Path);parser.add_argument('mixed_peer',type=Path)
parser.add_argument('peer_output',type=Path);parser.add_argument('dictionary_output',type=Path)
parser.add_argument('--fault',choices=['malformed','unknown'])
a=parser.parse_args()
private=(workspace/'local-private').resolve()
for target in (a.peer_output,a.dictionary_output):
 assert target.resolve().is_relative_to(private) and target.parent.is_dir(),target
assert hashlib.sha256(a.config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
 for priority,archive in archives(a.root,a.config):
  data=Index(stack.enter_context(archive.open('rb'))).lookup('customization/customization_id_manager.iff')
  if data is not None:break
 else:raise AssertionError('Missing original CIDM')
pin=hashlib.sha256(data).hexdigest().upper()
assert (priority,len(data),pin)==(53,3697,'FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2')
assert data[0:4]==b'FORM' and data[8:12]==b'CIDM' and data[20:24]==b'0001' and data[24:28]==b'DATA'
assert struct.unpack_from('>I',data,4)[0]==len(data)-8 and struct.unpack_from('>I',data,28)[0]==len(data)-32
ids={};cursor=32;expected_id=1
while cursor<len(data):
 ident=struct.unpack_from('<H',data,cursor)[0];cursor+=2;end=data.index(0,cursor);name=data[cursor:end].decode('ascii');cursor=end+1
 assert ident==expected_id and name not in ids;ids[name]=ident;expected_id+=1
assert expected_id==122 and cursor==len(data)
profiles={name:json.loads((workspace/f'poc/preview-profiles/human-customization-{name}.json').read_text(encoding='utf-8'))['values'] for name in ('zero','varied')}
def ident(row):return ids[('/private/' if row['factoryFlag'] else '/shared_owner/')+row['variableName']]
def packed(records):
 assert len(records)<=255
 decoded=bytearray([1,len(records)])
 for id,value,width in records:
  assert 1<=id<=127 and width in (1,2)
  decoded.append(id|(128 if width==2 else 0));decoded.extend(struct.pack('<B' if width==1 else '<h',value))
 escaped=bytearray()
 for b in decoded:escaped.extend(bytes([255,1 if b==0 else 2]) if b in (0,255) else bytes([b]))
 return bytes(escaped)+bytes([255,3])
full={name:packed([(ident(r),r['value'],1) for r in rows]) for name,rows in profiles.items()}
base=json.loads(a.mixed_peer.read_text(encoding='utf-8-sig'));baseline=bytes.fromhex(base['human_baseline']);object_id=struct.unpack_from('<Q',baseline,6)[0];tag=baseline[14:18]
assert baseline[18]==3
skin=ids['/private/index_color_skin']
def delta(raw,extended=False,declared=1):
 string=(struct.pack('<HI',65535,len(raw)) if extended else struct.pack('<H',len(raw)))+raw
 fields=struct.pack('<H',4)+string;payload=struct.pack('<H',declared)+fields
 return (struct.pack('<HIQ',5,0x12862153,object_id)+tag+bytes([3])+struct.pack('<I',len(payload))+payload).hex().upper()
messages={
 'wire_zero':delta(full['zero']), 'wire_varied':delta(full['varied']),
 'wire_invalid_skin':delta(packed([(skin,-1,2)])),
 'wire_empty':delta(b''), 'wire_count_zero':delta(packed([])),
 'wire_duplicate':delta(packed([(skin,75,1),(skin,-1,2)])),
 'wire_reset':delta(full['zero']), 'wire_extended_zero':delta(full['zero'],True),
 'wire_unknown_id':delta(packed([(127,1,1)])), 'wire_invalid_inner':delta(bytes([1,1])),
}
if a.fault:messages['wire_extended_zero']=messages['wire_invalid_inner' if a.fault=='malformed' else 'wire_unknown_id']
result=dict(version=1,synthetic=True,ObjectId=object_id,DictionarySha256=pin,DictionaryBytes=len(data),DictionaryPriority=priority,Keys=[dict(r,Id=ident(r)) for r in profiles['zero']],Messages=messages,
 ExpectedFaceHashes=dict(zero='A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808',varied='60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5'))
a.dictionary_output.write_bytes(data);a.peer_output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS: synthetic ordered field4 fixture and pinned private original CIDM; ten frames, eight explicit consumer keys')
