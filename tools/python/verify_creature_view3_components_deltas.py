"""Pinned-instruction components-set oracle; synthetic raw snapshots/delta fixtures."""
import argparse,hashlib,json,struct
from pathlib import Path
from verify_creature_view3_baselines import fixture as full_baseline
P=lambda fmt,*v:struct.pack('<'+fmt,*v)
ID=0xF122334455667788;MASK=0xFFFFFFFF
SHA='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS={'00642F52':'8D5718','00642F94':'0F9CC1','00642FEA':'7D19','00643005':'C645FF00','00643081':'8D3C08','00643086':'0F86A8000000','0064308C':'2BC2','0064308E':'03C1','00643092':'7602','006430B7':'740D','00643105':'0F84C2010000','00643152':'42','006431DF':'0F84E8000000','00643225':'FF4618','00643260':'746B','006432A2':'FF4618','006432E2':'395618','006432E5':'7303','006432E7':'895618','00645583':'0F9CC1','0064562D':'0F8D8B000000','006456B5':'C6400401','006456C5':'C6410400','00644E24':'7C07','00644E54':'7D07','00642BAF':'8B7008','00642BC4':'E85738CB00'}


def anchors(exe):
    import pefile
    assert hashlib.sha256(exe.read_bytes()).hexdigest().upper()==SHA
    pe=pefile.PE(str(exe),fast_load=True)
    for va,raw in ANCHORS.items():
        b=bytes.fromhex(raw);assert pe.get_data(int(va,16)-pe.OPTIONAL_HEADER.ImageBase,len(b))==b,va
    print(f'Pinned component-set executable: {len(ANCHORS)} instruction anchors verified.')


def field(c,ops):
    body=P('HII',5,len(ops),c)
    for k,key in ops:
        body+=P('B',k)
        if k!=2:body+=P('i',key)
    return body


def message(fields,count=0):return P('HIQIBIH',5,0x12862153,ID,0x4352454F,3,len(fields)+2,count)+fields


def parse(wire,keycap=32):
    h=struct.unpack_from('<HIQIBIH',wire)
    if h[:5]!=(5,0x12862153,ID,0x4352454F,3) or h[5]!=len(wire)-23:raise ValueError('envelope')
    pos=25;updates=[];commands=keys=0
    def read(fmt):
        nonlocal pos
        size=struct.calcsize('<'+fmt)
        if pos+size>len(wire):raise ValueError('truncation')
        value=struct.unpack_from('<'+fmt,wire,pos);pos+=size;return value
    while pos<len(wire):
        if len(updates)>=4:raise ValueError('field budget')
        index,n,c=read('HII')
        if index!=5:raise ValueError('unsupported index')
        commands+=n
        if commands>32:raise ValueError('command budget')
        ops=[]
        for _ in range(n):
            kind=read('B')[0]
            if kind not in (0,1,2):raise ValueError('unknown command')
            key=0
            if kind!=2:
                keys+=1
                if keys>keycap:raise ValueError('key budget')
                key=read('i')[0]
            ops.append((kind,key))
        updates.append((n,c,ops))
    return updates


def apply(local,raw,updates,cap):
    members=set(raw)
    if len(members)>cap:raise ValueError('seed size')
    for n,incoming,ops in updates:
        # 00643078..00643094 unsigned gate, wrapping LEA/sub/add and conditional clamp.
        summed=(local+n)&MASK
        if summed<=incoming:skip=0
        else:skip=min((local-incoming+n)&MASK,n)
        for kind,key in ops[skip:]:
            changed=False
            if kind==0:
                if key in members:members.remove(key);changed=True
            elif kind==1:
                if key not in members:
                    if len(members)>=cap:raise ValueError('add capacity')
                    members.add(key);changed=True
            elif kind==2:
                changed=bool(members);members.clear()
            if changed:local=(local+1)&MASK
        local=max(local,incoming) # 006432E2..006432E7 unsigned maximum.
    return dict(Counter=local,Keys=sorted(members))


def cases():
    seed=[-1,-2147483648,-1]
    full=field(5,[(1,17),(1,17),(0,123),(0,-1),(2,0)])+field(7,[(1,-2147483648),(1,2147483647)])
    out=[('golden',0,seed,full,8,32),('count65535',0,seed,full,8,32),('future',0,seed,field(7,[(1,17)]),8,32),
      ('old',9,seed,field(1,[(1,17)]),8,32),('overlap',1,[17],field(2,[(1,17),(1,23)]),8,32),
      ('zero',0,seed,field(7,[]),8,32),('wrapfresh',MASK,seed,field(0,[(1,17)]),8,32),
      ('wrapmax',MASK,seed,field(1,[(1,17)]),8,32),('duplicatewrap',MASK,seed,field(1,[(1,-1)]),8,32),
      ('missingwrap',MASK,seed,field(1,[(0,123)]),8,32),('clearEmptywrap',MASK,[],field(1,[(2,0)]),8,32),
      ('clearNonemptywrap',MASK,seed,field(1,[(2,0)]),8,32),('baselinecounter',0xF1234567,seed,field(0xF1234568,[(1,17)]),8,32),
      ('lateGrowth',0,seed,field(1,[(1,17)])+field(2,[(1,23)]),3,32),
      ('lateUnknownField',0,seed,field(1,[(1,17)])+P('HII',17,0,0),8,32),
      ('lateKeyBudget',0,seed,field(1,[(1,17)])+field(2,[(0,-1)]),8,1),
      ('unknownCommand',0,seed,field(1,[(3,7)]),8,32),('truncatedKey',0,seed,field(1,[(1,17)])[:-1],8,32),
      ('clearZeroKeys',0,[],field(1,[(2,0)]),0,0),('countHuge',0,seed,P('HII',5,MASK,0),8,32)]
    for local in [0,1,2,0x7FFFFFFF,0x80000000,0xFFFFFFFE,MASK]:
        for incoming in [0,1,2,0x7FFFFFFF,0x80000000,0xFFFFFFFE,MASK]:
            out.append((f'counter-{local:08X}-{incoming:08X}',local,seed,field(incoming,[(1,17),(0,-1)]),8,32))
    return out


def main():
    p=argparse.ArgumentParser();p.add_argument('--exe',type=Path,required=True);p.add_argument('--generate',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args();anchors(a.exe)
    raw=[-1,-2147483648,-1];counter=0xF1234567;payload=P('IIiii',3,counter,*raw)
    if a.generate:
        a.generate.mkdir(parents=True,exist_ok=True);b=bytearray(full_baseline());b[85:89]=P('i',-1);(a.generate/'baseline.bin').write_bytes(b)
        (a.generate/'snapshot.bin').write_bytes(payload);specs=[]
        for name,local,keys,fields,cap,keycap in cases():
            filename=name+'.bin';(a.generate/filename).write_bytes(message(fields,65535 if name=='count65535' else 0))
            specs.append(dict(Name=name,File=filename,Counter=local,RawKeys=keys,Elements=cap,KeyBudget=keycap))
        (a.generate/'cases.json').write_text(json.dumps(specs),encoding='utf-8')
    if a.verify:
        assert (a.verify/'snapshot.bin').read_bytes()==payload
        assert json.loads((a.verify/'seed.json').read_text())==dict(ObjectId=ID,Counter=counter,RawCount=3,RawKeys=raw,Keys=sorted(set(raw)))
        expected=[]
        for name,local,keys,fields,cap,keycap in cases():
            wire=message(fields,65535 if name=='count65535' else 0);decoded=False
            try:
                updates=parse(wire,keycap);decoded=True;assert (a.verify/(name+'.bin')).read_bytes()==wire,name
                final=apply(local,keys,updates,cap);expected.append(dict(Name=name,Decoded=True,Accepted=True,**final))
            except ValueError:expected.append(dict(Name=name,Decoded=decoded,Accepted=False,Counter=local,Keys=sorted(set(keys))))
        actual=json.loads((a.verify/'results.json').read_text());assert actual==expected,'Independent components state/counter mismatch'
        print(f'Independent raw/normalized baseline, exact wire and instruction-derived state verified for {len(expected)} cases.')


if __name__=='__main__':main()
