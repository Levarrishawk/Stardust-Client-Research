"""Independent handpacked CREO field4 envelopes; payloads intentionally opaque/synthetic."""
import argparse,json,struct
from pathlib import Path
P=lambda fmt,*v:struct.pack('<'+fmt,*v)

def envelope(oid,count,fields,tag=0x4352454F,view=3,opcode=0x12862153):
    return P('HIQIBIH',5,opcode,oid,tag,view,len(fields)+2,count)+fields

def field(raw,extended):
    return P('H',4)+(P('HI',65535,len(raw)) if extended else P('H',len(raw)))+raw

def produce():
    cases=[]
    def add(name,values,count=1,oid=0xF122334455667788):
        updates=[dict(Hex=b.hex().upper(),Extended=e or len(b)>=65535) for b,e in values]
        fields=b''.join(field(bytes.fromhex(v['Hex']),v['Extended']) for v in updates)
        cases.append(dict(Name=name,ObjectId=oid,DeclaredCount=count,Updates=updates,Wire=envelope(oid,count,fields).hex().upper()))
    add('short-empty',[(b'',False)])
    add('extended-empty',[(b'',True)],0)
    add('embedded',[(bytes.fromhex('0080FF00FEFF0400'),False)],65535,0)
    add('extended-small',[(b'\x00\xff\x80',True)],7)
    add('ordered-repeated',[(b'A',False),(b'',True),(b'A',True),(b'B\0\xff',False)],2)
    add('all-octets',[(bytes(range(256)),False)],0)
    for count in [0,1,2,255,256,65535]:
        add(f'raw-count-{count}',[(b'\xff\x04',False),(b'\0',True)],count)
    for n in [1,2,254,255,256,65534,65535,65536]:
        b=bytes((i*17+3)&255 for i in range(n))
        add(f'length-{n}',[(b,n>=65535)],65535)
    add('maximum-fields',[(b'',i%2==0) for i in range(4096)],1)
    good=bytes.fromhex(cases[0]['Wire'])
    negatives=[]
    def neg(name,b):negatives.append(dict(Name=name,Wire=b.hex().upper()))
    neg('zero-updates',envelope(1,0,b''))
    for index in [0,3,5,11,65535]:neg(f'other-index-{index}',envelope(1,1,P('HH',index,0)))
    neg('mixed-late-index',envelope(1,2,field(b'A',False)+P('HB',11,1)))
    for tail in [b'\x00',b'\x04',b'\x04\x00',b'\x04\x00\xff\xff\x01',b'\x04\x00\xff\xff\xff\xff\xff\xff']:
        neg('partial-tail-'+tail.hex(),envelope(1,1,field(b'OK',False)+tail))
    neg('short-extent',envelope(1,1,P('HH',4,5)+b'AB'))
    neg('extended-extent',envelope(1,1,P('HHI',4,65535,5)+b'AB'))
    for tag in [0x54414E4F,0,0x504C4159]:neg('family-'+str(tag),envelope(1,1,field(b'',False),tag=tag))
    for view in [0,2,4,255]:neg('view-'+str(view),envelope(1,1,field(b'',False),view=view))
    neg('baseline-kind',envelope(1,1,field(b'',False),opcode=0x68A75F0C))
    for pos in [0,1,2,3,4,5,19,20,21,22]:
        b=bytearray(good);b[pos]^=1;neg(f'header-{pos}',bytes(b))
    return dict(Cases=cases,Negatives=negatives)

def read_wire(wire):
    count,op,oid,tag,view,size,declared=struct.unpack_from('<HIQIBIH',wire)
    assert count==5 and op==0x12862153 and tag==0x4352454F and view==3 and size==len(wire)-23
    cursor=25;updates=[]
    while cursor<len(wire):
        index,prefix=struct.unpack_from('<HH',wire,cursor);cursor+=4
        assert index==4
        extended=prefix==65535
        length=struct.unpack_from('<I',wire,cursor)[0] if extended else prefix
        if extended:cursor+=4
        assert cursor+length<=len(wire)
        updates.append(dict(Hex=wire[cursor:cursor+length].hex().upper(),Extended=extended));cursor+=length
    assert updates
    return oid,declared,updates

def verify(peer,export):
    assert len(peer['Cases'])==len(export['Cases'])==21
    for f,r in zip(peer['Cases'],export['Cases']):
        oid,count,updates=read_wire(bytes.fromhex(f['Wire']))
        assert (oid,count,updates)==(f['ObjectId'],f['DeclaredCount'],f['Updates'])
        assert r==dict(Name=f['Name'],ObjectId=oid,DeclaredCount=count,Updates=updates,Wire=f['Wire'])
    for bad in peer['Negatives']:
        try: read_wire(bytes.fromhex(bad['Wire']))
        except (AssertionError,struct.error): pass
        else: raise AssertionError('Independent negative accepted: '+bad['Name'])
    assert export['Negatives']>2000 and export['OwnershipChecks']>4000
    print(f"Independent field4 oracle: {len(peer['Cases'])} exact cases, {export['Negatives']} negative probes, order/raw counts/prefix spelling preserved")

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--generate');p.add_argument('--peer');p.add_argument('--verify');a=p.parse_args()
    if a.generate:Path(a.generate).write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8')
    else:verify(json.loads(Path(a.peer).read_text()),json.loads(Path(a.verify).read_text()))
