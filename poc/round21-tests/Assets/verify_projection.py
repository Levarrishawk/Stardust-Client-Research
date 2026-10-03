"""Independent authored packed bytes/local integer oracle; original dictionary mapping only, no ACST claim."""
import contextlib, hashlib, json, struct, sys
from pathlib import Path

def wire(records, outer=False):
    if outer: return ''
    raw=bytes([1,len(records)])+b''.join(bytes([i | (128 if len(v)==2 else 0)])+v for i,v in records)
    return (b''.join(b'\xff\x01' if b==0 else b'\xff\x02' if b==255 else bytes([b]) for b in raw)+b'\xff\x03').hex().upper()

def fixtures():
    result=[dict(Name='outer-empty',Wire=''),dict(Name='count-zero',Wire=wire([]))]
    for i in range(256):
        result.append(dict(Name=f'u8-signed-{i}',Wire=wire([(1,bytes([i]))])))
        result.append(dict(Name=f'u8-palette-{i}',Wire=wire([(2,bytes([i]))])))
        result.append(dict(Name=f's16-high-{i}',Wire=wire([(1,bytes([90,i]))])))
    for i in (-32768,-32767,-129,-128,-101,-100,-99,-1,0,1,3,4,127,128,255,256,299,300,32766,32767):
        for ident in (1,2):result.append(dict(Name=f'signed-edge-{ident}-{i}',Wire=wire([(ident,struct.pack('<h',i))])))
    sets={'valid-invalid':[(1,b'*'),(1,b'\x00\x80')], 'invalid-valid':[(1,b'\x00\x80'),(1,b'*')],
          'duplicates':[(1,b'*'),(2,b'\x03'),(1,b'\x2b'),(1,b'\xff\xff'),(2,b'\x04')],
          'skip-names':[(3,b'\xff\xff'),(4,b'\x01'),(5,b'\x02'),(1,b'\x05')],
          'unknown-first':[(6,b'\x01')], 'unknown-late':[(1,b'*'),(6,b'\x01')],
          'maximum-records':[(1,b'*' if i%2==0 else b'\x00\x80') for i in range(255)]}
    result.extend(dict(Name=n,Wire=wire(r)) for n,r in sets.items());return result

def decode(hexadecimal):
    if not hexadecimal:return True,[]
    b=bytes.fromhex(hexadecimal);cursor=0
    def read():
        nonlocal cursor
        v=b[cursor];cursor+=1
        if v==255:
            e=b[cursor];cursor+=1;assert e in (1,2);return 0 if e==1 else 255
        return v
    assert read()==1;count=read();records=[]
    for _ in range(count):
        tag=read();value=bytes([read(),read()]) if tag&128 else bytes([read()]);records.append((tag&127,value))
    assert b[cursor:]==b'\xff\x03';return False,records

def project(full):
    for prefix,private in (('/private/',True),('/shared_owner/',False)):
        if full.startswith(prefix) and len(full)>len(prefix):return full[len(prefix):],private
    return None

def expected(hexadecimal,names,ranges,seed):
    outer,records=decode(hexadecimal)
    if any(i not in names for i,_ in records):return dict(Failed=True)
    values=seed.copy();diagnostics=[]
    for index,(ident,raw) in enumerate(records):
        attempted=raw[0] if len(raw)==1 else int.from_bytes(raw,'little',signed=True)
        full=names[ident];key=project(full);before=after=None
        if key is None:decision='SkippedUnsupportedNamespace'
        elif key not in ranges:decision='SkippedUndeclaredConsumer'
        else:
            before=values[key];lo,hi=ranges[key]
            if lo<=attempted<hi:values[key]=attempted;decision='Applied'
            else:decision='RetainedOutOfRange'
            after=values[key]
        diagnostics.append(dict(RecordIndex=index,Id=ident,FullName=full,VariableName=key[0] if key else None,Private=key[1] if key else None,AttemptedValue=attempted,PreviousValue=before,ResultValue=after,Decision=decision))
    return dict(Failed=False,IsOuterEmpty=outer,Values=[dict(VariableName=k[0],Private=k[1],Value=v) for k,v in sorted(values.items())],Diagnostics=diagnostics)

if sys.argv[1]=='generate':
    Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Generated independently authored fixtures:',len(fixtures()));sys.exit()
root,config,profile,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:')
rows={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]}
names={1:'/private/signed',2:'/shared_owner/palette',3:'private/anomaly',4:'/private/absent',5:'/other/unknown'}
ranges={('signed',True):(-100,300),('palette',False):(0,4)};seed={('signed',True):17,('palette',False):2}
native=rows['projection'];f=fixtures();assert len(native['Fixtures'])==len(f)
for fixture,actual in zip(f,native['Fixtures']):assert actual==dict(Name=fixture['Name'],**expected(fixture['Wire'],names,ranges,seed)),fixture['Name']
assert native['Ownership'] and native['ConcurrentIndependent'] and native['Capacity']==4096 and native['ContextNegatives']==13
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
    for priority,archive in archives(root,config):
        raw=Index(stack.enter_context(archive.open('rb'))).lookup('customization/customization_id_manager.iff')
        if raw is not None:break
    else:raise AssertionError('CIDM missing')
pin=hashlib.sha256(raw).hexdigest().upper();assert (priority,len(raw),pin)==(53,3697,'FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2')
assert raw[:4]==b'FORM' and raw[8:12]==b'CIDM' and raw[20:24]==b'0001' and raw[24:28]==b'DATA'
assert [struct.unpack_from('>I',raw,i)[0] for i in (4,16,28)]==[len(raw)-8,len(raw)-20,len(raw)-32]
originalNames={};cursor=32
while cursor<len(raw):
    ident=struct.unpack_from('<H',raw,cursor)[0];cursor+=2;end=raw.index(0,cursor);originalNames[ident]=raw[cursor:end].decode('ascii');cursor=end+1
assert len(originalNames)==121 and list(originalNames)==list(range(1,122)) and originalNames[118]=='private/alternate_shader_blade'
original=rows['original_projection'];profileRows=json.loads(profile.read_text())['values'];supplied={(p['variableName'],p['factoryFlag']):p['value'] for p in profileRows}
assert len(supplied)==8 and original['DictionarySha256']==pin and original['ProfileSha256']==hashlib.sha256(profile.read_bytes()).hexdigest().upper()
assert original['BoundsPolicy']=='supplied-consumer-DescribeControls-min0-maxPlus1-not-ACST'
contexts={}
for m in original['Mappings']:
    key=(m['VariableName'],m['Private']);assert project(originalNames[m['Id']])==key and originalNames[m['Id']]==m['FullName']
    assert m['MinimumInclusive']==0 and m['MaximumExclusive']>1 and m['Current']==supplied[key];contexts[key]=(0,m['MaximumExclusive'])
assert set(contexts)==set(supplied)
oracle=expected(original['Wire'],originalNames,contexts,supplied)
assert original['Values']==oracle['Values'] and original['Diagnostics']==oracle['Diagnostics'] and len(oracle['Diagnostics'])==17
assert all(v['Value']==1 for v in oracle['Values']) and original['Owned'] and original['ArchiveClosed'] and (original['ExternalReads'],original['SourceBytes'])==(76,5940390)
print(json.dumps(dict(passed=True,fixtures=len(f),unknown_atomic_failures=sum(r['Failed'] for r in native['Fixtures']),context_negatives=native['ContextNegatives'],original_profile_keys=8,original_ordered_records=17,dictionary_sha256=pin,consumer_bounds_supplied=True,acst_ranges_recovered=False,gpu=False),indent=2))
