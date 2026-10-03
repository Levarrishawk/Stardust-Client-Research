"""Independent twelve-chunk ACST synthetic fixtures and targeted original schema/query oracle."""
import contextlib, hashlib, json, struct, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from audit_tre_filename_crc import swg_crc

TAGS=['NAME','PNOF','VNOF','DEFV','IRNG','RTYP','UCMP','ULST','UIDX','LLST','LIDX','CIDX']
WIDTHS=[1,2,2,4,8,2,3,2,5,2,5,6]
def pack(fmt,rows):return b''.join(struct.pack('<'+fmt,*r) for r in rows)
def chunk(tag,raw):return tag.encode()+struct.pack('>I',len(raw))+raw
def form(tag,raw):return chunk('FORM',tag.encode()+raw)
def encode(t):return form('ACST',form('0000',b''.join(chunk(k,t[k]) for k in TAGS)))
def fixture_tables():
    names=['palette/synthetic.pal','/private/direct','/private/inherited','/shared_owner/shared','/private/nested','/private/Direct']
    pool=b'';offset=[]
    for name in names:offset.append(len(pool));pool+=name.encode()+b'\0'
    return dict(NAME=pool,PNOF=pack('H',[(offset[0],)]),VNOF=pack('H',[(i,) for i in offset[1:]]),
        DEFV=pack('i',[(-7,),(3,),(9,),(55,)]),IRNG=pack('ii',[(-10,10),(0,20)]),RTYP=pack('H',[(1,),(0x8001,),(2,)]),
        UCMP=pack('BBB',[(1,1,1),(1,3,3),(3,2,2),(2,3,3),(4,2,4),(5,1,1)]),ULST=pack('H',[(1,),(2,),(3,),(4,),(5,),(1,),(6,)]),
        UIDX=pack('HHB',[(10,0,1),(20,1,3),(30,4,3)]),LLST=pack('H',[(20,),(30,),(30,)]),LIDX=pack('HHB',[(10,0,2),(20,2,1)]),
        CIDX=pack('IH',[(0xAA22,10),(0xAA11,20)]))
def fixtures():
    t=fixture_tables(); result=[]
    def add(name,changes=None,reject=False,raw=None):result.append(dict(Name=name,Hex=(raw if raw is not None else encode(t | (changes or {}))).hex().upper(),Reject=reject))
    add('inheritance-precedence');add('duplicate-pool-spelling',dict(NAME=t['NAME']+b'palette/synthetic.pal\0'))
    add('all-empty',{k:b'' for k in TAGS})
    add('signed-int32-edges',dict(IRNG=pack('ii',[(-2147483648,2147483647),(0,20)]),DEFV=pack('i',[(-2147483648,),(2147483647,),(9,),(55,)])))
    add('authored-default-outside-range',dict(DEFV=pack('i',[(-77,),(3,),(9,),(55,)])))
    for tag,width in zip(TAGS[1:],WIDTHS[1:]):add('width-'+tag,{tag:t[tag]+b'X'},True)
    for name,tag,raw in [('name-unterminated','NAME',t['NAME'][:-1]),('name-control','NAME',b'\x01\0'),('name-empty','NAME',b'\0'),
       ('palette-offset','PNOF',pack('H',[(1,)])),('variable-offset','VNOF',pack('H',[(65535,)])),
       ('type-zero','RTYP',pack('H',[(0,)])),('type-palette','RTYP',pack('H',[(0x8002,)])),('type-range','RTYP',pack('H',[(3,)])),
       ('variable-index','UCMP',pack('BBB',[(0,1,1)])),('type-index','UCMP',pack('BBB',[(1,0,1)])),('default-index','UCMP',pack('BBB',[(1,1,0)])),
       ('value-index','ULST',pack('H',[(0,)])),('value-outside','ULST',pack('H',[(7,)])),
       ('direct-key-zero','UIDX',pack('HHB',[(0,0,1)])),('direct-slice','UIDX',pack('HHB',[(10,65535,1)])),
       ('direct-count','UIDX',pack('HHB',[(10,6,2)])),('direct-duplicate','UIDX',t['UIDX']+pack('HHB',[(10,0,1)])),
       ('link-key-zero','LIDX',pack('HHB',[(0,0,1)])),('link-slice','LIDX',pack('HHB',[(10,3,1)])),
       ('link-duplicate','LIDX',t['LIDX']+pack('HHB',[(10,0,1)])),('link-missing','LLST',pack('H',[(99,)])),('link-zero','LLST',pack('H',[(0,)])),
       ('crc-missing','CIDX',pack('IH',[(0xAA22,99)])),('crc-duplicate','CIDX',t['CIDX']+pack('IH',[(0xAA22,10)])),
       ('range-equal','IRNG',pack('ii',[(10,10),(0,20)])),('range-reverse','IRNG',pack('ii',[(10,-10),(0,20)]))]:add(name,{tag:raw},True)
    add('cycle',dict(LLST=pack('H',[(10,),(30,),(30,)])),True)
    add('unreachable-cycle',dict(LLST=t['LLST']+pack('H',[(40,)]),LIDX=t['LIDX']+pack('HHB',[(40,3,1)])),True)
    raw=encode(t)
    add('root',raw=raw[:8]+b'XXXX'+raw[12:],reject=True);add('version',raw=raw[:20]+b'0001'+raw[24:],reject=True)
    add('trailing',raw=raw+b'\0',reject=True);add('truncated',raw=raw[:-1],reject=True)
    add('missing-chunk',raw=form('ACST',form('0000',b''.join(chunk(k,t[k]) for k in TAGS[:-1]))),reject=True)
    add('order',raw=form('ACST',form('0000',b''.join(chunk(k,t[k]) for k in [TAGS[1],TAGS[0],*TAGS[2:]]))),reject=True)
    return result
def decode(raw):
    assert raw[:4]==b'FORM' and raw[8:12]==b'ACST' and raw[20:24]==b'0000'
    assert [struct.unpack_from('>I',raw,o)[0] for o in (4,16)]==[len(raw)-8,len(raw)-20]
    t={};cursor=24
    for tag in TAGS:
        assert raw[cursor:cursor+4]==tag.encode();n=struct.unpack_from('>I',raw,cursor+4)[0];t[tag]=raw[cursor+8:cursor+8+n];cursor+=8+n
    assert cursor==len(raw);return t
def tables(t):
    names={};cursor=0
    while cursor<len(t['NAME']):end=t['NAME'].index(0,cursor);names[cursor]=t['NAME'][cursor:end].decode('ascii');cursor=end+1
    rows={k:list(struct.iter_unpack('<'+fmt,t[k])) for k,fmt in [('PNOF','H'),('VNOF','H'),('DEFV','i'),('IRNG','ii'),('RTYP','H'),('UCMP','BBB'),('ULST','H'),('UIDX','HHB'),('LLST','H'),('LIDX','HHB'),('CIDX','IH')]}
    variables=[]
    for vn,rt,dv in rows['UCMP']:
        name=names[rows['VNOF'][vn-1][0]];typ=rows['RTYP'][rt-1][0];default=rows['DEFV'][dv-1][0];idx=(typ&32767)-1
        variables.append(dict(Kind='Palette' if typ&32768 else 'Ranged',FullName=name,AuthoredDefault=default,PalettePath=names[rows['PNOF'][idx][0]] if typ&32768 else None,MinimumInclusive=rows['IRNG'][idx][0] if not typ&32768 else None,MaximumExclusive=rows['IRNG'][idx][1] if not typ&32768 else None))
    direct={k:[rows['ULST'][i][0]-1 for i in range(start,start+n)] for k,start,n in rows['UIDX']}
    links={k:[rows['LLST'][i][0] for i in range(start,start+n)] for k,start,n in rows['LIDX']}
    roots=dict(rows['CIDX']);inv=[dict(RowIndex=i,PreviousCrc=rows['CIDX'][i-1][0],CurrentCrc=c) for i,(c,_) in enumerate(rows['CIDX']) if i and c<rows['CIDX'][i-1][0]]
    meta=dict(TableCounts={k:len(t[k])//w for k,w in zip(TAGS,WIDTHS)},NameStrings=len(names),DuplicateNameStrings=len(names)-len(set(names.values())),KnownKeys=len(set(direct)|set(links)),CrcOrderingInversions=inv)
    return variables,direct,links,roots,meta
def query(parsed,crc,suppress=False):
    variables,direct,links,roots,_=parsed
    if crc not in roots:return dict(FilenameCrc=crc,Found=False,RootKey=None,Variables=[])
    result=[];seen=set()
    def visit(key):
        for index in direct.get(key,[]):
            v=variables[index]
            if v['FullName'] in seen:continue
            seen.add(v['FullName'])
            if not suppress or '/shared_owner/' not in v['FullName']:result.append(v)
        for child in links.get(key,[]):visit(child)
    visit(roots[crc]);return dict(FilenameCrc=crc,Found=True,RootKey=roots[crc],Variables=result)

if sys.argv[1]=='generate':Path(sys.argv[2]).write_text(json.dumps(fixtures(),indent=2),encoding='utf-8');print('Synthetic directory fixtures:',len(fixtures()));sys.exit()
root,config,output=map(Path,sys.argv[2:]);lines=output.read_text(encoding='utf-8-sig').splitlines();assert lines[-1].startswith('PASS:')
r={k:json.loads(v) for line in lines if '=' in line for k,v in [line.split('=',1)]}
for fixture,actual in zip(fixtures(),r['synthetic']['Cases']):
    assert fixture['Name']==actual['Name'] and fixture['Reject']==actual['Rejected']
    if not fixture['Reject']:
        parsed=tables(decode(bytes.fromhex(fixture['Hex'])));assert actual['Metadata']==parsed[-1]
        assert actual['Queries']==[query(parsed,0xAA22),query(parsed,0xAA22,True),query(parsed,99)]
assert len(r['synthetic']['Cases'])==len(fixtures()) and r['synthetic']['Owned'] and r['synthetic']['BudgetNegatives']==7
assert hashlib.sha256(config.read_bytes()).hexdigest().upper()=='22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
with contextlib.ExitStack() as stack:
    for priority,archive in archives(root,config):
        raw=Index(stack.enter_context(archive.open('rb'))).lookup('customization/asset_customization_manager.iff')
        if raw is not None:break
    else:raise AssertionError('Missing ACST')
pin=hashlib.sha256(raw).hexdigest().upper();assert (priority,archive.name,len(raw),pin)==(53,'stardust_3_01.tre',458340,'5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20')
parsed=tables(decode(raw));original=r['acst'];assert original['Metadata']==parsed[-1] and original['Sha256']==pin and original['Bytes']==len(raw) and original['Priority']==priority
assert original['Queries']==[query(parsed,swg_crc(b'appearance/hum_m.sat')),query(parsed,swg_crc(b'appearance/hum_m.sat'),True),query(parsed,swg_crc(b'appearance/togruta_f.sat')),query(parsed,swg_crc(b'appearance/togruta_f.sat'),True)]
assert original['Owned'] and original['ArchiveClosed'] and parsed[-1]['KnownKeys']==26082 and parsed[-1]['CrcOrderingInversions']==[dict(RowIndex=26079,PreviousCrc=0xFFFD72EB,CurrentCrc=0xE201EE84)]
print(json.dumps(dict(passed=True,synthetic_cases=len(fixtures()),malformed=sum(f['Reject'] for f in fixtures()),budget_negatives=7,bytes=len(raw),sha256=pin,known_keys=26082,queries=[dict(crc=q['FilenameCrc'],root=q['RootKey'],variables=len(q['Variables'])) for q in original['Queries']],original_inversions=1,palette_counts_resolved=False,live_seed_recovered=False),indent=2))
