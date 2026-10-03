"""Independent synthetic ordered field4/scalar wire producer and exact-byte oracle."""
import argparse,json,struct,math
from pathlib import Path
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
WIDTH={11:1,12:1,13:8,14:4,15:4,16:8}
def envelope(fields,count=65535,tag=0x4352454F,view=3,opcode=0x12862153):
    return P('HIQIBIH',5,opcode,0xF122334455667788,tag,view,len(fields)+2,count)+fields

def row(index,raw,extended=False):return dict(Index=index,Hex=raw.hex().upper(),Extended=extended or index==4 and len(raw)>=65535)
def field(r):
    b=bytes.fromhex(r['Hex'])
    return P('H',r['Index'])+((P('HI',65535,len(b)) if r['Extended'] else P('H',len(b))) if r['Index']==4 else b'')+b

def produce():
    cases=[];invalid=[]
    raw=row(4,bytes.fromhex('010101FF040080FF00'))
    def add(name,rows,count=65535):cases.append(dict(Name=name,Rows=rows,DeclaredCount=count,Wire=envelope(b''.join(map(field,rows)),count).hex().upper()))
    add('raw-only',[raw],0);add('scalar-only',[row(11,b'\xff')],0)
    values={11:[0,255],12:[0,255],13:[0,0xFFFFFFFFFFFFFFFF,0xFEDCBA9876543210],
            14:[0,0x80000000,1,0x80000001,0x007FFFFF,0x00800000,0x3F800000,0xBF800000,0x7F7FFFFF,0xFF7FFFFF],
            15:[0,0xFFFFFFFF,0xF1234567],16:[0,0xFFFFFFFFFFFFFFFF,0xFEDCBA9876543210]}
    for index,vs in values.items():
        for value in vs:
            scalar=row(index,value.to_bytes(WIDTH[index],'little'))
            add(f'raw-first-{index}-{value}',[raw,scalar,row(4,b'',True)],0)
            add(f'scalar-first-{index}-{value}',[scalar,row(4,b'\x00\xff\x80',True),raw,scalar],2)
    allscalars=[row(i,vs[-1].to_bytes(WIDTH[i],'little')) for i,vs in values.items()]
    add('all-raw-between',[raw]+allscalars[:3]+[row(4,b'',False)]+allscalars[3:]+[raw],1)
    add('all-reversed',list(reversed(allscalars))+[row(4,bytes(range(256)),False)],65535)
    for count in [0,1,2,255,256,65535]:add(f'raw-count-{count}',[raw,row(14,P('I',0x3F800000))],count)
    add('extended-boundary',[row(14,P('I',0x80000000)),row(4,b'A'*65535),row(15,P('I',0xFFFFFFFF))],7)
    add('maximum-fields',[row(4,b'',i%2==0) if i%2==0 else row(11,bytes((i&255,))) for i in range(4096)],1)
    def bad(name,fields,**kw):invalid.append(dict(Name=name,Wire=envelope(fields,**kw).hex().upper()))
    bad('zero-updates',b'')
    for index in [0,3,5,10,17,18,65535]:bad(f'unknown-late-{index}',field(raw)+P('H',index))
    for bits in [0x7F800000,0xFF800000,0x7FC00000,0xFFC00000,0x7F800001]:bad(f'nonfinite-{bits}',field(raw)+P('HI',14,bits))
    for tail in [b'\x04',P('H',4),P('HH',4,65535)+b'\0',P('HHI',4,65535,0xFFFFFFFF),P('HH',4,4)+b'AB',P('H',13)+b'1234567']:
        bad('truncated-'+tail.hex(),field(raw)+tail)
    for tag in [0,0x54414E4F]:bad(f'tag-{tag}',field(raw),tag=tag)
    for view in [0,2,4,255]:bad(f'view-{view}',field(raw),view=view)
    bad('baseline-kind',field(raw),opcode=0x68A75F0C)
    return dict(Cases=cases,Negatives=invalid)

def parse(wire):
    count,opcode,oid,tag,view,size,declared=struct.unpack_from('<HIQIBIH',wire)
    assert count==5 and opcode==0x12862153 and tag==0x4352454F and view==3 and size==len(wire)-23
    cursor=25;rows=[]
    while cursor<len(wire):
        index=struct.unpack_from('<H',wire,cursor)[0];cursor+=2;extended=False
        if index==4:
            prefix=struct.unpack_from('<H',wire,cursor)[0];cursor+=2;extended=prefix==65535
            width=struct.unpack_from('<I',wire,cursor)[0] if extended else prefix
            if extended:cursor+=4
        else:assert index in WIDTH;width=WIDTH[index]
        assert cursor+width<=len(wire)
        raw=wire[cursor:cursor+width];cursor+=width
        if index==14:assert math.isfinite(struct.unpack('<f',raw)[0])
        rows.append(row(index,raw,extended))
    assert rows
    return oid,declared,rows

def verify(peer,result):
    assert len(peer['Cases'])==len(result['Cases'])
    for f,r in zip(peer['Cases'],result['Cases']):
        oid,count,rows=parse(bytes.fromhex(f['Wire']))
        assert rows==f['Rows'] and count==f['DeclaredCount']
        assert r==dict(Name=f['Name'],ObjectId=oid,DeclaredCount=count,Rows=rows,Wire=f['Wire'])
    for f in peer['Negatives']:
        try:parse(bytes.fromhex(f['Wire']))
        except (AssertionError,struct.error):pass
        else:raise AssertionError('Negative accepted '+f['Name'])
    assert result['Negatives']>3000 and result['OwnershipChecks']>4000
    print(f"Independent mixed oracle: {len(peer['Cases'])} exact cases/{result['Negatives']} negatives; ordered raw/scalar values, float bits and spelling match")

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--generate');p.add_argument('--peer');p.add_argument('--verify');a=p.parse_args()
    if a.generate:Path(a.generate).write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8')
    else:verify(json.loads(Path(a.peer).read_text()),json.loads(Path(a.verify).read_text()))
