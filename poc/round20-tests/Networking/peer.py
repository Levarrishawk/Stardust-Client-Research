"""Independent raw packed fixtures and strict grammar oracle; no production imports."""
import argparse,json
from pathlib import Path

def escape(raw):
    return b''.join(bytes((255,1)) if b==0 else bytes((255,2)) if b==255 else bytes((b,)) for b in raw)

def packed(rows,empty=False):
    if empty:return b''
    raw=bytearray((1,len(rows)))
    for r in rows:
        value=bytes.fromhex(r['Value']);raw.append(r['Id'] | (128 if len(value)==2 else 0));raw.extend(value)
    return escape(raw)+bytes((255,3))

def produce():
    cases=[]
    def add(name,rows,empty=False,wire=None):
        canonical=packed(rows,empty)
        cases.append(dict(Name=name,OuterEmpty=empty,Rows=rows,Wire=(canonical if wire is None else wire).hex().upper(),Canonical=canonical.hex().upper()))
    row=lambda i,*v:dict(Id=i,Value=bytes(v).hex().upper())
    add('outer-empty',[],True);add('version-one-zero-count',[]);add('raw-zero-count',[],wire=bytes.fromhex('0100FF03'))
    for b in range(256):
        add(f'u8-{b}',[row(7,b)])
        add(f'width-two-high-{b}',[row(127,165,b)])
    for i in range(1,128):
        add(f'id-{i}-one',[row(i,255)])
        add(f'id-{i}-two',[row(i,0,128)])
    add('signed-boundaries',[row(1,0,128),row(2,255,127),row(3,255,255),row(4,0,0)])
    add('ordered-duplicates',[row(9,1),row(2,2,3),row(9,4,5),row(2,6)])
    add('maximum-count',[row(127,255,0) for _ in range(255)])
    add('raw-zero-value',[row(1,0)],wire=bytes.fromhex('01010100FF03'))
    return cases

def decode(wire):
    if not wire:return True,[]
    decoded=[];cursor=0
    while cursor<len(wire):
        b=wire[cursor];cursor+=1
        if b==255:
            if cursor==len(wire):raise ValueError()
            code=wire[cursor];cursor+=1
            if code==3:
                if cursor!=len(wire):raise ValueError()
                break
            if code not in (1,2):raise ValueError()
            b=0 if code==1 else 255
        decoded.append(b)
    else:raise ValueError()
    if len(decoded)<2 or decoded[0]!=1:raise ValueError()
    count=decoded[1];offset=2;rows=[]
    for _ in range(count):
        if offset>=len(decoded):raise ValueError()
        tag=decoded[offset];offset+=1;width=2 if tag&128 else 1
        if not tag&127 or offset+width>len(decoded):raise ValueError()
        rows.append(dict(Id=tag&127,Value=bytes(decoded[offset:offset+width]).hex().upper()));offset+=width
    if offset!=len(decoded):raise ValueError()
    return False,rows

def verify(peer,result):
    assert len(result['Cases'])==len(peer)==773
    for fixture,actual in zip(peer,result['Cases']):
        empty,rows=decode(bytes.fromhex(fixture['Wire']))
        assert empty==fixture['OuterEmpty'] and rows==fixture['Rows']
        assert packed(rows,empty).hex().upper()==fixture['Canonical']
        expected=dict(Name=fixture['Name'],OuterEmpty=empty,Rows=rows,Wire=fixture['Wire'],Canonical=fixture['Canonical'],DecodedBytes=0 if empty else 2+sum(1+len(bytes.fromhex(r['Value'])) for r in rows))
        assert actual==expected,(actual,expected)
    assert result['Negatives']>5000 and result['OwnershipChecks']>=len(peer)*4
    print(f"Independent packed oracle: {len(peer)} exact cases; {result['Negatives']} negative probes; owned copies/order/duplicates match")

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--generate');p.add_argument('--peer');p.add_argument('--verify');a=p.parse_args()
    if a.generate:Path(a.generate).write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8')
    else:verify(json.loads(Path(a.peer).read_text()),json.loads(Path(a.verify).read_text()))
