"""Independently authored STF variant1 fixtures and two targeted original byte/key/ID oracles."""
import contextlib, hashlib, json, struct, sys, unicodedata
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
PINS={'string/en/obj_n.stf':(2527,'3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054'), 'string/en/terminal_name.stf':(4610,'CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603')}
def encode(values,keys,reserved=0x1234,next_id=0xFFEEDDCC,variant=1,count=None):
    result=struct.pack('<HHBII',0xABCD,reserved,variant,next_id,len(values) if count is None else count)
    for ident,crc,units in values:result+=struct.pack('<III',ident,crc,len(units))+struct.pack('<'+'H'*len(units),*units)
    for ident,key in keys:result+=struct.pack('<II',ident,len(key))+key
    return result
def fixtures():
    values=[(0,0xFFFFFFFF,[0,0xD800,65,0xDC00,0xD83D,0xDE00,92,10,0xFFFF]),(0xFFFFFFFF,0,[65,66])]
    keys=[(0xFFFFFFFF,b'Key\x80\0\xff'),(0,b'key\\')];good=encode(values,keys);result=[]
    def add(name,raw,reject=False):result.append(dict(Name=name,Hex=raw.hex().upper(),Reject=reject))
    add('raw-boundary-permuted',good);add('empty-table',encode([],[]));add('empty-value-and-key',encode([(7,9,[])],[(7,b'')]))
    add('case-high-nul-distinct',encode([(i,i,[i]) for i in range(1,6)],[(i,key) for i,key in enumerate([b'key',b'Key',b'key\0',b'key\x80',b'key\xff'],1)]))
    for i in range(len(good)):add('truncate-'+str(i),good[:i],True)
    add('magic',b'\0\0'+good[2:],True)
    for variant in (0,2,255):add('variant-'+str(variant),encode(values,keys,variant=variant),True)
    add('duplicate-value-id',encode([values[0],values[0]],keys),True)
    add('duplicate-key-id',encode(values,[(0,b'a'),(0,b'b')]),True)
    add('duplicate-key-bytes',encode(values,[(0,b'a'),(0xFFFFFFFF,b'a')]),True)
    add('orphan-key-and-value',encode(values,[(0,b'a'),(99,b'b')]),True)
    add('count-max',encode(values,keys,count=0xFFFFFFFF),True)
    add('value-length-max',good[:21]+struct.pack('<I',0xFFFFFFFF)+good[25:],True)
    key_start=13+sum(12+len(v[2])*2 for v in values)
    add('key-length-max',good[:key_start+4]+struct.pack('<I',0xFFFFFFFF)+good[key_start+8:],True)
    add('trailing-zero',good+b'\0',True);add('trailing-data',good+b'extra',True)
    return result
def display_units(units):
    out=[];i=0
    while i<len(units):
        u=units[i];i+=1
        if 0xD800<=u<=0xDBFF and i<len(units) and 0xDC00<=units[i]<=0xDFFF:out.append(chr(0x10000+((u-0xD800)<<10)+(units[i]-0xDC00)));i+=1
        elif 0xD800<=u<=0xDFFF:out.append('\uFFFD')
        elif u==92:out.append('\\\\')
        elif unicodedata.category(chr(u))=='Cc':out.append('\\u%04X'%u)
        else:out.append(chr(u))
    return ''.join(out)
def display_key(raw):return ''.join('\\\\' if b==92 else chr(b) if 32<=b<=126 else '\\x%02X'%b for b in raw)
def decode(raw):
    magic,reserved,variant,next_id,count=struct.unpack_from('<HHBII',raw);assert magic==0xABCD and variant==1;cursor=13;values=[];keys={};key_order=[]
    for _ in range(count):
        ident,crc,length=struct.unpack_from('<III',raw,cursor);cursor+=12;units=list(struct.unpack_from('<'+'H'*length,raw,cursor));cursor+=length*2;values.append((ident,crc,units))
    for _ in range(count):
        ident,length=struct.unpack_from('<II',raw,cursor);cursor+=8;keys[ident]=raw[cursor:cursor+length];cursor+=length;key_order.append(ident)
    assert cursor==len(raw)
    return dict(Header=dict(Magic=magic,ReservedRaw=reserved,Variant=variant,MaxNextIdentityRaw=next_id,RecordCount=count),KeyPassIds=key_order,TotalValueUnits=sum(len(v[2]) for v in values),TotalKeyBytes=sum(len(v) for v in keys.values()),Records=[dict(Id=i,ValueCrcRaw=crc,KeyHex=keys[i].hex().upper(),Units=u,DisplayKey=display_key(keys[i]),DisplayText=display_units(u)) for i,crc,u in values])
if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Independent STF fixtures:',len(fixtures()));sys.exit()
root,config,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:')
r={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]};synthetic=r['stf_synthetic'];assert len(synthetic['Cases'])==len(fixtures())
for fixture,actual in zip(fixtures(),synthetic['Cases']):
    assert actual['Name']==fixture['Name'] and actual['Rejected']==fixture['Reject']
    if not fixture['Reject']:assert actual['Table']==decode(bytes.fromhex(fixture['Hex']))
assert synthetic['BudgetNegatives']==6 and synthetic['Owned'] and synthetic['ExactLimits'] and synthetic['ConcurrentLookups']==32
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
    indices=[(p,Index(stack.enter_context(f.open('rb')))) for p,f in archives(root,config)]
    original=r['stf_original'];assert len(original['Tables'])==2 and original['ArchiveClosed'] and original['Owned']
    for actual in original['Tables']:
        path=actual['Path'];assert path in PINS
        for priority,index in indices:
            raw=index.lookup(path)
            if raw is not None:break
        else:raise AssertionError(path)
        size,pin=PINS[path];assert (priority,len(raw),hashlib.sha256(raw).hexdigest().upper())==(53,size,pin)
        assert actual['Priority']==priority and actual['Bytes']==size and actual['Sha256']==pin and actual['Table']==decode(raw)
        assert actual['LookupMatches']==len(actual['Table']['Records']) and actual['Table']['Header']['ReservedRaw']==0
print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),malformed=sum(f['Reject'] for f in fixtures()),budget_negatives=6,original_tables=[dict(path=a['Path'],bytes=a['Bytes'],sha256=a['Sha256'],records=a['Table']['Header']['RecordCount'],variant=a['Table']['Header']['Variant'],max_next_raw=a['Table']['Header']['MaxNextIdentityRaw'],units=a['Table']['TotalValueUnits'],key_bytes=a['Table']['TotalKeyBytes']) for a in original['Tables']],variant0_supported=False,raw_units_and_keys_preserved=True,locale_manager=False),indent=2))
